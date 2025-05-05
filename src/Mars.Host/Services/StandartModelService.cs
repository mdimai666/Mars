using AppShared.Dto;
using AppShared.Models;
using Mars.Host.Data;
using Mars.Host.Data.Contexts;
using Mars.Host.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mars.Host.Services;

public class StandartModelService<TEntity> : StandartModelService<TEntity, MarsDbContext>
    where TEntity : class, IBasicEntity, new()
{
    public StandartModelService(IConfiguration configuration, IServiceProvider serviceProvider) : base(configuration, serviceProvider)
    {

    }
}

public class StandartModelService<TEntity, TDbContext> : BasicModelService<TEntity, TDbContext>, IStandartModelService<TEntity>
    where TEntity : class, IBasicEntity, new()
    where TDbContext : MarsDbContext
{
    protected ILogger<StandartModelService<TEntity>> _logger;
    public StandartModelService(IConfiguration configuration, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<StandartModelService<TEntity>>>();
    }


    ///////////////////////////////////////
    // Comments
    ///////////////////////////////////////
    public async Task<List<CommentDto>> Comments(Guid id) //TODO: CommentDto
    {
        if (typeof(TEntity).IsAssignableTo(typeof(ICommentsSupport)) == false)
        {
            throw new NotImplementedException("Model not support Comment");
        }

        using var ef = GetEFContext();

        var item = await ef.Set<TEntity>()
            .Include(s => (s as ICommentsSupport).Comments)
            .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id);

        var comments = (item as ICommentsSupport).Comments.OrderBy(s => s.Created);

        //List<CommentDto> commentsDtoList = comments.Where(s => s.ParentCommentId is null).Select(s => new CommentDto(s)).ToList();

        Dictionary<Guid, CommentDto> commentsDict = comments.ToDictionary(s => s.Id, s => new CommentDto(s));

        int MAX_TRY = 3000;

        foreach (var comment in comments.Where(s => s.ParentCommentId is not null))
        {
            Guid rootParent = comment.ParentCommentId.Value;

            if (!commentsDict.ContainsKey(rootParent)) continue;

            while (commentsDict[rootParent].ParentCommentId is not null)
            {
                MAX_TRY--;
                if (MAX_TRY <= 0) throw new ArgumentOutOfRangeException(nameof(comment.ParentCommentId));
                rootParent = commentsDict[rootParent].ParentCommentId.Value;
            }

            commentsDict[rootParent].ChildComments ??= new();
            commentsDict[rootParent].ChildComments.Add(commentsDict[comment.Id]);
        }

        foreach (var comment in commentsDict.Values)
        {
            if (comment.ChildComments is not null)
            {
                comment.ChildComments = comment.ChildComments.OrderBy(s => s.Created).ToList();
            }
        }

        return commentsDict.Values.Where(s => s.ParentCommentId is null).ToList();
    }

    public async Task<UserActionResult<Comment>> AddComment(CommentAddDto dto)
    {
        throw new NotImplementedException();

        //if (typeof(TEntity).IsAssignableTo(typeof(ICommentsSupport)) == false)
        //{
        //    throw new NotImplementedException("Model not support Comment");
        //}

        //try
        //{
        //    using var ef = GetEFContext();

        //    var user = await GetCurrentUser();

        //    if (user is null)
        //    {
        //        return new UserActionResult<Comment>
        //        {
        //            Message = "Вы не авторизованы"
        //        };
        //    }

        //    var item = await ef.Set<TEntity>().Include(s => (s as ICommentsSupport).Comments).FirstOrDefaultAsync(s => s.Id == dto.PostId);

        //    Comment comment = new Comment
        //    {
        //        Id = Guid.NewGuid(),
        //        MessageHtml = dto.Message,
        //        UserId = user.Id,
        //        ParentCommentId = dto.ParentCommentId
        //    };

        //    if (item is Post)
        //    {
        //        comment.PostId = dto.PostId;
        //    }
        //    //else if (item is Zayavka)
        //    //{
        //    //    comment.ZayavkaId = dto.PostId;
        //    //}
        //    //else if (item is Meeting)
        //    //{
        //    //    comment.MeetingId = dto.PostId;
        //    //}
        //    else
        //    {
        //        throw new NotImplementedException();
        //    }

        //    if (item is ICommentsSupport commentable)
        //    {
        //        //commentable.Comments.Add(comment);
        //        ef.Comments.Add(comment);

        //        commentable.CommentsCount = commentable.Comments.Count;
        //    }

        //    ef.SaveChanges();

        //    comment.UserId = user.Id;

        //    return new UserActionResult<Comment>
        //    {
        //        Ok = true,
        //        Message = "Комментарий добавлен",
        //        Data = comment

        //    };
        //}
        //catch (Exception ex)
        //{
        //    return new UserActionResult<Comment>
        //    {
        //        Message = ex.Message
        //    };
        //}
    }

    public async Task<UserActionResult> RemoveComment(Guid postId, Guid commentId)
    {
        throw new NotImplementedException();

        //if (typeof(TEntity).IsAssignableTo(typeof(ICommentsSupport)) == false)
        //{
        //    throw new NotImplementedException("Model not support Comment");
        //}

        //try
        //{
        //    using var ef = GetEFContext();

        //    var user = await GetCurrentUser();
        //    var roles = await _userManager.GetRolesAsync(user);

        //    bool isAdmin = roles.Contains("Admin");

        //    var item = await ef.Set<TEntity>().Include(s => (s as ICommentsSupport).Comments).FirstOrDefaultAsync(s => s.Id == postId);

        //    if (!isAdmin && user.Id != (item as ICommentsSupport).UserId)
        //    {
        //        return new UserActionResult
        //        {
        //            Ok = false,
        //            Message = "У вас нет прав"
        //        };
        //    }

        //    if (item is ICommentsSupport commentable)
        //    {
        //        var toRemove = commentable.Comments.First(s => s.Id == commentId);

        //        var collectedToremove = CollectCommentChilds(commentable.Comments, commentId).ToList();

        //        commentable.Comments.Remove(toRemove);
        //        ef.Comments.Remove(toRemove);

        //        foreach (var comment in collectedToremove)
        //        {
        //            commentable.Comments.Remove(comment);
        //        }
        //        ef.Comments.RemoveRange(collectedToremove);

        //        commentable.CommentsCount = commentable.Comments.Count;
        //    }
        //    ef.SaveChanges();

        //    return new UserActionResult
        //    {
        //        Ok = true,
        //        Message = "Комментарий удален"
        //    };
        //}
        //catch (Exception ex)
        //{
        //    return new UserActionResult
        //    {
        //        Message = ex.Message
        //    };
        //}
    }

    IEnumerable<Comment> CollectCommentChilds(ICollection<Comment> comments, Guid parentId)
    {
        var list = comments.Where(s => s.ParentCommentId == parentId);

        if (list.Count() == 0) yield break;

        foreach (var a in list)
        {
            yield return a;

            foreach (var b in CollectCommentChilds(comments, a.Id))
            {
                yield return b;
            }
        }
    }

    ///////////////////////////////////////
    // Likes
    ///////////////////////////////////////
    public async Task<IEnumerable<UserDto>> LikedUsers(Guid id)
    {
        if (typeof(TEntity).IsAssignableTo(typeof(ILikesSupport)) == false)
        {
            throw new NotImplementedException("Model not support Likes");
        }

        using var ef = GetEFContext();

        var item = await ef.Set<TEntity>()
            .Include(s => (s as ILikesSupport).Likes)
            .Include(s => (s as ILikesSupport).User)
            .FirstOrDefaultAsync(s => s.Id == id);

        return (item as ILikesSupport).Likes.Select(s => new UserDto(s.User));
    }

    public async Task<UserLikeResult> LikePost(Guid id, bool addLike = true)
    {
        if (typeof(TEntity).IsAssignableTo(typeof(ILikesSupport)) == false)
        {
            throw new NotImplementedException("Model not support Likes");
        }

        try
        {
            using var ef = GetEFContext();

            var user = await GetCurrentUser();

            if (user is null)
            {
                return new UserLikeResult
                {
                    Message = "Вы не авторизованы"
                };
            }

            var item = await ef.Set<TEntity>().Include(s => (s as ILikesSupport).Likes).FirstOrDefaultAsync(s => s.Id == id);

            bool hasLike = (item as ILikesSupport).Likes.Count(s => s.UserId == user.Id) > 0;

            if (hasLike && addLike)
            {
                return new UserLikeResult
                {
                    Ok = true,
                    Message = "Уже помечено",
                    LikedState = hasLike,
                    TotalLikes = (item as ILikesSupport).Likes.Count()
                };
            }


            if (hasLike == false && addLike == false)
            {
                return new UserLikeResult
                {
                    Ok = true,
                    Message = "Итак не помечено",
                    LikedState = hasLike,
                    TotalLikes = (item as ILikesSupport).Likes.Count()
                };
            }

            if (addLike)
            {
                PostLike like = new()
                {
                    UserId = user.Id
                };

                if (item is Post)
                {
                    like.PostId = id;
                }
                //else if (item is Zayavka)
                //{
                //    like.ZayavkaId = id;
                //}
                //else if (item is Meeting)
                //{
                //    like.MeetingId = id;
                //}
                else
                {
                    throw new NotImplementedException();
                }

                if (item is ILikesSupport commentable)
                {
                    commentable.Likes.Add(like);

                    commentable.LikesCount = commentable.Likes.Count;
                }
            }
            else
            {
                if (item is ILikesSupport commentable)
                {
                    var toRemove = commentable.Likes.First(s => s.UserId == user.Id);
                    commentable.Likes.Remove(toRemove);
                    commentable.LikesCount = commentable.Likes.Count;
                    //TODO: тут лайк из базы не удаляется
                }
            }

            ef.SaveChanges();

            //var newReq = ef.Set<TEntity>().Include(s => (s as ILikesSupport).Likes).First(s => s.Id == id);
            //int totalLikes = (newReq as ILikesSupport).l

            return new UserLikeResult
            {
                Ok = true,
                Message = "Помечено",
                LikedState = addLike,
                TotalLikes = (item as ILikesSupport).LikesCount
            };
        }
        catch (Exception ex)
        {
            return new UserLikeResult
            {
                Message = ex.Message
            };
        }
    }

    public async Task<UserLikeResult> UnlikePost(Guid id)
    {
        return await LikePost(id, false);
    }
}

