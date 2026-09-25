namespace Mars.CodeCompletion.Contracts.Dto;

public class SignatureHelpResponseDto
{
    public IReadOnlyList<SignatureDto> Signatures { get; set; } = [];

    public int ActiveSignature { get; set; }

    public int ActiveParameter { get; set; }
}

public class SignatureDto
{
    public string Label { get; set; } = "";

    public string? Documentation { get; set; }

    public IReadOnlyList<SignatureParameterDto> Parameters { get; set; } = [];
}

public class SignatureParameterDto
{
    public string Label { get; set; } = "";

    public string? Documentation { get; set; }
}
