using ConduitNet.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ConduitNet.Server
{
    public static class ConduitQueryExtensions
    {
        public static IQueryable<T> ApplyConduitQuery<T>(this IQueryable<T> query, ConduitQuery conduitQuery)
        {
            if (conduitQuery == null) return query;

            // 1. Apply Filters
            foreach (var filter in conduitQuery.Filters)
            {
                query = query.ApplyFilter(filter);
            }

            // 2. Apply Sorts
            if (conduitQuery.Sorts.Any())
            {
                query = query.ApplySorts(conduitQuery.Sorts);
            }

            // 3. Apply Paging
            if (conduitQuery.Skip.HasValue)
            {
                query = query.Skip(conduitQuery.Skip.Value);
            }

            if (conduitQuery.Take.HasValue)
            {
                query = query.Take(conduitQuery.Take.Value);
            }

            return query;
        }

        private static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, QueryFilter filter)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, filter.FieldName);
            var constant = Expression.Constant(Convert.ChangeType(filter.Value, property.Type));

            Expression comparison;

            switch (filter.Operator)
            {
                case FilterOperator.Eq:
                    comparison = Expression.Equal(property, constant);
                    break;
                case FilterOperator.Neq:
                    comparison = Expression.NotEqual(property, constant);
                    break;
                case FilterOperator.Gt:
                    comparison = Expression.GreaterThan(property, constant);
                    break;
                case FilterOperator.Lt:
                    comparison = Expression.LessThan(property, constant);
                    break;
                case FilterOperator.Gte:
                    comparison = Expression.GreaterThanOrEqual(property, constant);
                    break;
                case FilterOperator.Lte:
                    comparison = Expression.LessThanOrEqual(property, constant);
                    break;
                case FilterOperator.Contains:
                    var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                    if (containsMethod == null) throw new InvalidOperationException("String.Contains method not found.");
                    comparison = Expression.Call(property, containsMethod, constant);
                    break;
                // TODO: Implement other operators
                default:
                    throw new NotSupportedException($"Operator {filter.Operator} is not supported.");
            }

            var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
            return query.Where(lambda);
        }

        private static IQueryable<T> ApplySorts<T>(this IQueryable<T> query, List<QuerySort> sorts)
        {
            IOrderedQueryable<T>? orderedQuery = null;

            for (int i = 0; i < sorts.Count; i++)
            {
                var sort = sorts[i];
                var parameter = Expression.Parameter(typeof(T), "x");
                var property = Expression.Property(parameter, sort.FieldName);
                var lambda = Expression.Lambda(property, parameter);

                var methodName = "";
                if (i == 0)
                {
                    methodName = sort.IsDescending ? "OrderByDescending" : "OrderBy";
                }
                else
                {
                    methodName = sort.IsDescending ? "ThenByDescending" : "ThenBy";
                }

                var resultExpression = Expression.Call(
                    typeof(Queryable),
                    methodName,
                    new Type[] { typeof(T), property.Type },
                    (orderedQuery ?? query).Expression,
                    Expression.Quote(lambda)
                );

                orderedQuery = (IOrderedQueryable<T>)query.Provider.CreateQuery<T>(resultExpression);
            }

            return orderedQuery ?? query;
        }
    }
}
