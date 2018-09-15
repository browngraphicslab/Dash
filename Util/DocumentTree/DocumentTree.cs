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

        public static List<List<DocumentController>> GetPathsToDocuments(DocumentController doc, bool useDataDoc = true)
        {
            List<DocumentNode> nodes;

            if (useDataDoc)
            {
                var dataDoc = doc.GetDataDocument();
                nodes = MainPageTree.Where(node => node.DataDocument.Equals(dataDoc)).ToList();
            }
            else
            {
                nodes = MainPageTree.Where(node => node.ViewDocument.Equals(doc)).ToList();
            }

            var paths = new List<List<DocumentController>>(nodes.Count);
            foreach (var node in nodes)
            {
                var path = new List<DocumentController>();
                var currentNode = node;
                while (currentNode != null)
                {
                    path.Add(currentNode.ViewDocument);
                    currentNode = currentNode.Parent;
                }

                path.Reverse();
                paths.Add(path);
            }

            return paths;
        }

        public IEnumerator<DocumentNode> GetEnumerator() => Head.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
