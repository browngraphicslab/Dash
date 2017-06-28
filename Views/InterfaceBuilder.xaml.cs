using System;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class InterfaceBuilder : WindowTemplate
    {

        private List<GuideLineViewModel> _guides = new List<GuideLineViewModel>();

        /// <summary>
        /// The document view model of the document which is being edited
        /// </summary>
        private DocumentViewModel _documentViewModel;

        /// <summary>
        /// the document controller of the document which is being edited
        /// </summary>
        private DocumentController _documentController;

        /// <summary>
        /// The document view of the document which is being edited
        /// </summary>
        private DocumentView _documentView;

        /// <summary>
        /// dictionary of keys to template models for the template models on the document which is being edited
        /// </summary>
        private Dictionary<Key, TemplateModel> _keyToTemplateModel = new Dictionary<Key, TemplateModel>();

        /// <summary>
        /// dictionary of keys to field models for the field models on the document which is being edited
        /// </summary>
        private Dictionary<Key, FieldModelController> _keyToFieldModel = new Dictionary<Key, FieldModelController>();

        public InterfaceBuilder(DocumentViewModel viewModel,int width=800, int height=500)
        {
            this.InitializeComponent();
            Width = width;
            Height = height;

            // set the view model, document model and view variables
            _documentViewModel = new DocumentViewModel(viewModel.DocumentController);
            _documentViewModel.IsDetailedUserInterfaceVisible = false;
            _documentViewModel.IsMoveable = false;
            _documentController = viewModel.DocumentController;
            _documentView = new DocumentView(_documentViewModel);
            xDocumentHolder.Children.Add(_documentView);

            //ApplyEditable();
        }

        private void ApplyEditable()
        {
            List<FrameworkElement> editableElements = new List<FrameworkElement>();

            foreach (var kvp in _keyToFieldModel)
            {
                var key = kvp.Key;

                // if there is no template model for the key don't try to display it
                if (!_keyToTemplateModel.ContainsKey(key))
                    continue;

                var fieldModel = kvp.Value;

                var fieldView = _keyToTemplateModel[key].MakeViewUI(fieldModel, null).First(); // bcz: Fix -- need to apply to all returned elements

                var editableBorder = new EditableFieldFrame(key)
                {
                    EditableContent = fieldView,
                    //Width = fieldView.Width,
                    //Height = fieldView.Height
                };

                Canvas.SetLeft(editableBorder, _keyToTemplateModel[key].Pos.X);
                Canvas.SetTop(editableBorder, _keyToTemplateModel[key].Pos.Y);
                //var guideModel = new GuideLineModel();
                //var guideViewModel = new GuideLineViewModel(guideModel);
                //var guideView = new GuideLineView(guideViewModel);
                //// maybe add guideView to documentView Canvas
                //_guides.Add(guideViewModel);

                editableBorder.FieldSizeChanged += EditableBorderOnFieldSizeChanged;
                editableBorder.FieldPositionChanged += EditableBorderOnFieldPositionChanged;
                editableElements.Add(editableBorder);
            }

            _documentView.SetUIElements(editableElements);
        }


        private void EditableBorderOnFieldPositionChanged(object sender, double deltaX, double deltaY)
        {
            var editableFieldFrame = sender as EditableFieldFrame;
            Debug.Assert(editableFieldFrame != null);

            var key = editableFieldFrame.Key;

            var templateModel = _keyToTemplateModel[key];
            templateModel.Pos = new Point(templateModel.Pos.X + deltaX,
                templateModel.Pos.Y + deltaY);
        }

        private void EditableBorderOnFieldSizeChanged(object sender, double newWidth, double newHeight)
        {
            var editableFieldFrame = sender as EditableFieldFrame;
            Debug.Assert(editableFieldFrame != null);

            var key = editableFieldFrame.Key;

            var templateModel = _keyToTemplateModel[key];
            templateModel.Width = newWidth;
            templateModel.Height = newHeight;
        }


        /// <summary>
        /// Needed to make sure that the bounds on the windows size (min and max) don't exceed the size of the free form canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XDocumentsPane_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            FreeformView freeform = sender as FreeformView;
            Debug.Assert(freeform != null);
            this.MaxHeight = HeaderHeight + freeform.CanvasHeight - 5;
            this.MaxWidth = xSettingsPane.ActualWidth + freeform.CanvasWidth;
            
            this.MinWidth = xSettingsPane.ActualWidth + 50;
            this.MinHeight = HeaderHeight * 2;
        }

        private void ApplyEditableOnTapped(object sender, TappedRoutedEventArgs e)
        {
            ApplyEditable();
        }
    }
}
