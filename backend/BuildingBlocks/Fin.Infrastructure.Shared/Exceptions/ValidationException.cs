using FluentValidation.Results;

namespace Fin.Infrastructure.Shared.Exceptions
{
    public class ValidationException : Exception
    {
        public ValidationException() : base("Dữ liệu gửi lên không hợp lệ.")
        {
        }

        public ValidationException(string message) : base(message)
        {
        }

        public ValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ValidationException(IEnumerable<ValidationFailure> failures) : this()
        {
            Errors = failures.Select(f => f.ErrorMessage).Distinct().ToList();
        }

        public List<string> Errors { get; } = [];
    }
}
