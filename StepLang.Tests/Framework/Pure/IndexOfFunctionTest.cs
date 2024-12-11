using StepLang.Expressions.Results;
using StepLang.Framework.Pure;
using System.Diagnostics.CodeAnalysis;

namespace StepLang.Tests.Framework.Pure;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes")]
public class IndexOfFunctionTest
{
	[Theory]
	[InlineData("", "", -1)]
	[InlineData("", "a", -1)]
	[InlineData("a", "", 0)]
	[InlineData("a", "a", 0)]
	[InlineData("a", "b", -1)]
	[InlineData("ab", "a", 0)]
	[InlineData("ab", "b", 1)]
	[InlineData("a🤷‍♂️b", "a", 0)]
	[InlineData("a🤷‍♂️b", "🤷‍♂️", 1)]
	[InlineData("a🤷‍♂️b", "b", 2)]
	public void TestIndexOfString(string subject, string value, int expected)
	{
		var result = IndexOfFunction.GetResult(new StringResult(subject), new StringResult(value));

		var numResult = Assert.IsType<NumberResult>(result);
		Assert.Equal(expected, numResult.Value);
	}
}
