using System;

namespace Digicando.ExecContext.Exceptions
{
    public class ExecutionContextNotFoundException : Exception
    {
        public ExecutionContextNotFoundException(string message) : base(message)
        { }

        public ExecutionContextNotFoundException(string message, Exception innerException) : base(message, innerException)
        { }

        public ExecutionContextNotFoundException()
        { }
    }
}
