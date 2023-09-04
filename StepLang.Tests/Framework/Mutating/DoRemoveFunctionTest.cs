using System.Diagnostics.CodeAnalysis;
using StepLang.Framework.Mutating;
using StepLang.Interpreting;
using StepLang.Parsing.Expressions;
using StepLang.Tokenizing;

namespace StepLang.Tests.Framework.Mutating;

public class DoRemoveFunctionTest
{
    [Theory]
    [ClassData(typeof(DoRemoveData))]
    public async Task TestDoRemove(ListResult list, Expression elementExpression, ListResult resultingList)
    {
        const string listIdentifier = "mylist";

        var interpreter = new Interpreter();
        interpreter.CurrentScope.SetVariable(listIdentifier, list);
        var listVarExpression = new VariableExpression(new(TokenType.Identifier, listIdentifier, null));
        var function = new DoRemoveFunction();

        var result = await function.EvaluateAsync(interpreter, new[] { listVarExpression, elementExpression });

        Assert.Equal(VoidResult.Instance, result);
        Assert.Equal(resultingList.Value.Count, list.Value.Count);
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used by xUnit")]
    private sealed class DoRemoveData : TheoryData<ListResult, Expression, ListResult>
    {
        public DoRemoveData()
        {
            Add(ListResult.Empty, ConstantExpression.Str("test"), ListResult.Empty);
            Add(ListResult.Empty, ConstantExpression.Number(1), ListResult.Empty);
            Add(ListResult.Empty, ConstantExpression.True, ListResult.Empty);
            Add(ListResult.Empty, Expression.Add(ConstantExpression.Number(1), ConstantExpression.Number(2)), ListResult.Empty);
            Add(ListResult.From(BoolResult.True), ConstantExpression.False, ListResult.From(BoolResult.True));
            Add(ListResult.From(BoolResult.False), ConstantExpression.False, ListResult.Empty);
        }
    }
}