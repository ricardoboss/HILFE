using StepLang.Expressions;
using StepLang.Expressions.Results;
using StepLang.Interpreting;

namespace StepLang.Framework.Pure;

public class SqrtFunction : NativeFunction
{
    public const string Identifier = "sqrt";

    public override IEnumerable<(ResultType[] types, string identifier)> Parameters => new[] { (new[] { ResultType.Number }, "x") };

    public override async Task<ExpressionResult> EvaluateAsync(Interpreter interpreter, IReadOnlyList<Expression> arguments, CancellationToken cancellationToken = default)
    {
        CheckArgumentCount(arguments);

        var x = await arguments.Single().EvaluateAsync(interpreter, r => r.ExpectNumber().Value, cancellationToken);

        return new NumberResult(Math.Sqrt(x));
    }
}