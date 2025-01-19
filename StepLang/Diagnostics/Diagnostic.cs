using StepLang.Tokenizing;

namespace StepLang.Diagnostics;

public class Diagnostic
{
	public required Severity Severity { get; init; }

	public required string Message { get; init; }

	public required string Code { get; init; }

	public DiagnosticKind? Kind { get; init; }

	public DiagnosticArea Area { get; init; }

	public Token? Token { get; init; }

	private TokenLocation? tokenLocation;
	public TokenLocation? Location
	{
		get
		{
			if (tokenLocation is not null)
				return tokenLocation;

			if (Token is null)
				return null;

			tokenLocation = Token.Location;
			return tokenLocation;
		}
		init => tokenLocation = value;
	}

	public Uri? DocumentUri => Location?.DocumentUri;

	public FileSystemInfo? File => Location?.File;

	public int Line => Location?.Line ?? 0;

	public int Column => Location?.Column ?? 0;

	public int Length => Location?.Length ?? 0;

	public IEnumerable<Token>? RelatedTokens { get; init; }
}
