using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash
{
    /// <summary>
    /// Base data class for documents; holds data and displays it as UIElement 
    /// </summary>
    public abstract class FieldModel
    {
        public string Key { get; set; }

        /// <summary>
        /// Abstract method to return views using layout information from templates 
        /// </summary>
        public abstract UIElement MakeView(TemplateModel template);
    }
}
