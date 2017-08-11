using System;
using DashShared;

namespace Dash
{
    /// <summary>
    ///     A field model used to represent a Document
    /// </summary>
    public class DocumentFieldModel : FieldModel
    {
        /// <summary>
        ///     The DocumentModel which this field is encapsulating
        /// </summary>
        public DocumentModel Data;

        /// <summary>
        ///     Creates a new field model which represents a document
        /// </summary>
        /// <param name="data"></param>
        public DocumentFieldModel(DocumentModel data, string id = null) : base(id)
        {
            Data = data;
        }

        protected override FieldModelDTO GetFieldDTOHelper()
        {
            return new FieldModelDTO(TypeInfo.Document, Data, Id);
        }
    }
}