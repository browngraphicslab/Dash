using System;
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
using Dash.ViewModels;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236
namespace Dash {

    public sealed partial class DocumentView : UserControl {
        /// <summary>
        /// Contains methods which allow the document to be moved around a free form canvas
        /// </summary>
        private ManipulationControls manipulator;

        private DocumentViewModel _vm;

        public DocumentView() {
            this.InitializeComponent();
            DataContextChanged += DocumentView_DataContextChanged;

            this.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            // add manipulation code
            manipulator = new ManipulationControls(this);

            // set bounds
            MinWidth = 200;
            MinHeight = 400;
        }

        public DocumentView(DocumentViewModel documentViewModel):this()
        {
            DataContext = documentViewModel;

            // reset the fields on the documetn to be those displayed by the documentViewModel
            ResetFields(documentViewModel);
        }

        /// <summary>
        /// Resets the fields on the document to exactly resemble the fields the DocumentViewModel wants to display
        /// </summary>
        /// <param name="documentViewModel"></param>
        private void ResetFields(DocumentViewModel documentViewModel) {
            // clear any current children (fields) and then add them over again
            xCanvas.Children.Clear();
            var elements = documentViewModel.GetUiElements();
            foreach (var element in elements) {
                xCanvas.Children.Add(element);
            }
        }

        /// <summary>
        /// Hacky way of adding the editable fields to the document in the interface builder
        /// </summary>
        /// <param name="uiElements"></param>
        public void SetUIElements(List<UIElement> uiElements) {
            xCanvas.Children.Clear();
            foreach (var element in uiElements) {
                xCanvas.Children.Add(element);
            }
        }


        /// <summary>
        /// Brings up OperationWindow when DocumentView is double tapped 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (_vm != null && _vm.DoubleTapEnabled)
            {
                e.Handled = true;
                var window = new OperationWindow(1000, 800, new OperationWindowViewModel(_vm.DocumentModel));

                var center = RenderTransform.TransformPoint(e.GetPosition(this));

                FreeformView.MainFreeformView.ViewModel.AddElement(window, (float)(center.X - window.Width / 2), (float)(center.Y - window.Height / 2));
            }
        }

        /// <summary>
        /// Called whenever a field is changed on the document
        /// </summary>
        /// <param name="fieldReference"></param>
        private void DocumentModel_DocumentFieldUpdated(ReferenceFieldModel fieldReference)
        {
            //ResetFields(_vm);
            Debug.WriteLine("DocumentView.DocumentModel_DocumentFieldUpdated COMMENTED OUT LINE");
        }

        /// <summary>
        /// Right tapping to bring up the interface builder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e) {
            var dvm = DataContext as DocumentViewModel;
            Debug.Assert(dvm != null);

            var interfaceBuilder = new InterfaceBuilder(dvm);
            var center = RenderTransform.TransformPoint(e.GetPosition(this));
            FreeformView.MainFreeformView.ViewModel.AddElement(interfaceBuilder, (float)(center.X - interfaceBuilder.Width / 2), (float)(center.Y - interfaceBuilder.Height / 2));
        }

        /// <summary>
        /// The first time the local DocumentViewModel _vm can be set to the new datacontext
        /// this resets the fields otherwise does nothing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void DocumentView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) {
            _vm = DataContext as DocumentViewModel;
            if (_vm != null) {
                ResetFields(_vm);
                // Add any methods
                //_vm.DocumentModel.DocumentFieldUpdated -= DocumentModel_DocumentFieldUpdated;
                //_vm.DocumentModel.DocumentFieldUpdated += DocumentModel_DocumentFieldUpdated;
            }
        }
    }
}