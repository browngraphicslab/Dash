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
        /// the document controller of the document which is being edited. This contains the fields for all the data
        /// that is going to be displayed
        /// </summary>
        private DocumentController _documentController;

        /// <summary>
        /// The document view of the document which is being edited
        /// </summary>
        private DocumentView _documentView;

        /// <summary>
        /// The document controller of the layout document for the document which is being edited. The fields in this
        /// document are courtesy documents used to display data from fields in the _documentController
        /// </summary>
        private DocumentCollectionFieldModelController _layoutDocumentCollection;

        public InterfaceBuilder(DocumentViewModel viewModel,int width=800, int height=500)
        {
            this.InitializeComponent();
            Width = width;
            Height = height;

            // set the view model, document model and view variables
            _documentViewModel = new DocumentViewModel(viewModel.DocumentController) // create a new documentViewModel because  the view in the editor is different from the view in the workspace
            {
                IsDetailedUserInterfaceVisible = false,
                IsMoveable = false
            };
            _documentController = viewModel.DocumentController;

            // get the layout field on the document being displayed
            var layoutField = viewModel.DocumentController.GetField(DashConstants.KeyStore.LayoutKey) as DocumentFieldModelController;
            // get the layout document controller from the layout field
            var layoutDocumentController = layoutField?.Data;
            // get the documentCollectionFieldModelController from the layout document controller
            _layoutDocumentCollection = layoutDocumentController?.GetField(DashConstants.KeyStore.DataKey) as DocumentCollectionFieldModelController;
            if (_layoutDocumentCollection == null)
            {
                throw new NotImplementedException("we can't edit views without a layout yet");
            }


            _documentView = new DocumentView(_documentViewModel);
            xDocumentHolder.Children.Add(_documentView);

            ApplyEditable();
        }

        private void ApplyEditable()
        {
            List<FrameworkElement> editableElements = new List<FrameworkElement>();

            // iterate over all the documents which define views
            foreach (var layoutDocument in _layoutDocumentCollection.GetDocuments())
            {
                // get the controller for the data field that the layout document is parameterizing a view for
                var referenceToData = layoutDocument.GetField(DashConstants.KeyStore.DataKey) as ReferenceFieldModelController;
                Debug.Assert(referenceToData != null, "The layout document customarily defines the data key as a reference to the data field that it is defining a layout for");

                // use the layout document to generate a UI
                var fieldView = layoutDocument.MakeViewUI();

                var editableBorder = new EditableFieldFrame(layoutDocument.GetId())
                {
                    EditableContent = fieldView.FirstOrDefault(),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };

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

            var layoutDocumentId = editableFieldFrame.DocumentId;

            var editedLayoutDocument = _layoutDocumentCollection.GetDocuments().FirstOrDefault(doc => doc.GetId() == layoutDocumentId);
            Debug.Assert(editedLayoutDocument != null);
            var translateController =
                editedLayoutDocument.GetField(DashConstants.KeyStore.PositionFieldKey) as PointFieldModelController;
            Debug.Assert(translateController != null);

            var p = translateController.Data;
            var newTranslation = new Point(p.X + deltaX, p.Y + deltaY);
            translateController.Data = newTranslation;
            
            editableFieldFrame.Margin = new Thickness(newTranslation.X, newTranslation.Y, 0, 0);
        }

        private void EditableBorderOnFieldSizeChanged(object sender, double newWidth, double newHeight)
        {
            var editableFieldFrame = sender as EditableFieldFrame;
            Debug.Assert(editableFieldFrame != null);

            var layoutDocumentId = editableFieldFrame.DocumentId;

            var editedLayoutDocument = _layoutDocumentCollection.GetDocuments().FirstOrDefault(doc => doc.GetId() == layoutDocumentId);
            Debug.Assert(editedLayoutDocument != null);
            var widthFieldController =
                editedLayoutDocument.GetField(DashConstants.KeyStore.WidthFieldKey) as NumberFieldModelController;
            var heightFieldController =
                editedLayoutDocument.GetField(DashConstants.KeyStore.HeightFieldKey) as NumberFieldModelController;
            Debug.Assert(widthFieldController != null && heightFieldController != null);

            heightFieldController.Data = newHeight;
            widthFieldController.Data = newWidth;
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
