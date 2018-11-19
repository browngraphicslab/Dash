using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class DashSearchGrammarKvVisitor : SearchGrammarBaseVisitor<List<KeyController>>
    {
        public override List<KeyController> VisitKeylist([NotNull] SearchGrammarParser.KeylistContext context)
        {
            return context.value().Select(v => KeyController.Get(v.GetText().Trim('"'))).ToList();
        }
    }
}
