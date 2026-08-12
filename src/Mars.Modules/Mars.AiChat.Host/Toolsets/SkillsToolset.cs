using Mars.AiChat.Host.Tools;
using Microsoft.Extensions.AI;

namespace Mars.AiChat.Host.Toolsets;

/// <summary>
/// Каталог скиллов: SearchSkills (поиск по имени/описанию) + LoadSkill (полные инструкции).
/// </summary>
public class SkillsToolset : IAiToolset
{
    private readonly MarsSkillsTools _skillsTools;

    public SkillsToolset(MarsSkillsTools skillsTools) => _skillsTools = skillsTools;

    public string Name => "skills";

    public bool IsEnabled(AiToolsetContext ctx) => ctx.SkillsEnabled;

    public IReadOnlyList<AIFunction> Build(AiToolsetContext ctx) =>
    [
        AIFunctionFactory.Create(_skillsTools.SearchSkills),
        AIFunctionFactory.Create(_skillsTools.LoadSkill),
    ];
}
