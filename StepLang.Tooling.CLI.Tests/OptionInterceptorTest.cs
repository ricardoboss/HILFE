using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;
using StepLang.Tooling.CLI.Widgets;
using StepLang.Tooling.Meta;
using System.Runtime.InteropServices;

namespace StepLang.Tooling.CLI.Tests;

public class OptionInterceptorTest
{
	[Fact]
	public void TestDoesNothingForNonGlobalCommandSettings()
	{
		// Arrange
		var consoleMock = new Mock<IAnsiConsole>();
		var metadataProviderMock = new Mock<IMetadataProvider>();
		var remainingArgumentsMock = new Mock<IRemainingArguments>();

		var context = new CommandContext([], remainingArgumentsMock.Object, "test", null);

		var interceptor = new OptionInterceptor(
			consoleMock.Object,
			metadataProviderMock.Object,
			new Dictionary<string, IMetadataProvider>(),
			() => { }
		);

		var settings = new EmptyCommandSettings();

		// Act
		interceptor.Intercept(context, settings);

		// Assert
		consoleMock.VerifyAll();
		metadataProviderMock.VerifyAll();
		remainingArgumentsMock.VerifyAll();
	}

	[Fact]
	public void TestDoesNothingIfNoOptionsAreSet()
	{
		// Arrange
		var consoleMock = new Mock<IAnsiConsole>();
		var metadataProviderMock = new Mock<IMetadataProvider>();
		var remainingArgumentsMock = new Mock<IRemainingArguments>();

		var context = new CommandContext([], remainingArgumentsMock.Object, "test", null);

		var interceptor = new OptionInterceptor(
			consoleMock.Object,
			metadataProviderMock.Object,
			new Dictionary<string, IMetadataProvider>(),
			() => { }
		);

		var settings = new EmptyGlobalCommandSettings { Info = false, Version = false, };

		// Act
		interceptor.Intercept(context, settings);

		// Assert
		consoleMock.VerifyAll();
		metadataProviderMock.VerifyAll();
		remainingArgumentsMock.VerifyAll();
	}

	[Fact]
	public void TestPrintsVersionIfVersionOptionIsSet()
	{
		// Arrange
		const string fullSemVer = "fullSemVer";

		var consoleMock = new Mock<IAnsiConsole>();
		var metadataProviderMock = new Mock<IMetadataProvider>();
		var remainingArgumentsMock = new Mock<IRemainingArguments>();

		metadataProviderMock
			.Setup(g => g.FullSemVer)
			.Returns(fullSemVer)
			.Verifiable();

		IRenderable? writtenWidget = null;
		consoleMock
			.Setup(c => c.Write(It.IsAny<IRenderable>()))
			.Callback<IRenderable>(t => writtenWidget = t)
			.Verifiable();

		var context = new CommandContext([], remainingArgumentsMock.Object, "test", null);

		var interceptor = new OptionInterceptor(
			consoleMock.Object,
			metadataProviderMock.Object,
			new Dictionary<string, IMetadataProvider>(),
			() => { }
		);

		var settings = new EmptyGlobalCommandSettings { Info = false, Version = true, };

		// Act
		interceptor.Intercept(context, settings);

		// Assert
		Assert.NotNull(writtenWidget);
		var text = Assert.IsType<Text>(writtenWidget);
		Assert.Equal(fullSemVer, text.GetTextContent().TrimEnd(Environment.NewLine.ToCharArray()));

		consoleMock.VerifyAll();
		metadataProviderMock.VerifyAll();
		remainingArgumentsMock.VerifyAll();
	}

	[Fact]
	public void TestPrintsInfoGridIfInfoOptionIsSet()
	{
		// Arrange
		const string metadataComponentName = "Test";
		var buildTimeUtc = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
		const string sha = "sha";
		const string fullSemVer = "fullSemVer";
		const string branchName = "branchName";

		var consoleMock = new Mock<IAnsiConsole>();
		var metadataProviderMock = new Mock<IMetadataProvider>();
		var remainingArgumentsMock = new Mock<IRemainingArguments>();

		metadataProviderMock
			.SetupGet(g => g.BuildTime)
			.Returns(buildTimeUtc)
			.Verifiable();

		metadataProviderMock
			.Setup(g => g.Sha)
			.Returns(sha)
			.Verifiable();

		metadataProviderMock
			.Setup(g => g.FullSemVer)
			.Returns(fullSemVer)
			.Verifiable();

		metadataProviderMock
			.Setup(g => g.BranchName)
			.Returns(branchName)
			.Verifiable();

		metadataProviderMock
			.Setup(b => b.BuildTime)
			.Returns(buildTimeUtc)
			.Verifiable();

		IRenderable? writtenWidget = null;
		consoleMock
			.Setup(c => c.Write(It.IsAny<IRenderable>()))
			.Callback<IRenderable>(t => writtenWidget = t)
			.Verifiable();

		var context = new CommandContext([], remainingArgumentsMock.Object, "test", null);

		var interceptor = new OptionInterceptor(
			consoleMock.Object,
			metadataProviderMock.Object,
			new Dictionary<string, IMetadataProvider>
			{
				{ metadataComponentName, metadataProviderMock.Object },
			},
			() => { }
		);

		var settings = new EmptyGlobalCommandSettings { Info = true, Version = false, };

		// Act
		interceptor.Intercept(context, settings);

		// Assert
		Assert.NotNull(writtenWidget);
		var definitionList = Assert.IsType<DefinitionList>(writtenWidget);

		Assert.Equal(2, definitionList.Items.Count);

		var metadataComponentLabel = Assert.IsType<Markup>(definitionList.Items[0].Label);
		Assert.Equal(metadataComponentName, metadataComponentLabel.GetTextContent());

		var metadataGrid = Assert.IsType<Grid>(definitionList.Items[0].Definition);
		Assert.Equal(2, metadataGrid.Columns.Count);
		Assert.Equal(3, metadataGrid.Rows.Count);

		var buildDateRow = metadataGrid.Rows[0];
		var buildDateHeader = buildDateRow[0];
		var buildDateValue = buildDateRow[1];
		var buildDateHeaderText = Assert.IsType<Text>(buildDateHeader);
		Assert.Equal("Build Date", buildDateHeaderText.GetTextContent());
		var buildDateValueText = Assert.IsType<Text>(buildDateValue);
		Assert.Equal("2025-01-01 00:00:00 UTC", buildDateValueText.GetTextContent());

		var versionRow = metadataGrid.Rows[1];
		var versionHeader = versionRow[0];
		var versionValue = versionRow[1];
		var versionHeaderText = Assert.IsType<Text>(versionHeader);
		Assert.Equal("Version", versionHeaderText.GetTextContent());
		var versionValueText = Assert.IsType<Text>(versionValue);
		Assert.Equal($"{sha} ({fullSemVer})", versionValueText.GetTextContent());

		var branchRow = metadataGrid.Rows[2];
		var branchHeader = branchRow[0];
		var branchValue = branchRow[1];
		var branchHeaderText = Assert.IsType<Text>(branchHeader);
		Assert.Equal("Branch", branchHeaderText.GetTextContent());
		var branchValueText = Assert.IsType<Text>(branchValue);
		Assert.Equal(branchName, branchValueText.GetTextContent());

		var environmentLabel = Assert.IsType<Markup>(definitionList.Items[1].Label);
		Assert.Equal("Environment", environmentLabel.GetTextContent());

		var environmentGrid = Assert.IsType<Grid>(definitionList.Items[1].Definition);
		Assert.Equal(2, environmentGrid.Columns.Count);
		Assert.Equal(2, environmentGrid.Rows.Count);

		var clrVersionRow = environmentGrid.Rows[0];
		var clrVersionHeader = clrVersionRow[0];
		var clrVersionValue = clrVersionRow[1];
		var clrVersionHeaderText = Assert.IsType<Text>(clrVersionHeader);
		Assert.Equal("CLR Version", clrVersionHeaderText.GetTextContent());
		var clrVersionValueText = Assert.IsType<Text>(clrVersionValue);
		Assert.Equal(Environment.Version.ToString(), clrVersionValueText.GetTextContent());

		var osVersionRow = environmentGrid.Rows[1];
		var osVersionHeader = osVersionRow[0];
		var osVersionValue = osVersionRow[1];
		var osVersionHeaderText = Assert.IsType<Text>(osVersionHeader);
		Assert.Equal("OS Version", osVersionHeaderText.GetTextContent());
		var osVersionValueText = Assert.IsType<Text>(osVersionValue);
		Assert.Equal($"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})", osVersionValueText.GetTextContent());

		consoleMock.VerifyAll();
		metadataProviderMock.VerifyAll();
		remainingArgumentsMock.VerifyAll();
	}
}

file sealed class EmptyGlobalCommandSettings : CommandSettings, IGlobalCommandSettings
{
	public required bool Info { get; init; }

	public required bool Version { get; init; }
}
