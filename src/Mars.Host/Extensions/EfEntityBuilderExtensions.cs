using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Mars.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Query;
//using Remote.Linq;

namespace Mars.Host.Extensions
{
    public static class EfEntityBuilderExtensions
    {
        //public static PropertyBuilder<TProperty> HasColumnName<TProperty>([NotNullAttribute] this PropertyBuilder<TProperty> propertyBuilder, [CanBeNullAttribute] string name)
        //{

        //}

        public static PropertyBuilder<TProperty> IgnorePropertyFromUpdate<TProperty>([NotNull] this PropertyBuilder<TProperty> propertyBuilder)
        {

            var meta = propertyBuilder.Metadata;

            meta.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            meta.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

            return propertyBuilder;
        }

        //public static IIncludableQueryable<TEntity, TProperty> Query<TEntity, TProperty>(
        //    this IQueryable<TEntity> source,
        //    [NotNullAttribute] Expression<Func<TEntity, TProperty>> include
        //    ) where TEntity : class
        //{
        //    return source.Include<TEntity, TProperty>(include);
        //    //throw new NotImplementedException();
        //}

        //public static IQueryable<TEntity> Query<TEntity>(
        //    this IQueryable<TEntity> source, 
        //    QueryFilter filter)
        //    where TEntity : class, IBasicEntity
        //{
        //    source = string.IsNullOrWhiteSpace(filter.OrderByStringParam)
        //                ? source.OrderByDescending(s => s.Created)
        //                : source.OrderBySortStringParam(filter.OrderByStringParam);

        //    //items = await query.Skip(filter.Skip).Take(filter.Take).ToListAsync();

        //    //totalCount = await query.CountAsync();

        //    return source;

        //}
        /*
        public static async Task<TotalResponse<TEntity>> QueryTable<TEntity>(
            this IQueryable<TEntity> source,
            QueryFilter filter,
            Expression<Func<TEntity, bool>> predicate = null,
            bool throwException = true
            ) where TEntity : class, IBasicEntity, new()
        {
            try
            {
                if (predicate is not null)
                {
                    source = source.Where(predicate);
                }

                //if (string.IsNullOrEmpty(filter.JsonExpression) == false)
                //{
                //    string vvv = Uri.UnescapeDataString(filter.JsonExpression);

                //    //var expression = RemoteLinq.DeserialiseRemoteExpression<Func<TEntity,bool>>(filter.JsonExpression);
                //    Remote.Linq.Expressions.Expression q = RemoteLinq.DeserialiseRemoteExpression<Remote.Linq.Expressions.Expression>(filter.JsonExpression);
                //    Expression sysExpr = q.ToLinqExpression();
                //    var exp = (q as Remote.Linq.Expressions.LambdaExpression).ToLinqExpression<TEntity, bool>();

                //    source = source.Where(exp);
                //}

                source = string.IsNullOrWhiteSpace(filter.OrderByStringParam)
                            ? source.OrderByDescending(s => s.Created)
                            : source.OrderBySortStringParam(filter.OrderByStringParam);

                var ne = new TEntity();

                //if (typeof(TEntity).IsAssignableFrom(typeof(IDeleteIsChangeStatus<TEntity>)))
                //if (ne is IDeleteIsChangeStatus<TEntity> dq)
                //{
                //    //source = source.Where((dq as IDeleteIsChangeStatus<TEntity>).IsNonDeletedFilter());
                //    source = source.Where(dq.IsNonDeletedFilter());
                //    //source = dq.example(source);

                //    //source = source.Where(dq.IsNonDeletedFilter());
                //}

                var items = await source.Skip(filter.Skip).Take(filter.Take).ToListAsync();

                int totalCount = await source.CountAsync();

                return new TotalResponse<TEntity>
                {
                    Result = ETotalResponeResult.OK,
                    Records = items,
                    TotalCount = totalCount
                };

            }
            catch (Exception ex)
            {
                if (throwException) throw;

                return new TotalResponse<TEntity>
                {
                    Result = ETotalResponeResult.ERROR,
                    Message = ex.Message
                };
            }
        }

        public static IIncludableQueryable<TEntity, TProperty> IncludeMany<TEntity, TProperty>(
            [NotNull] this IQueryable<TEntity> source,
            [NotNull] params Expression<Func<TEntity, TProperty>>[] include) where TEntity : class
        {
            IIncludableQueryable<TEntity, TProperty> query = null;

            foreach (var inc in include)
            {
                if (query == null)
                {
                    query = source.Include(inc);
                }
                else
                {
                    query = query.Include(inc);
                }
            }

            return query;
        }
        */

    }
}
