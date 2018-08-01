using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Dash
{
    /// <inheritdoc />
    /// <summary>
    /// This class can be created at any time to give an immediate tree-representation of all documents in the collection hierarchy stemming form the input parameter document.
    /// This tree will NOT maintain state as the documents and relationships change, rather this will only give the tree structure at the moment of instantiation
    /// </summary>
    public class DocumentTree : IEnumerable<DocumentNode>
    {
        private DocumentNode Head { get; }
        public Dictionary<DocumentController, DocumentNode> Nodes = new Dictionary<DocumentController, DocumentNode>();

        public DocumentTree(DocumentController headRef)
        {
            var title = headRef.GetField<TextController>(KeyStore.TitleKey);
            headRef.SetField<TextController>(KeyStore.TitleKey, $"*{title}*", true);
            Head = new DocumentNode(headRef, null, Nodes);
        }

        public IEnumerable<DocumentNode> GetAllNodes()
        {
            var toSearch = new List<DocumentController>();
            var cachedNodes = new Dictionary<DocumentController, DocumentNode>();

            toSearch.Add(Head.ViewDocument);

            while (toSearch.Any())
            {
                var doc = toSearch.Last();
                toSearch.RemoveAt(toSearch.Count - 1);
                if (doc.GetField(KeyStore.RegionsKey) == null && doc.GetField(KeyStore.LinkDestinationKey) == null)
                {
                    cachedNodes[doc] = new DocumentNode(doc, null, null);
                }

                //var region = (doc.GetDataDocument().GetField(KeyStore.RegionsKey) as DocumentController).GetDataDocument();
                //var regions = (region.GetLinks(KeyStore.LinkToKey)?.TypedData.ToList() ?? new List<DocumentController>()).Concat(
                //    region.GetLinks(KeyStore.LinkFromKey)?.TypedData.ToList() ?? new List<DocumentController>());
                //foreach (var reg in regions)
                //{
                //    if (cachedNodes.ContainsKey(reg))
                //    {
                //        continue;
                //    } else
                //    {
                //        cachedNodes[reg] = new DocumentNode(reg, null, null);
                //    }
                //}

                var dfields = doc.EnumDisplayableFields().ToList();
                foreach (var enumDisplayableField in dfields)
                {
                    if (enumDisplayableField.Value is DocumentController docField)
                    {
                        if (cachedNodes.ContainsKey(docField))
                        {
                            continue;
                        }
                        toSearch.Add(docField);
                    } else if(enumDisplayableField.Value is ListController<DocumentController> listField)
                    {
                        foreach (var documentController in listField)
                        {
                            if (cachedNodes.ContainsKey(documentController))
                            {
                                continue;
                            }
                            toSearch.Add(documentController);
                        }
                    }
                }

                var dataDoc = doc.GetDataDocument();
                if (!dataDoc.Equals(doc))
                {
                    foreach (var enumDisplayableField in dataDoc.EnumDisplayableFields())
                    {
                        if (enumDisplayableField.Value is DocumentController docField)
                        {
                            if (cachedNodes.ContainsKey(docField))
                            {
                                continue;
                            }
                            toSearch.Add(docField);
                        }
                        else if (enumDisplayableField.Value is ListController<DocumentController> listField)
                        {
                            foreach (var documentController in listField)
                            {
                                if (cachedNodes.ContainsKey(documentController))
                                {
                                    continue;
                                }
                                toSearch.Add(documentController);
                            }
                        }
                    }
                }
            }

            var l = this.ToList();
            l.AddRange(cachedNodes.Values.Where(n => !l.Contains(n)));
            return l;
        }

        /*
         * Returns a new instance of DocumentTree with the main page as the start of the recursive tree construction
         */
        public static DocumentTree MainPageTree => new DocumentTree(MainPage.Instance.MainDocument);

        public IEnumerator<DocumentNode> GetEnumerator() => Head.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
