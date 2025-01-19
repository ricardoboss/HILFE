using StepLang.Parsing.Nodes.Import;

namespace StepLang.Parsing;

public interface IImportNodeVisitor
{
	void Visit(ImportNode importNode);

	void Visit(ErrorImportNode importNode);
}
