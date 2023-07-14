namespace HILFE.Statements;

public class FunctionCallStatement : BaseStatement
{
    /// <inheritdoc />
    public FunctionCallStatement(IReadOnlyList<Token> tokens) : base(StatementType.FunctionCall, tokens)
    {
    }

    /// <inheritdoc />
    public override Task ExecuteAsync(Interpreter interpreter)
    {
        var meaningfulTokens = Tokens.OnlyMeaningful().ToArray();

        var identifierToken = meaningfulTokens[0];
        if (identifierToken.Type != TokenType.Identifier)
            throw new TokenizerException($"Expected {TokenType.Identifier} token, but got {identifierToken.Type} instead");

        var identifier = identifierToken.Value;
        
        var expressionOpener = meaningfulTokens[1];
        if (expressionOpener.Type != TokenType.ExpressionOpener)
            throw new TokenizerException($"Expected {TokenType.ExpressionOpener} token, but got {expressionOpener.Type} instead");

        var expressions = new List<Expression>();
        var endOffset = meaningfulTokens.Reverse().TakeWhile(t => t.Type != TokenType.ExpressionCloser).Count();
        for (var i = 2; i < meaningfulTokens.Length - endOffset; )
        {
            var expressionTokens = meaningfulTokens.Skip(i).TakeWhile(t => t.Type != TokenType.ExpressionSeparator && t.Type != TokenType.ExpressionCloser).ToList();
            var expression = new Expression(expressionTokens);
            expressions.Add(expression);
            i += expressionTokens.Count + 1;
        }

        var args = expressions.Select(e => e.Evaluate(interpreter)).ToList();

        var functionVariable = interpreter.Scope.CurrentScope.GetByIdentifier(identifier);
        var functionDefiniton = functionVariable.Value as string;
        switch (functionDefiniton)
        {
            case "StdIn.ReadLine":
                var line = interpreter.StdIn.ReadLine();
                interpreter.Scope.CurrentScope.ParentScope?.AddIdentifier("$$RETURN", new("$$RETURN", "string?", line));
                break;
            case "StdOut.WriteLine":
                var stringArgs = args.Select(a => a?.ToString() ?? string.Empty).Cast<string>().ToList();
                interpreter.StdOut.WriteLine(string.Join("", stringArgs));
                break;
            default:
                throw new InterpreterException("Undefined function: " + functionDefiniton);
        }

        return Task.CompletedTask;
    }
}