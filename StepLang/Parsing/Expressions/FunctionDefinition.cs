using System.Diagnostics.CodeAnalysis;
using StepLang.Interpreting;

namespace StepLang.Parsing.Expressions;

public abstract class FunctionDefinition
{
    public abstract Task<ExpressionResult> EvaluateAsync(Interpreter interpreter, IReadOnlyList<Expression> arguments, CancellationToken cancellationToken = default);

    protected abstract string DebugParamsString { get; }

    protected abstract string DebugBodyString { get; }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        var paramStr = DebugParamsString;
        var bodyStr = DebugBodyString;

        return $"({paramStr}) => {{ {bodyStr} }}>";
    }
}