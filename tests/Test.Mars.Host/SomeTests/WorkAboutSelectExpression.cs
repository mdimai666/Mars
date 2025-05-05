using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Templators;
using Microsoft.EntityFrameworkCore;
using Test.Mars.Host.TestHostApp;

namespace Test.Mars.Host.SomeTests;

public class WorkAboutSelectExpression : UnitTestHostBaseClass
{

    [Fact]
    public void TestSomeEntities()
    {
        var pctx = _pctx();
        XInterpreter ppt = new();
        var ef = pctx.ef;

        var posts1 = ef.Posts
                        .Include(s => s.MetaValues)
                            .ThenInclude(s => s.MetaField)
                        .Select(TestPostMto.selectExpression)
                        .Where(s => s.int1 > 11).Count();

        //var posts2 = wer(ef.Posts).Where(s => s.int1 > 11).Count();


        Assert.Equal(2, posts1);
    }

    //public IQueryable<TestPostMto> wer(IQueryable<IMetaPostSelect> posts)
    //{
    //    var z = posts.Where(s => s.Type == s.GetTypeName())
    //                    .Include(s => s.MetaValues)
    //                        .ThenInclude(s => s.MetaField)
    //                    .Select(s => s.SelectExpression((s as Post)!)((s as Post)!));
    //    //.Where(s => s.int1 > 11).Count();
    //    return z;
    //}

    public interface IMetaPostSelect //: IPost, IMetaValueSupport
    {
        public string GetTypeName();
        public Func<PostEntity, TestPostMto> SelectExpression(PostEntity post);
    }

    [Display(Name = "testPost")]
    public partial class TestPostMto : PostEntity, IMetaPostSelect
    {
        public static readonly string _TypeName = "testPost";
        public readonly string _typeName = "testPost";
        [Display(Name = "int1")]
        public int int1 { get; set; }

        [Display(Name = "str1")]
        public string str1 { get; set; } = default!;

        public static readonly Expression<Func<PostEntity, TestPostMto>> selectExpression = post => new TestPostMto
        {
            Slug = post.Slug,
            Tags = post.Tags,
            //ParentId = post.ParentId,
            //CategoryId = post.CategoryId,
            Title = post.Title,
            Content = post.Content,
            Excerpt = post.Excerpt,
            //Image = post.Image,
            Status = post.Status,
            //Type = post.Type,
            //SetTags = post.SetTags,
            //FileList = post.FileList,
            PostFiles = post.PostFiles,
            PostMetaValues = post.PostMetaValues,
            MetaValues = post.MetaValues,
            //LikesCount = post.LikesCount,
            //Likes = post.Likes,
            //CommentsCount = post.CommentsCount,
            //Comments = post.Comments,
            UserId = post.UserId,
            User = post.User,
            Id = post.Id,
            CreatedAt = post.CreatedAt,
            ModifiedAt = post.ModifiedAt,

            //Extra fields
            int1 =
                post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(int1)) == null
                ? default
                : post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(int1)).Int,
            str1 =
                (post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(str1)) == null
                ? default
                : post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(str1)).StringShort)!,

            //int1 = post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(TestPostMto.int1) && post.ParentId == Guid.Empty)?.Int ?? default,
            //str1 = post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(TestPostMto.str1) && post.ParentId == Guid.Empty)?.StringShort ?? default,
        };

        public string GetTypeName() => _typeName;

        //public Func<Post, TestPostMto> SelectExpression(Post post)
        //{
        //    throw new NotImplementedException();
        //}

        public Func<PostEntity, TestPostMto> SelectExpression(PostEntity post) => selectExpression.Compile();
    }

    [Display(Name = "doctor")]
    public partial class DoctorDto : PostEntity, IDtoMarker
    {
        public static readonly string _typeName = "doctor";
        [Display(Name = "category")]
        public CategoryMto cat { get; set; } = default!;
        public Guid _cat { get; set; }

        [Display(Name = "nickname")]
        public string nickname { get; set; } = default!;

        [Display(Name = "Age")]
        public int age { get; set; }

        public class CategoryMto : PostEntity
        {

        }


        //public static string[] _RelationFields = { "cat", "nickname", "age" };

        public void Fill(Dictionary<Guid, MetaRelationObjectDict> dataDict)
        {
            if (dataDict.ContainsKey(_cat)) cat = (Convert.ChangeType(dataDict[_cat].Entity, typeof(CategoryMto)) as CategoryMto)!;

        }
    }
}
