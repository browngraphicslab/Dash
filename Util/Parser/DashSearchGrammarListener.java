// Generated from DashSearchGrammar.g4 by ANTLR 4.7.1
import org.antlr.v4.runtime.tree.ParseTreeListener;

/**
 * This interface defines a complete listener for a parse tree produced by
 * {@link DashSearchGrammarParser}.
 */
public interface DashSearchGrammarListener extends ParseTreeListener {
	/**
	 * Enter a parse tree produced by {@link DashSearchGrammarParser#argument}.
	 * @param ctx the parse tree
	 */
	void enterArgument(DashSearchGrammarParser.ArgumentContext ctx);
	/**
	 * Exit a parse tree produced by {@link DashSearchGrammarParser#argument}.
	 * @param ctx the parse tree
	 */
	void exitArgument(DashSearchGrammarParser.ArgumentContext ctx);
	/**
	 * Enter a parse tree produced by {@link DashSearchGrammarParser#arguments}.
	 * @param ctx the parse tree
	 */
	void enterArguments(DashSearchGrammarParser.ArgumentsContext ctx);
	/**
	 * Exit a parse tree produced by {@link DashSearchGrammarParser#arguments}.
	 * @param ctx the parse tree
	 */
	void exitArguments(DashSearchGrammarParser.ArgumentsContext ctx);
	/**
	 * Enter a parse tree produced by {@link DashSearchGrammarParser#function_expr}.
	 * @param ctx the parse tree
	 */
	void enterFunction_expr(DashSearchGrammarParser.Function_exprContext ctx);
	/**
	 * Exit a parse tree produced by {@link DashSearchGrammarParser#function_expr}.
	 * @param ctx the parse tree
	 */
	void exitFunction_expr(DashSearchGrammarParser.Function_exprContext ctx);
	/**
	 * Enter a parse tree produced by {@link DashSearchGrammarParser#and_token}.
	 * @param ctx the parse tree
	 */
	void enterAnd_token(DashSearchGrammarParser.And_tokenContext ctx);
	/**
	 * Exit a parse tree produced by {@link DashSearchGrammarParser#and_token}.
	 * @param ctx the parse tree
	 */
	void exitAnd_token(DashSearchGrammarParser.And_tokenContext ctx);
	/**
	 * Enter a parse tree produced by {@link DashSearchGrammarParser#or_token}.
	 * @param ctx the parse tree
	 */
	void enterOr_token(DashSearchGrammarParser.Or_tokenContext ctx);
	/**
	 * Exit a parse tree produced by {@link DashSearchGrammarParser#or_token}.
	 * @param ctx the parse tree
	 */
	void exitOr_token(DashSearchGrammarParser.Or_tokenContext ctx);
	/**
	 * Enter a parse tree produced by {@link DashSearchGrammarParser#operator}.
	 * @param ctx the parse tree
	 */
	void enterOperator(DashSearchGrammarParser.OperatorContext ctx);
	/**
	 * Exit a parse tree produced by {@link DashSearchGrammarParser#operator}.
	 * @param ctx the parse tree
	 */
	void exitOperator(DashSearchGrammarParser.OperatorContext ctx);
	/**
	 * Enter a parse tree produced by {@link DashSearchGrammarParser#phrase}.
	 * @param ctx the parse tree
	 */
	void enterPhrase(DashSearchGrammarParser.PhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link DashSearchGrammarParser#phrase}.
	 * @param ctx the parse tree
	 */
	void exitPhrase(DashSearchGrammarParser.PhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link DashSearchGrammarParser#kv_search}.
	 * @param ctx the parse tree
	 */
	void enterKv_search(DashSearchGrammarParser.Kv_searchContext ctx);
	/**
	 * Exit a parse tree produced by {@link DashSearchGrammarParser#kv_search}.
	 * @param ctx the parse tree
	 */
	void exitKv_search(DashSearchGrammarParser.Kv_searchContext ctx);
	/**
	 * Enter a parse tree produced by {@link DashSearchGrammarParser#search_term}.
	 * @param ctx the parse tree
	 */
	void enterSearch_term(DashSearchGrammarParser.Search_termContext ctx);
	/**
	 * Exit a parse tree produced by {@link DashSearchGrammarParser#search_term}.
	 * @param ctx the parse tree
	 */
	void exitSearch_term(DashSearchGrammarParser.Search_termContext ctx);
	/**
	 * Enter a parse tree produced by {@link DashSearchGrammarParser#not_search_term}.
	 * @param ctx the parse tree
	 */
	void enterNot_search_term(DashSearchGrammarParser.Not_search_termContext ctx);
	/**
	 * Exit a parse tree produced by {@link DashSearchGrammarParser#not_search_term}.
	 * @param ctx the parse tree
	 */
	void exitNot_search_term(DashSearchGrammarParser.Not_search_termContext ctx);
	/**
	 * Enter a parse tree produced by {@link DashSearchGrammarParser#query}.
	 * @param ctx the parse tree
	 */
	void enterQuery(DashSearchGrammarParser.QueryContext ctx);
	/**
	 * Exit a parse tree produced by {@link DashSearchGrammarParser#query}.
	 * @param ctx the parse tree
	 */
	void exitQuery(DashSearchGrammarParser.QueryContext ctx);
}