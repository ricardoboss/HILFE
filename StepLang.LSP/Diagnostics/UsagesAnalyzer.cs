using StepLang.Parsing;
using StepLang.Tokenizing;

namespace StepLang.LSP.Diagnostics;

public class UsagesAnalyzer
{
	public IVariableDeclarationNode? FindDeclaration(Uri documentUri, RootNode document, Token identifier)
	{
		if (identifier.Type != TokenType.Identifier)
			throw new InvalidOperationException($"Expected identifier, got {identifier.Type}");

		var collector = new VariableDeclarationCollector(documentUri);

		collector.Visit(document);

		// find the scope that contains the identifier
		var allScopes = collector.Scopes;
		var tightestScope = collector.RootScope;
		foreach (var scope in allScopes.OrderBy(s => s.OpenLocation.Line).ThenBy(s => s.OpenLocation.Column))
		{
			if (scope.OpenLocation.Line <= identifier.Location.Line &&
			    identifier.Location.Line <= scope.CloseLocation?.Line &&
			    scope.OpenLocation.Column <= identifier.Location.Column &&
			    identifier.Location.Column <= scope.CloseLocation?.Column)
			{
				tightestScope = scope;
			}
		}

		return tightestScope.FindDeclaration(identifier.Value);
	}

	public IEnumerable<IVariableDeclarationNode> FindUnusedDeclarations(RootNode document)
	{
		yield break;
	}
}
