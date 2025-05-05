using System.Linq.Expressions;
using Mars.Host.Extensions;
using Mars.Host.Shared.Dto.Galleries;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Mars.Host.Services.GallerySpace;

internal class GalleryService : IGalleryService
{
    private readonly IPostService _postService;
    private readonly IPostRepository _postRepository;
    private readonly IPostTypeService _postTypeService;
    private readonly IMediaService _mediaService;

    public GalleryService(IPostService postService,
                        IPostRepository postRepository,
                        IPostTypeService postTypeService,
                        IMediaService mediaService)
    {
        _postService = postService;
        _postRepository = postRepository;
        _postTypeService = postTypeService;
        _mediaService = mediaService;
    }

    public Task<GalleryDetail> Create(CreateGalleryQuery query, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<UserActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<GallerySummary?> Get(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<GalleryDetail?> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<GalleryDetail?> GetDetailBySlug(string slug, string type, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<ListDataResult<GallerySummary>> List(ListGalleryQuery query, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<PagingResult<GallerySummary>> ListTable(ListGalleryQuery query, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<GalleryDetail> Update(UpdateGalleryQuery query, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
    /*
public Task<GallerySummary?> Get(Guid id, CancellationToken cancellationToken)
   => _postRepository.Get(id, cancellationToken).ToSummary();

public Task<PostDetail?> GetDetail(Guid id, CancellationToken cancellationToken)
   => _postRepository.GetDetail(id, cancellationToken);

public Task<PostDetail?> GetDetailBySlug(string slug, string type, CancellationToken cancellationToken)
   => _postRepository.GetDetailBySlug(slug, type, cancellationToken);

public Task<ListDataResult<PostSummary>> List(ListPostQuery query, CancellationToken cancellationToken)
   => _postRepository.List(query, cancellationToken);

public Task<PagingResult<PostSummary>> ListTable(ListPostQuery query, CancellationToken cancellationToken)
   => _postRepository.ListTable(query, cancellationToken);

public async Task<PostDetail> Create(CreatePostQuery query, CancellationToken cancellationToken)
{

}

public async Task<PostDetail> Update(UpdatePostQuery query, CancellationToken cancellationToken)
{
}
public async Task<UserActionResult> Delete(Guid id, CancellationToken cancellationToken)
{

}

//OLD

public async Task<GalleryDetail> Create(Gallery entity)
{
   var added = await _postService.Add(entity);

   return new Gallery(added);
}

public async Task<UserActionResult> Delete(Guid id)
{
   var galleryId = id;

   var galleryPhotos = await GalleryPhotos(id, new QueryFilter(1, 10000));

   var galleryPhotosIds = galleryPhotos.Records.Select(s => s.Id).ToArray();

   var res = await _postService.Delete(id);

   if (res.Ok)
   {
       var photosDeleteResult = await GalleryDeletePhotos(galleryPhotosIds);

       return photosDeleteResult;
   }

   return res;
}

async Task<FileEntity?> GalleryImage(Guid postId)
{
   var post = await _postService.Get(postId);
   return await GalleryImage(post);
}

async Task<FileEntity?> GalleryImage(Post postWithMetaFields)
{
   if (postWithMetaFields.MetaValues is null) throw new ArgumentException("require .MetaValues.MetaField");
   var imageMetaField = ImageMetaField(postWithMetaFields);
   if (imageMetaField is null) return null;
   var fileModelId = imageMetaField.ModelId;
   return await _mediaService.Get(fileModelId);
}

async Task<MetaField?> FindPhotoGalleyField()
{
   PostType photoType = await _postTypeService.Get(s => s.TypeName == "photo");
   var galleryMetaField = photoType.MetaFields.FirstOrDefault(s => s.Key == "gallery");
   return galleryMetaField;
}

async Task<int> GalleryPhotosCount(Guid postId)
{
   //XInterpreter ppt = new(null, contextForIterator: new Dictionary<string, object>
   //{
   //    ["galleryId"] = postId
   //});
   //string query = "photo.Count(post.gallery == galleryId)";

   //var qef = new EntityQuery(_serviceProvider, ppt, null);
   //var result = qef.Query(query);

   //return (int)result;

   var ef = GetEFContext();
   var photosCount = await ef.Posts
                       .Include(s => s.MetaValues)
                       .ThenInclude(s => s.MetaField)
                       .AsNoTracking()
                       .Where(s => s.Type == "photo")
                       .CountAsync(s => s.MetaValues.First(x => x.MetaField.Key == "gallery").ModelId == postId);

   return photosCount;
}

public async Task<Gallery> Get(Guid id)
{
   var post = await _postService.Get(id);
   return new Gallery(post)
   {
       GalleryImage = await GalleryImage(post),
       GalleryPhotosCount = await GalleryPhotosCount(id),
   };
}

public async Task<List<Gallery>> List(Expression<Func<Gallery, bool>> predicate = null, int offset = 0, int limit = 10)
{
   throw new NotImplementedException();
}

public async Task<TotalResponse<Gallery>> ListTable(QueryFilter filter, Expression<Func<Gallery, bool>> predicate = null, Expression<Func<Gallery, object>>[] include = null)
{
   var ef = GetEFContext();

   if (predicate is not null || include is not null)
   {
       throw new NotImplementedException("predicate and include not implemented");
   }

   var res = await ef.Posts.Include(s => s.MetaValues)
                       .ThenInclude(s => s.MetaField)
                       .Include(s => s.User)
                       .AsNoTracking()
                       .Where(s => s.Type == "gallery")
                       .QueryTable(filter);

   var ids = res.Records.Select(x => x.Id);

   var photosCount = ef.Posts
                       .Include(s => s.MetaValues)
                       .ThenInclude(s => s.MetaField)
                       .AsNoTracking()
                       .Where(s => s.Type == "photo")
                       .Where(s => s.MetaValues.FirstOrDefault(x => x.MetaField.Key == "gallery") != null)
                       .GroupBy(s => s.MetaValues.First(x => x.MetaField.Key == "gallery").ModelId)
                       .Where(s => ids.Contains(s.Key))
                       .Select(s => new { PostId = s.Key, Count = s.Count() })
                       .ToList()
                       .ToDictionary(s => s.PostId, s => s.Count);

   var galleries = new List<Gallery>(res.Records.Count());

   foreach (var post in res.Records)
   {
       var a = new Gallery(post)
       {
           GalleryImage = await GalleryImage(post),
           GalleryPhotosCount = photosCount.TryGetValue(post.Id, out var _count) ? _count : 0,
           MetaValues = null,
       };
       a.User.MetaValues = null;
       a.User.GeoRegion = null;
       a.User.GeoLocation = null;
       a.User.GeoMunicipality = null;

       galleries.Add(a);
   }

   return new TotalResponse<Gallery>
   {
       Message = res.Message,
       Result = res.Result,
       TotalCount = res.TotalCount,
       Records = galleries
   };
}

public async Task<Gallery> Update(Guid id, Gallery entity, Expression<Func<Gallery, object>>[]? include = null)
{
   await _postService.Update(id, entity);
   return await Get(id);
}

public static MetaValue? ImageMetaField(Post post)
{
   if (post.MetaValues is null) return null;

   var imageMeta = post.MetaValues.FirstOrDefault(s => s.MetaField.ParentId == Guid.Empty && s.MetaField.Key == "image" && s.MetaField.Type == EMetaFieldType.Image);

   return imageMeta;
}

public async Task<TotalResponse<GalleryPhoto>> GalleryPhotos(Guid id, QueryFilter queryFilter)
{
   var ef = GetEFContext();

   var photosPostsQuery = ef.Posts
                       .Include(s => s.MetaValues)
                       .ThenInclude(s => s.MetaField)
                       .OrderByDescending(s => s.Created)
                       .Where(s => s.Type == "photo")
                       .Where(s => s.MetaValues.FirstOrDefault(x => x.MetaField.Key == "gallery") != null
                                 && s.MetaValues.FirstOrDefault(x => x.MetaField.Key == "image") != null)
                       .Where(s => s.MetaValues.First(x => x.MetaField.Key == "gallery").ModelId == id)
                       //Если сразу не делать выборку то из-за разбиения SplitQuery слетают мета поля
                       .Select(s => new
                       {
                           Post = s,
                           PhotoFileId = s.MetaValues.FirstOrDefault(x => x.MetaField.Key == "image").ModelId
                       });

   var photosPostsTotal = photosPostsQuery.Count();

   var photos0 = await photosPostsQuery.Skip(queryFilter.Skip).Take(queryFilter.Take).ToListAsync();

   var photos = photos0.Select(s =>
   {
       //var imageId = s.MetaValues.FirstOrDefault(x => x.MetaField.Key == "image")?.ModelId ?? Guid.Empty;
       return new GalleryPhoto(s.Post, id, s.PhotoFileId);
   }).ToList();

   var photosFilesIds = photos.Where(s => s.PhotoFileId is not null)
                       .Select(s => s.PhotoFileId)
                       .ToList();

   var files = ef.Files.Where(s => photosFilesIds.Contains(s.Id)).ToList().ToDictionary(s => s.Id);

   foreach (var photo in photos)
   {
       if (photo.PhotoFileId is not null && files.TryGetValue(photo.PhotoFileId.Value, out var file))
       {
           photo.Image = file;
       }
   }

   return new TotalResponse<GalleryPhoto>
   {
       Message = "",
       Result = ETotalResponeResult.OK,
       TotalCount = photosPostsTotal,
       Records = photos
   };
}

public async Task<UserActionResult<List<FileEntity>>> GalleryAddPhotos(Guid galleryId, IFormFileCollection files)
{
   List<FileEntity> addedFiles = new(files.Count);

   var file_group = "photo";

   foreach (var _file in files)
   {
       var file = _file;
       FileEntity f = _mediaService.WriteUpload(file, EFileType.Media, file_group);
       addedFiles.Add(f);
   }

   PostType photoPostType = await _postTypeService.Get(s => s.TypeName == "photo");

   var imageMetaField = photoPostType.MetaFields.First(s => s.Key == "image");
   var galleryMetaField = photoPostType.MetaFields.First(s => s.Key == "gallery");

   Guid userId = _userId.Value;

   var ef = GetEFContext();

   List<Post> posts = new List<Post>();

   foreach (var file in addedFiles)
   {
       var id = Guid.NewGuid();
       var galleryMetaValueId = Guid.NewGuid();
       var imageMetaValueId = Guid.NewGuid();

       Post post = new()
       {
           Id = id,
           Title = file.FileName,
           UserId = userId,
           Slug = "photo-" + id.ToString(),
           Type = photoPostType.TypeName,
           Status = photoPostType.PostStatusList.FirstOrDefault()?.Slug ?? "",
           PostMetaValues = new List<PostMetaValue>
           {
               new PostMetaValue
               {
                   Id = Guid.NewGuid(),
                   PostId = id,
                   MetaValueId = galleryMetaValueId,
                   MetaValue = new MetaValue
                   {
                       ModelId = galleryId,
                       NULL = false,
                       MetaFieldId = galleryMetaField.Id,
                       Type = EMetaFieldType.Relation,
                   }
               }
               ,new PostMetaValue
               {
                   Id = Guid.NewGuid(),
                   PostId = id,
                   MetaValueId = imageMetaValueId,
                   MetaValue = new MetaValue
                   {
                       ModelId = file.Id,
                       NULL = false,
                       MetaFieldId = imageMetaField.Id,
                       Type = EMetaFieldType.Image,
                   }
               }
           }
       };
       posts.Add(post);
   }

   ef.Posts.AddRange(posts);
   await ef.SaveChangesAsync();

   return new UserActionResult<List<FileEntity>>
   {
       Ok = true,
       Message = "файлы успешно добавлены",
       Data = addedFiles,
   };
}

public async Task<UserActionResult> GalleryDeletePhotos(Guid[] photosIds)
{
   var ef = GetEFContext();

   var photoPostsFilesIds = ef.PostMetaValues
       .AsNoTracking()
       .Include(s => s.MetaValue)
       .ThenInclude(s => s.MetaField)
       .Where(s => photosIds.Contains(s.PostId))
       .Where(s => (s.MetaValue.MetaField.Type == EMetaFieldType.File || s.MetaValue.MetaField.Type == EMetaFieldType.Image))
       .Select(s => s.MetaValue.ModelId)
       .ToList()
       .Distinct()
       .Except([Guid.Empty])
       .ToList();

   var filesUsage = ef.MetaValues
       .AsNoTracking()
       .Include(s => s.MetaField)
       .Where(s => (s.MetaField.Type == EMetaFieldType.File || s.MetaField.Type == EMetaFieldType.Image) && photoPostsFilesIds.Contains(s.ModelId))
       .GroupBy(s => s.ModelId)
       .Select(s => new { FileId = s.Key, Count = s.Count() })
       .ToList();

   var forDeleteFilesIds = filesUsage.Where(s => s.Count == 1).Select(s => s.FileId).ToList();

   var file_group = "photo";

   var forDeleteFiles = ef.Files.Where(s => s.FileGroup == file_group && forDeleteFilesIds.Contains(s.Id)).ToList();

   foreach (var postId in photosIds)
   {
       await _postService.Delete(postId);
   }

   ef.Files.RemoveRange(forDeleteFiles);

   await ef.SaveChangesAsync();

   return UserActionResult.SuccessDeleted();
}
*/
}

