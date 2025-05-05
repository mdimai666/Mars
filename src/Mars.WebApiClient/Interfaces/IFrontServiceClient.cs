using Mars.Shared.Contracts.WebSite.Dto;

namespace Mars.WebApiClient.Interfaces;

public interface IFrontServiceClient
{
    Task<FMarsAppFrontTemplateMinimumResponse> FrontMinimal();
    Task<FMarsAppFrontTemplateSummaryResponse> FrontFiles();
    Task<FrontSummaryInfoResponse> FrontSummaryInfo();
    Task<FWebPartResponse?> GetPart(string fileRelPath);

}
