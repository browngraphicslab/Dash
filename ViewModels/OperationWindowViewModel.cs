﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Dash.Models;
using DashShared;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    public class OperationWindowViewModel
    {
        public DocumentModel InputDocument;
        public DocumentModel OutputDocument;

        public OperationWindowViewModel(DocumentModel inputDocument)
        {
            InputDocument = inputDocument;
            Dictionary<Key, FieldModel> fields = new Dictionary<Key, FieldModel>();
            foreach (var documentModelField in InputDocument.EnumFields())
            {
                if (documentModelField.Value is DocumentModelFieldModel || documentModelField.Value is LayoutModelFieldModel)
                {
                    continue;
                }
                fields.Add(documentModelField.Key, documentModelField.Value.Copy());
                InputDocumentCollection[documentModelField.Key] =
                    _defaultTemplateModels[documentModelField.Value.GetType()].MakeView(documentModelField.Value);
            }
            DocumentEndpoint docEndpoint = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            OutputDocument = docEndpoint.CreateDocumentAsync(InputDocument.DocumentType.Type);//TODO Should this be the same as source document?
            OutputDocument.SetFields(fields);
        }

        private Dictionary<Type, TemplateModel> _defaultTemplateModels = new Dictionary<Type, TemplateModel>
        {
            {typeof(TextFieldModel), new TextTemplateModel(0, 0, FontWeights.Normal)},
            {typeof(NumberFieldModel), new TextTemplateModel(0, 0, FontWeights.Normal)},
            {typeof(ImageFieldModel), new ImageTemplateModel(0, 0, 100, 100)},
            {typeof(DocumentCollectionFieldModel), new DocumentCollectionTemplateModel(0, 0, 100, 100) }
        };

        private void InputDocument_DocumentFieldUpdated(ReferenceFieldModel fieldReference)
        {
            if (InputDocumentCollection.ContainsKey(fieldReference.FieldKey))
            {
                return;
            }

            DocumentEndpoint docEndpoint = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            FieldModel model = docEndpoint.GetFieldInDocument(fieldReference);
            InputDocumentCollection[fieldReference.FieldKey] = _defaultTemplateModels[model.GetType()].MakeView(model);
        }

        private void OutputDocument_DocumentFieldUpdated(ReferenceFieldModel fieldReference)
        {
            if (OutputDocumentCollection.ContainsKey(fieldReference.FieldKey))
            {
                return;
            }

            DocumentEndpoint docEndpoint = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            FieldModel model = docEndpoint.GetFieldInDocument(fieldReference);
            OutputDocumentCollection[fieldReference.FieldKey] = _defaultTemplateModels[model.GetType()].MakeView(model);
        }

        public ObservableDictionary<Key, UIElement> InputDocumentCollection { get; set; } =
            new ObservableDictionary<Key, UIElement>();

        public ObservableDictionary<Key, UIElement> OutputDocumentCollection { get; set; } =
            new ObservableDictionary<Key, UIElement>();
    }
}
