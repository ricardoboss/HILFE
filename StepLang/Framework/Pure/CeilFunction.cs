using StepLang.Interpreting;
using StepLang.Parsing.Expressions;

namespace StepLang.Framework.Pure;

public class CeilFunction : NativeFunction
{
    public const string Identifier = "ceil";

    public override IEnumerable<(ResultType[] types, string identifier)> Parameters => new[] { (new[] { ResultType.Number }, "x") };

    public override async Task<ExpressionResult> EvaluateAsync(Interpreter interpreter, IReadOnlyList<Expression> arguments, CancellationToken cancellationToken = default)
    {
        CheckArgumentCount(arguments);

        var x = await arguments.Single().EvaluateAsync(interpreter, r => r.ExpectNumber().Value, cancellationToken);

        return new NumberResult(Math.Ceiling(x));
    }
}