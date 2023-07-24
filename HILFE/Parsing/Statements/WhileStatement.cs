using HILFE.Interpreting;
using HILFE.Parsing.Expressions;

namespace HILFE.Parsing.Statements;

public class WhileStatement : Statement
{
    private readonly Expression condition;
    private readonly IReadOnlyList<Statement> statements;

    /// <inheritdoc />
    public WhileStatement(Expression condition, IReadOnlyList<Statement> statements) : base(StatementType.WhileStatement)
    {
        this.condition = condition;
        this.statements = statements;
    }

    public async Task<bool> ShouldLoopAsync(Interpreter interpreter, CancellationToken cancellationToken = default)
    {
        var result = await condition.EvaluateAsync(interpreter, cancellationToken);

        return result is { ValueType: "bool", Value: true };
    }

    /// <inheritdoc />
    public override async Task ExecuteAsync(Interpreter interpreter, CancellationToken cancellationToken = default)
    {
        interpreter.PushScope();

        while (await ShouldLoopAsync(interpreter, cancellationToken))
            await interpreter.InterpretAsync(statements.ToAsyncEnumerable(), cancellationToken);

        interpreter.PopScope();
    }
    
    protected override string DebugRenderContent()
    {
        return $"{condition} {{ [{statements.Count} statements] }}";
    }
}