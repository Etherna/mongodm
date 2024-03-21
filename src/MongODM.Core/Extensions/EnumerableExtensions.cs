// Copyright 2020-present Etherna SA
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Etherna.MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Etherna.MongODM.Core.Extensions
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Order and paginate a list of elements
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="TKey">Ordering key type</typeparam>
        /// <param name="values">Source values</param>
        /// <param name="orderKeySelector">Ordering key selector</param>
        /// <param name="page">Page to take</param>
        /// <param name="take">Elements per page</param>
        /// <returns>Selected elements page</returns>
        /// <exception cref="ArgumentOutOfRangeException">Throw with invalid parameter values</exception>
        public static IEnumerable<TSource> Paginate<TSource, TKey>(
            this IEnumerable<TSource> values,
            Func<TSource, TKey> orderKeySelector,
            int page,
            int take)
		{
            if (page < 0)
                throw new ArgumentOutOfRangeException(nameof(page), page, "Value can't be negative");
            if (take < 1)
                throw new ArgumentOutOfRangeException(nameof(take), take, "Value can't be less than 1");

            return values.OrderBy(orderKeySelector)
                         .Skip(page * take)
                         .Take(take);
        }

        /// <summary>
        /// Order and paginate a list of elements
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="TKey">Ordering key type</typeparam>
        /// <param name="values">Source values</param>
        /// <param name="orderKeySelector">Ordering key selector</param>
        /// <param name="page">Page to take</param>
        /// <param name="take">Elements per page</param>
        /// <returns>Selected elements page</returns>
        /// <exception cref="ArgumentOutOfRangeException">Throw with invalid parameter values</exception>
        public static IMongoQueryable<TSource> Paginate<TSource, TKey>(
            this IMongoQueryable<TSource> values,
            Expression<Func<TSource, TKey>> orderKeySelector,
            int page,
            int take)
        {
            if (page < 0)
                throw new ArgumentOutOfRangeException(nameof(page), page, "Value can't be negative");
            if (take < 1)
                throw new ArgumentOutOfRangeException(nameof(take), take, "Value can't be less than 1");

            return values.OrderBy(orderKeySelector)
                         .Skip(page * take)
                         .Take(take);
        }

        /// <summary>
        /// Descending order and paginate a list of elements
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="TKey">Ordering key type</typeparam>
        /// <param name="values">Source values</param>
        /// <param name="orderKeySelector">Ordering key selector</param>
        /// <param name="page">Page to take</param>
        /// <param name="take">Elements per page</param>
        /// <returns>Selected elements page</returns>
        /// <exception cref="ArgumentOutOfRangeException">Throw with invalid parameter values</exception>
        public static IEnumerable<TSource> PaginateDescending<TSource, TKey>(
            this IEnumerable<TSource> values,
            Func<TSource, TKey> orderKeySelector,
            int page,
            int take)
        {
            if (page < 0)
                throw new ArgumentOutOfRangeException(nameof(page), page, "Value can't be negative");
            if (take < 1)
                throw new ArgumentOutOfRangeException(nameof(take), take, "Value can't be less than 1");

            return values.OrderByDescending(orderKeySelector)
                         .Skip(page * take)
                         .Take(take);
        }

        /// <summary>
        /// Descending order and paginate a list of elements
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="TKey">Ordering key type</typeparam>
        /// <param name="values">Source values</param>
        /// <param name="orderKeySelector">Ordering key selector</param>
        /// <param name="page">Page to take</param>
        /// <param name="take">Elements per page</param>
        /// <returns>Selected elements page</returns>
        /// <exception cref="ArgumentOutOfRangeException">Throw with invalid parameter values</exception>
        public static IMongoQueryable<TSource> PaginateDescending<TSource, TKey>(
            this IMongoQueryable<TSource> values,
            Expression<Func<TSource, TKey>> orderKeySelector,
            int page,
            int take)
        {
            if (page < 0)
                throw new ArgumentOutOfRangeException(nameof(page), page, "Value can't be negative");
            if (take < 1)
                throw new ArgumentOutOfRangeException(nameof(take), take, "Value can't be less than 1");

            return values.OrderByDescending(orderKeySelector)
                         .Skip(page * take)
                         .Take(take);
        }
    }
}