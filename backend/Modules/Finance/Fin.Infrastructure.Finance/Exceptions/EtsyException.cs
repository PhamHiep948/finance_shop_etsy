using Fin.Infrastructure.Shared.Exceptions;

namespace Fin.Infrastructure.Finance.Exceptions
{
    public sealed class EtsyException : ApiException
    {
        public EtsyException()
        {
        }

        public EtsyException(string message) : base(message)
        {
        }

        public EtsyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
