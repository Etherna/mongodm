using Castle.DynamicProxy;
using Digicando.DomainHelper;
using Digicando.DomainHelper.Attributes;
using Digicando.DomainHelper.ProxyModel;
using Digicando.MongODM.Models;
using Digicando.MongODM.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Digicando.MongODM.ProxyModels
{
    public class ReferenceableInterceptor<TModel, TKey> : ModelInterceptorBase<TModel>
        where TModel : class, IEntityModel<TKey>
    {
        // Fields.
        private bool isSummary;
        private readonly Dictionary<string, bool> settedMemberNames = new Dictionary<string, bool>(); //<memberName, isFromSummary>
        private readonly IRepository<TModel, TKey> repository;

        // Constructors.
        public ReferenceableInterceptor(
            IEnumerable<Type> additionalInterfaces,
            IDbContext dbContext)
            : base(additionalInterfaces)
        {
            repository = (IRepository<TModel, TKey>)dbContext.RepositoryRegister.ModelRepositoryMap[typeof(TModel)];
        }

        // Protected methods.
        protected override bool InterceptInterface(IInvocation invocation)
        {
            // Intercept ISummarizable invocations
            if (invocation.Method.DeclaringType == typeof(IReferenceable))
            {
                if (invocation.Method.Name == $"get_{nameof(IReferenceable.IsSummary)}")
                {
                    invocation.ReturnValue = isSummary;
                }
                else if (invocation.Method.Name == $"get_{nameof(IReferenceable.SettedMemberNames)}")
                {
                    invocation.ReturnValue = settedMemberNames.Select(pair => pair.Key);
                }
                else if (invocation.Method.Name == nameof(IReferenceable.ClearSettedMembers))
                {
                    settedMemberNames.Clear();
                }
                else if (invocation.Method.Name == nameof(IReferenceable.MergeFullModel))
                {
                    MergeFullModel((TModel)invocation.Proxy, invocation.GetArgumentValue(0) as TModel);
                }
                else if (invocation.Method.Name == nameof(IReferenceable.SetAsSummary))
                {
                    isSummary = true;

                    var summaryLoadedMemberNames = (invocation.GetArgumentValue(0) as IEnumerable<string>).ToArray();
                    foreach (var memberName in summaryLoadedMemberNames)
                        settedMemberNames[memberName] = true;
                }
                else
                {
                    throw new NotSupportedException();
                }
                return true;
            }

            return false;
        }

        protected override void InterceptModel(IInvocation invocation)
        {
            // Filter gets.
            if (invocation.Method.Name.StartsWith("get_") && isSummary)
            {
                var propertyName = invocation.Method.Name.Substring(4);

                // If member is not loaded, load the full object.
                if (!settedMemberNames.ContainsKey(propertyName))
                {
                    var task = FullLoadAsync((TModel)invocation.Proxy);
                    task.Wait();
                }
            }

            // Filter sets.
            else if (invocation.Method.Name.StartsWith("set_"))
            {
                var propertyName = invocation.Method.Name.Substring(4);

                // Report property as setted.
                settedMemberNames[propertyName] = false;
            }

            // Filter normal methods.
            else
            {
                var attributes = invocation.Method.GetCustomAttributes<PropertyAltererAttribute>(true) ?? new PropertyAltererAttribute[0];
                foreach (var propertyName in from attribute in attributes
                                             select attribute.PropertyName)
                {
                    if (isSummary)
                    {
                        // If member is not setted and is summary, load the full object.
                        if (!settedMemberNames.ContainsKey(propertyName))
                        {
                            var task = FullLoadAsync((TModel)invocation.Proxy);
                            task.Wait();

                            break;
                        }
                    }
                    else
                    {
                        // Report property as setted.
                        settedMemberNames[propertyName] = false;
                    }
                }
            }

            invocation.Proceed();
        }

        // Helpers.
        private async Task FullLoadAsync(TModel model)
        {
            if (isSummary)
            {
                // Merge full object to current.
                var fullModel = await repository.TryFindOneAsync(model.Id);
                MergeFullModel(model, fullModel);
            }
        }

        private void MergeFullModel(TModel model, TModel? fullModel)
        {
            if (fullModel != null)
            {
                // Temporary disable auditing.
                (model as IAuditable)?.DisableAuditing();

                // Copy from full object every member in list that is not already loaded
                foreach (var member in ReflectionHelper.GetWritableInstanceProperties(typeof(TModel))
                                       .Where(info => !settedMemberNames.ContainsKey(info.Name) || settedMemberNames[info.Name])
                                       .ToArray())
                {
                    var value = ReflectionHelper.GetValue(fullModel, member);
                    ReflectionHelper.SetValue(model, member, value);
                }

                // Reenable auditing.
                (model as IAuditable)?.EnableAuditing();
            }

            isSummary = false;
        }
    }
}
