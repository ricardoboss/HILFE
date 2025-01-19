using StepLang.Tokenizing;

namespace StepLang.Parsing;

public abstract class ParserException(
	int errorCode,
	Token? lastToken,
	string message,
	string helpText,
	Exception? inner = null)
	: StepLangException($"PAR{errorCode:000}", lastToken?.Location, message, helpText, inner)
{
	public Token? LastToken { get; } = lastToken;
}
