using DashShared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class RichTextView : UserControl
    {
        private RichTextFieldModelController _richTextFieldModelController;
        ObservableCollection<FontFamily> fonts = new ObservableCollection<FontFamily>();
        private int _selectionStart;
        private int _selectionEnd;

        private ITextSelection _selectedText
        {
            set { _richTextFieldModelController.SelectedText = value; }
        }

        public RichTextView(RichTextFieldModelController richTextFieldModelController)
        {
            this.InitializeComponent();
            _richTextFieldModelController = richTextFieldModelController;
            Loaded += OnLoaded;
            xRichEitBox.SelectionChanged += XRichEitBox_SelectionChanged;
            xRichEitBox.LostFocus += XRichEitBox_LostFocus;
            xRichEitBox.GotFocus += XRichEitBoxOnGotFocus;
            xRichEitBox.TextChanged += XRichEitBoxOnTextChanged;
            _richTextFieldModelController.FieldModelUpdated += RichTextFieldModelControllerOnFieldModelUpdated;
        }

        private void XRichEitBoxOnGotFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            ITextSelection selectedText = xRichEitBox.Document.Selection;
            if (selectedText != null)
            {
                xRichEitBox.Document.Selection.SetRange(_selectionStart, _selectionEnd);
                selectedText.CharacterFormat.BackgroundColor = Colors.White;
            }
        }

        private void XRichEitBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            _selectedText = xRichEitBox.Document.Selection;
        }


        private void XRichEitBox_LostFocus(object sender, RoutedEventArgs e)
        {
            //var richText = string.Empty;
            //xRichEitBox.Document.GetText(TextGetOptions.FormatRtf, out richText);
            //_richTextFieldModelController.RichTextData = richText;
            //xFormatRow.Height = new GridLength(0);

            //xRichEitBox.ManipulationMode = ManipulationModes.All;

            //_selectionEnd = xRichEitBox.Document.Selection.EndPosition;
            //_selectionStart = xRichEitBox.Document.Selection.StartPosition;

            //ITextSelection selectedText = xRichEitBox.Document.Selection;
            //if (selectedText != null)
            //{
            //    selectedText.CharacterFormat.BackgroundColor = Colors.LightGray;
            //}
        }

        private async Task<string> LoadText()
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/rtf.txt"));
            var rtfString = await FileIO.ReadTextAsync(file);
            return rtfString;
        }

        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            if (_richTextFieldModelController.RichTextData != null)
            {
                xRichEitBox.Document.SetText(TextSetOptions.FormatRtf, _richTextFieldModelController.RichTextData);
            }
            else
            {
                var rtfString = await LoadText();
                xRichEitBox.Document.SetText(TextSetOptions.FormatRtf, rtfString);
            }

        }


        private void RichTextFieldModelControllerOnFieldModelUpdated(FieldModelController sender, Context c)
        {
            var text = string.Empty;
            xRichEitBox.Document.GetText(TextGetOptions.FormatRtf, out text);
            if (_richTextFieldModelController.RichTextData != null && !text.Equals(_richTextFieldModelController.RichTextData))
            {
                xRichEitBox.Document.SetText(TextSetOptions.FormatRtf, _richTextFieldModelController.RichTextData);
            }
        }

        // freezes the app
        private void XRichEitBoxOnTextChanged(object sender, RoutedEventArgs routedEventArgs)
        {

            //var richText = string.Empty;
            //xRichEitBox.Document.GetText(TextGetOptions.FormatRtf, out richText);
            //_richTextFieldModelController.RichTextData = richText;

            var startPt = new Point();
            string allText;
            var s1 = this.xRichEitBox.Document.Selection.StartPosition;
            var s2 = this.xRichEitBox.Document.Selection.EndPosition;
            this.xRichEitBox.SelectionChanged -= xRichEitBox_SelectionChanged_1;

            xRichEitBox.Document.GetText(TextGetOptions.None, out allText);
            this.xRichEitBox.Document.Selection.GetPoint(HorizontalCharacterAlignment.Center, VerticalCharacterAlignment.Baseline, PointOptions.Start, out startPt);

            // try to get last typed character based on the current selection position 
            this.xRichEitBox.Document.Selection.SetRange(Math.Max(0, s1 - 1), s1);
            string character;
            this.xRichEitBox.Document.Selection.GetText(TextGetOptions.None, out character);

            // if the last character is white space, then we check to see if it terminates a hyperlink
            if (character == " " || character == "\r")
            {
                // search through all the text for the nearest '@' indicating the start of a possible hyperlink
                this.xRichEitBox.Document.Selection.SetRange(0, allText.Length);
                var atPos = -1;
                while (this.xRichEitBox.Document.Selection.FindText("@", 0, FindOptions.None) > 0)
                {
                    if (this.xRichEitBox.Document.Selection.StartPosition < s1)
                    {
                        atPos = this.xRichEitBox.Document.Selection.StartPosition;
                        this.xRichEitBox.Document.Selection.SetRange(atPos + 1, allText.Length);
                    }
                    else break;
                }

                // we found the nearest '@'
                if (atPos != -1)
                {
                    // get the text betweent the '@' and the current input position 
                    this.xRichEitBox.Document.Selection.SetRange(atPos + 1, s2 - 1);
                    string refText;
                    this.xRichEitBox.Document.Selection.GetText(TextGetOptions.None, out refText);
                    if (refText.StartsWith("HYPERLINK"))
                    {
                        refText = refText.Split('\"')[2].Trim(' ', '\r');
                    }
                    if (refText.StartsWith("http"))
                    {
                        // set the hyperlink for the matched text
                        this.xRichEitBox.Document.Selection.Link = "\"" + refText + "\"";
                        // advance the end selection past the RTF embedded HYPERLINK keyword
                        s2 += this.xRichEitBox.Document.Selection.Link.Length + "HYPERLINK".Length + 1;
                        s1 = s2;
                        this.xRichEitBox.Document.Selection.CharacterFormat.BackgroundColor = Colors.LightCyan;
                        this.xRichEitBox.Document.Selection.SetPoint(startPt, PointOptions.Start, true);
                    } else
                    {
                        // see if we can find a document whose primary keys match the text
                        var theDoc = DocumentController.FindDocMatchingPrimaryKeys(new List<string>(new string[] { refText }));
                        if (theDoc != null && this.xRichEitBox.Document.Selection.StartPosition != this.xRichEitBox.Document.Selection.EndPosition && this.xRichEitBox.Document.Selection.Link != "\"" + theDoc.GetId() + "\"")
                        {
                            // set the hyperlink for the matched text
                            this.xRichEitBox.Document.Selection.Link = "\"" + theDoc.GetId() + "\"";
                            // advance the end selection past the RTF embedded HYPERLINK keyword
                            s2 += this.xRichEitBox.Document.Selection.Link.Length + "HYPERLINK".Length + 1;
                            s1 = s2;
                            this.xRichEitBox.Document.Selection.CharacterFormat.BackgroundColor = Colors.LightCyan;
                            this.xRichEitBox.Document.Selection.SetPoint(startPt, PointOptions.Start, true);
                        }

                    }
                }
            }

            this.xRichEitBox.Document.Selection.SetRange(s1, s2);
            this.xRichEitBox.SelectionChanged += xRichEitBox_SelectionChanged_1;
        }

        int LastS1 = 0, LastS2 = 0;
        private void xRichEitBox_SelectionChanged_1(object sender, RoutedEventArgs e)
        {
            var s1 = this.xRichEitBox.Document.Selection.StartPosition;
            var s2 = this.xRichEitBox.Document.Selection.EndPosition;
            if (LastS1 != s1 || LastS2 != s2)
            {
                // if the selection has actually changed, then see if there's a Document hyperlink
                if (this.xRichEitBox.Document.Selection.Link.Length > 1)
                {
                    var target = this.xRichEitBox.Document.Selection.Link.Split('\"')[1];
                    if (target.StartsWith("http"))
                    {
                        var WebDoc = DBTest.PrototypeWeb.MakeDelegate();
                        {
                            WebDoc.SetField(DashConstants.KeyStore.ThisKey, new DocumentFieldModelController(WebDoc), true);
                            WebDoc.SetField(DBTest.WebUrlKey, new TextFieldModelController(target), true);
                            var webLayout = DBTest.PrototypeWebLayout.MakeDelegate();
                            webLayout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                            WebDoc.SetActiveLayout(webLayout, forceMask: true, addToLayoutList: true);
                            var pt = this.TransformToVisual(MainPage.Instance).TransformPoint(new Point());
                            pt.X -= 150;
                            pt.Y -= 50;
                            MainPage.Instance.DisplayDocument(WebDoc, pt);
                        }
                    }
                    else
                    {
                        var theDoc = ContentController.GetController<DocumentController>(target);
                        if (theDoc != DBTest.DBNull && theDoc != null)
                        {
                            var pt = this.TransformToVisual(MainPage.Instance).TransformPoint(new Point());
                            pt.X -= 150;
                            pt.Y -= 50;
                            MainPage.Instance.DisplayDocument(theDoc, pt);
                        }
                    }
                }
            }
            LastS1 = s1;
            LastS2 = s2;
        }
    }
}
