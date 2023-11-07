using System.Text;
using StepLang.Expressions.Results;
using StepLang.Interpreting;
using StepLang.Parsing;

namespace StepLang.Framework.Other;

public class FileWriteFunction : NativeFunction
{
    public const string Identifier = "fileWrite";

    protected override IEnumerable<NativeParameter> NativeParameters { get; } = new NativeParameter[] { (new[] { ResultType.Str }, "path"), (new[] { ResultType.Str }, "content"), (new[] { ResultType.Bool }, "append") };

    /// <inheritdoc />
    public override ExpressionResult Invoke(Interpreter interpreter, IReadOnlyList<ExpressionNode> arguments)
    {
        CheckArgumentCount(arguments, 2, 3);

        var path = await arguments[0].EvaluateAsync(interpreter, r => r.ExpectString().Value, cancellationToken);
        var content = await arguments[1].EvaluateAsync(interpreter, r => r.ExpectString().Value, cancellationToken);

        var append = false;
        if (arguments.Count >= 3)
            append = await arguments[2].EvaluateAsync(interpreter, r => r.ExpectBool().Value, cancellationToken);

        try
        {
            if (append)
                await File.AppendAllTextAsync(path, content, Encoding.ASCII, cancellationToken);
            else
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                await File.WriteAllTextAsync(path, content, Encoding.ASCII, cancellationToken);
            }
        }
        catch (Exception e) when (e is IOException or SystemException)
        {
            return BoolResult.False;
        }

        return BoolResult.True;
    }
}