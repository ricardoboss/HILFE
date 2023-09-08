using System.Diagnostics.CodeAnalysis;
using StepLang.Interpreting;
using StepLang.Parsing.Expressions;

namespace StepLang.Framework.Pure;

public class SubstringFunction : NativeFunction
{
    public const string Identifier = "substring";

    /// <inheritdoc />
    public override async Task<ExpressionResult> EvaluateAsync(Interpreter interpreter, IReadOnlyList<Expression> arguments, CancellationToken cancellationToken = default)
    {
        CheckArgumentCount(arguments);

        var (subjectExp, startExp, lengthExp) = (arguments[0], arguments[1], arguments[2]);

        var subject = await subjectExp.EvaluateAsync(interpreter, r => r.ExpectString().Value, cancellationToken);
        var start = await startExp.EvaluateAsync(interpreter, r => r.ExpectNumber().RoundedIntValue, cancellationToken);
        var length = await lengthExp.EvaluateAsync(interpreter, r => r.ExpectNumber().RoundedIntValue, cancellationToken);

        if (length <= 0)
            return StringResult.Empty;

        if (start < 0)
            start = subject.Length + start;

        if (start < 0 || start >= subject.Length)
            return StringResult.Empty;

        length = Math.Min(length, subject.Length - start);

        var substring = subject.Substring(start, length);

        return new StringResult(substring);
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    protected override string DebugParamsString => "string subject, number start, number length";
}