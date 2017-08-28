using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using DashShared;

namespace Dash
{
    public static class DocumentDropHelper
    {
        public static async void HandleDrop(object sender, DragEventArgs e)
        {
            var docView = sender as DocumentView;
            if (!e.DataView.Contains(StandardDataFormats.StorageItems) || docView == null) return;
            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count > 0)
            {
                var doc = docView.ViewModel.DocumentController;
                var dropPoint = e.GetPosition(docView);
                foreach (var item in items)
                {
                    var storageFile = item as StorageFile;
                    var key = new KeyController(Guid.NewGuid().ToString(), storageFile.DisplayName);
                    var layout = doc.GetActiveLayout();
                    var activeLayout = layout.Data.GetDereferencedField(KeyStore.DataKey, null) as DocumentCollectionFieldModelController;
                    if (storageFile.IsOfType(StorageItemTypes.Folder))
                    {
                        //Add collection of new documents?
                    }
                    else if (storageFile.IsOfType(StorageItemTypes.File))
                    {
                        object data = null;
                        TypeInfo t;
                        switch (storageFile.FileType.ToLower())
                        {
                            case ".jpg":
                            case ".jpeg":
                            case ".png":
                                t = TypeInfo.Image;
                                //Todo: needs to be fixed bc the images wont display if you use the system uri (i.e. storageFile.Path)
                                data = new Uri("https://img.discogs.com/dMKvzkG5Cx_NXkM1tFekGIniTuw=/fit-in/300x300/filters:strip_icc():format(jpeg):mode_rgb():quality(40)/discogs-images/R-1752886-1374045563-3674.jpeg.jpg", UriKind.Absolute);
                                break;
                            case ".txt":
                                t = TypeInfo.Text;
                                data = await FileIO.ReadTextAsync(storageFile);
                                break;
                            case ".json":
                                t = TypeInfo.Document;
                                data = await FileIO.ReadTextAsync(storageFile);
                                break;
                            case ".rtf":
                                t = TypeInfo.RichText;
                                data = await FileIO.ReadTextAsync(storageFile);
                                break;
                            default:
                                try
                                {
                                    data = await FileIO.ReadTextAsync(storageFile);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine("could not import field from file " + storageFile.Name + " due to exception: " + ex);
                                }
                                t = TypeInfo.Text;
                                break;
                        }
                        if (data != null)
                        {
                            var layoutDoc = AddFieldFromData(data, doc, key, dropPoint, t, activeLayout);
                            dropPoint.Y += layoutDoc.GetHeightField().Data + 10;
                        }
                        
                    }
                }
            }
            e.Handled = true;
        }

        public static DocumentController CreateFieldLayoutDocumentFromReference(ReferenceFieldModelController reference, double x = 0, double y = 0, double w = 200, double h = 200, TypeInfo listType = TypeInfo.None)
        {
            var type = reference.DereferenceToRoot(null).TypeInfo;
            switch (type)
            {
                case TypeInfo.Text:
                    return new TextingBox(reference, x, y, w, h).Document;
                case TypeInfo.Number:
                    return new TextingBox(reference, x, y, w, h).Document;
                case TypeInfo.Image:
                    return new ImageBox(reference, x, y, w, h).Document;
                case TypeInfo.Collection:
                    return new CollectionBox(reference, x, y, w, h).Document;
                case TypeInfo.Document:
                    return new DocumentBox(reference, x, y, w, h).Document;
                case TypeInfo.Point:
                    return new TextingBox(reference, x, y, w, h).Document;
                case TypeInfo.RichText:
                    return new RichTextBox(reference, x, y, w, h).Document;
                default:
                    return null;
            }
        }

        public static DocumentController AddFieldFromData(object data, DocumentController document, KeyController key, Point position, TypeInfo type, DocumentCollectionFieldModelController activeLayout)
        {
            FieldModelDTO dto = new FieldModelDTO(type, data);
            var fmc = TypeInfoHelper.CreateFieldModelControllerHelper(dto);
            document.SetField(key, fmc, true);
            var layoutDoc =
                CreateFieldLayoutDocumentFromReference(new ReferenceFieldModelController(document.GetId(), key), position.X, position.Y);
            activeLayout?.AddDocument(layoutDoc);
            return layoutDoc;
        }
    }
}
