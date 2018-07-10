using System.Collections.Generic;
using System.Linq;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public static class Search
    {
        public static IEnumerable<DocumentNode> GetAllDocs()
        {
            return DocumentTree.MainPageTree;
        }

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

        public static IEnumerable<SearchResult> SearchByQuery(string query)
        {
            return DocumentTree.MainPageTree.Select(node =>
            { 
                var numMatchedFields = node.ViewDocument.EnumDisplayableFields().Count(field => field.Value.DereferenceToRoot(null).SearchForString(query) != StringSearchModel.False) + 
                                       node.DataDocument.EnumDisplayableFields().Count(field => field.Value.DereferenceToRoot(null).SearchForString(query) != StringSearchModel.False);
                return new SearchResult(node, query, numMatchedFields);
            })
                .Where(res => res.Rank > 0)
                .OrderByDescending(res => res.Rank)
                .ToList();
        }

        public static IEnumerable<SearchResult> SearchByAlias(string id, bool avoidDuplicateViews = false)
        {
            var doc = SearchIndividualById(id);
            var dataDoc = doc.GetDataDocument();
            var filteredNodes = DocumentTree.MainPageTree.Where(node => node.DataDocument.Equals(dataDoc));
            if (avoidDuplicateViews) filteredNodes = filteredNodes.Where(node => !node.ViewDocument.Equals(doc));
            return filteredNodes.Select(node => new SearchResult(node, id));
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
