// Generated from DashSearchGrammar.g4 by ANTLR 4.7.1
import org.antlr.v4.runtime.tree.ParseTreeVisitor;

/**
 * This interface defines a complete generic visitor for a parse tree produced
 * by {@link DashSearchGrammarParser}.
 *
 * @param <T> The return type of the visit operation. Use {@link Void} for
 * operations with no return type.
 */
public interface DashSearchGrammarVisitor<T> extends ParseTreeVisitor<T> {
	/**
	 * Visit a parse tree produced by {@link DashSearchGrammarParser#arguments}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitArguments(DashSearchGrammarParser.ArgumentsContext ctx);
	/**
	 * Visit a parse tree produced by {@link DashSearchGrammarParser#input}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitInput(DashSearchGrammarParser.InputContext ctx);
	/**
	 * Visit a parse tree produced by {@link DashSearchGrammarParser#function_expr}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitFunction_expr(DashSearchGrammarParser.Function_exprContext ctx);
	/**
	 * Visit a parse tree produced by {@link DashSearchGrammarParser#and_token}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitAnd_token(DashSearchGrammarParser.And_tokenContext ctx);
	/**
	 * Visit a parse tree produced by {@link DashSearchGrammarParser#or_token}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitOr_token(DashSearchGrammarParser.Or_tokenContext ctx);
	/**
	 * Visit a parse tree produced by {@link DashSearchGrammarParser#operator}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitOperator(DashSearchGrammarParser.OperatorContext ctx);
	/**
	 * Visit a parse tree produced by {@link DashSearchGrammarParser#phrase}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitPhrase(DashSearchGrammarParser.PhraseContext ctx);
	/**
	 * Visit a parse tree produced by {@link DashSearchGrammarParser#chain}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitChain(DashSearchGrammarParser.ChainContext ctx);
	/**
	 * Visit a parse tree produced by {@link DashSearchGrammarParser#logical_expr}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitLogical_expr(DashSearchGrammarParser.Logical_exprContext ctx);
	/**
	 * Visit a parse tree produced by {@link DashSearchGrammarParser#kv_search}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitKv_search(DashSearchGrammarParser.Kv_searchContext ctx);
	/**
	 * Visit a parse tree produced by {@link DashSearchGrammarParser#search_term}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitSearch_term(DashSearchGrammarParser.Search_termContext ctx);
	/**
	 * Visit a parse tree produced by {@link DashSearchGrammarParser#not_search_term}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitNot_search_term(DashSearchGrammarParser.Not_search_termContext ctx);
	/**
	 * Visit a parse tree produced by {@link DashSearchGrammarParser#query}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitQuery(DashSearchGrammarParser.QueryContext ctx);
}