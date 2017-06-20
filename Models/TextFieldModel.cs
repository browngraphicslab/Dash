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
    /// Field model for holding text data
    /// </summary>
    class TextFieldModel : FieldModel
    {
        public TextFieldModel() { }

        /// <summary>
        /// Create a new text field model with the passed in string as data
        /// </summary>
        /// <param name="data"></param>
        public TextFieldModel(string data)
        {
            Data = data;
        }

        private string _data;

        /// <summary>
        /// The text which is the field model contains
        /// </summary>
        public string Data
        {
            get { return _data; }
            set { SetProperty(ref _data, value); }
        }

        protected override void UpdateValue(ReferenceFieldModel fieldReference)
        {
            var documentEndpoint = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            TextFieldModel fm = documentEndpoint.GetFieldInDocument(fieldReference) as TextFieldModel;
            if (fm != null)
            {
                Data = fm.Data;
            }
        }
    }
}
