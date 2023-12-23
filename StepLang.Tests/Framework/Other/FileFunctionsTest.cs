using System.Runtime.InteropServices;
using StepLang.Expressions.Results;
using StepLang.Framework.Other;
using StepLang.Interpreting;
using StepLang.Parsing;

namespace StepLang.Tests.Framework.Other;

public class FileFunctionsTest
{
    [SkippableTheory]
    [InlineData("Windows", @"C:\temp\test.txt")]
    [InlineData("Linux", "/tmp/test.txt")]
    [InlineData("OSX", "/tmp/test.txt")]
    public void TestFileFunctions(string platform, string filename)
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Create(platform)), $"Test only for {platform}");

        const string content = "Hello World";

        var interpreter = new Interpreter();

        var fileExistsFunction = new FileExistsFunction();
        var fileWriteFunction = new FileWriteFunction();
        var fileReadFunction = new FileReadFunction();
        var fileDeleteFunction = new FileDeleteFunction();

        var preWriteExistsResult = fileExistsFunction.Invoke(new(), interpreter, new List<ExpressionNode> { LiteralExpressionNode.FromString(filename) });

        var preWriteExistsBoolResult = Assert.IsType<BoolResult>(preWriteExistsResult);
        Assert.False(preWriteExistsBoolResult.Value);

        var writeArguments = new List<ExpressionNode> { LiteralExpressionNode.FromString(filename), LiteralExpressionNode.FromString(content) };
        var writeResult = fileWriteFunction.Invoke(new(), interpreter, writeArguments);

        var writeBoolResult = Assert.IsType<BoolResult>(writeResult);
        Assert.True(writeBoolResult.Value);

        var postWriteExistsResult = fileExistsFunction.Invoke(new(), interpreter, new List<ExpressionNode> { LiteralExpressionNode.FromString(filename) });

        var postWriteExistsBoolResult = Assert.IsType<BoolResult>(postWriteExistsResult);
        Assert.True(postWriteExistsBoolResult.Value);

        var firstReadResult = fileReadFunction.Invoke(new(), interpreter, new List<ExpressionNode> { LiteralExpressionNode.FromString(filename) });

        var firstReadStringResult = Assert.IsType<StringResult>(firstReadResult);
        Assert.Equal(content, firstReadStringResult.Value);

        var appendArguments = new List<ExpressionNode> { LiteralExpressionNode.FromString(filename), LiteralExpressionNode.FromString(content), LiteralExpressionNode.FromBoolean(true) };
        var appendResult = fileWriteFunction.Invoke(new(), interpreter, appendArguments);

        var appendBoolResult = Assert.IsType<BoolResult>(appendResult);
        Assert.True(appendBoolResult.Value);

        var secondReadResult = fileReadFunction.Invoke(new(), interpreter, new List<ExpressionNode> { LiteralExpressionNode.FromString(filename) });

        var secondReadStringResult = Assert.IsType<StringResult>(secondReadResult);
        Assert.Equal(content + content, secondReadStringResult.Value);

        var deleteResult = fileDeleteFunction.Invoke(new(), interpreter, new List<ExpressionNode> { LiteralExpressionNode.FromString(filename) });

        var deleteBoolResult = Assert.IsType<BoolResult>(deleteResult);
        Assert.True(deleteBoolResult.Value);
    }
}