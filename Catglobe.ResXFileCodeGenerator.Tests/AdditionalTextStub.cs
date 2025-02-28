namespace Catglobe.ResXFileCodeGenerator.Tests;

internal class AdditionalTextStub(string path, string? text = null) : AdditionalText
{
    private readonly SourceText? _text = text is null ? null : SourceText.From(text, checksumAlgorithm: SourceHashAlgorithm.Sha256);

    public override string Path { get; } = path;

    public override SourceText? GetText(CancellationToken cancellationToken = new()) => _text;
}
