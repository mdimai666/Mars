using System.Reflection;
using Mars.Nodes.Core.Attributes;
using Microsoft.AspNetCore.Components;

namespace Mars.Nodes.FormEditor;

public interface INodeFormsLocator
{
    public void RegisterAssembly(Assembly assembly);
    public Type GetForNodeType(Type nodeType);
    public Type? TryGetForNodeType(Type nodeType);

    public List<Type> RegisteredForms();

    /// <summary>
    /// find NodeEditFormForNodeAttribute in assembly
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns>Key NodeType; Valye FormType</returns>
    public Dictionary<Type, Type> GetNodeEditForms(Assembly assembly);
}
