namespace StepLang.Parsing.Expressions;

public class ListResult : ValueExpressionResult<IList<ExpressionResult>>
{
    public static readonly ListResult Empty = new(new List<ExpressionResult>());

    /// <inheritdoc />
    public ListResult(IList<ExpressionResult> value) : base(ResultType.List, value)
    {
    }
}