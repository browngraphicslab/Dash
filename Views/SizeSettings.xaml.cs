﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using DashShared;
using Visibility = Windows.UI.Xaml.Visibility;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class SizeSettings : UserControl
    {
        private TextBox _heightTextBox;
        private TextBox _widthTextBox;
        public SizeSettings()
        {
            this.InitializeComponent();
        }

        public SizeSettings(DocumentController editedLayoutDocument, IEnumerable<DocumentController> docContextList) : this()
        {
            BindWidth(editedLayoutDocument, docContextList);
            BindHeight(editedLayoutDocument, docContextList);
        }

        private void BindHeight(DocumentController docController, IEnumerable<DocumentController> docContextList)
        {
            var fmc = docController.GetField(DashConstants.KeyStore.HeightFieldKey);
            var heightController = DocumentController.GetDereferencedField(fmc, fmc.Context) as NumberFieldModelController;
            Debug.Assert(heightController != null);

            var heightBinding = new Binding
            {
                Source = heightController,
                Path = new PropertyPath(nameof(heightController.Data)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            xHeightTextBox.SetBinding(TextBox.TextProperty, heightBinding);

        }

        private void BindWidth(DocumentController docController, IEnumerable<DocumentController> docContextList)
        {
            var fmc = docController.GetField(DashConstants.KeyStore.WidthFieldKey);
            NumberFieldModelController widthController = DocumentController.GetDereferencedField(fmc, fmc.Context) as NumberFieldModelController;
            Debug.Assert(widthController != null);

            var widthBinding = new Binding
            {
                Source = widthController,
                Path = new PropertyPath(nameof(widthController.Data)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            xWidthTextBox.SetBinding(TextBox.TextProperty, widthBinding);
        }
    }
}
