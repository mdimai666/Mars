using Mars.Contracts.Common;
using Mars.Contracts.WebSite.Dto;

namespace Mars.WebApiClient.Interfaces;

public interface IFrontServiceClient
{
    Task<FMarsAppFrontTemplateMinimumResponse> FrontMinimal();
    Task<FMarsAppFrontTemplateSummaryResponse> FrontFiles();
    Task<FrontSummaryInfoResponse> FrontSummaryInfo();
    Task<FWebPartResponse?> GetPart(string fileRelPath);

    Task<IReadOnlyCollection<FFrontEngineResponse>> Engines();

    /// <summary>
    /// Стартовые шаблоны для новых фронтов (папки Res/front_templates, без специальных).
    /// </summary>
    Task<IReadOnlyCollection<string>> FrontTemplates();
    Task<FFrontTreeNodeResponse> FrontTree(string slug);
    Task<IReadOnlyCollection<FFrontPageResponse>> FrontPages(string slug);
    Task<FFrontFileContentResponse> ReadFrontFile(string slug, string relPath);

    Task<UserActionResult> CreateFront(FCreateFrontRequest request);
    Task<UserActionResult> DeleteFront(string slug, bool deleteFolder);
    Task<UserActionResult> SaveFrontFile(string slug, string relPath, string content);
    Task<UserActionResult> CreateFrontFile(string slug, string relPath, bool isFolder);
    Task<UserActionResult> RenameFrontFile(string slug, string relPath, string newRelPath);
    Task<UserActionResult> DeleteFrontFile(string slug, string relPath);
}
