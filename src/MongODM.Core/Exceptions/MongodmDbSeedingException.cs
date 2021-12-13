using System;

namespace Etherna.MongODM.Core.Exceptions
{
    public class MongodmDbSeedingException : Exception
    {
        public MongodmDbSeedingException()
        {
        }

        public MongodmDbSeedingException(string message) : base(message)
        {
        }

        public MongodmDbSeedingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
