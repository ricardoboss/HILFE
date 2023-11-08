using System.Diagnostics;
using StepLang.Tokenizing;

namespace StepLang.Parsing;

public class Parser
{
    private readonly TokenQueue tokens;

    public Parser(IEnumerable<Token> tokens)
    {
        this.tokens = new(tokens)
        {
            IgnoreWhitespace = true,
        };
    }

    public RootNode ParseRoot()
    {
        var imports = ParseImports();
        var statements = ParseStatements(TokenType.EndOfFile);

        return new(imports, statements);
    }

    private List<ImportNode> ParseImports()
    {
        var imports = new List<ImportNode>();
        while (tokens.PeekType() is TokenType.ImportKeyword)
            imports.Add(ParseImport());

        return imports;
    }

    private ImportNode ParseImport()
    {
        _ = tokens.Dequeue(TokenType.ImportKeyword);
        var path = tokens.Dequeue(TokenType.LiteralString);

        if (tokens.PeekType() is TokenType.NewLine)
            _ = tokens.Dequeue(TokenType.NewLine);

        return new(path);
    }

    private List<StatementNode> ParseStatements(TokenType stopTokenType)
    {
        var statements = new List<StatementNode>();
        TokenType nextType;
        while ((nextType = tokens.PeekType()) != stopTokenType)
        {
            if (nextType is TokenType.NewLine)
            {
                _ = tokens.Dequeue(TokenType.NewLine);

                continue;
            }

            statements.Add(ParseStatement());
        }

        return statements;
    }

    private StatementNode ParseStatement()
    {
        var token = tokens.Peek();
        if (token.Type == TokenType.TypeName)
        {
            var declaration = ParseVariableDeclaration();

            return new VariableDeclarationStatement(declaration);
        }

        if (token.Type == TokenType.Identifier)
        {
            var next = tokens.Peek(1);

            if (next.Type == TokenType.EqualsSymbol)
                return ParseVariableAssignment();

            if (next.Type == TokenType.OpeningParentheses)
                return ParseFunctionCall();

            if (next.Type == TokenType.OpeningSquareBracket)
                return ParseIndexAssignment();

            var nextNext = tokens.Peek(2);
            if (nextNext is { Type: var nextNextType })
            {
                if (next.Type.IsShorthandMathematicalOperation() && nextNextType == next.Type)
                    return ParseShorthandMathOperation();

                if (next.Type.IsShorthandMathematicalOperationWithAssignment())
                    return ParseShorthandMathOperationExpression();

                throw new UnexpectedTokenException(nextNext, next.Type, TokenType.EqualsSymbol);
            }

            throw new UnexpectedTokenException(next,
                new[]
                    {
                        TokenType.EqualsSymbol,
                        TokenType.OpeningParentheses,
                        TokenType.OpeningSquareBracket,
                    }.Concat(TokenTypes.ShorthandMathematicalOperations)
                    .Concat(TokenTypes.ShorthandMathematicalOperationsWithAssignment)
                    .Distinct()
                    .ToArray());
        }

        if (token.Type == TokenType.IfKeyword)
            return ParseIfStatement();

        if (token.Type == TokenType.WhileKeyword)
            return ParseWhileStatement();

        if (token.Type == TokenType.ForEachKeyword)
            return ParseForeachStatement();

        if (token.Type == TokenType.ReturnKeyword)
            return ParseReturnStatement();

        if (token.Type == TokenType.BreakKeyword)
            return ParseBreakStatement();

        if (token.Type == TokenType.ContinueKeyword)
            return ParseContinueStatement();

        if (token.Type == TokenType.OpeningCurlyBracket)
            return ParseCodeBlock();

        throw new UnexpectedTokenException(token, TokenType.TypeName, TokenType.Identifier, TokenType.NewLine, TokenType.LineComment);
    }

    private IdentifierIndexAssignmentNode ParseIndexAssignment()
    {
        var identifier = tokens.Dequeue(TokenType.Identifier);

        _ = tokens.Dequeue(TokenType.OpeningSquareBracket);

        var index = ParseExpression();

        _ = tokens.Dequeue(TokenType.ClosingSquareBracket);
        _ = tokens.Dequeue(TokenType.EqualsSymbol);

        var expression = ParseExpression();

        return new(identifier, index, expression);
    }

    private StatementNode ParseShorthandMathOperation()
    {
        var identifier = tokens.Dequeue(TokenType.Identifier);
        var operation = tokens.Dequeue(TokenTypes.ShorthandMathematicalOperations);
        _ = tokens.Dequeue(operation.Type);

        return operation.Type switch
        {
            TokenType.PlusSymbol => new IncrementStatementNode(identifier),
            TokenType.MinusSymbol => new DecrementStatementNode(identifier),
            _ => throw new UnexpectedTokenException(operation, TokenType.PlusSymbol, TokenType.MinusSymbol),
        };
    }

    private VariableAssignmentNode ParseShorthandMathOperationExpression()
    {
        var identifier = tokens.Dequeue(TokenType.Identifier);
        var identifierExpression = new IdentifierExpressionNode(identifier);

        var operatorTokens = PeekContinuousOperators(TokenTypes.ShorthandMathematicalOperationsWithAssignment);
        Debug.Assert(operatorTokens.Count > 0);

        _ = tokens.Dequeue(operatorTokens.Count);
        _ = tokens.Dequeue(TokenType.EqualsSymbol);

        var expression = ParseExpression();
        var firstOperator = operatorTokens[0];

        return firstOperator.Type switch
        {
            TokenType.PlusSymbol => new(identifier, new AddExpressionNode(firstOperator.Location, identifierExpression, expression)),
            TokenType.MinusSymbol => new(identifier, new SubtractExpressionNode(firstOperator.Location, identifierExpression, expression)),
            TokenType.AsteriskSymbol => operatorTokens.Count switch
            {
                1 => new(identifier, new MultiplyExpressionNode(firstOperator.Location, identifierExpression, expression)),
                2 => operatorTokens[1].Type switch
                {
                    TokenType.AsteriskSymbol => new(identifier, new PowerExpressionNode(firstOperator.Location, identifierExpression, expression)),
                    _ => throw new UnexpectedTokenException(operatorTokens[1], TokenType.AsteriskSymbol),
                },
                _ => throw new UnexpectedEndOfTokensException(firstOperator.Location, "Expected an operator"),
            },
            TokenType.SlashSymbol => new(identifier, new DivideExpressionNode(firstOperator.Location, identifierExpression, expression)),
            TokenType.PercentSymbol => new(identifier, new ModuloExpressionNode(firstOperator.Location, identifierExpression, expression)),
            TokenType.PipeSymbol => new(identifier, new BitwiseOrExpressionNode(firstOperator.Location, identifierExpression, expression)),
            TokenType.AmpersandSymbol => new(identifier, new BitwiseAndExpressionNode(firstOperator.Location, identifierExpression, expression)),
            TokenType.HatSymbol => new(identifier, new BitwiseXorExpressionNode(firstOperator.Location, identifierExpression, expression)),
            TokenType.QuestionMarkSymbol => new(identifier, new CoalesceExpressionNode(firstOperator.Location, identifierExpression, expression)),
            _ => throw new UnexpectedTokenException(firstOperator, TokenType.PlusSymbol, TokenType.MinusSymbol, TokenType.AsteriskSymbol, TokenType.SlashSymbol, TokenType.PercentSymbol, TokenType.PipeSymbol, TokenType.AmpersandSymbol, TokenType.HatSymbol, TokenType.QuestionMarkSymbol),
        };
    }

    private StatementNode ParseForeachStatement()
    {
        var foreachKeyword = tokens.Dequeue(TokenType.ForEachKeyword);
        _ = tokens.Dequeue(TokenType.OpeningParentheses);

        IVariableDeclarationNode? keyDeclaration = null;
        Token? keyIdentifier = null;
        IVariableDeclarationNode? valueDeclaration = null;
        Token? valueIdentifier = null;

        var next = tokens.Peek();
        if (next.Type == TokenType.TypeName)
        {
            var firstDeclaration = ParseVariableDeclaration();

            next = tokens.Peek();
            if (next.Type == TokenType.ColonSymbol)
            {
                keyDeclaration = firstDeclaration;

                _ = tokens.Dequeue(TokenType.ColonSymbol);

                next = tokens.Peek();
                if (next.Type == TokenType.TypeName)
                    valueDeclaration = ParseVariableDeclaration();
                else if (next.Type == TokenType.Identifier)
                    valueIdentifier = tokens.Dequeue(TokenType.Identifier);
                else
                    throw new UnexpectedTokenException(next, TokenType.TypeName, TokenType.Identifier);
            }
            else if (next.Type == TokenType.InKeyword)
                valueDeclaration = firstDeclaration;
            else
                throw new UnexpectedTokenException(next, TokenType.ColonSymbol, TokenType.InKeyword);
        }
        else if (next.Type == TokenType.Identifier)
        {
            var firstIdentifier = tokens.Dequeue(TokenType.Identifier);

            next = tokens.Peek();
            if (next.Type == TokenType.ColonSymbol)
            {
                keyIdentifier = firstIdentifier;

                _ = tokens.Dequeue(TokenType.ColonSymbol);

                next = tokens.Peek();
                if (next.Type == TokenType.TypeName)
                    valueDeclaration = ParseVariableDeclaration();
                else if (next.Type == TokenType.Identifier)
                    valueIdentifier = tokens.Dequeue(TokenType.Identifier);
                else
                    throw new UnexpectedTokenException(next, TokenType.TypeName, TokenType.Identifier);
            }
            else if (next.Type == TokenType.InKeyword)
                valueIdentifier = firstIdentifier;
            else
                throw new UnexpectedTokenException(next, TokenType.ColonSymbol, TokenType.InKeyword);
        }
        else
            throw new UnexpectedTokenException(next, TokenType.TypeName, TokenType.Identifier);

        if (valueDeclaration is null && valueIdentifier is null)
            throw new UnexpectedTokenException(tokens.Peek(), "Foreach without value declaration or identifier");

        _ = tokens.Dequeue(TokenType.InKeyword);

        var list = ParseExpression();

        _ = tokens.Dequeue(TokenType.ClosingParentheses);

        var body = ParseCodeBlock().Body;

        if (keyDeclaration is not null)
        {
            if (valueDeclaration is not null)
                return new ForeachDeclareKeyDeclareValueStatementNode(foreachKeyword, keyDeclaration, valueDeclaration, list, body);

            return new ForeachDeclareKeyValueStatementNode(foreachKeyword, keyDeclaration, valueIdentifier!, list, body);
        }

        if (keyIdentifier is not null)
        {
            if (valueDeclaration is not null)
                return new ForeachKeyDeclareValueStatementNode(foreachKeyword, keyIdentifier, valueDeclaration, list, body);

            return new ForeachKeyValueStatementNode(foreachKeyword, keyIdentifier, valueIdentifier!, list, body);
        }

        if (valueDeclaration is not null)
            return new ForeachDeclareValueStatementNode(foreachKeyword, valueDeclaration, list, body);

        return new ForeachValueStatementNode(foreachKeyword, valueIdentifier!, list, body);
    }

    private CodeBlockStatementNode ParseCodeBlock()
    {
        var openCurlyBrace = tokens.Dequeue(TokenType.OpeningCurlyBracket);

        var statements = ParseStatements(TokenType.ClosingCurlyBracket);

        _ = tokens.Dequeue(TokenType.ClosingCurlyBracket);

        return new(openCurlyBrace, statements);
    }

    private ContinueStatementNode ParseContinueStatement()
    {
        var continueToken = tokens.Dequeue(TokenType.ContinueKeyword);

        return new(continueToken);
    }

    private BreakStatementNode ParseBreakStatement()
    {
        var breakToken = tokens.Dequeue(TokenType.BreakKeyword);

        return new(breakToken);
    }

    private StatementNode ParseReturnStatement()
    {
        var returnKeyword = tokens.Dequeue(TokenType.ReturnKeyword);

        if (tokens.PeekType() is TokenType.NewLine)
            return new ReturnStatementNode(returnKeyword);

        var expression = ParseExpression();

        return new ReturnExpressionStatementNode(returnKeyword, expression);
    }

    private WhileStatementNode ParseWhileStatement()
    {
        var whileKeyword = tokens.Dequeue(TokenType.WhileKeyword);

        _ = tokens.Dequeue(TokenType.OpeningParentheses);

        var condition = ParseExpression();

        _ = tokens.Dequeue(TokenType.ClosingParentheses);

        var codeBlock = ParseCodeBlock();

        return new(whileKeyword, condition, codeBlock.Body);
    }

    private StatementNode ParseIfStatement()
    {
        var ifKeyword = tokens.Dequeue(TokenType.IfKeyword);
        _ = tokens.Dequeue(TokenType.OpeningParentheses);
        var condition = ParseExpression();
        _ = tokens.Dequeue(TokenType.ClosingParentheses);
        var codeBlock = ParseCodeBlock();

        if (tokens.PeekType() is TokenType.ElseKeyword)
        {
            _ = tokens.Dequeue(TokenType.ElseKeyword);
            if (tokens.PeekType() is TokenType.IfKeyword)
            {
                _ = tokens.Dequeue(TokenType.IfKeyword);
                _ = tokens.Dequeue(TokenType.OpeningParentheses);
                var elseCondition = ParseExpression();
                _ = tokens.Dequeue(TokenType.ClosingParentheses);
                var elseIfCodeBlock = ParseCodeBlock();
                return new IfElseIfStatementNode(ifKeyword, condition, codeBlock.Body, elseCondition, elseIfCodeBlock.Body);
            }

            var elseCodeBlock = ParseCodeBlock();
            return new IfElseStatementNode(ifKeyword, condition, codeBlock.Body, elseCodeBlock.Body);
        }

        return new IfStatementNode(ifKeyword, condition, codeBlock.Body);
    }

    private CallStatementNode ParseFunctionCall()
    {
        var callExpression = ParseFunctionCallExpression();

        return new(callExpression);
    }

    private IVariableDeclarationNode ParseVariableDeclaration()
    {
        var type = tokens.Dequeue(TokenType.TypeName);

        Token? nullabilityIndicator = null;
        if (tokens.Peek() is { Type: TokenType.QuestionMarkSymbol })
            nullabilityIndicator = tokens.Dequeue(TokenType.QuestionMarkSymbol);

        var identifier = tokens.Dequeue(TokenType.Identifier);

        if (tokens.PeekType() is not TokenType.EqualsSymbol)
        {
            if (nullabilityIndicator is not null)
                return new NullableVariableDeclarationNode(new[] { type }, nullabilityIndicator, identifier);

            return new VariableDeclarationNode(new[] { type }, identifier);
        }

        _ = tokens.Dequeue(TokenType.EqualsSymbol);

        var expression = ParseExpression();

        if (nullabilityIndicator is not null)
            return new NullableVariableInitializationNode(new[] { type }, nullabilityIndicator, identifier, expression);

        return new VariableInitializationNode(new[] { type }, identifier, expression);
    }

    private VariableAssignmentNode ParseVariableAssignment()
    {
        var identifier = tokens.Dequeue(TokenType.Identifier);
        _ = tokens.Dequeue(TokenType.EqualsSymbol);
        var expression = ParseExpression();
        return new(identifier, expression);
    }

    private ExpressionNode ParseExpression(int parentPrecedence = 0)
    {
        var left = ParsePrimaryExpression();

        while (tokens.PeekType() is TokenType.OpeningSquareBracket)
        {
            _ = tokens.Dequeue(TokenType.OpeningSquareBracket);

            var index = ParseExpression();

            _ = tokens.Dequeue(TokenType.ClosingSquareBracket);

            left = new IndexAccessExpressionNode(left, index);
        }

        while (tokens.PeekType().IsOperator())
        {
            var operatorTokens = PeekContinuousOperators(TokenTypes.Operators);
            var binaryOperator = ParseExpressionOperator(operatorTokens);

            var precedence = binaryOperator.Precedence();
            if (precedence < parentPrecedence)
                break;

            // actually consume the operator
            _ = tokens.Dequeue(operatorTokens.Count);

            var right = ParseExpression(precedence + 1);

            left = Combine(operatorTokens.First().Location, left, binaryOperator, right);
        }

        return left;
    }

    private static ExpressionNode Combine(TokenLocation operatorLocation, ExpressionNode left, BinaryExpressionOperator binaryOperator, ExpressionNode right)
    {
        return binaryOperator switch
        {
            BinaryExpressionOperator.Add => new AddExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.Coalesce => new CoalesceExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.NotEqual => new NotEqualsExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.Equal => new EqualsExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.Subtract => new SubtractExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.Multiply => new MultiplyExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.Divide => new DivideExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.Modulo => new ModuloExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.Power => new PowerExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.GreaterThan => new GreaterThanExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.LessThan => new LessThanExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.GreaterThanOrEqual => new GreaterThanOrEqualExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.LessThanOrEqual => new LessThanOrEqualExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.LogicalAnd => new LogicalAndExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.LogicalOr => new LogicalOrExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.BitwiseAnd => new BitwiseAndExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.BitwiseOr => new BitwiseOrExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.BitwiseXor => new BitwiseXorExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.BitwiseShiftLeft => new BitwiseShiftLeftExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.BitwiseShiftRight => new BitwiseShiftRightExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.BitwiseRotateLeft => new BitwiseRotateLeftExpressionNode(operatorLocation, left, right),
            BinaryExpressionOperator.BitwiseRotateRight => new BitwiseRotateRightExpressionNode(operatorLocation, left, right),
            _ => throw new NotImplementedException("Expression for operator " + binaryOperator.ToSymbol() + " not implemented"),
        };
    }

    private List<Token> PeekContinuousOperators(TokenType[] allowedOperators)
    {
        // whitespaces have meaning in operators
        tokens.IgnoreWhitespace = false;

        var offset = 0;
        var operators = new List<Token>();
        Token next;
        while ((next = tokens.Peek(offset)).Type is not TokenType.EndOfFile)
        {
            if (allowedOperators.Contains(next.Type))
                operators.Add(tokens.Peek(offset++));
            else if (operators.Count == 0)
            {
                if (next.Type is not TokenType.Whitespace)
                    throw new UnexpectedTokenException(next, allowedOperators);

                offset++; // skip leading whitespace
            }
            else
                break;
        }

        tokens.IgnoreWhitespace = true;

        return operators;
    }

    private BinaryExpressionOperator ParseExpressionOperator(IReadOnlyList<Token> operatorTokens)
    {
        if (operatorTokens.Count == 3)
        {
            var (first, second, third) = (operatorTokens[0], operatorTokens[1], operatorTokens[2]);

            return first.Type switch
            {
                TokenType.GreaterThanSymbol => second.Type switch
                {
                    TokenType.GreaterThanSymbol => third.Type switch
                    {
                        TokenType.GreaterThanSymbol => BinaryExpressionOperator.BitwiseRotateRight,
                        _ => throw new UnexpectedTokenException(third, TokenType.GreaterThanSymbol),
                    },
                    _ => throw new UnexpectedTokenException(second, TokenType.GreaterThanSymbol),
                },
                TokenType.LessThanSymbol => second.Type switch
                {
                    TokenType.LessThanSymbol => third.Type switch
                    {
                        TokenType.LessThanSymbol => BinaryExpressionOperator.BitwiseRotateLeft,
                        _ => throw new UnexpectedTokenException(third, TokenType.LessThanSymbol),
                    },
                    _ => throw new UnexpectedTokenException(second, TokenType.LessThanSymbol),
                },
                _ => throw new UnexpectedTokenException(first, TokenType.GreaterThanSymbol, TokenType.LessThanSymbol),
            };
        }

        if (operatorTokens.Count == 2)
        {
            var (first, second) = (operatorTokens[0], operatorTokens[1]);

            return first.Type switch
            {
                TokenType.AsteriskSymbol => second.Type switch
                {
                    TokenType.AsteriskSymbol => BinaryExpressionOperator.Power,
                    _ => throw new UnexpectedTokenException(second, TokenType.AsteriskSymbol),
                },
                TokenType.EqualsSymbol => second.Type switch
                {
                    TokenType.EqualsSymbol => BinaryExpressionOperator.Equal,
                    _ => throw new UnexpectedTokenException(second, TokenType.EqualsSymbol),
                },
                TokenType.ExclamationMarkSymbol => second.Type switch
                {
                    TokenType.EqualsSymbol => BinaryExpressionOperator.NotEqual,
                    _ => throw new UnexpectedTokenException(second, TokenType.EqualsSymbol),
                },
                TokenType.AmpersandSymbol => second.Type switch
                {
                    TokenType.AmpersandSymbol => BinaryExpressionOperator.LogicalAnd,
                    _ => throw new UnexpectedTokenException(second, TokenType.AmpersandSymbol),
                },
                TokenType.PipeSymbol => second.Type switch
                {
                    TokenType.PipeSymbol => BinaryExpressionOperator.LogicalOr,
                    _ => throw new UnexpectedTokenException(second, TokenType.PipeSymbol),
                },
                TokenType.LessThanSymbol => second.Type switch
                {
                    TokenType.EqualsSymbol => BinaryExpressionOperator.LessThanOrEqual,
                    TokenType.LessThanSymbol => BinaryExpressionOperator.BitwiseShiftLeft,
                    _ => throw new UnexpectedTokenException(second, TokenType.EqualsSymbol, TokenType.LessThanSymbol),
                },
                TokenType.GreaterThanSymbol => second.Type switch
                {
                    TokenType.EqualsSymbol => BinaryExpressionOperator.GreaterThanOrEqual,
                    TokenType.GreaterThanSymbol => BinaryExpressionOperator.BitwiseShiftRight,
                    _ => throw new UnexpectedTokenException(second, TokenType.EqualsSymbol,
                        TokenType.GreaterThanSymbol),
                },
                TokenType.QuestionMarkSymbol => second.Type switch
                {
                    TokenType.QuestionMarkSymbol => BinaryExpressionOperator.Coalesce,
                    _ => throw new UnexpectedTokenException(second, TokenType.QuestionMarkSymbol),
                },
                _ => throw new UnexpectedTokenException(first, TokenType.AsteriskSymbol, TokenType.EqualsSymbol,
                    TokenType.ExclamationMarkSymbol, TokenType.AmpersandSymbol, TokenType.PipeSymbol,
                    TokenType.LessThanSymbol, TokenType.GreaterThanSymbol, TokenType.QuestionMarkSymbol),
            };
        }

        if (operatorTokens.Count == 1)
        {
            var first = operatorTokens[0];

            return first.Type switch
            {
                TokenType.PlusSymbol => BinaryExpressionOperator.Add,
                TokenType.MinusSymbol => BinaryExpressionOperator.Subtract,
                TokenType.AsteriskSymbol => BinaryExpressionOperator.Multiply,
                TokenType.SlashSymbol => BinaryExpressionOperator.Divide,
                TokenType.PercentSymbol => BinaryExpressionOperator.Modulo,
                TokenType.GreaterThanSymbol => BinaryExpressionOperator.GreaterThan,
                TokenType.LessThanSymbol => BinaryExpressionOperator.LessThan,
                TokenType.PipeSymbol => BinaryExpressionOperator.BitwiseOr,
                TokenType.AmpersandSymbol => BinaryExpressionOperator.BitwiseAnd,
                TokenType.HatSymbol => BinaryExpressionOperator.BitwiseXor,
                _ => throw new UnexpectedTokenException(first, TokenType.PlusSymbol, TokenType.MinusSymbol,
                    TokenType.AsteriskSymbol, TokenType.SlashSymbol, TokenType.PercentSymbol,
                    TokenType.GreaterThanSymbol, TokenType.LessThanSymbol, TokenType.PipeSymbol,
                    TokenType.AmpersandSymbol, TokenType.HatSymbol),
            };
        }

        if (operatorTokens.Count == 0)
            throw new UnexpectedEndOfTokensException(tokens.LastToken?.Location, "Expected an operator");

        throw new UnexpectedTokenException(operatorTokens[0], "Operators can only be chained up to 3 times");
    }

    private ExpressionNode ParsePrimaryExpression()
    {
        var tokenType = tokens.PeekType();
        if (tokenType.IsLiteral())
            return new LiteralExpressionNode(tokens.Dequeue(tokenType));

        switch (tokenType)
        {
            case TokenType.OpeningParentheses:
                {
                    var nextType = tokens.PeekType(1);
                    return nextType switch
                    {
                        TokenType.TypeName => ParseFunctionDefinitionExpression(),
                        _ => ParseNestedExpression(),
                    };
                }
            case TokenType.Identifier:
                {
                    var nextType = tokens.PeekType(1);
                    return nextType switch
                    {
                        TokenType.OpeningParentheses => ParseFunctionCallExpression(),
                        _ => ParseIdentifierExpression(),
                    };
                }
            case TokenType.OpeningSquareBracket:
                return ParseListExpression();
            case TokenType.OpeningCurlyBracket:
                return ParseMapExpression();
            case TokenType.MinusSymbol:
                return ParseNegateExpression();
            case TokenType.ExclamationMarkSymbol:
                return ParseNotExpression();
            case TokenType.TypeName when tokens.Peek().Value.ToLowerInvariant() == "null":
                var nullToken = tokens.Dequeue(TokenType.TypeName);

                return new LiteralExpressionNode(new(TokenType.LiteralNull, nullToken.Value, nullToken.Location));
            default:
                throw new MissingExpressionException(tokens.LastToken ?? tokens.Peek());
        }
    }

    private ExpressionNode ParseNotExpression()
    {
        var exclamationMark = tokens.Dequeue(TokenType.ExclamationMarkSymbol);

        var expression = ParseExpression();

        return new NotExpressionNode(exclamationMark, expression);
    }

    private ExpressionNode ParseNegateExpression()
    {
        var minus = tokens.Dequeue(TokenType.MinusSymbol);

        var expression = ParseExpression();

        return new NegateExpressionNode(minus, expression);
    }

    private ExpressionNode ParseMapExpression()
    {
        var openCurlyBrace = tokens.Dequeue(TokenType.OpeningCurlyBracket);

        var map = new Dictionary<Token, ExpressionNode>();

        while (tokens.PeekType() is not TokenType.ClosingCurlyBracket)
        {
            var key = tokens.Dequeue(TokenType.LiteralString);

            _ = tokens.Dequeue(TokenType.ColonSymbol);

            var value = ParseExpression();

            map.Add(key, value);

            if (tokens.PeekType() is TokenType.CommaSymbol)
                _ = tokens.Dequeue(TokenType.CommaSymbol);
        }

        _ = tokens.Dequeue(TokenType.ClosingCurlyBracket);

        return new MapExpressionNode(openCurlyBrace, map);
    }

    private ExpressionNode ParseListExpression()
    {
        var openSquareBracket = tokens.Dequeue(TokenType.OpeningSquareBracket);

        var list = new List<ExpressionNode>();

        while (tokens.PeekType() is not TokenType.ClosingSquareBracket)
        {
            list.Add(ParseExpression());

            if (tokens.PeekType() is TokenType.CommaSymbol)
                _ = tokens.Dequeue(TokenType.CommaSymbol);
        }

        _ = tokens.Dequeue(TokenType.ClosingSquareBracket);

        return new ListExpressionNode(openSquareBracket, list);
    }

    private ExpressionNode ParseIdentifierExpression()
    {
        var identifier = tokens.Dequeue(TokenType.Identifier);

        return new IdentifierExpressionNode(identifier);
    }

    private CallExpressionNode ParseFunctionCallExpression()
    {
        var identifier = tokens.Dequeue(TokenType.Identifier);

        _ = tokens.Dequeue(TokenType.OpeningParentheses);

        var arguments = ParseCallArguments();

        _ = tokens.Dequeue(TokenType.ClosingParentheses);

        return new(identifier, arguments);
    }

    private ExpressionNode ParseNestedExpression()
    {
        _ = tokens.Dequeue(TokenType.OpeningParentheses);

        var expression = ParseExpression();

        _ = tokens.Dequeue(TokenType.ClosingParentheses);

        return expression;
    }

    private ExpressionNode ParseFunctionDefinitionExpression()
    {
        var openParenthesis = tokens.Dequeue(TokenType.OpeningParentheses);

        var parameters = new List<IVariableDeclarationNode>();
        while (tokens.PeekType() is not TokenType.ClosingParentheses)
        {
            var declaration = ParseVariableDeclaration();

            parameters.Add(declaration);

            if (tokens.PeekType() is TokenType.CommaSymbol)
                _ = tokens.Dequeue(TokenType.CommaSymbol);
        }

        _ = tokens.Dequeue(TokenType.ClosingParentheses);

        var body = ParseCodeBlock().Body;

        if (tokens.PeekType() is not TokenType.OpeningParentheses)
            return new FunctionDefinitionExpressionNode(openParenthesis, parameters, body);

        // direct definition call
        _ = tokens.Dequeue(TokenType.OpeningParentheses);

        var callArguments = ParseCallArguments();

        _ = tokens.Dequeue(TokenType.ClosingParentheses);

        return new FunctionDefinitionCallExpressionNode(openParenthesis, parameters, body, callArguments);
    }

    private List<ExpressionNode> ParseCallArguments()
    {
        var arguments = new List<ExpressionNode>();

        while (tokens.PeekType() is not TokenType.ClosingParentheses)
        {
            var argumentExpression = ParseExpression();

            arguments.Add(argumentExpression);

            if (tokens.PeekType() is TokenType.CommaSymbol)
                _ = tokens.Dequeue();
            else
                break;
        }

        return arguments;
    }
}