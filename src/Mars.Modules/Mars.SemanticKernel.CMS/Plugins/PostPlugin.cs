using System.ComponentModel;
using Mars.Core.Extensions;
using Mars.Host.Shared.Mappings.Posts;
using Mars.Host.Shared.Mappings.PostTypes;
using Mars.Host.Shared.Services;
using Mars.SemanticKernel.CMS.Posts;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Posts;
using Mars.Shared.Contracts.PostTypes;
using Microsoft.SemanticKernel;

namespace Mars.SemanticKernel.CMS.Plugins;

// Не до конца протестировал задумку. Просто сделал чтобы работало.
internal class PostPlugin
{
    private readonly IPostService _postService;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;

    public PostPlugin(IPostService postService, IMetaModelTypesLocator metaModelTypesLocator)
    {
        _postService = postService;
        _metaModelTypesLocator = metaModelTypesLocator;
    }

    [KernelFunction]
    [Description("Retrieves detailed information about a post by its unique identifier")]
    [return: Description("Post detail object or null if not found")]
    public async Task<PostDetailResponse?> GetDetail(
        [Description("Unique post identifier (GUID)")] Guid id,
        [Description("If true — returns rendered HTML content, if false — returns raw content")] bool renderContent = true)
    {
        return (await _postService.GetDetail(id, renderContent, default))?.ToResponse();
    }

    [KernelFunction]
    [Description("Retrieves detailed information about a post by its URL-friendly slug and type")]
    [return: Description("Post detail object or null if not found")]
    public async Task<PostDetailResponse?> GetDetailBySlug(
        [Description("URL-friendly identifier (e.g., 'my-article-title')")] string slug,
        [Description("Post type (default is 'post'). Can be 'page', 'news', etc.")] string postType = "post",
        [Description("If true — returns rendered HTML content, if false — returns raw content")] bool renderContent = true)
    {
        return (await _postService.GetDetailBySlug(slug, postType, renderContent, default))?.ToResponse();
    }

    [KernelFunction]
    [Description("Searches posts by keywords with pagination support and type filtering")]
    [return: Description("Search result object containing posts array and total count")]
    public async Task<ListDataResult<PostListItemResponse>> SearchPostsAsync(
        [Description("Number of records to skip (pagination offset)")] int skip = 0,
        [Description("Maximum number of records to return (page size)")] int take = 100,
        [Description("Keyword or phrase to search in title and content (optional)")] string? search = null,
        [Description("Post type for filtering. Default is 'post'")] string postType = "post")
    {
        var posts = await _postService.List(new()
        {
            Skip = skip,
            Take = take,
            Search = search,
            Type = postType.IsNotNullOrEmpty() ? "post" : postType
        }, default);

        return posts.ToResponse();
    }

    [KernelFunction]
    [Description("Retrieves a list of all available post types supported by the system")]
    [return: Description("List of available post types with their metadata")]
    public async Task<List<PostTypeSummaryResponse>> GetAvailablePostTypes()
    {
        var types = _metaModelTypesLocator.PostTypesDict();
        return types.Values.Select(t => t.ToSummaryResponse()).ToList();
    }

    [KernelFunction]
    [Description("Creates a new blog post with the specified title, content, and post type")]
    public async Task<string> CreatePost(
        [Description("Title of the post")] string title,
        [Description("Content of the post in HTML or markdown format")] string content,
        [Description("Post type (post, page, news). Default is 'post'")] string postType = "post",
        [Description("Post tags. Separated by ';'")] string tags = "")
    {

        var query = new AiCreatePostQuery
        {
            Title = title,
            Content = content,
            Type = postType,
            Tags = tags.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToArray()
        };

        var handler = new AiCreatePostHandler(_postService, _metaModelTypesLocator);

        await handler.Handle(query, default);

        return $"Post '{title}' created successfully";
    }
}
