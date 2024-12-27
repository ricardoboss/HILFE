using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace StepLang.LSP.Handlers.TextDocument;

internal sealed class CodeActionTextDocumentHandler : CodeActionHandlerBase
{
	protected override CodeActionRegistrationOptions CreateRegistrationOptions(CodeActionCapability capability,
		ClientCapabilities clientCapabilities)
	{
		return new CodeActionRegistrationOptions
		{
			DocumentSelector = StepTextDocumentSelector.Instance,
			ResolveProvider = false,
			WorkDoneProgress = false,
			CodeActionKinds = Container.From(CodeActionKind.Empty),
		};
	}

	public override async Task<CommandOrCodeActionContainer?> Handle(CodeActionParams request, CancellationToken cancellationToken)
	{
		return null;
	}

	public override async Task<CodeAction> Handle(CodeAction request, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}
