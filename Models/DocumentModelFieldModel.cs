using System;
using DashShared;

namespace Dash
{
    /// <summary>
    ///     A field model used to represent a Document
    /// </summary>
    public class DocumentModelFieldModel : FieldModel
    {
        /// <summary>
        ///     The DocumentModel which this field is encapsulating
        /// </summary>
        public DocumentModel Data;

        /// <summary>
        ///     Creates a new field model which represents a document
        /// </summary>
        /// <param name="data"></param>
        public DocumentModelFieldModel(DocumentModel data)
        {
            Data = data;
        }

        public override FieldModelDTO GetFieldDTO()
        {
            return new FieldModelDTO(TypeInfo.Document, Data);
        }
    }
}