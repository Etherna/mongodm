//   Copyright 2020-present Etherna Sagl
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

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
