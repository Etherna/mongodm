using Castle.DynamicProxy;
using Digicando.DomainHelper.Attributes;
using Digicando.DomainHelper.ProxyModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Digicando.MongoDM.ProxyModels
{
    public class AuditableInterceptor<TModel> : ModelInterceptorBase<TModel>
    {
        // Fields.
        private bool isAuditingEnabled;
        private readonly HashSet<MemberInfo> changedMembers = new HashSet<MemberInfo>();

        // Constructors.
        public AuditableInterceptor(IEnumerable<Type> additionalInterfaces)
            : base(additionalInterfaces)
        { }

        // Protected methods.
        protected override bool InterceptInterface(IInvocation invocation)
        {
            // Intercept ISummarizable invocations
            if (invocation.Method.DeclaringType == typeof(IAuditable))
            {
                if (invocation.Method.Name == $"get_{nameof(IAuditable.IsAuditingEnabled)}")
                    invocation.ReturnValue = isAuditingEnabled;
                else if (invocation.Method.Name == $"get_{nameof(IAuditable.IsChanged)}")
                    invocation.ReturnValue = changedMembers.Any();
                else if (invocation.Method.Name == $"get_{nameof(IAuditable.ChangedMembers)}")
                    invocation.ReturnValue = changedMembers;
                else if (invocation.Method.Name == nameof(IAuditable.DisableAuditing))
                    isAuditingEnabled = false;
                else if (invocation.Method.Name == nameof(IAuditable.EnableAuditing))
                    isAuditingEnabled = true;
                else if (invocation.Method.Name == nameof(IAuditable.ResetChangedMembers))
                    changedMembers.Clear();
                else
                    throw new NotSupportedException();

                return true;
            }

            return false;
        }

        protected override void InterceptModel(IInvocation invocation)
        {
            // Filter sets.
            if (isAuditingEnabled)
            {
                if (invocation.Method.Name.StartsWith("set_"))
                {
                    var propertyName = invocation.Method.Name.Substring(4);
                    var propertyInfo = typeof(TModel).GetMember(propertyName).Single();

                    // Add property to edited set.
                    changedMembers.Add(propertyInfo);
                }
                else if (invocation.Method.Name.StartsWith("get_"))
                {
                    //ignore get
                }
                else //normal methods
                {
                    var alteredPropertiesName = from attribute in invocation.Method.GetCustomAttributes<PropertyAltererAttribute>(true)
                                                select attribute.PropertyName;
                    var propertiesInfo = from propertyName in alteredPropertiesName
                                         select typeof(TModel).GetMember(propertyName).Single();

                    // Add properties to edited set.
                    foreach (var propertyInfo in propertiesInfo)
                        changedMembers.Add(propertyInfo);
                }
            }

            invocation.Proceed();
        }
    }
}
