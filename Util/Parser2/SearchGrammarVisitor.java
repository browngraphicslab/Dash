// Generated from SearchGrammar.g4 by ANTLR 4.7.1
import org.antlr.v4.runtime.tree.ParseTreeVisitor;

/**
 * This interface defines a complete generic visitor for a parse tree produced
 * by {@link SearchGrammarParser}.
 *
 * @param <T> The return type of the visit operation. Use {@link Void} for
 * operations with no return type.
 */
public interface SearchGrammarVisitor<T> extends ParseTreeVisitor<T> {
	/**
	 * Visit a parse tree produced by {@link SearchGrammarParser#value}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitValue(SearchGrammarParser.ValueContext ctx);
	/**
	 * Visit a parse tree produced by {@link SearchGrammarParser#keylist}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitKeylist(SearchGrammarParser.KeylistContext ctx);
	/**
	 * Visit a parse tree produced by {@link SearchGrammarParser#kvsearch}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitKvsearch(SearchGrammarParser.KvsearchContext ctx);
	/**
	 * Visit a parse tree produced by {@link SearchGrammarParser#args}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitArgs(SearchGrammarParser.ArgsContext ctx);
	/**
	 * Visit a parse tree produced by {@link SearchGrammarParser#function}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitFunction(SearchGrammarParser.FunctionContext ctx);
	/**
	 * Visit a parse tree produced by {@link SearchGrammarParser#term}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitTerm(SearchGrammarParser.TermContext ctx);
	/**
	 * Visit a parse tree produced by {@link SearchGrammarParser#negation}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitNegation(SearchGrammarParser.NegationContext ctx);
	/**
	 * Visit a parse tree produced by {@link SearchGrammarParser#or}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitOr(SearchGrammarParser.OrContext ctx);
	/**
	 * Visit a parse tree produced by {@link SearchGrammarParser#and}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitAnd(SearchGrammarParser.AndContext ctx);
	/**
	 * Visit a parse tree produced by {@link SearchGrammarParser#query}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitQuery(SearchGrammarParser.QueryContext ctx);
}