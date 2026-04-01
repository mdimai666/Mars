namespace Mars.SemanticKernel.Shared.Options;

public interface ILLMOptions
{
    string ModelId { get; }
    string Endpoint { get; }
}
