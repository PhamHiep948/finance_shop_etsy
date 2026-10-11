using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Extensions;

namespace Fin.Infrastructure.Shared.Wrappers
{
    public class Response<T>
    {
        public bool Succeeded { get; set; }
        public string? Message { get; set; }
        public string? ErrorCode { get; set; }
        public List<string>? Errors { get; set; }
        public T? Data { get; set; }

        public static Response<T> Ok(T data, string? message = null) => new() { Succeeded = true, Data = data, Message = message };

        public static Response<T> Fail(ErrorCodeEnum errorCode) => Fail(errorCode, errorCode.GetDescription());

        public static Response<T> Fail(ErrorCodeEnum errorCode, string message) =>
            new() { Succeeded = false, Message = message, ErrorCode = errorCode.ToString() };
    }
}
