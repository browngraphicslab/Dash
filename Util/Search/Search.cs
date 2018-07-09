using System.Collections.Generic;
using System.Linq;

namespace Dash
{
    public class Search
    {
        public IEnumerable<DocumentNode> SearchByKeyValuePair(string key, string value)
        {
            return DocumentTree.MainPageTree.Where(node => node.DataDocument?.GetDereferencedField(new KeyController(key), null)?.SearchForString(value) != StringSearchModel.False).ToList();
        }

        public IEnumerable<DocumentNode> SearchByQuery(string query)
        {
            return DocumentTree.MainPageTree.Where(doc =>
            {
                foreach (var field in doc.ViewDocument.EnumDisplayableFields())
                {
                    if (field.Value.SearchForString(query) != StringSearchModel.False) return true;
                }
                foreach (var field in doc.DataDocument.EnumDisplayableFields())
                {
                    if (field.Value.SearchForString(query) != StringSearchModel.False) return true;
                }
                return false;
            }).ToList();
        }

        private IEnumerable<SearchResult> Results(IEnumerable<DocumentNode> nodes)
        {
            return null;
        }

        /*
         * Conducts a search of the content controller
         */
        private static IList<SearchResultViewModel> LocalSearch(string searchString)
        {
            var documentTree = DocumentTree.MainPageTree;
            var countToResults = new Dictionary<int, List<SearchResultViewModel>>();

            foreach (var documentController in DocumentTree.MainPageTree)
            {
                var foundCount = 0;
                var lastTopText = "";
                StringSearchModel lastKeySearch = null;
                StringSearchModel lastFieldSearch = null;

                foreach (var kvp in documentController.EnumDisplayableFields())
                {
                    var keySearch = StringSearchModel.False;//kvp.Key.SearchForString(searchString);
                    var fieldSearch = kvp.Value.Dereference(new Context(documentController))?.SearchForString(searchString) ?? StringSearchModel.False;

                    string topText = null;
                    if (fieldSearch.StringFound)
                    {
                        topText = kvp.Key.Name;
                    }
                    else if (keySearch.StringFound)
                    {
                        topText = "Name Of Key: " + keySearch.RelatedString;
                    }

                    if (keySearch.StringFound || fieldSearch.StringFound)
                    {
                        foundCount++;

                        //compare old search models to current one, trying to predict which would be better for the user to see
                        var newIsBetter = lastFieldSearch == null ||
                                          (lastFieldSearch.RelatedString?.Length ?? 0) <
                                          (fieldSearch.RelatedString?.Length ?? 0);
                        newIsBetter |= (lastFieldSearch?.RelatedString?.ToCharArray()?.Take(50)
                                            ?.Where(c => c == ' ')?.Count() ?? 0) <
                                       (fieldSearch?.RelatedString?.ToCharArray()?.Take(50)?.Where(c => c == ' ')
                                            ?.Count() ?? 0);

                        if (newIsBetter)
                        {
                            lastTopText = topText;
                            lastKeySearch = keySearch;
                            lastFieldSearch = fieldSearch;
                        }
                    }
                }

                if (foundCount > 0)
                {
                    var bottomText = (lastFieldSearch?.RelatedString ?? lastKeySearch?.RelatedString)
                        ?.Replace('\n', ' ').Replace('\t', ' ').Replace('\r', ' ');
                    var title = string.IsNullOrEmpty(documentController.Title)
                        ? lastTopText
                        : documentController.Title;

                    var vm = CreateSearchResults(documentTree, documentController, bottomText, title,
                        lastFieldSearch.IsUseFullRelatedString);

                    if (!countToResults.ContainsKey(foundCount))
                    {
                        countToResults.Add(foundCount, new List<SearchResultViewModel>());
                    }
                    countToResults[foundCount].AddRange(vm);
                }
                else if (documentController.Id.ToLower() == searchString)
                {
                    if (!countToResults.ContainsKey(1))
                    {
                        countToResults[1] = new List<SearchResultViewModel>();
                    }
                    countToResults[1].AddRange(CreateSearchResults(documentTree, documentController, "test", "test", true));
                }
            }

            return countToResults.OrderBy(kvp => -kvp.Key).SelectMany(i => i.Value);
        }

        /*
         * Creates a SearchResultViewModel and correctly fills in fields to help the user understand the search result
         */
        private static SearchResultViewModel[] CreateSearchResults(DocumentTree documentTree, DocumentController dataDocumentController, string bottomText, string titleText, bool isLikelyUsefulContextText = false)
        {
            var vms = new List<SearchResultViewModel>();
            var preTitle = "";

            var documentNodes = documentTree.GetNodesFromDataDocumentId(dataDocumentController.Id);
            foreach (var documentNode in documentNodes ?? new DocumentNode[0])
            {
                if (documentNode?.Parents?.FirstOrDefault() != null)
                {
                    preTitle = " >  " +
                        ((string.IsNullOrEmpty(documentNode.Parents.First().DataDocument
                                   .GetDereferencedField<TextController>(KeyStore.TitleKey, null)?.Data)
                                   ? "?"
                                   : documentNode.Parents.First().DataDocument
                                       .GetDereferencedField<TextController>(KeyStore.TitleKey, null)?.Data))
                             ;
                }

                var vm = new SearchResultViewModel(titleText + preTitle, bottomText ?? "",
                    dataDocumentController.Id,
                    documentNode?.ViewDocument ?? dataDocumentController,
                    documentNode?.Parents?.FirstOrDefault()?.ViewDocument, isLikelyUsefulContextText);
                vms.Add(vm);
            }

            return vms.ToArray();
        }
    }
}
