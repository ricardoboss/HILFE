using StepLang.Diagnostics;

namespace StepLang.Tokenizing;

public static class TokenizingDiagnosticCollectionExtensions
{
	public static void AddUnterminatedString(this DiagnosticCollection collection, Token errorToken)
	{
		collection.Add(DiagnosticArea.Tokenizing, Severity.Error, "Unterminated string", "TOK002", errorToken);
	}

	public static void AddInvalidIdentifier(this DiagnosticCollection collection, Token errorToken)
	{
		collection.Add(DiagnosticArea.Tokenizing, Severity.Error, "Invalid identifier", "TOK001", errorToken);
	}

	public static void AddUnescapedControlCharacter(this DiagnosticCollection collection, Token errorToken)
	{
		collection.Add(DiagnosticArea.Tokenizing, Severity.Error, "Unescaped control character", "TOK002", errorToken);
	}
}
