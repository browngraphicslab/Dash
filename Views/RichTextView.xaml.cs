using Dash.Models.DragModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using DashShared;
using static Dash.FieldControllerBase;
using TextWrapping = Windows.UI.Xaml.TextWrapping;
using Visibility = Windows.UI.Xaml.Visibility;
using Windows.UI.Xaml.Data;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236
namespace Dash
{
    public sealed partial class RichTextView : UserControl
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(RichTextModel.RTD), typeof(RichTextView), new PropertyMetadata(default(RichTextModel.RTD)));
        public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(
            "TextWrapping", typeof(TextWrapping), typeof(RichTextView), new PropertyMetadata(default(TextWrapping)));
        
        int   _prevQueryLength;// The length of the previous search query
        int   _nextMatch = 0;// Index of the next highlighted search result
        FormattingMenuView xFormattingMenuView = null;

        /// <summary>
        /// A dictionary of the original character formats of all of the highlighted search results
        /// </summary>
        Dictionary<int, ITextCharacterFormat> _originalCharFormat = new Dictionary<int, ITextCharacterFormat>();

        private int NoteFontSize => SettingsView.Instance.NoteFontSize;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public RichTextView()
        {
            this.InitializeComponent();
            Loaded += OnLoaded;

            AddHandler(PointerPressedEvent, new PointerEventHandler((object s, PointerRoutedEventArgs e) =>
            {
                if (e.IsRightPressed() || this.IsCtrlPressed())// Prevents the selecting of text when right mouse button is pressed so that the user can drag the view around
                    new ManipulationControlHelper(this, e.Pointer, (e.KeyModifiers & VirtualKeyModifiers.Shift) != 0);
            }), true);
            AddHandler(TappedEvent, new TappedEventHandler(xRichEditBox_Tapped), true);

            xSearchDelete.Click += (s, e) =>
            {
                setSelected("");
                xSearchBoxPanel.Visibility = Visibility.Collapsed;
            };

            xSearchBox.QuerySubmitted += (s,e) => NextResult(); // Selects the next highlighted search result on enter in the xRichEditBox

            xSearchBox.QueryChanged += (s,e) => setSelected(e.QueryText);// Searches content of the xRichEditBox, highlights all results

            xRichEditBox.AddHandler(KeyDownEvent, new KeyEventHandler(XRichEditBox_OnKeyDown), true);

            xRichEditBox.Drop += (s, e) =>
            {
                e.Handled = true;
                xRichEditBox_Drop(s, e);
                this.GetFirstAncestorOfType<DocumentView>()?.This_DragLeave(null, null); // bcz: rich text Drop's don't bubble to parent docs even if they are set to grab handled events
            };

            PointerWheelChanged += (s, e) => e.Handled = true;
            xRichEditBox.GotFocus += (s, e) =>  FlyoutBase.GetAttachedFlyout(xRichEditBox)?.Hide(); // close format options

            xRichEditBox.TextChanged += (s, e) => UpdateDocumentFromXaml();

            xRichEditBox.KeyUp += (s, e) => {
                if (e.Key == VirtualKey.Back && (string.IsNullOrEmpty(getReadableText())))
                    getDocView().DeleteDocument(true);
                e.Handled = true;
            };

            xRichEditBox.ContextMenuOpening += (s,e) => e.Handled = true; // suppresses the Cut, Copy, Paste, Undo, Select All context menu from the native view

            xRichEditBox.SelectionHighlightColorWhenNotFocused = new SolidColorBrush(Colors.Gray) { Opacity = 0.5 };

            var sizeBinding = new Binding
            {
                Source = SettingsView.Instance,
                Path = new PropertyPath(nameof(SettingsView.Instance.NoteFontSize)),
                Mode = BindingMode.OneWay
            };
            xRichEditBox.SetBinding(FontSizeProperty, sizeBinding); 

        }

        public void UpdateDocumentFromXaml()
        {
            if (DataContext != null && Text != null)
            {
                convertTextFromXamlRTF();
                setContainerHeight();

                // auto-generate key/value pairs by scanning the text
                var reg = new Regex("[a-zA-Z 0-9]*:=[a-zA-Z 0-9'_,;{}+-=()*&!?@#$%<>]*");
                var matches = reg.Matches(getReadableText());
                foreach (var str in matches)
                {
                    var split = str.ToString().Split(":=");
                    var key = split.FirstOrDefault().Trim(' ');
                    var value = split.LastOrDefault().Trim(' ');

                    var keycontroller = KeyController.LookupKeyByName(key, true);
                    var containerDoc = this.GetFirstAncestorOfType<CollectionView>()?.ViewModel;
                    if (containerDoc != null)
                    {
                        var containerData = containerDoc.ContainerDocument.GetDataDocument();
                        containerData.SetField(keycontroller, new RichTextController(new RichTextModel.RTD(value)), true);
                        var where = getLayoutDoc().GetPositionField()?.Data ?? new Point();
                        var dbox = new DataBox(new DocumentReferenceController(containerData.Id, keycontroller), where.X, where.Y).Document;
                        dbox.SetField(KeyStore.DocumentContextKey, containerData, true);
                        dbox.SetField(KeyStore.TitleKey, new TextController(keycontroller.Name), true);
                        containerDoc.AddDocument(dbox, null);
                        //DataDocument.SetField(KeyStore.DataKey, new DocumentReferenceController(containerData.Id, keycontroller), true);
                    }
                }
            }
        }
        public RichTextModel.RTD   Text
        {
            get { return (RichTextModel.RTD)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        public DocumentController  DataDocument { get; set; }
        public DocumentController  LayoutDocument { get; set; }
        DocumentView       getDocView() { return this.GetFirstAncestorOfType<DocumentView>(); }
        DocumentController getLayoutDoc() { return getDocView()?.ViewModel.LayoutDocument; }
        DocumentController getDataDoc() { return getDocView()?.ViewModel.DataDocument; }
        string             getSelected()
        {
            return getDataDoc()?.GetDereferencedField<TextController>(CollectionDBView.SelectedKey, null)?.Data ??
                   getLayoutDoc()?.GetDereferencedField<TextController>(CollectionDBView.SelectedKey, null)?.Data;
        }
        void               setSelected(string query)
        {
            getDataDoc().SetField(CollectionDBView.SelectedKey, new TextController(query), true);
        }
        string             getReadableText()
        {
            string allText;
            xRichEditBox.Document.GetText(TextGetOptions.UseObjectText, out allText);
            return allText;
        }
        string             getRtfText()
        {
            string allRtfText;
            xRichEditBox.Document.GetText(TextGetOptions.FormatRtf, out allRtfText);
            var strippedRtf = allRtfText.Replace("\r\n\\pard\\tx720\\par\r\n", ""); // RTF editor adds a trailing extra paragraph when queried -- need to strip that off
            return new Regex("\\\\par[\r\n}\\\\]*\0").Replace(strippedRtf, "}\r\n\0");
        }
        /// <summary>
        /// Retrieves the Xaml RTF of this view and stores it in the Dash document's Text field model.
        /// </summary>
        void               convertTextFromXamlRTF()
        {
            var xamlRTF = getRtfText();
            if (!xamlRTF.Equals(_lastXamlRTFText))  // don't update if the Text is the same as what we last set it to
                Text = new RichTextModel.RTD(xamlRTF);
            _lastXamlRTFText = xamlRTF;
        }
        void               setContainerHeight()
        {
            if (FocusManager.GetFocusedElement() == xRichEditBox)
            {
                if (Parent is RelativePanel relative)
                {
                    if (xRichEditBox.TextWrapping == TextWrapping.NoWrap)
                        LayoutDocument.SetField(KeyStore.TextWrappingKey, new TextController(TextWrapping.Wrap.ToString()), true);
                    xRichEditBox.Measure(new Size(ActualWidth, 1000));
                    if (relative != null)
                    {
                        var pad = relative.Children.OfType<FrameworkElement>().Where((ele) => ele != this).Aggregate(0.0, (val, ele) => val + ele.ActualHeight);
                        relative.Height = xRichEditBox.DesiredSize.Height + pad;
                    }
                }
                else
                    Height = double.NaN;
            }
        }

        #region eventhandlers
        string _lastXamlRTFText = "";
        void xRichTextView_TextChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            if (FocusManager.GetFocusedElement() != xRichEditBox && Text != null)
            {
                if (Text.RtfFormatString != _lastXamlRTFText)
                {
                    xRichEditBox.Document.SetText(TextSetOptions.FormatRtf, Text.RtfFormatString); // setting the RTF text does not mean that the Xaml view will literally store an identical RTF string to what we passed
                    _lastXamlRTFText = getRtfText(); // so we need to retrieve what Xaml actually stored and treat that as an 'alias' for the format string we used to set the text.
                }
                if (getSelected() is string selected)
                {
                    _prevQueryLength = selected.Length;
                    var selectionFound = xRichEditBox.Document.Selection.FindText(selected, 100000, FindOptions.None);

                    var s = xRichEditBox.Document.Selection.StartPosition;
                    _originalCharFormat.Add(s, xRichEditBox.Document.Selection.CharacterFormat.GetClone());
                    this.xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = Colors.Yellow;
                    this.xRichEditBox.Document.Selection.CharacterFormat.Bold = FormatEffect.On;
                }
            }
        }
        void xRichEditBox_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var target = getHyperlinkTargetForSelection();
            if (target != null)
            {
                var theDoc = ContentController<FieldModel>.GetController<DocumentController>(target);
                var nearest = FindNearestDisplayedTarget(e.GetPosition(MainPage.Instance), theDoc?.GetDataDocument(), this.IsCtrlPressed());
                if (nearest != null && !nearest.Equals(this.GetFirstAncestorOfType<DocumentView>()))
                {
                    if (this.IsCtrlPressed())
                        nearest.DeleteDocument();
                    else MainPage.Instance.NavigateToDocumentInWorkspace(nearest.ViewModel.DocumentController);
                }
                else
                {
                    var pt = new Point(getDocView().ViewModel.XPos + getDocView().ActualWidth, getDocView().ViewModel.YPos);
                    if (theDoc != null)
                    {
                        Actions.DisplayDocument(this.GetFirstAncestorOfType<CollectionView>()?.ViewModel, theDoc.GetViewCopy(pt));
                    }
                    else if (target.StartsWith("http"))
                    {
                        if (MainPage.Instance.WebContext != null)
                            MainPage.Instance.WebContext.SetUrl(target);
                        else
                        {
                            Actions.DisplayDocument(this.GetFirstAncestorOfType<CollectionView>()?.ViewModel, theDoc);
                        }
                    }
                }
            }
            DocumentView FindNearestDisplayedTarget(Point where, DocumentController targetData, bool onlyOnPage = true)
            {
                double dist = double.MaxValue;
                DocumentView nearest = null;
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
        }

        async void xRichEditBox_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)))
            {
                linkDocumentToSelection(((DragDocumentModel)e.DataView.Properties[nameof(DragDocumentModel)]).GetDropDocument(new Point(), true), true);
            }
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                linkDocumentToSelection(await FileDropHelper.GetDroppedFile(e), false);
            }

            e.Handled = true;
        }
        /// <summary>
        /// Create short cuts for the xRichEditBox (ctrl+I creates indentation by default, ctrl-Z will get rid of the indentation, showing only the italized text)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void XRichEditBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!this.IsCtrlPressed() && !this.IsAltPressed() && !e.Key.Equals(VirtualKey.Shift))
            {
                getDataDoc().CaptureNeighboringContext();
            }
            else if (this.IsShiftPressed() && !e.Key.Equals(VirtualKey.Shift) && e.Key.Equals(VirtualKey.Enter))
            {
                xRichEditBox.Document.Selection.MoveStart(TextRangeUnit.Character, -1);
                xRichEditBox.Document.Selection.Delete(TextRangeUnit.Character, 1);
                getDocView().HandleShiftEnter();
            }
            else if (this.IsCtrlPressed() && !e.Key.Equals(VirtualKey.Control) && e.Key.Equals(VirtualKey.Enter))
            {
                xRichEditBox.Document.Selection.MoveStart(TextRangeUnit.Character, -1);
                xRichEditBox.Document.Selection.Delete(TextRangeUnit.Character, 1);
                getDocView().HandleCtrlEnter();
            }
			
			/**
			else if (this.IsAltPressed()) // opens the format options flyout 
            {
				if (xFormattingMenuView == null)
                {
                    xFormattingMenuView = new FormattingMenuView();
                    // store a clone of character format after initialization as default format
                    xFormattingMenuView.defaultCharFormat = xRichEditBox.Document.Selection.CharacterFormat.GetClone();
                    // store a clone of paragraph format after initialization as default format
                    xFormattingMenuView.defaultParFormat = xRichEditBox.Document.Selection.ParagraphFormat.GetClone();
                    xFormattingMenuView.richTextView = this;
                    xFormattingMenuView.xRichEditBox = xRichEditBox;
                    xAttachedFlyout.Children.Add(xFormattingMenuView);
                }
                FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
                FlyoutBase.GetAttachedFlyout(sender as FrameworkElement)?.ShowAt(sender as FrameworkElement);
            }
	*/
		
            else if (this.IsTabPressed())
            {
                xRichEditBox.Document.Selection.TypeText("\t");
                e.Handled = true;
            }
            else if (this.IsCtrlPressed())   // ctrl-B, ctrl-I, ctrl-U handled natively by the text editor
            {
                switch (e.Key)
                {
                    case VirtualKey.N:
                        xRichEditBox.Document.Redo();
                        break;
                    case VirtualKey.H:
                        this.Highlight(Colors.Yellow, true); // using RichTextFormattingHelper extenions
                        break;
                    case VirtualKey.F:
                        xSearchBoxPanel.Visibility = Visibility.Visible;
                        xSearchBox.Focus(FocusState.Programmatic);
                        break;
                    case VirtualKey.L:
                        if (this.IsShiftPressed())
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
                        break;
                }
            }
        }

        #endregion

        #region load/unload
        void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            var selectedFieldUpdatedHdlr = new FieldUpdatedHandler((s, e, c) => MatchQuery(getSelected()));
            DataDocument.AddFieldUpdatedListener(CollectionDBView.SelectedKey, selectedFieldUpdatedHdlr);
            var id = RegisterPropertyChangedCallback(TextProperty, xRichTextView_TextChangedCallback);

            void UnLoaded(object s, RoutedEventArgs e)
            {
                UnregisterPropertyChangedCallback(TextProperty, id);
                DataDocument.RemoveFieldUpdatedListener(CollectionDBView.SelectedKey, selectedFieldUpdatedHdlr);
                Unloaded -= UnLoaded;
            }

            Unloaded += UnLoaded;
        }

        #endregion

        #region hyperlink

        string getHyperlinkTargetForSelection()
        {
            var s1 = xRichEditBox.Document.Selection.StartPosition;
            var s2 = xRichEditBox.Document.Selection.EndPosition;
            if (s1 == s2)
            {
                xRichEditBox.Document.Selection.SetRange(s1, s2 + 1);
            }

            string target = xRichEditBox.Document.Selection.Link.Length > 1 ? xRichEditBox.Document.Selection.Link.Split('\"')[1] : null;

            if (xRichEditBox.Document.Selection.EndPosition != s2)
                xRichEditBox.Document.Selection.SetRange(s1, s2);
            return target;
        }

        void linkDocumentToSelection(DocumentController theDoc, bool forceLocal)
        {
            var s1 = this.xRichEditBox.Document.Selection.StartPosition;
            var s2 = this.xRichEditBox.Document.Selection.EndPosition;

            if (theDoc != null)
                createRTFHyperlink(theDoc, ref s1, ref s2, false, forceLocal);

            convertTextFromXamlRTF();

            xRichEditBox.Document.Selection.SetRange(s1, s2);
        }

        void createRTFHyperlink(DocumentController theDoc, ref int s1, ref int s2, bool createIfNeeded, bool forceLocal)
        {
            Point startPt;
            this.xRichEditBox.Document.Selection.GetPoint(HorizontalCharacterAlignment.Center, VerticalCharacterAlignment.Baseline, PointOptions.Start, out startPt);
            string link = "\"" + theDoc.GetId() + "\"";
            if (!forceLocal && theDoc.GetDataDocument().DocumentType.Equals(HtmlNote.DocumentType) && (bool)theDoc.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DataKey, null)?.Data?.StartsWith("http"))
            {
                link = "\"" + theDoc.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DataKey, null).Data + "\"";
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
        
        #region search

        private void MatchQuery(string query)
        {
            this.ClearSearchHighlights();
            _nextMatch = 0;
            _prevQueryLength = query == null ? 0 : query.Length;
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
                    _originalCharFormat.Add(s, selectedText.CharacterFormat.GetClone());
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
        /// Selects the next highlighted search result on enter in the xRichEditBox
        /// </summary>
        private void NextResult()
        {
            var keys = _originalCharFormat.Keys;
            if (keys.Count != 0)
            {
                var start = keys.ElementAt(_nextMatch);
                xRichEditBox.Document.Selection.StartPosition = start;
                xRichEditBox.Document.Selection.EndPosition = start + _prevQueryLength;
                xRichEditBox.Document.Selection.ScrollIntoView(PointOptions.None);
                if (_nextMatch < keys.Count - 1)
                    _nextMatch++;
                else
                    _nextMatch = 0;
            }
        }

        /// <summary>
        /// Clears the highlights that result from searching within the xRichEditBox (to make sure that
        /// original highlights wouldn't get erased)
        /// </summary>
        private void ClearSearchHighlights()
        {
            xRichEditBox.SelectionHighlightColorWhenNotFocused = new SolidColorBrush(Colors.Transparent);
            var keys = _originalCharFormat.Keys;
            foreach (var key in keys)
            {
                xRichEditBox.Document.Selection.StartPosition = key;
                xRichEditBox.Document.Selection.EndPosition = key + _prevQueryLength;
                xRichEditBox.Document.Selection.CharacterFormat.SetClone(_originalCharFormat[key]);
                xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = Colors.Transparent;
            }
            UpdateDocumentFromXaml();
            _originalCharFormat.Clear();
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
                _originalCharFormat.TryGetValue(start, out clone);
                if (clone != null)
                {
                    xRichEditBox.Document.Selection.CharacterFormat.SetClone(clone);
                    _originalCharFormat.Remove(start);
                    if (_nextMatch >= _originalCharFormat.Keys.Count || _nextMatch == 0)
                        _nextMatch = 0;
                    else
                        _nextMatch--;
                    this.NextResult();
                }
            }
        }
        #endregion

        #region commented out code

        //void XRichEditBox_KeyUp(object sender, KeyRoutedEventArgs e)
        //{
        //    e.Handled = true;
        //    if (e.Key == VirtualKey.Back && (string.IsNullOrEmpty(GetAllText()))
        //        GetDocView().DeleteDocument(true);
        //    string allText;
        //    xRichEditBox.Document.GetText(TextGetOptions.UseObjectText, out allText);

        //    var s1 = this.xRichEditBox.Document.Selection.StartPosition;
        //    var s2 = this.xRichEditBox.Document.Selection.EndPosition;

        //    // try to get last typed character based on the current selection position 
        //    this.xRichEditBox.Document.Selection.SetRange(Math.Max(0, s1 - 1), s1);
        //    string lastTypedCharacter;
        //    this.xRichEditBox.Document.Selection.GetText(TextGetOptions.None, out lastTypedCharacter);

        //    // if the last lastTypedCharacter is white space, then we check to see if it terminates a hyperlink
        //    if (lastTypedCharacter == " " || lastTypedCharacter == "\r" || lastTypedCharacter == "^")
        //    {
        //        // search through all the text for the nearest '@' indicating the start of a possible hyperlink
        //        int atPos = findPreviousHyperlinkStartMarker(allText, s1);

        //        // we found the nearest '@'
        //        if (atPos != -1)
        //        {
        //            // get the text between the '@' and the current input position 
        //            var refText = getHyperlinkText(atPos, s2);

        //            if (!refText.StartsWith("HYPERLINK")) // @HYPERLINK means we've already created the hyperlink
        //            {
        //                // see if we can find a document whose primary keys match the text
        //                var theDoc = findHyperlinkTarget(lastTypedCharacter == "^", refText);

        //                var startPt = new Point();
        //                try
        //                {
        //                    this.xRichEditBox.Document.Selection.GetPoint(HorizontalCharacterAlignment.Center, VerticalCharacterAlignment.Baseline, PointOptions.Start, out startPt);
        //                }
        //                catch (Exception exception)
        //                {
        //                    Debug.WriteLine(exception);
        //                }
        //                createRTFHyperlink(theDoc, startPt, ref s1, ref s2, lastTypedCharacter == "^", false);
        //            }
        //        }
        //    }

        //    this.xRichEditBox.Document.Selection.SetRange(s1, s2);
        //}


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



