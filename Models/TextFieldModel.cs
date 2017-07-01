using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Microsoft.Extensions.DependencyInjection;
using Dash.Models;

namespace Dash
{
    /// <summary>
    /// A Field Model which holds text data
    /// </summary>
    public class TextFieldModel : FieldModel
    {

        /// <summary>
        /// Create a new text field model with the passed in string as data
        /// </summary>
        /// <param name="data">The data whcih the field model will encapsulate</param>
        public TextFieldModel(string data)
        {
            Data = data;
        }

        /// <summary>
        /// The text which is the field model contains
        /// </summary>
        public string Data;
    }
}
