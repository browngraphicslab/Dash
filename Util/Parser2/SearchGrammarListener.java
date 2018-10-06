// Generated from SearchGrammar.g4 by ANTLR 4.7.1
import org.antlr.v4.runtime.tree.ParseTreeListener;

/**
 * This interface defines a complete listener for a parse tree produced by
 * {@link SearchGrammarParser}.
 */
public interface SearchGrammarListener extends ParseTreeListener {
	/**
	 * Enter a parse tree produced by {@link SearchGrammarParser#value}.
	 * @param ctx the parse tree
	 */
	void enterValue(SearchGrammarParser.ValueContext ctx);
	/**
	 * Exit a parse tree produced by {@link SearchGrammarParser#value}.
	 * @param ctx the parse tree
	 */
	void exitValue(SearchGrammarParser.ValueContext ctx);
	/**
	 * Enter a parse tree produced by {@link SearchGrammarParser#keylist}.
	 * @param ctx the parse tree
	 */
	void enterKeylist(SearchGrammarParser.KeylistContext ctx);
	/**
	 * Exit a parse tree produced by {@link SearchGrammarParser#keylist}.
	 * @param ctx the parse tree
	 */
	void exitKeylist(SearchGrammarParser.KeylistContext ctx);
	/**
	 * Enter a parse tree produced by {@link SearchGrammarParser#kvsearch}.
	 * @param ctx the parse tree
	 */
	void enterKvsearch(SearchGrammarParser.KvsearchContext ctx);
	/**
	 * Exit a parse tree produced by {@link SearchGrammarParser#kvsearch}.
	 * @param ctx the parse tree
	 */
	void exitKvsearch(SearchGrammarParser.KvsearchContext ctx);
	/**
	 * Enter a parse tree produced by {@link SearchGrammarParser#args}.
	 * @param ctx the parse tree
	 */
	void enterArgs(SearchGrammarParser.ArgsContext ctx);
	/**
	 * Exit a parse tree produced by {@link SearchGrammarParser#args}.
	 * @param ctx the parse tree
	 */
	void exitArgs(SearchGrammarParser.ArgsContext ctx);
	/**
	 * Enter a parse tree produced by {@link SearchGrammarParser#function}.
	 * @param ctx the parse tree
	 */
	void enterFunction(SearchGrammarParser.FunctionContext ctx);
	/**
	 * Exit a parse tree produced by {@link SearchGrammarParser#function}.
	 * @param ctx the parse tree
	 */
	void exitFunction(SearchGrammarParser.FunctionContext ctx);
	/**
	 * Enter a parse tree produced by {@link SearchGrammarParser#term}.
	 * @param ctx the parse tree
	 */
	void enterTerm(SearchGrammarParser.TermContext ctx);
	/**
	 * Exit a parse tree produced by {@link SearchGrammarParser#term}.
	 * @param ctx the parse tree
	 */
	void exitTerm(SearchGrammarParser.TermContext ctx);
	/**
	 * Enter a parse tree produced by {@link SearchGrammarParser#negation}.
	 * @param ctx the parse tree
	 */
	void enterNegation(SearchGrammarParser.NegationContext ctx);
	/**
	 * Exit a parse tree produced by {@link SearchGrammarParser#negation}.
	 * @param ctx the parse tree
	 */
	void exitNegation(SearchGrammarParser.NegationContext ctx);
	/**
	 * Enter a parse tree produced by {@link SearchGrammarParser#or}.
	 * @param ctx the parse tree
	 */
	void enterOr(SearchGrammarParser.OrContext ctx);
	/**
	 * Exit a parse tree produced by {@link SearchGrammarParser#or}.
	 * @param ctx the parse tree
	 */
	void exitOr(SearchGrammarParser.OrContext ctx);
	/**
	 * Enter a parse tree produced by {@link SearchGrammarParser#and}.
	 * @param ctx the parse tree
	 */
	void enterAnd(SearchGrammarParser.AndContext ctx);
	/**
	 * Exit a parse tree produced by {@link SearchGrammarParser#and}.
	 * @param ctx the parse tree
	 */
	void exitAnd(SearchGrammarParser.AndContext ctx);
	/**
	 * Enter a parse tree produced by {@link SearchGrammarParser#query}.
	 * @param ctx the parse tree
	 */
	void enterQuery(SearchGrammarParser.QueryContext ctx);
	/**
	 * Exit a parse tree produced by {@link SearchGrammarParser#query}.
	 * @param ctx the parse tree
	 */
	void exitQuery(SearchGrammarParser.QueryContext ctx);
}