using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Dash.Models.DragModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Web.Http;
using Gma.CodeCloud.Controls;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.IO;
using Syncfusion.Pdf.Parsing;
using Point = Windows.Foundation.Point;

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


            String filename = "Sample.pdf";
           // Windows.Storage.Streams.IInputStream stream =
             //   await sampleFile.OpenReadAsync();

          //  stream.Position = 0;

            StorageFile stFile;
            if (!(Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons")))
            {
                FileSavePicker savePicker = new FileSavePicker();
                savePicker.DefaultFileExtension = ".pdf";
                savePicker.SuggestedFileName = "Dash";
                savePicker.FileTypeChoices.Add("Adobe PDF Document", new List<string>() { ".pdf" });
                stFile = await savePicker.PickSaveFileAsync();
                //await Windows.Storage.FileIO.WriteTextAsync(stFile, "Swift as a shadow");

                // var stream = await stFile.OpenAsync(FileAccessMode.ReadWrite);

                //  PdfDocument pdf = await PdfDocument.LoadFromStreamAsync(stream);
                StorageFolder folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                StorageFile file = await folder.GetFileAsync("test.pdf");
                PdfDocument pdf = await PdfDocument.LoadFromFileAsync(file);
                //PdfDocument pdf = await PdfDocument.LoadFromFileAsync(stFile);
                // PdfDocument pdf = pd.GetResults();
                PdfPage page = pdf.GetPage(1);

               

            }
            else
            {
                StorageFolder local = Windows.Storage.ApplicationData.Current.LocalFolder;
                stFile = await local.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                await Windows.Storage.FileIO.WriteTextAsync(stFile, "Swift as a shadow");
            }

          

           





            //StreamReader rdr = new StreamReader(sampleFile);

            //  PdfDocument pdf = new PdfDocument();

            //  Document doc = new Document();
            // PdfWriter.GetInstance(doc, new FileStream(txtOutput.Text, FileMode.Create));

            /*

            //Load the PDF document as stream
            Debug.WriteLine("t - " + typeof(MainPage).GetTypeInfo().Assembly);
            Stream pdfStream = typeof(MainPage).GetTypeInfo().Assembly.GetManifestResourceStream("Sample.Assets.Data.Sample.pdf");

            //Creates an empty PDF loaded document instance
            PdfLoadedDocument document = new PdfLoadedDocument();

            //Loads or opens an existing PDF document through Open method of PdfLoadedDocument class
            //await document.OpenAsync(pdfStream);

            MemoryStream stream = new MemoryStream(100);

            await document.SaveAsync(stream);

            //Close the documents

            document.Close(true);

            //Save the stream as PDF document file in local machine

            Save(stream, "Result.pdf");


             //Load the PDF document.

              PdfLoadedDocument loadedDocument = new PdfLoadedDocument
                  (Encoding.ASCII.GetBytes("saved.pdf"));

            //Get the loaded form.

            PdfLoadedForm loadedForm = loadedDocument.Form;

            //Get the loaded text box field and fill it.

            PdfLoadedTextBoxField loadedTextBoxField = loadedForm.Fields[0] as PdfLoadedTextBoxField;

            loadedTextBoxField.Text = "First Name";

            //Save the modified document.

            Save(stream, "sample.pdf");

            //Close the document

            loadedDocument.Close(true); */
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
