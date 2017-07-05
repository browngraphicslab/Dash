using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Dash
{
    /// <summary>
    /// A field model which encapsulates a list of Documents
    /// </summary>
    public class DocumentCollectionFieldModel : FieldModel
    {
        /// <summary>
        /// The list of <see cref="DocumentModel.Id"/> which the <see cref="DocumentCollectionFieldModel"/> is encapsulating
        /// </summary>
        public IEnumerable<string> Data;

        /// <summary>
        /// Creates a new DocumentCollectionFieldModel using the passed in list of <see cref="DocumentModel"/>
        /// </summary>
        /// <param name="documents"></param>
        public DocumentCollectionFieldModel(IEnumerable<DocumentModel> documents)
        {
            Debug.Assert(documents != null);
            Data = documents.Select(document => document.Id);
        }

        /// <summary>
        /// Creates a new DocumentCollectionFieldModel using the passed in list of <see cref="DocumentModel.Id"/>
        /// </summary>
        /// <param name="documents"></param>
        public DocumentCollectionFieldModel(IEnumerable<string> documents)
        {
            Debug.Assert(documents != null);
            Data = documents;
        }
    }
}