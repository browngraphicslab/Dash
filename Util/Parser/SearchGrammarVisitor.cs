using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace Dash
{
    public class SearchGrammarVisitor : DashSearchGrammarBaseVisitor<Predicate<DocumentController>>
    {
        protected override Predicate<DocumentController> DefaultResult => doc => true;

        public override Predicate<DocumentController> VisitArgument([NotNull] DashSearchGrammarParser.ArgumentContext context)
        {
            return base.VisitArgument(context);
        }

        public override Predicate<DocumentController> VisitArguments([NotNull] DashSearchGrammarParser.ArgumentsContext context)
        {
            return base.VisitArguments(context);
        }

        public override Predicate<DocumentController> VisitFunction_expr([NotNull] DashSearchGrammarParser.Function_exprContext context)
        {
            return base.VisitFunction_expr(context);
        }

        public override Predicate<DocumentController> VisitKv_search([NotNull] DashSearchGrammarParser.Kv_searchContext context)
        {
            return base.VisitKv_search(context);
        }

        public override Predicate<DocumentController> VisitNot_search_term([NotNull] DashSearchGrammarParser.Not_search_termContext context)
        {
            return base.VisitNot_search_term(context);
        }

        public override Predicate<DocumentController> VisitOperator([NotNull] DashSearchGrammarParser.OperatorContext context)
        {
            return base.VisitOperator(context);
        }

        public override Predicate<DocumentController> VisitOr_token([NotNull] DashSearchGrammarParser.Or_tokenContext context)
        {
            return base.VisitOr_token(context);
        }

        public override Predicate<DocumentController> VisitPhrase([NotNull] DashSearchGrammarParser.PhraseContext context)
        {
            return base.VisitPhrase(context);
        }

        public override Predicate<DocumentController> VisitQuery([NotNull] DashSearchGrammarParser.QueryContext context)
        {
            return base.VisitQuery(context);
        }

        public override Predicate<DocumentController> VisitSearch_term([NotNull] DashSearchGrammarParser.Search_termContext context)
        {
            return base.VisitSearch_term(context);
        }
    }

}
