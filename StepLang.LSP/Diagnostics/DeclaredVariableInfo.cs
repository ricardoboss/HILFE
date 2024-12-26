using StepLang.Parsing;

namespace StepLang.LSP.Diagnostics;

public class DeclaredVariableInfo
{
	public required string Name { get; init; }

	public required IVariableDeclarationNode Declaration { get; init; }

	public required VariableScope? DeclaringScope { get; init; }

	public override string ToString()
	{
		return $"DeclaredVariableInfo: \"{Name}\" in {Declaration.Location}";
	}
}
