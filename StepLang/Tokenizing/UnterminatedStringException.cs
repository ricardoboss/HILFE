namespace StepLang.Tokenizing;

public class UnterminatedStringException : TokenizerException
{
    private static string BuildHelpText(char stringDelimiter)
    {
        return $"A string is missing a {(stringDelimiter == '"' ? "'\"' (double quote)" : "\"'\" (single quote)")}. Strings must be closed with a matching delimiter.";
    }

    public char StringDelimiter { get; }

    /// <inheritdoc />
    public UnterminatedStringException(TokenLocation? location, char stringDelimiter) : base(location, "A string was not properly terminated", BuildHelpText(stringDelimiter))
    {
        StringDelimiter = stringDelimiter;
    }
}