﻿using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Digicando.MongODM.Extensions
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