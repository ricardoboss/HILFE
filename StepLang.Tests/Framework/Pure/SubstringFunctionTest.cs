using StepLang.Diagnostics;
using StepLang.Interpreting;
using StepLang.Parsing;
using StepLang.Tokenizing;

namespace StepLang.Tests.Framework.Pure;

public class SubstringFunctionTest
{
	[Theory]
	[InlineData("substring(\"abc\", 0, 1)", "a")]
	[InlineData("substring(\"Hello, world!\", 0, 5)", "Hello")]
	[InlineData("substring(\"Hello, world!\", 7, 5)", "world")]
	[InlineData("substring(\"Hello, world!\", 7, 100)", "world!")]
	[InlineData("substring(\"Hello, world!\", 7, 0)", "")]
	[InlineData("substring(\"Hello, world!\", 7, -1)", "")]
	[InlineData("substring(\"Hello, world!\", 7, 1.5)", "wo")]
	[InlineData("substring(\"Hello, world!\", 7.5, 1)", "o")]
	[InlineData("substring(\"Hello, world!\", 13, 0)", "")]
	[InlineData("substring(\"Hello, world!\", 13, 1)", "")]
	[InlineData("substring(\"Hello, world!\", 100, 5)", "")]
	[InlineData("substring(\"Hello, world!\", 12, 1)", "!")]
	[InlineData("substring(\"Hello, world!\", -1, 3)", "!")]
	[InlineData("substring(\"Hello, world!\", -2, 3)", "d!")]
	[InlineData("substring(\"Hello, world!\", -3, 3)", "ld!")]
	[InlineData("substring(\"Hello, world!\", -4, 3)", "rld")]
	[InlineData("substring(\"Hello, world!\", -13, 5)", "Hello")]
	[InlineData("substring(\"Hello, world!\", -14, 5)", "")]
	[InlineData("substring(\"Hello, world!\", -100, 5)", "")]
	public void TestSubstring(string code, string result)
	{
		var diagnostics = new DiagnosticCollection();

		var tokenizer = new Tokenizer($"print({code})", diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken);

		var parser = new Parser(tokens, diagnostics);
		var root = parser.ParseRoot();

		using var output = new StringWriter();
		var interpreter = new Interpreter(output);
		root.Accept(interpreter);

		Assert.Equal(result, output.ToString());
		Assert.Empty(diagnostics);
	}
}
