using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace StepLang.LSP.Diagnostics.Analyzers;

public class UnusedDeclarationsAnalyzer(ILogger<UnusedDeclarationsAnalyzer> logger) : IAnalyzer
{
	private const string DiagnosticCode = "unused-declaration";

	public Task AnalyzeAsync(SessionState sessionState, DocumentState documentState,
		CancellationToken cancellationToken)
	{
		var symbols = documentState.Symbols;
		if (symbols is null)
		{
			logger.LogWarning("Document {Document} has no symbols; skipping unused declarations analysis", documentState);

			return Task.CompletedTask;
		}

		var declarations = symbols.Declarations;
		var unused = declarations
			.Select(declaration => new
			{
				declaration, anyUsage = symbols.Scopes.Any(scope => scope.Usages.ContainsKey(declaration.Name)),
			})
			.Where(t => !t.anyUsage)
			.Select(t => t.declaration)
			.ToList();

		if (unused.Count == 0)
		{
			logger.LogTrace("Document {Document} has no unused declarations", documentState);

			return Task.CompletedTask;
		}

		foreach (var unusedDeclaration in unused)
		{
			sessionState.Diagnostics[documentState.DocumentUri].Add(CreateDiagnostic(unusedDeclaration));
		}

		return Task.CompletedTask;
	}

	private Diagnostic CreateDiagnostic(DeclaredVariableInfo unusedDeclaration)
	{
		logger.LogTrace("Creating diagnostic for unused declaration {Declaration}", unusedDeclaration);

		return new Diagnostic
		{
			Code = new DiagnosticCode(DiagnosticCode),
			Message = $"Unused declaration '{unusedDeclaration.Name}'",
			Severity = DiagnosticSeverity.Hint,
			Range = unusedDeclaration.Declaration.Location.ToRange(),
		};
	}
}
