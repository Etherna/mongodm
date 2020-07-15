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
            invocation.Proceed();
        }
    }
}
