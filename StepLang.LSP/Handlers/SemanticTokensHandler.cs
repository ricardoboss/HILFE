using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using StepLang.Tokenizing;

namespace StepLang.LSP.Handlers;

public class SemanticTokensHandler : SemanticTokensHandlerBase
{
	protected override async Task Tokenize(
		SemanticTokensBuilder builder,
		ITextDocumentIdentifierParams identifier,
		CancellationToken cancellationToken
	)
	{
		ArgumentNullException.ThrowIfNull(builder);

		var text = await File.ReadAllTextAsync(
			DocumentUri.GetFileSystemPath(identifier) ?? throw new InvalidOperationException(), cancellationToken);

		var tokens = new Tokenizer(text).Tokenize(cancellationToken).ToList();

		foreach (var token in tokens.Where(t =>
			         t.Type is not TokenType.Whitespace and not TokenType.NewLine and not TokenType.EndOfFile))
		{
			builder.Push(
				token.Location.Line,
				token.Location.Column,
				token.Location.Length,
				new SemanticTokenType(token.Type.ToString()),
				Array.Empty<SemanticTokenModifier>()
			);
		}

		builder.Commit();
	}

	protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(ITextDocumentIdentifierParams @params,
		CancellationToken cancellationToken)
	{
		return Task.FromResult(new SemanticTokensDocument(RegistrationOptions.Legend));
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
