using DashShared;
using static Dash.DocumentController;

namespace Dash
{
    public class FieldUpdatedEventArgs
    {
        public readonly TypeInfo Type;
        public readonly FieldUpdatedAction Action;

        /// <summary>
        /// Create new <see cref="FieldUpdatedEventArgs"/>
        /// </summary>
        /// <param name="type">The type of the field, can be None</param>
        /// <param name="action">The action which occured on the field</param>
        public FieldUpdatedEventArgs(TypeInfo type, FieldUpdatedAction action)
        {
            Type = type;
            Action = action;
        }
    }
}
