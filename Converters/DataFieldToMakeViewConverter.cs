using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
        private DocumentType _lastDocType = DocumentType.DefaultType;
        private DocumentController _lastDocument = null;

        public DataFieldToMakeViewConverter(DocumentController docController, Context context = null)
        {
            _docController = docController;
            _context = context;
        }

        public override FrameworkElement ConvertDataToXaml(FieldControllerBase data, object parameter = null)
        {
            FrameworkElement currView = null;

            if (_lastType == data?.TypeInfo && 
                (_lastType != TypeInfo.Document || 
                 (_lastDocType.Equals((data as DocumentController).DocumentType) &&
                  _lastDocument.Equals(data as DocumentController))))
            {
                return _lastElement;
            }
            if (data is ImageController img)
            {
                currView = ImageBox.MakeView(_docController, _context);
            }
            if (data is PdfController)
            {
                currView = PdfBox.MakeView(_docController, _context);
            }
            if (data is VideoController)
            {
                currView = VideoBox.MakeView(_docController, _context);
            }
            else if (data is AudioController)
            {
                currView = AudioBox.MakeView(_docController, KeyStore.DataKey, _context);
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
                _lastDocType = dc.DocumentType;
                _lastDocument = dc;
            }
            else if (data is TextController || data is NumberController || data is DateTimeController)
            {
                currView = TextingBox.MakeView(_docController, _context);
            }
            else if (data is RichTextController)
            {
                currView = RichTextBox.MakeView(_docController, KeyStore.DataKey, _context);
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
