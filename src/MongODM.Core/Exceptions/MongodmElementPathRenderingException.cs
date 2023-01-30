using System;

namespace Etherna.MongODM.Core.Exceptions
{
    public class MongodmElementPathRenderingException : Exception
    {
        public MongodmElementPathRenderingException()
        {
        }

        public MongodmElementPathRenderingException(string message) : base(message)
        {
        }

        public MongodmElementPathRenderingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
