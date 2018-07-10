using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class Search
    {
        public static IEnumerable<SearchResult> SearchByKeyValuePair(KeyController key, string value, bool negate = false)
        {
            var filteredNodes = DocumentTree.MainPageTree.Select(node =>
            {
                var stringSearchModel = node.DataDocument?.GetDereferencedField(key, null)?.SearchForString(value);
                var matchLength = stringSearchModel == null ? 0 :
                    (stringSearchModel == StringSearchModel.False) ? 0 : stringSearchModel.RelatedString.Length;
                return new SearchResult(node, stringSearchModel?.RelatedString, matchLength);
            }).OrderByDescending(res => res.Rank);

            return negate ? filteredNodes.Where(res => res.Rank == 0) : filteredNodes.Where(res => res.Rank > 0);
        }

        public static IEnumerable<SearchResult> SearchByQuery(string query, bool negate = false)
        {
            var filteredNodes = DocumentTree.MainPageTree.Select(node =>
            { 
                var numMatchedFields = node.ViewDocument.EnumDisplayableFields().Count(field => field.Value.DereferenceToRoot(null).SearchForString(query) != StringSearchModel.False) + 
                                       node.DataDocument.EnumDisplayableFields().Count(field => field.Value.DereferenceToRoot(null).SearchForString(query) != StringSearchModel.False);
                return new SearchResult(node, query, numMatchedFields);
            })
                .OrderByDescending(res => res.Rank);

            return negate ? filteredNodes.Where(res => res.Rank == 0) : filteredNodes.Where(res => res.Rank > 0);
        }

        private IEnumerable<SearchResult> GetBasicSearchResults(string searchPart, bool negate = false)
        {
            searchPart = searchPart ?? " ";
            //if the part is a quote, it ignores the colon
            if (searchPart.Contains(":") && searchPart[0] != '"')
            {
                //   Debug.Assert(searchPart.Count(c => c == ':') == 1);//TODO handle the case of multiple ':'

                //splits after first colon
                var parts = searchPart.Split(':', 2).Select(s => s.Trim()).ToArray();
                //created a key field query function with both parts as parameters if parts[0] isn't a function name

                return SearchByKeyValuePair(new KeyController(parts[0]), parts[1], negate);
            }
            else
            {
                return SearchByQuery(searchPart, negate);
            }
        }

        private int FindNextDivider(string inputString)
        {
            bool inParen = false;
            int parenCounter = 0;
            if (inputString.TrimStart('!').StartsWith("("))
            {
                inParen = true;
            }

            bool inQuote = false;
            int len = inputString.Length;
            for (int i = 0; i < len; i++)
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
        private int FindEndParenthesis(string inputString)
        {
            int parenCounter = 0;
            bool inQuote = false;
            int len = inputString.Length;
            for (int i = 0; i < len; i++)
            {
                char curChar = inputString[i];
                if (curChar == '"')
                {
                    if (inQuote)
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

            for (int i = 0; i < len - (rep1 - 1); i++)
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

        private IEnumerable<SearchResult> Parse(string inputString)
        {
            int dividerIndex = FindNextDivider(inputString);
            string searchTerm = inputString.Substring(0, dividerIndex);
            bool isNegated = searchTerm.StartsWith("!");
            string modifiedSearchTerm = searchTerm.TrimStart('!');

            if (modifiedSearchTerm.StartsWith('"') && modifiedSearchTerm.EndsWith('"'))
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


            IEnumerable<SearchResult> searchDict;
            if (endParenthesis > 0 || (inputString.StartsWith('(') && inputString.EndsWith(')') && (modInput.Contains(' ') || modInput.Contains('|'))))
            {
                string newInput = modInput.Substring(1, modInput.Length - 2);
                searchDict = Parse(newInput);
            }
            else
            {
                searchDict = GetBasicSearchResults(modifiedSearchTerm, isNegated);
            }


            int len = inputString.Length;

            if (dividerIndex == len)
            {
                return searchDict;
            }
            else
            {
                char divider = inputString[dividerIndex];
                string rest = inputString.Substring(dividerIndex + 1);

                if (divider == ' ')
                {
                    return JoinTwoSearchesWithIntersection(searchDict, Parse(rest));
                }
                else if (divider == '|')
                {
                    return JoinTwoSearchesWithUnion(searchDict, Parse(rest));
                }
                else
                {
                    throw new Exception("Unknown Divider");
                }

            }
        }

        private IEnumerable<SearchResult> JoinTwoSearchesWithIntersection(
            IEnumerable<SearchResult> search1, IEnumerable<SearchResult> search2)
        {
            //probably won't work
            //return search1.Intersect(search2);

            return (search1.Concat(search2)).DistinctBy(node => node.ViewDocument);
        }

        private IEnumerable<SearchResult> JoinTwoSearchesWithUnion(
    IEnumerable<SearchResult> search1, IEnumerable<SearchResult> search2)
        {
            //probably won't work
            //return search1.Union(search2);

            var joined = new List<SearchResult>();

            foreach (var result in search1)
            {
                if (search2.Any(node => node.ViewDocument == result.ViewDocument))
                    joined.Add(result);
            }
            foreach (var result in search2)
            {
                if (search1.Any(node => node.ViewDocument == result.ViewDocument) &&
                    !joined.Any((node => node.ViewDocument == result.ViewDocument)))
                    joined.Add(result);
                //TODO: combine information from the repeated results
            }
            return joined;
        }



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
