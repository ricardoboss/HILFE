using StepLang.Expressions.Results;
using StepLang.Interpreting;
using StepLang.Tokenizing;

namespace StepLang.Framework.Pure;

public class SinFunction : GenericFunction<NumberResult>
{
	public const string Identifier = "sin";

	protected override IEnumerable<NativeParameter> NativeParameters { get; } =
		[new(OnlyNumber, "x")];

	protected override IEnumerable<ResultType> ReturnTypes { get; } = OnlyNumber;

	protected override NumberResult Invoke(TokenLocation callLocation, Interpreter interpreter, NumberResult argument1)
	{
		return Math.Sin(argument1);
	}
}
