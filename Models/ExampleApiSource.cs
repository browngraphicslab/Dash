using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using DashShared;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Dash
{
    public class ExampleApiSource
    {
        // the documents in this api
        private List<ExampleObject> _documents;

        // the keys to the documents in this api
        public List<Key> Keys;

        // the type of this document
        public DocumentType DocumentType;

        // the objects in this api
        private class ExampleObject
        {
            public int id { get; set; }
            public string first_name { get; set; }
            public string last_name { get; set; }
            public string email { get; set; }
            public string gender { get; set; }
            public string ip_address { get; set; }
        }

        public ExampleApiSource()
        {

         }

        public async Task Initialize()
        {
            //StorageFolder folder =
            //    await StorageFolder.GetFolderFromPathAsync(
            //        @"C:\Users\luke murray\Documents\Visual Studio 2015\Projects\Dash\Assets");
            //var file = await folder.GetFileAsync("MOCK_DATA.json");

            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/a.txt"));
            var docSource = await FileIO.ReadTextAsync(file);

            _documents = JsonConvert.DeserializeObject<List<ExampleObject>>(docSource);


            var keyController = App.Instance.Container.GetRequiredService<KeyController>();
            Keys = new List<Key>
            {
                keyController.CreateKeyAsync("id"),
                keyController.CreateKeyAsync("first_name"),
                keyController.CreateKeyAsync("last_name"),
                keyController.CreateKeyAsync("email"),
                keyController.CreateKeyAsync("gender"),
                keyController.CreateKeyAsync("ip_address"),
            };

            var typeController = App.Instance.Container.GetRequiredService<TypeController>();
            DocumentType = typeController.CreateTypeAsync("example_api_object");

        }

        public List<DocumentModel> GetDocumentsAsync()
        {
            var docController = App.Instance.Container.GetRequiredService<DocumentController>();

            var outputDocs = new List<DocumentModel>();

            foreach (var doc in _documents)
            {
                var fields = new Dictionary<Key, FieldModel>();
                fields[Keys.First(k => k.Name == "id")] = new TextFieldModel("{doc.id}");
                fields[Keys.First(k => k.Name == "first_name")] = new TextFieldModel(doc.first_name);
                fields[Keys.First(k => k.Name == "last_name")] = new TextFieldModel(doc.last_name);
                fields[Keys.First(k => k.Name == "email")] = new TextFieldModel(doc.email);
                fields[Keys.First(k => k.Name == "gender")] = new TextFieldModel(doc.gender);
                fields[Keys.First(k => k.Name == "ip_address")] = new TextFieldModel(doc.ip_address);

                var newDoc = docController.CreateDocumentAsync(DocumentType);
                newDoc.Fields = fields;

                outputDocs.Add(newDoc);
            }

            return outputDocs;
        }

    }
}
