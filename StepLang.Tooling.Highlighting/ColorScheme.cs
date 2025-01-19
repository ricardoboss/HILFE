using System.Drawing;

namespace StepLang.Tooling.Highlighting;

public record ColorScheme(
	Style Default,
	Style Keyword,
	Style Type,
	Style Identifier,
	Style String,
	Style Number,
	Style Bool,
	Style Null,
	Style Comment,
	Style Operator,
	Style Punctuation,
	Style Error
)
{
	public static IEnumerable<string> Names => ["Pale", "Dim", "Mono"];

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
		new Style(Color.White, IsDefault: true),
		new Style(Color.PaleVioletRed),
		new Style(Color.Turquoise),
		new Style(Color.PaleGoldenrod),
		new Style(Color.DarkSeaGreen),
		new Style(Color.Plum),
		new Style(Color.CadetBlue),
		new Style(Color.CadetBlue),
		new Style(Color.Gray, true),
		new Style(Color.White),
		new Style(Color.White),
		new Style(Color.IndianRed)
	);

	public static ColorScheme Dim { get; } = new(
		new Style(Color.Black, IsDefault: true),
		new Style(Color.MediumVioletRed),
		new Style(Color.DarkCyan),
		new Style(Color.DarkBlue),
		new Style(Color.Brown),
		new Style(Color.DarkGreen),
		new Style(Color.CadetBlue),
		new Style(Color.CadetBlue),
		new Style(Color.DarkGray, true),
		new Style(Color.Black),
		new Style(Color.Black),
		new Style(Color.DarkRed)
	);

	public static ColorScheme Mono { get; } = new(
		new Style(Color.Gray, IsDefault: true),
		new Style(Color.LightGray),
		new Style(Color.Gray),
		new Style(Color.DarkGray),
		new Style(Color.DarkGray, true),
		new Style(Color.DarkGray, true),
		new Style(Color.DarkGray, true),
		new Style(Color.DarkGray, true),
		new Style(Color.DimGray, true),
		new Style(Color.Gray),
		new Style(Color.Gray),
		new Style(Color.DimGray)
	);
}
