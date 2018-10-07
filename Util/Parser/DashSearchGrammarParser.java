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
		RULE_argument = 0, RULE_arguments = 1, RULE_function_expr = 2, RULE_and_token = 3, 
		RULE_or_token = 4, RULE_operator = 5, RULE_phrase = 6, RULE_kv_search = 7, 
		RULE_search_term = 8, RULE_not_search_term = 9, RULE_query = 10;
	public static final String[] ruleNames = {
		"argument", "arguments", "function_expr", "and_token", "or_token", "operator", 
		"phrase", "kv_search", "search_term", "not_search_term", "query"
	};

	private static final String[] _LITERAL_NAMES = {
		null, "','", "'('", "')'", "'|'", "':'", "'!'"
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
	public static class ArgumentContext extends ParserRuleContext {
		public TerminalNode ALPHANUM() { return getToken(DashSearchGrammarParser.ALPHANUM, 0); }
		public TerminalNode STRING() { return getToken(DashSearchGrammarParser.STRING, 0); }
		public ArgumentContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_argument; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).enterArgument(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof DashSearchGrammarListener ) ((DashSearchGrammarListener)listener).exitArgument(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof DashSearchGrammarVisitor ) return ((DashSearchGrammarVisitor<? extends T>)visitor).visitArgument(this);
			else return visitor.visitChildren(this);
		}
	}

	public final ArgumentContext argument() throws RecognitionException {
		ArgumentContext _localctx = new ArgumentContext(_ctx, getState());
		enterRule(_localctx, 0, RULE_argument);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(22);
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

	public static class ArgumentsContext extends ParserRuleContext {
		public List<ArgumentContext> argument() {
			return getRuleContexts(ArgumentContext.class);
		}
		public ArgumentContext argument(int i) {
			return getRuleContext(ArgumentContext.class,i);
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
		enterRule(_localctx, 2, RULE_arguments);
		int _la;
		try {
			int _alt;
			setState(37);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case T__2:
				enterOuterAlt(_localctx, 1);
				{
				}
				break;
			case ALPHANUM:
			case STRING:
				enterOuterAlt(_localctx, 2);
				{
				setState(34);
				_errHandler.sync(this);
				switch ( getInterpreter().adaptivePredict(_input,2,_ctx) ) {
				case 1:
					{
					setState(30); 
					_errHandler.sync(this);
					_alt = 1;
					do {
						switch (_alt) {
						case 1:
							{
							{
							setState(25);
							argument();
							setState(26);
							match(T__0);
							setState(28);
							_errHandler.sync(this);
							_la = _input.LA(1);
							if (_la==WHITESPACE) {
								{
								setState(27);
								match(WHITESPACE);
								}
							}

							}
							}
							break;
						default:
							throw new NoViableAltException(this);
						}
						setState(32); 
						_errHandler.sync(this);
						_alt = getInterpreter().adaptivePredict(_input,1,_ctx);
					} while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER );
					}
					break;
				}
				setState(36);
				argument();
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
		public ArgumentsContext arguments() {
			return getRuleContext(ArgumentsContext.class,0);
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
			setState(39);
			match(ALPHANUM);
			setState(40);
			match(T__1);
			setState(41);
			arguments();
			setState(42);
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
			setState(44);
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
			setState(47);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==WHITESPACE) {
				{
				setState(46);
				match(WHITESPACE);
				}
			}

			setState(49);
			match(T__3);
			setState(51);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==WHITESPACE) {
				{
				setState(50);
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
			setState(55);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,6,_ctx) ) {
			case 1:
				enterOuterAlt(_localctx, 1);
				{
				setState(53);
				and_token();
				}
				break;
			case 2:
				enterOuterAlt(_localctx, 2);
				{
				setState(54);
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
			setState(57);
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
		enterRule(_localctx, 14, RULE_kv_search);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(59);
			phrase();
			setState(60);
			match(T__4);
			setState(61);
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
		enterRule(_localctx, 16, RULE_search_term);
		try {
			setState(70);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,7,_ctx) ) {
			case 1:
				enterOuterAlt(_localctx, 1);
				{
				setState(63);
				function_expr();
				}
				break;
			case 2:
				enterOuterAlt(_localctx, 2);
				{
				setState(64);
				match(T__1);
				setState(65);
				query(0);
				setState(66);
				match(T__2);
				}
				break;
			case 3:
				enterOuterAlt(_localctx, 3);
				{
				setState(68);
				phrase();
				}
				break;
			case 4:
				enterOuterAlt(_localctx, 4);
				{
				setState(69);
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
		enterRule(_localctx, 18, RULE_not_search_term);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(72);
			match(T__5);
			setState(73);
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
		public QueryContext query() {
			return getRuleContext(QueryContext.class,0);
		}
		public OperatorContext operator() {
			return getRuleContext(OperatorContext.class,0);
		}
		public Not_search_termContext not_search_term() {
			return getRuleContext(Not_search_termContext.class,0);
		}
		public Search_termContext search_term() {
			return getRuleContext(Search_termContext.class,0);
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
		return query(0);
	}

	private QueryContext query(int _p) throws RecognitionException {
		ParserRuleContext _parentctx = _ctx;
		int _parentState = getState();
		QueryContext _localctx = new QueryContext(_ctx, _parentState);
		QueryContext _prevctx = _localctx;
		int _startState = 20;
		enterRecursionRule(_localctx, 20, RULE_query, _p);
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			{
			}
			_ctx.stop = _input.LT(-1);
			setState(86);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,9,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					if ( _parseListeners!=null ) triggerExitRuleEvent();
					_prevctx = _localctx;
					{
					setState(84);
					_errHandler.sync(this);
					switch ( getInterpreter().adaptivePredict(_input,8,_ctx) ) {
					case 1:
						{
						_localctx = new QueryContext(_parentctx, _parentState);
						pushNewRecursionContext(_localctx, _startState, RULE_query);
						setState(76);
						if (!(precpred(_ctx, 2))) throw new FailedPredicateException(this, "precpred(_ctx, 2)");
						setState(77);
						operator();
						setState(78);
						not_search_term();
						}
						break;
					case 2:
						{
						_localctx = new QueryContext(_parentctx, _parentState);
						pushNewRecursionContext(_localctx, _startState, RULE_query);
						setState(80);
						if (!(precpred(_ctx, 1))) throw new FailedPredicateException(this, "precpred(_ctx, 1)");
						setState(81);
						operator();
						setState(82);
						search_term();
						}
						break;
					}
					} 
				}
				setState(88);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,9,_ctx);
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

	public boolean sempred(RuleContext _localctx, int ruleIndex, int predIndex) {
		switch (ruleIndex) {
		case 10:
			return query_sempred((QueryContext)_localctx, predIndex);
		}
		return true;
	}
	private boolean query_sempred(QueryContext _localctx, int predIndex) {
		switch (predIndex) {
		case 0:
			return precpred(_ctx, 2);
		case 1:
			return precpred(_ctx, 1);
		}
		return true;
	}

	public static final String _serializedATN =
		"\3\u608b\ua72a\u8133\ub9ed\u417c\u3be7\u7786\u5964\3\f\\\4\2\t\2\4\3\t"+
		"\3\4\4\t\4\4\5\t\5\4\6\t\6\4\7\t\7\4\b\t\b\4\t\t\t\4\n\t\n\4\13\t\13\4"+
		"\f\t\f\3\2\3\2\3\3\3\3\3\3\3\3\5\3\37\n\3\6\3!\n\3\r\3\16\3\"\5\3%\n\3"+
		"\3\3\5\3(\n\3\3\4\3\4\3\4\3\4\3\4\3\5\3\5\3\6\5\6\62\n\6\3\6\3\6\5\6\66"+
		"\n\6\3\7\3\7\5\7:\n\7\3\b\3\b\3\t\3\t\3\t\3\t\3\n\3\n\3\n\3\n\3\n\3\n"+
		"\3\n\5\nI\n\n\3\13\3\13\3\13\3\f\3\f\3\f\3\f\3\f\3\f\3\f\3\f\3\f\7\fW"+
		"\n\f\f\f\16\fZ\13\f\3\f\2\3\26\r\2\4\6\b\n\f\16\20\22\24\26\2\3\3\2\n"+
		"\13\2\\\2\30\3\2\2\2\4\'\3\2\2\2\6)\3\2\2\2\b.\3\2\2\2\n\61\3\2\2\2\f"+
		"9\3\2\2\2\16;\3\2\2\2\20=\3\2\2\2\22H\3\2\2\2\24J\3\2\2\2\26M\3\2\2\2"+
		"\30\31\t\2\2\2\31\3\3\2\2\2\32(\3\2\2\2\33\34\5\2\2\2\34\36\7\3\2\2\35"+
		"\37\7\f\2\2\36\35\3\2\2\2\36\37\3\2\2\2\37!\3\2\2\2 \33\3\2\2\2!\"\3\2"+
		"\2\2\" \3\2\2\2\"#\3\2\2\2#%\3\2\2\2$ \3\2\2\2$%\3\2\2\2%&\3\2\2\2&(\5"+
		"\2\2\2\'\32\3\2\2\2\'$\3\2\2\2(\5\3\2\2\2)*\7\n\2\2*+\7\4\2\2+,\5\4\3"+
		"\2,-\7\5\2\2-\7\3\2\2\2./\7\f\2\2/\t\3\2\2\2\60\62\7\f\2\2\61\60\3\2\2"+
		"\2\61\62\3\2\2\2\62\63\3\2\2\2\63\65\7\6\2\2\64\66\7\f\2\2\65\64\3\2\2"+
		"\2\65\66\3\2\2\2\66\13\3\2\2\2\67:\5\b\5\28:\5\n\6\29\67\3\2\2\298\3\2"+
		"\2\2:\r\3\2\2\2;<\t\2\2\2<\17\3\2\2\2=>\5\16\b\2>?\7\7\2\2?@\5\16\b\2"+
		"@\21\3\2\2\2AI\5\6\4\2BC\7\4\2\2CD\5\26\f\2DE\7\5\2\2EI\3\2\2\2FI\5\16"+
		"\b\2GI\5\20\t\2HA\3\2\2\2HB\3\2\2\2HF\3\2\2\2HG\3\2\2\2I\23\3\2\2\2JK"+
		"\7\b\2\2KL\5\22\n\2L\25\3\2\2\2MX\b\f\1\2NO\f\4\2\2OP\5\f\7\2PQ\5\24\13"+
		"\2QW\3\2\2\2RS\f\3\2\2ST\5\f\7\2TU\5\22\n\2UW\3\2\2\2VN\3\2\2\2VR\3\2"+
		"\2\2WZ\3\2\2\2XV\3\2\2\2XY\3\2\2\2Y\27\3\2\2\2ZX\3\2\2\2\f\36\"$\'\61"+
		"\659HVX";
	public static final ATN _ATN =
		new ATNDeserializer().deserialize(_serializedATN.toCharArray());
	static {
		_decisionToDFA = new DFA[_ATN.getNumberOfDecisions()];
		for (int i = 0; i < _ATN.getNumberOfDecisions(); i++) {
			_decisionToDFA[i] = new DFA(_ATN.getDecisionState(i), i);
		}
	}
}