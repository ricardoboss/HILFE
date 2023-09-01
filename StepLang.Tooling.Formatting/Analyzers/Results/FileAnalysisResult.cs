namespace StepLang.Tooling.Formatting.Analyzers.Results;

public record FileAnalysisResult : AnalysisResult
{
    public static FileAnalysisResult FixedAt(FileInfo fixedFile) => new(true, fixedFile);

    public static FileAnalysisResult RetainOriginal(FileInfo originalFile) => new(false, originalFile);

    private FileAnalysisResult(bool FixRequired, FileInfo FixedFile) : base(FixRequired)
    {
        this.FixedFile = FixedFile;
    }

    public FileInfo FixedFile { get; }
}