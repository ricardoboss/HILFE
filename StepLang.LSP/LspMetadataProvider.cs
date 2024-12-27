using cmdwtf;
using StepLang.Tooling.Meta;

namespace StepLang.LSP;

internal sealed class LspMetadataProvider : IMetadataProvider
{
	public static LspMetadataProvider Instance { get; } = new();

	private LspMetadataProvider() { }

	public DateTimeOffset BuildTime => BuildTimestamp.BuildTimeDto;

	public string FullSemVer => GitVersionInformation.FullSemVer;

	public string Sha => GitVersionInformation.Sha;

	public string BranchName => GitVersionInformation.BranchName;
}
