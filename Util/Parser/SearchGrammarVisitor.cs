using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace Dash
{
    public class SearchGrammarVisitor : DashSearchGrammarBaseVisitor<bool>
    {
        public Predicate<DocumentController> Predicate { get; private set; }

        public override bool Visit(IParseTree tree)
        {
            return base.Visit(tree);
        }

        public override bool VisitAnd_token([NotNull] DashSearchGrammarParser.And_tokenContext context)
        {
            return base.VisitAnd_token(context);
        }

        public override bool VisitArgument([NotNull] DashSearchGrammarParser.ArgumentContext context)
        {
            return base.VisitArgument(context);
        }

        public override bool VisitArguments([NotNull] DashSearchGrammarParser.ArgumentsContext context)
        {
            return base.VisitArguments(context);
        }

        public override bool VisitChildren(IRuleNode node)
        {
            return base.VisitChildren(node);
        }

        public override bool VisitErrorNode(IErrorNode node)
        {
            return base.VisitErrorNode(node);
        }

        public override bool VisitFunction_expr([NotNull] DashSearchGrammarParser.Function_exprContext context)
        {
            return base.VisitFunction_expr(context);
        }

        public override bool VisitKv_search([NotNull] DashSearchGrammarParser.Kv_searchContext context)
        {
            return base.VisitKv_search(context);
        }

        public override bool VisitNot_search_term([NotNull] DashSearchGrammarParser.Not_search_termContext context)
        {
            return base.VisitNot_search_term(context);
        }

        public override bool VisitOperator([NotNull] DashSearchGrammarParser.OperatorContext context)
        {
            return base.VisitOperator(context);
        }

        public override bool VisitOr_token([NotNull] DashSearchGrammarParser.Or_tokenContext context)
        {
            return base.VisitOr_token(context);
        }

        public override bool VisitPhrase([NotNull] DashSearchGrammarParser.PhraseContext context)
        {
            return base.VisitPhrase(context);
        }

        public override bool VisitQuery([NotNull] DashSearchGrammarParser.QueryContext context)
        {
            return base.VisitQuery(context);
        }

        public override bool VisitSearch_term([NotNull] DashSearchGrammarParser.Search_termContext context)
        {
            return base.VisitSearch_term(context);
        }

        public override bool VisitTerminal(ITerminalNode node)
        {
            return base.VisitTerminal(node);
        }

        protected override bool AggregateResult(bool aggregate, bool nextResult)
        {
            return base.AggregateResult(aggregate, nextResult);
        }

        protected override bool ShouldVisitNextChild(IRuleNode node, bool currentResult)
        {
            return base.ShouldVisitNextChild(node, currentResult);
        }
    }

}
