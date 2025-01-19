using StepLang.Diagnostics;
using StepLang.Interpreting;
using StepLang.Parsing;
using StepLang.Tokenizing;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace StepLang.Tests.Integration;

public class ExamplesIntegrationTest
{
	[Theory]
	[ClassData(typeof(ExampleFiles))]
	public async Task TestExamplesSucceed(string exampleFilePath)
	{
		// arrange
		var exampleFile = new FileInfo(exampleFilePath);
		var stdInText = "";
		if (File.Exists(exampleFile.FullName + ".in"))
		{
			stdInText = await File.ReadAllTextAsync(exampleFile.FullName + ".in",
				TestContext.Current.CancellationToken);
		}

		await using var stdOut = new StringWriter();
		await using var stdErr = new StringWriter();
		using var stdIn = new StringReader(stdInText);

		var expectedExitCode = 0;
		var expectedOutput = "";
		var expectedError = "";

		if (File.Exists(exampleFile.FullName + ".exit"))
		{
			var exitCodeStr =
				await File.ReadAllTextAsync(exampleFile.FullName + ".exit", TestContext.Current.CancellationToken);

			expectedExitCode = int.Parse(exitCodeStr, CultureInfo.InvariantCulture);
		}

		if (File.Exists(exampleFile.FullName + ".out"))
		{
			expectedOutput =
				await File.ReadAllTextAsync(exampleFile.FullName + ".out", TestContext.Current.CancellationToken);
		}

		if (File.Exists(exampleFile.FullName + ".err"))
		{
			expectedError =
				await File.ReadAllTextAsync(exampleFile.FullName + ".err", TestContext.Current.CancellationToken);
		}

		// act
		var diagnostics = new DiagnosticCollection();

		var tokenizer = new Tokenizer(exampleFile, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken);

		var parser = new Parser(tokens, diagnostics);
		var root = parser.ParseRoot();

		var interpreter = new Interpreter(stdOut, stdErr, stdIn);
		root.Accept(interpreter);

		// assert
		Assert.Multiple([SuppressMessage("ReSharper", "AccessToDisposedClosure")] () =>
		{
			Assert.Equal(expectedExitCode, interpreter.ExitCode);
			Assert.Equal(expectedOutput, stdOut.ToString(), ignoreLineEndingDifferences: true);
			Assert.Equal(expectedError, stdErr.ToString(), ignoreLineEndingDifferences: true);
			Assert.Empty(diagnostics);
		});
	}

	[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used by xUnit")]
	private sealed class ExampleFiles : IEnumerable<TheoryDataRow<string>>
	{
		public IEnumerator<TheoryDataRow<string>> GetEnumerator()
		{
			return Directory
				.EnumerateFiles("Examples", "*.step", SearchOption.AllDirectories)
				.Select(path => new TheoryDataRow<string>(path))
				.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
