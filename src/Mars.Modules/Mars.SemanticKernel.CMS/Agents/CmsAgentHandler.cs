using System.Reflection;
using Mars.Core.Extensions;
using Mars.SemanticKernel.Abstractions.Dto;
using Mars.SemanticKernel.Abstractions.Interfaces;
using Mars.SemanticKernel.CMS.Plugins;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Mars.SemanticKernel.CMS.Agents;

internal class CmsAgentHandler
{
    private readonly IKernelFactory _kernelFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly InstructionPlugin _instructions;

    public CmsAgentHandler(IKernelFactory kernelFactory, IServiceProvider serviceProvider, InstructionPlugin instructionPlugin)
    {
        _kernelFactory = kernelFactory;
        _serviceProvider = serviceProvider;
        _instructions = instructionPlugin;
    }

    public static void RegisterCmsAgent(IServiceCollection services, IWebHostEnvironment hostEnvironment)
    {
        services.AddTransient<CmsAgentHandler>();

        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var instructionRoot = Path.Combine(assemblyDir, "Skills");

        if (!Directory.Exists(instructionRoot))
            throw new DirectoryNotFoundException($"Instruction root directory not found: {instructionRoot}");

        var instructionPlugin = new InstructionPlugin(instructionRoot);
        services.AddSingleton(instructionPlugin);
    }

    public Task<AgentOutput> Handle(string prompt, string? systemPrompt = null, PromptExecutionSettings? promptExecutionSettings = null, CancellationToken cancellationToken = default)
    {
        ChatHistory history = [];
        if (systemPrompt.IsNotNullOrEmpty())
            history.AddSystemMessage(systemPrompt!);
        else
            history.AddSystemMessage(_instructions.DefaultSystemPrompt());
        history.AddSystemMessage(_instructions.ReadBasicConcepts());
        history.AddUserMessage(prompt);

        return Handle(history, promptExecutionSettings, cancellationToken);
    }

    public async Task<AgentOutput> Handle(ChatHistory chatMessages, PromptExecutionSettings? promptExecutionSettings = null, CancellationToken cancellationToken = default)
    {
        var kernel = _kernelFactory.Create();

        // Регистрируем плагины
        kernel.Plugins.AddFromObject(_instructions, pluginName: "Instruction");
        kernel.Plugins.AddFromType<PostPlugin>(serviceProvider: _serviceProvider);

        var executionSettings = promptExecutionSettings ?? _kernelFactory.ResolvePromptExecutionSettings();
        //var chat = kernel.GetRequiredService<IChatCompletionService>();
        var chat = _kernelFactory.CreateChatCompletionService();

        var response = await chat.GetChatMessageContentAsync(chatMessages, executionSettings, kernel, cancellationToken: cancellationToken);

        //https://github.com/microsoft/agent-framework - изучить

        return new() { Content = response.Content ?? "<none>" };
    }
}
