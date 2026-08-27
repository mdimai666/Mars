namespace Mars.SemanticKernel.Contracts.Options;

public interface ILLMOptions
{
    string ModelId { get; }
    string Endpoint { get; }
}
