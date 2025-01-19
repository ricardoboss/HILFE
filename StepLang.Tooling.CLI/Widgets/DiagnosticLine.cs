using Spectre.Console;
using Spectre.Console.Rendering;
using StepLang.Diagnostics;
using System.Text;

namespace StepLang.Tooling.CLI.Widgets;

public class DiagnosticLine : Renderable
{
	private readonly TextPath path;
	private readonly Paragraph text;

	public DiagnosticLine(Diagnostic diagnostic)
	{
		path = new TextPath(diagnostic.File?.FullName ?? "unknown") { LeafStyle = "bold" };
		text = new Paragraph();

		var sb = new StringBuilder();
		sb.Append('(');
		sb.Append(diagnostic.Line);
		sb.Append(',');
		sb.Append(diagnostic.Column);
		sb.Append("): ");
		_ = text.Append(sb.ToString());

		var severityStyle = diagnostic.Severity switch
		{
			Severity.Error => "bold red",
			Severity.Warning => "bold yellow",
			Severity.Suggestion => "aqua",
			Severity.Hint => "blue",
			_ => "default",
		};

		var severityLabel = diagnostic.Severity switch
		{
			Severity.Error => "error",
			Severity.Warning => "warning",
			Severity.Suggestion => "suggestion",
			Severity.Hint => "hint",
			_ => "",
		};

		sb.Clear();
		sb.Append(severityLabel);
		sb.Append(' ');
		sb.Append(diagnostic.Code);
		_ = text.Append(sb.ToString(), severityStyle);

		sb.Clear();
		sb.Append(": ");
		sb.Append(diagnostic.Message);
		_ = text.Append(sb.ToString());
	}

	protected override Measurement Measure(RenderOptions options, int maxWidth)
	{
		var pathMeasurement = path.Measure(options, maxWidth);
		var textMeasurement = ((IRenderable)text).Measure(options, maxWidth - pathMeasurement.Max);

		var totalWidth = pathMeasurement.Max + textMeasurement.Max;

		return new Measurement(totalWidth, totalWidth);
	}

	protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
	{
		foreach (var segment in path.Render(options, maxWidth))
			yield return segment;

		foreach (var segment in ((IRenderable)text).Render(options, maxWidth))
			yield return segment;

		yield return Segment.LineBreak;
	}
}
