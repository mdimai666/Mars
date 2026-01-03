using System.ComponentModel.DataAnnotations;
using Mars.Nodes.Core;
using Mars.WebApp.Nodes.Models.AppEntityForms;

namespace Mars.WebApp.Nodes.Nodes;

[Display(GroupName = "entity")]
public class AppEntityCreateNode : Node
{
    public override string DisplayName => FormCommand.EntityUri.HasValue ? $"Create {FormCommand.EntityUri}" : Label;

    public CreateAppEntityFromFormCommand FormCommand { get; set; } = new() { EntityUri = "", PropertyBindings = [] };

    public AppEntityCreateNode()
    {
        Color = "#21a366";
        Icon = "_content/Mars.Nodes.Workspace/nodes/db-48.png";
        Inputs = [new()];
        Outputs = [
            new (){ Label = "Success" },
            new (){ Label = "ValidationError" },
        ];
    }
}
