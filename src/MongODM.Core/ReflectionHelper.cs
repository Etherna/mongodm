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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace Etherna.MongODM.Core
{
    public static class ReflectionHelper
    {
        private static readonly Dictionary<Type, IEnumerable<PropertyInfo>> propertyRegister = new Dictionary<Type, IEnumerable<PropertyInfo>>();
        private static readonly ReaderWriterLockSlim propertyRegisterLock = new ReaderWriterLockSlim();

        public static MemberInfo FindProperty(LambdaExpression lambdaExpression)
        {
            if (lambdaExpression is null)
                throw new ArgumentNullException(nameof(lambdaExpression));

            Expression expressionToCheck = lambdaExpression;

            bool done = false;

            while (!done)
            {
                switch (expressionToCheck.NodeType)
                {
                    case ExpressionType.Convert:
                        expressionToCheck = ((UnaryExpression)expressionToCheck).Operand;
                        break;
                    case ExpressionType.Lambda:
                        expressionToCheck = ((LambdaExpression)expressionToCheck).Body;
                        break;
                    case ExpressionType.MemberAccess:
                        var memberExpression = (MemberExpression)expressionToCheck;

                        if (memberExpression.Expression.NodeType != ExpressionType.Parameter &&
                            memberExpression.Expression.NodeType != ExpressionType.Convert)
                        {
                            throw new ArgumentException(
                                $"Expression '{lambdaExpression}' must resolve to top-level member and not any child object's properties. Use a custom resolver on the child type or the AfterMap option instead.",
                                nameof(lambdaExpression));
                        }

                        MemberInfo member = memberExpression.Member;

                        return member;
                    default:
                        done = true;
                        break;
                }
            }

            throw new InvalidOperationException();
        }

        public static PropertyInfo FindPropertyImplementation(PropertyInfo interfacePropertyInfo, Type actualType)
        {
            if (interfacePropertyInfo is null)
                throw new ArgumentNullException(nameof(interfacePropertyInfo));
            if (actualType is null)
                throw new ArgumentNullException(nameof(actualType));

            var interfaceType = interfacePropertyInfo.DeclaringType;

            // An interface map must be used because because there is no
            // other officially documented way to derive the explicitly
            // implemented property name.
            var interfaceMap = actualType.GetInterfaceMap(interfaceType);

            var interfacePropertyAccessors = interfacePropertyInfo.GetAccessors(true);

            var actualPropertyAccessors = interfacePropertyAccessors.Select(interfacePropertyAccessor =>
            {
                var index = Array.IndexOf(interfaceMap.InterfaceMethods, interfacePropertyAccessor);

                return interfaceMap.TargetMethods[index];
            });

            // Binding must be done by accessor methods because interface
            // maps only map accessor methods and do not map properties.
            return actualType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(propertyInfo =>
                {
                    // we are looking for a property that implements all the required accessors
                    var propertyAccessors = propertyInfo.GetAccessors(true);
                    return actualPropertyAccessors.All(x => propertyAccessors.Contains(x));
                });
        }

        public static MemberInfo GetMemberInfoFromLambda<TModel, TMember>(
            Expression<Func<TModel, TMember>> memberLambda,
            Type? actualType = null)
        {
            if (memberLambda is null)
                throw new ArgumentNullException(nameof(memberLambda));

            var body = memberLambda.Body;
            MemberExpression memberExpression;
            switch (body.NodeType)
            {
                case ExpressionType.MemberAccess:
                    memberExpression = (MemberExpression)body;
                    break;
                case ExpressionType.Convert:
                    var convertExpression = (UnaryExpression)body;
                    memberExpression = (MemberExpression)convertExpression.Operand;
                    break;
                default:
                    throw new InvalidOperationException("Invalid lambda expression");
            }
            var memberInfo = memberExpression.Member;
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    break;
                case MemberTypes.Property:
                    if (actualType?.IsInterface == false &&
                        memberInfo.DeclaringType.IsInterface)
                    {
                        memberInfo = FindPropertyImplementation((PropertyInfo)memberInfo, actualType);
                    }
                    break;
                default:
                    memberInfo = null;
                    break;
            }
            if (memberInfo == null)
            {
                throw new InvalidOperationException("Invalid lambda expression");
            }
            return memberInfo;
        }

        public static object? GetValue(object source, MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo fieldInfo)
                return fieldInfo.GetValue(source);

            if (memberInfo is PropertyInfo propertyInfo && propertyInfo.CanRead)
                return propertyInfo.GetValue(source);

            return null;
        }

        public static TMember GetValueFromLambda<TModel, TMember>(TModel source, Expression<Func<TModel, TMember>> memberLambda)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var memberInfo = GetMemberInfoFromLambda(memberLambda, source.GetType());
            return (TMember)GetValue(source, memberInfo)!;
        }

        /// <summary>
        /// Return the list of writable instance property of a type
        /// </summary>
        /// <typeparam name="TModel">The model type</typeparam>
        /// <returns>The list of properties</returns>
        public static IEnumerable<PropertyInfo> GetWritableInstanceProperties(Type objectType)
        {
            if (objectType is null)
                throw new ArgumentNullException(nameof(objectType));

            propertyRegisterLock.EnterReadLock();
            try
            {
                if (propertyRegister.ContainsKey(objectType))
                {
                    return propertyRegister[objectType];
                }
            }
            finally
            {
                propertyRegisterLock.ExitReadLock();
            }

            propertyRegisterLock.EnterWriteLock();
            try
            {
                if (!propertyRegister.ContainsKey(objectType))
                {
                    var typeStack = new List<Type>();
                    var stackType = objectType;
                    do
                    {
                        typeStack.Add(stackType);
                        stackType = stackType.BaseType;
                    } while (stackType != null);

                    propertyRegister.Add(objectType, typeStack
                        .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                        .Where(prop => prop.CanWrite));
                }
                return propertyRegister[objectType];
            }
            finally
            {
                propertyRegisterLock.ExitWriteLock();
            }
        }

        public static void SetValue(object destination, MemberInfo memberInfo, object? value)
        {
            if (memberInfo is FieldInfo fieldInfo)
            {
                if (!fieldInfo.IsInitOnly)
                    fieldInfo.SetValue(destination, value);
                return;
            }

            if (memberInfo is PropertyInfo propertyInfo && propertyInfo.CanWrite)
                propertyInfo.SetValue(destination, value);
        }

        public static void SetValue<TModel, TMember>(TModel destination, Expression<Func<TModel, TMember>> memberLambda, TMember value)
        {
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));

            var memberInfo = GetMemberInfoFromLambda(memberLambda, destination.GetType());
            SetValue(destination, memberInfo, value);
        }
    }
}
