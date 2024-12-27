using StepLang.Parsing;
using StepLang.Tokenizing;

namespace StepLang.LSP.Diagnostics;

public class UsagesAnalyzer
{
	public IVariableDeclarationNode? FindDeclaration(RootNode document, Token identifier)
	{
		if (identifier.Type != TokenType.Identifier)
			throw new InvalidOperationException($"Expected identifier, got {identifier.Type}");

		var collector = new VariableCollector();

		collector.Visit(document);

		// gather all scopes into a flat list
		var searchScopes = new Queue<VariableScope>([collector.RootScope]);
		var allScopes = new List<VariableScope>();
		while (searchScopes.TryDequeue(out var s))
		{
			foreach (var c in s.Children)
				searchScopes.Enqueue(c);

			allScopes.Add(s);
		}

		// find the scope that contains the identifier
		var tightestScope = allScopes.FirstOrDefault();
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

		if (tightestScope is null)
			throw new InvalidOperationException(
				$"Could not find scope for identifier {identifier.Value}; are they in the same document?");

		return tightestScope.FindDeclaration(identifier.Value);
	}

	public IEnumerable<IVariableDeclarationNode> FindUnusedDeclarations(RootNode document)
	{
		yield break;
	}
}
