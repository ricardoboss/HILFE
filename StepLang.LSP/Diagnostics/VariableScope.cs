using StepLang.Parsing;
using StepLang.Tokenizing;

namespace StepLang.LSP.Diagnostics;

public class VariableScope(TokenLocation openLocation, VariableScope? parent = null)
{
	public readonly Dictionary<string, IVariableDeclarationNode> Declarations = new();
	public readonly Dictionary<string, List<Token>> Usages = new();
	public readonly VariableScope? Parent = parent;
	public readonly List<VariableScope> Children = [];

	public TokenLocation OpenLocation => openLocation;

	public TokenLocation? CloseLocation { get; set; }

	public VariableScope EnterChildScope(TokenLocation openTokenLocation)
	{
		var newScope = new VariableScope(openTokenLocation, this);

		Children.Add(newScope);

		return newScope;
	}

	public IVariableDeclarationNode? FindDeclaration(string name)
	{
		return Declarations.TryGetValue(name, out var declaration) ? declaration : Parent?.FindDeclaration(name);
	}
}
