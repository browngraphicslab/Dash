﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    //[ContentProperty("InnerContent")]
    public sealed partial class DocumentView : UserControl
    {
        Dictionary<string, TextBlock> textElementViews = new Dictionary<string, TextBlock>();
        private ManipulationControls manipulator;

        public DocumentView()
        {
            this.InitializeComponent();
            this.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            this.DataContextChanged += DocumentView_DataContextChanged;
            manipulator = new ManipulationControls(XGrid, this); // allow documents to be moved & zoomed.
            this.MinWidth = 200;
            this.MinHeight = 400;
        }

     
        /// <summary>
        /// Updates the document's visual display to reflect changes in its code-end
        /// data. Called when the DataContext changes. Replaces all existing elements
        /// with the new ones.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void DocumentView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var dvm = DataContext as DocumentViewModel;

            if (dvm != null) {
                dvm.DocumentModel.DocumentFieldUpdated += DocumentModel_DocumentFieldUpdated;

                xCanvas.Children.Clear();
                List<UIElement> elements = dvm.CreateUIElements();
                foreach (var element in elements)
                {
                    xCanvas.Children.Add(element);
                }
            }
        }

        /// <summary>
        /// Returns a list of all of the UIElements in a given document.
        /// </summary>
        /// <returns>a list of the document's UIElements</returns>
        public List<UIElement> GetUIElements()
        {
            var dvm = DataContext as DocumentViewModel;
            dvm.DocumentModel.DocumentFieldUpdated += DocumentModel_DocumentFieldUpdated;
            if (dvm != null)
            {
                List<UIElement> elements = dvm.CreateUIElements();
                return elements;
            }

            return null;
        }

        /// <summary>
        /// Populates a document to include a given list of UIElements by
        /// adding the child elements to the document's canvas. UNRELATED to
        /// tye. Used in interface buildier.
        /// </summary>
        /// <param name="uiElements">UI elements to add.</param>
        public void SetUIElements(List<UIElement> uiElements)
        {
            xCanvas.Children.Clear();
            foreach (var element in uiElements)
            {
                xCanvas.Children.Add(element);
            }
        }
        
        /// <summary>
        /// Brings up OperationWindow when DocumentView is double tapped 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
            var documentViewModel = DataContext as DocumentViewModel;
            if (documentViewModel != null && documentViewModel.DoubleTapEnabled) {
                e.Handled = true;
                OperationWindow window = new OperationWindow(1000, 800);

                var dvm = DataContext as DocumentViewModel;
                if (dvm != null) {
                    window.DocumentViewModel = dvm;
                }
                Point center = RenderTransform.TransformPoint(e.GetPosition(this));

                FreeformView.MainFreeformView.ViewModel.AddElement(window, (float)(center.X - window.Width / 2), (float)(center.Y - window.Height / 2));

            }
        }

        private void DocumentModel_DocumentFieldUpdated(ReferenceFieldModel fieldReference)
        {
            DocumentView_DataContextChanged(null, null);
        }

        private void elementModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ReLayout();
        }

        public void ReLayout()
        {
            //var dvm = DataContext as DocumentViewModel;
            //if (dvm != null)
            //{
            //    var lm = dvm.DocumentViewModelSource.DocumentLayoutModel(dvm.DocumentModel);
            //    foreach (var item in dvm.DocumentModel.Fields)
            //    {
            //        var elementKey   = item.Key;
            //        var elementModel = lm.Fields[elementKey];
            //        var content      = item.Value;

            //        if (!textElementViews.ContainsKey(elementKey))
            //        {
            //            xCanvas.Children.Add(new TextBlock());
            //            textElementViews.Add(elementKey, xCanvas.Children.Last() as TextBlock);
            //        }
            //        var tb = textElementViews[elementKey];
            //        tb.FontSize = 16;
            //        tb.Width = 200;
            //        tb.TextWrapping = elementModel.TextWrapping;
            //        tb.FontWeight = elementModel.FontWeight;
            //        tb.Text = content == null ? "" : content.ToString();
            //        tb.Name = "x" + elementKey;
            //        tb.HorizontalAlignment = HorizontalAlignment.Center;
            //        tb.VerticalAlignment = VerticalAlignment.Center;
            //        Canvas.SetLeft(tb, elementModel.Left);
            //        Canvas.SetTop(tb,  elementModel.Top);
            //        tb.Visibility = elementModel.Visibility;
            //        elementModel.PropertyChanged -= elementModel_PropertyChanged;
            //        elementModel.PropertyChanged += elementModel_PropertyChanged;
            //    }
            //}
        }

        private void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var dvm = DataContext as DocumentViewModel;
            Debug.Assert(dvm != null);

            var interfaceBuilder = new InterfaceBuilder(dvm);
            var center = RenderTransform.TransformPoint(e.GetPosition(this));
            FreeformView.MainFreeformView.ViewModel.AddElement(interfaceBuilder, (float)(center.X - interfaceBuilder.Width / 2), (float)(center.Y - interfaceBuilder.Height / 2));

        }
        
        protected override void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e)
        {
            base.OnManipulationCompleted(e);
        }
    }
}
