using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Dash.Controllers;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TemplateApplier : UserControl
    {
        private ObservableCollection<DocumentViewModel> Templates;
        private ObservableCollection<TemplateRecord> TemplateRecords;
        private DocumentController Document;

        public TemplateApplier(DocumentController doc,
            ObservableCollection<DocumentViewModel> documentViewModels)
        {
            this.InitializeComponent();

            Document = doc;
            Templates = new ObservableCollection<DocumentViewModel>();
            TemplateRecords = new ObservableCollection<TemplateRecord>();

            foreach (var dvm in documentViewModels)
            {
                if (dvm.LayoutDocument.DocumentType.Equals(TemplateBox.DocumentType) && !Templates.Contains(dvm))
                {
                    var tr = new TemplateRecord(dvm);
                    TemplateRecords.Add(tr);
                    tr.Tapped += Template_Picked;
                }
            }
        }

        private void Template_Picked(object sender, TappedRoutedEventArgs args)
        {
            var tr = sender as TemplateRecord;
            var template = tr.TemplateViewModel;
            if (template == null) return;
            var newDataDoc = template.DataDocument.GetDataCopy();
            var newLayoutDoc = template.LayoutDocument.GetViewCopy();
            newDataDoc.SetField(KeyStore.DocumentContextKey, Document, true);

            foreach (var doc in newLayoutDoc.GetField<ListController<DocumentController>>(KeyStore.DataKey)
                .TypedData)
            {
                // if either is true, then the layout doc needs to be abstracted
                if (doc.GetField<PointerReferenceController>(KeyStore.DataKey) != null || doc.GetDataDocument().Equals(Document))
                {
                    var specificKey = doc.GetField<ReferenceController>(KeyStore.DataKey).FieldKey;
                    if (specificKey == null) continue;

                    if (newDataDoc.GetField<DocumentController>(KeyStore.DocumentContextKey)
                            .GetField(specificKey) != null)
                    {
                        // set the layout doc's context to a reference of the data doc's context
                        doc.SetField(KeyStore.DocumentContextKey,
                            new DocumentReferenceController(
                                newDataDoc.GetField<DocumentController>(KeyStore.DocumentContextKey).Id,
                                KeyStore.DocumentContextKey),
                            true);
                    }
                    else
                    {
                        // set the layout doc's context to a reference of the data doc's context
                        doc.SetField(KeyStore.DocumentContextKey,
                            new DocumentReferenceController(newDataDoc.Id,
                                KeyStore.DocumentContextKey),
                            true);
                    }

                    // set the field of the document's data key to a pointer reference to this documents' docContext's specific key
                    doc.SetField(KeyStore.DataKey,
                        new PointerReferenceController(
                            doc.GetField<DocumentReferenceController>(KeyStore.DocumentContextKey), specificKey), true);
                }

                //// create new viewmodel with a copy of document, set editor to this
                //var dvm =
                //    new DocumentViewModel(doc, new Context(doc));
                //// adds layout doc to list of layout docs
                //var datakey = Document.GetField<DocumentController>(KeyStore.TemplateDocumentKey)
                //    .GetField<ListController<DocumentController>>(KeyStore.DataKey);
                //datakey.Add(dvm.LayoutDocument);
                //Document.GetField<DocumentController>(KeyStore.TemplateDocumentKey)
                //    .SetField(KeyStore.DataKey, datakey, true);
            }

            newLayoutDoc.SetField(KeyStore.DocumentContextKey, Document, true);
            newLayoutDoc.SetField(KeyStore.PositionFieldKey,
                Document.GetField<PointController>(KeyStore.PositionFieldKey), true);
            Document.SetField(KeyStore.ActiveLayoutKey, newLayoutDoc, true);
        }

        private void XApply_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void XDelete_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Search_Entered(object sender, TextChangedEventArgs textChangedEventArgs)
        {
            var matchingItems = TemplateRecords.Where(tr =>
                tr.Title.StartsWith((sender as TextBox).Text, StringComparison.OrdinalIgnoreCase)).ToArray();
            xListView.ItemsSource = matchingItems.Any()
                ? matchingItems
                : new Collection<TemplateRecord>() {new TemplateRecord(null)}.ToArray();
        }
    }
}
