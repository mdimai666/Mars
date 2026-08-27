namespace Test.Mars.Host.TemplateEngines;

internal interface ITemplateEngineInterfaceTests
{
    void Render_WithValidTemplateAndContext_ReturnsExpectedString();
    void RenderCached_CalledMultipleTimes_ReturnsCorrectResult();
    void RemoveFromCache_ExistingId_RemovesTemplateAndReturnsTrue();
    void ClearCache_WhenCacheHasItems_ClearsAllTemplates();
    void RenderCached_EmptyTemplateId_ThrowsArgumentException();
    void RenderCached_TemplateChangedForSameId_InvalidatesCacheAndReturnsNewResult();
    void Render_RawHtmlInTemplateString_ReturnsHtmlAsIsWithoutEscaping();

}
