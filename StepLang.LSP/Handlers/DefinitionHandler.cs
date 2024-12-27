using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using StepLang.LSP.Diagnostics;
using StepLang.Tokenizing;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace StepLang.LSP.Handlers;

internal sealed class DefinitionHandler(ILogger<DefinitionHandler> logger, SessionState state) : DefinitionHandlerBase
{
	protected override DefinitionRegistrationOptions CreateRegistrationOptions(DefinitionCapability capability,
		ClientCapabilities clientCapabilities)
	{
		return new DefinitionRegistrationOptions { DocumentSelector = StepTextDocumentSelector.Instance };
	}

	public override Task<LocationOrLocationLinks?> Handle(DefinitionParams request,
		CancellationToken cancellationToken)
	{
		logger.LogTrace("Handling Definition request");

		var documentUri = request.TextDocument.Uri.ToUri();
		var document = state.Documents[documentUri];
		if (document.Tokens is not { } tokens || document.Ast is not { } ast)
		{
			logger.LogWarning("Document {Document} is not fully processed; skipping definition analysis", document);

			return Task.FromResult<LocationOrLocationLinks?>(null);
		}

		// shift by 1 because tokens in STEP are 1-indexed
		var chosenToken = tokens.At(request.Position.Line + 1, request.Position.Character + 1);
		if (chosenToken is not { Type: TokenType.Identifier })
			return Task.FromResult<LocationOrLocationLinks?>(null);

		var declaration = new UsagesAnalyzer().FindDeclaration(documentUri, ast, chosenToken);
		if (declaration is null)
			return Task.FromResult<LocationOrLocationLinks?>(null);

		var result = new LocationOrLocationLinks(
			new LocationOrLocationLink(
				new Location
				{
					Uri = declaration.Location.DocumentUri ?? request.TextDocument.Uri,
					Range = new Range
					{
						Start = declaration.Identifier.Location.ToPosition(),
					},
				}
			)
		);

		return Task.FromResult<LocationOrLocationLinks?>(result);
	}
}
