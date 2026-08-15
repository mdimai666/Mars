namespace Mars.PxBlocks.Workspace;

/// <summary>Режим запуска в <see cref="PxSandboxEditor"/>.</summary>
public enum PxEditorRunMode
{
    /// <summary>По умолчанию: исполняются все верхнеуровневые стеки (события Loop — после всех).</summary>
    AllTopLevel,

    /// <summary>
    /// Исполняются только блоки-события, чьи имена переданы в PxSandboxEditor.RunEventNames:
    /// фазы в порядке списка — сначала все события первого имени, затем второго и т.д.
    /// </summary>
    Events
}
