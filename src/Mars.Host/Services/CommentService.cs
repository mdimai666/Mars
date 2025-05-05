using System;
using AppShared.Models;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;

namespace Mars.Host.Services;

public class CommentService : StandartModelService<Comment>
{
    public CommentService(IConfiguration configuration, IServiceProvider serviceProvider) : base(configuration, serviceProvider)
    {

    }

    public override Task<TotalResponse<Comment>> ListTable(QueryFilter filter, Expression<Func<Comment, bool>> predicate, [NotNull] params Expression<Func<Comment, object>>[] include)
    {
        if (include is null)
        {
            return base.ListTable(filter, predicate, s => s.User);
        }
        else
        {
            return base.ListTable(filter, predicate, include);
        }
    }

    public async override Task<Comment> Add(Comment entity)
    {
        var added = await base.Add(entity);

        return await Get(added.Id, s => s.User);
    }

    //public async Task<Comment> GetZayavkaComments(Guid id)
    //{
    //    using()
    //}
}

