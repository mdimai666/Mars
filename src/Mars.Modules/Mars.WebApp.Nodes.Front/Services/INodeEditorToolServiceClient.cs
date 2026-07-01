using Flurl.Http;
using Mars.Nodes.Core.Models.EntityQuery;

namespace Mars.WebApp.Nodes.Front.Services;

public interface INodeEditorToolServiceClient
{
    Task<NodeEntityQueryBuilderDictionary> NodeEntityQueryBuilderDictionary();
    IFlurlClient Client { get; }
}
