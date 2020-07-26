using System;

namespace Etherna.MongODM.AspNetCoreSample.Models
{
    public class Cat : EntityModelBase<string>
    {
        public Cat(string name, DateTime birthday)
        {
            Name = name;
            Birthday = birthday;
        }
        protected Cat() { }

        public virtual int Age => (int)((DateTime.Now - Birthday).TotalDays / 365);
        public virtual DateTime Birthday { get; protected set; }
        public virtual string Name { get; protected set; }
    }
}
