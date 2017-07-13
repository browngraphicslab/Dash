﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

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
        /// Create a new Ink Field Model that does not represent an image
        /// </summary>
        public InkFieldModel()
        {
        }

        /// <summary>
        /// Create a new Image Field Model which represents the ink pointed to by the <paramref name="data"/>
        /// </summary>
        /// <param name="data">The serialized data that the ink this field model encapsulates is drawn from</param>
        public InkFieldModel(string data)
        {
            Data = data;
        }
    }
}
