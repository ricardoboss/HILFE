using StepLang.Interpreting;
using StepLang.Parsing;
using StepLang.Parsing.Expressions;
using StepLang.Tokenizing;

namespace StepLang.Tests.Parsing;

public class ExpressionParserTest
{
    [Fact]
    public async Task TestParseVariableExpression()
    {
        var interpreter = new Interpreter();
        interpreter.CurrentScope.SetVariable("variable", BoolResult.True);
        var expression = await ExpressionParser.ParseAsync(new[] { new Token(TokenType.Identifier, "variable", null) });

        var result = await expression.EvaluateAsync(interpreter);

        Assert.True(result is BoolResult { Value: true });
    }

    [Fact]
    public async Task TestParseSimpleAddition()
    {
        var interpreter = new Interpreter();
        var expression = await ExpressionParser.ParseAsync(new[] { new Token(TokenType.LiteralNumber, "123", null), new Token(TokenType.PlusSymbol, "+", null), new Token(TokenType.LiteralNumber, "456", null) });

        var result = await expression.EvaluateAsync(interpreter);

        Assert.True(result is NumberResult { Value: 579 });
    }

    [Fact]
    public async Task TestParseAdditionWithMultipleSummands()
    {
        var interpreter = new Interpreter();
        var expression = await ExpressionParser.ParseAsync(new[] { new Token(TokenType.LiteralNumber, "1", null), new Token(TokenType.PlusSymbol, "+", null), new Token(TokenType.LiteralNumber, "2", null), new Token(TokenType.PlusSymbol, "+", null), new Token(TokenType.LiteralNumber, "3", null), new Token(TokenType.PlusSymbol, "+", null), new Token(TokenType.LiteralNumber, "4", null) });

        var result = await expression.EvaluateAsync(interpreter);

        Assert.True(result is NumberResult { Value: 10 });
    }

    [Fact]
    public async Task TestMultiplicativeAdditivePrecedences()
    {
        var interpreter = new Interpreter();
        var expression = await ExpressionParser.ParseAsync(new[] { new Token(TokenType.LiteralNumber, "1", null), new Token(TokenType.AsteriskSymbol, "*", null), new Token(TokenType.LiteralNumber, "2", null), new Token(TokenType.PlusSymbol, "+", null), new Token(TokenType.LiteralNumber, "3", null) });

        var result = await expression.EvaluateAsync(interpreter);

        Assert.True(result is NumberResult { Value: 5 });
    }

    [Fact]
    public async Task TestAdditiveMultiplicativePrecedences()
    {
        var interpreter = new Interpreter();
        var expression = await ExpressionParser.ParseAsync(new[] { new Token(TokenType.LiteralNumber, "1", null), new Token(TokenType.PlusSymbol, "+", null), new Token(TokenType.LiteralNumber, "2", null), new Token(TokenType.AsteriskSymbol, "*", null), new Token(TokenType.LiteralNumber, "3", null) });

        var result = await expression.EvaluateAsync(interpreter);

        Assert.True(result is NumberResult { Value: 7 });
    }

    [Fact]
    public async Task TestAdditiveMultiplicativePrecedencesWithParentheses()
    {
        var interpreter = new Interpreter();
        var expression = await ExpressionParser.ParseAsync(new[] { new Token(TokenType.OpeningParentheses, "(", null), new Token(TokenType.LiteralNumber, "1", null), new Token(TokenType.PlusSymbol, "+", null), new Token(TokenType.LiteralNumber, "2", null), new Token(TokenType.ClosingParentheses, ")", null), new Token(TokenType.AsteriskSymbol, "*", null), new Token(TokenType.LiteralNumber, "3", null) });

        var result = await expression.EvaluateAsync(interpreter);

        Assert.True(result is NumberResult { Value: 9 });
    }

    [Fact]
    public async Task TestParseThrowsUnexpectedEndOfTokensExceptionForEmptyExpression()
    {
        await Assert.ThrowsAsync<UnexpectedEndOfTokensException>(() => ExpressionParser.ParseAsync(Array.Empty<Token>()));
    }

    [Fact]
    public async Task TestParseThrowsUnexpectedEndOfTokensExceptionForMissingRightOperand()
    {
        await Assert.ThrowsAsync<UnexpectedEndOfTokensException>(() => ExpressionParser.ParseAsync(new[] { new Token(TokenType.LiteralNumber, "1", null), new Token(TokenType.PlusSymbol, "+", null) }));
    }
}