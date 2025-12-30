using Mars.Nodes.Core;
using Mars.Shared.Models;
using Mars.Shared.Models.Interfaces;
using Mars.WebApp.Nodes.Front.Models.AppEntityForms;
using Mars.WebApp.Nodes.Models.AppEntityForms;

namespace Mars.WebApp.Nodes.Host.Builders;

public interface IAppEntityCreateFormBuilder
{
    AppEntityCreateFormSchema CreateForm(SourceUri entityUri);
    IReadOnlyCollection<AppEntityCreateFormSchema> AllForms();
    Task<IBasicEntity> Save(CreateAppEntityFromFormCommand form, NodeMsg input, CancellationToken cancellationToken);
    Task<int> DeleteMany(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
