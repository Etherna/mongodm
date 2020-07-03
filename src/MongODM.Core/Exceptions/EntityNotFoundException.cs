using System;

namespace Etherna.MongODM.Exceptions
{
    public class EntityNotFoundException : Exception
    {
        // Constructors.
        public EntityNotFoundException()
        { }

        public EntityNotFoundException(string message) : base(message)
        { }

        public EntityNotFoundException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
