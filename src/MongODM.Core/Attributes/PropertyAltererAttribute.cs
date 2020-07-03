using System;

namespace Etherna.MongODM.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class PropertyAltererAttribute : Attribute
    {
        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="propertyName">The related property name</param>
        public PropertyAltererAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }

        public string PropertyName { get; }
    }
}
