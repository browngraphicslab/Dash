using Dash.Controllers.Operators;
using DashShared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Dash.Controllers;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.VisualBasic;
using Visibility = Windows.UI.Xaml.Visibility;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class RichTextView : UserControl
    {
        public static bool HasFocus;
        ObservableCollection<FontFamily> fonts = new ObservableCollection<FontFamily>();

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(RichTextFieldModel.RTD), typeof(RichTextView), new PropertyMetadata(default(RichTextFieldModel.RTD)));
        

        public RichTextFieldModel.RTD Text
        {
            get { return (RichTextFieldModel.RTD)GetValue(TextProperty); }
            set{ SetValue(TextProperty, value); }
        }

        public RichTextFieldModelController  TargetRTFController = null;
        public ReferenceFieldModelController TargetFieldReference = null;
        public Context                       TargetDocContext = null;
        private Brush _buttonBackground;
        private Brush _highlightedButtonBackgroud;

        private ITextParagraphFormat defaultParFormat;
        private ITextCharacterFormat defaultCharFormat;

        private Dictionary<string, ParagraphAlignment> alignments = new Dictionary<string, ParagraphAlignment>();
        private Dictionary<string, MarkerType> markers = new Dictionary<string, MarkerType>();
        private Dictionary<string, MarkerStyle> markerStyles = new Dictionary<string, MarkerStyle>();
        private Dictionary<string, MarkerAlignment> markerAlignments = new Dictionary<string, MarkerAlignment>();
        public RichTextView()
        {
            this.InitializeComponent();
            Loaded   += OnLoaded;
            Unloaded += UnLoaded;

            //this.AddFonts();
            //this.AddColors();
            //_buttonBackground = xBoldButton.Background;
            //_highlightedButtonBackgroud = xRichEditBox.SelectionHighlightColor;
            defaultCharFormat = xRichEditBox.Document.Selection.CharacterFormat.GetClone();
            defaultParFormat = xRichEditBox.Document.Selection.ParagraphFormat.GetClone();
            SetUpEnumDictionaries();
            AddSearchBoxHandlers();
            TextChangedCallbackToken = RegisterPropertyChangedCallback(TextProperty, TextChangedCallback);
        }
        long TextChangedCallbackToken;

        private void SetUpEnumDictionaries()
        {
            var keys = Enum.GetNames(typeof(ParagraphAlignment));
            var vals = Enum.GetValues(typeof(ParagraphAlignment));
            var length = keys.Length;
            for (int i = 0; i < length; i++)
            {
                if (keys[i].Equals("Undefined"))
                    keys[i] = "Clear Alignment";
                alignments.Add(keys[i], (ParagraphAlignment)vals.GetValue(i));
            }
            keys = Enum.GetNames(typeof(MarkerType));
            vals = Enum.GetValues(typeof(MarkerType));
            length = keys.Length;
            for (int i = 0; i < length; i++)
            {
                if (keys[i].Equals("Undefined"))
                    keys[i] = "Clear Marker";
                markers.Add(keys[i], (MarkerType)vals.GetValue(i));
            }
            markers.Remove("None");
            keys = Enum.GetNames(typeof(MarkerStyle));
            vals = Enum.GetValues(typeof(MarkerStyle));
            length = keys.Length;
            for (int i = 0; i < length; i++)
            {
                if (vals.GetValue(i).Equals(Windows.UI.Text.MarkerStyle.Undefined))
                    keys[i] = "Clear Style";
                markerStyles.Add(keys[i], (MarkerStyle)vals.GetValue(i));
            }
            keys = Enum.GetNames(typeof(MarkerAlignment));
            vals = Enum.GetValues(typeof(MarkerAlignment));
            length = keys.Length;
            for (int i = 0; i < length; i++)
            {
                if (keys[i].Equals("Undefined"))
                    keys[i] = "Reset Alignment";
                else
                    keys[i] = string.Concat("List Align ", keys[i]);
                markerAlignments.Add(keys[i], (MarkerAlignment)vals.GetValue(i));
            }
        }

        //private Dictionary<string, Type> SetUpDictionary(Type type)
        //{
        //    var dictionary = new Dictionary<string, Type>();
        //    var keys = Enum.GetNames(type);
        //    var vals = Enum.GetValues(type);
        //    var length = keys.Length;
        //    for (int i = 0; i < length; i++)
        //    {
        //        dictionary.Add(keys[i], (Type)vals.GetValue(i));
        //    }
        //    return dictionary;
        //}
        private void TextChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            xRichEditBox.Document.SetText(TextSetOptions.FormatRtf, Text.RtfFormatString);
            var selected = GetSelected();
            if (selected != null)
            {
                xRichEditBox.Document.Selection.FindText(selected, 100000, FindOptions.None);

                this.xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = Colors.Yellow;
                this.xRichEditBox.Document.Selection.CharacterFormat.Bold = FormatEffect.On;
                UpdateDocument();
            }
            xRichEditBox.Document.Selection.SetRange(LastS1, LastS2);
        }
        private void UnLoaded(object sender, RoutedEventArgs e)
        {
            xRichEditBox.TextChanged  -= xRichEditBoxOnTextChanged;
        }

        RichTextFieldModel.RTD GetText()
        {
            if (TargetRTFController != null)
                return TargetRTFController.Data;
            return TargetFieldReference?.Dereference(TargetDocContext)?.GetValue(TargetDocContext) as RichTextFieldModel.RTD;
        }

        string GetSelected()
        {
            var parentDoc =  this.GetFirstAncestorOfType<DocumentView>();
            if (parentDoc != null)
            {
                return parentDoc.ViewModel?.DocumentController?.GetDataDocument(null).GetDereferencedField<TextFieldModelController>(DBFilterOperatorFieldModelController.SelectedKey, null)?.Data ??
                       parentDoc.ViewModel?.DocumentController?.GetActiveLayout(null)?.Data?.GetDereferencedField<TextFieldModelController>(DBFilterOperatorFieldModelController.SelectedKey, null)?.Data;
            }
            return null;
        }

        DocumentController GetDoc()
        {
            var parentDoc = this.GetFirstAncestorOfType<DocumentView>();
            if (parentDoc != null)
            {
                var doc =  parentDoc.ViewModel.DocumentController;
                return doc.GetActiveLayout()?.Data ?? doc;
            }
            return null;
        }

        private async Task<string> LoadText()
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/rtf.txt"));
            return await FileIO.ReadTextAsync(file);
        }

        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            UnLoaded(sender, routedEventArgs); // make sure we're not adding handlers twice
            
            if (GetText() != null)
                xRichEditBox.Document.SetText(TextSetOptions.FormatRtf, GetText().RtfFormatString);
        
            
            xRichEditBox.TextChanged += xRichEditBoxOnTextChanged;
        }

        // freezes the app
        private void xRichEditBoxOnTextChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            string allText;
            xRichEditBox.Document.GetText(TextGetOptions.UseObjectText, out allText);

            var startPt = new Point();
            var s1 = this.xRichEditBox.Document.Selection.StartPosition;
            var s2 = this.xRichEditBox.Document.Selection.EndPosition;
            this.xRichEditBox.Document.Selection.GetPoint(HorizontalCharacterAlignment.Center, VerticalCharacterAlignment.Baseline, PointOptions.Start, out startPt);

            // try to get last typed character based on the current selection position 
            this.xRichEditBox.Document.Selection.SetRange(Math.Max(0, s1 - 1), s1);
            string lastTypedCharacter;
            this.xRichEditBox.Document.Selection.GetText(TextGetOptions.None, out lastTypedCharacter);

            // if the last lastTypedCharacter is white space, then we check to see if it terminates a hyperlink
            if (lastTypedCharacter == " " || lastTypedCharacter == "\r" || lastTypedCharacter == "^")
            {
                // search through all the text for the nearest '@' indicating the start of a possible hyperlink
                int atPos = findPreviousHyperlinkStartMarker(allText, s1);

                // we found the nearest '@'
                if (atPos != -1)
                {
                    // get the text between the '@' and the current input position 
                    var refText = getHyperlinkText(atPos, s2);

                    if (!refText.StartsWith("HYPERLINK")) // @HYPERLINK means we've already created the hyperlink
                    {
                        // see if we can find a document whose primary keys match the text
                        var theDoc = findHyperlinkTarget(lastTypedCharacter == "^", refText);

                        createRTFHyperlink(theDoc, startPt, ref s1, ref s2, lastTypedCharacter == "^");
                    }
                }
            }

            if (allText.TrimEnd('\r') != GetText()?.ReadableString?.TrimEnd('\r'))
            {
                string allRtfText;
                xRichEditBox.Document.GetText(TextGetOptions.FormatRtf, out allRtfText);
                UnregisterPropertyChangedCallback(TextProperty, TextChangedCallbackToken);
                Text = new RichTextFieldModel.RTD(allText, allRtfText.Replace("\\pard\\tx720\\par", ""));  // RTF editor adds a trailing extra paragraph when queried -- need to strip that off
                TextChangedCallbackToken = RegisterPropertyChangedCallback(TextProperty, TextChangedCallback);
            }
            this.xRichEditBox.Document.Selection.SetRange(s1, s2);
        }

        static DocumentController findHyperlinkTarget(bool createIfNeeded, string refText)
        {
            var primaryKeys = refText.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var theDoc = DocumentController.FindDocMatchingPrimaryKeys(new List<string>(primaryKeys));
            if (theDoc == null && createIfNeeded)
            {
                if (refText.StartsWith("http"))
                {
                     theDoc = DBTest.CreateWebPage(refText);
                }
                else if (primaryKeys.Count() == 2 && primaryKeys[0] == "Filter")
                {
                    //theDoc = DBFilterOperatorFieldModelController.CreateFilter(new DocumentReferenceFieldController(DBTest.DBDoc.GetId(), KeyStore.DataKey), primaryKeys.Last());
                }
                else
                {
                    theDoc = new NoteDocuments.RichTextNote(NoteDocuments.PostitNote.DocumentType).Document;
                    theDoc.GetDataDocument(null).SetField(KeyStore.TitleKey, new TextFieldModelController(refText), true);
                }
            }

            return theDoc;
        }

        void createRTFHyperlink(DocumentController theDoc, Point startPt, ref int s1, ref int s2, bool createIfNeeded)
        {
            if (theDoc != null && this.xRichEditBox.Document.Selection.StartPosition != this.xRichEditBox.Document.Selection.EndPosition && 
                this.xRichEditBox.Document.Selection.Link != "\"" + theDoc.GetId() + "\"")
            {
                // set the hyperlink for the matched text
                this.xRichEditBox.Document.Selection.Link = "\"" + theDoc.GetId() + "\"";
                // advance the end selection past the RTF embedded HYPERLINK keyword
                s2 += this.xRichEditBox.Document.Selection.Link.Length + "HYPERLINK".Length + 1;
                s1 = s2;
                this.xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = Colors.LightCyan;
                this.xRichEditBox.Document.Selection.SetPoint(startPt, PointOptions.Start, true);
            }
        }
        

        string getHyperlinkText(int atPos, int s2)
        {
            this.xRichEditBox.Document.Selection.SetRange(atPos + 1, s2 - 1);
            string refText;
            this.xRichEditBox.Document.Selection.GetText(TextGetOptions.None, out refText);

            return refText;
        }

        int findPreviousHyperlinkStartMarker(string allText, int s1)
        {
            this.xRichEditBox.Document.Selection.SetRange(0, allText.Length);
            var atPos = -1;
            while (this.xRichEditBox.Document.Selection.FindText("@", 0, FindOptions.None) > 0)
            {
                if (this.xRichEditBox.Document.Selection.StartPosition < s1)
                {
                    atPos = this.xRichEditBox.Document.Selection.StartPosition;
                    this.xRichEditBox.Document.Selection.SetRange(atPos + 1, allText.Length);
                }
                else break;
            }

            return atPos;
        }

        private void AddSearchBoxHandlers()
        {
            xSearchDelete.Tapped += delegate { xSearchBoxPanel.Visibility = Visibility.Collapsed; };
            xSearchBox.LostFocus += delegate { xSearchBoxPanel.Opacity = 0.5; };
            xSearchBox.GotFocus += delegate { xSearchBoxPanel.Opacity = 1; };
            xSearchBox.QueryChanged += XSearchBox_OnQueryChanged;
        }

        private string currentFont;
        private void AddFonts()
        {
            var FontNames = new List<string>()
            {
                "Arial",
                "Calibri",
                "Cambria",
                "Cambria Math",
                "Comic Sans MS",
                "Courier New",
                "Ebrima",
                "Gadugi",
                "Georgia",
                "Javanese Text Regular Fallback font for Javanese script",
                "Leelawadee UI",
                "Lucida Console",
                "Malgun Gothic",
                "Microsoft Himalaya",
                "Microsoft JhengHei",
                "Microsoft JhengHei UI",
                "Microsoft New Tai Lue",
                "Microsoft PhagsPa",
                "Microsoft Tai Le",
                "Microsoft YaHei",
                "Microsoft YaHei UI",
                "Microsoft Yi Baiti",
                "Mongolian Baiti",
                "MV Boli",
                "Myanmar Text",
                "Nirmala UI",
                "Segoe MDL2 Assets",
                "Segoe Print",
                "Segoe UI",
                "Segoe UI Emoji",
                "Segoe UI Historic",
                "Segoe UI Symbol",
                "SimSun",
                "Times New Roman",
                "Trebuchet MS",
                "Verdana",
                "Webdings",
                "Wingdings",
                "Yu Gothic",
                "Yu Gothic UI"
            };
            foreach (var font in FontNames)
            {
                fonts.Add(new FontFamily(font));
                var item = new MenuFlyoutItem();
                item.Text = font;
                item.Foreground = new SolidColorBrush(Colors.White);
                item.Click += delegate
                {
                    //currentFont = font;
                    currentCharFormat = xRichEditBox.Document.Selection.CharacterFormat.GetClone();
                    UpdateDocument();
                };
                item.GotFocus += delegate
                {
                    xRichEditBox.Document.Selection.CharacterFormat.Name = font;
                };
                xFont?.Items?.Add(item);
            }
        }

        private Color currentForeground;
        private void AddColors(MenuFlyoutSubItem item)
        {
            AddColorRange(Colors.Red, Colors.Violet, item);
            AddColorRange(Colors.Violet, Colors.Blue, item);
            AddColorRange(Colors.Blue, Colors.Aqua, item);
            AddColorRange(Colors.Aqua, Colors.Green, item);
            AddColorRange(Colors.Green, Colors.Yellow, item);
            item?.Items?.Add(new MenuFlyoutSeparator());
            AddColorRange(Colors.White, Colors.Black, item);
        }

        private void AddColorRange(Color color1, Color color2, MenuFlyoutSubItem item)
        {
            int r1 = color1.R;
            int rEnd = color2.R;
            int b1 = color1.B;
            int bEnd = color2.B;
            int g1 = color1.G;
            int gEnd = color2.G;
            for (byte i = 0; i < 25; i++)
            {
                var rAverage = r1 + (int)((rEnd - r1) * i / 25);
                var gAverage = g1 + (int)((gEnd - g1) * i / 25);
                var bAverage = b1 + (int)((bEnd - b1) * i / 25);
                AddColorMenuItem(Color.FromArgb(255, (byte)rAverage, (byte)gAverage, (byte)bAverage), item, 2);
            }
        }

        private void AddColorMenuItem(Color color, MenuFlyoutSubItem submenu, double height)
        {
            var item = new MenuFlyoutItem();
            item.Background = new SolidColorBrush(color);
            item.Height = height;
            if (submenu == xColor)
            {
                item.Click += delegate
                {
                    UpdateDocument();
                    currentCharFormat = null;
                };
                item.GotFocus += delegate { Foreground(color, false); };
            }
            else if (submenu == xHighlight)
            {
                item.Click += delegate
                {
                    UpdateDocument();
                    currentCharFormat = null;
                };
                item.GotFocus += delegate { Highlight(color, false); };
            }

            submenu?.Items?.Add(item);
        }

        private void AddCharacterFormats()
        {
            var basics = new List<string>() {"Bold (ctrl+B)", "Italics (ctrl+I)", "Underline (ctrl+U)", "Strikethrough"};
            var scripts = new List<string>() {"Superscript", "Subscript" };
            var caps = new List<string>() {"AllCaps", "SmallCaps"};
            foreach (var basic in basics)
            {
                AddFormatMenuItem(basic, xFormat, true);
            }
            foreach (var script in scripts)
            {
                AddFormatMenuItem(script, xScript, true);
            }
            foreach (var cap in caps)
            {
                AddFormatMenuItem(cap, xCaps, true);
            }
        }

        private void AddParagraphFormats()
        {
            //var alignments = new List<string>() {"Left", "Center", "Right", "Justify", "Reset"};
            var alignmentNames = alignments.Keys;
            foreach (var alignment in alignmentNames)
            {
                AddFormatMenuItem(alignment, xAlignment, false);
            }
            var listTypes = markers.Keys;
            foreach (var type in listTypes)
            {
                AddFormatMenuItem(type, xListTypes, false);
            }
            var listStyles = markerStyles.Keys;
            foreach (var style in listStyles)
            {
                AddFormatMenuItem(style, xListStyles, false);
            }
            var listAlignments = markerAlignments.Keys;
            foreach (var listAlignment in listAlignments)
            {
                AddFormatMenuItem(listAlignment, xListAlignments, false);
            }
        }

        private void AddFormatMenuItem(string menuText, MenuFlyoutSubItem subMenu, bool charFormat)
        {
            var item = new MenuFlyoutItem();
            item.Text = menuText;
            item.Foreground = new SolidColorBrush(Colors.White);
            item.Click += Format_OnClick;
            if (charFormat)
            {
                item.GotFocus += CharFormat_OnGotFocus;
            }
            else
            {
                item.GotFocus += ParFormat_OnGotFocus;
            }
            subMenu?.Items?.Add(item);
        }

        private void Bold(bool updateDocument)
        {
            if (this.xRichEditBox.Document.Selection.CharacterFormat.Bold == FormatEffect.On)
            {
                this.xRichEditBox.Document.Selection.CharacterFormat.Bold = FormatEffect.Off;
            }
            else
            {
                this.xRichEditBox.Document.Selection.CharacterFormat.Bold = FormatEffect.On;
            }
            //var selected = GetSelected();
            //if (selected != null)
            //{
            //    xRichEditBox.Document.Selection.FindText(selected, 100000, FindOptions.None);

            //    this.xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = Colors.Yellow;
            //    this.xRichEditBox.Document.Selection.CharacterFormat.Bold = FormatEffect.On;
            //    UpdateDocument();
            //}
            //this.xRichEditBox.Document.Selection.CharacterFormat.Bold = FormatEffect.Toggle;
            if(updateDocument) UpdateDocument();
        }

        private void Italicize(bool updateDocument)
        {
            if (this.xRichEditBox.Document.Selection.CharacterFormat.Italic == FormatEffect.On)
            {
                this.xRichEditBox.Document.Selection.CharacterFormat.Italic = FormatEffect.Off;
            }
            else
            {
                this.xRichEditBox.Document.Selection.CharacterFormat.Italic = FormatEffect.On;
            }
            //this.xRichEditBox.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;
            if (updateDocument) UpdateDocument();
        }

        private void Underline(bool updateDocument)
        {
            if (this.xRichEditBox.Document.Selection.CharacterFormat.Underline == UnderlineType.None)
                this.xRichEditBox.Document.Selection.CharacterFormat.Underline = UnderlineType.Single;
            else
                this.xRichEditBox.Document.Selection.CharacterFormat.Underline = UnderlineType.None;
            if (updateDocument) UpdateDocument();
        }

        private void Strikethrough(bool updateDocument)
        {
            if (xRichEditBox.Document.Selection.CharacterFormat.Strikethrough == FormatEffect.On)
                xRichEditBox.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.Off;
            else
                xRichEditBox.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.On;
            if (updateDocument) UpdateDocument();
        }

        private void Superscript(bool updateDocument)
        {
            if (xRichEditBox.Document.Selection.CharacterFormat.Superscript == FormatEffect.On)
                xRichEditBox.Document.Selection.CharacterFormat.Superscript = FormatEffect.Off;
            else
                xRichEditBox.Document.Selection.CharacterFormat.Superscript = FormatEffect.On;
            if (updateDocument) UpdateDocument();
        }

        private void Subscript(bool updateDocument)
        {
            if (xRichEditBox.Document.Selection.CharacterFormat.Subscript == FormatEffect.On)
                xRichEditBox.Document.Selection.CharacterFormat.Subscript = FormatEffect.Off;
            else
                xRichEditBox.Document.Selection.CharacterFormat.Subscript = FormatEffect.On;
            if (updateDocument) UpdateDocument();
        }

        private void TextScript(TextScript script, bool updateDocument)
        {
            xRichEditBox.Document.Selection.CharacterFormat.TextScript = script;
            if(updateDocument) UpdateDocument();
        }

        private void SmallCaps(bool updateDocument)
        {
            if (xRichEditBox.Document.Selection.CharacterFormat.SmallCaps == FormatEffect.On)
                xRichEditBox.Document.Selection.CharacterFormat.SmallCaps = FormatEffect.Off;
            else
                xRichEditBox.Document.Selection.CharacterFormat.SmallCaps = FormatEffect.On;
            if (updateDocument) UpdateDocument();
        }

        private void AllCaps(bool updateDocument)
        {
            if (xRichEditBox.Document.Selection.CharacterFormat.AllCaps == FormatEffect.On)
                xRichEditBox.Document.Selection.CharacterFormat.AllCaps = FormatEffect.Off;
            else
                xRichEditBox.Document.Selection.CharacterFormat.AllCaps = FormatEffect.On;
            if (updateDocument) UpdateDocument();
        }

        private void CharFormat(String menuText, bool updateDocument)
        {
            if (menuText == "BolBold (ctrl+B)")
            {
                Bold(updateDocument);
            }
            else if (menuText == "Italics (ctrl+I)")
            {
                Italicize(updateDocument);
            }
            else if (menuText == "Underline (ctrl+U)")
            {
                Underline(updateDocument);
            } else if (menuText == "Strikethrough")
            {
                Strikethrough(updateDocument);
            } else if (menuText == "Superscript")
            {
                Superscript(updateDocument);
            } else if (menuText == "Subscript")
            {
                Subscript(updateDocument);
            } else if (menuText == "AllCaps")
            {
                AllCaps(updateDocument);
            } else if (menuText == "SmallCaps")
            {
                SmallCaps(updateDocument);
            }
        }

        private void ParFormat(String menuText, bool updateDocument)
        {
            ParagraphAlignment alignment;
            if(alignments.TryGetValue(menuText, out alignment))
                Alignment(alignment, updateDocument);

            MarkerType listType;
            if(markers.TryGetValue(menuText, out listType))
                Marker(listType, updateDocument);

            MarkerStyle listStyle;
            if(markerStyles.TryGetValue(menuText, out listStyle))
                MarkerStyle(listStyle, updateDocument);

            MarkerAlignment listAlignment;
            if (markerAlignments.TryGetValue(menuText, out listAlignment))
                MarkerAlignment(listAlignment, updateDocument);
        }

        private void Highlight(Color background, bool updateDocument)
        {
            xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = background;
            if (updateDocument) UpdateDocument();
        }

        private void Foreground(Color color, bool updateDocument)
        {
            xRichEditBox.Document.Selection.CharacterFormat.ForegroundColor = color;
            if (updateDocument) UpdateDocument();
        }
  
        private void Alignment(object alignment, bool updateDocument)
        {
            if (alignment != null && alignment.GetType() == typeof(ParagraphAlignment))
            {
                xRichEditBox.Document.Selection.ParagraphFormat.Alignment = (ParagraphAlignment)alignment;
                if (updateDocument) UpdateDocument();
            }
        }

        private void Marker(object type, bool updateDocument)
        {
            if (type != null && type.GetType() == typeof(MarkerType))
            {
                xRichEditBox.Document.Selection.ParagraphFormat.ListType = (MarkerType)type;
                if (updateDocument) UpdateDocument();
            }
        }

        private void MarkerStyle(object type, bool updateDocument)
        {
            if (type != null && type.GetType() == typeof(MarkerStyle))
            {
                xRichEditBox.Document.Selection.ParagraphFormat.ListStyle = (MarkerStyle)type;
                if (updateDocument) UpdateDocument();
            }
        }

        private void MarkerAlignment(object type, bool updateDocument)
        {
            if (type != null && type.GetType() == typeof(MarkerAlignment))
            {
                xRichEditBox.Document.Selection.ParagraphFormat.ListAlignment = (MarkerAlignment)type;
                if (updateDocument) UpdateDocument();
            }
        }

        //private void BoldButton_Tapped(object sender, TappedRoutedEventArgs e)
        //{
        //    Bold(true);
        //}
        //private void ItalicButton_Tapped(object sender, TappedRoutedEventArgs e)
        //{
        //    Italicize(true);
        //}

        //private void UnderlineButton_Tapped(object sender, TappedRoutedEventArgs e)
        //{
        //    Underline(true);
        //}
        //private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    this.xRichEditBox.FontFamily = fonts[xFontComboBox.SelectedIndex];
        //}
        //private void Grid_GotFocus(object sender, RoutedEventArgs e)
        //{

        //    //xFormatRow.Height = new GridLength(30);
        //}

        //private void Grid_LostFocus(object sender, RoutedEventArgs e)
        //{
        //    xFormatRow.Height = new GridLength(0);
        //}
        void UpdateDocument()
        {
            string allText;
            xRichEditBox.Document.GetText(TextGetOptions.UseObjectText, out allText);
            string allRtfText;
            xRichEditBox.Document.GetText(TextGetOptions.FormatRtf, out allRtfText);
            UnregisterPropertyChangedCallback(TextProperty, TextChangedCallbackToken);
            Text = new RichTextFieldModel.RTD(allText, allRtfText.Replace("\\pard\\tx720\\par", ""));  // RTF editor adds a trailing extra paragraph when queried -- need to strip that off
            TextChangedCallbackToken = RegisterPropertyChangedCallback(TextProperty, TextChangedCallback);
        }

        private void xRichEditBox_GotFocus(object sender, RoutedEventArgs e)
        {
            //xFormatRow.Height = new GridLength(30);
            HasFocus = true;
            FlyoutBase.GetAttachedFlyout(xRichEditBox)?.Hide();
            UpdateDocument();
        }

        private void XRichEditBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            HasFocus = false;

        }

        private void xRichEditBox_Drop(object sender, DragEventArgs e)
        {
            DocumentController theDoc = null;
            if (e.DataView.Properties.ContainsKey("DocumentControllerList"))
            {
                var docCtrls = e.DataView.Properties["DocumentControllerList"] as List<DocumentController>;
                theDoc = docCtrls.First();
            }

            string allText;
            xRichEditBox.Document.GetText(TextGetOptions.UseObjectText, out allText);

            var startPt = new Point();
            var s1 = this.xRichEditBox.Document.Selection.StartPosition;
            var s2 = this.xRichEditBox.Document.Selection.EndPosition;
            this.xRichEditBox.Document.Selection.GetPoint(HorizontalCharacterAlignment.Center, VerticalCharacterAlignment.Baseline, PointOptions.Start, out startPt);

            createRTFHyperlink(theDoc, startPt, ref s1, ref s2, false);

            if (allText.TrimEnd('\r') != GetText()?.ReadableString?.TrimEnd('\r'))
            {
                string allRtfText;
                xRichEditBox.Document.GetText(TextGetOptions.FormatRtf, out allRtfText);
                UnregisterPropertyChangedCallback(TextProperty, TextChangedCallbackToken);
                Text = new RichTextFieldModel.RTD(allText, allRtfText.Replace("\\pard\\tx720\\par", ""));  // RTF editor adds a trailing extra paragraph when queried -- need to strip that off
                TextChangedCallbackToken = RegisterPropertyChangedCallback(TextProperty, TextChangedCallback);
            }
            this.xRichEditBox.Document.Selection.SetRange(s1, s2);
            e.Handled = true;
            if (DocumentView.DragDocumentView != null)
            {
                DocumentView.DragDocumentView.OuterGrid.BorderThickness = new Thickness(0);
                DocumentView.DragDocumentView.IsHitTestVisible = true;
                DocumentView.DragDocumentView = null;
            }
        }

        int LastS1 = 0, LastS2 = 0;
        void xRichEditBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var s1 = this.xRichEditBox.Document.Selection.StartPosition;
            var s2 = this.xRichEditBox.Document.Selection.EndPosition;
            if (LastS1 != s1 || LastS2 != s2)  // test if the selection has actually changed... seem to get in here when nothing has happened perhaps because of losing focus?
            {
                // If there's a Document hyperlink in the selection, then follow it.  This is a hack because
                // I don't seem to be able to get direct access to the hyperlink events in the rich edit box.
                if (this.xRichEditBox.Document.Selection.Link.Length > 1)
                {
                    var doc = GetDoc();
                    var point = doc.GetPositionField().Data;

                    var target = this.xRichEditBox.Document.Selection.Link.Split('\"')[1];
                    var theDoc = ContentController<DocumentModel>.GetController<DocumentController>(target);
                    if (theDoc != null && !theDoc.Equals(DBTest.DBNull))
                    {
                        var pt = point;
                        pt.X -= 150;
                        pt.Y -= 50;
                        MainPage.Instance.DisplayDocument(theDoc.GetViewCopy(pt));
                    }
                    else if (target.StartsWith("http"))
                    {
                        theDoc = DocumentController.FindDocMatchingPrimaryKeys(new string[] { target });
                        if (theDoc != null && theDoc != DBTest.DBNull)
                        {
                            var pt = point;
                            pt.X -= 150;
                            pt.Y -= 50;
                            MainPage.Instance.DisplayDocument(theDoc, pt);
                        }
                        else
                        {
                            var WebDoc = DBTest.CreateWebPage(target);
                            var pt = point;
                            pt.X -= 150;
                            pt.Y -= 50;
                            MainPage.Instance.DisplayDocument(WebDoc, pt);
                        }
                    }
                }
            }
            LastS1 = s1;
            LastS2 = s2;
            //this.HighlightFormatButtons();
        }

        //private void HighlightFormatButtons()
        //{
        //    var selection = xRichEditBox.Document.Selection;
        //    // for when cursor changes position
        //    if (selection.StartPosition == selection.EndPosition)
        //    {
        //        var initialPosition = selection.StartPosition;
        //        bool isBold = false;
        //        bool isItalics = false;
        //        bool isUnderlined = false;
        //        // check character in front of the cursor
        //        var previous = xRichEditBox.Document.GetRange(initialPosition - 1, initialPosition);
        //        if (previous.CharacterFormat.Bold == FormatEffect.On)
        //        {
        //            isBold = true;
        //        }
        //        if (selection.CharacterFormat.Italic == FormatEffect.On)
        //        {
        //            isItalics = true;
        //        }
        //        if (previous.CharacterFormat.Underline == UnderlineType.Single)
        //        {
        //            isUnderlined = true;
        //        }
        //        // check character after the cursor
        //        var next = xRichEditBox.Document.GetRange(initialPosition, initialPosition + 1);
        //        if (next.CharacterFormat.Bold == FormatEffect.Off)
        //        {
        //            isBold = false;
        //        }
        //        if (next.CharacterFormat.Italic == FormatEffect.Off)
        //        {
        //            isItalics = false;
        //        }
        //        if (next.CharacterFormat.Underline == UnderlineType.None)
        //        {
        //            isUnderlined = false;
        //        }
        //        // if both the character before and after the cursor has a certain format, highlight the button for that format
        //        xBoldButton.Background = isBold ? _highlightedButtonBackgroud : _buttonBackground;
        //        xItalicButton.Background = isItalics ? _highlightedButtonBackgroud : _buttonBackground;
        //        xUnderlineButton.Background = isUnderlined ? _highlightedButtonBackgroud : _buttonBackground;
        //    }
        //    // for when text is selected
        //    else
        //    {
        //        bool isBold = false;
        //        bool isItalics = false;
        //        bool isUnderlined = false;
        //        if (selection.CharacterFormat.Bold == FormatEffect.On)
        //        {
        //            isBold = true;
        //        }
        //        if (selection.CharacterFormat.Italic == FormatEffect.On)
        //        {
        //            isItalics = true;
        //        }
        //        if (selection.CharacterFormat.Underline == UnderlineType.Single)
        //        {
        //            isUnderlined = true;
        //        }
        //        xBoldButton.Background = isBold ? _highlightedButtonBackgroud : _buttonBackground;
        //        xItalicButton.Background = isItalics ? _highlightedButtonBackgroud : _buttonBackground;
        //        xUnderlineButton.Background = isUnderlined ? _highlightedButtonBackgroud : _buttonBackground;
        //    }
        //}


        private void XRichEditBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var ctrlState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var altState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Menu)
                .HasFlag(CoreVirtualKeyStates.Down);
            var tabState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Tab)
                .HasFlag(CoreVirtualKeyStates.Down);
            if (tabState)
            {
                xRichEditBox.Document.Selection.TypeText("\t");
                e.Handled = true;
            }
            if (ctrlState)
            {
                var selection = xRichEditBox.Document.Selection;
                if (e.Key.Equals(VirtualKey.B))
                {
                    Bold(true);
                } else if (e.Key.Equals(VirtualKey.I))
                {
                    Italicize(true);
                } else if (e.Key.Equals(VirtualKey.U))
                {
                    Underline(true);
                } 
                else if (e.Key.Equals(VirtualKey.F))
                {
                    xSearchBoxPanel.Visibility = Visibility.Visible;
                    xSearchBox.Focus(FocusState.Programmatic);
                } else if (e.Key.Equals(VirtualKey.N))
                {
                    xRichEditBox.Document.Redo();
                } else if (e.Key.Equals(VirtualKey.H))
                {
                    Highlight(Colors.Yellow, true);
                    UpdateDocument();
                } else if (e.Key.Equals(VirtualKey.O))
                {
                    OpenContextMenu(sender);
                }
            }
            if (altState)
            {
                OpenContextMenu(sender);
            }
        }

        private void XSearchBox_OnQueryChanged(SearchBox sender, SearchBoxQueryChangedEventArgs args)
        {
            var query = args.QueryText;
            //var s1 = xRichEditBox.Document.Selection.StartPosition;
            //var s2 = xRichEditBox.Document.Selection.EndPosition;
                // temporary, find way to find end of file
            string text;
            xRichEditBox.Document.GetText(TextGetOptions.None, out text);
            var length = text.Length;
            if (xRichEditBox.Document.Selection.StartPosition == xRichEditBox.Document.Selection.EndPosition)
            {
                xRichEditBox.Document.Selection.SetRange(0, length);
            }
            //int i = 1;
            //while (i > 0)
            //{
            //    i = xRichEditBox.Document.Selection.FindText(query, length, FindOptions.None);
            //    var selectedText = xRichEditBox.Document.Selection;
            //    if (selectedText != null)
            //    {
            //        selectedText.CharacterFormat.BackgroundColor = Colors.Yellow;
            //    }
            //}
            var found = xRichEditBox.Document.GetRange(0, length).FindText(query, length, FindOptions.None);
            var start = xRichEditBox.Document.Selection.StartPosition;
            var end = xRichEditBox.Document.Selection.StartPosition;
            //xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = Colors.Yellow;
        }

        private void OpenContextMenu(object sender)
        {
            var element = sender as FrameworkElement;
            if (element != null)
            {
                FlyoutBase.ShowAttachedFlyout(element);
                FlyoutBase.GetAttachedFlyout(element)?.ShowAt(element);
            }
            currentCharFormat = xRichEditBox.Document.Selection.CharacterFormat.GetClone();
        }

        private void Format_OnClick(object sender, RoutedEventArgs e)
        {
            UpdateDocument();
            currentCharFormat = null;
            currentParagraphFormat = null;
        }

        private void XRichEditBox_OnHolding(object sender, HoldingRoutedEventArgs e)
        {
            OpenContextMenu(sender);
            FlyoutBase.GetAttachedFlyout(xRichEditBox).AllowFocusOnInteraction = true;
            if (xFont.Items.Count == 0) AddFonts();
            if (xColor.Items.Count == 0) AddColors(xColor);
            if (xHighlight.Items.Count == 0) AddColors(xHighlight);
        }

        private void FontGroup_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (xFont.Items.Count == 0) AddFonts();
            if (currentCharFormat != null) xRichEditBox.Document.Selection.CharacterFormat.SetClone(currentCharFormat);
        }

        private Color currentHighlight = Colors.Transparent;
        private void HighlightGroup_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (xHighlight.Items.Count == 0) AddColors(xHighlight);
            if (currentCharFormat != null) xRichEditBox.Document.Selection.CharacterFormat.SetClone(currentCharFormat);
        }

        private void ColorGroup_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (xColor.Items.Count == 0) AddColors(xColor);
            if (currentCharFormat != null) xRichEditBox.Document.Selection.CharacterFormat.SetClone(currentCharFormat);
        }

        private ITextCharacterFormat currentCharFormat;
        private void FormatGroup_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if(xFormat.Items.Count == 2) AddCharacterFormats();
            if(currentCharFormat != null) xRichEditBox.Document.Selection.CharacterFormat.SetClone(currentCharFormat);
        }

        private void CharFormat_OnGotFocus(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as MenuFlyoutItem;
            var menuText = menuFlyoutItem?.Text;
            if(currentCharFormat != null) xRichEditBox.Document.Selection.CharacterFormat.SetClone(currentCharFormat);
            CharFormat(menuText, false);
        }

        private void ParFormat_OnGotFocus(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as MenuFlyoutItem;
            var menuText = menuFlyoutItem?.Text;
            if (currentParagraphFormat != null) xRichEditBox.Document.Selection.CharacterFormat.SetClone(currentCharFormat);
            ParFormat(menuText, false);
        }

        private ITextParagraphFormat currentParagraphFormat;
        private void ParagraphGroup_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (xAlignment.Items.Count == 0) AddParagraphFormats();
            if(currentParagraphFormat != null) xRichEditBox.Document.Selection.ParagraphFormat.SetClone(currentParagraphFormat);
        }

        private void XResetAll_OnClick(object sender, RoutedEventArgs e)
        {
            xRichEditBox.Document.Selection.CharacterFormat.SetClone(defaultCharFormat);
            xRichEditBox.Document.Selection.ParagraphFormat.SetClone(defaultParFormat);
        }
    }
}

