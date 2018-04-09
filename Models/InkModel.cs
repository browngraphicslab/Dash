using DashShared;

namespace Dash
{
    /// <summary>
    /// A Field Model which holds ink data
    /// </summary>
    [FieldModelTypeAttribute(TypeInfo.Ink)]
    public class InkModel : FieldModel
    {
        /// <summary>
        /// serialized ink data
        /// </summary>
        public string Data;

        /// <summary>
        /// Create a new Image Field Model which represents the ink pointed to by the <paramref name="data"/>
        /// </summary>
        /// <param name="data">The serialized data that the ink this field model encapsulates is drawn from</param>
        public InkModel(string data = null, string id = null) : base(id)
        {
            Data = data ?? "";
        }
    }
}
