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
                    Key = _key,
                    Mode = BindingMode.OneWay,
                    Converter = UriToBitmapImageConverter.Instance
                };
                image.AddFieldBinding(Image.SourceProperty, binding);
                image.Height = 100;
                image.Margin = new Thickness(10, 10, 10, 10);
                currView = image;
            }
            if (data is VideoController)
            {

                var vid = new MediaPlayerElement();
                var binding = new FieldBinding<VideoController>
                {
                    Document = _docController,
                    Key = _key,
                    Mode = BindingMode.OneWay,
                    Converter = UriToIMediaPlayBackSourceConverter.Instance
                };
                vid.AddFieldBinding(MediaPlayerElement.SourceProperty, binding);
                currView = vid;
            }
            else if (data is AudioController)
            {
                currView = AudioBox.MakeView(_docController, _key, _context);
            }
            else if (data is BoolController val)
            {

                var toggleSwitch = new ToggleSwitch
                {
                    OnContent = "True",
                    OffContent = "False",
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                toggleSwitch.AddFieldBinding(ToggleSwitch.IsOnProperty, new FieldBinding<BoolController> { Document = _docController, Key = _key, Mode = BindingMode.TwoWay, FieldAssignmentDereferenceLevel = XamlDereferenceLevel.DereferenceToRoot });
                currView = toggleSwitch;
            }
            else if (data is ListController<TextController> textList)
            {
                WrapPanel wrap = new WrapPanel();
                wrap.HorizontalAlignment = HorizontalAlignment.Center;
                wrap.Margin = new Thickness(0, 12, 0, 0);
                foreach (var text in textList)
                {
                    var r = new Random();
                    var hexColor = Color.FromArgb(150, (byte)r.Next(256), (byte)r.Next(256), (byte)r.Next(256));
                    var listText = new KVPListText(text.Data, hexColor);
                    wrap.Children.Add(listText);
                }

                currView = wrap;

            }
            else if (data is ListController<DocumentController> docList)
            {

                WrapPanel wrap = new WrapPanel();

                KVPDocBox docBox = null;
                wrap.HorizontalAlignment = HorizontalAlignment.Center;
                wrap.Margin = new Thickness(0, 10, 0, 10);
                foreach (var doc in docList)
                {
                    docBox = new KVPDocBox(doc.DocumentType, doc.Title);
                    wrap.Children.Add(docBox);

                }
                currView = wrap;
            }
            else if (data is DocumentController dc)
            {
                var Stack = new StackPanel() { Orientation = Orientation.Horizontal, Padding=new Thickness(0) };
                if (dc.DocumentType.Equals(DataBox.DocumentType))
                {
                    var db = dc.GetField(KeyStore.DataKey) as ReferenceController;
                    Stack.Children.Add(new TextBlock() { Padding = new Thickness(0), VerticalAlignment=VerticalAlignment.Center, Text = "DATABOX("  + db.GetDocumentController(null)?.Title + "->" + db.FieldKey.Name +")"?? ":" });
                }
                else
                {
                    Stack.Children.Add(new TextBlock() { Padding = new Thickness(0), VerticalAlignment = VerticalAlignment.Center, Text = "Document:" });
                    var tb = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(0), VerticalAlignment = VerticalAlignment.Center, HorizontalTextAlignment = TextAlignment.Left };

                    tb.AddFieldBinding(TextBlock.TextProperty, new FieldBinding<TextController>()
                    {
                        Mode = BindingMode.OneWay,
                        Document = dc,
                        Key = KeyStore.TitleKey,
                    });
                    Stack.Children.Add(tb);
                }
                currView = Stack;
            }
            else if (data is ListController<BoolController> boolList)
            {
                WrapPanel wrap = new WrapPanel();
                currView = wrap;

            }
            else if (data is RichTextController)
            {

                var mv = RichTextBox.MakeView(_docController, _key, _context);
                Grid grid = new Grid();
                grid.Children.Add(mv);
                grid.Margin = new Thickness(0, 8, 0, 0);
                currView = grid;
            }
            else if (data is TextController)
            {
                var mv = TextingBox.MakeView(_docController, _key, _context);
                Grid grid = new Grid();
                grid.Children.Add(mv);
                grid.Margin = new Thickness(0, -4, 0, 0);
                currView = grid;
            }

            if (currView == null)
            {
                var tb = new TextBlock();
                var binding = new FieldBinding<FieldControllerBase, TextController>()
                {
                    Document = _docController,
                    Key = _key,
                    Mode = BindingMode.TwoWay,
                    GetConverter = FieldConversion.GetFieldtoStringConverter,
                    FallbackValue = "<null>",
                };
                tb.AddFieldBinding(TextBlock.TextProperty, binding);
                currView = tb;
            }

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
