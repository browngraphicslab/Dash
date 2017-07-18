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
        public FieldModel()
        {
            // Initialize Local Variables

            // Add Any Events
        }

    }
}