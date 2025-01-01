using StepLang.Parsing.Nodes.VariableDeclarations;
using StepLang.Tokenizing;

namespace StepLang.LSP.Diagnostics;

public class VariableScope(TokenLocation openLocation, VariableScope? parent = null)
{
	public Dictionary<string, IVariableDeclarationNode> Declarations { get; } = new();
	public Dictionary<string, List<Token>> Usages { get; } = new();
	public VariableScope? Parent { get; } = parent;
	public IReadOnlyList<VariableScope> Children => children;
	private readonly List<VariableScope> children = [];

	public TokenLocation OpenLocation => openLocation;

	public TokenLocation? CloseLocation { get; set; }

	public VariableScope EnterChildScope(TokenLocation openTokenLocation)
	{
		var newScope = new VariableScope(openTokenLocation, this);

		children.Add(newScope);

		return newScope;
	}

	public IVariableDeclarationNode? FindDeclaration(string name)
	{
		return Declarations.TryGetValue(name, out var declaration) ? declaration : Parent?.FindDeclaration(name);
	}
}
