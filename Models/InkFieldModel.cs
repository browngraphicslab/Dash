using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using DashShared;
using DashShared.Models;

namespace Dash
{
    /// <summary>
    /// A Field Model which holds ink data
    /// </summary>
    public class InkFieldModel : FieldModel
    {
        /// <summary>
        /// serialized ink data
        /// </summary>
        public string Data;

        /// <summary>
        /// Create a new Image Field Model which represents the ink pointed to by the <paramref name="data"/>
        /// </summary>
        /// <param name="data">The serialized data that the ink this field model encapsulates is drawn from</param>
        public InkFieldModel(string data = null, string id = null) : base(id)
        {
            Data = data ?? "";
        }
    }
}
