using System;
using System.Diagnostics;
using DashShared;

namespace Dash
{
    /// <summary>
    /// A Field Model which holds text data
    /// </summary>
    [FieldModelTypeAttribute(TypeInfo.Text)]
    public class TextModel : FieldModel
    {

        /// <summary>
        /// Create a new text field model with the passed in string as data
        /// </summary>
        /// <param name="data">The data whcih the field model will encapsulate</param>
        public TextModel(string data, string id = null) : base(id)
        {
            Data = data;
        }

        /// <summary>
        /// The text which is the field model contains
        /// </summary>
        public string Data;

        public override string ToString()
        {
            return $"TextModel: {Data}";
        }
    }
}
