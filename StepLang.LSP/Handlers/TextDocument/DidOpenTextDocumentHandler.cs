using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace StepLang.LSP.Handlers.TextDocument;

public class DidOpenTextDocumentHandler(ILogger<DidOpenTextDocumentHandler> logger, SessionState state)
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
		logger.LogTrace("Handling DidOpenTextDocument");

		AddDocumentToSession(request.TextDocument);

		return Task.FromResult(Unit.Value);
	}

	private void AddDocumentToSession(TextDocumentItem document)
	{
		var documentState = new DocumentState
		{
			DocumentUri = document.Uri.ToUri(),
			Version = document.Version ?? 0,
			Text = document.Text,
		};

		state.AddDocument(documentState);
	}
}
