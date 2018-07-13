using System;
using System.Collections.Generic;
using System.Linq;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class Search
    {
        //TODO: Type-Based Search
        //TODO: Search in collections - check out "collected docs note in viewdocument.getdatadocument.documenttype.type

        public static IEnumerable<SearchResult> SearchByKeyValuePair(KeyController key, string value, bool negate = false)
        {
            var filteredNodes = DocumentTree.MainPageTree.Select(node =>
            {
                var stringSearchModel = node.DataDocument?.GetDereferencedField(key, null)?.SearchForString(value);
                int matchLength = stringSearchModel == null ? 0 :
                    (stringSearchModel == StringSearchModel.False) ? 0 : stringSearchModel.RelatedString.Length;
                return new SearchResult(node, $" >> {key}", $"\" {stringSearchModel?.RelatedString} \"", matchLength);
            }).OrderByDescending(res => res.Rank);

            return negate ? filteredNodes.Where(res => res.Rank == 0) : filteredNodes.Where(res => res.Rank > 0);
        }

        public static IEnumerable<SearchResult> SearchByQuery(string query, bool negate = false)
        {
            var filteredNodes = DocumentTree.MainPageTree.Select(node =>
            {
                var relatedString = "";
                KeyController relatedField = null;

                var numMatchedFields = 0;
                foreach (var field in node.ViewDocument.EnumDisplayableFields())
                {
                    var ssm = field.Value.DereferenceToRoot(null).SearchForString(query);
                    if (ssm == StringSearchModel.False) continue;

                    if (string.IsNullOrEmpty(relatedString))
                    {
                        relatedString = ssm.RelatedString;
                        relatedField = field.Key;
                    }
                    numMatchedFields++;
                }
                foreach (var field in node.DataDocument.EnumDisplayableFields())
                {
                    var ssm = field.Value.DereferenceToRoot(null).SearchForString(query);
                    if (ssm == StringSearchModel.False) continue;

                    if (string.IsNullOrEmpty(relatedString))
                    {
                        relatedString = ssm.RelatedString;
                        relatedField = field.Key;
                    }
                    numMatchedFields++;
                }

                var s = "";
                var e = "";
                if (!string.IsNullOrEmpty(relatedString))
                {
                    int ind = relatedString.ToLower().IndexOf(query.ToLower(), StringComparison.Ordinal);
                    //TODO: ugly code in the following 2 lines, fix later
                    if (ind < 0)
                        return new SearchResult(node, $" >> { relatedField }", $"\" {s}{relatedString}{e} \" ", numMatchedFields);
                    var pre = 0;
                    while (ind - pre > 0 && pre < 5/* && !$"{relatedString[ind - pre]}".Equals("\r")*/) { pre++; }

                    var post = 0;
                    while (ind + post + query.Length < relatedString.Length && post < 5/* && !$"{relatedString[ind + post]}".Equals("\r")*/) { post++; }

                    if (ind - pre != 0) s = "...";
                    if (post == 5) e = "...";

                    relatedString = relatedString.Substring(ind - pre, pre + query.Length + post);
                }

                    return new SearchResult(node, $" >> { relatedField }", $"\" {s}{relatedString}{e} \" ", numMatchedFields);
            })
                .OrderByDescending(res => res.Rank);

            return negate ? filteredNodes.Where(res => res.Rank == 0) : filteredNodes.Where(res => res.Rank > 0);
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

        public static IEnumerable<SearchResult> GetBasicSearchResults(string searchPart, bool negate = false)
        {
            searchPart = searchPart ?? " ";
            //if the part is a quote, it ignores the colon
            if (searchPart.Contains(":") && searchPart[0] != '"')
            {
                //   Debug.Assert(searchPart.Count(c => c == ':') == 1);//TODO handle the case of multiple ':'

                //splits after first colon
                var parts = searchPart.Split(':', 2).Select(s => s.Trim()).ToArray();
                //created a key field query function with both parts as parameters if parts[0] isn't a function name


                return ParameterizeFunction(parts[0], parts[1], negate); ;
            }
            return SearchByQuery(searchPart, negate);
        }

        private static IEnumerable<SearchResult> ParameterizeFunction(string name, string paramName, bool negate)
        {
            //this returns a string that more closely follows function syntax
            //TODO check if func exists

            if (!DSL.FuncNameExists(name))
            {
                return SearchByKeyValuePair(new KeyController(name), paramName, negate);
            }
            try
            {
                paramName = paramName.Trim('"');
                var resultDocs = DSL.Interpret(name + "(\"" + paramName + "\")");
                if (resultDocs is BaseListController resultList)
                {
                    var res = DocumentTree.MainPageTree.Where(node => resultList.Data.Contains(node.ViewDocument));
                    //return resultList.Data.Select(fcb => new SearchResult(fcb));

                    //TODO: Currently a band-aid fix, we shouldn't be searching for the node again after already searching
                    string trimParam = paramName.Length >= 10 ? paramName.Substring(0, 10) + "..." : paramName;
                    string newParam = "";
                    return res.Select(node => new SearchResult(node, $" >> Operator: { name }", trimParam, 1));
                }
            }
            catch (Exception e)
            {
                return new List<SearchResult>();
            }
            return new List<SearchResult>();
        }

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

        private string SelectivelyReplace(string inputString, string toReplace, string toIgnore, string replaceWith)
        {
            int len = inputString.Length;
            int rep1 = toReplace.Length;
            int rep2 = toIgnore.Length;
            int repW1 = replaceWith.Length;

            for (var i = 0; i < len - (rep1 - 1); i++)
            {
                if (len - i > rep2 - 1 && inputString.Substring(i, rep2).Equals(toIgnore))
                {
                    i += rep2 - 1;
                }
                else if (inputString.Substring(i, rep1).Equals(toReplace))
                {
                    inputString = inputString.Remove(i, rep1).Insert(i, replaceWith);
                    i += repW1 - 1;

                }
            }
            return inputString;
        }

        public static IEnumerable<SearchResult> Parse(string inputString)
        {
            if (string.IsNullOrEmpty(inputString))
            {
                return new List<SearchResult>();
            }
            int dividerIndex = FindNextDivider(inputString);
            string searchTerm = inputString.Substring(0, dividerIndex);
            bool isNegated = searchTerm.StartsWith("!");
            string modifiedSearchTerm = searchTerm.TrimStart('!');

            if (modifiedSearchTerm.Length > 2 && modifiedSearchTerm.StartsWith('"') && modifiedSearchTerm.EndsWith('"'))
            {
                modifiedSearchTerm = modifiedSearchTerm.Substring(1, modifiedSearchTerm.Length - 2);
            }

            string modInput = inputString.TrimStart('!');

            int endParenthesis = -2;

            // Making sure parenthesis doesn't clash with regex
            if ((modifiedSearchTerm.StartsWith("(") && !modifiedSearchTerm.EndsWith(")")) ||
                (isNegated && modifiedSearchTerm.StartsWith("(") && modifiedSearchTerm.EndsWith(")")))
            {
                endParenthesis = FindEndParenthesis(inputString);
            }


            IEnumerable<SearchResult> searchResults;
            if (endParenthesis > 0 || (inputString.StartsWith('(') && inputString.EndsWith(')') && (modInput.Contains(' ') || modInput.Contains('|'))))
            {
                string newInput = modInput.Substring(1, modInput.Length - 2);
                searchResults = Parse(newInput);
            }
            else
            {
                searchResults = GetBasicSearchResults(modifiedSearchTerm, isNegated);
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
                    return JoinTwoSearchesWithIntersection(searchResults, Parse(rest));
                case '|':
                    return JoinTwoSearchesWithUnion(searchResults, Parse(rest));
                default:
                    throw new Exception("Unknown Divider");
            }
        }

        private static IEnumerable<SearchResult> JoinTwoSearchesWithUnion(
            IEnumerable<SearchResult> search1, IEnumerable<SearchResult> search2)
        {
            //probably won't work
            //return search1.Union(search2);

            return (search1.Concat(search2)).DistinctBy(node => node.ViewDocument);
        }

        private static IEnumerable<SearchResult> JoinTwoSearchesWithIntersection(IEnumerable<SearchResult> search1, IEnumerable<SearchResult> search2)
        {
            //probably won't work
            //return search1.Intersection(search2);

            var search1List = search1.ToList();
            var joined = search1List.Where(result => search2.Any(node => node.ViewDocument == result.ViewDocument)).ToList();

            foreach (var result in search2)
            {
                if (search1List.Any(node => node.ViewDocument == result.ViewDocument) &&
                    !joined.Any((node => node.ViewDocument == result.ViewDocument)))
                    joined.Add(result);
                //TODO: combine information from the repeated results
            }
            return joined;
        }

        public static IEnumerable<SearchResult> SearchByAlias(string id, bool avoidDuplicateViews = false)
        {
            var doc = SearchIndividualById(id);
            var dataDoc = doc.GetDataDocument();
            var filteredNodes = DocumentTree.MainPageTree.Where(node => node.DataDocument.Equals(dataDoc));
            if (avoidDuplicateViews) filteredNodes = filteredNodes.Where(node => !node.ViewDocument.Equals(doc));
            return filteredNodes.Select(node => new SearchResult(node, "", id));
        }

        public static DocumentController SearchIndividualById(string id) => ContentController<FieldModel>.GetController<DocumentController>(id) ?? new DocumentController();

        /*
         * Creates a SearchResultViewModel and correctly fills in fields to help the user understand the search result
         */
        //private static SearchResultViewModel[] CreateSearchResults(DocumentTree documentTree, DocumentController dataDocumentController, string bottomText, string titleText, bool isLikelyUsefulContextText = false)
        //{
        //    var vms = new List<SearchResultViewModel>();
        //    var preTitle = "";

        //    var documentNodes = documentTree.GetNodesFromDataDocumentId(dataDocumentController.Id);
        //    foreach (var documentNode in documentNodes ?? new DocumentNode[0])
        //    {
        //        if (documentNode?.Parents?.FirstOrDefault() != null)
        //        {
        //            preTitle = " >  " +
        //                ((string.IsNullOrEmpty(documentNode.Parents.First().DataDocument
        //                           .GetDereferencedField<TextController>(KeyStore.TitleKey, null)?.Data)
        //                           ? "?"
        //                           : documentNode.Parents.First().DataDocument
        //                               .GetDereferencedField<TextController>(KeyStore.TitleKey, null)?.Data))
        //                     ;
        //        }

        //        var vm = new SearchResultViewModel(titleText + preTitle, bottomText ?? "",
        //            dataDocumentController.Id,
        //            documentNode?.ViewDocument ?? dataDocumentController,
        //            documentNode?.Parents?.FirstOrDefault()?.ViewDocument, isLikelyUsefulContextText);
        //        vms.Add(vm);
        //    }

        //    return vms.ToArray();
        //}
    }
}
