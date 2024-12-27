using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using StepLang.Tokenizing;

namespace StepLang.LSP.Handlers;

internal sealed class SemanticTokensHandler(ILogger<SemanticTokensHandler> logger, SessionState state)
	: SemanticTokensHandlerBase
{
	protected override Task Tokenize(
		SemanticTokensBuilder builder,
		ITextDocumentIdentifierParams identifier,
		CancellationToken cancellationToken
	)
	{
		var document = state.Documents[identifier.TextDocument.Uri.ToUri()];
		if (document.Tokens is not { } tokens)
		{
			logger.LogWarning("Document {Document} is not fully processed; skipping semantic tokens", document);

			return Task.CompletedTask;
		}

		foreach (var token in tokens.Where(t =>
			         t.Type is not TokenType.Whitespace and not TokenType.NewLine and not TokenType.EndOfFile))
		{
			// shift by 1 because tokens in STEP are 1-indexed
			builder.Push(
				token.Location.Line - 1,
				token.Location.Column - 1,
				token.Location.Length,
				new SemanticTokenType(token.Type.ToString()),
				Array.Empty<SemanticTokenModifier>()
			);
		}

		return Task.CompletedTask;
	}

	protected override async Task<SemanticTokensDocument> GetSemanticTokensDocument(ITextDocumentIdentifierParams @params,
		CancellationToken cancellationToken)
	{
		var document = new SemanticTokensDocument(RegistrationOptions.Legend);
		var builder = new SemanticTokensBuilder(document, RegistrationOptions.Legend);

		await Tokenize(builder, @params, cancellationToken);

		return builder.Commit();
	}

	protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability? capability,
		ClientCapabilities clientCapabilities)
	{
		var legend = new SemanticTokensLegend
		{
			TokenModifiers = Container.From(Array.Empty<SemanticTokenModifier>()),
			TokenTypes = Container.From(Enum.GetValues<TokenType>().Select(c => new SemanticTokenType(c.ToString()))
				.ToArray()),
		};

		return new SemanticTokensRegistrationOptions
		{
			DocumentSelector = StepTextDocumentSelector.Instance, Legend = legend, Full = true, Range = true,
		};
	}
}
