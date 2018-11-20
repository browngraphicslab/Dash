using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;


// ReSharper disable once CheckNamespace
namespace Dash
{
    using SearchPair = KeyValuePair<KeyController, StringSearchModel>;
    using Result = List<KeyValuePair<KeyController, StringSearchModel>>;

    public delegate Result SearchPredicate(DocumentController document);

    public class DashSearchGrammarVisitor : SearchGrammarBaseVisitor<SearchPredicate>
    {
        public DocumentController SearchRoot { get; private set; }
        public override SearchPredicate VisitAnd([NotNull] SearchGrammarParser.AndContext context)
        {
            var l = context.or().Select(c => c.Accept(this)).ToList();
            return doc =>
            {
                var result = new Result();
                foreach (var searchPredicate in l)
                {
                    var keyValuePairs = searchPredicate(doc);
                    if (!keyValuePairs.Any())
                    {
                        return new Result();
                    }
                    result.AddRange(keyValuePairs);
                }
                return result;
            };
        }

        public override SearchPredicate VisitFunction([NotNull] SearchGrammarParser.FunctionContext context)
        {
            var func = context.GetText();
            var funcName = context.WORD().GetText();
            var exp = TypescriptToOperatorParser.ParseToExpression(func);
            try
            {
                var field = exp.Execute(new Scope()).GetAwaiter().GetResult().Item1; //TODO This probably shouldn't access Result

                if (field is ListController<DocumentController> list)
                {
                    return document =>
                    {
                        if (list.Contains(document))
                        {
                            return new Result()
                            {
                            new SearchPair(KeyController.Get(funcName),
                                new StringSearchModel("Was contained in " + func))
                            };
                        }

                        return new Result();
                    };
                }
            }
            catch (ScriptExecutionException e)
            {
                if (e.Error is VariableNotFoundExecutionErrorModel varErr && varErr.VariableName == "doc")
                {
                    bool failed = false;

                    Result SearchPredicate(DocumentController document)
                    {
                        if (failed)
                        {
                            return new Result();
                        }

                        var scope = new Scope();
                        scope.DeclareVariable("doc", document);
                        var result = exp.Execute(scope).GetAwaiter().GetResult().Item1;
                        if (result is BoolController b && b.Data)
                        {
                            return new Result()
                            {
                            new SearchPair(KeyController.Get(funcName),
                                new StringSearchModel("Matched predicate " + func))
                            };
                        }
                        else
                        {
                            failed = true;
                            return new Result();
                        }
                    }

                    return SearchPredicate;
                }
            }

            return doc => new Result();
        }

        public override SearchPredicate VisitKvsearch([NotNull] SearchGrammarParser.KvsearchContext context)
        {
            var keys = new HashSet<KeyController>(context.keylist().Accept(new DashSearchGrammarKvVisitor()));
            var value = context.value().GetText().Trim('"');
            var negate = context.ChildCount == 4;

            if (keys.Count == 1 && keys.First().Name == "SearchPath")
            {
                var doc = DocumentTree.GetDocumentAtPath(value);
                if (doc != null)
                {
                    SearchRoot = doc;
                    return document => new Result {new SearchPair(keys.First(), new StringSearchModel("In path"))};
                }
            }
            return doc =>
            {
                var result = new Result();
                if (negate)
                {
                    foreach (var field in doc.EnumDisplayableFields())
                    {
                        if (keys.Contains(field.Key))
                        {
                            continue;
                        }
                        var res = field.Value.SearchForString(value);
                        if (res.StringFound)
                        {
                            result.Add(new SearchPair(field.Key, res));
                        }
                    }
                }
                else
                {
                    foreach (var key in keys)
                    {
                        var res = doc.GetDereferencedField(key, null)?.SearchForString(value);
                        if (res?.StringFound ?? false)
                        {
                            result.Add(new SearchPair(key, res));
                        }
                    }
                }

                return result;
            };
        }

        public override SearchPredicate VisitNegation([NotNull] SearchGrammarParser.NegationContext context)
        {
            var visitNegation = context.term().Accept(this);
            if (context.ChildCount == 1)
            {
                return visitNegation;
            }

            return doc =>
            {
                var result = visitNegation(doc);
                if (result.Any())
                {
                    return new Result();
                }

                return new Result()
                {
                    new SearchPair(KeyController.Get("Negation"), new StringSearchModel("Negation"))
                };
            };
        }

        public override SearchPredicate VisitOr([NotNull] SearchGrammarParser.OrContext context)
        {
            var l = context.negation().Select(c => c.Accept(this)).ToList();
            return doc =>
            {
                var result = new Result();
                foreach (var searchPredicate in l)
                {
                    result.AddRange(searchPredicate(doc));
                }
                return result;
            };
        }

        public override SearchPredicate VisitTerm([NotNull] SearchGrammarParser.TermContext context)
        {
            return context.ChildCount == 1 ? base.VisitTerm(context) : context.query().Accept(this);
        }

        public override SearchPredicate VisitValue([NotNull] SearchGrammarParser.ValueContext context)
        {
            string textToSearch = context.WORD()?.Symbol.Text ?? context.STRING().Symbol.Text.Trim('"');
            return doc =>
            {
                var result = new Result();
                foreach (var field in doc.EnumDisplayableFields())
                {
                    var res = field.Value.SearchForString(textToSearch);
                    if (res.StringFound)
                    {
                        result.Add(new SearchPair(field.Key, res));
                    }
                }
                return result;
            };
        }
    }
}
