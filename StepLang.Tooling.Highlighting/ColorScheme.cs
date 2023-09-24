using System.Drawing;

namespace StepLang.Tooling.Highlighting;

public record ColorScheme(Style Default, Style Keyword, Style Type, Style Identifier, Style String, Style Number,
    Style Bool, Style Comment,
    Style Operator, Style Punctuation)
{
    public static IEnumerable<string> Names => new[] {"Pale", "Dim", "Mono"};

    public static ColorScheme ByName(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "pale" => Pale,
            "dim" => Dim,
            "mono" => Mono,
            _ => throw new NotSupportedException($"The color scheme '{name}' is not supported."),
        };
    }

    public static ColorScheme Pale { get; } = new(
        new(Color.White),
        new(Color.PaleVioletRed),
        new(Color.Turquoise),
        new(Color.PaleGoldenrod),
        new(Color.DarkSeaGreen),
        new(Color.Plum),
        new(Color.CadetBlue),
        new(Color.Gray, true),
        new(Color.White),
        new(Color.White)
    );

    public static ColorScheme Dim { get; } = new(
        new(Color.Black),
        new(Color.MediumVioletRed),
        new(Color.DarkCyan),
        new(Color.DarkBlue),
        new(Color.Brown),
        new(Color.DarkGreen),
        new(Color.CadetBlue),
        new(Color.DarkGray, true),
        new(Color.Black),
        new(Color.Black)
    );

    public static ColorScheme Mono { get; } = new(
        new(Color.Gray),
        new(Color.LightGray),
        new(Color.Gray),
        new(Color.DarkGray),
        new(Color.DarkGray, true),
        new(Color.DarkGray, true),
        new(Color.DarkGray, true),
        new(Color.DimGray, true),
        new(Color.Gray),
        new(Color.Gray)
    );
}
