using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Dash.Models.DragModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Syncfusion.Pdf.Parsing;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionTreeView : UserControl
    {

        public CollectionViewModel ViewModel => DataContext as CollectionViewModel;

        public CollectionTreeView()
        {
            this.InitializeComponent();
            this.AllowDrop = true;
            this.Drop += CollectionTreeView_Drop;
            this.DragOver += CollectionTreeView_DragOver;
        }

        private void CollectionTreeView_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)) || e.DataView.Properties.ContainsKey(nameof(List<DragDocumentModel>)))
            {
                e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None ? DataPackageOperation.Copy : e.DataView.RequestedOperation;
            }
            else
                e.AcceptedOperation = DataPackageOperation.None;
            e.Handled = true;
        }

        private void CollectionTreeView_Drop(object sender, DragEventArgs e)
        {
            Debug.Assert(ViewModel != null, "ViewModel != null");
            var dvm = e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)) ? e.DataView.Properties[nameof(DragDocumentModel)] as DragDocumentModel : null;
            if (dvm != null)
                ViewModel.ContainerDocument.GetField<ListController<DocumentController>>(KeyStore.DataKey)?.Add(dvm.DraggedDocument);
            e.Handled = true;
        }

        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            Debug.Assert(ViewModel != null, "ViewModel != null");
            var documentController = new NoteDocuments.CollectionNote(new Point(0, 0), CollectionView.CollectionViewType.Freeform, double.NaN, double.NaN).Document;//, "New Workspace " + cvm.CollectionController.Count);
            ViewModel.ContainerDocument.GetField<ListController<DocumentController>>(KeyStore.DataKey)?.Add(documentController);

        }


        public void Highlight(DocumentController document, bool? flag)
        {
            xTreeRoot.Highlight(document, flag);
        }

        private async void MakePdf_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            //Load the PDF document as stream
            Stream pdfStream = typeof(MainPage).GetTypeInfo().Assembly.GetManifestResourceStream("Sample.Assets.Data.Sample.pdf");

            //Creates an empty PDF loaded document instance
            PdfLoadedDocument document = new PdfLoadedDocument();

            //Loads or opens an existing PDF document through Open method of PdfLoadedDocument class
            await document.OpenAsync(pdfStream);

            MemoryStream stream = new MemoryStream();

            await document.SaveAsync(stream);

            //Close the documents

            document.Close(true);

            //Save the stream as PDF document file in local machine

            Save(stream, "Result.pdf");

           

                //Load the PDF document.

                PdfLoadedDocument loadedDocument = new PdfLoadedDocument("saved.pdf");

            //Get the loaded form.

            PdfLoadedForm loadedForm = loadedDocument.Form;

            //Get the loaded text box field and fill it.

            PdfLoadedTextBoxField loadedTextBoxField = loadedForm.Fields[0] as PdfLoadedTextBoxField;

            loadedTextBoxField.Text = "First Name";

            //Save the modified document.

            Save(stream, "sample.pdf");

            //Close the document

            loadedDocument.Close(true);
        }

        async void Save(Stream stream, string filename)
        {

            stream.Position = 0;

            StorageFile stFile;
            if (!(Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons")))
            {
                FileSavePicker savePicker = new FileSavePicker();
                savePicker.DefaultFileExtension = ".pdf";
                savePicker.SuggestedFileName = "Sample";
                savePicker.FileTypeChoices.Add("Adobe PDF Document", new List<string>() {".pdf"});
                stFile = await savePicker.PickSaveFileAsync();
            }
            else
            {
                StorageFolder local = Windows.Storage.ApplicationData.Current.LocalFolder;
                stFile = await local.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            }

            if (stFile != null)
            {
                Windows.Storage.Streams.IRandomAccessStream fileStream =
                    await stFile.OpenAsync(FileAccessMode.ReadWrite);
                Stream st = fileStream.AsStreamForWrite();
                st.Write((stream as MemoryStream).ToArray(), 0, (int) stream.Length);
                st.Flush();
                st.Dispose();
                fileStream.Dispose();
            }
        }


    }
}
