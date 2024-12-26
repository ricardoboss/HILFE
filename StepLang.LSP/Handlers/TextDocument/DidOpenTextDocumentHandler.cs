using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using StepLang.LSP.Diagnostics;
using StepLang.Parsing;
using StepLang.Tokenizing;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace StepLang.LSP.Handlers.TextDocument;

public class DidOpenTextDocumentHandler(ILogger<DidOpenTextDocumentHandler> logger, ILanguageServerFacade server)
	: DidOpenTextDocumentHandlerBase
{
	protected override TextDocumentOpenRegistrationOptions CreateRegistrationOptions(
		TextSynchronizationCapability capability,
		ClientCapabilities clientCapabilities)
	{
		return new TextDocumentOpenRegistrationOptions { DocumentSelector = StepTextDocumentSelector.Instance, };
	}

	public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
	{
		_ = Task.Run(() => RunDiagnostics(request.TextDocument));

		return Task.FromResult(Unit.Value);
	}

	private async Task RunDiagnostics(TextDocumentItem document)
	{
		logger.LogDebug("Running diagnostics for {DocumentUri}", document.Uri);

		var text = await File.ReadAllTextAsync(
			DocumentUri.GetFileSystemPath(document) ?? throw new InvalidOperationException()
		);

		var tokens = new Tokenizer(text).Tokenize().ToArray();
		var parser = new Parser(tokens);
		var root = parser.ParseRoot();

		var unusedDeclarations = new UsagesAnalyzer().FindUnusedDeclarations(root);

		var diagnostics = new PublishDiagnosticsParams
		{
			Uri = document.Uri,
			Version = document.Version,
			Diagnostics = new Container<Diagnostic>(
				new Diagnostic
				{
					Code = new DiagnosticCode("unused-declaration"),
					Message = "Unused declaration",
					Severity = DiagnosticSeverity.Warning,
					Range = new Range
					{
						Start = new Position { Line = 0, Character = 0, },
						End = new Position { Line = 0, Character = 0, },
					},
				}
			),
		};

		server.TextDocument.PublishDiagnostics(diagnostics);
	}
}
