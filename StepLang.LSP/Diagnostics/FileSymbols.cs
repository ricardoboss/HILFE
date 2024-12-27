namespace StepLang.LSP.Diagnostics;

internal sealed class FileSymbols
{
	public required Uri DocumentUri { get; init; }

	public required IReadOnlyList<DeclaredVariableInfo> Declarations { get; init; }

	public required IReadOnlyList<VariableScope> Scopes { get; init; }
}
