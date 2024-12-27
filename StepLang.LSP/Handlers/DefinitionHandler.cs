using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using StepLang.LSP.Diagnostics;
using StepLang.Parsing;
using StepLang.Tokenizing;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace StepLang.LSP.Handlers;

public class DefinitionHandler : DefinitionHandlerBase
{
	protected override DefinitionRegistrationOptions CreateRegistrationOptions(DefinitionCapability capability,
		ClientCapabilities clientCapabilities)
	{
		return new DefinitionRegistrationOptions { DocumentSelector = StepTextDocumentSelector.Instance, };
	}

	public override async Task<LocationOrLocationLinks?> Handle(DefinitionParams request,
		CancellationToken cancellationToken)
	{
		var text = await File.ReadAllTextAsync(
			DocumentUri.GetFileSystemPath(request.TextDocument) ?? throw new InvalidOperationException(),
			cancellationToken
		);

		var tokens = new TokenCollection(new Tokenizer(text).Tokenize(cancellationToken).ToArray());
		// shift by 1 because tokens in STEP are 1-indexed
		var chosenToken = tokens.At(request.Position.Line + 1, request.Position.Character + 1);
		if (chosenToken is not { Type: TokenType.Identifier })
			return null;

		var parser = new Parser(tokens);
		var root = parser.ParseRoot();
		var declaration = new UsagesAnalyzer().FindDeclaration(root, chosenToken);
		if (declaration is null)
			return null;

		return new LocationOrLocationLinks(
			new LocationOrLocationLink(
				new Location
				{
					Uri = declaration.Location.DocumentUri ?? request.TextDocument.Uri,
					Range = new Range
					{
						Start = new Position
						{
							Line = declaration.Identifier.Location.Line - 1,
							Character = declaration.Identifier.Location.Column - 1,
						},
					},
				}
			)
		);
	}
}
