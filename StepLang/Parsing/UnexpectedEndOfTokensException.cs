using StepLang.Tokenizing;

namespace StepLang.Parsing;

public class UnexpectedEndOfTokensException(Token? lastToken, string message = "Expected a statement")
	: ParserException(2, lastToken, message, GeneralHelpText)
{
	private const string GeneralHelpText =
		"The current statement was not complete. Check the syntax of the statement you are trying to use. Also review your code for syntax errors, such as missing parentheses or other punctuation.";
}
