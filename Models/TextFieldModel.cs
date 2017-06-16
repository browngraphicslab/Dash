﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Dash
{
    /// <summary>
    /// Field model for holding text data
    /// </summary>
    class TextFieldModel : FieldModel
    {
        public TextFieldModel() { }

        public TextFieldModel(string data)
        {
            Data = data;
        }

        private string _data;
        public string Data
        {
            get { return _data; }
            set { SetProperty(ref _data, value); }
        }

        /// <summary>
        /// Creates TextBlock using layout information from template and Data 
        /// </summary>
        public override UIElement MakeView(TemplateModel template)
        {
            var textTemplate = template as TextTemplateModel;
            Debug.Assert(textTemplate != null);
            var binding = new Binding
            {
                Source = this,
                Path = new PropertyPath("Data")
            };
            var tb = textTemplate.Editable ? (FrameworkElement)new TextBox() : new TextBlock();
            if (tb is TextBox)
            {
                tb.SetBinding(TextBox.TextProperty, binding);
                (tb as TextBox).TextChanged += ((s,e) => Data = (s as TextBox).Text);
                (tb as TextBox).FontWeight   = textTemplate.FontWeight;
                (tb as TextBox).TextWrapping = textTemplate.TextWrapping;
            } else
            {
                tb.SetBinding(TextBlock.TextProperty, binding);
                (tb as TextBlock).FontWeight   = textTemplate.FontWeight;
                (tb as TextBlock).TextWrapping = textTemplate.TextWrapping;
            }
            Canvas.SetTop(tb, textTemplate.Top);
            Canvas.SetLeft(tb, textTemplate.Left);
            tb.Visibility = textTemplate.Visibility;
            return tb;
        }
    }
}
