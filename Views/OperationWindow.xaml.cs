﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    /// <summary>
    /// Window that allows users to create their own Key,Value pairs 
    /// </summary>
    public sealed partial class OperationWindow : WindowTemplate
    {
        private DocumentViewModel _documentViewModel; 
        
        public DocumentViewModel DocumentViewModel
        {
            get { return _documentViewModel; }
            set
            {
                _documentViewModel = value;
                InitializeGrid(XDocumentGridLeft);
                InitializeGrid(XDocumentGridRight);
            }
        }

        public OperationWindow(int width, int height)
        {
            this.InitializeComponent();
            Width = width;
            Height = height;
        }

        /// <summary>
        ///  Makes the left grid representing Key,Value pairs of document tapped 
        /// </summary>
        public void InitializeGrid(Grid grid)
        {
            grid.Children.Clear();
            DocumentModel doc = DocumentViewModel.DocumentModel;
            LayoutModel layout = DocumentViewModel.DocumentViewModelSource.DocumentLayoutModel(doc);

            //Create rows
            for (int i = 0; i < doc.Fields.Count + 1; ++i)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
            //Create columns 
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            //Make Key, Value headers 
            TextBlock v = new TextBlock
            {
                Text = "Value",
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(v, 1);
            Grid.SetRow(v, 0);
            grid.Children.Add(v);

            TextBlock k = new TextBlock
            {
                Text = "Key",
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(k, 0);
            Grid.SetRow(k, 0);
            grid.Children.Add(k);

            //Fill in Grid 
            int j = 1;
            foreach (KeyValuePair<string, FieldModel> pair in doc.Fields)
            {
                //Add Value as FrameworkElement (field values)  
                TemplateModel template = null;
                if (layout.Fields.ContainsKey(pair.Key))
                    template = layout.Fields[pair.Key];
                else
                    Debug.Assert(false);

                FrameworkElement element = pair.Value.MakeView(template) as FrameworkElement;
                if (element != null)
                {
                    element.VerticalAlignment = VerticalAlignment.Center;
                    element.HorizontalAlignment = HorizontalAlignment.Center;
                }
                //Grid g1 = new Grid();
                element.Margin = new Thickness(12, 5, 12, 5);
                Grid.SetColumn(element, 1);
                Grid.SetRow(element, j);
                //g1.BorderBrush = new SolidColorBrush(Colors.Transparent);
                //g1.BorderThickness = new Thickness(0, 5, 25, 5);
                //g1.Children.Add(element);
                grid.Children.Add(element);

                //Add Key Values (field names) 
                TextBlock tb = new TextBlock
                {
                    Text = pair.Key,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(tb, 0);
                Grid.SetRow(tb, j);
                tb.Padding = new Thickness(12, 5, 12, 5);
                grid.Children.Add(tb);

                j++;
            }
        }

        private void FreeformView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FreeformView freeform = sender as FreeformView;
            Debug.Assert(freeform != null);
            this.MaxHeight = HeaderHeight + freeform.CanvasHeight - 5;
            this.MaxWidth = XDocumentGridLeft.ActualWidth + freeform.CanvasWidth + XDocumentGridRight.ActualWidth;
            this.MinWidth = XDocumentGridLeft.ActualWidth + XDocumentGridRight.ActualWidth + 50;
            this.MinHeight = HeaderHeight * 2;
        }
    }
}
