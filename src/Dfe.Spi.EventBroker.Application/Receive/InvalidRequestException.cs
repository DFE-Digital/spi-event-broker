using System;

namespace Dfe.Spi.EventBroker.Application.Receive
{
    public class InvalidRequestException : Exception
    {
        public string Code { get; }

        public InvalidRequestException(string code, string message, Exception innnerException = null)
            : base(message, innnerException)
        {
            Code = code;
        }
    }
}