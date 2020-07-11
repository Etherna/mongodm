using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.MongODM.ProxyModels
{
    public abstract class ModelInterceptorBase<TModel> : IInterceptor
    {
        private readonly IEnumerable<Type> additionalInterfaces;

        public ModelInterceptorBase(IEnumerable<Type> additionalInterfaces)
        {
            this.additionalInterfaces = additionalInterfaces;
        }

        public void Intercept(IInvocation invocation)
        {
            if (invocation is null)
                throw new ArgumentNullException(nameof(invocation));

            if (additionalInterfaces.Contains(invocation.Method.DeclaringType))
            {
                var handled = InterceptInterface(invocation);
                if (handled)
                    return;
                invocation.Proceed();
            }
            else
            {
                // Check model type.
                if (invocation.Method.DeclaringType != typeof(TModel) &&
                    !invocation.Method.DeclaringType.IsAssignableFrom(typeof(TModel)))
                {
                    throw new InvalidOperationException();
                }

                InterceptModel(invocation);
            }
        }

        /// <summary>
        /// Intercept an extra interface
        /// </summary>
        /// <param name="invocation">Current invocation</param>
        /// <returns>Returns true if the call handled the invocation</returns>
        protected virtual bool InterceptInterface(IInvocation invocation)
        {
            return false;
        }

        /// <summary>
        /// Intercept a call to the model
        /// </summary>
        /// <param name="invocation">Current invocation</param>
        protected virtual void InterceptModel(IInvocation invocation)
        {
            if (invocation is null)
                throw new ArgumentNullException(nameof(invocation));

            invocation.Proceed();
        }
    }
}
