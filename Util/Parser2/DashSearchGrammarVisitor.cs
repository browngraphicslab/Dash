﻿using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;


// ReSharper disable once CheckNamespace
namespace Dash
{
    using SearchPair = KeyValuePair<KeyController, StringSearchModel>;
    using Result = List<KeyValuePair<KeyController, StringSearchModel>>;


    public delegate Result SearchPredicate(DocumentController document, Search.SearchOptions options);

    public class DashSearchGrammarVisitor : SearchGrammarBaseVisitor<SearchPredicate>
    {
        public DocumentController SearchRoot { get; private set; }
        public override SearchPredicate VisitAnd([NotNull] SearchGrammarParser.AndContext context)
        {
            var l = context.or().Select(c => c.Accept(this)).ToList();
            return (doc, options) =>
            {
                var result = new Result();
                foreach (var searchPredicate in l)
                {
                    var keyValuePairs = searchPredicate(doc, options);
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
                    return (document, options) =>
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

                    Result SearchPredicate(DocumentController document, Search.SearchOptions options)
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

            return (doc, options) => new Result();
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
                    return (document, options) => new Result {new SearchPair(keys.First(), new StringSearchModel("In path"))};
                }
            }
            return (doc, options) =>
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
                        var res = field.Value.SearchForString(value, options);
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
                        var res = doc.GetDereferencedField(key, null)?.SearchForString(value, options);
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

            return (doc, options) =>
            {
                var result = visitNegation(doc, options);
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
            return (doc, options) =>
            {
                var result = new Result();
                foreach (var searchPredicate in l)
                {
                    result.AddRange(searchPredicate(doc, options));
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
            return (doc, options) =>
            {
                var result = new Result();
                foreach (var field in doc.EnumDisplayableFields())
                {
                    var res = field.Value.SearchForString(textToSearch, options);
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
