using StepLang.Interpreting;
using StepLang.Tokenizing;

namespace StepLang.Parsing.Nodes.VariableDeclarations;

public record ErrorVariableDeclarationNode(string Description, IReadOnlyCollection<Token> Tokens) : IVariableDeclarationNode
{
	public Variable EvaluateUsing(IVariableDeclarationEvaluator evaluator)
	{
		return evaluator.Evaluate(this);
	}

	public TokenLocation Location => Tokens.FirstOrDefault()?.Location ?? new TokenLocation();

	public IEnumerable<Token> Types { get; } = [];

	public Token Identifier { get; } = new(TokenType.Identifier, "?????");

	public bool HasValue => false;
}
