using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Dash.Controllers;
using DashShared;

namespace Dash.Converters
{

    class DataFieldToMakeViewConverter : SafeDataToXamlConverter<FieldControllerBase, FrameworkElement>
    {

        private DocumentController _docController = null;
        private Context _context = null;
        private TypeInfo _lastType = TypeInfo.None;
        private FrameworkElement _lastElement = null;

        public DataFieldToMakeViewConverter(DocumentController docController, Context context)
        {
            _docController = docController;
            _context = context;
        }

        public override FrameworkElement ConvertDataToXaml(FieldControllerBase data, object parameter = null)
        {
            if (data is TextController txt && txt.Data.StartsWith("=="))
            {
                try
                {
                    data = DSL.InterpretUserInput(txt.Data)?.DereferenceToRoot(null);
                }
                catch (Exception) { }
            }
            //if (data is ListController<DocumentController> documentList)
            //{
            //    data = new TextController(new ObjectToStringConverter().ConvertDataToXaml(documentList, null));
            //}

            FrameworkElement currView = null;

            if (_lastType == data?.TypeInfo && _lastType != TypeInfo.Document)
            {
                return _lastElement;
            }
            if (data is ImageController img)
            {
                if (img.Data.LocalPath.EndsWith(".pdf"))
                    return PdfBox.MakeView(_docController, _context);

                currView = ImageBox.MakeView(_docController, _context);
            }
            if (data is VideoController)
            {
                currView = VideoBox.MakeView(_docController, _context);
            }
            else if (data is AudioController)
            {
                currView = AudioBox.MakeView(_docController, _context);
            }
            else if (data is ListController<DocumentController> docList)
            {
                if (double.IsNaN( _docController.GetWidth()))
                { // if we're going to show a CollectionBox, give it initial dimensions, otherwise it has no default size
                    _docController.SetWidth(400);
                    _docController.SetHeight(400);
                }
                currView = CollectionBox.MakeView(_docController, _context);
            }
            else if (data is DocumentController dc)
            {
                // hack to check if the dc is a view document
                if (KeyStore.TypeRenderer.ContainsKey(dc.DocumentType))
                {
                    currView = dc.MakeViewUI(_context);
                }
                else
                {
                    currView = dc.GetKeyValueAlias().MakeViewUI(_context);
                }
                //bcz: this is odd -- the DocumentViewModel is bound to the DataBox, so we have to transfer the
                //   "container-like" bindings from the contained data view to the DataBox
                //TODO: DO I NEED THE NEXT LINE?
                //SetupBindings(view, _docController, _context);
                //return view;
            }
            else if (data is TextController || data is NumberController || data is DateTimeController)
            {
                currView = TextingBox.MakeView(_docController, _context);
            }
            else if (data is RichTextController)
            {
                currView = RichTextBox.MakeView(_docController, _context);
            }
            if (currView == null) currView = new Grid();

            _lastElement = currView;
            _lastType = data?.TypeInfo ?? TypeInfo.None;

            return currView;
        }

        public override FieldControllerBase ConvertXamlToData(FrameworkElement xaml, object parameter = null)
        {
            throw new NotImplementedException();
        }
    }
}
