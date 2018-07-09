using System;
using System.Collections;
using System.Collections.Generic;

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

        public DocumentTree(DocumentController headRef) => Head = new DocumentNode(headRef, null, Nodes);

        /*
         * Returns a new instance of DocumentTree with the main page as the start of the recursive tree construction
         */
        public static DocumentTree MainPageTree => new DocumentTree(MainPage.Instance.MainDocument);

        public IEnumerator<DocumentNode> GetEnumerator() => Head.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
