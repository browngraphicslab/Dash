using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class Search
    {
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
            IEnumerable<DocumentController> docs = null)
        {
            if (string.IsNullOrEmpty(inputString)) return new List<SearchResult>();


            var searchBoxLexer = new SearchGrammarLexer(new AntlrInputStream(inputString));
            var parser = new SearchGrammarParser(new CommonTokenStream(searchBoxLexer)) {BuildParseTree = true};
            var visitor = new DashSearchGrammarVisitor();
            var parseTree = visitor.Visit(parser.query());

            var results = new List<SearchResult>();
            var keyRefs = new List<string>();
            var fieldRefs = new List<string>();

            void DocSearch(DocumentController doc)
            {
                var res = parseTree(doc);
                foreach (var result in res)
                {
                    keyRefs.Add(result.Key.Name);
                    fieldRefs.Add(result.Value.RelatedString);
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
