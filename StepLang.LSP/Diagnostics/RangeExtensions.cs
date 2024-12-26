using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using StepLang.Tokenizing;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace StepLang.LSP.Diagnostics;

public static class RangeExtensions
{
	public static Range ToRange(this TokenLocation location)
	{
		var start = new Position
		{
			Line = location.Line + 1,
			Character = location.Column + 1,
		};

		var end = new Position
		{
			Line = start.Line,
			Character = start.Character + location.Length,
		};

		return new Range(start, end);
	}
}
