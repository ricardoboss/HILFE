using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StepLang.LSP.Diagnostics.Analyzers;

namespace StepLang.LSP.Diagnostics;

internal sealed class DiagnosticsRunner(IServiceProvider services, ILogger<DiagnosticsRunner> logger)
{
	private readonly Lazy<IAnalyzer[]> lazyAnalyzers = new(() => services.GetServices<IAnalyzer>().ToArray());

	private IAnalyzer[] Analyzers => lazyAnalyzers.Value;

	public void Dispatch(SessionState session, DocumentState document)
	{
		logger.LogDebug("Dispatching diagnostics for document {Document}", document);

		_ = ThreadPool.QueueUserWorkItem(RunDiagnostics, new DiagnosticsContext(session, document));
	}

	private class DiagnosticsContext(SessionState session, DocumentState document)
	{
		public SessionState Session { get; } = session;

		public DocumentState Document { get; } = document;
	}

	private void RunDiagnostics(object? state)
	{
		if (state is not DiagnosticsContext context)
			throw new InvalidOperationException("Unexpected state object");

		logger.LogDebug("Running diagnostics for document {Document}", context.Document);

		Task.WhenAll(
				Analyzers.Select(a =>
				{
					logger.LogTrace("Running analyzer \"{Analyzer}\" on document {Document}", a.Name, context.Document);

					return a.AnalyzeAsync(context.Session, context.Document, CancellationToken.None);
				})
			)
			.ConfigureAwait(true)
			.GetAwaiter()
			.GetResult();

		logger.LogDebug("Finished running diagnostics for document {Document}", context.Document);
	}
}
