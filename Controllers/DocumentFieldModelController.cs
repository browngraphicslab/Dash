using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using Windows.UI.Xaml.Data;
using Dash.Converters;
using System.Linq;

namespace Dash
{
    public class DocumentFieldModelController : FieldModelController
    {
        public DocumentFieldModelController(DocumentController document) : base(new DocumentModelFieldModel(document?.DocumentModel))
        {
            Data = document;
        }

        /// <summary>
        ///     The <see cref="DocumentModelFieldModel" /> associated with this <see cref="DocumentFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public DocumentModelFieldModel DocumentModelFieldModel => FieldModel as DocumentModelFieldModel;


        private DocumentController _data;
        /// <summary>
        ///     A wrapper for <see cref="DocumentModelFieldModel.Data" />. Change this to propagate changes
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
                    // update local
                    // update server
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
            // if the field model is changed, then this Binding will fire and the text will update
            Binding textBinding = new Binding
            {
                Source = this,
                Converter = new DocumentFieldModelToStringConverter(),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            textBlock.SetBinding(TextBlock.TextProperty, textBinding);

            // if fields within the document change, then we need to recreate the binding to 
            // force the text to update in case one of the fields that changed is a Primary Key (and thus affects the text conversion).
            Data.DocumentFieldUpdated += ((sender, ctxt) =>
            {
                if ((Data.GetDereferencedField(KeyStore.PrimaryKeyKey, ctxt.Context) as ListFieldModelController<TextFieldModelController>).Data.Where((d) => (d as TextFieldModelController).Data == ctxt.Reference.FieldKey.Id).Count() > 0)
                {
                    textBinding = new Binding
                    {
                        Source = this,
                        Converter = new DocumentFieldModelToStringConverter(),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    };
                    textBlock.SetBinding(TextBlock.TextProperty, textBinding);
                }
            });
          
            // textBlock.Text = $"Document of type: {DocumentModelFieldModel.Data.DocumentType}";
        }

        public override FieldModelController Copy()
        {
            return new DocumentFieldModelController(Data);
        }
    }
}