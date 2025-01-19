using StepLang.Diagnostics;
using StepLang.Interpreting;
using StepLang.Parsing;
using StepLang.Tokenizing;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StepLang.Tests.Integration;

public class FailuresIntegrationTest
{
	[Theory]
	[ClassData(typeof(FailureFiles))]
	public async Task TestFailuresFail(string exampleFilePath)
	{
		// arrange
		var exampleFile = new FileInfo(exampleFilePath);
		var stdInText = "";
		if (File.Exists(exampleFile.FullName + ".in"))
		{
			stdInText = await File.ReadAllTextAsync(exampleFile.FullName + ".in", TestContext.Current.CancellationToken);
		}

		var detailsFile = exampleFile.FullName + ".exception.json";
		Assert.SkipUnless(File.Exists(detailsFile), $"No exception details file found for {exampleFile.FullName}");

		var detailsContent = await File.ReadAllTextAsync(detailsFile, TestContext.Current.CancellationToken);
		var details =
			JsonSerializer.Deserialize(detailsContent, ExceptionDetailsJsonContext.Default.ListExceptionDetails);

		Assert.SkipWhen(details is null, $"Failed to deserialize exception details for {exampleFile.FullName}");

		await using var stdOut = new StringWriter();
		await using var stdErr = new StringWriter();
		using var stdIn = new StringReader(stdInText);

		// act
		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(exampleFile, diagnostics);
		var interpreter = new Interpreter(stdOut, stdErr, stdIn);

		// assert
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken);
		if (diagnostics.ContainsErrors)
		{
			AssertErrors(diagnostics, exampleFile, details);

			return;
		}

		var parser = new Parser(tokens, diagnostics);
		var root = parser.ParseRoot();
		if (diagnostics.ContainsErrors)
		{
			AssertErrors(diagnostics, exampleFile, details);

			return;
		}

		// no tokenizer error and no parser error means the exception must throw at runtime
		var e = Assert.ThrowsAny<StepLangException>(() => root.Accept(interpreter));

		AssertException(e, exampleFile, details);
	}

	private static void AssertErrors(DiagnosticCollection diagnostics, FileInfo sourceFile,
		List<ExceptionDetails> details)
	{
		Assert.Equal(details.Count, diagnostics.Count);

		foreach (var (diagnostic, detail) in diagnostics.Zip(details))
		{
			Assert.Multiple(() =>
			{
				Assert.Equal(detail.Severity, diagnostic.Severity);
				Assert.Equal(detail.Message, diagnostic.Message);
				Assert.Equal(detail.ErrorCode, diagnostic.Code);
				Assert.Equal(detail.Kind, diagnostic.Kind);
				Assert.Equal(detail.Area, diagnostic.Area);
				Assert.Equal(detail.Line, diagnostic.Line);
				Assert.Equal(detail.Column, diagnostic.Column);
				Assert.Equal(detail.Length, diagnostic.Length);
				Assert.Equal(sourceFile.FullName, diagnostic.File?.FullName);
			});
		}
	}

	private static void AssertException(StepLangException caught, FileInfo sourceFile, List<ExceptionDetails> details)
	{
		if (details.Count != 1)
			Assert.Fail("Expected exactly one exception detail to match against caught exception");

		var detail = details[0];

		Assert.Multiple(() =>
		{
			Assert.Equal(detail.ErrorCode, caught.ErrorCode);
			Assert.Equal(detail.Message, caught.Message);
			Assert.Equal(detail.HelpText, caught.HelpText);
			Assert.Equal(detail.Line, caught.Location?.Line);
			Assert.Equal(detail.Column, caught.Location?.Column);
			Assert.Equal(detail.Length, caught.Location?.Length);
			Assert.Equal(sourceFile.FullName, caught.Location?.File?.FullName);
		});
	}

	[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used by xUnit")]
	private sealed class FailureFiles : IEnumerable<TheoryDataRow<string>>
	{
		public IEnumerator<TheoryDataRow<string>> GetEnumerator()
		{
			return Directory
				.EnumerateFiles("Failures", "*.step")
				.Select(file => new TheoryDataRow<string>(file))
				.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Deserialized from JSON")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local", Justification = "Initialized from JSON")]
internal sealed class ExceptionDetails
{
	public string? ErrorCode { get; init; }
	public string? Message { get; init; }
	public string? HelpText { get; init; }
	[JsonConverter(typeof(JsonStringEnumConverter<Severity>))]
	public Severity? Severity { get; init; }
	public int Line { get; init; }
	public int Column { get; init; }
	public int? Length { get; init; }
	[JsonConverter(typeof(JsonStringEnumConverter<DiagnosticKind>))]
	public DiagnosticKind? Kind { get; init; }
	[JsonConverter(typeof(JsonStringEnumConverter<DiagnosticArea>))]
	public DiagnosticArea? Area { get; init; }
}

[JsonSourceGenerationOptions(WriteIndented = true, Converters = [typeof(JsonStringEnumConverter<DiagnosticArea>), typeof(JsonStringEnumConverter<DiagnosticKind>), typeof(JsonStringEnumConverter<Severity>)])]
[JsonSerializable(typeof(List<ExceptionDetails>))]
internal sealed partial class ExceptionDetailsJsonContext : JsonSerializerContext;
