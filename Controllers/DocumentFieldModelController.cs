using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using Windows.UI.Xaml.Data;
using Dash.Converters;
using System.Linq;
using DashShared.Models;
using static Dash.DocumentController;

namespace Dash
{
    public class DocumentFieldModelController : FieldModelController<DocumentFieldModel>
    {

        public DocumentFieldModelController(DocumentController document) : base(new DocumentFieldModel(document.GetId()))
        {
            Data = document;
        }

        private DocumentFieldModelController(DocumentController document, DocumentFieldModel model) : base(model)
        {
            Data = document;
        }

        public DocumentFieldModelController(DocumentFieldModel model) : base(model)
        {
            Data = ContentController<DocumentModel>.GetController<DocumentController>(model.Data);
        }

        /*
        public static async Task<DocumentFieldModelController> CreateFromServer(DocumentFieldModel documentFieldModel)
        {
            var localController = ContentController<FieldModel>.GetController<DocumentFieldModelController>(documentFieldModel.Id);
            if (localController != null)
            {
                return localController;
            }

            DocumentController docController = null;

            await RESTClient.Instance.Documents.GetDocument(documentFieldModel.Data, async model =>
            {
                docController = new DocumentController(model);

                foreach (var keyFieldPair in docController.EnumFields(true))
                {
                    if (keyFieldPair.Value is DocumentFieldModelController)
                    {
                        var dfmc = (DocumentFieldModelController)keyFieldPair.Value;
                        await RESTClient.Instance.Documents.GetDocument(
                            dfmc.DocumentFieldModel.Data, async protoDto =>
                            {
                                dfmc.Data = new DocumentController(protoDto);
                            }, exception => throw exception);
                    }

                    if (keyFieldPair.Value is DocumentCollectionFieldModelController)
                    {
                        Debug.Assert(keyFieldPair.Key.Equals(KeyStore.DelegatesKey) == false, "the document controller should skip over creating any delegates field since it creates infinite loops");

                        var dcfmc = ((DocumentCollectionFieldModelController)keyFieldPair.Value);
                        var documentIds = dcfmc.DocumentCollectionFieldModel.Data;

                        await RESTClient.Instance.Documents.GetDocuments(documentIds, async docmodelDtos =>
                        {
                            foreach (var docDto in docmodelDtos)
                            {
                                new  DocumentController(docDto);
                            }
                        }, exception => throw exception);
                    }
                }
            }, exception => throw exception);


            return new DocumentFieldModelController(docController, documentFieldModel);

        }
        */
        /// <summary>
        ///     The <see cref="DocumentFieldModel" /> associated with this <see cref="DocumentFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public DocumentFieldModel DocumentFieldModel => Model as DocumentFieldModel;


        private DocumentController _data;

        /// <summary>
        ///     A wrapper for <see cref="DocumentFieldModel.Data" />. Change this to propagate changes
        ///     to the server
        /// </summary>
        /// 
        public override object GetValue(Context context)
        {
            return Data;
        }
        public override bool SetValue(object value)
        {
            if (!(value is DocumentController))
                return false;
            Data = value as DocumentController;
            return true;
        }
        OnDocumentFieldUpdatedHandler primaryKeyHandler;
        public DocumentController Data
        {
            get { return _data; }
            set
            {
                var oldData = _data;

                if (_data == null || _data.Equals(value))
                {
                    _data = value;
                    if (oldData != null)
                        oldData.DocumentFieldUpdated -= primaryKeyHandler;
                    primaryKeyHandler = (sender, args) =>
                    {
                        var keylist = (_data.GetDereferencedField<ListFieldModelController<TextFieldModelController>>(KeyStore.PrimaryKeyKey, new Context(_data))?.Data.Select((d) => (d as TextFieldModelController).Data));
                        if (keylist != null && keylist.Contains(args.Reference.FieldKey.Id))
                            OnFieldModelUpdated(null);
                    };
                    value.DocumentFieldUpdated += primaryKeyHandler;
                    OnFieldModelUpdated(null);
                    UpdateOnServer();
                }
            }
        }
        /// <summary>
        /// Returns a simple view of the model which the controller encapsulates, for use in a Table Cell
        /// </summary>
        /// <returns></returns>
        //public override FrameworkElement GetTableCellView(Context context)
        //{
        //    var tb = new DocumentView(new DocumentViewModel(Data, false, context));
        //    tb.Height = 25;
        //    return tb;
        //}
        public override TypeInfo TypeInfo => TypeInfo.Document;

        public override IEnumerable<DocumentController> GetReferences()
        {
            yield return Data;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new DocumentFieldModelController(Data.GetPrototype() ?? new DocumentController(new DocumentModel(new Dictionary<KeyModel, FieldModel>(), new DocumentType(DashShared.Util.GetDeterministicGuid("Default Document")))));
        }

        public override void MakeAllViewUI(DocumentController container, KeyController kc, Context context, Panel sp, string id, bool isInterfaceBuilder=false)
        {
            var view = new DocumentView(new DocumentViewModel(Data, isInterfaceBuilder));
            sp.Children.Add(view);
        }

        public override FieldModelController<DocumentFieldModel> Copy()
        {
            return new DocumentFieldModelController(Data.GetCopy());
        }
    }
}