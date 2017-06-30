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
using static Dash.CourtesyDocuments;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class InterfaceBuilder : WindowTemplate
    {

        private List<GuideLineViewModel> _guides = new List<GuideLineViewModel>();
        
        /// <summary>
        /// The document view of the document which is being edited
        /// </summary>
        private DocumentView _documentView;

        /// <summary>
        /// The document controller of the layout document for the document which is being edited. The fields in this
        /// document are courtesy documents used to display data from fields in the _documentController
        /// </summary>
        private DocumentCollectionFieldModelController _layoutDocumentCollection { get { return LayoutCourtesyDocument.LayoutDocumentCollectionController;  } }

        /// <summary>
        /// Courtesy document that manages getting the necessary layout fields to edit the document's layout
        /// </summary>
        private LayoutCourtesyDocument LayoutCourtesyDocument;

        public InterfaceBuilder(DocumentViewModel viewModel,int width=800, int height=500)
        {
            this.InitializeComponent();
            Width = width;
            Height = height;
            
            LayoutCourtesyDocument = new LayoutCourtesyDocument(viewModel.DocumentController);
           
            _documentView = LayoutCourtesyDocument.MakeView(LayoutCourtesyDocument.Document).First() as DocumentView;

            xDocumentHolder.Children.Add(_documentView);

            ApplyEditable();
        }

        private void ApplyEditable()
        {
            List<FrameworkElement> editableElements = new List<FrameworkElement>();

            // iterate over all the documents which define views
            foreach (var layoutDocument in _layoutDocumentCollection.GetDocuments())
            {
                // use the layout document to generate a UI
                var fieldView = layoutDocument.MakeViewUI();

                var editableBorder = new EditableFieldFrame(layoutDocument.GetId())
                {
                    EditableContent = fieldView.FirstOrDefault(),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };

                // apply the transform of the view to the editable frame
                //TODO this throws a null reference exception
                //var position =
                //    (layoutDocument.GetField(DashConstants.KeyStore.PositionFieldKey) as PointFieldModelController)?
                //    .Data;
                //if (position != null)
                //{
                //    editableBorder.ApplyContentTranslationToFrame(position.Value);
                //}

                editableBorder.Tapped += EditableBorder_Tapped;
                editableBorder.FieldSizeChanged += EditableBorderOnFieldSizeChanged;
                editableBorder.FieldPositionChanged += EditableBorderOnFieldPositionChanged;
                editableElements.Add(editableBorder);
            }

            _documentView.SetUIElements(editableElements);
        }

        private void EditableBorder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            xSettingsPane.Children.Clear();

            var editableFieldFrame = sender as EditableFieldFrame;
            Debug.Assert(editableFieldFrame != null);

            var layoutDocumentId = editableFieldFrame.DocumentId;

            var editedLayoutDocument = _layoutDocumentCollection.GetDocuments().FirstOrDefault(doc => doc.GetId() == layoutDocumentId);
            Debug.Assert(editedLayoutDocument != null);

            xSettingsPane.Children.Add(SettingsPaneFromDocumentControllerFactory.CreateSettingsPane(editedLayoutDocument));
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

            editableFieldFrame.ApplyContentTranslationToFrame(newTranslation);
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
    }

    public static class SettingsPaneFromDocumentControllerFactory
    {
        public static UIElement CreateSettingsPane(DocumentController editedLayoutDocument)
        {
            if (editedLayoutDocument.DocumentType == ImageBox.DocumentType)
            {
                return CreateImageSettingsLayout(editedLayoutDocument);
            }
            if (editedLayoutDocument.DocumentType == TextingBox.DocumentType)
            {
                return CreateTextSettingsLayout(editedLayoutDocument);
            }

            Debug.Assert(false, $"We do not create a settings pane for the document with type {editedLayoutDocument.DocumentType}");
            return null;
        }

        private static UIElement CreateImageSettingsLayout(DocumentController editedLayoutDocument)
        {
            return new ImageSettings(editedLayoutDocument);
        }

        private static UIElement CreateTextSettingsLayout(DocumentController editedLayoutDocument)
        {
            return new TextSettings(editedLayoutDocument);
        }
    }
}
