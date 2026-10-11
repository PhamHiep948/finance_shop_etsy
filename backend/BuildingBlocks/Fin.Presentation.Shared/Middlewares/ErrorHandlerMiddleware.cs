using System.Data.Common;
using System.Net;
using System.Text.Json;
using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Exceptions;
using Fin.Infrastructure.Shared.Extensions;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Presentation.Shared.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fin.Presentation.Shared.Middlewares
{
    public sealed class ErrorHandlerMiddleware
    {
        private const string DatabaseErrorMessage = "Không kết nối được MySQL. Kiểm tra XAMPP, chuỗi kết nối và đã chạy Fin.Migration chưa.";
        // Relaxed escaping keeps Vietnamese messages readable in the raw response, like the controllers' own output.
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlerMiddleware> _logger;
        private readonly AppSettingOption _appSettings;

        public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger, IOptions<AppSettingOption> appSettings)
        {
            _next = next;
            _logger = logger;
            _appSettings = appSettings.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception error)
            {
                if (context.Response.HasStarted)
                {
                    // Headers are already on the wire, so no JSON body can be written any more.
                    _logger.LogError(error, "Unhandled exception after the response started for {Method} {Path}", context.Request.Method, context.Request.Path);
                    throw;
                }

                await WriteJsonAsync(context, error);
            }
        }

        private async Task WriteJsonAsync(HttpContext context, Exception error)
        {
            var response = context.Response;
            response.ContentType = "application/json";
            var body = new Response<string> { Succeeded = false, Message = error.Message, ErrorCode = ErrorCodeEnum.COM_ERR_000.ToString() };

            switch (error)
            {
                case ValidationException e:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    body.Errors = e.Errors;
                    body.Message = e.Errors.Count > 0 ? string.Join(" ", e.Errors) : e.Message;
                    break;
                case ApiException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                case KeyNotFoundException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;
                case DbException:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    _logger.LogError(error, "Database failure for {Method} {Path}", context.Request.Method, context.Request.Path);
                    body.Message = DatabaseErrorMessage;
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    _logger.LogError(error, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
                    if (_appSettings.EnableErrorDetail)
                    {
                        body.Data = error.ToString();
                    }
                    else
                    {
                        // Internal messages can leak connection strings or table names; clients only get a generic text.
                        body.Message = ErrorCodeEnum.COM_ERR_000.GetDescription();
                    }
                    break;
            }

            await response.WriteAsync(JsonSerializer.Serialize(body, SerializerOptions));
        }
    }
}
