// Generated from DashSearchGrammar.g4 by ANTLR 4.7.1
import org.antlr.v4.runtime.atn.*;
import org.antlr.v4.runtime.dfa.DFA;
import org.antlr.v4.runtime.*;
import org.antlr.v4.runtime.misc.*;
import org.antlr.v4.runtime.tree.*;
import java.util.List;
import java.util.Iterator;
import java.util.ArrayList;

@SuppressWarnings({"all", "warnings", "unchecked", "unused", "cast"})
public class DashSearchGrammarParser extends Parser {
	static { RuntimeMetaData.checkVersion("4.7.1", RuntimeMetaData.VERSION); }

	protected static final DFA[] _decisionToDFA;
	protected static final PredictionContextCache _sharedContextCache =
		new PredictionContextCache();
	public static final int
		T__0=1, T__1=2, T__2=3, T__3=4, T__4=5, T__5=6, NEWLINE=7, ALPHANUM=8, 
		STRING=9, WHITESPACE=10;
	public static final int
		RULE_arguments = 0, RULE_input = 1, RULE_function_expr = 2, RULE_and_token = 3, 
		RULE_or_token = 4, RULE_operator = 5, RULE_phrase = 6, RULE_chain = 7, 
		RULE_logical_expr = 8, RULE_kv_search = 9, RULE_search_term = 10, RULE_not_search_term = 11, 
		RULE_query = 12;
	public static final String[] ruleNames = {
		"arguments", "input", "function_expr", "and_token", "or_token", "operator", 
		"phrase", "chain", "logical_expr", "kv_search", "search_term", "not_search_term", 
		"query"
	};

	private static final String[] _LITERAL_NAMES = {
		null, "','", "'('", "')'", "'|'", "'!'", "':'"
	};
	private static final String[] _SYMBOLIC_NAMES = {
		null, null, null, null, null, null, null, "NEWLINE", "ALPHANUM", "STRING", 
		"WHITESPACE"
	};
	public static final Vocabulary VOCABULARY = new VocabularyImpl(_LITERAL_NAMES, _SYMBOLIC_NAMES);

	/**
	 * @deprecated Use {@link #VOCABULARY} instead.
	 */
	@Deprecated
	public static final String[] tokenNames;
	static {
		tokenNames = new String[_SYMBOLIC_NAMES.length];
		for (int i = 0; i < tokenNames.length; i++) {
			tokenNames[i] = VOCABULARY.getLiteralName(i);
			if (tokenNames[i] == null) {
				tokenNames[i] = VOCABULARY.getSymbolicName(i);
			}

			if (tokenNames[i] == null) {
				tokenNames[i] = "<INVALID>";
			}
		}
	}

	@Override
	@Deprecated
	public String[] getTokenNames() {
		return tokenNames;
	}

	@Override

	public Vocabulary getVocabulary() {
		return VOCABULARY;
	}

	@Override
	public String getGrammarFileName() { return "DashSearchGrammar.g4"; }

	@Override
	public String[] getRuleNames() { return ruleNames; }

	@Override
	public String getSerializedATN() { return _serializedATN; }

	@Override
	public ATN getATN() { return _ATN; }

	public DashSearchGrammarParser(TokenStream input) {
		super(input);
		_interp = new ParserATNSimulator(this,_ATN,_decisionToDFA,_sharedContextCache);
	}
	public static class ArgumentsContext extends ParserRuleContext {
		public List<TerminalNode> ALPHANUM() { return getTokens(DashSearchGrammarParser.ALPHANUM); }
		public TerminalNode ALPHANUM(int i) {
			return getToken(DashSearchGrammarParser.ALPHANUM, i);
		}
		public List<TerminalNode> WHITESPACE() { return getTokens(DashSearchGrammarParser.WHITESPACE); }
		public TerminalNode WHITESPACE(int i) {
			return getToken(DashSearchGrammarParser.WHITESPACE, i);
		}
		public ArgumentsContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_arguments; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).enterArguments(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).exitArguments(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof DashSearchGrammarVisitor ) return ((DashSearchGrammarVisitor<? extends T>)visitor).visitArguments(this);
			else return visitor.visitChildren(this);
		}
	}

	public final ArgumentsContext arguments() throws RecognitionException {
		ArgumentsContext _localctx = new ArgumentsContext(_ctx, getState());
		enterRule(_localctx, 0, RULE_arguments);
		int _la;
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(31); 
			_errHandler.sync(this);
			_alt = 1+1;
			do {
				switch (_alt) {
				case 1+1:
					{
					{
					setState(26);
					match(ALPHANUM);
					setState(27);
					match(T__0);
					setState(29);
					_errHandler.sync(this);
					_la = _input.LA(1);
					if (_la==WHITESPACE) {
						{
						setState(28);
						match(WHITESPACE);
						}
					}

					}
					}
					break;
				default:
					throw new NoViableAltException(this);
				}
				setState(33); 
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,1,_ctx);
			} while ( _alt!=1 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER );
			setState(35);
			match(ALPHANUM);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class InputContext extends ParserRuleContext {
		public ArgumentsContext arguments() {
			return getRuleContext(ArgumentsContext.class,0);
		}
		public InputContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_input; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).enterInput(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).exitInput(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof DashSearchGrammarVisitor ) return ((DashSearchGrammarVisitor<? extends T>)visitor).visitInput(this);
			else return visitor.visitChildren(this);
		}
	}

	public final InputContext input() throws RecognitionException {
		InputContext _localctx = new InputContext(_ctx, getState());
		enterRule(_localctx, 2, RULE_input);
		try {
			setState(39);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case T__2:
				enterOuterAlt(_localctx, 1);
				{
				}
				break;
			case ALPHANUM:
				enterOuterAlt(_localctx, 2);
				{
				setState(38);
				arguments();
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Function_exprContext extends ParserRuleContext {
		public TerminalNode ALPHANUM() { return getToken(DashSearchGrammarParser.ALPHANUM, 0); }
		public InputContext input() {
			return getRuleContext(InputContext.class,0);
		}
		public Function_exprContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_function_expr; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).enterFunction_expr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).exitFunction_expr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof DashSearchGrammarVisitor ) return ((DashSearchGrammarVisitor<? extends T>)visitor).visitFunction_expr(this);
			else return visitor.visitChildren(this);
		}
	}

	public final Function_exprContext function_expr() throws RecognitionException {
		Function_exprContext _localctx = new Function_exprContext(_ctx, getState());
		enterRule(_localctx, 4, RULE_function_expr);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(41);
			match(ALPHANUM);
			setState(42);
			match(T__1);
			setState(43);
			input();
			setState(44);
			match(T__2);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class And_tokenContext extends ParserRuleContext {
		public TerminalNode WHITESPACE() { return getToken(DashSearchGrammarParser.WHITESPACE, 0); }
		public And_tokenContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_and_token; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).enterAnd_token(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).exitAnd_token(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof DashSearchGrammarVisitor ) return ((DashSearchGrammarVisitor<? extends T>)visitor).visitAnd_token(this);
			else return visitor.visitChildren(this);
		}
	}

	public final And_tokenContext and_token() throws RecognitionException {
		And_tokenContext _localctx = new And_tokenContext(_ctx, getState());
		enterRule(_localctx, 6, RULE_and_token);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(46);
			match(WHITESPACE);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Or_tokenContext extends ParserRuleContext {
		public List<TerminalNode> WHITESPACE() { return getTokens(DashSearchGrammarParser.WHITESPACE); }
		public TerminalNode WHITESPACE(int i) {
			return getToken(DashSearchGrammarParser.WHITESPACE, i);
		}
		public Or_tokenContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_or_token; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).enterOr_token(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).exitOr_token(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof DashSearchGrammarVisitor ) return ((DashSearchGrammarVisitor<? extends T>)visitor).visitOr_token(this);
			else return visitor.visitChildren(this);
		}
	}

	public final Or_tokenContext or_token() throws RecognitionException {
		Or_tokenContext _localctx = new Or_tokenContext(_ctx, getState());
		enterRule(_localctx, 8, RULE_or_token);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(49);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==WHITESPACE) {
				{
				setState(48);
				match(WHITESPACE);
				}
			}

			setState(51);
			match(T__3);
			setState(53);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==WHITESPACE) {
				{
				setState(52);
				match(WHITESPACE);
				}
			}

			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class OperatorContext extends ParserRuleContext {
		public And_tokenContext and_token() {
			return getRuleContext(And_tokenContext.class,0);
		}
		public Or_tokenContext or_token() {
			return getRuleContext(Or_tokenContext.class,0);
		}
		public OperatorContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_operator; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).enterOperator(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).exitOperator(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof DashSearchGrammarVisitor ) return ((DashSearchGrammarVisitor<? extends T>)visitor).visitOperator(this);
			else return visitor.visitChildren(this);
		}
	}

	public final OperatorContext operator() throws RecognitionException {
		OperatorContext _localctx = new OperatorContext(_ctx, getState());
		enterRule(_localctx, 10, RULE_operator);
		try {
			setState(57);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,5,_ctx) ) {
			case 1:
				enterOuterAlt(_localctx, 1);
				{
				setState(55);
				and_token();
				}
				break;
			case 2:
				enterOuterAlt(_localctx, 2);
				{
				setState(56);
				or_token();
				}
				break;
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class PhraseContext extends ParserRuleContext {
		public TerminalNode ALPHANUM() { return getToken(DashSearchGrammarParser.ALPHANUM, 0); }
		public TerminalNode STRING() { return getToken(DashSearchGrammarParser.STRING, 0); }
		public PhraseContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_phrase; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).enterPhrase(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).exitPhrase(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof DashSearchGrammarVisitor ) return ((DashSearchGrammarVisitor<? extends T>)visitor).visitPhrase(this);
			else return visitor.visitChildren(this);
		}
	}

	public final PhraseContext phrase() throws RecognitionException {
		PhraseContext _localctx = new PhraseContext(_ctx, getState());
		enterRule(_localctx, 12, RULE_phrase);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(59);
			_la = _input.LA(1);
			if ( !(_la==ALPHANUM || _la==STRING) ) {
			_errHandler.recoverInline(this);
			}
			else {
				if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
				_errHandler.reportMatch(this);
				consume();
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class ChainContext extends ParserRuleContext {
		public List<PhraseContext> phrase() {
			return getRuleContexts(PhraseContext.class);
		}
		public PhraseContext phrase(int i) {
			return getRuleContext(PhraseContext.class,i);
		}
		public List<OperatorContext> operator() {
			return getRuleContexts(OperatorContext.class);
		}
		public OperatorContext operator(int i) {
			return getRuleContext(OperatorContext.class,i);
		}
		public ChainContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_chain; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).enterChain(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).exitChain(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof DashSearchGrammarVisitor ) return ((DashSearchGrammarVisitor<? extends T>)visitor).visitChain(this);
			else return visitor.visitChildren(this);
		}
	}

	public final ChainContext chain() throws RecognitionException {
		ChainContext _localctx = new ChainContext(_ctx, getState());
		enterRule(_localctx, 14, RULE_chain);
		int _la;
		try {
			int _alt;
			setState(74);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,8,_ctx) ) {
			case 1:
				enterOuterAlt(_localctx, 1);
				{
				setState(62);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if (_la==T__4) {
					{
					setState(61);
					match(T__4);
					}
				}

				setState(64);
				phrase();
				}
				break;
			case 2:
				enterOuterAlt(_localctx, 2);
				{
				setState(68); 
				_errHandler.sync(this);
				_alt = 1;
				do {
					switch (_alt) {
					case 1:
						{
						{
						setState(65);
						phrase();
						setState(66);
						operator();
						}
						}
						break;
					default:
						throw new NoViableAltException(this);
					}
					setState(70); 
					_errHandler.sync(this);
					_alt = getInterpreter().adaptivePredict(_input,7,_ctx);
				} while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER );
				setState(72);
				phrase();
				}
				break;
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Logical_exprContext extends ParserRuleContext {
		public ChainContext chain() {
			return getRuleContext(ChainContext.class,0);
		}
		public List<Logical_exprContext> logical_expr() {
			return getRuleContexts(Logical_exprContext.class);
		}
		public Logical_exprContext logical_expr(int i) {
			return getRuleContext(Logical_exprContext.class,i);
		}
		public OperatorContext operator() {
			return getRuleContext(OperatorContext.class,0);
		}
		public Logical_exprContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_logical_expr; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).enterLogical_expr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).exitLogical_expr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof DashSearchGrammarVisitor ) return ((DashSearchGrammarVisitor<? extends T>)visitor).visitLogical_expr(this);
			else return visitor.visitChildren(this);
		}
	}

	public final Logical_exprContext logical_expr() throws RecognitionException {
		return logical_expr(0);
	}

	private Logical_exprContext logical_expr(int _p) throws RecognitionException {
		ParserRuleContext _parentctx = _ctx;
		int _parentState = getState();
		Logical_exprContext _localctx = new Logical_exprContext(_ctx, _parentState);
		Logical_exprContext _prevctx = _localctx;
		int _startState = 16;
		enterRecursionRule(_localctx, 16, RULE_logical_expr, _p);
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(82);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case T__4:
			case ALPHANUM:
			case STRING:
				{
				setState(77);
				chain();
				}
				break;
			case T__1:
				{
				setState(78);
				match(T__1);
				setState(79);
				logical_expr(0);
				setState(80);
				match(T__2);
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
			_ctx.stop = _input.LT(-1);
			setState(90);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,10,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					if ( _parseListeners!=null ) triggerExitRuleEvent();
					_prevctx = _localctx;
					{
					{
					_localctx = new Logical_exprContext(_parentctx, _parentState);
					pushNewRecursionContext(_localctx, _startState, RULE_logical_expr);
					setState(84);
					if (!(precpred(_ctx, 2))) throw new FailedPredicateException(this, "precpred(_ctx, 2)");
					setState(85);
					operator();
					setState(86);
					logical_expr(3);
					}
					} 
				}
				setState(92);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,10,_ctx);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			unrollRecursionContexts(_parentctx);
		}
		return _localctx;
	}

	public static class Kv_searchContext extends ParserRuleContext {
		public List<PhraseContext> phrase() {
			return getRuleContexts(PhraseContext.class);
		}
		public PhraseContext phrase(int i) {
			return getRuleContext(PhraseContext.class,i);
		}
		public Kv_searchContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_kv_search; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).enterKv_search(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).exitKv_search(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof DashSearchGrammarVisitor ) return ((DashSearchGrammarVisitor<? extends T>)visitor).visitKv_search(this);
			else return visitor.visitChildren(this);
		}
	}

	public final Kv_searchContext kv_search() throws RecognitionException {
		Kv_searchContext _localctx = new Kv_searchContext(_ctx, getState());
		enterRule(_localctx, 18, RULE_kv_search);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(93);
			phrase();
			setState(94);
			match(T__5);
			setState(95);
			phrase();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Search_termContext extends ParserRuleContext {
		public Function_exprContext function_expr() {
			return getRuleContext(Function_exprContext.class,0);
		}
		public QueryContext query() {
			return getRuleContext(QueryContext.class,0);
		}
		public PhraseContext phrase() {
			return getRuleContext(PhraseContext.class,0);
		}
		public Kv_searchContext kv_search() {
			return getRuleContext(Kv_searchContext.class,0);
		}
		public Search_termContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_search_term; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).enterSearch_term(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).exitSearch_term(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof DashSearchGrammarVisitor ) return ((DashSearchGrammarVisitor<? extends T>)visitor).visitSearch_term(this);
			else return visitor.visitChildren(this);
		}
	}

	public final Search_termContext search_term() throws RecognitionException {
		Search_termContext _localctx = new Search_termContext(_ctx, getState());
		enterRule(_localctx, 20, RULE_search_term);
		try {
			setState(104);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,11,_ctx) ) {
			case 1:
				enterOuterAlt(_localctx, 1);
				{
				setState(97);
				function_expr();
				}
				break;
			case 2:
				enterOuterAlt(_localctx, 2);
				{
				setState(98);
				match(T__1);
				setState(99);
				query();
				setState(100);
				match(T__2);
				}
				break;
			case 3:
				enterOuterAlt(_localctx, 3);
				{
				setState(102);
				phrase();
				}
				break;
			case 4:
				enterOuterAlt(_localctx, 4);
				{
				setState(103);
				kv_search();
				}
				break;
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Not_search_termContext extends ParserRuleContext {
		public Search_termContext search_term() {
			return getRuleContext(Search_termContext.class,0);
		}
		public Not_search_termContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_not_search_term; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).enterNot_search_term(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).exitNot_search_term(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof DashSearchGrammarVisitor ) return ((DashSearchGrammarVisitor<? extends T>)visitor).visitNot_search_term(this);
			else return visitor.visitChildren(this);
		}
	}

	public final Not_search_termContext not_search_term() throws RecognitionException {
		Not_search_termContext _localctx = new Not_search_termContext(_ctx, getState());
		enterRule(_localctx, 22, RULE_not_search_term);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(107);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==T__4) {
				{
				setState(106);
				match(T__4);
				}
			}

			setState(109);
			search_term();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class QueryContext extends ParserRuleContext {
		public List<Not_search_termContext> not_search_term() {
			return getRuleContexts(Not_search_termContext.class);
		}
		public Not_search_termContext not_search_term(int i) {
			return getRuleContext(Not_search_termContext.class,i);
		}
		public List<OperatorContext> operator() {
			return getRuleContexts(OperatorContext.class);
		}
		public OperatorContext operator(int i) {
			return getRuleContext(OperatorContext.class,i);
		}
		public QueryContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_query; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).enterQuery(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).exitQuery(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof DashSearchGrammarVisitor ) return ((DashSearchGrammarVisitor<? extends T>)visitor).visitQuery(this);
			else return visitor.visitChildren(this);
		}
	}

	public final QueryContext query() throws RecognitionException {
		QueryContext _localctx = new QueryContext(_ctx, getState());
		enterRule(_localctx, 24, RULE_query);
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(116);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,13,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					{
					{
					setState(111);
					not_search_term();
					setState(112);
					operator();
					}
					} 
				}
				setState(118);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,13,_ctx);
			}
			setState(119);
			not_search_term();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public boolean sempred(RuleContext _localctx, int ruleIndex, int predIndex) {
		switch (ruleIndex) {
		case 8:
			return logical_expr_sempred((Logical_exprContext)_localctx, predIndex);
		}
		return true;
	}
	private boolean logical_expr_sempred(Logical_exprContext _localctx, int predIndex) {
		switch (predIndex) {
		case 0:
			return precpred(_ctx, 2);
		}
		return true;
	}

	public static final String _serializedATN =
		"\3\u608b\ua72a\u8133\ub9ed\u417c\u3be7\u7786\u5964\3\f|\4\2\t\2\4\3\t"+
		"\3\4\4\t\4\4\5\t\5\4\6\t\6\4\7\t\7\4\b\t\b\4\t\t\t\4\n\t\n\4\13\t\13\4"+
		"\f\t\f\4\r\t\r\4\16\t\16\3\2\3\2\3\2\5\2 \n\2\6\2\"\n\2\r\2\16\2#\3\2"+
		"\3\2\3\3\3\3\5\3*\n\3\3\4\3\4\3\4\3\4\3\4\3\5\3\5\3\6\5\6\64\n\6\3\6\3"+
		"\6\5\68\n\6\3\7\3\7\5\7<\n\7\3\b\3\b\3\t\5\tA\n\t\3\t\3\t\3\t\3\t\6\t"+
		"G\n\t\r\t\16\tH\3\t\3\t\5\tM\n\t\3\n\3\n\3\n\3\n\3\n\3\n\5\nU\n\n\3\n"+
		"\3\n\3\n\3\n\7\n[\n\n\f\n\16\n^\13\n\3\13\3\13\3\13\3\13\3\f\3\f\3\f\3"+
		"\f\3\f\3\f\3\f\5\fk\n\f\3\r\5\rn\n\r\3\r\3\r\3\16\3\16\3\16\7\16u\n\16"+
		"\f\16\16\16x\13\16\3\16\3\16\3\16\3#\3\22\17\2\4\6\b\n\f\16\20\22\24\26"+
		"\30\32\2\3\3\2\n\13\2~\2!\3\2\2\2\4)\3\2\2\2\6+\3\2\2\2\b\60\3\2\2\2\n"+
		"\63\3\2\2\2\f;\3\2\2\2\16=\3\2\2\2\20L\3\2\2\2\22T\3\2\2\2\24_\3\2\2\2"+
		"\26j\3\2\2\2\30m\3\2\2\2\32v\3\2\2\2\34\35\7\n\2\2\35\37\7\3\2\2\36 \7"+
		"\f\2\2\37\36\3\2\2\2\37 \3\2\2\2 \"\3\2\2\2!\34\3\2\2\2\"#\3\2\2\2#$\3"+
		"\2\2\2#!\3\2\2\2$%\3\2\2\2%&\7\n\2\2&\3\3\2\2\2\'*\3\2\2\2(*\5\2\2\2)"+
		"\'\3\2\2\2)(\3\2\2\2*\5\3\2\2\2+,\7\n\2\2,-\7\4\2\2-.\5\4\3\2./\7\5\2"+
		"\2/\7\3\2\2\2\60\61\7\f\2\2\61\t\3\2\2\2\62\64\7\f\2\2\63\62\3\2\2\2\63"+
		"\64\3\2\2\2\64\65\3\2\2\2\65\67\7\6\2\2\668\7\f\2\2\67\66\3\2\2\2\678"+
		"\3\2\2\28\13\3\2\2\29<\5\b\5\2:<\5\n\6\2;9\3\2\2\2;:\3\2\2\2<\r\3\2\2"+
		"\2=>\t\2\2\2>\17\3\2\2\2?A\7\7\2\2@?\3\2\2\2@A\3\2\2\2AB\3\2\2\2BM\5\16"+
		"\b\2CD\5\16\b\2DE\5\f\7\2EG\3\2\2\2FC\3\2\2\2GH\3\2\2\2HF\3\2\2\2HI\3"+
		"\2\2\2IJ\3\2\2\2JK\5\16\b\2KM\3\2\2\2L@\3\2\2\2LF\3\2\2\2M\21\3\2\2\2"+
		"NO\b\n\1\2OU\5\20\t\2PQ\7\4\2\2QR\5\22\n\2RS\7\5\2\2SU\3\2\2\2TN\3\2\2"+
		"\2TP\3\2\2\2U\\\3\2\2\2VW\f\4\2\2WX\5\f\7\2XY\5\22\n\5Y[\3\2\2\2ZV\3\2"+
		"\2\2[^\3\2\2\2\\Z\3\2\2\2\\]\3\2\2\2]\23\3\2\2\2^\\\3\2\2\2_`\5\16\b\2"+
		"`a\7\b\2\2ab\5\16\b\2b\25\3\2\2\2ck\5\6\4\2de\7\4\2\2ef\5\32\16\2fg\7"+
		"\5\2\2gk\3\2\2\2hk\5\16\b\2ik\5\24\13\2jc\3\2\2\2jd\3\2\2\2jh\3\2\2\2"+
		"ji\3\2\2\2k\27\3\2\2\2ln\7\7\2\2ml\3\2\2\2mn\3\2\2\2no\3\2\2\2op\5\26"+
		"\f\2p\31\3\2\2\2qr\5\30\r\2rs\5\f\7\2su\3\2\2\2tq\3\2\2\2ux\3\2\2\2vt"+
		"\3\2\2\2vw\3\2\2\2wy\3\2\2\2xv\3\2\2\2yz\5\30\r\2z\33\3\2\2\2\20\37#)"+
		"\63\67;@HLT\\jmv";
	public static final ATN _ATN =
		new ATNDeserializer().deserialize(_serializedATN.toCharArray());
	static {
		_decisionToDFA = new DFA[_ATN.getNumberOfDecisions()];
		for (int i = 0; i < _ATN.getNumberOfDecisions(); i++) {
			_decisionToDFA[i] = new DFA(_ATN.getDecisionState(i), i);
		}
	}
}