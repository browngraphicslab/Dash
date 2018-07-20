using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Dash.Controllers;

namespace Dash.Converters
{

	class DataFieldToMakeViewConverter : SafeDataToXamlConverter<FieldControllerBase, UIElement>
	{

		private DocumentController _docController = null;
		private Context _context = null;
		private UIElement _lastView = null;

		public DataFieldToMakeViewConverter(DocumentController docController, Context context)
		{
			_docController = docController;
			_context = context;
		}

		public override UIElement ConvertDataToXaml(FieldControllerBase data, object parameter = null)
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

			UIElement currView = null;

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
				currView =  AudioBox.MakeView(_docController, _context);
			}
			else if (data is ListController<DocumentController> docList)
			{
				currView =  CollectionBox.MakeView(_docController, _context);
			}
			else if (data is DocumentController dc)
			{
				// hack to check if the dc is a view document
				FrameworkElement view = null;
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

			//check if the view has changed
			if (_lastView == null || currView.GetType() != _lastView.GetType())
			{
				_lastView = currView;
				return currView;
			}
			else
			{
				return null;
			}
		}

		public override FieldControllerBase ConvertXamlToData(UIElement xaml, object parameter = null)
		{
			throw new NotImplementedException();
		}
	}
}
