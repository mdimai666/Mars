using System.Text;
using Mars.AiChat.Contracts.Options;

namespace Mars.AiChat.Host;

/// <summary>
/// Системный промпт агента: только ядро (стиль, базовые правила, память, работа со скиллами).
/// Доменные инструкции живут в скиллах (ai-skills/**/SKILL.md + <data>/ai/skills): в контекст
/// попадает компактный каталог, полные тела модель берёт через LoadSkill, а скиллы открытой
/// страницы preload-ит PageSkillRouter — прогрессивное раскрытие, как в Qwen Code CLI.
/// </summary>
internal static class AiChatPrompts
{
    public static string BuildInstructions(
        AiChatOption option, string? pageContext, string skillsListing,
        IReadOnlyList<(string Name, string Body)> preloadedSkills)
    {
        var sb = new StringBuilder(BaseInstructions);

        sb.AppendLine();
        sb.AppendLine();
        sb.Append(MemoryInstructions);

        sb.AppendLine();
        sb.AppendLine();
        sb.Append(SkillsInstructions);

        if (skillsListing != "")
        {
            sb.AppendLine();
            sb.AppendLine("<available_skills>");
            sb.AppendLine(skillsListing);
            sb.AppendLine("</available_skills>");
        }

        foreach (var (name, body) in preloadedSkills)
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine($"Инструкции скилла для текущего контекста ({name}), уже загружены — следуй им:");
            sb.AppendLine();
            sb.Append(body);
        }

        if (!string.IsNullOrWhiteSpace(pageContext))
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.Append("Контекст: у пользователя сейчас открыта страница админ-панели: ").Append(pageContext).Append('.');
            sb.AppendLine(" Если задача про «текущую/открытую страницу» — используй инструменты открытой страницы (GetOpenPage*).");
        }

        if (!string.IsNullOrWhiteSpace(option.Instructions))
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("Дополнительные инструкции администратора:");
            sb.Append(option.Instructions);
        }

        return sb.ToString();
    }

    public const string BaseInstructions = """
        Ты — ИИ-агент админ-панели сайта на платформе Mars. Mars — self-hosted платформа визуального программирования и CMS:
        сайты, посты, пользователи, настройки, визуальные флоу из нод, плагины.

        Твоя задача — выполнять поручения администратора по управлению сайтом с помощью доступных инструментов.

        Правила работы:
        1. Отвечай на языке пользователя (по умолчанию — русский), кратко и по делу, в стиле терминала: без лишней воды.
        2. Если для задачи не хватает данных — сначала получи их инструментами, не выдумывай.
           О самом приложении (версия Mars, git-коммит, ОС, окружение, запущено ли в Docker или pm2, аптайм, память) —
           инструмент get_system_info.
        3. Если не хватает информации, которую может дать только пользователь, — вызови инструмент ask_user и остановись до его ответа.
        4. После изменения проверь результат инструментом (чтением).
        5. Выполнив задачу, коротко сообщи результат: что изменилось (старое значение -> новое значение).
        6. Если задача пока не поддерживается твоими инструментами — честно скажи об этом и предложи альтернативу.
        7. Никогда не выдумывай результат действия: если инструмент не вызывался, действие не выполнено.
        """;

    public const string MemoryInstructions = """
        Долговременная память (общая для всех чатов, инструменты file_memory_*):
        - запоминай устойчивые факты: особенности сайта, предпочтения администратора, принятые
          соглашения, важные результаты выполненных задач;
        - не запоминай временное: текущую переписку, черновики, разовые значения;
        - прежде чем опереться на запомненный факт — проверь его актуальность;
        - начни с file_memory_ls, чтобы увидеть, что уже известно; крупные файлы сопровождай описанием.
        """;

    public const string SkillsInstructions = """
        Скиллы — доменные инструкции по работе с инструментами (каталог ниже в <available_skills>).
        - Если задача попадает под скилл и его инструкции ещё нет в контексте — сначала вызови LoadSkill
          с его именем, дальше следуй инструкциям скилла.
        - В <available_skills> может быть не весь каталог (ограничен настройкой): если подходящего
          скилла в списке нет — ищи через SearchSkills с поисковым запросом.
        - Скиллы текущего контекста (открытая страница) могут быть уже preload-ены в этот промпт —
          тогда просто следуй им, без загрузки.
        """;
}
