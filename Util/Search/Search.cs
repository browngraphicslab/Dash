﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class Search
    {

        public class SearchOptions
        {
            public string SearchString { get; }
            public Regex Regex { get; }
            public SearchOptions(string searchString, bool matchCase, bool useRegex, bool matchWholeWord)
            {
                if (useRegex)
                {
                    Regex = new Regex(searchString);
                }
                else
                {
                    if (matchCase && matchWholeWord)
                    {
                        Regex = new Regex(@"(?:^|\W)" + searchString + @"(?:$|\W)",
                            RegexOptions.Compiled);
                    }
                    else
                    {
                        if (matchWholeWord)
                        {
                            Regex = new Regex(@"(?:^|\W)" + searchString + @"(?:$|\W)",
                                RegexOptions.Compiled | RegexOptions.IgnoreCase);
                        }
                        else
                        {
                            Regex = new Regex(searchString,
                                RegexOptions.Compiled);
                        }
                    }
                }
            }

            public StringSearchModel Matches(string data)
            {
                int maxStringSize = 125;
                int textDecrementForContext = 8;
                if (Regex!=null)
                {
                    if (Regex.IsMatch(data))
                    {
                        return new StringSearchModel(data);
                    }
                }
                else
                // standard text search
                {
                    int index = data.IndexOf(SearchString);
                    if (index < 0)
                    {
                        return StringSearchModel.False;
                    }
                    else
                    {
                        index = Math.Max(0, index - textDecrementForContext);
                        var substring = data.Substring(index, Math.Min(maxStringSize, data.Length - index));
                        return new StringSearchModel(substring);
                    }
                }

                return null;
            }
        }

        private SearchOptions ConvertStringOptionstoSeachOptions(string inputString,HashSet<string> stringoptions)
        {
            bool matchword = false;
            bool matchcase = false;
            bool useregex = false;
            if (stringoptions.Contains("Match whole word"))
            {
                matchword = true;
            }

            if (stringoptions.Contains("Case sensitive"))
            {
                matchcase = true;
            }

            if (stringoptions.Contains("Regex"))
            {
                useregex = true;
            }
            return new SearchOptions(inputString,matchcase,useregex,matchword);
        }

        //TODO: Type-Based Search
        //TODO: Search in collections - check out "collected docs note in viewdocument.getdatadocument.documenttype.type
        //TODO: ModifiedTime not existing until document is modified

        // Checks the DataDocuments of all DocumentControllers in the Dash view for a specific Key-Value pair
        public static IEnumerable<SearchResult> SearchByKeyValuePair(string key, string value, bool negate = false)
        {
            return Parse((negate ? "!" : "") + key + ":" + value);
        }


        /// <summary>
        /// Run a search with the given string
        /// </summary>
        /// <param name="inputString">The search query</param>
        /// <param name="useAll">Search over all documents or only those in the main tree</param>
        /// <param name="docs">Optional list of documents to search over instead of all documents</param>
        /// <returns></returns>
        public static List<SearchResult> Parse(string inputString, bool useAll = false,
            IEnumerable<DocumentController> docs = null, HashSet<string> options = null)
        {
            if (string.IsNullOrEmpty(inputString)) return new List<SearchResult>();


            var searchBoxLexer = new SearchGrammarLexer(new AntlrInputStream(inputString));
            var parser = new SearchGrammarParser(new CommonTokenStream(searchBoxLexer)) { BuildParseTree = true };
            var visitor = new DashSearchGrammarVisitor();
            var parseTree = visitor.Visit(parser.query());

            var results = new List<SearchResult>();
            var keyRefs = new List<string>();
            var fieldRefs = new List<string>();

            Regex rx = null;

            if (options != null && options.Count > 0)
            {
                if (options.Count == 1)
                {
                    if (options.Contains("Match whole word"))
                    {
                        rx = new Regex(@"(?:^|\W)" + inputString + @"(?:$|\W)",
                            RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    }
                    else if (options.Contains("Case sensitive"))
                    {
                        rx = new Regex(inputString,
                            RegexOptions.Compiled);
                    }
                    else
                    {
                        rx = new Regex(@inputString);
                    }
                }
                else
                {
                    rx = new Regex(@"(?:^|\W)" + inputString + @"(?:$|\W)",
                        RegexOptions.Compiled);
                }
            }

            void DocSearch(DocumentController doc)
            {
                var res = parseTree(doc);
                foreach (var result in res)
                {
                    string keyref = result.Key.Name;
                    string fieldRef = result.Value.RelatedString;
                    if (rx != null)
                    {
                        if (rx.IsMatch(fieldRef))
                        {
                            keyRefs.Add(keyref);
                            fieldRefs.Add(fieldRef);
                        }
                    }
                    else
                    {
                        keyRefs.Add(keyref);
                        fieldRefs.Add(fieldRef);
                    }
                }
            }

            if (docs == null)
            {
                var nodes = useAll ? DocumentTree.MainPageTree.GetAllNodes() : DocumentTree.MainPageTree;
                foreach (var node in nodes)
                {
                    DocSearch(node.ViewDocument);
                    DocSearch(node.DataDocument);


                    if (keyRefs.Any())
                    {
                        results.Add(new SearchResult(node, keyRefs, fieldRefs,
                            keyRefs.Count));
                    }

                    keyRefs.Clear();
                    fieldRefs.Clear();
                }
            }
            else
            {
                foreach (var documentController in docs)
                {
                    DocSearch(documentController);
                    var dataDoc = documentController.GetDataDocument();
                    if (dataDoc != documentController)
                    {
                        DocSearch(dataDoc);
                    }

                    if (keyRefs.Any())
                    {
                        results.Add(new SearchResult(documentController, keyRefs, fieldRefs,
                            keyRefs.Count));
                    }

                    keyRefs.Clear();
                    fieldRefs.Clear();
                }
            }

            return results;
        }

        public struct SearchTerm
        {

            public bool Negate { get; set; }
            public readonly string _term;
            public SearchTerm(string term, bool negate = false)
            {
                Negate = negate;
                _term = term;
            }

            public static ListController<TextController> ConvertSearchTerms(List<SearchTerm> searchTerms)
            {
                var list = new ListController<TextController>();
                foreach (var term in searchTerms)
                {
                    if (!term.Negate)
                    {
                        list.Add(new TextController(term._term));
                    }
                }
                return list;
            }
        }


        public static IEnumerable<SearchResult> SearchByAlias(string id, bool avoidDuplicateViews = false)
        {
            var doc = SearchIndividualById(id);
            var dataDoc = doc.GetDataDocument();
            var filteredNodes = DocumentTree.MainPageTree.GetAllNodes().Where(node => node.DataDocument.Equals(dataDoc));
            if (avoidDuplicateViews) filteredNodes = filteredNodes.Where(node => !node.ViewDocument.Equals(doc));
            return filteredNodes.Select(node => new SearchResult(node, new List<string>(), new List<string> { id }));
        }

        public static DocumentController SearchIndividualById(string id) => RESTClient.Instance.Fields.GetController<DocumentController>(id);
    }
}
