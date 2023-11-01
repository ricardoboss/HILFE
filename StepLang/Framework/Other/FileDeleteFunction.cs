using StepLang.Expressions.Results;
using StepLang.Interpreting;
using StepLang.Tokenizing;

namespace StepLang.Framework.Other;

public class FileDeleteFunction : FileFunction
{
    public const string Identifier = "fileDelete";

    protected override IEnumerable<NativeParameter> NativeParameters { get; } = new NativeParameter[]
    {
        new(OnlyString, "path"),
    };

    protected override IEnumerable<ResultType> ReturnTypes { get; } = OnlyBool;

    protected override BoolResult Invoke(TokenLocation callLocation, Interpreter interpreter, StringResult argument1)
    {
        var path = argument1.Value;
        var info = GetFileInfoFromPath(path);

        try
        {
            File.Delete(info.FullName);
        }
        catch (IOException)
        {
            return false;
        }

        return true;
    }
}