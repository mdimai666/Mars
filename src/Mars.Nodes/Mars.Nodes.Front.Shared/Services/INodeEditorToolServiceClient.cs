using Mars.Nodes.Core.Models.EntityQuery;

namespace Mars.Nodes.Front.Shared.Services;

public interface INodeEditorToolServiceClient
{
    Task<NodeEntityQueryBuilderDictionary> NodeEntityQueryBuilderDictionary();
}
