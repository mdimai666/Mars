namespace Test.Mars.Host.TemplateEngines;

internal interface ITemplateEngineSyntaxTests
{
    void Render_VariableSubstitution_ReplacesPlaceholdersWithValues();
    void Render_IfConditionTrue_RendersTrueBlock();
    void Render_IfConditionFalse_RendersElseBlock();
    void Render_EachLoop_RendersAllItemsInCollection();
    void Render_MissingPropertyInContext_OutputsEmptyStringWithoutCrashing();

}
