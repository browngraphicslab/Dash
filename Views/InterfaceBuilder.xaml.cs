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
        /// the document model of the document which is being edited
        /// </summary>
        private DocumentModel _documentModel;

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
        private Dictionary<Key, FieldModel> _keyToFieldModel = new Dictionary<Key, FieldModel>();

        public InterfaceBuilder(DocumentViewModel viewModel,int width=500, int height=500)
        {
            this.InitializeComponent();

            // set width and height to avoid Width = NaN ...
            Width = 500;
            Height = 500;

            // set the view model, document model and view variables
            _documentViewModel = viewModel;
            _documentModel = viewModel.DocumentModel;
            _documentView = new DocumentView();

            // add the document view to the canvas in the center
            _documentView.DataContext = _documentViewModel;
            Canvas.SetLeft(_documentView, xDocumentsPane.CanvasWidth / 2 - _documentView.Width);
            Canvas.SetTop(_documentView, xDocumentsPane.CanvasHeight / 2 - _documentView.Height);
            xDocumentsPane.Canvas.Children.Add(_documentView);

            InitializeKeyDicts();
        }

        private void InitializeKeyDicts()
        {
            _keyToTemplateModel = _documentViewModel.GetLayoutModel().Fields;
            _keyToFieldModel = _documentModel.EnumFields().ToDictionary(x => x.Key, x => x.Value);
        }

        private void ApplyEditable()
        {
            List<UIElement> editableElements = new List<UIElement>();

            foreach (var kvp in _keyToFieldModel)
            {
                var key = kvp.Key;

                if (!_keyToTemplateModel.ContainsKey(key))
                    continue;

                var fieldModel = kvp.Value;

                var uiElement = fieldModel.MakeView(_keyToTemplateModel[key]);
                var left = Canvas.GetLeft(uiElement);
                var top = Canvas.GetTop(uiElement);

                var editableBorder = new EditableFieldFrame(key) { EditableContent = uiElement, BorderBrush = new SolidColorBrush(Colors.CornflowerBlue), BorderThickness = new Thickness(1) };
                var guideModel = new GuideLineModel();
                var guideViewModel = new GuideLineViewModel(guideModel);
                var guideView = new GuideLineView(guideViewModel);
                // maybe add guideView to documentView Canvas
                _guides.Add(guideViewModel);

                editableBorder.SizeChanged += EditableBorder_SizeChanged;
                editableBorder.PositionChanged += EditableBorderPositionChanged;
                editableElements.Add(editableBorder);
                Canvas.SetLeft(editableBorder, left);
                Canvas.SetTop(editableBorder, top);
            }

            _documentView.SetUIElements(editableElements);
        }


        private void EditableBorderPositionChanged(object sender, double deltaX, double deltaY)
        {
            var editableFieldFrame = sender as EditableFieldFrame;
            Debug.Assert(editableFieldFrame != null);

            var key = editableFieldFrame.Key;

            var templateModel = _keyToTemplateModel[key];
            templateModel.Left += deltaX;
            templateModel.Top += deltaY;
        }

        private void EditableBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var editableFieldFrame = sender as EditableFieldFrame;
            Debug.Assert(editableFieldFrame != null);

            var key = editableFieldFrame.Key;

            var templateModel = _keyToTemplateModel[key];
            templateModel.Width = e.NewSize.Width;
            templateModel.Height = e.NewSize.Height;
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
