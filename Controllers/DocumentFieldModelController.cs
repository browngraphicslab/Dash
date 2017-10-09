using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using Windows.UI.Xaml.Data;
using Dash.Converters;
using System.Linq;
using static Dash.DocumentController;

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
                if (SetProperty(ref _data, value))
                {
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
                    // update local
                    // update server
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

        public override FieldModelController GetDefaultController()
        {
            return new DocumentFieldModelController(Data.GetPrototype() ?? 
                new DocumentController(new Dictionary<KeyController, FieldModelController>(), new DocumentType(DashShared.Util.GetDeterministicGuid("Default Document"))));
        }

        public override void MakeAllViewUI(DocumentController container, KeyController kc, Context context, Panel sp, string id, bool isInterfaceBuilder=false)
        {
            var view = new DocumentView(new DocumentViewModel(Data, isInterfaceBuilder));
            sp.Children.Add(view);
        }

        public override FieldModelController Copy()
        {
            return new DocumentFieldModelController(Data);
        }
    }
}