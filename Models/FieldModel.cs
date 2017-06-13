﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using DashShared;

namespace Dash
{
    /// <summary>
    /// Base data class for documents; holds data and displays it as UIElement 
    /// </summary>
    public abstract class FieldModel
    {
        public Key Key { get; set; }

        public ReferenceFieldModel InputReference { get; set; } 
        protected List<ReferenceFieldModel> OutputReferences { get; set; } = new List<ReferenceFieldModel>();

        public void AddOutputReference(ReferenceFieldModel reference)
        {
            OutputReferences.Add(reference);
        }

        /// <summary>
        /// Abstract method to return views using layout information from templates 
        /// </summary>
        public abstract UIElement MakeView(TemplateModel template);
    }
}
