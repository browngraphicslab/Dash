using System.Collections;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class DocumentNode : IEnumerable<DocumentNode>
    {
        public IReadOnlyList<DocumentNode> Children { get; }

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

        private HashSet<DocumentController> foundDocuments;

        /*
         * Only constructor must have two documents, one for the view and one for the data.
         * They can be the same document
         */
        public DocumentNode(DocumentController viewDocument, DocumentNode parent, IDictionary<DocumentController, DocumentNode> nodes)
        {
            ViewDocument = viewDocument;
            DataDocument = ViewDocument.GetDataDocument();

            nodes.TryAdd(DataDocument, this);

            //all enum displayable fields, if list of doc cont = add all, if ref add doc it refs , hash set to  check if already found
            //each region and doc has link to and from - all vis
            //search through links + regions, discard link / region, parent null = dock
            Parent = parent;

            var childRegions = DataDocument.GetField<ListController<DocumentController>>(KeyStore.RegionsKey);
            IEnumerable<FieldControllerBase> childDocControllers2 = viewDocument.EnumDisplayableFields().Select(x => x.Value);
            var childDocControllers = DataDocument.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
             Children = childDocControllers == null ? new List<DocumentNode>() : childDocControllers.Select(child => new DocumentNode(child, this, nodes)).ToList();
            //maybe add Children to hash set here?
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
    }
}
