using System;
using DashShared;
using DashShared.Models;

namespace Dash
{
    /// <summary>
    ///     A field model used to represent a Document
    /// </summary>
    [FieldModelTypeAttribute(TypeInfo.Document)]
    public class DocumentFieldModel : FieldModel
    {
        /// <summary>
        ///     The id of the DocumentModel which this field is encapsulating
        /// </summary>
        public string Data;

        /// <summary>
        ///     Creates a new field model which represents a document
        /// </summary>
        /// <param name="data"></param>
        public DocumentFieldModel(string data, string id = null) : base(id)
        {
            Data = data;
        }
    }
}