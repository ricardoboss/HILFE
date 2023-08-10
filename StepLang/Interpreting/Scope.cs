using System.Diagnostics.CodeAnalysis;
using StepLang.Framework.Conversion;
using StepLang.Framework.IO;
using StepLang.Framework.Reflection;
using StepLang.Parsing.Expressions;
using StepLang.Tokenizing;

namespace StepLang.Interpreting;

public class Scope
{
    public static readonly Scope GlobalScope = new();

    private readonly Dictionary<string, Variable> identifiers = new();
    private readonly Scope? parentScope;

    public Scope(Scope parent) => parentScope = parent;

    private Scope()
    {
        parentScope = null;

        // globally defined identifiers
        SetVariable(PrintFunction.Identifier, new FunctionResult(new PrintFunction()));
        SetVariable(PrintlnFunction.Identifier, new FunctionResult(new PrintlnFunction()));
        SetVariable(ReadlineFunction.Identifier, new FunctionResult(new ReadlineFunction()));
        SetVariable(TypenameFunction.Identifier, new FunctionResult(new TypenameFunction()));
        SetVariable(ParseFunction.Identifier, new FunctionResult(new ParseFunction()));
        SetVariable(JsonEncodeFunction.Identifier, new FunctionResult(new JsonEncodeFunction()));
        SetVariable(JsonDecodeFunction.Identifier, new FunctionResult(new JsonDecodeFunction()));

        SetVariable("null", NullResult.Instance);

        SetVariable("EOL", new StringResult(Environment.NewLine));
    }

    public void SetVariable(string identifier, ExpressionResult value)
    {
        // only look for variable in the current scope for assigning
        // this enables use to shadow variables from parent scopes
        if (identifiers.TryGetValue(identifier, out var variable))
        {
            variable.Assign(value);
        }
        else
        {
            identifiers[identifier] = new(identifier, value);
        }
    }

    private bool TryGetVariable(string identifier, [NotNullWhen(true)] out Variable? variable)
    {
        if (identifiers.TryGetValue(identifier, out variable))
            return true;

        if (parentScope != null)
            return parentScope.TryGetVariable(identifier, out variable);

        variable = null;
        return false;
    }

    public Variable GetVariable(Token identifierToken)
    {
        if (TryGetVariable(identifierToken.Value, out var variable))
            return variable;

        throw new UndefinedIdentifierException(identifierToken);
    }

    public void UpdateValue(Token identifierToken, ExpressionResult value)
    {
        var variable = GetVariable(identifierToken);

        variable.Assign(value);
    }

    public void SetResult(ExpressionResult result) => SetVariable("$$RETURN", result);

    public bool TryGetResult([NotNullWhen(true)] out ExpressionResult? result)
    {
        result = null;
        if (!TryGetVariable("$$RETURN", out var resultVar))
            return false;

        result = resultVar.Value;
        return true;
    }

    public override string ToString()
    {
        return $"Scope: {{{string.Join(", ", identifiers.Select(kvp => kvp.Value.ToString()))}}}";
    }
}