using StepLang.Parsing.Nodes.Statements;

namespace StepLang.Parsing;

public interface IStatementVisitor
{
	void Visit(CallStatementNode statementNode);
	void Visit(CodeBlockStatementNode statementNode);
	void Visit(ContinueStatementNode statementNode);
	void Visit(ForeachDeclareKeyDeclareValueStatementNode statementNode);
	void Visit(BreakStatementNode statementNode);
	void Visit(ForeachDeclareKeyValueStatementNode statementNode);
	void Visit(ForeachDeclareValueStatementNode statementNode);
	void Visit(ForeachKeyValueStatementNode statementNode);
	void Visit(ForeachKeyDeclareValueStatementNode statementNode);
	void Visit(ForeachValueStatementNode statementNode);
	void Visit(IdentifierIndexAssignmentNode statementNode);
	void Visit(IfStatementNode statementNode);
	void Visit(ReturnExpressionStatementNode statementNode);
	void Visit(VariableAssignmentNode statementNode);
	void Visit(WhileStatementNode statementNode);
	void Visit(IncrementStatementNode statementNode);
	void Visit(DecrementStatementNode statementNode);
	void Visit(VariableDeclarationStatementNode statementNode);
	void Visit(ReturnStatementNode statementNode);
	void Visit(DiscardStatementNode discardStatementNode);
	void Visit(ErrorStatementNode errorStatementNode);
}
