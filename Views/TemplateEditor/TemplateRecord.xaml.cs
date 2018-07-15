﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TemplateRecord : UserControl
    {
        public string Title => xTemplateTitle.Text;

        public DocumentController Template { get; private set; }
        private TemplateApplier _applier;
        private DocumentController _template;

        /// <summary>
        ///     creates a record view of a template, useful when viewing template
        ///     boxes in lists or cells. templaterecord should never be created in
        ///     xaml files. this constructor should be the only use case for this.
        ///     passing in null for the first parameter will yield a record that
        ///     displays "No results found".
        /// </summary>
        /// <param name="templateViewModel"></param>
        /// <param name="applier"></param>
        public TemplateRecord(DocumentController template, TemplateApplier applier)
        {
            this.InitializeComponent();

            _applier = applier;
            _template = template;

            // if null is passed into the first parameter
            if (template != null)
            {
                // binds the template title to the title of the template's layout doc
                var binding = new FieldBinding<TextController>()
                {
                    Mode = BindingMode.TwoWay,
                    Document = template,
                    Key = KeyStore.TitleKey
                };
                xTemplateTitle.AddFieldBinding(TextBlock.TextProperty, binding);
                Template = template;
            }
            else
            {
                // display default text, used for displaying search results
                xTemplateTitle.Text = "No results found";
            }
        }

        private void xDelete_OnClick(object sender, RoutedEventArgs e)
        {
            _applier.TemplateRecords.Remove(this);
            _applier.Templates.Remove(_applier.Templates.First(tvm => tvm.DocumentController.Equals(Template)));
        }

        private void XApply_OnClick(object sender, RoutedEventArgs e)
        {
            _applier.Apply_Template(this);
        }

        public void showButtons()
        {
            xApply.Visibility = Visibility.Visible;
            xDelete.Visibility = Visibility.Visible;
        }


        public void hideButtons()
        {
            xApply.Visibility = Visibility.Collapsed;
            xDelete.Visibility = Visibility.Collapsed;
        }
    }
}
