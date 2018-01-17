using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    /// <summary>
    /// This class can be created at any time to give an immediate tree-representation of all documents in the collection hierarchy stemming form the input parameter document.
    /// This tree will NOT maintain state as the documents and relationships change, rather this will only give the tree structure at the moment of instantiation
    /// </summary>
    public class DocumentTree
    {
        private Dictionary<string, DocumentNode> _nodes = new Dictionary<string, DocumentNode>();
        private HashSet<DocumentNode> _parsed = new HashSet<DocumentNode>();
        private DocumentNode Head { get; }
        private Dictionary<string, DocumentNode> _controllerIdMap = new Dictionary<string, DocumentNode>();
        public DocumentTree(DocumentController parseStart)
        {
            Head = CreateNode(parseStart);
            Parse(Head);
            MakeControllerIdMap();
        }

        /// <summary>
        /// returns a new instance of DocumentTree with the main page as the start of the parse
        /// </summary>
        public static DocumentTree MainPageTree
        {
            get
            {
                return new DocumentTree(MainPage.Instance.MainDocument);
            }
        }

        public DocumentNode this[string id]
        {
            get { return _controllerIdMap.ContainsKey(id) ? _controllerIdMap[id] : null; }
        }

        private void MakeControllerIdMap()
        {
            foreach (DocumentNode node in _nodes.Values)
            {
                _controllerIdMap[node.DataDocument.Id] = node;
                _controllerIdMap[node.ViewDocument.Id] = node;
            }
        }


        private DocumentNode CreateNode(DocumentController document)
        {
            var node = new DocumentNode(document);
            if (_nodes.ContainsKey(node.Id))
            {
                return _nodes[node.Id];
            }
            else
            {
                _nodes.Add(node.Id, node);
                return node;
            }
        }
        

        private void Parse(DocumentNode node)
        {
            var childDocuments = node.DataDocument.GetField<ListController<DocumentController>>(KeyStore.CollectionKey);
            var childNodes = new List<DocumentNode>(childDocuments.Count);
            foreach (var childDoc in childDocuments.TypedData)
            {
                var childnode = CreateNode(childDoc);
                node.AddChild(childnode);
                childNodes.Add(node);
            }
            foreach (var childNode in childNodes)
            {
                if (!_parsed.Contains(childNode))
                {
                    _parsed.Add(childNode);
                    Parse(childNode);
                }
            }
        }
    }
}
