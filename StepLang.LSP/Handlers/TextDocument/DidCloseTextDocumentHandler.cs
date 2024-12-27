using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace StepLang.LSP.Handlers.TextDocument;

internal sealed class DidCloseTextDocumentHandler(ILogger<DidCloseTextDocumentHandler> logger, SessionState state)
	: DidCloseTextDocumentHandlerBase
{
	protected override TextDocumentCloseRegistrationOptions CreateRegistrationOptions(
		TextSynchronizationCapability capability,
		ClientCapabilities clientCapabilities)
	{
		return new TextDocumentCloseRegistrationOptions { DocumentSelector = StepTextDocumentSelector.Instance };
	}

	public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
	{
		logger.LogTrace("Handling DidCloseTextDocument");

		state.RemoveDocument(request.TextDocument.Uri.ToUri());

		return Task.FromResult(Unit.Value);
	}
}
