using Mars.Nodes.EditorApi.Interfaces;
using Mars.Nodes.Workspace.Services;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

[EditorActionCommand("TerminateAllJobs", "Ctrl+Shift+KeyT")]
public class TerminateAllJobsAction(INodeServiceClient apiClient) : IEditorAction
{
    public bool CanExecute() => true;

    public void Execute() => apiClient.TerminateAllJobs();

}
