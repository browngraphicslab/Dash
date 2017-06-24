using System.Collections.Generic;
using DashShared;

namespace Dash
{
    /// <summary>
    ///     Base data class for documents. If a document is a spread sheet, a field is a cell, and a fieldModel wraps
    ///     the data which is stored in a cell
    /// </summary>
    public abstract class FieldModel : EntityBase
    {
        /// <summary>
        ///     Optional reference to a separate <see cref="FieldModel" /> that this <see cref="FieldModel" /> takes as input
        /// </summary>
        public ReferenceFieldModel InputReference;

        /// <summary>
        ///     List of references to fields that take this field as input
        /// </summary>
        public List<ReferenceFieldModel> OutputReferences;
    }
}