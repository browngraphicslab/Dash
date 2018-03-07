﻿using Dash.Controllers.Operators;
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
using Dash.Models.DragModels;

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

            TextChangedCallbackToken = RegisterPropertyChangedCallback(TextProperty, TextChangedCallback);
            xRichEditBox.AddHandler(KeyDownEvent, new KeyEventHandler(XRichEditBox_OnKeyDown), true);

            _rtfHelper = new RichTextFormattingHelper(this, xRichEditBox);

            //xRichEditBox.Document.Selection.CharacterFormat.Name = "Calibri";
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
            if (this.IsInVisualTree() && CanSizeToFit)
            {
                xRichEditBox.Measure(new Size(xRichEditBox.ActualWidth, 1000));
                var relative = this.GetFirstAncestorOfType<RelativePanel>();
                if (relative != null)
                    relative.Height = Math.Max(ActualHeight, xRichEditBox.DesiredSize.Height);
            }
        }

        private void TextChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            var reg = new Regex("\\\\par[\r\n}\\\\]*\0");
            var newstr = reg.Replace(Text.RtfFormatString, "}\r\n\0");
            xRichEditBox.Document.SetText(TextSetOptions.FormatRtf, newstr);
            var selected = GetSelected();
            if (selected != null)
            {
                prevQueryLength = selected.Length;
                var selectionFound = xRichEditBox.Document.Selection.FindText(selected, 100000, FindOptions.None);

                var s = xRichEditBox.Document.Selection.StartPosition;
                originalCharFormat.Add(s, this.xRichEditBox.Document.Selection.CharacterFormat.GetClone());
                this.xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = Colors.Yellow;
                this.xRichEditBox.Document.Selection.CharacterFormat.Bold = FormatEffect.On;
                UpdateDocument();
            }
            xRichEditBox.Document.Selection.SetRange(LastS1, LastS2);
        }

        public void UpdateDocument()
        {
            if (DataContext == null || Text == null)
                return;
            string allText;
            xRichEditBox.Document.GetText(TextGetOptions.UseObjectText, out allText);
            string allRtfText = GetRtfText();
            UnregisterPropertyChangedCallback(TextProperty, TextChangedCallbackToken);
            if (!allRtfText.Equals(Text.RtfFormatString))
                Text = new RichTextModel.RTD(allRtfText);  // RTF editor adds a trailing extra paragraph when queried -- need to strip that off
            TextChangedCallbackToken = RegisterPropertyChangedCallback(TextProperty, TextChangedCallback);


            var parentDoc = this.GetFirstAncestorOfType<DocumentView>();
            var reg = new Regex("[a-zA-Z 0-9]*:[a-zA-Z 0-9'_,;{}+-=()*&!?@#$%<>]*");
            var matches = reg.Matches(allText);
            foreach (var str in matches)
            {
                var split = str.ToString().Split(':');
                var key = split.FirstOrDefault().Trim(' ');
                var value = split.LastOrDefault().Trim(' ');

                var k = KeyController.LookupKeyByName(key);
                if (k == null)
                    k = new KeyController(DashShared.UtilShared.GenerateNewId(), key);

                _parentDataDocument.SetField(k, new TextController(value), true);
            }
            // var rest = reg.Replace(allText, "");
            _parentDataDocument.SetField(KeyStore.DocumentTextKey, new TextController(allText), true);
        }


        #region eventhandlers
        private void Scroll_LayoutUpdated(object sender, object e)
        {
            if (Scroll.Visibility == Visibility.Visible)
                released(null, null);
        }

        private void tapped(object sender, TappedRoutedEventArgs e)
        {
            var ctrlDown = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            string target = null;
            var s1 = xRichEditBox.Document.Selection.StartPosition;
            var s2 = xRichEditBox.Document.Selection.EndPosition;
            if (s1 == s2)
            {
                xRichEditBox.Document.Selection.SetRange(s1, s2 + 1);
            }
            if (xRichEditBox.Document.Selection.Link.Length > 1)
            {
                target = xRichEditBox.Document.Selection.Link.Split('\"')[1];
            }
            if (xRichEditBox.Document.Selection.EndPosition != s2)
                xRichEditBox.Document.Selection.SetRange(s1, s2);

            if (target != null)
            {
                var doc = GetDoc();
                var point = doc.GetPositionField().Data;

                var theDoc = ContentController<FieldModel>.GetController<DocumentController>(target);

                var collection = this.GetFirstAncestorOfType<CollectionView>();
                var contextDoc = theDoc.GetDereferencedField<DocumentController>(KeyStore.DocumentContextKey, null);
                if (theDoc.DocumentType.Equals(DataBox.DocumentType) && contextDoc != null)
                {
                    var pt = point;
                    pt.X += doc.GetField<NumberController>(KeyStore.ActualWidthKey)?.Data ?? 150.0;
                    pt.X += 10;
                    pt.Y += 0;
                    
                    var menuFlyout = new MenuFlyout() { Placement = FlyoutPlacementMode.Bottom };
                    var menuFlyoutItem = new MenuFlyoutItem() { Text = theDoc.GetDereferencedField<TextController>(KeyStore.DataKey, null)?.Data ?? "<>" };
                    menuFlyoutItem.Click += (s, a) => Actions.DisplayDocument(collection.ViewModel, contextDoc.GetDataDocument(null).GetKeyValueAlias(pt));
                    menuFlyout.Items.Add(menuFlyoutItem);
                    menuFlyout.Closed += (s, a) => this.ContextFlyout = null;
                    this.ContextFlyout = menuFlyout;
                    this.ContextFlyout.ShowAt(this);
                }
                else
                {
                    var nearest = FindNearestDisplayedTarget(e.GetPosition(MainPage.Instance), ContentController<FieldModel>.GetController<DocumentController>(target), ctrlDown);
                    if (nearest != null)
                    {
                        if (ctrlDown)
                            nearest.DeleteDocument();
                        else MainPage.Instance.NavigateToDocumentInWorkspace(nearest.ViewModel.DocumentController);
                        return;
                    }

                    if (theDoc != null && !theDoc.Equals(DBTest.DBNull))
                    {
                        var pt = point;
                        pt.X += doc.GetField<NumberController>(KeyStore.ActualWidthKey)?.Data ?? 150.0;
                        pt.X += 10;
                        pt.Y += 0;
                        var api = theDoc.GetDereferencedField<TextController>(KeyStore.AbstractInterfaceKey, null)?.Data;
                        if (api == CollectionNote.APISignature)
                            theDoc = new CollectionNote(theDoc, pt, CollectionView.CollectionViewType.Schema, 200, 100).Document;
                        if (collection != null)
                        {
                            Actions.DisplayDocument(collection.ViewModel, theDoc.GetViewCopy(pt));
                        }
                    }
                    else if (target.StartsWith("http"))
                    {
                        theDoc = DocumentController.FindDocMatchingPrimaryKeys(new string[] { target });
                        if (theDoc != null && theDoc != DBTest.DBNull)
                        {
                            var pt = point;
                            pt.X -= 150;
                            pt.Y -= 50;
                            if (collection != null)
                            {
                                Actions.DisplayDocument(collection.ViewModel, theDoc.GetViewCopy(pt));
                            }
                        }
                        else
                        {
                            if (MainPage.Instance.WebContext != null)
                                MainPage.Instance.WebContext.SetUrl(target);
                            else
                            {
                                var pt = point;
                                pt.X += doc.GetField<NumberController>(KeyStore.ActualWidthKey)?.Data ?? 150.0;
                                pt.X += 10;
                                pt.Y += 0;
                                var page = new HtmlNote(target, target, pt).Document;
                                Actions.DisplayDocument(collection.ViewModel, page);
                            }
                        }
                    }
                }
                this.xRichEditBox.Document.Selection.SetRange(this.xRichEditBox.Document.Selection.StartPosition, this.xRichEditBox.Document.Selection.StartPosition);
            }
        }

        private DocumentView FindNearestDisplayedTarget(Point where, DocumentController target, bool onlyOnPage=true)
        {
            double dist = double.MaxValue;
            DocumentView nearest = null;
            var targetData = target?.GetDataDocument(null);
            foreach (var presenter in (this.GetFirstAncestorOfType<CollectionView>().CurrentView as CollectionFreeformView).xItemsControl.ItemsPanelRoot.Children.Select((c) => (c as ContentPresenter)))
            {
                var dvm = presenter.GetFirstDescendantOfType<DocumentView>();
                if (dvm.ViewModel.DataDocument.GetId().ToString() == targetData?.Id)
                {
                    var mprect = dvm.GetBoundingRect(MainPage.Instance);
                    var center = new Point((mprect.Left + mprect.Right) / 2, (mprect.Top + mprect.Bottom) / 2);
                    if (!onlyOnPage || MainPage.Instance.GetBoundingRect().Contains(center))
                    {
                        var d = Math.Sqrt((where.X - center.X) * (where.X - center.X) + (where.Y - center.Y) * (where.Y - center.Y));
                        if (d < dist)
                        {
                            d = dist;
                            nearest = dvm;
                        }
                    }
                }
            }

            return nearest;
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
            e.Handled = true;
            return;
            string allText;
            xRichEditBox.Document.GetText(TextGetOptions.UseObjectText, out allText);

            var s1 = this.xRichEditBox.Document.Selection.StartPosition;
            var s2 = this.xRichEditBox.Document.Selection.EndPosition;

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

                        var startPt = new Point();
                        try
                        {
                            this.xRichEditBox.Document.Selection.GetPoint(HorizontalCharacterAlignment.Center, VerticalCharacterAlignment.Baseline, PointOptions.Start, out startPt);
                        }
                        catch (Exception exception)
                        {
                            Debug.WriteLine(exception);
                        }
                        createRTFHyperlink(theDoc, startPt, ref s1, ref s2, lastTypedCharacter == "^", false);
                    }
                }
            }

            this.xRichEditBox.Document.Selection.SetRange(s1, s2);
        }

        private async void released(object sender, PointerRoutedEventArgs e)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () => SizeToFit());
        }
        
        private async void xRichEditBox_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            DocumentController theDoc = null;
            if (e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)))
            {
                var dragModel = (DragDocumentModel)e.DataView.Properties[nameof(DragDocumentModel)];
                theDoc = dragModel.GetDropDocument(new Point(), true);
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
            
            string allRtfText = GetRtfText();
            UnregisterPropertyChangedCallback(TextProperty, TextChangedCallbackToken);
            Text = new RichTextModel.RTD(allRtfText);
            TextChangedCallbackToken = RegisterPropertyChangedCallback(TextProperty, TextChangedCallback);

            xRichEditBox.Document.Selection.SetRange(s1, s2);
            e.Handled = true;
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
                return parentDoc.ViewModel?.DataDocument?.GetDereferencedField<TextController>(CollectionDBView.SelectedKey, null)?.Data ??
                       parentDoc.ViewModel?.LayoutDocument?.GetDereferencedField<TextController>(CollectionDBView.SelectedKey, null)?.Data;
            }
            return null;
        }
        void SetSelected(string query)
        {
            var parentDoc = this.GetFirstAncestorOfType<DocumentView>();
            if (parentDoc != null)
            {
                parentDoc.ViewModel?.DataDocument?.SetField(CollectionDBView.SelectedKey, new TextController(query), true);
            }
        }

        DocumentController GetDoc()
        {
            var parentDoc = this.GetFirstAncestorOfType<DocumentView>();
            if (parentDoc != null)
            {
                return parentDoc.ViewModel.LayoutDocument;
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
        PointerEventHandler _releasedHdlr = null;
        PointerEventHandler _pressedHdlr = null;
        TappedEventHandler _tappedHdlr = null;
        DocumentView _parentDocView = null;
        DocumentController _parentDataDocument = null;
        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            _parentDocView = this.GetFirstAncestorOfType<DocumentView>();
            _parentDataDocument = _parentDocView.ViewModel.DataDocument;
            _releasedHdlr = new PointerEventHandler(released);
            _pressedHdlr = new PointerEventHandler(RichTextView_PointerPressed);
            _tappedHdlr = new TappedEventHandler(tapped);

            UnLoaded(sender, routedEventArgs); // make sure we're not adding handlers twice
            
            xRichEditBox.SelectionHighlightColorWhenNotFocused = highlightNotFocused;

            // Set up dictionaries and bindings to set up rich text formatting functionalities 
            //SetUpEnumDictionaries();
            //SetFontSizeBinding();

            xRichEditBox.KeyUp += XRichEditBox_KeyUp;
            MainPage.Instance.AddHandler(PointerReleasedEvent, _releasedHdlr, true);
            this.AddHandler(PointerPressedEvent, _pressedHdlr, true);
            this.AddHandler(TappedEvent, _tappedHdlr, true);
            this.xRichEditBox.ContextMenuOpening += XRichEditBox_ContextMenuOpening;
            Scroll = this.GetFirstDescendantOfType<ScrollBar>();
            Scroll.LayoutUpdated += Scroll_LayoutUpdated;
            _parentDataDocument.AddFieldUpdatedListener(CollectionDBView.SelectedKey, selectedFieldChanged);
            xFormattingMenuView.richTextView = this;
            xFormattingMenuView.xRichEditBox = xRichEditBox;
            Unloaded -= UnLoaded;
            Unloaded += UnLoaded;
        }

        void selectedFieldChanged(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            MatchQuery(GetSelected());
        }



        /// <summary>
        /// Unsubscribes TextChanged handler on Unload
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnLoaded(object sender, RoutedEventArgs e)
        {
            xRichEditBox.KeyUp -= XRichEditBox_KeyUp;
            MainPage.Instance.RemoveHandler(PointerReleasedEvent, _releasedHdlr);
            this.RemoveHandler(PointerPressedEvent, _pressedHdlr);
            this.RemoveHandler(TappedEvent, _tappedHdlr);
            this.xRichEditBox.ContextMenuOpening -= XRichEditBox_ContextMenuOpening;

            _parentDataDocument.RemoveFieldUpdatedListener(CollectionDBView.SelectedKey, selectedFieldChanged);
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
            var rightPressed = e.GetCurrentPoint(this).Properties.IsRightButtonPressed || Window.Current.CoreWindow
                                .GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            if (rightPressed)
            {
               new ManipulationControlHelper(this, e.Pointer, (e.KeyModifiers & VirtualKeyModifiers.Shift) != 0);
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

                if (xRichEditBox.Document.Selection.Link != link)
                {
                    if (xRichEditBox.Document.Selection.StartPosition == xRichEditBox.Document.Selection.EndPosition)
                    {
                        xRichEditBox.Document.Selection.SetText(TextSetOptions.None, theDoc.Title);
                    }

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
            var query = args.QueryText;
            SetSelected(query);
        }

        private void MatchQuery(string query)
        {
            this.ClearSearchHighlights();
            nextMatch = 0;
            prevQueryLength = query == null ? 0 : query.Length;
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
            xRichEditBox.Document.Selection.StartPosition = 0;
            xRichEditBox.Document.Selection.EndPosition = 0;
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

                this.xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = Colors.Transparent;
            }
            UpdateDocument();
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
            if (e.Key.Equals(VirtualKey.B))
            {
                //xRichEditBox.Document.Selection.CharacterFormat.Bold = xRichEditBox.Document.Selection.CharacterFormat.Bold == FormatEffect.On ? FormatEffect.Off : FormatEffect.On;
                //UpdateDocument();
            }
            else if (e.Key.Equals(VirtualKey.I))
            {
                //xRichEditBox.Document.Selection.CharacterFormat.Italic = xRichEditBox.Document.Selection.CharacterFormat.Bold == FormatEffect.On ? FormatEffect.Off : FormatEffect.On;
                //UpdateDocument();
            }
            else if (e.Key.Equals(VirtualKey.U))
            {
                //xRichEditBox.Document.Selection.CharacterFormat.Underline = xRichEditBox.Document.Selection.CharacterFormat.Underline == UnderlineType.None ? UnderlineType.Single : UnderlineType.None;
                //UpdateDocument();
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

            e.Handled = true;
        }

        private void xSearchDelete_Click(object sender, RoutedEventArgs e)
        {
            SetSelected("");
            //ClearSearchHighlights();
            xSearchBoxPanel.Visibility = Visibility.Collapsed;
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
            parent?.DocumentView_OnTapped(null, null);
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

        private void XRichEditBox_OnTextChanged(object sender, RoutedEventArgs e)
        {
            UpdateDocument();
        }
    }
}



