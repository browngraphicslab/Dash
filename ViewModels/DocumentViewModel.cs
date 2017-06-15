﻿using Dash;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;
using DashShared;
using Microsoft.Extensions.DependencyInjection;
using Dash.Models;

namespace Dash
{
    public class DocumentViewModel
    {
        public DocumentViewModel() { }
        public DocumentViewModel(DocumentModel docModel)
        {
            DocumentModel = docModel;
        }
        public DocumentModel DocumentModel { get; set; }

        public virtual List<UIElement> GetUiElements()
        {
            var uiElements = new List<UIElement>(DocumentModel.Fields.Count);
            LayoutModel layout = GetLayoutModel();
            foreach (var field in DocumentModel.Fields)
            {
                if (layout.Fields.ContainsKey(field.Key))
                {
                    uiElements.Add(field.Value.MakeView(layout.Fields[field.Key]));
                }
            }
            return uiElements;
        }
        public LayoutModel GetLayoutModel()
        {
            var keyController = App.Instance.Container.GetRequiredService<KeyController>();
            var layoutModelRef = GetLayoutModelRefForDoc(DocumentModel);

            var docController = App.Instance.Container.GetRequiredService<DocumentController>();
            var refField = docController.GetDocumentAsync(layoutModelRef.DocId).Field(layoutModelRef.FieldKey) as LayoutModelFieldModel;

            return refField.Data;
        }
        public void SetLayoutModel(LayoutModel layoutModel)
        {
            var keyController = App.Instance.Container.GetRequiredService<KeyController>();
            var layoutModelRef = GetLayoutModelRefForDoc(DocumentModel);

            // set value of layoutModelRef to layoutModel
        }

        static DocumentModel DefaultLayoutModelSource = null;
        static Key GetFieldKeyByName(string name)
        {
            var keyController = App.Instance.Container.GetRequiredService<KeyController>();
            var key = keyController.GetKeyAsync(name);
            if (key == null)
                key = keyController.CreateKeyAsync(name);
            return key;
        }
        static ReferenceFieldModel GetLayoutModelRefForDoc(DocumentModel doc)
        {
            var docController = App.Instance.Container.GetRequiredService<DocumentController>();
            var layoutKey = GetFieldKeyByName("Layout");

            // look for a specific layout stored on the document itself
            if (doc.Field(layoutKey) is LayoutModelFieldModel)
            {
                return new ReferenceFieldModel(doc.Id, layoutKey);
            }

            // then look for a directory document where we can lookup a layoutModel
            DocumentModel layoutModelSource = null;
            if (doc.Field(layoutKey) is DocumentModelFieldModel)
            {
                layoutModelSource = (doc.Field(layoutKey) as DocumentModelFieldModel).Data;
            }
             // finally, use the default directory document for looking up a layout model given a document type
            else
            {
                if (DefaultLayoutModelSource == null)
                {
                    DefaultLayoutModelSource = docController.CreateDocumentAsync("DefaultLayoutModelSource");
                }
                layoutModelSource = DefaultLayoutModelSource;
            }
            var layoutKeyForDocumentType = GetFieldKeyByName(doc.DocumentType.Type);
            if (!layoutModelSource.Fields.ContainsKey(layoutKeyForDocumentType))
            {
                // bcz: hack to have a default layout for known types: recipes, Umpires
                if (doc.DocumentType.Type == "recipes")
                    layoutModelSource.Fields.Add(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.Food2ForkRecipeModel(doc)));
                else if (doc.DocumentType.Type == "Umpires")
                    layoutModelSource.Fields.Add(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.UmpireModel(doc)));
                else if (doc.DocumentType.Type == "oneimage")
                    layoutModelSource.Fields.Add(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.OneImageModel(doc)));
                else if (doc.DocumentType.Type == "twoimages")
                    layoutModelSource.Fields.Add(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.TwoImagesAndTextModel(doc)));
                else if (doc.DocumentType.Type == "operator")
                    layoutModelSource.Fields.Add(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.OperatorLayoutModel(doc)));
                else if (doc.DocumentType.Type == "example_api_object")
                    layoutModelSource.Fields.Add(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.ExampleApiObject(doc)));
                else if (doc.DocumentType.Type == "collection_example")
                    layoutModelSource.Fields.Add(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.ExampleCollectionModel(doc)));
                else if (doc.DocumentType.Type == "price_per_square_foot")
                    layoutModelSource.Fields.Add(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.PricePerSquareFootApiObject(doc)));
            }
            return new ReferenceFieldModel(layoutModelSource.Id, layoutKeyForDocumentType);
        }
    }
}
