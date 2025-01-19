using StepLang.Parsing;
using StepLang.Parsing.Nodes.Import;
using StepLang.Tokenizing;

namespace StepLang.Interpreting;

public partial class Interpreter : IImportNodeVisitor
{
	public void Visit(ImportNode importNode)
	{
		var pathToken = importNode.PathToken;

		FileInfo importedFile;
		if (pathToken.Location.File is null || Path.IsPathRooted(pathToken.StringValue))
		{
			importedFile = new FileInfo(pathToken.StringValue);
		}
		else
		{
			var baseFile = pathToken.Location.File;
			var currentDirectory = Path.GetDirectoryName(baseFile.FullName);
			if (currentDirectory is null)
			{
				throw new IOException("Could not get current directory.");
			}

			var resolvedPath = Path.Combine(currentDirectory, pathToken.StringValue);

			importedFile = new FileInfo(resolvedPath);
		}

		if (!importedFile.Exists)
		{
			throw new ImportedFileDoesNotExistException(pathToken.Location, importedFile);
		}

		if (importedFile.FullName == pathToken.Location.File?.FullName)
		{
			throw new ImportedFileIsSelfException(pathToken.Location, importedFile);
		}

		var source = CharacterSource.FromFile(importedFile);
		// TODO: handle diagnostics differently?
		var tokenizer = new Tokenizer(source, []);
		var tokens = tokenizer.Tokenize();

		var parser = new Parser(tokens, []);
		var root = parser.ParseRoot();

		Visit(root);
	}

	public void Visit(ErrorImportNode importNode)
	{
		throw new NotSupportedException("Cannot interpret imports with errors");
	}
}
