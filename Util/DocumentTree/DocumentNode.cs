using System.Collections;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class DocumentNode : IEnumerable<DocumentNode>
    {
        private List<DocumentNode> _children;

        public IReadOnlyList<DocumentNode> Children
        {
            get
            {
                if (_children == null)
                {
                    var childDocControllers = ViewDocument.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
                    _children = childDocControllers == null ? new List<DocumentNode>() : childDocControllers.Select(child => new DocumentNode(child, this)).ToList();
                }

                return _children;
            }
        }

        public DocumentNode Parent { get; }

        /*
         * The data Document that this tree/graph node represents.
         * This can be the same as the view document
         */
        public DocumentController DataDocument { get; }

        /*
         * the view Document that this tree/graph node represents.
         * This can be the same as the data document
         */
        public DocumentController ViewDocument { get; }

        /*
         * Only constructor must have two documents, one for the view and one for the data.
         * They can be the same document
         */
        public DocumentNode(DocumentController viewDocument, DocumentNode parent)
        {
            ViewDocument = viewDocument;
            DataDocument = ViewDocument.GetDataDocument();



            //all enum displayable fields, if list of doc cont = add all, if ref add doc it refs , hash set to  check if already found
            //each region and doc has link to and from - all vis
            //search through links + regions, discard link / region, parent null = dock
            Parent = parent;
        }

        public IEnumerator<DocumentNode> GetEnumerator()
        {
            yield return this;
            foreach (var child in Children)
            {
                foreach (var node in child)
                {
                    yield return node;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override bool Equals(object obj)
        {
            var node = obj as DocumentNode;
            return node != null &&
                   EqualityComparer<DocumentController>.Default.Equals(ViewDocument, node.ViewDocument);
        }

        public override int GetHashCode()
        {
            return 1363513657 + EqualityComparer<DocumentController>.Default.GetHashCode(ViewDocument);
        }
    }
}
