using System;

namespace Etherna.MongODM.Core.Exceptions
{
    public class MongodmIndexBuildingException : Exception
    {
        public MongodmIndexBuildingException()
        {
        }

        public MongodmIndexBuildingException(string message) : base(message)
        {
        }

        public MongodmIndexBuildingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
