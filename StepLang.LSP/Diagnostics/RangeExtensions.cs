using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using StepLang.Tokenizing;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace StepLang.LSP.Diagnostics;

internal static class RangeExtensions
{
	public static Range ToRange(this TokenLocation location)
	{
		var start = location.ToPosition();

		var end = new Position
		{
			Line = start.Line,
			Character = start.Character + location.Length,
		};

		return new Range(start, end);
	}

	public static Range ToRangeStart(this TokenLocation location)
	{
		var start = location.ToPosition();

		return new Range(start, start);
	}

	public static Position ToPosition(this TokenLocation location)
	{
		// STEP is 1-indexed; LSP is 0-indexed
		return new Position
		{
			Line = location.Line - 1,
			Character = location.Column - 1,
		};
	}
}
