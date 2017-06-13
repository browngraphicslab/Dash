﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using DashShared;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Dash
{
    public class PricePerSquareFootApi
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
            public int price { get; set; }
            public double sqft { get; set; }
        }

        public PricePerSquareFootApi()
        {

        }

        public async Task Initialize()
        {

            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/ppsqft.txt"));
            var docSource = await FileIO.ReadTextAsync(file);

            _documents = JsonConvert.DeserializeObject<List<ExampleObject>>(docSource);


            var keyController = App.Instance.Container.GetRequiredService<KeyController>();
            Keys = new List<Key>
            {
                keyController.CreateKeyAsync("price"),
                keyController.CreateKeyAsync("sqft")
            };

            var typeController = App.Instance.Container.GetRequiredService<TypeController>();
            DocumentType = typeController.CreateTypeAsync("price_per_square_foot");

        }

        public List<DocumentModel> GetDocumentsAsync()
        {
            var docController = App.Instance.Container.GetRequiredService<DocumentController>();

            var outputDocs = new List<DocumentModel>();

            foreach (var doc in _documents)
            {
                var fields = new Dictionary<Key, FieldModel>();
                fields[Keys.First(k => k.Name == "price")] = new NumberFieldModel(doc.price);
                fields[Keys.First(k => k.Name == "sqft")] = new NumberFieldModel(doc.sqft);

                var newDoc = docController.CreateDocumentAsync(DocumentType);
                newDoc.Fields = fields;

                outputDocs.Add(newDoc);
            }

            return outputDocs;
        }
    }
}
