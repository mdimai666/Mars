using Mars.Core.Models;
using Mars.Shared.Contracts.WebSite.Models;

namespace Mars.Shared.Contracts.WebSite.Dto;

public record FMarsAppFrontTemplateSummaryResponse
{
    public required FWebPartSummaryResponse RootPage { get; init; }
    public required IReadOnlyCollection<FWebPageSummaryResponse> Pages { get; init; }
    public required IReadOnlyCollection<FWebPartSummaryResponse> Parts { get; init; }
    public required IReadOnlyCollection<FWebPartSummaryResponse> Layouts { get; init; }

    public required string? Page404FileRelPath { get; init; }
    public required string? Page500FileRelPath { get; init; }
    public required string IndexPageFileRelPath { get; init; }

}

public record FMarsAppFrontTemplateMinimumResponse
{
    public required FWebPartSummaryResponse RootPage { get; init; }
    public required IReadOnlyCollection<FWebPageSummaryResponse> Pages { get; init; }
    public required string? Page404FileRelPath { get; init; }
    public required string? Page500FileRelPath { get; init; }
    public required string IndexPageFileRelPath { get; init; }

}

public record FWebPartSummaryResponse
{
    public required WebSitePartType Type { get; init; }
    public required string FileName { get; init; }
    public required string Title { get; init; }
    public required string FileRelPath { get; init; }
    public required IReadOnlyDictionary<string, string> Attributes { get; init; }

}

public record FWebPartResponse : FWebPartSummaryResponse
{
    public required string Content { get; init; }
}

public record FWebPageSummaryResponse : FWebPartSummaryResponse
{
    public required string Url { get; init; }
    public required string? Layout { get; init; }

}

public record FWebPageResponse : FWebPartResponse
{
    public required string Url { get; init; }
    public required string? Layout { get; init; }

}

public record FrontSummaryInfoResponse
{
    public required AppFrontMode Mode { get; init; }
    public required int PagesCount { get; init; }
    public required int PartsCount { get; init; }
}
