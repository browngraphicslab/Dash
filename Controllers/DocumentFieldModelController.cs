using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using Windows.UI.Xaml.Data;
using Dash.Converters;

namespace Dash
{
    public class DocumentFieldModelController : FieldModelController
    {
        public DocumentFieldModelController(DocumentController document) : base(new DocumentFieldModel(document?.DocumentModel), false)
        {
            Data = document;
        }

        private DocumentFieldModelController(DocumentFieldModel documentFieldModel) : base(documentFieldModel, true)
        {
            Data = DocumentController.CreateFromServer(RESTClient.Instance.Documents.GetDocument(documentFieldModel.Id).Result);
        }

        public static DocumentFieldModelController CreateFromServer(DocumentFieldModel documentFieldModel)
        {
            return new DocumentFieldModelController(documentFieldModel);
        }

        /// <summary>
        ///     The <see cref="DocumentFieldModel" /> associated with this <see cref="DocumentFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public DocumentFieldModel DocumentFieldModel => FieldModel as DocumentFieldModel;


        private DocumentController _data;

        /// <summary>
        ///     A wrapper for <see cref="DocumentFieldModel.Data" />. Change this to propagate changes
        ///     to the server
        /// </summary>
        public DocumentController Data
        {
            get { return _data; }
            set
            {
                if (SetProperty(ref _data, value))
                {
                    OnFieldModelUpdated(null);
                    RESTClient.Instance.Fields.UpdateField(FieldModel, dto => { }, exception => { });
                }
            }
        }
        public override TypeInfo TypeInfo => TypeInfo.Document;

        public override FrameworkElement GetTableCellView(Context context)
        {
            return GetTableCellViewOfScrollableText(BindTextOrSetOnce);
        }

        public override IEnumerable<DocumentController> GetReferences()
        {
            yield return Data;
        }

        public override FieldModelController GetDefaultController()
        {
            return new DocumentFieldModelController(Data.GetPrototype() ?? 
                new DocumentController(new Dictionary<KeyController, FieldModelController>(), new DocumentType(DashShared.Util.GetDeterministicGuid("Default Document"))));
        }

        private void BindTextOrSetOnce(TextBlock textBlock)
        {
            Binding textBinding = new Binding
            {
                Source = this,
                Converter = new DocumentFieldModelToStringConverter(),
                Mode = BindingMode.TwoWay
            };
            textBlock.SetBinding(TextBlock.TextProperty, textBinding);
           // textBlock.Text = $"Document of type: {DocumentFieldModel.Data.DocumentType}";
        }

        public override FieldModelController Copy()
        {
            return new DocumentFieldModelController(Data);
        }
    }
}