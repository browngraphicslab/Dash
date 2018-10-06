using System;
using System.Collections.Generic;
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
        public static IEnumerable<SearchResult> SearchByKeyValuePair(KeyController key, string value, bool negate = false)
        {
            var nodes = DocumentTree.MainPageTree.GetAllNodes();
            var filteredNodes = new List<SearchResult>();
            foreach (var node in nodes)
            {
                var relatedFields = new List<string>();
                var relatedStrings = new List<string>();

                StringSearchModel dataStringSearchModel = node.DataDocument?.GetDereferencedField(key, null)?.SearchForString(value);
                StringSearchModel layoutStringSearchModel = node.ViewDocument?.GetDereferencedField(key, null)?.SearchForString(value);

                var numMatchedFields = 0;

                if (layoutStringSearchModel != null && layoutStringSearchModel != StringSearchModel.False)
                {
                    relatedFields.Add($" >> v.{key}");
                    relatedStrings.Add($"\" {layoutStringSearchModel?.RelatedString} \"");
                    numMatchedFields++;
                }

                if (dataStringSearchModel != null && dataStringSearchModel != StringSearchModel.False)
                {
                    relatedFields.Add($" >> d.{key}");
                    relatedStrings.Add($"\" {dataStringSearchModel?.RelatedString} \"");
                    numMatchedFields++;
                }

                filteredNodes.Add(new SearchResult(node, relatedFields, relatedStrings, numMatchedFields));
            }
            filteredNodes.OrderByDescending(res => res.Rank);

            return negate ? filteredNodes.Where(res => res.Rank == 0) : filteredNodes.Where(res => res.Rank > 0);

            //var filteredNodes = DocumentTree.MainPageTree.GetAllNodes().Select(node =>
            //{
            //    var relatedFields = new List<string>();
            //    var relatedStrings = new List<string>();

            //    StringSearchModel dataStringSearchModel = node.DataDocument?.GetDereferencedField(key, null)?.SearchForString(value);
            //    StringSearchModel layoutStringSearchModel = node.ViewDocument?.GetDereferencedField(key, null)?.SearchForString(value);

            //    var numMatchedFields = 0;

            //    if (layoutStringSearchModel != null && layoutStringSearchModel != StringSearchModel.False)
            //    {
            //        relatedFields.Add($" >> v.{key}");
            //        relatedStrings.Add($"\" {layoutStringSearchModel?.RelatedString} \"");
            //        numMatchedFields++;
            //    }

            //    if (dataStringSearchModel != null && dataStringSearchModel != StringSearchModel.False)
            //    {
            //        relatedFields.Add($" >> d.{key}");
            //        relatedStrings.Add($"\" {dataStringSearchModel?.RelatedString} \"");
            //        numMatchedFields++;
            //    }

            //    return new SearchResult(node, relatedFields, relatedStrings, numMatchedFields);
            //}).OrderByDescending(res => res.Rank);

            //return negate ? filteredNodes.Where(res => res.Rank == 0) : filteredNodes.Where(res => res.Rank > 0);
        }

        // Searches the ViewDocument and DataDocuments of all DocumentControllers in the Dash View for a given query string
        public static IEnumerable<SearchResult> SearchByQuery(string query, bool negate = false)
        {
            var nodes = DocumentTree.MainPageTree.GetAllNodes();
            var filteredNodes = new List<SearchResult>();
            foreach (var node in nodes)
            {
                var relatedFields = new List<string>();
                var relatedStrings = new List<string>();

                var numMatchedFields = 0;
                foreach (var field in node.ViewDocument.EnumDisplayableFields())
                {
                    var ss = field.Value.DereferenceToRoot(null);
                    var ssm = ss?.SearchForString(query);
                    if (ssm == null || ssm == StringSearchModel.False) continue;
                    relatedStrings.Add(ssm.RelatedString);
                    relatedFields.Add($" >> v.{field.Key}");
                    numMatchedFields++;
                }
                foreach (var field in node.DataDocument.EnumDisplayableFields())
                {
                    var ssm = field.Value.DereferenceToRoot(null)?.SearchForString(query);
                    if (ssm == null || ssm == StringSearchModel.False) continue;

                    relatedStrings.Add($" >> d.{field.Key}");
                    relatedFields.Add($" >> d.{field.Key}");
                    numMatchedFields++;
                }
                filteredNodes.Add(new SearchResult(node, relatedFields, Process(relatedStrings, query), numMatchedFields));
            }
            filteredNodes.OrderByDescending(res => res.Rank);

            return negate ? filteredNodes.Where(res => res.Rank == 0) : filteredNodes.Where(res => res.Rank > 0);

            // the above code is faster than the below code by a huge margin

            //var filteredNodes = DocumentTree.MainPageTree.GetAllNodes().Select(node =>
            //{
            //    var relatedFields = new List<string>();
            //    var relatedStrings = new List<string>();
                
            //    var numMatchedFields = 0;
            //    foreach (var field in node.ViewDocument.EnumDisplayableFields())
            //    {
            //        var ssm = field.Value.DereferenceToRoot(null)?.SearchForString(query);
            //        if (ssm == null || ssm == StringSearchModel.False) continue;

            //        relatedStrings.Add(ssm.RelatedString);
            //        relatedFields.Add($" >> v.{field.Key}");
            //        numMatchedFields++;
            //    }
            //    foreach (var field in node.DataDocument.EnumDisplayableFields())
            //    {
            //        var ssm = field.Value.DereferenceToRoot(null)?.SearchForString(query);
            //        if (ssm == null || ssm == StringSearchModel.False) continue;

            //        relatedStrings.Add(ssm.RelatedString);
            //        relatedFields.Add($" >> d.{field.Key}");
            //        numMatchedFields++;
            //    }
            //    return new SearchResult(node, relatedFields, Process(relatedStrings, query), numMatchedFields);
            //})
            //    .OrderByDescending(res => res.Rank);

            //return negate ? filteredNodes.Where(res => res.Rank == 0) : filteredNodes.Where(res => res.Rank > 0);
        }

        // Shortens the helpful text so that the user is given a meaningful helptext string that can help
        // identify where the match was found, while not being too long such that the Data string isn't
        // just vomited onto the search result dropdown
        private static List<string> Process(IEnumerable<string> relatedStrings, string query)
        {
            var outList = new List<string>();
            foreach (string relatedString in relatedStrings)
            {
                var s = "";
                var e = "";
                int ind = relatedString.ToLower().IndexOf(query.ToLower(), StringComparison.Ordinal);

                if (ind < 0)
                {
                    outList.Add("No Helptext Available");
                    continue;
                }

                var pre = 0;
                while (ind - pre > 0 && pre < 5/* && !$"{relatedString[ind - pre]}".Equals("\r")*/) { pre++; }

                var post = 0;
                while (ind + post + query.Length < relatedString.Length && post < 5/* && !$"{relatedString[ind + post]}".Equals("\r")*/) { post++; }

                if (ind - pre != 0) s = "...";
                if (post == 5) e = "...";

                string processed = $"\" {s}{relatedString.Substring(ind - pre, pre + query.Length + post)}{e} \"";
                outList.Add(processed);
            }

            return outList;
        }

        public static void UnHighlightAllDocs()
        {
            //TODO:call this when search is unfocused
            //list of all collections
            var allCollections =
                MainPage.Instance.MainDocument.GetField<ListController<DocumentController>>(KeyStore.DataKey).TypedData;

            foreach (var coll in allCollections)
            {
                UnHighlightDocs(coll);
            }
        }

        public static void UnHighlightDocs(DocumentController coll)
        {
            var colDocs = coll.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null).TypedData;
            //unhighlight each doc in collection
            foreach (var doc in colDocs)
            {
                MainPage.Instance.HighlightDoc(doc, false, 2);
                if (doc.DocumentType.ToString() == "Collection Box")
                {
                    UnHighlightDocs(doc);
                }
            }
        }

        // Handles instances where the user inserted a colon, and determines whether or not the user meant to
        // search the colon as part of a string, or perform a parameterized search
        public static IEnumerable<SearchResult> GetBasicSearchResults(string searchPart)
        {
            searchPart = searchPart ?? " ";
            //if the part is a quote, it ignores the colon
            if (searchPart.Contains(":") && searchPart[0] != '"')
            {
                //   Debug.Assert(searchPart.Count(c => c == ':') == 1);//TODO handle the case of multiple ':'

                //splits after first colon
                var parts = searchPart.Split(':', 2).Select(s => s.Trim()).ToArray();
                //created a key field query function with both parts as parameters if parts[0] isn't a function name


                return ParameterizeFunction(parts[0], parts[1]); ;
            }
            return SearchByQuery(searchPart);
        }

        // Determines what kind of parameterized search the user intended
        private static IEnumerable<SearchResult> ParameterizeFunction(string name, string paramName)
        {
            // Workaround for calling search in collection, since the "in" keyword has system significance and
            // can't be used as an enum
            if (name.Equals("in"))
            {
                name = "inside";
            }

            // Not really sure what the point of it is, but it was in MainSearchBox, so I adapted it to the new search
            // All it does it do a search only taking into account rich text boxes
            if (name == "rtf" ||
                name == "rt" ||
                name == "richtext" ||
                name == "richtextformat")
            {
                var res = DocumentTree.MainPageTree.Where(node => node.DataDocument.EnumFields().Any(f => f.Value is RichTextController &&
                ((RichTextController)f.Value).SearchForStringInRichText(paramName).StringFound));
                return res.Select(node => new SearchResult(node, new List<string> { $" >> { name }" }, new List<string> { "\"" + paramName + "\"" }));
            }

            //If the user didn't input a DSL recognized function, then they probably intended to search for a
            // Key-value pair.
            //this returns a string that more closely follows function syntax
            if (!DSL.FuncNameExists(name))
            {
                return SearchByKeyValuePair(new KeyController(name), paramName.Trim('"'));
            }
            
            try
            {
                paramName = paramName.Trim('"');
                var resultDocs = DSL.Interpret(name + "(\"" + paramName + "\")");
                if (resultDocs is BaseListController resultList)
                {
                    var res = DocumentTree.MainPageTree.GetAllNodes().Where(node => resultList.Data.Contains(node.ViewDocument) ||
                    resultList.Data.Contains(node.DataDocument));
                    //return resultList.Data.Select(fcb => new SearchResult(fcb));

                    //TODO: Currently a band-aid fix, we shouldn't be searching for the node again after already searching
                    string trimParam = paramName.Length >= 10 ? paramName.Substring(0, 10) + "..." : paramName;
                    var relatedFields = new List<string> { $" >> Operator: { name }" };
                    switch (name)
                    {
                        case "before":
                            return res.Select(node => new SearchResult(node, relatedFields, new List<string> { "Modified at: " + node.DataDocument.GetField<Controllers.DateTimeController>(KeyStore.DateModifiedKey)?.Data }));
                        case "after":
                            return res.Select(node => new SearchResult(node, relatedFields, new List<string> { "Modified at: " + node.DataDocument.GetField<Controllers.DateTimeController>(KeyStore.DateModifiedKey)?.Data }));
                    }
                    return res.Select(node => new SearchResult(node, new List<string> { $" >> Operator: { name }" }, new List<string> { trimParam }));
                }
            }
            catch (Exception e)
            {
                return new List<SearchResult>();
            }
            return new List<SearchResult>();
        }

        // Finds the index of the next logical operator
        private static int FindNextDivider(string inputString)
        {
            var inParen = false;
            var parenCounter = 0;
            if (inputString.TrimStart('!').StartsWith("("))
            {
                inParen = true;
            }

            var inQuote = false;
            int len = inputString.Length;
            for (var i = 0; i < len; i++)
            {
                // if it starts with quotes, ignore parenthesis, if it starts with parenthesis, ignore quotes
                char curChar = inputString[i];
                if (curChar == '"')
                {
                    if (inQuote && !inParen)
                    {
                        inQuote = false;
                    }
                    else
                    {
                        inQuote = true;
                    }

                }
                else if (!inQuote && curChar == '(')
                {
                    inParen = true;
                    parenCounter += 1;
                }
                else if (!inQuote && inParen && curChar == ')')
                {
                    parenCounter -= 1;
                    if (parenCounter == 0)
                    {
                        inParen = false;
                    }
                }
                else if (!inQuote && !inParen && (curChar == ' ' || curChar == '|'))
                {
                    return i;
                }
            }
            return len;
        }

        // Assumes that the inputString starts with "(" or "!("
        private static int FindEndParenthesis(string inputString)
        {
            var parenCounter = 0;
            var inQuote = false;
            int len = inputString.Length;
            for (var i = 0; i < len; i++)
            {
                char curChar = inputString[i];
                if (curChar == '"')
                {
                    inQuote = !inQuote;
                }
                else if (!inQuote && curChar == '(')
                {
                    parenCounter += 1;
                }
                else if (!inQuote && curChar == ')')
                {
                    parenCounter -= 1;
                    if (parenCounter == 0)
                    {
                        return i;
                    }
                }           
            }
            return -1;
        }

        // Breaks down the user string while searching based on the desired logical operators and placement
        // of quotes/parenthesis
        public static IEnumerable<SearchResult> Parse(string inputString)
        {
            if (string.IsNullOrEmpty(inputString)) return new List<SearchResult>();



            var searchBoxLexer = new SearchGrammarLexer(new AntlrInputStream(inputString));
            var parser = new SearchGrammarParser(new CommonTokenStream(searchBoxLexer)) { BuildParseTree = true };
            var visitor = new DashSearchGrammarVisitor();
            var parseTree = visitor.Visit(parser.query());

            var results = new List<SearchResult>();
            var keyRefs = new List<string>();
            var fieldRefs = new List<string>();
            foreach (var node in DocumentTree.MainPageTree)//TODO .GetAllNodes()
            {
                var layoutResults = parseTree(node.ViewDocument);
                var dataResults = parseTree(node.DataDocument);
                foreach (var layoutResult in layoutResults)
                {
                    keyRefs.Add(layoutResult.Key.Name);
                    fieldRefs.Add(layoutResult.Value.RelatedString);
                }
                foreach (var dataResult in dataResults)
                {
                    keyRefs.Add(dataResult.Key.Name);
                    fieldRefs.Add(dataResult.Value.RelatedString);
                }

                if (keyRefs.Any())
                {
                    results.Add(new SearchResult(node, keyRefs, fieldRefs, layoutResults.Count + dataResults.Count));
                }

                keyRefs.Clear();
                fieldRefs.Clear();
            }

            return results;


            int dividerIndex = FindNextDivider(inputString);
            string searchTerm = inputString.Substring(0, dividerIndex);
            int negate = 0;
            for (int i = 0; i < searchTerm.Length; i++)
            {
                if (searchTerm[i] != '!')
                {
                    negate = i;
                    break;
                }
            }

            string modifiedSearchTerm = searchTerm.TrimStart('!');

            if (modifiedSearchTerm.Length > 2 && modifiedSearchTerm.StartsWith('"') && modifiedSearchTerm.EndsWith('"'))
            {
                modifiedSearchTerm = modifiedSearchTerm.Substring(1, modifiedSearchTerm.Length - 2);
            }

            string modInput = inputString.TrimStart('!');

            int endParenthesis = -2;

            // Making sure parenthesis doesn't clash with regex
            if ((modifiedSearchTerm.StartsWith("(") && !modifiedSearchTerm.EndsWith(")")))
            {
                endParenthesis = FindEndParenthesis(inputString);
            }


            IEnumerable<SearchResult> searchResults;
            if (endParenthesis > 0 || (inputString.TrimStart('!').StartsWith('(') && inputString.EndsWith(')') && (modInput.Contains(' ') || modInput.Contains('|'))))
            {
                string newInput = modInput.Substring(1, modInput.Length - 2);
                searchResults = Parse(newInput);
            }
            else
            {
                searchResults = GetBasicSearchResults(modifiedSearchTerm).Select(res => res.AddRtfTerm(new SearchTerm(modifiedSearchTerm)));
            }

            if (negate >= 0 && negate % 2 == 1)
            {
                var searchResultsList = NegateSearch(searchResults, modifiedSearchTerm);
                foreach (var res in searchResultsList)
                {
                    var list = new List<SearchTerm>();
                    foreach (var term in res.RtfHighlight)
                    {
                        list.Add(new SearchTerm(term._term, !term.Negate));
                    }
                    res.RtfHighlight = list;
                }
                searchResults = searchResultsList.AsEnumerable();
            }

            int len = inputString.Length;

            if (dividerIndex == len)
            {
                return searchResults;
            }

            char divider = inputString[dividerIndex];
            string rest = inputString.Substring(dividerIndex + 1);

            switch (divider)
            {
                case ' ':
                    return JoinTwoSearchesWithIntersection(searchResults, Parse(rest), modifiedSearchTerm);
                case '|':
                    return JoinTwoSearchesWithUnion(searchResults, Parse(rest));
                default:
                    throw new Exception("Unknown Divider");
            }
        }

        public struct SearchTerm {

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

        private static List<SearchResult> NegateSearch(IEnumerable<SearchResult> search, string term)
        {
            var results = DocumentTree.MainPageTree.GetAllNodes().Where(node => !search.Any(res => res.DataDocument == node.DataDocument || res.ViewDocument == node.ViewDocument));
            return results.Select(res => new SearchResult(res, new List<string>().Append(" >> N/A").ToList(), new List<string>().Append($"Negation Search: \"{term}\"").ToList())).ToList();
        }

        private static IEnumerable<SearchResult> JoinTwoSearchesWithUnion(
            IEnumerable<SearchResult> search1, IEnumerable<SearchResult> search2)
        {
            //probably won't work
            //return search1.Union(search2);
            var joined = new List<SearchResult>();
            foreach (var res in search1)
            {
                foreach (var res2 in search2)
                {
                    if (res2.ViewDocument == res.ViewDocument)
                    {
                        res.RtfHighlight.AddRange(res2.RtfHighlight);
                        res.FormattedKeyRef.AddRange(res2.FormattedKeyRef);
                        res.RelevantText.AddRange(res2.RelevantText);
                        break;
                    }
                }
                joined.Add(res);
            }

            foreach (var res in search2)
            {
                if (!joined.Any(res1 => res1.ViewDocument == res.ViewDocument))
                {
                    joined.Add(res);
                }
            }

            return joined;
        }

        private static IEnumerable<SearchResult> JoinTwoSearchesWithIntersection(IEnumerable<SearchResult> search1, IEnumerable<SearchResult> search2, string searchTerm)
        {
            //probably won't work
            //return search1.Intersection(search2);

            var search2List = search2.ToList();
            var joined = new List<SearchResult>();
            foreach (var result in search2List)
            {
                foreach (var res in search1)
                {
                    if (res.ViewDocument == result.ViewDocument)
                    {
                        result.FormattedKeyRef.AddRange(res.FormattedKeyRef);
                        result.RelevantText.AddRange(res.RelevantText);
                        result.RtfHighlight.Add(new SearchTerm(searchTerm));
                        joined.Add(result);
                        break;
                    }
                }
            }

            foreach (var result in search1)
            {
                if (joined.Any((node => node.ViewDocument == result.ViewDocument)))
                {
                    continue;
                }
                foreach (var res in search2List)
                {
                    if (result.ViewDocument == res.ViewDocument)
                    {
                        result.RtfHighlight.Add(new SearchTerm(searchTerm));
                        result.FormattedKeyRef.AddRange(res.FormattedKeyRef);
                        result.RelevantText.AddRange(res.RelevantText);
                        joined.Add(result);
                        break;
                    }
                }
            }
            return joined;
        }

        public static IEnumerable<SearchResult> SearchByAlias(string id, bool avoidDuplicateViews = false)
        {
            var doc = SearchIndividualById(id);
            var dataDoc = doc.GetDataDocument();
            var filteredNodes = DocumentTree.MainPageTree.GetAllNodes().Where(node => node.DataDocument.Equals(dataDoc));
            if (avoidDuplicateViews) filteredNodes = filteredNodes.Where(node => !node.ViewDocument.Equals(doc));
            return filteredNodes.Select(node => new SearchResult(node, new List<string>(), new List<string> { id }));
        }

        public static DocumentController SearchIndividualById(string id) => ContentController<FieldModel>.GetController<DocumentController>(id);
    }
}
