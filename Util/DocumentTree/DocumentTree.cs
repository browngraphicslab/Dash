using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private Dictionary<string, DocumentNode> _controllerViewIdMap = new Dictionary<string, DocumentNode>();
        private Dictionary<string, List<DocumentNode>> _controllerDataIdMap = new Dictionary<string, List<DocumentNode>>();
        public DocumentTree(DocumentController parseStart)
        {
            Head = CreateNode(parseStart, new DocumentNodeGroup());
            Parse(Head);
            MakeControllerIdMap();
        }

        /// <summary>
        /// returns a new instance of DocumentTree with the main page as the start of the parse
        /// </summary>
        public static DocumentTree MainPageTree => new DocumentTree(MainPage.Instance.MainDocument);

        public DocumentNode GetNodeFromViewId(string viewId)
        {
            return viewId != null && _controllerViewIdMap.ContainsKey(viewId) ? _controllerViewIdMap[viewId] : null;;
        }


        public DocumentNode[] GetNodesFromDataDocumentId(string dataDocumentId)
        {
            return dataDocumentId != null && _controllerDataIdMap.ContainsKey(dataDocumentId) ? _controllerDataIdMap[dataDocumentId].ToArray() : null; ;
        }



        private void MakeControllerIdMap()
        {
            foreach (DocumentNode node in _nodes.Values)
            {
                if (!_controllerDataIdMap.ContainsKey(node.DataDocument.Id))
                {
                    _controllerDataIdMap[node.DataDocument.Id] = new List<DocumentNode>();
                }
                _controllerDataIdMap[node.DataDocument.Id].Add(node);
                _controllerViewIdMap[node.ViewDocument.Id] = node;
            }
        }


        private DocumentNode CreateNode(DocumentController document, DocumentNodeGroup group)
        {
            var node = new DocumentNode(document, group, this);
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
            throw new NotImplementedException();
            _parsed.Add(node);
            var childDocuments = node.DataDocument.GetField<ListController<DocumentController>>(KeyStore.CollectionKey)?.TypedData?.Where(i => i != null)?.ToList() ?? new List<DocumentController>();
            //var groups = node.DataDocument.GetField<ListController<DocumentController>>(KeyStore.GroupingKey)
                //?.TypedData ?? new List<DocumentController>();
            var groupDict = new Dictionary<string, DocumentNodeGroup>();

            //foreach (var group in groups)
            //{
            //    var groupList = group.GetField<ListController<DocumentController>>(KeyStore.GroupingKey);
            //    if (groupList == null) //Group of 1
            //    {
            //        var documentNodeGroup = new DocumentNodeGroup();
            //        groupDict[group.Id] = documentNodeGroup;
            //    }
            //    else
            //    {
            //        var groupNode = new DocumentNodeGroup();
            //        foreach (var documentController in groupList.TypedData)
            //        {
            //            groupDict[documentController.Id] = groupNode;
            //        }
            //    }
            //}

            //childDocuments.AddRange(node.ViewDocument.GetField<ListController<DocumentController>>(KeyStore.CollectionKey)?.TypedData?.Where(i => i != null) ?? new List<DocumentController>());

            //var childPossibleGroups = node.DataDocument.GetField<ListController<DocumentController>>(KeyStore.GroupingKey)?.TypedData?.Where(i => i != null)?.ToList() ?? new List<DocumentController>();
            var childNodes = new Dictionary<string,DocumentNode>(childDocuments.Count);

            //create document nodes and add child-parent relationships
            foreach (var childDoc in childDocuments)
            {
                if (!groupDict.ContainsKey(childDoc.Id))
                {
                    //Debug.WriteLine("FIX ME: DocumentTree has document without group");
                    continue;
                }
                var childNode = CreateNode(childDoc, groupDict[childDoc.Id]);
                Debug.Assert(childNode != null);
                node.AddChild(childNode);
                childNodes.Add(childDoc.Id, childNode);
            }

            //recuresively parse
            var toParse = childNodes.Values.Where(i => !_parsed.Contains(i)).ToList();
            foreach (var childNode in toParse)
            {
                Parse(childNode);
            }
        }

        public class DocumentNodeGroup
        {
            private List<DocumentNode> _members = new List<DocumentNode>();

            public void AddMember(DocumentNode node)
            {
                _members.Add(node);
            }

            public DocumentNode[] Members
            {
                get { return _members.ToArray(); }
            }
        }
    }
}
