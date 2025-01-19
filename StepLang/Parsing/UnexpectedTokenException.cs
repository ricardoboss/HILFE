using StepLang.Tokenizing;

namespace StepLang.Parsing;

public class UnexpectedTokenException(string message, Token token) : ParserException(1, token, message, BuildHelpText())
{
	private static readonly char[] Vowels = ['a', 'e', 'i', 'o', 'u'];
	private static string BuildMessage(Token token, TokenType[] allowed)
	{
		string expectation;
		switch (allowed.Length)
		{
			case 0:
				expectation = "nothing";
				break;
			case 1:
				var display = allowed[0].ToDisplay();
				var firstChar = display.ToLowerInvariant()[0];
				var article = Vowels.Any(c => firstChar == c) ? "an" : "a";

				expectation = $"{article} {display}";
				break;
			default:
				expectation =
					$"one of {string.Join(", ", allowed[..^1].Select(TokenTypes.ToDisplay))} or {allowed[^1].ToDisplay()}";

				break;
		}

		return $"Expected {expectation}, but got {token.Type.ToDisplay()} instead";
	}

	private static string BuildHelpText()
	{
		return
			"This error is usually caused by a missing token or a typo. Check the syntax of the statement you are trying to use. Also review your code for syntax errors, such as missing parentheses or other punctuation.";
	}

	public UnexpectedTokenException(Token token, params TokenType[] allowed) : this(BuildMessage(token, allowed), token)
	{
		Token = token;
	}

	public Token Token { get; } = token;
}
