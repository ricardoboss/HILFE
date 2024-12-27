using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace StepLang.LSP.Handlers.TextDocument;

internal sealed class DidChangeTextDocumentHandler(ILogger<DidChangeTextDocumentHandler> logger, SessionState state)
	: DidChangeTextDocumentHandlerBase
{
	protected override TextDocumentChangeRegistrationOptions CreateRegistrationOptions(
		TextSynchronizationCapability capability,
		ClientCapabilities clientCapabilities)
	{
		return new TextDocumentChangeRegistrationOptions
		{
			DocumentSelector = StepTextDocumentSelector.Instance, SyncKind = TextDocumentSyncKind.Full,
		};
	}

	public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
	{
		logger.LogTrace("Handling DidChangeTextDocument");

		var updatedText = "text"; // TODO

		state.UpdateDocument(request.TextDocument.Uri.ToUri(), request.TextDocument.Version ?? 0, updatedText);

		return Task.FromResult(Unit.Value);
	}
}
