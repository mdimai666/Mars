using Mars.Nodes.Front.Abstractions.Editor.Interfaces;

namespace Mars.Nodes.Workspace.Components;

public record NodeEditorSettingsDialogModel(string StartMenuName, INodeEditorApi Editor);

public record NodeTaskJobResultDialogModel(Guid TaskId, INodeEditorApi Editor);
