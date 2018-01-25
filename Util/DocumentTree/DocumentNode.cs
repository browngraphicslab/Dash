using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class DocumentNode
    {
        private readonly Dictionary<string,DocumentNode> _children = new Dictionary<string, DocumentNode>();
        private readonly Dictionary<string, DocumentNode> _parents= new Dictionary<string, DocumentNode>();

        public DocumentTree Tree { get; }

        /// <summary>
        /// the Groups that it's in
        /// </summary>
        private readonly Dictionary<string, DocumentNode> _groups = new Dictionary<string, DocumentNode>();

        public DocumentNode[] Children
        {
            get { return _children.Values.ToArray(); }
        }
        public DocumentNode[] Parents
        {
            get { return _parents.Values.ToArray(); }
        }

        /// <summary>
        /// the groups that this node is in
        /// </summary>
        public DocumentNode[] Groups
        {
            get { return _groups.Values.ToArray(); }
        }


        /// <summary>
        /// the groups that this node is in
        /// </summary>
        public DocumentNode[] GroupPeers
        {
            get
            {
                var result = _groups.Values.SelectMany(i => i.DataDocument.GetField<ListController<DocumentController>>(KeyStore.GroupingKey)?.TypedData ?? new List<DocumentController>(){i.DataDocument}).Select(i => Tree[i.Id]).ToArray();
                return result;
            }
        }


        /// <summary>
        /// The Id of this DocumentNode
        /// </summary>
        public string Id
        {
            get { return ViewDocument.Id + DataDocument.Id; }
        }

        /// <summary>
        /// the data Document that this tree/graph node represents.
        /// This can be the same as the view document
        /// </summary>
        public DocumentController DataDocument { get; }

        /// <summary>
        /// the view Document that this tree/graph node represents.
        /// This can be the same as the data document
        /// </summary>
        public DocumentController ViewDocument { get; }

        /// <summary>
        /// Only constructor must have two documents, one for the view and one for the data.
        /// They can be the same document
        /// </summary>
        /// <param name="viewDocument"></param>
        /// <param name="dataDocument"></param>
        public DocumentNode(DocumentController viewDocument, DocumentTree tree)
        {
            Tree = tree;
            ViewDocument = viewDocument;
            DataDocument = ViewDocument.GetDataDocument();
        }

        public void AddParent(DocumentNode parent)
        {
            if (!_parents.ContainsKey(parent.Id))
            {
                _parents[parent.Id] = parent;
                parent.AddChild(this);
            }
        }

        public void AddParentGroup(DocumentNode parentGroup)
        {
            if (!_groups.ContainsKey(parentGroup.Id))
            {
                _groups[parentGroup.Id] = parentGroup;
            }
        }


        public void AddChild(DocumentNode child)
        {
            if (!_children.ContainsKey(child.Id))
            {
                _children[child.Id] = child;
                child.AddParent(this);
            }
        }

        public override bool Equals(object obj)
        {
            return (obj is DocumentNode) && ((DocumentNode) obj).Id.Equals(Id);
        }
    }
}
