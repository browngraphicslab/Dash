using System.Diagnostics;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace Dash
{
    public class SearchGrammarVisitor : DashSearchGrammarBaseVisitor<string>
    {
        public override string Visit(IParseTree tree)
        {
            return base.Visit(tree);
        }

        public override string VisitAnd_token([NotNull] DashSearchGrammarParser.And_tokenContext context)
        {
            Debug.WriteLine("VISITING AND_TOKEN!!");
            return base.VisitAnd_token(context);
        }

        public override string VisitArguments([NotNull] DashSearchGrammarParser.ArgumentsContext context)
        {
            return base.VisitArguments(context);
        }

        public override string VisitChain([NotNull] DashSearchGrammarParser.ChainContext context)
        {
            return base.VisitChain(context);
        }

        public override string VisitChildren(IRuleNode node)
        {
            return base.VisitChildren(node);
        }

        public override string VisitErrorNode(IErrorNode node)
        {
            return base.VisitErrorNode(node);
        }

        public override string VisitFunction_expr([NotNull] DashSearchGrammarParser.Function_exprContext context)
        {
            return base.VisitFunction_expr(context);
        }

        public override string VisitInput([NotNull] DashSearchGrammarParser.InputContext context)
        {
            return base.VisitInput(context);
        }

        public override string VisitKv_search([NotNull] DashSearchGrammarParser.Kv_searchContext context)
        {
            return base.VisitKv_search(context);
        }

        public override string VisitLogical_expr([NotNull] DashSearchGrammarParser.Logical_exprContext context)
        {
            return base.VisitLogical_expr(context);
        }

        public override string VisitOperator([NotNull] DashSearchGrammarParser.OperatorContext context)
        {
            return base.VisitOperator(context);
        }

        public override string VisitOr_token([NotNull] DashSearchGrammarParser.Or_tokenContext context)
        {
            return base.VisitOr_token(context);
        }

        public override string VisitPhrase([NotNull] DashSearchGrammarParser.PhraseContext context)
        {
            return base.VisitPhrase(context);
        }
    }
}
