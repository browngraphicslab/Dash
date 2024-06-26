//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.7.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from SearchGrammar.g4 by ANTLR 4.7.1

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using DFA = Antlr4.Runtime.Dfa.DFA;

[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.7.1")]
[System.CLSCompliant(false)]
public partial class SearchGrammarParser : Parser {
	protected static DFA[] decisionToDFA;
	protected static PredictionContextCache sharedContextCache = new PredictionContextCache();
	public const int
		T__0=1, T__1=2, T__2=3, T__3=4, T__4=5, T__5=6, T__6=7, T__7=8, T__8=9, 
		NEWLINE=10, WORD=11, STRING=12, WS=13;
	public const int
		RULE_value = 0, RULE_keylist = 1, RULE_kvsearch = 2, RULE_function = 3, 
		RULE_term = 4, RULE_negation = 5, RULE_or = 6, RULE_and = 7, RULE_query = 8;
	public static readonly string[] ruleNames = {
		"value", "keylist", "kvsearch", "function", "term", "negation", "or", 
		"and", "query"
	};

	private static readonly string[] _LiteralNames = {
		null, "'{'", "','", "'}'", "'-'", "':'", "'('", "')'", "'!'", "'|'"
	};
	private static readonly string[] _SymbolicNames = {
		null, null, null, null, null, null, null, null, null, null, "NEWLINE", 
		"WORD", "STRING", "WS"
	};
	public static readonly IVocabulary DefaultVocabulary = new Vocabulary(_LiteralNames, _SymbolicNames);

	[NotNull]
	public override IVocabulary Vocabulary
	{
		get
		{
			return DefaultVocabulary;
		}
	}

	public override string GrammarFileName { get { return "SearchGrammar.g4"; } }

	public override string[] RuleNames { get { return ruleNames; } }

	public override string SerializedAtn { get { return new string(_serializedATN); } }

	static SearchGrammarParser() {
		decisionToDFA = new DFA[_ATN.NumberOfDecisions];
		for (int i = 0; i < _ATN.NumberOfDecisions; i++) {
			decisionToDFA[i] = new DFA(_ATN.GetDecisionState(i), i);
		}
	}

		public SearchGrammarParser(ITokenStream input) : this(input, Console.Out, Console.Error) { }

		public SearchGrammarParser(ITokenStream input, TextWriter output, TextWriter errorOutput)
		: base(input, output, errorOutput)
	{
		Interpreter = new ParserATNSimulator(this, _ATN, decisionToDFA, sharedContextCache);
	}
	public partial class ValueContext : ParserRuleContext {
		public ITerminalNode WORD() { return GetToken(SearchGrammarParser.WORD, 0); }
		public ITerminalNode STRING() { return GetToken(SearchGrammarParser.STRING, 0); }
		public ValueContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_value; } }
		public override void EnterRule(IParseTreeListener listener) {
			ISearchGrammarListener typedListener = listener as ISearchGrammarListener;
			if (typedListener != null) typedListener.EnterValue(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ISearchGrammarListener typedListener = listener as ISearchGrammarListener;
			if (typedListener != null) typedListener.ExitValue(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ISearchGrammarVisitor<TResult> typedVisitor = visitor as ISearchGrammarVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitValue(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public ValueContext value() {
		ValueContext _localctx = new ValueContext(Context, State);
		EnterRule(_localctx, 0, RULE_value);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 18;
			_la = TokenStream.LA(1);
			if ( !(_la==WORD || _la==STRING) ) {
			ErrorHandler.RecoverInline(this);
			}
			else {
				ErrorHandler.ReportMatch(this);
			    Consume();
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class KeylistContext : ParserRuleContext {
		public ValueContext[] value() {
			return GetRuleContexts<ValueContext>();
		}
		public ValueContext value(int i) {
			return GetRuleContext<ValueContext>(i);
		}
		public ITerminalNode[] WS() { return GetTokens(SearchGrammarParser.WS); }
		public ITerminalNode WS(int i) {
			return GetToken(SearchGrammarParser.WS, i);
		}
		public KeylistContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_keylist; } }
		public override void EnterRule(IParseTreeListener listener) {
			ISearchGrammarListener typedListener = listener as ISearchGrammarListener;
			if (typedListener != null) typedListener.EnterKeylist(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ISearchGrammarListener typedListener = listener as ISearchGrammarListener;
			if (typedListener != null) typedListener.ExitKeylist(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ISearchGrammarVisitor<TResult> typedVisitor = visitor as ISearchGrammarVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitKeylist(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public KeylistContext keylist() {
		KeylistContext _localctx = new KeylistContext(Context, State);
		EnterRule(_localctx, 2, RULE_keylist);
		int _la;
		try {
			int _alt;
			State = 56;
			ErrorHandler.Sync(this);
			switch (TokenStream.LA(1)) {
			case WORD:
			case STRING:
				EnterOuterAlt(_localctx, 1);
				{
				State = 20; value();
				}
				break;
			case T__0:
				EnterOuterAlt(_localctx, 2);
				{
				State = 21; Match(T__0);
				State = 25;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
				while (_la==WS) {
					{
					{
					State = 22; Match(WS);
					}
					}
					State = 27;
					ErrorHandler.Sync(this);
					_la = TokenStream.LA(1);
				}
				State = 44;
				ErrorHandler.Sync(this);
				_alt = Interpreter.AdaptivePredict(TokenStream,3,Context);
				while ( _alt!=2 && _alt!=global::Antlr4.Runtime.Atn.ATN.INVALID_ALT_NUMBER ) {
					if ( _alt==1 ) {
						{
						{
						State = 28; value();
						State = 32;
						ErrorHandler.Sync(this);
						_la = TokenStream.LA(1);
						while (_la==WS) {
							{
							{
							State = 29; Match(WS);
							}
							}
							State = 34;
							ErrorHandler.Sync(this);
							_la = TokenStream.LA(1);
						}
						State = 35; Match(T__1);
						State = 39;
						ErrorHandler.Sync(this);
						_la = TokenStream.LA(1);
						while (_la==WS) {
							{
							{
							State = 36; Match(WS);
							}
							}
							State = 41;
							ErrorHandler.Sync(this);
							_la = TokenStream.LA(1);
						}
						}
						} 
					}
					State = 46;
					ErrorHandler.Sync(this);
					_alt = Interpreter.AdaptivePredict(TokenStream,3,Context);
				}
				State = 47; value();
				State = 51;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
				while (_la==WS) {
					{
					{
					State = 48; Match(WS);
					}
					}
					State = 53;
					ErrorHandler.Sync(this);
					_la = TokenStream.LA(1);
				}
				State = 54; Match(T__2);
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class KvsearchContext : ParserRuleContext {
		public KeylistContext keylist() {
			return GetRuleContext<KeylistContext>(0);
		}
		public ValueContext value() {
			return GetRuleContext<ValueContext>(0);
		}
		public KvsearchContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_kvsearch; } }
		public override void EnterRule(IParseTreeListener listener) {
			ISearchGrammarListener typedListener = listener as ISearchGrammarListener;
			if (typedListener != null) typedListener.EnterKvsearch(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ISearchGrammarListener typedListener = listener as ISearchGrammarListener;
			if (typedListener != null) typedListener.ExitKvsearch(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ISearchGrammarVisitor<TResult> typedVisitor = visitor as ISearchGrammarVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitKvsearch(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public KvsearchContext kvsearch() {
		KvsearchContext _localctx = new KvsearchContext(Context, State);
		EnterRule(_localctx, 4, RULE_kvsearch);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 59;
			ErrorHandler.Sync(this);
			_la = TokenStream.LA(1);
			if (_la==T__3) {
				{
				State = 58; Match(T__3);
				}
			}

			State = 61; keylist();
			State = 62; Match(T__4);
			State = 63; value();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class FunctionContext : ParserRuleContext {
		public ITerminalNode WORD() { return GetToken(SearchGrammarParser.WORD, 0); }
		public FunctionContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_function; } }
		public override void EnterRule(IParseTreeListener listener) {
			ISearchGrammarListener typedListener = listener as ISearchGrammarListener;
			if (typedListener != null) typedListener.EnterFunction(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ISearchGrammarListener typedListener = listener as ISearchGrammarListener;
			if (typedListener != null) typedListener.ExitFunction(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ISearchGrammarVisitor<TResult> typedVisitor = visitor as ISearchGrammarVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitFunction(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public FunctionContext function() {
		FunctionContext _localctx = new FunctionContext(Context, State);
		EnterRule(_localctx, 6, RULE_function);
		try {
			int _alt;
			EnterOuterAlt(_localctx, 1);
			{
			State = 65; Match(WORD);
			State = 66; Match(T__5);
			State = 70;
			ErrorHandler.Sync(this);
			_alt = Interpreter.AdaptivePredict(TokenStream,7,Context);
			while ( _alt!=1 && _alt!=global::Antlr4.Runtime.Atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1+1 ) {
					{
					{
					State = 67;
					MatchWildcard();
					}
					} 
				}
				State = 72;
				ErrorHandler.Sync(this);
				_alt = Interpreter.AdaptivePredict(TokenStream,7,Context);
			}
			State = 73; Match(T__6);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class TermContext : ParserRuleContext {
		public KvsearchContext kvsearch() {
			return GetRuleContext<KvsearchContext>(0);
		}
		public ValueContext value() {
			return GetRuleContext<ValueContext>(0);
		}
		public FunctionContext function() {
			return GetRuleContext<FunctionContext>(0);
		}
		public QueryContext query() {
			return GetRuleContext<QueryContext>(0);
		}
		public ITerminalNode[] WS() { return GetTokens(SearchGrammarParser.WS); }
		public ITerminalNode WS(int i) {
			return GetToken(SearchGrammarParser.WS, i);
		}
		public TermContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_term; } }
		public override void EnterRule(IParseTreeListener listener) {
			ISearchGrammarListener typedListener = listener as ISearchGrammarListener;
			if (typedListener != null) typedListener.EnterTerm(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ISearchGrammarListener typedListener = listener as ISearchGrammarListener;
			if (typedListener != null) typedListener.ExitTerm(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ISearchGrammarVisitor<TResult> typedVisitor = visitor as ISearchGrammarVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitTerm(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public TermContext term() {
		TermContext _localctx = new TermContext(Context, State);
		EnterRule(_localctx, 8, RULE_term);
		int _la;
		try {
			State = 94;
			ErrorHandler.Sync(this);
			switch ( Interpreter.AdaptivePredict(TokenStream,10,Context) ) {
			case 1:
				EnterOuterAlt(_localctx, 1);
				{
				State = 75; kvsearch();
				}
				break;
			case 2:
				EnterOuterAlt(_localctx, 2);
				{
				State = 76; value();
				}
				break;
			case 3:
				EnterOuterAlt(_localctx, 3);
				{
				State = 77; function();
				}
				break;
			case 4:
				EnterOuterAlt(_localctx, 4);
				{
				State = 78; Match(T__5);
				State = 82;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
				while (_la==WS) {
					{
					{
					State = 79; Match(WS);
					}
					}
					State = 84;
					ErrorHandler.Sync(this);
					_la = TokenStream.LA(1);
				}
				State = 85; query();
				State = 89;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
				while (_la==WS) {
					{
					{
					State = 86; Match(WS);
					}
					}
					State = 91;
					ErrorHandler.Sync(this);
					_la = TokenStream.LA(1);
				}
				State = 92; Match(T__6);
				}
				break;
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class NegationContext : ParserRuleContext {
		public TermContext term() {
			return GetRuleContext<TermContext>(0);
		}
		public NegationContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_negation; } }
		public override void EnterRule(IParseTreeListener listener) {
			ISearchGrammarListener typedListener = listener as ISearchGrammarListener;
			if (typedListener != null) typedListener.EnterNegation(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ISearchGrammarListener typedListener = listener as ISearchGrammarListener;
			if (typedListener != null) typedListener.ExitNegation(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ISearchGrammarVisitor<TResult> typedVisitor = visitor as ISearchGrammarVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitNegation(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public NegationContext negation() {
		NegationContext _localctx = new NegationContext(Context, State);
		EnterRule(_localctx, 10, RULE_negation);
		try {
			State = 99;
			ErrorHandler.Sync(this);
			switch (TokenStream.LA(1)) {
			case T__0:
			case T__3:
			case T__5:
			case WORD:
			case STRING:
				EnterOuterAlt(_localctx, 1);
				{
				State = 96; term();
				}
				break;
			case T__7:
				EnterOuterAlt(_localctx, 2);
				{
				State = 97; Match(T__7);
				State = 98; term();
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class OrContext : ParserRuleContext {
		public NegationContext[] negation() {
			return GetRuleContexts<NegationContext>();
		}
		public NegationContext negation(int i) {
			return GetRuleContext<NegationContext>(i);
		}
		public ITerminalNode[] WS() { return GetTokens(SearchGrammarParser.WS); }
		public ITerminalNode WS(int i) {
			return GetToken(SearchGrammarParser.WS, i);
		}
		public OrContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_or; } }
		public override void EnterRule(IParseTreeListener listener) {
			ISearchGrammarListener typedListener = listener as ISearchGrammarListener;
			if (typedListener != null) typedListener.EnterOr(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ISearchGrammarListener typedListener = listener as ISearchGrammarListener;
			if (typedListener != null) typedListener.ExitOr(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ISearchGrammarVisitor<TResult> typedVisitor = visitor as ISearchGrammarVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitOr(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public OrContext or() {
		OrContext _localctx = new OrContext(Context, State);
		EnterRule(_localctx, 12, RULE_or);
		int _la;
		try {
			int _alt;
			EnterOuterAlt(_localctx, 1);
			{
			State = 117;
			ErrorHandler.Sync(this);
			_alt = Interpreter.AdaptivePredict(TokenStream,14,Context);
			while ( _alt!=2 && _alt!=global::Antlr4.Runtime.Atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					{
					{
					State = 101; negation();
					State = 105;
					ErrorHandler.Sync(this);
					_la = TokenStream.LA(1);
					while (_la==WS) {
						{
						{
						State = 102; Match(WS);
						}
						}
						State = 107;
						ErrorHandler.Sync(this);
						_la = TokenStream.LA(1);
					}
					State = 108; Match(T__8);
					State = 112;
					ErrorHandler.Sync(this);
					_la = TokenStream.LA(1);
					while (_la==WS) {
						{
						{
						State = 109; Match(WS);
						}
						}
						State = 114;
						ErrorHandler.Sync(this);
						_la = TokenStream.LA(1);
					}
					}
					} 
				}
				State = 119;
				ErrorHandler.Sync(this);
				_alt = Interpreter.AdaptivePredict(TokenStream,14,Context);
			}
			State = 120; negation();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class AndContext : ParserRuleContext {
		public OrContext[] or() {
			return GetRuleContexts<OrContext>();
		}
		public OrContext or(int i) {
			return GetRuleContext<OrContext>(i);
		}
		public ITerminalNode[] WS() { return GetTokens(SearchGrammarParser.WS); }
		public ITerminalNode WS(int i) {
			return GetToken(SearchGrammarParser.WS, i);
		}
		public AndContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_and; } }
		public override void EnterRule(IParseTreeListener listener) {
			ISearchGrammarListener typedListener = listener as ISearchGrammarListener;
			if (typedListener != null) typedListener.EnterAnd(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ISearchGrammarListener typedListener = listener as ISearchGrammarListener;
			if (typedListener != null) typedListener.ExitAnd(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ISearchGrammarVisitor<TResult> typedVisitor = visitor as ISearchGrammarVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitAnd(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public AndContext and() {
		AndContext _localctx = new AndContext(Context, State);
		EnterRule(_localctx, 14, RULE_and);
		try {
			int _alt;
			EnterOuterAlt(_localctx, 1);
			{
			State = 127;
			ErrorHandler.Sync(this);
			_alt = Interpreter.AdaptivePredict(TokenStream,15,Context);
			while ( _alt!=2 && _alt!=global::Antlr4.Runtime.Atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					{
					{
					State = 122; or();
					State = 123; Match(WS);
					}
					} 
				}
				State = 129;
				ErrorHandler.Sync(this);
				_alt = Interpreter.AdaptivePredict(TokenStream,15,Context);
			}
			State = 130; or();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class QueryContext : ParserRuleContext {
		public AndContext and() {
			return GetRuleContext<AndContext>(0);
		}
		public QueryContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_query; } }
		public override void EnterRule(IParseTreeListener listener) {
			ISearchGrammarListener typedListener = listener as ISearchGrammarListener;
			if (typedListener != null) typedListener.EnterQuery(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ISearchGrammarListener typedListener = listener as ISearchGrammarListener;
			if (typedListener != null) typedListener.ExitQuery(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ISearchGrammarVisitor<TResult> typedVisitor = visitor as ISearchGrammarVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitQuery(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public QueryContext query() {
		QueryContext _localctx = new QueryContext(Context, State);
		EnterRule(_localctx, 16, RULE_query);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 132; and();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	private static char[] _serializedATN = {
		'\x3', '\x608B', '\xA72A', '\x8133', '\xB9ED', '\x417C', '\x3BE7', '\x7786', 
		'\x5964', '\x3', '\xF', '\x89', '\x4', '\x2', '\t', '\x2', '\x4', '\x3', 
		'\t', '\x3', '\x4', '\x4', '\t', '\x4', '\x4', '\x5', '\t', '\x5', '\x4', 
		'\x6', '\t', '\x6', '\x4', '\a', '\t', '\a', '\x4', '\b', '\t', '\b', 
		'\x4', '\t', '\t', '\t', '\x4', '\n', '\t', '\n', '\x3', '\x2', '\x3', 
		'\x2', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\a', '\x3', '\x1A', 
		'\n', '\x3', '\f', '\x3', '\xE', '\x3', '\x1D', '\v', '\x3', '\x3', '\x3', 
		'\x3', '\x3', '\a', '\x3', '!', '\n', '\x3', '\f', '\x3', '\xE', '\x3', 
		'$', '\v', '\x3', '\x3', '\x3', '\x3', '\x3', '\a', '\x3', '(', '\n', 
		'\x3', '\f', '\x3', '\xE', '\x3', '+', '\v', '\x3', '\a', '\x3', '-', 
		'\n', '\x3', '\f', '\x3', '\xE', '\x3', '\x30', '\v', '\x3', '\x3', '\x3', 
		'\x3', '\x3', '\a', '\x3', '\x34', '\n', '\x3', '\f', '\x3', '\xE', '\x3', 
		'\x37', '\v', '\x3', '\x3', '\x3', '\x3', '\x3', '\x5', '\x3', ';', '\n', 
		'\x3', '\x3', '\x4', '\x5', '\x4', '>', '\n', '\x4', '\x3', '\x4', '\x3', 
		'\x4', '\x3', '\x4', '\x3', '\x4', '\x3', '\x5', '\x3', '\x5', '\x3', 
		'\x5', '\a', '\x5', 'G', '\n', '\x5', '\f', '\x5', '\xE', '\x5', 'J', 
		'\v', '\x5', '\x3', '\x5', '\x3', '\x5', '\x3', '\x6', '\x3', '\x6', '\x3', 
		'\x6', '\x3', '\x6', '\x3', '\x6', '\a', '\x6', 'S', '\n', '\x6', '\f', 
		'\x6', '\xE', '\x6', 'V', '\v', '\x6', '\x3', '\x6', '\x3', '\x6', '\a', 
		'\x6', 'Z', '\n', '\x6', '\f', '\x6', '\xE', '\x6', ']', '\v', '\x6', 
		'\x3', '\x6', '\x3', '\x6', '\x5', '\x6', '\x61', '\n', '\x6', '\x3', 
		'\a', '\x3', '\a', '\x3', '\a', '\x5', '\a', '\x66', '\n', '\a', '\x3', 
		'\b', '\x3', '\b', '\a', '\b', 'j', '\n', '\b', '\f', '\b', '\xE', '\b', 
		'm', '\v', '\b', '\x3', '\b', '\x3', '\b', '\a', '\b', 'q', '\n', '\b', 
		'\f', '\b', '\xE', '\b', 't', '\v', '\b', '\a', '\b', 'v', '\n', '\b', 
		'\f', '\b', '\xE', '\b', 'y', '\v', '\b', '\x3', '\b', '\x3', '\b', '\x3', 
		'\t', '\x3', '\t', '\x3', '\t', '\a', '\t', '\x80', '\n', '\t', '\f', 
		'\t', '\xE', '\t', '\x83', '\v', '\t', '\x3', '\t', '\x3', '\t', '\x3', 
		'\n', '\x3', '\n', '\x3', '\n', '\x3', 'H', '\x2', '\v', '\x2', '\x4', 
		'\x6', '\b', '\n', '\f', '\xE', '\x10', '\x12', '\x2', '\x3', '\x3', '\x2', 
		'\r', '\xE', '\x2', '\x91', '\x2', '\x14', '\x3', '\x2', '\x2', '\x2', 
		'\x4', ':', '\x3', '\x2', '\x2', '\x2', '\x6', '=', '\x3', '\x2', '\x2', 
		'\x2', '\b', '\x43', '\x3', '\x2', '\x2', '\x2', '\n', '`', '\x3', '\x2', 
		'\x2', '\x2', '\f', '\x65', '\x3', '\x2', '\x2', '\x2', '\xE', 'w', '\x3', 
		'\x2', '\x2', '\x2', '\x10', '\x81', '\x3', '\x2', '\x2', '\x2', '\x12', 
		'\x86', '\x3', '\x2', '\x2', '\x2', '\x14', '\x15', '\t', '\x2', '\x2', 
		'\x2', '\x15', '\x3', '\x3', '\x2', '\x2', '\x2', '\x16', ';', '\x5', 
		'\x2', '\x2', '\x2', '\x17', '\x1B', '\a', '\x3', '\x2', '\x2', '\x18', 
		'\x1A', '\a', '\xF', '\x2', '\x2', '\x19', '\x18', '\x3', '\x2', '\x2', 
		'\x2', '\x1A', '\x1D', '\x3', '\x2', '\x2', '\x2', '\x1B', '\x19', '\x3', 
		'\x2', '\x2', '\x2', '\x1B', '\x1C', '\x3', '\x2', '\x2', '\x2', '\x1C', 
		'.', '\x3', '\x2', '\x2', '\x2', '\x1D', '\x1B', '\x3', '\x2', '\x2', 
		'\x2', '\x1E', '\"', '\x5', '\x2', '\x2', '\x2', '\x1F', '!', '\a', '\xF', 
		'\x2', '\x2', ' ', '\x1F', '\x3', '\x2', '\x2', '\x2', '!', '$', '\x3', 
		'\x2', '\x2', '\x2', '\"', ' ', '\x3', '\x2', '\x2', '\x2', '\"', '#', 
		'\x3', '\x2', '\x2', '\x2', '#', '%', '\x3', '\x2', '\x2', '\x2', '$', 
		'\"', '\x3', '\x2', '\x2', '\x2', '%', ')', '\a', '\x4', '\x2', '\x2', 
		'&', '(', '\a', '\xF', '\x2', '\x2', '\'', '&', '\x3', '\x2', '\x2', '\x2', 
		'(', '+', '\x3', '\x2', '\x2', '\x2', ')', '\'', '\x3', '\x2', '\x2', 
		'\x2', ')', '*', '\x3', '\x2', '\x2', '\x2', '*', '-', '\x3', '\x2', '\x2', 
		'\x2', '+', ')', '\x3', '\x2', '\x2', '\x2', ',', '\x1E', '\x3', '\x2', 
		'\x2', '\x2', '-', '\x30', '\x3', '\x2', '\x2', '\x2', '.', ',', '\x3', 
		'\x2', '\x2', '\x2', '.', '/', '\x3', '\x2', '\x2', '\x2', '/', '\x31', 
		'\x3', '\x2', '\x2', '\x2', '\x30', '.', '\x3', '\x2', '\x2', '\x2', '\x31', 
		'\x35', '\x5', '\x2', '\x2', '\x2', '\x32', '\x34', '\a', '\xF', '\x2', 
		'\x2', '\x33', '\x32', '\x3', '\x2', '\x2', '\x2', '\x34', '\x37', '\x3', 
		'\x2', '\x2', '\x2', '\x35', '\x33', '\x3', '\x2', '\x2', '\x2', '\x35', 
		'\x36', '\x3', '\x2', '\x2', '\x2', '\x36', '\x38', '\x3', '\x2', '\x2', 
		'\x2', '\x37', '\x35', '\x3', '\x2', '\x2', '\x2', '\x38', '\x39', '\a', 
		'\x5', '\x2', '\x2', '\x39', ';', '\x3', '\x2', '\x2', '\x2', ':', '\x16', 
		'\x3', '\x2', '\x2', '\x2', ':', '\x17', '\x3', '\x2', '\x2', '\x2', ';', 
		'\x5', '\x3', '\x2', '\x2', '\x2', '<', '>', '\a', '\x6', '\x2', '\x2', 
		'=', '<', '\x3', '\x2', '\x2', '\x2', '=', '>', '\x3', '\x2', '\x2', '\x2', 
		'>', '?', '\x3', '\x2', '\x2', '\x2', '?', '@', '\x5', '\x4', '\x3', '\x2', 
		'@', '\x41', '\a', '\a', '\x2', '\x2', '\x41', '\x42', '\x5', '\x2', '\x2', 
		'\x2', '\x42', '\a', '\x3', '\x2', '\x2', '\x2', '\x43', '\x44', '\a', 
		'\r', '\x2', '\x2', '\x44', 'H', '\a', '\b', '\x2', '\x2', '\x45', 'G', 
		'\v', '\x2', '\x2', '\x2', '\x46', '\x45', '\x3', '\x2', '\x2', '\x2', 
		'G', 'J', '\x3', '\x2', '\x2', '\x2', 'H', 'I', '\x3', '\x2', '\x2', '\x2', 
		'H', '\x46', '\x3', '\x2', '\x2', '\x2', 'I', 'K', '\x3', '\x2', '\x2', 
		'\x2', 'J', 'H', '\x3', '\x2', '\x2', '\x2', 'K', 'L', '\a', '\t', '\x2', 
		'\x2', 'L', '\t', '\x3', '\x2', '\x2', '\x2', 'M', '\x61', '\x5', '\x6', 
		'\x4', '\x2', 'N', '\x61', '\x5', '\x2', '\x2', '\x2', 'O', '\x61', '\x5', 
		'\b', '\x5', '\x2', 'P', 'T', '\a', '\b', '\x2', '\x2', 'Q', 'S', '\a', 
		'\xF', '\x2', '\x2', 'R', 'Q', '\x3', '\x2', '\x2', '\x2', 'S', 'V', '\x3', 
		'\x2', '\x2', '\x2', 'T', 'R', '\x3', '\x2', '\x2', '\x2', 'T', 'U', '\x3', 
		'\x2', '\x2', '\x2', 'U', 'W', '\x3', '\x2', '\x2', '\x2', 'V', 'T', '\x3', 
		'\x2', '\x2', '\x2', 'W', '[', '\x5', '\x12', '\n', '\x2', 'X', 'Z', '\a', 
		'\xF', '\x2', '\x2', 'Y', 'X', '\x3', '\x2', '\x2', '\x2', 'Z', ']', '\x3', 
		'\x2', '\x2', '\x2', '[', 'Y', '\x3', '\x2', '\x2', '\x2', '[', '\\', 
		'\x3', '\x2', '\x2', '\x2', '\\', '^', '\x3', '\x2', '\x2', '\x2', ']', 
		'[', '\x3', '\x2', '\x2', '\x2', '^', '_', '\a', '\t', '\x2', '\x2', '_', 
		'\x61', '\x3', '\x2', '\x2', '\x2', '`', 'M', '\x3', '\x2', '\x2', '\x2', 
		'`', 'N', '\x3', '\x2', '\x2', '\x2', '`', 'O', '\x3', '\x2', '\x2', '\x2', 
		'`', 'P', '\x3', '\x2', '\x2', '\x2', '\x61', '\v', '\x3', '\x2', '\x2', 
		'\x2', '\x62', '\x66', '\x5', '\n', '\x6', '\x2', '\x63', '\x64', '\a', 
		'\n', '\x2', '\x2', '\x64', '\x66', '\x5', '\n', '\x6', '\x2', '\x65', 
		'\x62', '\x3', '\x2', '\x2', '\x2', '\x65', '\x63', '\x3', '\x2', '\x2', 
		'\x2', '\x66', '\r', '\x3', '\x2', '\x2', '\x2', 'g', 'k', '\x5', '\f', 
		'\a', '\x2', 'h', 'j', '\a', '\xF', '\x2', '\x2', 'i', 'h', '\x3', '\x2', 
		'\x2', '\x2', 'j', 'm', '\x3', '\x2', '\x2', '\x2', 'k', 'i', '\x3', '\x2', 
		'\x2', '\x2', 'k', 'l', '\x3', '\x2', '\x2', '\x2', 'l', 'n', '\x3', '\x2', 
		'\x2', '\x2', 'm', 'k', '\x3', '\x2', '\x2', '\x2', 'n', 'r', '\a', '\v', 
		'\x2', '\x2', 'o', 'q', '\a', '\xF', '\x2', '\x2', 'p', 'o', '\x3', '\x2', 
		'\x2', '\x2', 'q', 't', '\x3', '\x2', '\x2', '\x2', 'r', 'p', '\x3', '\x2', 
		'\x2', '\x2', 'r', 's', '\x3', '\x2', '\x2', '\x2', 's', 'v', '\x3', '\x2', 
		'\x2', '\x2', 't', 'r', '\x3', '\x2', '\x2', '\x2', 'u', 'g', '\x3', '\x2', 
		'\x2', '\x2', 'v', 'y', '\x3', '\x2', '\x2', '\x2', 'w', 'u', '\x3', '\x2', 
		'\x2', '\x2', 'w', 'x', '\x3', '\x2', '\x2', '\x2', 'x', 'z', '\x3', '\x2', 
		'\x2', '\x2', 'y', 'w', '\x3', '\x2', '\x2', '\x2', 'z', '{', '\x5', '\f', 
		'\a', '\x2', '{', '\xF', '\x3', '\x2', '\x2', '\x2', '|', '}', '\x5', 
		'\xE', '\b', '\x2', '}', '~', '\a', '\xF', '\x2', '\x2', '~', '\x80', 
		'\x3', '\x2', '\x2', '\x2', '\x7F', '|', '\x3', '\x2', '\x2', '\x2', '\x80', 
		'\x83', '\x3', '\x2', '\x2', '\x2', '\x81', '\x7F', '\x3', '\x2', '\x2', 
		'\x2', '\x81', '\x82', '\x3', '\x2', '\x2', '\x2', '\x82', '\x84', '\x3', 
		'\x2', '\x2', '\x2', '\x83', '\x81', '\x3', '\x2', '\x2', '\x2', '\x84', 
		'\x85', '\x5', '\xE', '\b', '\x2', '\x85', '\x11', '\x3', '\x2', '\x2', 
		'\x2', '\x86', '\x87', '\x5', '\x10', '\t', '\x2', '\x87', '\x13', '\x3', 
		'\x2', '\x2', '\x2', '\x12', '\x1B', '\"', ')', '.', '\x35', ':', '=', 
		'H', 'T', '[', '`', '\x65', 'k', 'r', 'w', '\x81',
	};

	public static readonly ATN _ATN =
		new ATNDeserializer().Deserialize(_serializedATN);


}
