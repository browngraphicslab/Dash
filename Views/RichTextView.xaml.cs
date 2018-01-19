using Dash.Controllers.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Visibility = Windows.UI.Xaml.Visibility;
using System.ComponentModel;
using System.Globalization;
using Windows.UI.Xaml.Documents;
using DashShared.Models;
using static Dash.NoteDocuments;
using Windows.ApplicationModel.Core;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using System.Text.RegularExpressions;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236
namespace Dash
{
    public sealed partial class RichTextView : UserControl
    {

        #region instance variables

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(RichTextModel.RTD), typeof(RichTextView), new PropertyMetadata(default(RichTextModel.RTD)));
        
        public RichTextModel.RTD Text
        {
            get { return (RichTextModel.RTD)GetValue(TextProperty); }
            set{ SetValue(TextProperty, value); }
        }

        public DocumentController DataDocument { get; set; }

        public RichTextController  TargetRTFController = null;
        public ReferenceController TargetFieldReference = null;
        public Context TargetDocContext = null;

        private RichTextFormattingHelper _rtfHelper;

        public static bool HasFocus = false;

        private SolidColorBrush highlightNotFocused = new SolidColorBrush(Colors.Gray) {Opacity=0.5};
        private bool CanSizeToFit = false;
        long TextChangedCallbackToken;

        public string target = null;
        private bool _rightPressed = false;
        PointerEventHandler moveHdlr = null, releasedHdlr = null;

        private Point _rightDragLastPosition, _rightDragStartPosition;


        // for manipulation movement
        ScrollBar Scroll = null;

        // for rich edit box
        private int LastS1 = 0, LastS2 = 0;

        /// <summary>
        /// A dictionary of the original character formats of all of the highlighted search results
        /// </summary>
        private Dictionary<int, ITextCharacterFormat> originalCharFormat = new Dictionary<int, ITextCharacterFormat>();

        /// <summary>
        /// The length of the previous search query
        /// </summary>
        private int prevQueryLength;

        #endregion

    
        /// <summary>
        /// Constructor
        /// </summary>
        public RichTextView()
        {
            this.InitializeComponent();
            Loaded   += OnLoaded;
            Unloaded += UnLoaded;

            TextChangedCallbackToken = RegisterPropertyChangedCallback(TextProperty, TextChangedCallback);
            xRichEditBox.AddHandler(KeyDownEvent, new KeyEventHandler(XRichEditBox_OnKeyDown), true);

            _rtfHelper = new RichTextFormattingHelper(this, xRichEditBox);

            xRichEditBox.Document.Selection.CharacterFormat.Name = "Calibri";
            xRichEditBox.SelectionChanged += delegate(object sender, RoutedEventArgs args)
            {
                var freeform = this.GetFirstAncestorOfType<CollectionFreeformView>();
                if (freeform == null)
                {
                    return;
                }

                var docView = this.GetFirstAncestorOfType<DocumentView>();
                if(docView== null)
                {
                    return ;
                }

                if (freeform.TagNote(xRichEditBox.Document.Selection.Text, docView))
                {
                    //var start = xRichEditBox.Document.Selection.StartPosition;
                    //xRichEditBox.Document.Selection.SetRange(start, start);
                }
            };

            // store a clone of character format after initialization as default format
            xFormattingMenuView.defaultCharFormat = xRichEditBox.Document.Selection.CharacterFormat.GetClone();
            // store a clone of paragraph format after initialization as default format
            xFormattingMenuView.defaultParFormat = xRichEditBox.Document.Selection.ParagraphFormat.GetClone();

        }


        #region main functionality

        private void SizeToFit()
        {
            if (!this.IsInVisualTree() || !CanSizeToFit)
                return;
            var s1 = this.xRichEditBox.Document.Selection.StartPosition;
            var s2 = this.xRichEditBox.Document.Selection.EndPosition;
           
            xRichEditBox.Measure(new Size(xRichEditBox.ActualWidth, 1000));
            var relative = this.GetFirstAncestorOfType<RelativePanel>();
            if (relative != null)
                relative.Height = Math.Max(ActualHeight, xRichEditBox.DesiredSize.Height);

            if (false)
            {
                int count = 0;
                float lastMax = 20;
                float lastMin = 6;
                float lastGoodSize = 0;
                var selectedText = xRichEditBox.Document.Selection;

                while (Math.Abs(xRichEditBox.DesiredSize.Height - xRichEditBox.ActualHeight) > 0 && selectedText != null && count++ < 10)
                {
                    var charFormatting = selectedText.CharacterFormat;
                    var curSize = charFormatting.Size < 0 ? 10 : charFormatting.Size;
                    float delta = (float)(xRichEditBox.DesiredSize.Height > xRichEditBox.ActualHeight ? (lastMin - curSize) : (lastMax - curSize));
                    if (curSize > lastGoodSize && Scroll.Visibility == Visibility.Collapsed)
                        lastGoodSize = curSize;
                    if (delta < 0)
                    {
                        lastMax = curSize;
                        delta = (float)Math.Ceiling(delta);
                    }
                    else
                    {
                        lastMin = curSize;
                        if (delta < 1)
                            break;
                        else delta = (float)Math.Floor(delta);
                    }
                    try
                    {
                        charFormatting.Size = curSize + delta / 2;
                        selectedText.CharacterFormat = charFormatting;
                    }
                    catch (Exception) { }
                    xRichEditBox.Measure(new Size(xRichEditBox.ActualWidth, 1000));
                }
                if (Scroll.Visibility == Visibility.Visible && lastGoodSize > 0)
                {
                    var charFormatting = selectedText.CharacterFormat;
                    charFormatting.Size = lastGoodSize;
                    selectedText.CharacterFormat = charFormatting;
                    xRichEditBox.Measure(new Size(xRichEditBox.ActualWidth, 1000));
                }
            }
            this.xRichEditBox.Document.Selection.SetRange(s1, s2);
        }

        private void TextChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            var reg = new Regex("\\\\par[\r\n}\\\\]*\0");
            var newstr = reg.Replace(Text.RtfFormatString, "}\r\n\0");
            xRichEditBox.Document.SetText(TextSetOptions.FormatRtf, newstr);
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

        public void UpdateDocument()
        {
            string allText;
            xRichEditBox.Document.GetText(TextGetOptions.UseObjectText, out allText);
            string allRtfText = GetRtfText();
            UnregisterPropertyChangedCallback(TextProperty, TextChangedCallbackToken);
            Text = new RichTextModel.RTD(allText, allRtfText);  // RTF editor adds a trailing extra paragraph when queried -- need to strip that off
            TextChangedCallbackToken = RegisterPropertyChangedCallback(TextProperty, TextChangedCallback);
        }


        #region eventhandlers
        private void Scroll_LayoutUpdated(object sender, object e)
        {
            if (Scroll.Visibility == Visibility.Visible)
                released(null, null);
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
                        theDoc = new CollectionNote(theDoc, pt, CollectionView.CollectionViewType.Schema, 200, 100).Document;
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
                        MainPage.Instance.WebContext.SetUrl(target);
                    }
                }
                this.xRichEditBox.Document.Selection.SetRange(this.xRichEditBox.Document.Selection.StartPosition, this.xRichEditBox.Document.Selection.StartPosition);
            }
            target = null;
        }

        private void XRichEditBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            CanSizeToFit = true;
            var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
            if (!(ctrl.HasFlag(CoreVirtualKeyStates.Down) && e.Key == VirtualKey.H))
            {
                if (e.Key == VirtualKey.Back)
                {
                    string docText;
                    xRichEditBox.Document.GetText(TextGetOptions.UseObjectText, out docText);
                    if (docText == "")
                    {

                        var parentDoc = this.GetFirstAncestorOfType<DocumentView>();
                        parentDoc.DeleteDocument(true);
                    }
                    return;
                }
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
            if (e != null && (e.KeyModifiers & VirtualKeyModifiers.Control) != 0)
            {
                var c = DataDocument.GetField(KeyStore.WebContextKey) as TextController;
                if (c != null)
                {
                    BrowserView.OpenTab(c.Data);
                }
            }
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () => SizeToFit());
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
                string allRtfText = GetRtfText();
                UnregisterPropertyChangedCallback(TextProperty, TextChangedCallbackToken);
                Text = new RichTextModel.RTD(allText, allRtfText);
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
            //if(xRichEditBox.Document.Selection.StartPosition != xRichEditBox.Document.Selection.EndPosition) this.FormatToolTipInfo(xRichEditBox.Document.Selection);
            // set to display font size of the current selection on the lower left hand corner
            //WC.Size = xRichEditBox.Document.Selection.CharacterFormat.Size;
            //if (WC.Size < 0) WC.Size = 0;
        }
        
        #endregion

        #region getters
        RichTextModel.RTD GetText()
        {
            if (TargetRTFController != null)
                return TargetRTFController.Data;
            return TargetFieldReference?.Dereference(TargetDocContext)?.GetValue(TargetDocContext) as RichTextModel.RTD;
        }

        string GetSelected()
        {
            var parentDoc = this.GetFirstAncestorOfType<DocumentView>();
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
                var doc = parentDoc.ViewModel.DocumentController;
                return doc.GetActiveLayout() ?? doc;
            }
            return null;
        }

        private string GetRtfText()
        {
            string allRtfText;
            xRichEditBox.Document.GetText(TextGetOptions.FormatRtf, out allRtfText);
            var newtext = allRtfText.Replace("\r\n\\pard\\tx720\\par\r\n", ""); // RTF editor adds a trailing extra paragraph when queried -- need to strip that off
            return newtext;
        }
        #endregion

        #region load/unload
        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            UnLoaded(sender, routedEventArgs); // make sure we're not adding handlers twice
            
            xRichEditBox.SelectionHighlightColorWhenNotFocused = highlightNotFocused;

            // Set up dictionaries and bindings to set up rich text formatting functionalities 
            //SetUpEnumDictionaries();
            //SetFontSizeBinding();

            xRichEditBox.KeyUp += XRichEditBox_KeyUp;
            MainPage.Instance.AddHandler(PointerReleasedEvent, new PointerEventHandler(released), true);
            this.AddHandler(PointerPressedEvent, new PointerEventHandler(RichTextView_PointerPressed), true);
            this.AddHandler(TappedEvent, new TappedEventHandler(tapped), true);
            this.xRichEditBox.ContextMenuOpening += XRichEditBox_ContextMenuOpening;
            Scroll = this.GetFirstDescendantOfType<ScrollBar>();
            Scroll.LayoutUpdated += Scroll_LayoutUpdated;


            xFormattingMenuView.richTextView = this;
            xFormattingMenuView.xRichEditBox = xRichEditBox;
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
        #endregion

        #region DocView manipulation on right click

        /// <summary>
        /// Prevents the selecting of text when right mouse button is pressed so that the user can drag the view around
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RichTextView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _rightPressed = e.GetCurrentPoint(this).Properties.IsRightButtonPressed || Window.Current.CoreWindow
                                .GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            if (_rightPressed)
            {
                if (moveHdlr == null)
                    moveHdlr = RichTextView_PointerMoved;
                if (releasedHdlr == null)
                    releasedHdlr = RichTextView_PointerReleased;
                this.RemoveHandler(PointerReleasedEvent, releasedHdlr);
                this.AddHandler(PointerReleasedEvent, releasedHdlr, true);
                this.RemoveHandler(PointerMovedEvent, moveHdlr);
                this.AddHandler(PointerMovedEvent, moveHdlr, true);
                var docView = this.GetFirstAncestorOfType<DocumentView>();
                docView?.ToFront();
                var parent = this.GetFirstAncestorOfType<DocumentView>();
                var pointerPosition = MainPage.Instance
                    .TransformToVisual(parent.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(Windows.UI.Core
                        .CoreWindow.GetForCurrentThread().PointerPosition);
                _rightDragStartPosition = _rightDragLastPosition = pointerPosition;
                this.CapturePointer(e.Pointer);
                parent.ManipulationControls.ElementOnManipulationStarted(null, null);
                parent.DocumentView_PointerEntered(null, null);

            }
        }


        /// <summary>
        /// Move view around if right mouse button is held down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RichTextView_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var parent = this.GetFirstAncestorOfType<DocumentView>();
            var pointerPosition = MainPage.Instance.TransformToVisual(parent.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(CoreWindow.GetForCurrentThread().PointerPosition);
            var translation = new Point(pointerPosition.X - _rightDragLastPosition.X, pointerPosition.Y - _rightDragLastPosition.Y);
            _rightDragLastPosition = pointerPosition;
            parent.ManipulationControls.TranslateAndScale(new
                ManipulationDeltaData(new Point(pointerPosition.X, pointerPosition.Y),
                    translation,
                    1.0f), parent.ManipulationControls._grouping);

            //Only preview a snap if the grouping only includes the current node. TODO: Why is _grouping public?
            if(parent.ManipulationControls._grouping == null || parent.ManipulationControls._grouping.Count < 2) parent.ManipulationControls.Snap(true);
        }

        private void RichTextView_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            this.RemoveHandler(PointerReleasedEvent, releasedHdlr);
            this.RemoveHandler(PointerMovedEvent, moveHdlr);

            var parent = this.GetFirstAncestorOfType<DocumentView>();
            var pointerPosition = MainPage.Instance.TransformToVisual(parent.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition);

            //if (parent != null)
            //    parent.MoveToContainingCollection();
            if (_rightPressed)
            {
                var delta = new Point(pointerPosition.X - _rightDragStartPosition.X, pointerPosition.Y - _rightDragStartPosition.Y);
                var dist = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
                if (dist < 100)
                    parent.OnTapped(sender, new TappedRoutedEventArgs());
                else
                    parent.ManipulationControls.ElementOnManipulationCompleted(null, null);
                var dvm = parent.ViewModel;
                parent.DocumentView_PointerExited(null, null);
                parent.DocumentView_ManipulationCompleted(null, null);

            }
        }

        #endregion

        #region hyperlink

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

        #endregion

        #region focus

        /// <summary>
        /// Closes format options flyout and sets HasFocus to true (tab indents in this case instead of invoking the tab menu)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xRichEditBox_GotFocus(object sender, RoutedEventArgs e)
        {
            FlyoutBase.GetAttachedFlyout(xRichEditBox)?.Hide();
            HasFocus = true;
        }


        /// <summary>
        /// Sets HasFocus to false (allows tab to invoke tab menu when richeditbox does not have focus)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XRichEditBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            HasFocus = false;
            UpdateDocument();
        }

        #endregion

        #region search

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
        #endregion

        #endregion

        #region text formatting

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
            var shiftState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Shift)
                .HasFlag(CoreVirtualKeyStates.Down);

            if (shiftState && !e.Key.Equals(VirtualKey.Shift))
            {
                if (e.Key.Equals(VirtualKey.Enter))
                {
                    this.GetFirstAncestorOfType<DocumentView>().HandleShiftEnter();
                    xRichEditBox.Document.Selection.MoveStart(TextRangeUnit.Character, -1);
                    xRichEditBox.Document.Selection.Delete(TextRangeUnit.Character, 1);

                }
            }
            if (tabState)
            {
                xRichEditBox.Document.Selection.TypeText("\t");
                e.Handled = true;
            }
            if (ctrlState)
            {
                handleControlPressed(sender, e);
            }
            if (altState)
            {
                OpenContextMenu(sender);
            }

            if (ctrlState && shiftState && e.Key.Equals(VirtualKey.L))
            {
                if (xRichEditBox.Document.Selection.ParagraphFormat.ListType == MarkerType.None)
                {
                    xRichEditBox.Document.Selection.ParagraphFormat.ListType = MarkerType.Bullet;
                }
                else if (xRichEditBox.Document.Selection.ParagraphFormat.ListType == MarkerType.Bullet)
                {
                    xRichEditBox.Document.Selection.ParagraphFormat.ListType = MarkerType.None;
                }
            }
            if (!ctrlState && !altState)
            {
                var parent = this.GetFirstAncestorOfType<DocumentView>();
                parent.ViewModel.DocumentController.CaptureNeighboringContext();
            }
        }

        private void handleControlPressed(object sender, KeyRoutedEventArgs e)
        {
            var selection = xRichEditBox.Document.Selection;
            if (e.Key.Equals(VirtualKey.B))
            {
                _rtfHelper.Bold(true);
                //FormatToolTipInfo(xRichEditBox.Document.Selection);
            }
            else if (e.Key.Equals(VirtualKey.I))
            {
                _rtfHelper.Italicize(true);
                //FormatToolTipInfo(xRichEditBox.Document.Selection);
                e.Handled = true;
            }
            else if (e.Key.Equals(VirtualKey.U))
            {
                _rtfHelper.Underline(true);
                //FormatToolTipInfo(xRichEditBox.Document.Selection);
            }
            else if (e.Key.Equals(VirtualKey.F))
            {
                xSearchBoxPanel.Visibility = Visibility.Visible;
                xSearchBox.Focus(FocusState.Programmatic);
            }
            else if (e.Key.Equals(VirtualKey.N))
            {
                xRichEditBox.Document.Redo();
            }
            else if (e.Key.Equals(VirtualKey.H))
            {
                _rtfHelper.Highlight(Colors.Yellow, true);
                UpdateDocument();
            }
            else if (e.Key.Equals(VirtualKey.O))
            {
                OpenContextMenu(sender);
            }
        }

        private void xRichEditBox_SelectionChanged_1(object sender, RoutedEventArgs e)
        {

        }

        #region opening
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
            }
            //currentCharFormat = xRichEditBox.Document.Selection.CharacterFormat.GetClone();
            xRichEditBox.SelectionHighlightColorWhenNotFocused = highlightNotFocused;
        }



        private void XRichEditBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            e.Handled = true;

            var parent = this.GetFirstAncestorOfType<DocumentView>();
            parent?.OnTapped(null, null);
        }
        #endregion

        #endregion
        
        #region commented out code
    /// <summary>
    /// Sets up and shows tooltip, which lists some main formatting properties of the current selection
    /// </summary>
    /// <param name="range"></param>
    //private async void FormatToolTipInfo(ITextRange range)
    //{
    //    // TODO: show tooltip at position of the mouse (horizontal & vertical offset (alone) do not work)
    //    xFormatTipText.Inlines.Clear();
    //    AddPropertyRun("Alignment", range.ParagraphFormat.Alignment.ToString());
    //    AddPropertyRun("Font", range.CharacterFormat.Name);
    //    AddPropertyRun("Size", range.CharacterFormat.Size.ToString(CultureInfo.InvariantCulture));
    //    AddPropertyRun("Bold", range.CharacterFormat.Bold.ToString());
    //    AddPropertyRun("Italic", range.CharacterFormat.Italic.ToString());
    //    AddPropertyRun("Underline", range.CharacterFormat.Underline.ToString());
    //    AddPropertyRun("Strikethrough", range.CharacterFormat.Strikethrough.ToString());
    //    AddPropertyRun("Superscript", range.CharacterFormat.Superscript.ToString());
    //    AddPropertyRun("Subscript", range.CharacterFormat.Subscript.ToString());
    //    AddPropertyRun("ListType", range.ParagraphFormat.ListType.ToString());
    //    // show tooltip at mouse position, does not work
    //    //xFormatTip.Placement = PlacementMode.Mouse;
    //    //Point point;
    //    //xRichEditBox.Document.Selection.GetPoint(HorizontalCharacterAlignment.Left, VerticalCharacterAlignment.Baseline, PointOptions.ClientCoordinates, out point);
    //    //xFormatTip.HorizontalOffset = point.X;
    //    //xFormatTip.VerticalOffset = -point.Y;
    //    //xFormatTip.PlacementTarget = xRichEditBox;
    //    xFormatTip.Placement = PlacementMode.Left;
    //    xFormatTip.IsOpen = false; //TO use the tool tip again, set this to true
    //                               //await Task.Delay(3000);
    //    if (!isFlyoutOpen)
    //        xFormatTip.IsOpen = false;
    //}

    ///// <summary>
    ///// Adds format property to the tooltip if it is not None, Undefined, or Off
    ///// </summary>
    ///// <param name="key"></param>
    ///// <param name="value"></param>
    //private void AddPropertyRun(string key, string value)
    //{
    //    if (!value.Equals("None") && !value.Equals("Undefined") && !value.Equals("Off"))
    //    {
    //        var keyRun = new Run();
    //        keyRun.Text = key + ": ";
    //        var valRun = new Run();
    //        if (value.StartsWith("-"))
    //            valRun.Text = "-";
    //        else
    //            valRun.Text = value;
    //        var lineBreak = new LineBreak();
    //        xFormatTipText.Inlines.Add(keyRun);
    //        xFormatTipText.Inlines.Add(valRun);
    //        xFormatTipText.Inlines.Add(lineBreak);
    //    }
    //}
    #endregion
    }
}



