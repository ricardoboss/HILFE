using StepLang.Interpreting;
using StepLang.Parsing.Expressions;

namespace StepLang.Framework.Pure;

public class ConvertedFunction : ListManipulationFunction
{
    public const string Identifier = "converted";

    protected override IAsyncEnumerable<ExpressionResult> EvaluateListManipulationAsync(Interpreter interpreter, IAsyncEnumerable<ConstantExpression[]> arguments,
        FunctionDefinition callback, CancellationToken cancellationToken = default)
    {
        return arguments.SelectAwait(async args => await callback.EvaluateAsync(interpreter, args, cancellationToken));
    }
}