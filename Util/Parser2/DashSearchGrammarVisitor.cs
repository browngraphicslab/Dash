using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class DashSearchGrammarVisitor : SearchGrammarBaseVisitor<Predicate<DocumentController>>
    {
        public override Predicate<DocumentController> VisitAnd([NotNull] SearchGrammarParser.AndContext context)
        {
            var l = context.or().Select(c => c.Accept(this)).ToList();
            return doc =>
            {
                bool accept = true;
                foreach (var predicate in l)
                {
                    accept &= predicate(doc);
                }
                return accept;
            };
        }

        public override Predicate<DocumentController> VisitFunction([NotNull] SearchGrammarParser.FunctionContext context)
        {
            return base.VisitFunction(context);
        }

        public override Predicate<DocumentController> VisitKvsearch([NotNull] SearchGrammarParser.KvsearchContext context)
        {
            var keys = context.keylist().Accept(new DashSearchGrammarKvVisitor());
            var value = context.value().GetText().Trim('"');
            return doc => doc == null; //<-- ignore, filler. Correct --> Search.SearchByKeyValuePair(keys, value, context.ChildCount == 4);
        }

        public override Predicate<DocumentController> VisitNegation([NotNull] SearchGrammarParser.NegationContext context)
        {
            var visitNegation = context.term().Accept(this);
            if (context.ChildCount == 1)
            {
                return visitNegation;
            }

            return doc => !visitNegation(doc);
        }

        public override Predicate<DocumentController> VisitOr([NotNull] SearchGrammarParser.OrContext context)
        {
            var l = context.negation().Select(c => c.Accept(this)).ToList();
            return doc =>
            {
                bool returnDoc = false;
                foreach (var predicate in l)
                {
                    returnDoc |= predicate(doc);
                }
                return returnDoc;
            };
        }

        public override Predicate<DocumentController> VisitTerm([NotNull] SearchGrammarParser.TermContext context)
        {
            return context.ChildCount == 1 ? base.VisitTerm(context) : context.query().Accept(this);
        }

        public override Predicate<DocumentController> VisitValue([NotNull] SearchGrammarParser.ValueContext context)
        {
            //string textToSearch = context.WORD().Symbol.Text ?? context.STRING().Symbol.Text.Trim('"');
            return doc => doc.SearchForString(context.GetText().ToLower()).StringFound;
        }
    }
}
