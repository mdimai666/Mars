using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Mars.SemanticKernel.CMS.Plugins;

internal class InstructionPlugin
{
    private readonly string _root;

    public InstructionPlugin(string root)
    {
        _root = root;
    }

    [KernelFunction]
    [Description("list instruction markdown files as links")]
    public string ListInstructions()
    {
        var files = Directory.GetFiles(_root, "*.md")
                             .Select(Path.GetFileName);

        var links = files.Select(f => $"[{f}](<{f}>)");

        return string.Join("\n", links);
    }

    [KernelFunction]
    [Description("read file content")]
    public string ReadInstruction(
        [Description("Relative path to the file.")] string fileName)
    {
        var path = Path.Combine(_root, fileName);
        //Console.WriteLine($">ReadInstruction: {fileName}");
        return File.Exists(path)
            ? File.ReadAllText(path)
            : throw new FileNotFoundException($"Instruction '{fileName}' not found.");
    }

    public string ReadBasicConcepts()
        => ReadInstruction("BasicConcepts.md");

    public string DefaultSystemPrompt()
        => ReadInstruction("DefaultSystemPrompt.md");
}
