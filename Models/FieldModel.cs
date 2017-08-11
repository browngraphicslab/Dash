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
        public FieldModel(string id = null) : base(id)
        {
            // Initialize Local Variables

            // Add Any Events
        }


        /// <summary>
        ///     Optional reference to a separate <see cref="FieldModel" /> that this <see cref="FieldModel" /> takes as input
        /// </summary>
        public ReferenceFieldModelController InputReference;

        /// <summary>
        /// Implemented by inheritors of this class. Builds the server-representation data transfer object
        /// representing this field. This includes two fields: the TypeInfo and Data field.
        /// </summary>
        /// <returns></returns>
        protected abstract FieldModelDTO GetFieldDTOHelper();

        /// <summary>
        /// Returns the final DTO for server use. Sets ID equal to field ID.
        /// </summary>
        /// <returns>the data transfer object</returns>
        public FieldModelDTO GetFieldDTO() {
            var fieldModelDto = GetFieldDTOHelper();
            return fieldModelDto;
        }
    }
}