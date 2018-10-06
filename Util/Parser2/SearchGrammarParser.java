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
		RULE_value = 0, RULE_keylist = 1, RULE_kvsearch = 2, RULE_args = 3, RULE_function = 4, 
		RULE_term = 5, RULE_negation = 6, RULE_or = 7, RULE_and = 8, RULE_query = 9;
	public static final String[] ruleNames = {
		"value", "keylist", "kvsearch", "args", "function", "term", "negation", 
		"or", "and", "query"
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
			setState(20);
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
			setState(58);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case WORD:
			case STRING:
				enterOuterAlt(_localctx, 1);
				{
				setState(22);
				value();
				}
				break;
			case T__0:
				enterOuterAlt(_localctx, 2);
				{
				setState(23);
				match(T__0);
				setState(27);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==WS) {
					{
					{
					setState(24);
					match(WS);
					}
					}
					setState(29);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(46);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,3,_ctx);
				while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
					if ( _alt==1 ) {
						{
						{
						setState(30);
						value();
						setState(34);
						_errHandler.sync(this);
						_la = _input.LA(1);
						while (_la==WS) {
							{
							{
							setState(31);
							match(WS);
							}
							}
							setState(36);
							_errHandler.sync(this);
							_la = _input.LA(1);
						}
						setState(37);
						match(T__1);
						setState(41);
						_errHandler.sync(this);
						_la = _input.LA(1);
						while (_la==WS) {
							{
							{
							setState(38);
							match(WS);
							}
							}
							setState(43);
							_errHandler.sync(this);
							_la = _input.LA(1);
						}
						}
						} 
					}
					setState(48);
					_errHandler.sync(this);
					_alt = getInterpreter().adaptivePredict(_input,3,_ctx);
				}
				setState(49);
				value();
				setState(53);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==WS) {
					{
					{
					setState(50);
					match(WS);
					}
					}
					setState(55);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(56);
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
			setState(61);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==T__3) {
				{
				setState(60);
				match(T__3);
				}
			}

			setState(63);
			keylist();
			setState(64);
			match(T__4);
			setState(65);
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

	public static class ArgsContext extends ParserRuleContext {
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
		public ArgsContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_args; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof SearchGrammarListener ) ((SearchGrammarListener)listener).enterArgs(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof SearchGrammarListener ) ((SearchGrammarListener)listener).exitArgs(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof SearchGrammarVisitor ) return ((SearchGrammarVisitor<? extends T>)visitor).visitArgs(this);
			else return visitor.visitChildren(this);
		}
	}

	public final ArgsContext args() throws RecognitionException {
		ArgsContext _localctx = new ArgsContext(_ctx, getState());
		enterRule(_localctx, 6, RULE_args);
		int _la;
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(83);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,9,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					{
					{
					setState(67);
					value();
					setState(71);
					_errHandler.sync(this);
					_la = _input.LA(1);
					while (_la==WS) {
						{
						{
						setState(68);
						match(WS);
						}
						}
						setState(73);
						_errHandler.sync(this);
						_la = _input.LA(1);
					}
					setState(74);
					match(T__1);
					setState(78);
					_errHandler.sync(this);
					_alt = getInterpreter().adaptivePredict(_input,8,_ctx);
					while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
						if ( _alt==1 ) {
							{
							{
							setState(75);
							match(WS);
							}
							} 
						}
						setState(80);
						_errHandler.sync(this);
						_alt = getInterpreter().adaptivePredict(_input,8,_ctx);
					}
					}
					} 
				}
				setState(85);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,9,_ctx);
			}
			setState(87);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==WORD || _la==STRING) {
				{
				setState(86);
				value();
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

	public static class FunctionContext extends ParserRuleContext {
		public TerminalNode WORD() { return getToken(SearchGrammarParser.WORD, 0); }
		public ArgsContext args() {
			return getRuleContext(ArgsContext.class,0);
		}
		public List<TerminalNode> WS() { return getTokens(SearchGrammarParser.WS); }
		public TerminalNode WS(int i) {
			return getToken(SearchGrammarParser.WS, i);
		}
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
		enterRule(_localctx, 8, RULE_function);
		int _la;
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(89);
			match(WORD);
			setState(90);
			match(T__5);
			setState(94);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,11,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					{
					{
					setState(91);
					match(WS);
					}
					} 
				}
				setState(96);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,11,_ctx);
			}
			setState(97);
			args();
			setState(101);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==WS) {
				{
				{
				setState(98);
				match(WS);
				}
				}
				setState(103);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(104);
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
		enterRule(_localctx, 10, RULE_term);
		int _la;
		try {
			setState(125);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,15,_ctx) ) {
			case 1:
				enterOuterAlt(_localctx, 1);
				{
				setState(106);
				kvsearch();
				}
				break;
			case 2:
				enterOuterAlt(_localctx, 2);
				{
				setState(107);
				value();
				}
				break;
			case 3:
				enterOuterAlt(_localctx, 3);
				{
				setState(108);
				function();
				}
				break;
			case 4:
				enterOuterAlt(_localctx, 4);
				{
				setState(109);
				match(T__5);
				setState(113);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==WS) {
					{
					{
					setState(110);
					match(WS);
					}
					}
					setState(115);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(116);
				query();
				setState(120);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==WS) {
					{
					{
					setState(117);
					match(WS);
					}
					}
					setState(122);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(123);
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
		enterRule(_localctx, 12, RULE_negation);
		try {
			setState(130);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case T__0:
			case T__3:
			case T__5:
			case WORD:
			case STRING:
				enterOuterAlt(_localctx, 1);
				{
				setState(127);
				term();
				}
				break;
			case T__7:
				enterOuterAlt(_localctx, 2);
				{
				setState(128);
				match(T__7);
				setState(129);
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
		enterRule(_localctx, 14, RULE_or);
		int _la;
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(148);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,19,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					{
					{
					setState(132);
					negation();
					setState(136);
					_errHandler.sync(this);
					_la = _input.LA(1);
					while (_la==WS) {
						{
						{
						setState(133);
						match(WS);
						}
						}
						setState(138);
						_errHandler.sync(this);
						_la = _input.LA(1);
					}
					setState(139);
					match(T__8);
					setState(143);
					_errHandler.sync(this);
					_la = _input.LA(1);
					while (_la==WS) {
						{
						{
						setState(140);
						match(WS);
						}
						}
						setState(145);
						_errHandler.sync(this);
						_la = _input.LA(1);
					}
					}
					} 
				}
				setState(150);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,19,_ctx);
			}
			setState(151);
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
		enterRule(_localctx, 16, RULE_and);
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(158);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,20,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					{
					{
					setState(153);
					or();
					setState(154);
					match(WS);
					}
					} 
				}
				setState(160);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,20,_ctx);
			}
			setState(161);
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
		enterRule(_localctx, 18, RULE_query);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(163);
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
		"\3\u608b\ua72a\u8133\ub9ed\u417c\u3be7\u7786\u5964\3\17\u00a8\4\2\t\2"+
		"\4\3\t\3\4\4\t\4\4\5\t\5\4\6\t\6\4\7\t\7\4\b\t\b\4\t\t\t\4\n\t\n\4\13"+
		"\t\13\3\2\3\2\3\3\3\3\3\3\7\3\34\n\3\f\3\16\3\37\13\3\3\3\3\3\7\3#\n\3"+
		"\f\3\16\3&\13\3\3\3\3\3\7\3*\n\3\f\3\16\3-\13\3\7\3/\n\3\f\3\16\3\62\13"+
		"\3\3\3\3\3\7\3\66\n\3\f\3\16\39\13\3\3\3\3\3\5\3=\n\3\3\4\5\4@\n\4\3\4"+
		"\3\4\3\4\3\4\3\5\3\5\7\5H\n\5\f\5\16\5K\13\5\3\5\3\5\7\5O\n\5\f\5\16\5"+
		"R\13\5\7\5T\n\5\f\5\16\5W\13\5\3\5\5\5Z\n\5\3\6\3\6\3\6\7\6_\n\6\f\6\16"+
		"\6b\13\6\3\6\3\6\7\6f\n\6\f\6\16\6i\13\6\3\6\3\6\3\7\3\7\3\7\3\7\3\7\7"+
		"\7r\n\7\f\7\16\7u\13\7\3\7\3\7\7\7y\n\7\f\7\16\7|\13\7\3\7\3\7\5\7\u0080"+
		"\n\7\3\b\3\b\3\b\5\b\u0085\n\b\3\t\3\t\7\t\u0089\n\t\f\t\16\t\u008c\13"+
		"\t\3\t\3\t\7\t\u0090\n\t\f\t\16\t\u0093\13\t\7\t\u0095\n\t\f\t\16\t\u0098"+
		"\13\t\3\t\3\t\3\n\3\n\3\n\7\n\u009f\n\n\f\n\16\n\u00a2\13\n\3\n\3\n\3"+
		"\13\3\13\3\13\2\2\f\2\4\6\b\n\f\16\20\22\24\2\3\3\2\r\16\2\u00b4\2\26"+
		"\3\2\2\2\4<\3\2\2\2\6?\3\2\2\2\bU\3\2\2\2\n[\3\2\2\2\f\177\3\2\2\2\16"+
		"\u0084\3\2\2\2\20\u0096\3\2\2\2\22\u00a0\3\2\2\2\24\u00a5\3\2\2\2\26\27"+
		"\t\2\2\2\27\3\3\2\2\2\30=\5\2\2\2\31\35\7\3\2\2\32\34\7\17\2\2\33\32\3"+
		"\2\2\2\34\37\3\2\2\2\35\33\3\2\2\2\35\36\3\2\2\2\36\60\3\2\2\2\37\35\3"+
		"\2\2\2 $\5\2\2\2!#\7\17\2\2\"!\3\2\2\2#&\3\2\2\2$\"\3\2\2\2$%\3\2\2\2"+
		"%\'\3\2\2\2&$\3\2\2\2\'+\7\4\2\2(*\7\17\2\2)(\3\2\2\2*-\3\2\2\2+)\3\2"+
		"\2\2+,\3\2\2\2,/\3\2\2\2-+\3\2\2\2. \3\2\2\2/\62\3\2\2\2\60.\3\2\2\2\60"+
		"\61\3\2\2\2\61\63\3\2\2\2\62\60\3\2\2\2\63\67\5\2\2\2\64\66\7\17\2\2\65"+
		"\64\3\2\2\2\669\3\2\2\2\67\65\3\2\2\2\678\3\2\2\28:\3\2\2\29\67\3\2\2"+
		"\2:;\7\5\2\2;=\3\2\2\2<\30\3\2\2\2<\31\3\2\2\2=\5\3\2\2\2>@\7\6\2\2?>"+
		"\3\2\2\2?@\3\2\2\2@A\3\2\2\2AB\5\4\3\2BC\7\7\2\2CD\5\2\2\2D\7\3\2\2\2"+
		"EI\5\2\2\2FH\7\17\2\2GF\3\2\2\2HK\3\2\2\2IG\3\2\2\2IJ\3\2\2\2JL\3\2\2"+
		"\2KI\3\2\2\2LP\7\4\2\2MO\7\17\2\2NM\3\2\2\2OR\3\2\2\2PN\3\2\2\2PQ\3\2"+
		"\2\2QT\3\2\2\2RP\3\2\2\2SE\3\2\2\2TW\3\2\2\2US\3\2\2\2UV\3\2\2\2VY\3\2"+
		"\2\2WU\3\2\2\2XZ\5\2\2\2YX\3\2\2\2YZ\3\2\2\2Z\t\3\2\2\2[\\\7\r\2\2\\`"+
		"\7\b\2\2]_\7\17\2\2^]\3\2\2\2_b\3\2\2\2`^\3\2\2\2`a\3\2\2\2ac\3\2\2\2"+
		"b`\3\2\2\2cg\5\b\5\2df\7\17\2\2ed\3\2\2\2fi\3\2\2\2ge\3\2\2\2gh\3\2\2"+
		"\2hj\3\2\2\2ig\3\2\2\2jk\7\t\2\2k\13\3\2\2\2l\u0080\5\6\4\2m\u0080\5\2"+
		"\2\2n\u0080\5\n\6\2os\7\b\2\2pr\7\17\2\2qp\3\2\2\2ru\3\2\2\2sq\3\2\2\2"+
		"st\3\2\2\2tv\3\2\2\2us\3\2\2\2vz\5\24\13\2wy\7\17\2\2xw\3\2\2\2y|\3\2"+
		"\2\2zx\3\2\2\2z{\3\2\2\2{}\3\2\2\2|z\3\2\2\2}~\7\t\2\2~\u0080\3\2\2\2"+
		"\177l\3\2\2\2\177m\3\2\2\2\177n\3\2\2\2\177o\3\2\2\2\u0080\r\3\2\2\2\u0081"+
		"\u0085\5\f\7\2\u0082\u0083\7\n\2\2\u0083\u0085\5\f\7\2\u0084\u0081\3\2"+
		"\2\2\u0084\u0082\3\2\2\2\u0085\17\3\2\2\2\u0086\u008a\5\16\b\2\u0087\u0089"+
		"\7\17\2\2\u0088\u0087\3\2\2\2\u0089\u008c\3\2\2\2\u008a\u0088\3\2\2\2"+
		"\u008a\u008b\3\2\2\2\u008b\u008d\3\2\2\2\u008c\u008a\3\2\2\2\u008d\u0091"+
		"\7\13\2\2\u008e\u0090\7\17\2\2\u008f\u008e\3\2\2\2\u0090\u0093\3\2\2\2"+
		"\u0091\u008f\3\2\2\2\u0091\u0092\3\2\2\2\u0092\u0095\3\2\2\2\u0093\u0091"+
		"\3\2\2\2\u0094\u0086\3\2\2\2\u0095\u0098\3\2\2\2\u0096\u0094\3\2\2\2\u0096"+
		"\u0097\3\2\2\2\u0097\u0099\3\2\2\2\u0098\u0096\3\2\2\2\u0099\u009a\5\16"+
		"\b\2\u009a\21\3\2\2\2\u009b\u009c\5\20\t\2\u009c\u009d\7\17\2\2\u009d"+
		"\u009f\3\2\2\2\u009e\u009b\3\2\2\2\u009f\u00a2\3\2\2\2\u00a0\u009e\3\2"+
		"\2\2\u00a0\u00a1\3\2\2\2\u00a1\u00a3\3\2\2\2\u00a2\u00a0\3\2\2\2\u00a3"+
		"\u00a4\5\20\t\2\u00a4\23\3\2\2\2\u00a5\u00a6\5\22\n\2\u00a6\25\3\2\2\2"+
		"\27\35$+\60\67<?IPUY`gsz\177\u0084\u008a\u0091\u0096\u00a0";
	public static final ATN _ATN =
		new ATNDeserializer().deserialize(_serializedATN.toCharArray());
	static {
		_decisionToDFA = new DFA[_ATN.getNumberOfDecisions()];
		for (int i = 0; i < _ATN.getNumberOfDecisions(); i++) {
			_decisionToDFA[i] = new DFA(_ATN.getDecisionState(i), i);
		}
	}
}