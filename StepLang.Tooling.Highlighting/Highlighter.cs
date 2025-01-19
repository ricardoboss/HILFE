using StepLang.Tokenizing;

namespace StepLang.Tooling.Highlighting;

public class Highlighter(ColorScheme scheme)
{
	public IEnumerable<StyledToken> Highlight(string sourceCode)
	{
		var tokenizer = new Tokenizer(sourceCode, []);

		foreach (var token in tokenizer.Tokenize())
		{
			yield return new StyledToken(token.Type, token.Value, GetStyle(token.Type, scheme));
		}
	}

	public static Style GetStyle(TokenType tokenType, ColorScheme scheme)
	{
		return tokenType switch
		{
			TokenType.Identifier => scheme.Identifier,
			TokenType.TypeName => scheme.Type,
			TokenType.EqualsSymbol => scheme.Operator,
			TokenType.LiteralString => scheme.String,
			TokenType.LiteralNumber => scheme.Number,
			TokenType.LiteralBoolean => scheme.Bool,
			TokenType.LiteralNull => scheme.Null,
			TokenType.IfKeyword => scheme.Keyword,
			TokenType.ElseKeyword => scheme.Keyword,
			TokenType.WhileKeyword => scheme.Keyword,
			TokenType.BreakKeyword => scheme.Keyword,
			TokenType.ContinueKeyword => scheme.Keyword,
			TokenType.OpeningCurlyBracket => scheme.Punctuation,
			TokenType.ClosingCurlyBracket => scheme.Punctuation,
			TokenType.OpeningParentheses => scheme.Punctuation,
			TokenType.ClosingParentheses => scheme.Punctuation,
			TokenType.CommaSymbol => scheme.Punctuation,
			TokenType.GreaterThanSymbol => scheme.Operator,
			TokenType.LessThanSymbol => scheme.Operator,
			TokenType.PlusSymbol => scheme.Operator,
			TokenType.MinusSymbol => scheme.Operator,
			TokenType.AsteriskSymbol => scheme.Operator,
			TokenType.SlashSymbol => scheme.Operator,
			TokenType.PercentSymbol => scheme.Operator,
			TokenType.PipeSymbol => scheme.Operator,
			TokenType.AmpersandSymbol => scheme.Operator,
			TokenType.ExclamationMarkSymbol => scheme.Operator,
			TokenType.HatSymbol => scheme.Operator,
			TokenType.QuestionMarkSymbol => scheme.Operator,
			TokenType.ReturnKeyword => scheme.Keyword,
			TokenType.UnderscoreSymbol => scheme.Operator,
			TokenType.LineComment => scheme.Comment,
			TokenType.OpeningSquareBracket => scheme.Punctuation,
			TokenType.ClosingSquareBracket => scheme.Punctuation,
			TokenType.ColonSymbol => scheme.Punctuation,
			TokenType.ImportKeyword => scheme.Keyword,
			TokenType.ForEachKeyword => scheme.Keyword,
			TokenType.InKeyword => scheme.Keyword,
			TokenType.Error => scheme.Error,
			_ => scheme.Default,
		};
	}
}
