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

        public override FieldModelController Copy()
        {
            return new DocumentFieldModelController(Data);
        }
    }
}