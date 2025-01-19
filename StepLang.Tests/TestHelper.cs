using StepLang.Diagnostics;
using StepLang.Interpreting;
using StepLang.Parsing;
using StepLang.Parsing.Nodes;
using StepLang.Tokenizing;

namespace StepLang.Tests;

internal static class TestHelper
{
	public static IEnumerable<Token> AsTokens(this string code, DiagnosticCollection? diagnostics = null)
	{
		var tokenizer = new Tokenizer(code, diagnostics ?? []);

		return tokenizer.Tokenize(TestContext.Current.CancellationToken);
	}

	public static RootNode AsParsed(this string code, DiagnosticCollection? diagnostics = null)
	{
		var parser = new Parser(code.AsTokens(diagnostics), diagnostics ?? []);

		return parser.ParseRoot();
	}

	public static void Interpret(this string code)
	{
		var root = code.AsParsed();

		var interpreter = new Interpreter();

		root.Accept(interpreter);
	}
}
