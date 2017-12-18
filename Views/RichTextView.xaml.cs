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
using System.ComponentModel;
using System.Globalization;
using Windows.UI.Xaml.Documents;
using Flurl.Util;
using DashShared.Models;
using static Dash.NoteDocuments;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using System.Diagnostics;
using Windows.System;
using Windows.ApplicationModel.DataTransfer;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class RichTextView : UserControl
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(RichTextModel.RTD), typeof(RichTextView), new PropertyMetadata(default(RichTextModel.RTD)));
        

        public RichTextModel.RTD Text
        {
            get { return (RichTextModel.RTD)GetValue(TextProperty); }
            set{ SetValue(TextProperty, value); }
        }

        public RichTextController  TargetRTFController = null;
        public ReferenceController TargetFieldReference = null;
        public Context                       TargetDocContext = null;

        /// <summary>
        /// Default rich text paragraph format (text alignment, list, spacing... ect.)
        /// </summary>
        private ITextParagraphFormat defaultParFormat;
        /// <summary>
        /// Default rich text character format (bold, italics, underlin, script, caps.. ect.)
        /// </summary>
        private ITextCharacterFormat defaultCharFormat;

        /// <summary>
        /// A dictionary mapping names of text alignments to their corresponding ParagraphAlignment enums
        /// </summary>
        private IDictionary<string, ParagraphAlignment> alignments;
        /// <summary>
        /// A dictionary mapping names of list types to their coresponding MarkerType enums
        /// </summary>
        private IDictionary<string, MarkerType> markerTypes;
        /// <summary>
        /// A dictionary mapping names of list styles to their correspoding MarkerStyle enums
        /// </summary>
        private IDictionary<string, MarkerStyle> markerStyles;
        /// <summary>
        /// A dictionary mapping names of list alignmetns to their correspoding MarkerAlignment enums
        /// </summary>
        private IDictionary<string, MarkerAlignment> markerAlignments;

        /// <summary>
        /// Instance of a class made for word count and font size binding
        /// </summary>
        public WordCount WC;

        /// <summary>
        /// Keeps track of whether or not the flyout is opened
        /// </summary>
        private bool isFlyoutOpen = false;

        public static bool HasFocus = false;

        private SolidColorBrush highlightNotFocused = new SolidColorBrush(Colors.Gray) {Opacity=0.5};
        public RichTextView()
        {
            this.InitializeComponent();
            Loaded   += OnLoaded;
            Unloaded += UnLoaded;

            // store a clone of character format after initialization as default format
            defaultCharFormat = xRichEditBox.Document.Selection.CharacterFormat.GetClone();
            // store a clone of paragraph format after initialization as default format
            defaultParFormat = xRichEditBox.Document.Selection.ParagraphFormat.GetClone();
            WC = new WordCount(xRichEditBox);

            TextChangedCallbackToken = RegisterPropertyChangedCallback(TextProperty, TextChangedCallback);
        }
        long TextChangedCallbackToken;

        /// <summary>
        /// Binds the font size of the current selection to the text property of the xFontSizeTextBox
        /// </summary>
        private void SetFontSizeBinding()
        {
            var fontSizeBinding = new Binding()
            {
                Source = WC,
                Path = new PropertyPath(nameof(WC.Size)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            xFontSizeTextBox.SetBinding(TextBox.TextProperty, fontSizeBinding);
        }

        /// <summary>
        /// Sets up all format dictionaries (for creating the flyout menu for format options)
        /// </summary>
        private void SetUpEnumDictionaries()
        {
            SetUpDictionary<ParagraphAlignment>(typeof(ParagraphAlignment), out alignments);
            SetUpDictionary<MarkerType>(typeof(MarkerType), out markerTypes);
            SetUpDictionary<MarkerStyle>(typeof(MarkerStyle), out markerStyles);
            SetUpDictionary<MarkerAlignment>(typeof(MarkerAlignment), out markerAlignments);
        }

        /// <summary>
        /// Sets up an enum dictionary where the name of the enum is the key, and the enum itself
        /// is the value
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="type"></param>
        /// <param name="dict"></param>
        private void SetUpDictionary<TValue>(Type type, out IDictionary<string,TValue> dict)
        {
            dict = new Dictionary<string, TValue>();
            var keys = Enum.GetNames(type);
            var vals = Enum.GetValues(type);
            var length = keys.Length;
            for (int i = 0; i < length; i++)
            {
                var key = keys[i];
                if (key != "Undefined") 
                    dict.Add(key, (TValue)vals.GetValue(i));
            }
        }

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

        /// <summary>
        /// Unsubscribes TextChanged handler on Unload
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnLoaded(object sender, RoutedEventArgs e)
        {
            xRichEditBox.KeyUp -= XRichEditBox_KeyUp;
            MainPage.Instance.RemoveHandler(PointerReleasedEvent, new PointerEventHandler(released));
        }

        RichTextModel.RTD GetText()
        {
            if (TargetRTFController != null)
                return TargetRTFController.Data;
            return TargetFieldReference?.Dereference(TargetDocContext)?.GetValue(TargetDocContext) as RichTextModel.RTD;
        }

        string GetSelected()
        {
            var parentDoc =  this.GetFirstAncestorOfType<DocumentView>();
            if (parentDoc != null)
            {
                return parentDoc.ViewModel?.DocumentController?.GetDataDocument(null).GetDereferencedField<TextController>(DBFilterOperatorController.SelectedKey, null)?.Data ??
                       parentDoc.ViewModel?.DocumentController?.GetActiveLayout(null)?.GetDereferencedField<TextController>(DBFilterOperatorController.SelectedKey, null)?.Data;
            }
            return null;
        }

        DocumentController GetDoc()
        {
            var parentDoc = this.GetFirstAncestorOfType<DocumentView>();
            if (parentDoc != null)
            {
                var doc =  parentDoc.ViewModel.DocumentController;
                return doc.GetActiveLayout() ?? doc;
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
        
            
            xRichEditBox.SelectionHighlightColorWhenNotFocused = highlightNotFocused;
            // Add handlers and and bindings to set up rich text formatting functionalities 
            SetUpEnumDictionaries();
            AddSearchBoxHandlers();
            AddWordCountHandlers();
            AddFontSizeHandlers();
            AddFlyoutHandlers();
            SetFontSizeBinding();

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
            {
                if (GetText() != null)
                    xRichEditBox.Document.SetText(TextSetOptions.FormatRtf, GetText().RtfFormatString);
            });

            xRichEditBox.KeyUp += XRichEditBox_KeyUp;
            MainPage.Instance.AddHandler(PointerReleasedEvent, new PointerEventHandler(released), true);
            this.AddHandler(PointerPressedEvent, new PointerEventHandler(RichTextView_PointerPressed), true);
            this.AddHandler(PointerMovedEvent, new PointerEventHandler(RichTextView_PointerMoved), true);
            this.AddHandler(PointerReleasedEvent, new PointerEventHandler(RichTextView_PointerReleased), true);
            this.AddHandler(TappedEvent, new TappedEventHandler(tapped), true);
            this.xRichEditBox.ContextMenuOpening += XRichEditBox_ContextMenuOpening;
            xRichEditBox.TextChanged += xRichEditBoxOnTextChanged;
        }
        Tuple<Point, Point> HackToDragWithRightMouseButton = null;

        private void XRichEditBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            e.Handled = true;

            var parent = this.GetFirstAncestorOfType<DocumentView>();
            parent.OnTapped(null, null);
        }

        public string target = null;
        private bool _rightPressed = false;
        private void RichTextView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _rightPressed = e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
            if (_rightPressed)
            {
                var docView = this.GetFirstAncestorOfType<DocumentView>();
                docView?.ToFront();
                var down_and_offset = HackToDragWithRightMouseButton;
                var parent = this.GetFirstAncestorOfType<DocumentView>();
                var pointerPosition = MainPage.Instance.TransformToVisual(parent.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition);

                var rt = parent.RenderTransform.TransformPoint(new Point());
                HackToDragWithRightMouseButton = new Tuple<Point, Point>(pointerPosition, new Point(pointerPosition.X - rt.X, pointerPosition.Y - rt.Y));
                this.CapturePointer(e.Pointer);
            }
        }
        private void RichTextView_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed && HackToDragWithRightMouseButton != null)
            {
                var down_and_offset = HackToDragWithRightMouseButton;
                var down   = down_and_offset.Item1;
                var offset = down_and_offset.Item2;
                var parent = this.GetFirstAncestorOfType<DocumentView>();
                var pointerPosition = MainPage.Instance.TransformToVisual(parent.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition);

                parent.RenderTransform = new TranslateTransform() { X = pointerPosition.X - offset.X, Y = pointerPosition.Y - offset.Y };
            }
        }
        private void RichTextView_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var parent = this.GetFirstAncestorOfType<DocumentView>();
            if (parent != null)
                parent.MoveToContainingCollection();
            if (_rightPressed)
            {
                parent.OnTapped(sender, new TappedRoutedEventArgs());
            }
        }
        private void tapped(object sender, TappedRoutedEventArgs e)
        {
            if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
            {
                var s1 = this.xRichEditBox.Document.Selection.StartPosition;
                var s2 = this.xRichEditBox.Document.Selection.EndPosition;
                if (s1 == s2)
                {
                    this.xRichEditBox.Document.Selection.SetRange(s1, s2 + 1);
                }
                if (this.xRichEditBox.Document.Selection.Link.Length > 1)
                {
                    target = this.xRichEditBox.Document.Selection.Link.Split('\"')[1];
                }
                this.xRichEditBox.Document.Selection.SetRange(s1, s2);
            }
            if (target != null)
            {
                var doc = GetDoc();
                var point = doc.GetPositionField().Data;

                var theDoc = ContentController<FieldModel>.GetController<DocumentController>(target);
                if (theDoc != null && !theDoc.Equals(DBTest.DBNull))
                {
                    var pt = point;
                    pt.X -= 150;
                    pt.Y -= 50;
                    if (theDoc.GetDereferencedField<TextController>(KeyStore.AbstractInterfaceKey, null)?.Data == CollectionNote.APISignature)
                        theDoc = new CollectionNote(theDoc, pt, CollectionView.CollectionViewType.Schema, "", 200, 100).Document;
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
                        Windows.System.Launcher.LaunchUriAsync(new Uri(target));
                        //var WebDoc = DBTest.CreateWebPage(target);
                        //var pt = point;
                        //pt.X -= 150;
                        //pt.Y -= 50;
                        //MainPage.Instance.DisplayDocument(WebDoc, pt);
                    }
                }
                this.xRichEditBox.Document.Selection.SetRange(this.xRichEditBox.Document.Selection.StartPosition, this.xRichEditBox.Document.Selection.StartPosition);
            }
            target = null;
        }

        private void xRichEditBoxOnTextChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            WC.CountWords();
        }

        private void XRichEditBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
            if (!(ctrl.HasFlag(CoreVirtualKeyStates.Down) && e.Key == VirtualKey.H))
            {
                return;
            }
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

                        createRTFHyperlink(theDoc, startPt, ref s1, ref s2, lastTypedCharacter == "^", false);
                    }
                }
            }

            this.xRichEditBox.Document.Selection.SetRange(s1, s2);
            e.Handled = true;
        }

        private async void released(object sender, PointerRoutedEventArgs e)
        {
            Tag = null;
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () => SizeToFit());
        }

        static DocumentController findHyperlinkTarget(bool createIfNeeded, string refText)
        {
            var primaryKeys = refText.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var theDoc = DocumentController.FindDocMatchingPrimaryKeys(new List<string>(primaryKeys));
            if (theDoc == null && createIfNeeded)
            {
                if (refText.StartsWith("http"))
                {
                    theDoc = new HtmlNote(refText).Document;
                }
                else if (primaryKeys.Count() == 2 && primaryKeys[0] == "Filter")
                {
                    //theDoc = DBFilterOperatorController.CreateFilter(new DocumentReferenceFieldController(DBTest.DBDoc.GetId(), KeyStore.DataKey), primaryKeys.Last());
                }
                else
                {
                    theDoc = new NoteDocuments.RichTextNote(NoteDocuments.PostitNote.DocumentType).Document;
                    theDoc.GetDataDocument(null).SetField(KeyStore.TitleKey, new TextController(refText), true);
                }
            }

            return theDoc;
        }

        void createRTFHyperlink(DocumentController theDoc, Point startPt, ref int s1, ref int s2, bool createIfNeeded, bool forceLocal)
        {
            if (theDoc != null)
            {
                string link = "\"" + theDoc.GetId() + "\"";
                if (!forceLocal && theDoc.GetDataDocument(null).DocumentType.Equals(HtmlNote.DocumentType) && (bool)theDoc.GetDataDocument(null).GetDereferencedField<TextController>(KeyStore.HtmlTextKey, null)?.Data?.StartsWith("http"))
                {
                    link = "\"" + theDoc.GetDataDocument(null).GetDereferencedField<TextController>(KeyStore.HtmlTextKey, null).Data + "\"";
                }

                if (xRichEditBox.Document.Selection.StartPosition != xRichEditBox.Document.Selection.EndPosition && xRichEditBox.Document.Selection.Link != link)
                {
                    // set the hyperlink for the matched text
                    this.xRichEditBox.Document.Selection.Link = link;
                    // advance the end selection past the RTF embedded HYPERLINK keyword
                    s2 += this.xRichEditBox.Document.Selection.Link.Length + "HYPERLINK".Length + 1;
                    s1 = s2;
                    this.xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = Colors.LightCyan;
                    this.xRichEditBox.Document.Selection.SetPoint(startPt, PointOptions.Start, true);
                }
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

        /// <summary>
        /// Adds delegates to handle search related events (search doesn't highlight results yet)
        /// </summary>
        private void AddSearchBoxHandlers()
        {
            xSearchDelete.Tapped += delegate { xSearchBoxPanel.Visibility = Visibility.Collapsed; };
            xSearchBox.LostFocus += delegate { xSearchBoxPanel.Opacity = 0.5; };
            xReplaceBox.LostFocus += delegate { xSearchBoxPanel.Opacity = 0.5; };
            xSearchBox.GotFocus += delegate { xSearchBoxPanel.Opacity = 1; };
            xReplaceBox.GotFocus += delegate { xSearchBoxPanel.Opacity = 1; };
            xSearchBox.QueryChanged += XSearchBox_OnQueryChanged;
            xSearchBox.QuerySubmitted += XSearchBox_QuerySubmitted;
            //xReplaceBox.TextChanged += XReplaceBox_TextChanged;
            xReplaceModeButton.Tapped += delegate
            {
                xReplaceBoxPanel.Visibility = xReplaceBoxPanel.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                xReplaceModeButton.Content = xReplaceModeButton.Content.Equals("▲") ? "▼" : "▲";
            };
            xReplaceBox.KeyDown += XReplaceBox_KeyDown;
            xSearchBox.GotFocus += delegate { HasFocus = true; };
            xSearchBox.LostFocus += delegate { HasFocus = false; };
        }

        /// <summary>
        /// Add delegates to manage word count in the lower left hand corner of the richtextbox
        /// </summary>
        private void AddWordCountHandlers()
        {
            xWordCountBorder.PointerEntered += delegate { xWordCountBorder.Opacity = 1; };
            xWordCountBorder.PointerExited += delegate { xWordCountBorder.Opacity = 0.3; };
            xWordCount.DataContext = WC;
            WC.CountWords();
        }

        /// <summary>
        /// Add delegates to manage font size in the lower left hand corner of the richtextbox (next to the word count)
        /// </summary>
        private void AddFontSizeHandlers()
        {
            xFontSizePanel.PointerEntered += delegate { xFontSizePanel.Opacity = 1; };
            xFontSizePanel.PointerExited += delegate { xFontSizePanel.Opacity = 0.3; };
            xFontSizeLabel.Tapped += delegate
            {
                xFontSizeTextBox.Focus(FocusState.Programmatic); 
                xFontSizeTextBox.SelectAll();
            };
        }

        /// <summary>
        /// Add delegates to handle flyout (for format options) related events 
        /// </summary>
        private void AddFlyoutHandlers()
        {
            var flyout = FlyoutBase.GetAttachedFlyout(xRichEditBox);
            flyout.Opened += delegate { isFlyoutOpen = true; };
            flyout.Closed += delegate {
                isFlyoutOpen = false;
                xFormatTip.IsOpen = false;
            };
        }

        /// <summary>
        /// Add fonts to the format options flyout (under Fonts)
        /// </summary>
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
                var item = new MenuFlyoutItem();
                item.Text = font;
                item.Foreground = new SolidColorBrush(Colors.White);
                item.Click += delegate
                {
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

        /// <summary>
        /// Add colors to the format options flyout (under Highlight and Color)
        /// </summary>
        /// <param name="item"></param>
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

        /// <summary>
        /// Creates a range of color starting from color1 to color2
        /// </summary>
        /// <param name="color1"></param>
        /// <param name="color2"></param>
        /// <param name="item"></param>
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

        /// <summary>
        /// Adds color as a menu item under the specified submenu tab (either Highlight or Color) in the format options flyout
        /// </summary>
        /// <param name="color"></param>
        /// <param name="submenu"></param>
        /// <param name="height"></param>
        private void AddColorMenuItem(Color color, MenuFlyoutSubItem submenu, double height)
        {
            var item = new MenuFlyoutItem();
            item.Background = new SolidColorBrush(color);
            item.Height = height;
            // handles what happens when the item gets focus and when it is clicked
            if (submenu == xColor)
            {
                item.Click += delegate
                {
                    UpdateDocument();
                    currentCharFormat = null;
                    xRichEditBox.SelectionHighlightColorWhenNotFocused = highlightNotFocused;
                };
                item.GotFocus += delegate
                {
                    Foreground(color, false);
                    xRichEditBox.SelectionHighlightColorWhenNotFocused = new SolidColorBrush(Colors.Transparent);
                };
            }
            else if (submenu == xHighlight)
            {
                item.Click += delegate
                {
                    UpdateDocument();
                    currentCharFormat = null;
                    xRichEditBox.SelectionHighlightColorWhenNotFocused = highlightNotFocused;
                };
                item.GotFocus += delegate
                {
                    Highlight(color, false);
                    xRichEditBox.SelectionHighlightColorWhenNotFocused = new SolidColorBrush(Colors.Transparent);
                };
            }

            submenu?.Items?.Add(item);
        }

        /// <summary>
        /// Add opacity levels to the format options flyout (under Highlight, in Opacity tab)
        /// </summary>
        private void AddHighlightOpacity()
        {
            // doesn't really work yet
            //double opacity = 1;
            //while (opacity > 0)
            //{
            //    var item = new MenuFlyoutItem();
            //    var menuText = opacity.ToString();
            //    item.Text = menuText.ToString();
            //    item.Click += delegate
            //    {
            //        UpdateDocument();
            //        currentCharFormat = null;
            //        xRichEditBox.SelectionHighlightColorWhenNotFocused = new SolidColorBrush(Colors.Gray);
            //    };
            //    item.GotFocus += delegate
            //    {
            //        var highlight = xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor;
            //        var color = Color.FromArgb((byte)(opacity * 255), highlight.R, highlight.G, highlight.B);
            //        Highlight(color, false);
            //        xRichEditBox.SelectionHighlightColorWhenNotFocused = new SolidColorBrush(Colors.Transparent);
            //    };
            //    xHighlightOpacity?.Items?.Add(item);
            //    opacity -= 0.1;
            //}
        }

        /// <summary>
        /// Adds all character formats to their corresponding tab in the format options flyout
        /// </summary>
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

        /// <summary>
        /// Adds all paragraph formats to their corresponding tab in the format options flyout
        /// </summary>
        private void AddParagraphFormats()
        {
            var alignmentNames = alignments.Keys;
            foreach (var alignment in alignmentNames)
            {
                AddFormatMenuItem(alignment, xAlignment, false);
            }
            var listTypes = markerTypes.Keys;
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

        /// <summary>
        /// Adds character format to the format options flyout as a menu item under the specified submenu tab
        /// </summary>
        /// <param name="menuText"></param>
        /// <param name="subMenu"></param>
        /// <param name="charFormat"></param>
        private void AddFormatMenuItem(string menuText, MenuFlyoutSubItem subMenu, bool charFormat)
        {
            var item = new MenuFlyoutItem();
            item.Text = menuText;
            item.Foreground = new SolidColorBrush(Colors.White);
            item.Click += Format_OnClick;

            // handles what happens when the item gets focus and when it is clicked
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

        /// <summary>
        /// Makes current selection in the xRichEditBox bold
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        private void Bold(bool updateDocument)
        {
            // on/off instead of toggle to know exactly what state it is in (to determine whether a selection is bold or not)
            if (this.xRichEditBox.Document.Selection.CharacterFormat.Bold == FormatEffect.On)
            {
                this.xRichEditBox.Document.Selection.CharacterFormat.Bold = FormatEffect.Off;
            }
            else
            {
                this.xRichEditBox.Document.Selection.CharacterFormat.Bold = FormatEffect.On;
            }
            if(updateDocument) UpdateDocument();
        }

        /// <summary>
        /// Italicizes current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
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

        /// <summary>
        /// Underlines the current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        private void Underline(bool updateDocument)
        {
            if (this.xRichEditBox.Document.Selection.CharacterFormat.Underline == UnderlineType.None)
                this.xRichEditBox.Document.Selection.CharacterFormat.Underline = UnderlineType.Single;
            else
                this.xRichEditBox.Document.Selection.CharacterFormat.Underline = UnderlineType.None;
            if (updateDocument) UpdateDocument();
        }

        /// <summary>
        /// Strikethrough the current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        private void Strikethrough(bool updateDocument)
        {
            if (xRichEditBox.Document.Selection.CharacterFormat.Strikethrough == FormatEffect.On)
                xRichEditBox.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.Off;
            else
                xRichEditBox.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.On;
            if (updateDocument) UpdateDocument();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into superscripts
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        private void Superscript(bool updateDocument)
        {
            if (xRichEditBox.Document.Selection.CharacterFormat.Superscript == FormatEffect.On)
                xRichEditBox.Document.Selection.CharacterFormat.Superscript = FormatEffect.Off;
            else
                xRichEditBox.Document.Selection.CharacterFormat.Superscript = FormatEffect.On;
            if (updateDocument) UpdateDocument();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into subscripts
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        private void Subscript(bool updateDocument)
        {
            if (xRichEditBox.Document.Selection.CharacterFormat.Subscript == FormatEffect.On)
                xRichEditBox.Document.Selection.CharacterFormat.Subscript = FormatEffect.Off;
            else
                xRichEditBox.Document.Selection.CharacterFormat.Subscript = FormatEffect.On;
            if (updateDocument) UpdateDocument();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into smallcaps
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        private void SmallCaps(bool updateDocument)
        {
            if (xRichEditBox.Document.Selection.CharacterFormat.SmallCaps == FormatEffect.On)
                xRichEditBox.Document.Selection.CharacterFormat.SmallCaps = FormatEffect.Off;
            else
                xRichEditBox.Document.Selection.CharacterFormat.SmallCaps = FormatEffect.On;
            if (updateDocument) UpdateDocument();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into allcaps
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        private void AllCaps(bool updateDocument)
        {
            if (xRichEditBox.Document.Selection.CharacterFormat.AllCaps == FormatEffect.On)
                xRichEditBox.Document.Selection.CharacterFormat.AllCaps = FormatEffect.Off;
            else
                xRichEditBox.Document.Selection.CharacterFormat.AllCaps = FormatEffect.On;
            if (updateDocument) UpdateDocument();
        }

        /// <summary>
        /// Highlights the current selection in xRichEditBox, the color of the highlight is specified by background
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="background"></param>
        /// <param name="updateDocument"></param>
        private void Highlight(Color background, bool updateDocument)
        {
            xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = background;
            if (updateDocument) UpdateDocument();
        }

        /// <summary>
        /// Changes the color of the font of the current selection in xRichEditBox, the font color is specified by color
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="color"></param>
        /// <param name="updateDocument"></param>
        private void Foreground(Color color, bool updateDocument)
        {
            xRichEditBox.Document.Selection.CharacterFormat.ForegroundColor = color;
            if (updateDocument) UpdateDocument();
        }

        /// <summary>
        /// Sets the paragraph alignment of the current selection to be what's specified by alignment
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="alignment"></param>
        /// <param name="updateDocument"></param>
        private void Alignment(object alignment, bool updateDocument)
        {
            if (alignment != null && alignment.GetType() == typeof(ParagraphAlignment))
            {
                xRichEditBox.Document.Selection.ParagraphFormat.Alignment = (ParagraphAlignment)alignment;
                if (updateDocument) UpdateDocument();
            }
        }

        /// <summary>
        /// Sets the list marker of the current selection to be what's specified by type
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="type"></param>
        /// <param name="updateDocument"></param>
        private void Marker(object type, bool updateDocument)
        {
            if (type != null && type.GetType() == typeof(MarkerType))
            {
                xRichEditBox.Document.Selection.ParagraphFormat.ListType = (MarkerType)type;
                if (updateDocument) UpdateDocument();
            }
        }

        /// <summary>
        /// Sets the list marker style of the current selection to be what's specified by type
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="type"></param>
        /// <param name="updateDocument"></param>
        private void MarkerStyle(object type, bool updateDocument)
        {
            if (type != null && type.GetType() == typeof(MarkerStyle))
            {
                xRichEditBox.Document.Selection.ParagraphFormat.ListStyle = (MarkerStyle)type;
                if (updateDocument) UpdateDocument();
            }
        }

        /// <summary>
        /// Sets the marker alignment of the current selection to be what's specified by type
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="type"></param>
        /// <param name="updateDocument"></param>
        private void MarkerAlignment(object type, bool updateDocument)
        {
            if (type != null && type.GetType() == typeof(MarkerAlignment))
            {
                xRichEditBox.Document.Selection.ParagraphFormat.ListAlignment = (MarkerAlignment)type;
                if (updateDocument) UpdateDocument();
            }
        }

        /// <summary>
        /// Sets the character formatting of the current selection according to the name of the menu item (clicked/gotfocus)
        /// in the format options flyout
        /// </summary>
        /// <param name="menuText"></param>
        /// <param name="updateDocument"></param>
        private void CharFormat(String menuText, bool updateDocument)
        {
            if (menuText == "Bold (ctrl+B)")
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
            }
            else if (menuText == "Strikethrough")
            {
                Strikethrough(updateDocument);
            }
            else if (menuText == "Superscript")
            {
                Superscript(updateDocument);
            }
            else if (menuText == "Subscript")
            {
                Subscript(updateDocument);
            }
            else if (menuText == "AllCaps")
            {
                AllCaps(updateDocument);
            }
            else if (menuText == "SmallCaps")
            {
                SmallCaps(updateDocument);
            }
        }

        private void SizeToFit()
        {
            var s1 = this.xRichEditBox.Document.Selection.StartPosition;
            var s2 = this.xRichEditBox.Document.Selection.EndPosition;
            var str = "";
            this.xRichEditBox.Document.GetText(TextGetOptions.FormatRtf, out str);
            xRichEditBox.Document.Selection.SetRange(0, str.Length);
            var selectedText = xRichEditBox.Document.Selection;
            xRichEditBox.Measure(new Size(xRichEditBox.ActualWidth, 1000));
            int count = 0;
            float lastMax = 20;
            float lastMin = 6;
            while (Math.Abs(xRichEditBox.DesiredSize.Height - xRichEditBox.ActualHeight) > 5 && selectedText != null && count++ < 10)
            {
                var charFormatting = selectedText.CharacterFormat;
                var curSize = charFormatting.Size < 0 ? 10 : charFormatting.Size;
                float delta = (float)(xRichEditBox.DesiredSize.Height > xRichEditBox.ActualHeight ? (lastMin - curSize) : (lastMax - curSize));
                if (delta < 0)
                    lastMax = curSize;
                else lastMin = curSize;
                try
                {
                    charFormatting.Size = curSize + delta / 2;
                    selectedText.CharacterFormat = charFormatting;
                }
                catch (Exception)
                {

                }
                xRichEditBox.Measure(new Size(xRichEditBox.ActualWidth, 1000));
            }
            this.xRichEditBox.Document.Selection.SetRange(s1, s2);
        }

        /// <summary>
        /// Sets the paragraph formatting of the current selection according to the name of the menu item (clicked/gotfocus)
        /// in the format options flyout
        /// </summary>
        /// <param name="menuText"></param>
        /// <param name="updateDocument"></param>
        private void ParFormat(string menuText, bool updateDocument)
        {
            ParagraphAlignment alignment;
            if (alignments.TryGetValue(menuText, out alignment))
                Alignment(alignment, updateDocument);

            MarkerType listType;
            if (markerTypes.TryGetValue(menuText, out listType))
                Marker(listType, updateDocument);

            MarkerStyle listStyle;
            if (markerStyles.TryGetValue(menuText, out listStyle))
                MarkerStyle(listStyle, updateDocument);

            MarkerAlignment listAlignment;
            if (markerAlignments.TryGetValue(menuText, out listAlignment))
                MarkerAlignment(listAlignment, updateDocument);
            // not sure what this is or how to merge it
            //string allText;
            //xRichEditBox.Document.GetText(TextGetOptions.UseObjectText, out allText);
            //if (allText.TrimEnd('\r') != GetText()?.ReadableString?.TrimEnd('\r'))
            //{
            //    string allRtfText;
            //    xRichEditBox.Document.GetText(TextGetOptions.FormatRtf, out allRtfText);
            //    UnregisterPropertyChangedCallback(TextProperty, TextChangedCallbackToken);
            //    Text = new RichTextModel.RTD(allText, allRtfText.Replace("\\pard\\tx720\\par", ""));  // RTF editor adds a trailing extra paragraph when queried -- need to strip that off
            //    TextChangedCallbackToken = RegisterPropertyChangedCallback(TextProperty, TextChangedCallback);
            //}
            //var ele = FocusManager.GetFocusedElement() as FrameworkElement;
            //if (!ele.GetAncestors().Contains(this) && (xFontComboBox.ItemsPanelRoot == null || !xFontComboBox.ItemsPanelRoot.Children.Contains(ele)))
            //{
            //    xFormatControls.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () => SizeToFit());
            //}
        }

        void UpdateDocument()
        {
            string allText;
            xRichEditBox.Document.GetText(TextGetOptions.UseObjectText, out allText);
            string allRtfText;
            xRichEditBox.Document.GetText(TextGetOptions.FormatRtf, out allRtfText);
            UnregisterPropertyChangedCallback(TextProperty, TextChangedCallbackToken);
            Text = new RichTextModel.RTD(allText, allRtfText.Replace("\\pard\\tx720\\par", ""));  // RTF editor adds a trailing extra paragraph when queried -- need to strip that off
            TextChangedCallbackToken = RegisterPropertyChangedCallback(TextProperty, TextChangedCallback);
        }

        /// <summary>
        /// Closes format options flyout and sets HasFocus to true (tab indents in this case instead of invoking the tab menu)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xRichEditBox_GotFocus(object sender, RoutedEventArgs e)
        {
            FlyoutBase.GetAttachedFlyout(xRichEditBox)?.Hide();
            HasFocus = true;
            UpdateDocument();
        }

        /// <summary>
        /// Sets HasFocus to false (allows tab to invoke tab menu when richeditbox does not have focus)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XRichEditBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            HasFocus = false;
        }

        private async void xRichEditBox_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            DocumentController theDoc = null;
            if (e.DataView.Properties.ContainsKey("DocumentControllerList"))
            {
                var docCtrls = e.DataView.Properties["DocumentControllerList"] as List<DocumentController>;
                theDoc = docCtrls.First();
            }
            var forceLocal = true;
            var sourceIsFileSystem = e.DataView.Contains(StandardDataFormats.StorageItems);
            if (sourceIsFileSystem)
            {
                theDoc = await FileDropHelper.GetDroppedFile(e);
                forceLocal = false;
            }


            string allText;
            xRichEditBox.Document.GetText(TextGetOptions.UseObjectText, out allText);

            var startPt = new Point();
            var s1 = this.xRichEditBox.Document.Selection.StartPosition;
            var s2 = this.xRichEditBox.Document.Selection.EndPosition;
            this.xRichEditBox.Document.Selection.GetPoint(HorizontalCharacterAlignment.Center, VerticalCharacterAlignment.Baseline, PointOptions.Start, out startPt);

            createRTFHyperlink(theDoc, startPt, ref s1, ref s2, false, forceLocal);

            if (allText.TrimEnd('\r') != GetText()?.ReadableString?.TrimEnd('\r'))
            {
                string allRtfText;
                xRichEditBox.Document.GetText(TextGetOptions.FormatRtf, out allRtfText);
                UnregisterPropertyChangedCallback(TextProperty, TextChangedCallbackToken);
                Text = new RichTextModel.RTD(allText, allRtfText.Replace("\\pard\\tx720\\par", ""));  // RTF editor adds a trailing extra paragraph when queried -- need to strip that off
                TextChangedCallbackToken = RegisterPropertyChangedCallback(TextProperty, TextChangedCallback);
            }
            xRichEditBox.Document.Selection.SetRange(s1, s2);
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
            }
            LastS1 = s1;
            LastS2 = s2;
            // format and update the tooltip to reflect the properties of the current selection
            if(xRichEditBox.Document.Selection.StartPosition != xRichEditBox.Document.Selection.EndPosition) this.FormatToolTipInfo(xRichEditBox.Document.Selection);
            // set to display font size of the current selection on the lower left hand corner
            WC.Size = xRichEditBox.Document.Selection.CharacterFormat.Size;
            if (WC.Size < 0) WC.Size = 0;
        }

        /// <summary>
        /// Create short cuts for the xRichEditBox (ctrl+I creates indentation by default, ctrl-Z will get rid of the indentation, showing only the italized text)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XRichEditBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var ctrlState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Control)
                .HasFlag(CoreVirtualKeyStates.Down);
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
                    FormatToolTipInfo(xRichEditBox.Document.Selection);
                } else if (e.Key.Equals(VirtualKey.I))
                {
                    Italicize(true);
                    FormatToolTipInfo(xRichEditBox.Document.Selection);
                    e.Handled = true;
                }
                else if (e.Key.Equals(VirtualKey.U))
                {
                    Underline(true);
                    FormatToolTipInfo(xRichEditBox.Document.Selection);
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

        /// <summary>
        /// A dictionary of the original character formats of all of the highlighted search results
        /// </summary>
        private Dictionary<int, ITextCharacterFormat> originalCharFormat = new Dictionary<int, ITextCharacterFormat>();

        /// <summary>
        /// The length of the previous search query
        /// </summary>
        private int prevQueryLength;

        /// <summary>
        /// Searches content of the xRichEditBox, highlights all results
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void XSearchBox_OnQueryChanged(SearchBox sender, SearchBoxQueryChangedEventArgs args)
        {
            this.ClearSearchHighlights();
            nextMatch = 0;
            var query = args.QueryText;
            prevQueryLength = query.Length;
            string text;
            xRichEditBox.Document.GetText(TextGetOptions.None, out text);
            var length = text.Length;
            xRichEditBox.Document.Selection.StartPosition = 0;
            xRichEditBox.Document.Selection.EndPosition = 0;
            int i = 1;
            // find and highlight all matches
            while (i > 0)
            {
                i = xRichEditBox.Document.Selection.FindText(query, length, FindOptions.None);
                var s = xRichEditBox.Document.Selection.StartPosition;
                var selectedText = xRichEditBox.Document.Selection;
                if (i > 0)
                {
                    originalCharFormat.Add(s, selectedText.CharacterFormat.GetClone());
                }
                if (selectedText != null)
                {
                    selectedText.CharacterFormat.BackgroundColor = Colors.Yellow;
                }
            }
        }

        /// <summary>
        /// Index of the next highlighted search result
        /// </summary>
        private int nextMatch = 0;

        /// <summary>
        /// Selects the next highlighted search result on enter in the xRichEditBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void XSearchBox_QuerySubmitted(SearchBox sender, SearchBoxQuerySubmittedEventArgs args)
        {
            this.NextResult();
        }

        /// <summary>
        /// Selects the next highlighted search result on enter in the xRichEditBox
        /// </summary>
        private void NextResult()
        {
            var keys = originalCharFormat.Keys;
            if (keys.Count != 0)
            {
                var start = keys.ElementAt(nextMatch);
                xRichEditBox.Document.Selection.StartPosition = start;
                xRichEditBox.Document.Selection.EndPosition = start + prevQueryLength;
                xRichEditBox.Document.Selection.ScrollIntoView(PointOptions.None);
                if (nextMatch < keys.Count - 1)
                    nextMatch++;
                else
                    nextMatch = 0;
            }
        }

        /// <summary>
        /// Clears the highlights that result from searching within the xRichEditBox (to make sure that
        /// original highlights wouldn't get erased)
        /// </summary>
        private void ClearSearchHighlights()
        {
            xRichEditBox.SelectionHighlightColorWhenNotFocused = new SolidColorBrush(Colors.Transparent);
            var keys = originalCharFormat.Keys;
            foreach (var key in keys)
            {
                xRichEditBox.Document.Selection.StartPosition = key;
                xRichEditBox.Document.Selection.EndPosition = key + prevQueryLength;
                xRichEditBox.Document.Selection.CharacterFormat.SetClone(originalCharFormat[key]);
            }
            xRichEditBox.SelectionHighlightColorWhenNotFocused = highlightNotFocused;
            originalCharFormat.Clear();
        }

        /// <summary>
        /// Pressing enter when the replace text box has focus selects the next highlighted search result
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XReplaceBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key.Equals(VirtualKey.Enter))
            {
                xRichEditBox.Document.Selection.SetText(TextSetOptions.None, (sender as TextBox).Text);
                var start = xRichEditBox.Document.Selection.StartPosition;
                ITextCharacterFormat clone;
                originalCharFormat.TryGetValue(start, out clone);
                if (clone != null)
                {
                    xRichEditBox.Document.Selection.CharacterFormat.SetClone(clone);
                    originalCharFormat.Remove(start);
                    if (nextMatch >= originalCharFormat.Keys.Count || nextMatch == 0)
                        nextMatch = 0;
                    else
                        nextMatch--;
                    this.NextResult();
                }
            }
        }

        /// <summary>
        /// Opens the format options flyout
        /// </summary>
        /// <param name="sender"></param>
        private void OpenContextMenu(object sender)
        {
            var element = sender as FrameworkElement;
            if (element != null)
            {
                FlyoutBase.ShowAttachedFlyout(element);
                FlyoutBase.GetAttachedFlyout(element)?.ShowAt(element);
                AddFlyoutItems();
            }
            currentCharFormat = xRichEditBox.Document.Selection.CharacterFormat.GetClone();
            xRichEditBox.SelectionHighlightColorWhenNotFocused = highlightNotFocused;
        }

        /// <summary>
        /// When a format is clicked, it has already triggered the gotfocus event, which has already set
        /// the format of the text (to create preview of the format), updating the document will confirm
        /// the change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Format_OnClick(object sender, RoutedEventArgs e)
        {
            UpdateDocument();
            currentCharFormat = null;
            currentParagraphFormat = null;
        }

        /// <summary>
        /// Opens format options flyout on holding
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XRichEditBox_OnHolding(object sender, HoldingRoutedEventArgs e)
        {
            OpenContextMenu(sender);
            FlyoutBase.GetAttachedFlyout(xRichEditBox).AllowFocusOnInteraction = true;
            if (xFont.Items.Count == 0) AddFonts();
            if (xColor.Items.Count == 0) AddColors(xColor);
            if (xHighlight.Items.Count == 1) AddColors(xHighlight);
        }

        /// <summary>
        /// Remove font preview when the submenu tab (Font) gets focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontGroup_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (currentCharFormat != null) xRichEditBox.Document.Selection.CharacterFormat.SetClone(currentCharFormat);
        }

        /// <summary>
        /// Remove highlight preview when the submenu tab (Highlight) gets focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HighlightGroup_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (currentCharFormat != null) xRichEditBox.Document.Selection.CharacterFormat.SetClone(currentCharFormat);
        }

        /// <summary>
        /// Remove highlight preview when the submenu tab (Color) gets focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorGroup_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (currentCharFormat != null) xRichEditBox.Document.Selection.CharacterFormat.SetClone(currentCharFormat);
        }

        /// <summary>
        /// Remove character formatting preview when the Font tab gets focus
        /// </summary>
        private ITextCharacterFormat currentCharFormat;
        private void FormatGroup_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if(currentCharFormat != null) xRichEditBox.Document.Selection.CharacterFormat.SetClone(currentCharFormat);
        }

        /// <summary>
        /// Remove character formatting preview when submenu tab (under the Font tab) gets focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CharFormat_OnGotFocus(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as MenuFlyoutItem;
            var menuText = menuFlyoutItem?.Text;
            if(currentCharFormat != null) xRichEditBox.Document.Selection.CharacterFormat.SetClone(currentCharFormat);
            CharFormat(menuText, false);
        }

        /// <summary>
        /// Remove paragraph formatting preview when submenu tab (under the paragraph tab) gets focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ParFormat_OnGotFocus(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as MenuFlyoutItem;
            var menuText = menuFlyoutItem?.Text;
            if (currentParagraphFormat != null) xRichEditBox.Document.Selection.CharacterFormat.SetClone(currentCharFormat);
            ParFormat(menuText, false);
        }

        /// <summary>
        /// Clone of current selection's paragraph format before preview is set (for removing preview)
        /// </summary>
        private ITextParagraphFormat currentParagraphFormat;

        /// <summary>
        /// Remove paragraph formatting preview when the Paragraph tab gets focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ParagraphGroup_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (currentParagraphFormat != null) xRichEditBox.Document.Selection.ParagraphFormat.SetClone(currentParagraphFormat);
        }

        /// <summary>
        /// Creates the content of the format options flyout by adding items to all tabs/submenus
        /// </summary>
        private void AddFlyoutItems()
        {
            if (xAlignment.Items.Count == 0) AddParagraphFormats();
            if (xFormat.Items.Count == 3) AddCharacterFormats();
            if (xColor.Items.Count == 0) AddColors(xColor);
            if (xHighlight.Items.Count == 1) AddColors(xHighlight);
            if (xFont.Items.Count == 0) AddFonts();
            if (xHighlightOpacity.Items.Count == 0) AddHighlightOpacity();
        }

        /// <summary>
        /// Remove all character and paragraph formatting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XResetAll_OnClick(object sender, RoutedEventArgs e)
        {
            xRichEditBox.Document.Selection.CharacterFormat.SetClone(defaultCharFormat);
            xRichEditBox.Document.Selection.ParagraphFormat.SetClone(defaultParFormat);
        }

        /// <summary>
        /// Sets up and shows tooltip, which lists some main formatting properties of the current selection
        /// </summary>
        /// <param name="range"></param>
        private async void FormatToolTipInfo(ITextRange range)
        {
            // TODO: show tooltip at position of the mouse (horizontal & vertical offset (alone) do not work)
            xFormatTipText.Inlines.Clear();
            AddPropertyRun("Alignment", range.ParagraphFormat.Alignment.ToString());
            AddPropertyRun("Font", range.CharacterFormat.Name);
            AddPropertyRun("Size", range.CharacterFormat.Size.ToString(CultureInfo.InvariantCulture));
            AddPropertyRun("Bold", range.CharacterFormat.Bold.ToString());
            AddPropertyRun("Italic", range.CharacterFormat.Italic.ToString());
            AddPropertyRun("Underline", range.CharacterFormat.Underline.ToString());
            AddPropertyRun("Strikethrough", range.CharacterFormat.Strikethrough.ToString());
            AddPropertyRun("Superscript", range.CharacterFormat.Superscript.ToString());
            AddPropertyRun("Subscript", range.CharacterFormat.Subscript.ToString());
            AddPropertyRun("ListType", range.ParagraphFormat.ListType.ToString());
            // show tooltip at mouse position, does not work
            //xFormatTip.Placement = PlacementMode.Mouse;
            //Point point;
            //xRichEditBox.Document.Selection.GetPoint(HorizontalCharacterAlignment.Left, VerticalCharacterAlignment.Baseline, PointOptions.ClientCoordinates, out point);
            //xFormatTip.HorizontalOffset = point.X;
            //xFormatTip.VerticalOffset = -point.Y;
            //xFormatTip.PlacementTarget = xRichEditBox;
            xFormatTip.Placement = PlacementMode.Left;
            xFormatTip.IsOpen = true;
            await Task.Delay(3000);
            if (!isFlyoutOpen)
                xFormatTip.IsOpen = false;
        }

        /// <summary>
        /// Adds format property to the tooltip if it is not None, Undefined, or Off
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void AddPropertyRun(string key, string value)
        {
            if (!value.Equals("None") && !value.Equals("Undefined") && !value.Equals("Off"))
            {
                var keyRun = new Run();
                keyRun.Text = key + ": ";
                var valRun = new Run();
                if (value.StartsWith("-"))
                    valRun.Text = "-";
                else
                    valRun.Text = value;
                var lineBreak = new LineBreak();
                xFormatTipText.Inlines.Add(keyRun);
                xFormatTipText.Inlines.Add(valRun);
                xFormatTipText.Inlines.Add(lineBreak);
            }
        }

    }

    /// <summary>
    /// Handles word count and font size bindings in RichTextView
    /// </summary>
    public class WordCount : INotifyPropertyChanged
    {
        public int Count
        {
            get => _count;
            set
            {
                _count = value;
                RaisePropertyChanged("Count");
            }
        }
        private int _count;

        public float Size
        {
            get => _size;
            set
            {
                _size = value;
                SetFontSize();
                RaisePropertyChanged("Size");
            }
        }
        private float _size;
        public event PropertyChangedEventHandler PropertyChanged;

        private RichEditBox _box;
        public WordCount(RichEditBox box)
        {
            _box = box;
        }

        public void CountWords()
        {
            string text;
            _box.Document.GetText(TextGetOptions.None, out text);
            var words = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Count = words.Length;
        }

        private void SetFontSize()
        {
            var selection = _box.Document.Selection;
            if (!selection.CharacterFormat.Size.Equals(_size) && _size > 0)
            {
                selection.CharacterFormat.Size = _size;
            }
        }

        protected void RaisePropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}

