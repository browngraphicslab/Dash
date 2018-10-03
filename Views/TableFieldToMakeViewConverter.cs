using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Dash.Controllers;
using Dash.Converters;
using DashShared;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI.Extensions;

namespace Dash
{
    class TableFieldToMakeViewConverter : SafeDataToXamlConverter<FieldControllerBase, FrameworkElement>
    {

        private DocumentController _docController = null;
        private KeyController _key = null;
        private Context _context = null;
        private TypeInfo _lastType = TypeInfo.None;
        private FrameworkElement _lastElement = null;

        public TableFieldToMakeViewConverter(DocumentController docController, KeyController key, Context context)
        {
            _docController = docController;
            _key = key;
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
                var image = new Image();
                var binding = new FieldBinding<ImageController>
                {
                    Document = _docController,
                    Key = KeyStore.DataKey,
                    Mode = BindingMode.OneWay,
                    Converter = UriToBitmapImageConverter.Instance
                };
                image.AddFieldBinding(Image.SourceProperty, binding);
                currView = image;
            }
            if (data is PdfController)
            {
                currView = PdfBox.MakeView(_docController, _context);
            }
            if (data is VideoController)
            {

                var vid = new MediaPlayerElement();
                var binding = new FieldBinding<VideoController>
                {
                    Document = _docController,
                    Key = KeyStore.DataKey,
                    Mode = BindingMode.OneWay,
                    Converter = UriToIMediaPlayBackSourceConverter.Instance
                };
                vid.AddFieldBinding(MediaPlayerElement.SourceProperty, binding);
                currView = vid;
                //currView = VideoBox.MakeView(_docController, _context);
            }
            else if (data is AudioController)
            {
                currView = AudioBox.MakeView(_docController, _context);
            }
            else if (data is BoolController val)
            {
                   
                var toggleSwitch = new ToggleSwitch();
                toggleSwitch.OnContent = "True";
                toggleSwitch.OffContent = "False";
                toggleSwitch.HorizontalAlignment = HorizontalAlignment.Center;
                toggleSwitch.AddFieldBinding(ToggleSwitch.IsOnProperty, new FieldBinding<BoolController> { Document = _docController, Key = _key, Mode = BindingMode.TwoWay, FieldAssignmentDereferenceLevel = XamlDereferenceLevel.DereferenceToRoot });
                {


                    //<SolidColorBrush x:Key="ToggleSwitchFillOff" Color="Green"></SolidColorBrush>
                    //<SolidColorBrush x:Key="ToggleSwitchFillOn" Color="Yellow"></SolidColorBrush>

                };
                toggleSwitch.Margin = new Thickness(0, 12, 0, 0);
                currView = toggleSwitch;
            }
            else if (data is ListController<TextController> textList)
            {
                WrapPanel wrap = new WrapPanel();
                KVPListText listText = null;
                wrap.HorizontalAlignment = HorizontalAlignment.Center;
                wrap.Margin = new Thickness(0, 12, 0, 0);
                foreach (var text in textList)
                {
                    var r = new Random();
                    var hexColor = Color.FromArgb(150, (byte)r.Next(256), (byte)r.Next(256), (byte)r.Next(256));
                    listText = new KVPListText(text.Data, hexColor);
                    wrap.Children.Add(listText);
                }

                currView = wrap;

            }
            else if (data is ListController<DocumentController> docList)
            {
                //if (double.IsNaN(_docController.GetWidth()))
                //{ // if we're going to show a CollectionBox, give it initial dimensions, otherwise it has no default size
                //    _docController.SetWidth(400);
                //    _docController.SetHeight(400);
                //}
                //currView = CollectionBox.MakeView(_docController, _context);

                WrapPanel wrap = new WrapPanel();
              
                KVPDocBox docBox = null;
                wrap.HorizontalAlignment = HorizontalAlignment.Center;
                wrap.Margin = new Thickness(0, 10, 0, 0);
                foreach (var doc in docList)
                {
                    docBox = new KVPDocBox(doc.DocumentType, doc.Title);
                    wrap.Children.Add(docBox);
                    
                }
                currView = wrap;
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
            } else if (data is ListController<BoolController> boolList)
            {
                WrapPanel wrap = new WrapPanel();
                //KVPListText listText = null;
                //wrap.HorizontalAlignment = HorizontalAlignment.Center;
                //wrap.Margin = new Thickness(0, 15, 0, 0);
                //foreach (var booler in boolList)
                //{
                //    Grid grid = new Grid();

                //    grid.Width = 24;
                //    grid.Height = 24;
                //    grid.Margin = new Thickness(6, 0, 0, 0);
                //    TextBlock text = new TextBlock();

                //    text.Margin = new Thickness(-1, -4, 0, 0);

                //    if (booler.Data)
                //    {
                //        text.Text = "T";
                //        text.Foreground = new SolidColorBrush(Color.FromArgb(255, 16, 160, 93));
                //    }
                //    else
                //    {
                //        text.Text = "F";
                //        text.Foreground = new SolidColorBrush(Color.FromArgb(255, 186, 0, 21));
                //    }
                //    grid.Children.Add(text);
                //    wrap.Children.Add(grid);
                //}

                currView = wrap;

            }
            else if (data is TextController || data is NumberController || data is DateTimeController)
            {
                FrameworkElement mv = TextingBox.MakeView(_docController, _context);
                Grid grid = new Grid();
                grid.Children.Add(mv);
                grid.Margin = new Thickness(0, 8, 0, 0);
                currView = grid;
            }
            else if (data is RichTextController)
            {

                FrameworkElement mv = RichTextBox.MakeView(_docController, _context);
                Grid grid = new Grid();
                grid.Children.Add(mv);
                grid.Margin = new Thickness(0, 8, 0, 0);
                currView = grid;
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
