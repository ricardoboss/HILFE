namespace StepLang.LSP.Diagnostics.Analyzers;

internal interface IAnalyzer
{
	string Name => GetType().Name;

	Task AnalyzeAsync(SessionState sessionState, DocumentState documentState, CancellationToken cancellationToken);
}
