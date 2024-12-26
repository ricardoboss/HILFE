using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace StepLang.LSP.Handlers.TextDocument;

public class CodeActionTextDocumentHandler : CodeActionHandlerBase
{
	protected override CodeActionRegistrationOptions CreateRegistrationOptions(CodeActionCapability capability,
		ClientCapabilities clientCapabilities)
	{
		return new()
		{
			DocumentSelector = StepTextDocumentSelector.Instance,
			ResolveProvider = false,
			WorkDoneProgress = false,
			CodeActionKinds = Container.From(CodeActionKind.Defaults),
		};
	}

	public override async Task<CommandOrCodeActionContainer?> Handle(CodeActionParams request, CancellationToken cancellationToken)
	{
		return new CommandOrCodeActionContainer(new CommandOrCodeAction(new CodeAction
		{
			Title = "Test",
			Kind = CodeActionKind.QuickFix,
		}));
	}

	public override async Task<CodeAction> Handle(CodeAction request, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}
