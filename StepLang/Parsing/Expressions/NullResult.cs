namespace StepLang.Parsing.Expressions;

public class NullResult : ExpressionResult
{
    public static readonly NullResult Instance = new();

    /// <inheritdoc />
    private NullResult() : base(ResultType.Null)
    {
    }

    protected override bool EqualsInternal(ExpressionResult other)
    {
        return other is NullResult;
    }

    public override ExpressionResult DeepClone()
    {
        return Instance;
    }
}