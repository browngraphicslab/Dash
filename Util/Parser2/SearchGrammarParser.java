// Generated from SearchGrammar.g4 by ANTLR 4.7.1
import org.antlr.v4.runtime.atn.*;
import org.antlr.v4.runtime.dfa.DFA;
import org.antlr.v4.runtime.*;
import org.antlr.v4.runtime.misc.*;
import org.antlr.v4.runtime.tree.*;
import java.util.List;
import java.util.Iterator;
import java.util.ArrayList;

@SuppressWarnings({"all", "warnings", "unchecked", "unused", "cast"})
public class SearchGrammarParser extends Parser {
	static { RuntimeMetaData.checkVersion("4.7.1", RuntimeMetaData.VERSION); }

	protected static final DFA[] _decisionToDFA;
	protected static final PredictionContextCache _sharedContextCache =
		new PredictionContextCache();
	public static final int
		T__0=1, T__1=2, T__2=3, T__3=4, T__4=5, T__5=6, T__6=7, T__7=8, T__8=9, 
		NEWLINE=10, WORD=11, STRING=12, WS=13;
	public static final int
		RULE_value = 0, RULE_keylist = 1, RULE_kvsearch = 2, RULE_function = 3, 
		RULE_term = 4, RULE_negation = 5, RULE_or = 6, RULE_and = 7, RULE_query = 8;
	public static final String[] ruleNames = {
		"value", "keylist", "kvsearch", "function", "term", "negation", "or", 
		"and", "query"
	};

	private static final String[] _LITERAL_NAMES = {
		null, "'{'", "','", "'}'", "'-'", "':'", "'('", "')'", "'!'", "'|'"
	};
	private static final String[] _SYMBOLIC_NAMES = {
		null, null, null, null, null, null, null, null, null, null, "NEWLINE", 
		"WORD", "STRING", "WS"
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
	public String getGrammarFileName() { return "SearchGrammar.g4"; }

	@Override
	public String[] getRuleNames() { return ruleNames; }

	@Override
	public String getSerializedATN() { return _serializedATN; }

	@Override
	public ATN getATN() { return _ATN; }

	public SearchGrammarParser(TokenStream input) {
		super(input);
		_interp = new ParserATNSimulator(this,_ATN,_decisionToDFA,_sharedContextCache);
	}
	public static class ValueContext extends ParserRuleContext {
		public TerminalNode WORD() { return getToken(SearchGrammarParser.WORD, 0); }
		public TerminalNode STRING() { return getToken(SearchGrammarParser.STRING, 0); }
		public ValueContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_value; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof SearchGrammarListener ) ((SearchGrammarListener)listener).enterValue(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof SearchGrammarListener ) ((SearchGrammarListener)listener).exitValue(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof SearchGrammarVisitor ) return ((SearchGrammarVisitor<? extends T>)visitor).visitValue(this);
			else return visitor.visitChildren(this);
		}
	}

	public final ValueContext value() throws RecognitionException {
		ValueContext _localctx = new ValueContext(_ctx, getState());
		enterRule(_localctx, 0, RULE_value);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(18);
			_la = _input.LA(1);
			if ( !(_la==WORD || _la==STRING) ) {
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

	public static class KeylistContext extends ParserRuleContext {
		public List<ValueContext> value() {
			return getRuleContexts(ValueContext.class);
		}
		public ValueContext value(int i) {
			return getRuleContext(ValueContext.class,i);
		}
		public List<TerminalNode> WS() { return getTokens(SearchGrammarParser.WS); }
		public TerminalNode WS(int i) {
			return getToken(SearchGrammarParser.WS, i);
		}
		public KeylistContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_keylist; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof SearchGrammarListener ) ((SearchGrammarListener)listener).enterKeylist(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof SearchGrammarListener ) ((SearchGrammarListener)listener).exitKeylist(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof SearchGrammarVisitor ) return ((SearchGrammarVisitor<? extends T>)visitor).visitKeylist(this);
			else return visitor.visitChildren(this);
		}
	}

	public final KeylistContext keylist() throws RecognitionException {
		KeylistContext _localctx = new KeylistContext(_ctx, getState());
		enterRule(_localctx, 2, RULE_keylist);
		int _la;
		try {
			int _alt;
			setState(56);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case WORD:
			case STRING:
				enterOuterAlt(_localctx, 1);
				{
				setState(20);
				value();
				}
				break;
			case T__0:
				enterOuterAlt(_localctx, 2);
				{
				setState(21);
				match(T__0);
				setState(25);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==WS) {
					{
					{
					setState(22);
					match(WS);
					}
					}
					setState(27);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(44);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,3,_ctx);
				while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
					if ( _alt==1 ) {
						{
						{
						setState(28);
						value();
						setState(32);
						_errHandler.sync(this);
						_la = _input.LA(1);
						while (_la==WS) {
							{
							{
							setState(29);
							match(WS);
							}
							}
							setState(34);
							_errHandler.sync(this);
							_la = _input.LA(1);
						}
						setState(35);
						match(T__1);
						setState(39);
						_errHandler.sync(this);
						_la = _input.LA(1);
						while (_la==WS) {
							{
							{
							setState(36);
							match(WS);
							}
							}
							setState(41);
							_errHandler.sync(this);
							_la = _input.LA(1);
						}
						}
						} 
					}
					setState(46);
					_errHandler.sync(this);
					_alt = getInterpreter().adaptivePredict(_input,3,_ctx);
				}
				setState(47);
				value();
				setState(51);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==WS) {
					{
					{
					setState(48);
					match(WS);
					}
					}
					setState(53);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(54);
				match(T__2);
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

	public static class KvsearchContext extends ParserRuleContext {
		public KeylistContext keylist() {
			return getRuleContext(KeylistContext.class,0);
		}
		public ValueContext value() {
			return getRuleContext(ValueContext.class,0);
		}
		public KvsearchContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_kvsearch; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof SearchGrammarListener ) ((SearchGrammarListener)listener).enterKvsearch(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof SearchGrammarListener ) ((SearchGrammarListener)listener).exitKvsearch(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof SearchGrammarVisitor ) return ((SearchGrammarVisitor<? extends T>)visitor).visitKvsearch(this);
			else return visitor.visitChildren(this);
		}
	}

	public final KvsearchContext kvsearch() throws RecognitionException {
		KvsearchContext _localctx = new KvsearchContext(_ctx, getState());
		enterRule(_localctx, 4, RULE_kvsearch);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(59);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==T__3) {
				{
				setState(58);
				match(T__3);
				}
			}

			setState(61);
			keylist();
			setState(62);
			match(T__4);
			setState(63);
			value();
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

	public static class FunctionContext extends ParserRuleContext {
		public TerminalNode WORD() { return getToken(SearchGrammarParser.WORD, 0); }
		public FunctionContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_function; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof SearchGrammarListener ) ((SearchGrammarListener)listener).enterFunction(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof SearchGrammarListener ) ((SearchGrammarListener)listener).exitFunction(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof SearchGrammarVisitor ) return ((SearchGrammarVisitor<? extends T>)visitor).visitFunction(this);
			else return visitor.visitChildren(this);
		}
	}

	public final FunctionContext function() throws RecognitionException {
		FunctionContext _localctx = new FunctionContext(_ctx, getState());
		enterRule(_localctx, 6, RULE_function);
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(65);
			match(WORD);
			setState(66);
			match(T__5);
			setState(70);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,7,_ctx);
			while ( _alt!=1 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1+1 ) {
					{
					{
					setState(67);
					matchWildcard();
					}
					} 
				}
				setState(72);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,7,_ctx);
			}
			setState(73);
			match(T__6);
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

	public static class TermContext extends ParserRuleContext {
		public KvsearchContext kvsearch() {
			return getRuleContext(KvsearchContext.class,0);
		}
		public ValueContext value() {
			return getRuleContext(ValueContext.class,0);
		}
		public FunctionContext function() {
			return getRuleContext(FunctionContext.class,0);
		}
		public QueryContext query() {
			return getRuleContext(QueryContext.class,0);
		}
		public List<TerminalNode> WS() { return getTokens(SearchGrammarParser.WS); }
		public TerminalNode WS(int i) {
			return getToken(SearchGrammarParser.WS, i);
		}
		public TermContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_term; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof SearchGrammarListener ) ((SearchGrammarListener)listener).enterTerm(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof SearchGrammarListener ) ((SearchGrammarListener)listener).exitTerm(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof SearchGrammarVisitor ) return ((SearchGrammarVisitor<? extends T>)visitor).visitTerm(this);
			else return visitor.visitChildren(this);
		}
	}

	public final TermContext term() throws RecognitionException {
		TermContext _localctx = new TermContext(_ctx, getState());
		enterRule(_localctx, 8, RULE_term);
		int _la;
		try {
			setState(94);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,10,_ctx) ) {
			case 1:
				enterOuterAlt(_localctx, 1);
				{
				setState(75);
				kvsearch();
				}
				break;
			case 2:
				enterOuterAlt(_localctx, 2);
				{
				setState(76);
				value();
				}
				break;
			case 3:
				enterOuterAlt(_localctx, 3);
				{
				setState(77);
				function();
				}
				break;
			case 4:
				enterOuterAlt(_localctx, 4);
				{
				setState(78);
				match(T__5);
				setState(82);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==WS) {
					{
					{
					setState(79);
					match(WS);
					}
					}
					setState(84);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(85);
				query();
				setState(89);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==WS) {
					{
					{
					setState(86);
					match(WS);
					}
					}
					setState(91);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(92);
				match(T__6);
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

	public static class NegationContext extends ParserRuleContext {
		public TermContext term() {
			return getRuleContext(TermContext.class,0);
		}
		public NegationContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_negation; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof SearchGrammarListener ) ((SearchGrammarListener)listener).enterNegation(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof SearchGrammarListener ) ((SearchGrammarListener)listener).exitNegation(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof SearchGrammarVisitor ) return ((SearchGrammarVisitor<? extends T>)visitor).visitNegation(this);
			else return visitor.visitChildren(this);
		}
	}

	public final NegationContext negation() throws RecognitionException {
		NegationContext _localctx = new NegationContext(_ctx, getState());
		enterRule(_localctx, 10, RULE_negation);
		try {
			setState(99);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case T__0:
			case T__3:
			case T__5:
			case WORD:
			case STRING:
				enterOuterAlt(_localctx, 1);
				{
				setState(96);
				term();
				}
				break;
			case T__7:
				enterOuterAlt(_localctx, 2);
				{
				setState(97);
				match(T__7);
				setState(98);
				term();
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

	public static class OrContext extends ParserRuleContext {
		public List<NegationContext> negation() {
			return getRuleContexts(NegationContext.class);
		}
		public NegationContext negation(int i) {
			return getRuleContext(NegationContext.class,i);
		}
		public List<TerminalNode> WS() { return getTokens(SearchGrammarParser.WS); }
		public TerminalNode WS(int i) {
			return getToken(SearchGrammarParser.WS, i);
		}
		public OrContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_or; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof SearchGrammarListener ) ((SearchGrammarListener)listener).enterOr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof SearchGrammarListener ) ((SearchGrammarListener)listener).exitOr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof SearchGrammarVisitor ) return ((SearchGrammarVisitor<? extends T>)visitor).visitOr(this);
			else return visitor.visitChildren(this);
		}
	}

	public final OrContext or() throws RecognitionException {
		OrContext _localctx = new OrContext(_ctx, getState());
		enterRule(_localctx, 12, RULE_or);
		int _la;
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(117);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,14,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					{
					{
					setState(101);
					negation();
					setState(105);
					_errHandler.sync(this);
					_la = _input.LA(1);
					while (_la==WS) {
						{
						{
						setState(102);
						match(WS);
						}
						}
						setState(107);
						_errHandler.sync(this);
						_la = _input.LA(1);
					}
					setState(108);
					match(T__8);
					setState(112);
					_errHandler.sync(this);
					_la = _input.LA(1);
					while (_la==WS) {
						{
						{
						setState(109);
						match(WS);
						}
						}
						setState(114);
						_errHandler.sync(this);
						_la = _input.LA(1);
					}
					}
					} 
				}
				setState(119);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,14,_ctx);
			}
			setState(120);
			negation();
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

	public static class AndContext extends ParserRuleContext {
		public List<OrContext> or() {
			return getRuleContexts(OrContext.class);
		}
		public OrContext or(int i) {
			return getRuleContext(OrContext.class,i);
		}
		public List<TerminalNode> WS() { return getTokens(SearchGrammarParser.WS); }
		public TerminalNode WS(int i) {
			return getToken(SearchGrammarParser.WS, i);
		}
		public AndContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_and; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof SearchGrammarListener ) ((SearchGrammarListener)listener).enterAnd(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof SearchGrammarListener ) ((SearchGrammarListener)listener).exitAnd(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof SearchGrammarVisitor ) return ((SearchGrammarVisitor<? extends T>)visitor).visitAnd(this);
			else return visitor.visitChildren(this);
		}
	}

	public final AndContext and() throws RecognitionException {
		AndContext _localctx = new AndContext(_ctx, getState());
		enterRule(_localctx, 14, RULE_and);
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(127);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,15,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					{
					{
					setState(122);
					or();
					setState(123);
					match(WS);
					}
					} 
				}
				setState(129);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,15,_ctx);
			}
			setState(130);
			or();
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
		public AndContext and() {
			return getRuleContext(AndContext.class,0);
		}
		public QueryContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_query; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof SearchGrammarListener ) ((SearchGrammarListener)listener).enterQuery(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof SearchGrammarListener ) ((SearchGrammarListener)listener).exitQuery(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof SearchGrammarVisitor ) return ((SearchGrammarVisitor<? extends T>)visitor).visitQuery(this);
			else return visitor.visitChildren(this);
		}
	}

	public final QueryContext query() throws RecognitionException {
		QueryContext _localctx = new QueryContext(_ctx, getState());
		enterRule(_localctx, 16, RULE_query);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(132);
			and();
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

	public static final String _serializedATN =
		"\3\u608b\ua72a\u8133\ub9ed\u417c\u3be7\u7786\u5964\3\17\u0089\4\2\t\2"+
		"\4\3\t\3\4\4\t\4\4\5\t\5\4\6\t\6\4\7\t\7\4\b\t\b\4\t\t\t\4\n\t\n\3\2\3"+
		"\2\3\3\3\3\3\3\7\3\32\n\3\f\3\16\3\35\13\3\3\3\3\3\7\3!\n\3\f\3\16\3$"+
		"\13\3\3\3\3\3\7\3(\n\3\f\3\16\3+\13\3\7\3-\n\3\f\3\16\3\60\13\3\3\3\3"+
		"\3\7\3\64\n\3\f\3\16\3\67\13\3\3\3\3\3\5\3;\n\3\3\4\5\4>\n\4\3\4\3\4\3"+
		"\4\3\4\3\5\3\5\3\5\7\5G\n\5\f\5\16\5J\13\5\3\5\3\5\3\6\3\6\3\6\3\6\3\6"+
		"\7\6S\n\6\f\6\16\6V\13\6\3\6\3\6\7\6Z\n\6\f\6\16\6]\13\6\3\6\3\6\5\6a"+
		"\n\6\3\7\3\7\3\7\5\7f\n\7\3\b\3\b\7\bj\n\b\f\b\16\bm\13\b\3\b\3\b\7\b"+
		"q\n\b\f\b\16\bt\13\b\7\bv\n\b\f\b\16\by\13\b\3\b\3\b\3\t\3\t\3\t\7\t\u0080"+
		"\n\t\f\t\16\t\u0083\13\t\3\t\3\t\3\n\3\n\3\n\3H\2\13\2\4\6\b\n\f\16\20"+
		"\22\2\3\3\2\r\16\2\u0091\2\24\3\2\2\2\4:\3\2\2\2\6=\3\2\2\2\bC\3\2\2\2"+
		"\n`\3\2\2\2\fe\3\2\2\2\16w\3\2\2\2\20\u0081\3\2\2\2\22\u0086\3\2\2\2\24"+
		"\25\t\2\2\2\25\3\3\2\2\2\26;\5\2\2\2\27\33\7\3\2\2\30\32\7\17\2\2\31\30"+
		"\3\2\2\2\32\35\3\2\2\2\33\31\3\2\2\2\33\34\3\2\2\2\34.\3\2\2\2\35\33\3"+
		"\2\2\2\36\"\5\2\2\2\37!\7\17\2\2 \37\3\2\2\2!$\3\2\2\2\" \3\2\2\2\"#\3"+
		"\2\2\2#%\3\2\2\2$\"\3\2\2\2%)\7\4\2\2&(\7\17\2\2\'&\3\2\2\2(+\3\2\2\2"+
		")\'\3\2\2\2)*\3\2\2\2*-\3\2\2\2+)\3\2\2\2,\36\3\2\2\2-\60\3\2\2\2.,\3"+
		"\2\2\2./\3\2\2\2/\61\3\2\2\2\60.\3\2\2\2\61\65\5\2\2\2\62\64\7\17\2\2"+
		"\63\62\3\2\2\2\64\67\3\2\2\2\65\63\3\2\2\2\65\66\3\2\2\2\668\3\2\2\2\67"+
		"\65\3\2\2\289\7\5\2\29;\3\2\2\2:\26\3\2\2\2:\27\3\2\2\2;\5\3\2\2\2<>\7"+
		"\6\2\2=<\3\2\2\2=>\3\2\2\2>?\3\2\2\2?@\5\4\3\2@A\7\7\2\2AB\5\2\2\2B\7"+
		"\3\2\2\2CD\7\r\2\2DH\7\b\2\2EG\13\2\2\2FE\3\2\2\2GJ\3\2\2\2HI\3\2\2\2"+
		"HF\3\2\2\2IK\3\2\2\2JH\3\2\2\2KL\7\t\2\2L\t\3\2\2\2Ma\5\6\4\2Na\5\2\2"+
		"\2Oa\5\b\5\2PT\7\b\2\2QS\7\17\2\2RQ\3\2\2\2SV\3\2\2\2TR\3\2\2\2TU\3\2"+
		"\2\2UW\3\2\2\2VT\3\2\2\2W[\5\22\n\2XZ\7\17\2\2YX\3\2\2\2Z]\3\2\2\2[Y\3"+
		"\2\2\2[\\\3\2\2\2\\^\3\2\2\2][\3\2\2\2^_\7\t\2\2_a\3\2\2\2`M\3\2\2\2`"+
		"N\3\2\2\2`O\3\2\2\2`P\3\2\2\2a\13\3\2\2\2bf\5\n\6\2cd\7\n\2\2df\5\n\6"+
		"\2eb\3\2\2\2ec\3\2\2\2f\r\3\2\2\2gk\5\f\7\2hj\7\17\2\2ih\3\2\2\2jm\3\2"+
		"\2\2ki\3\2\2\2kl\3\2\2\2ln\3\2\2\2mk\3\2\2\2nr\7\13\2\2oq\7\17\2\2po\3"+
		"\2\2\2qt\3\2\2\2rp\3\2\2\2rs\3\2\2\2sv\3\2\2\2tr\3\2\2\2ug\3\2\2\2vy\3"+
		"\2\2\2wu\3\2\2\2wx\3\2\2\2xz\3\2\2\2yw\3\2\2\2z{\5\f\7\2{\17\3\2\2\2|"+
		"}\5\16\b\2}~\7\17\2\2~\u0080\3\2\2\2\177|\3\2\2\2\u0080\u0083\3\2\2\2"+
		"\u0081\177\3\2\2\2\u0081\u0082\3\2\2\2\u0082\u0084\3\2\2\2\u0083\u0081"+
		"\3\2\2\2\u0084\u0085\5\16\b\2\u0085\21\3\2\2\2\u0086\u0087\5\20\t\2\u0087"+
		"\23\3\2\2\2\22\33\").\65:=HT[`ekrw\u0081";
	public static final ATN _ATN =
		new ATNDeserializer().deserialize(_serializedATN.toCharArray());
	static {
		_decisionToDFA = new DFA[_ATN.getNumberOfDecisions()];
		for (int i = 0; i < _ATN.getNumberOfDecisions(); i++) {
			_decisionToDFA[i] = new DFA(_ATN.getDecisionState(i), i);
		}
	}
}