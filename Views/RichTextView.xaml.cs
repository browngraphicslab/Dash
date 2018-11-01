using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using DashShared;
using TextWrapping = Windows.UI.Xaml.TextWrapping;
using System.Threading.Tasks;
using System.Windows;
using Windows.UI.Xaml.Documents;
using Dash.Controllers.Operators;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236
namespace Dash
{
    public sealed partial class RichTextView
    {
        #region Intilization 

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(RichTextModel.RTD), typeof(RichTextView), new PropertyMetadata(default(RichTextModel.RTD), xRichTextView_TextChangedCallbackStatic));
        public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(
            "TextWrapping", typeof(TextWrapping), typeof(RichTextView), new PropertyMetadata(default(TextWrapping)));
        
        private int           _prevQueryLength;// The length of the previous search query
        private int           _nextMatch;// Index of the next highlighted search result
        private string        _originalRtfFormat;
        private List<string>  _queries;
        private int           _textLength;
        private int           _queryIndex = -1;
        private string        _lastXamlRTFText = "";
        /// <summary>
        /// A dictionary of the original character formats of all of the highlighted search results
        /// </summary>
        private Dictionary<int, Color>    _originalCharFormat = new Dictionary<int, Color>();
        
        private ManipulationControlHelper _manipulator;
        private AnnotationManager         _annotationManager;
        public static bool _searchHighlight = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public RichTextView()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += UnLoaded;

            AddHandler(PointerPressedEvent, new PointerEventHandler((s, e) =>
            {
                var docView = this.GetFirstAncestorOfType<DocumentView>();
                if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
                {
                    if (!SelectionManager.IsSelected(docView))
                    {
                        SelectionManager.Select(docView, false);
                        var selection = xRichEditBox.Document.Selection;
                        string boxText;
                        xRichEditBox.Document.GetText(TextGetOptions.None, out boxText);
                        var lenght = boxText.Length - 1;
                        selection.StartPosition = lenght;
                        selection.EndPosition = lenght;
                        xRichEditBox.Focus(FocusState.Keyboard);
                        MenuToolbar.Instance.Update(SelectionManager.GetSelectedDocs());
                    }
                       
                    SelectionManager.TryInitiateDragDrop(docView, e, null);
                }
                _manipulator = !e.IsRightPressed() ? null: new ManipulationControlHelper(this, e, (e.KeyModifiers & VirtualKeyModifiers.Shift) != 0, true);
                DocumentView.FocusedDocument = docView;
                e.Handled = true;
            }), true);
            AddHandler(TappedEvent, new TappedEventHandler(xRichEditBox_Tapped), true);
            AddHandler(PointerMovedEvent, new PointerEventHandler((s, e) => _manipulator?.PointerMoved(s, e)), true);
            AddHandler(PointerReleasedEvent, new PointerEventHandler((s, e) => _manipulator = null), true);

            xSearchDelete.Click += (s, e) =>
            {
                ClearSearchHighlights();
                //SetSelected("");
                xSearchBoxPanel.Visibility = Visibility.Collapsed;
            };

            xSearchBox.KeyUp += (s, e) => e.Handled = true;

            xSearchBox.QuerySubmitted += (s, e) => NextResult(); // Selects the next highlighted search result on enter in the xRichEditBox

            xSearchBox.QueryChanged += (s, e) => SetSelected(e.QueryText);// Searches content of the xRichEditBox, highlights all results

            xRichEditBox.AddHandler(KeyDownEvent, new KeyEventHandler(XRichEditBox_OnKeyDown), true);
            xRichEditBox.AddHandler(KeyUpEvent, new KeyEventHandler(XRichEditBox_OnKeyUp), true);

            xRichEditBox.Drop += (s, e) =>
            {
                if (!MainPage.Instance.IsAltPressed())
                {
                    e.Handled = true;
                    xRichEditBox_Drop(s, e);
                }
            };

            PointerWheelChanged += (s, e) => e.Handled = true;

            xRichEditBox.GotFocus += (s, e) =>
            {
                var docView = getDocView();
                if (docView != null)
                {
                    if (!this.IsShiftPressed() && !this.IsRightBtnPressed())
                    {
                        if (SelectionManager.GetSelectedDocs().Count != 1 || !SelectionManager.IsSelected(docView))
                        {
                            SelectionManager.Select(docView, false);
                        }
                    }
                    FlyoutBase.GetAttachedFlyout(xRichEditBox)?.Hide(); // close format options
                    docView.CacheMode = null;
                    
                    ClearSearchHighlights();
                    xSearchBoxPanel.Visibility = Visibility.Collapsed;
                    Clipboard.ContentChanged += Clipboard_ContentChanged;
                }
            };

            xSearchBox.GotFocus += (s, e) => MatchQuery(getSelected());

            xSearchBox.LostFocus += (s, e) =>
            {
                ClearSearchHighlights();
                xSearchBoxPanel.Visibility = Visibility.Collapsed;
            };

            xRichEditBox.TextChanged += (s, e) =>
            {
                var xamlRTF = getRtfText();
                if (xamlRTF != _lastXamlRTFText)
                {
                    _lastXamlRTFText = xamlRTF;  // save the Xaml since so we can know when we get called back about this change not to update the UI and get into a loop.
                    SetValue(TextProperty, new RichTextModel.RTD(xamlRTF)); // push the change to the Dash binding which will update all other views, etc
                }
            };

            xRichEditBox.LostFocus += (s, e) =>
            {
                if (getDocView() != null)
                {
                    getDocView().CacheMode = new BitmapCache();
                }
                Clipboard.ContentChanged -= Clipboard_ContentChanged;
                var readableText = getReadableText();
                if (string.IsNullOrEmpty(getReadableText()))
                {
                    var docView = getDocView();
                    if (!SelectionManager.IsSelected(docView))
                    {
                        using (UndoManager.GetBatchHandle())
                        {
                            docView?.DeleteDocument();
                        }
                    }
                }
                else if (readableText.StartsWith("#"))
                {
                    xRichEditBox.Document.SetText(TextSetOptions.None, readableText.Substring(1));
                }
            };

            xRichEditBox.ContextMenuOpening += (s, e) => e.Handled = true; // suppresses the Cut, Copy, Paste, Undo, Select All context menu from the native view

            xRichEditBox.SelectionHighlightColorWhenNotFocused = new SolidColorBrush(Colors.Gray) { Opacity = 0.5 };

            var sizeBinding = new Binding
            {
                Source = SettingsView.Instance,
                Path = new PropertyPath(nameof(SettingsView.Instance.NoteFontSize)),
                Mode = BindingMode.OneWay
            };
            xRichEditBox.SetBinding(FontSizeProperty, sizeBinding);

            _annotationManager = new AnnotationManager(this);

            SizeChanged += (sender, e) =>
            {
                // we always need to make sure that our own Height is NaN
                // after any kind of resize happens so that we can grow as needed.
                // Height = double.NaN;
                // if we're inside of a RelativePanel that was resized, we need to 
                // reset it to have NaN height so that it can grow as we type.
                //xRichEditBox.Height = e.NewSize.Height;
                if (Parent is RelativePanel relative)
                {
                    relative.Height = double.NaN;
                }
            };
        }

        ~RichTextView()
        {
            //Debug.WriteLine("Finalized RichTextView");
        }

        private void SelectionManager_SelectionChanged(DocumentSelectionChangedEventArgs args)
        {
            if (string.IsNullOrEmpty(getReadableText()) && FocusManager.GetFocusedElement() != xRichEditBox)
            {
                var docView = getDocView();
                if (args.DeselectedViews.Contains(docView))
                    docView.DeleteDocument();
            }
        }

        //public void UpdateDocumentFromXaml()
        //{
        //    if (!(getSelected()?.Count == 0 || getSelected()?.First()?.Data != ""))
        //    {
        //        _originalRtfFormat = getRtfText();
        //        queryIndex = -1;
        //    }
        //}

        public DocumentController    DataDocument => getDataDoc();
        public DocumentController    LayoutDocument => getLayoutDoc();
        private DocumentView         getDocView() { return this.GetFirstAncestorOfType<DocumentView>(); }
        private DocumentController   getLayoutDoc() { return getDocView()?.ViewModel.LayoutDocument; }
        private DocumentController   getDataDoc() { return getDocView()?.ViewModel.DataDocument; }
        private List<TextController> getSelected()
        {
            return getDataDoc()?.GetDereferencedField<ListController<TextController>>(CollectionDBView.SelectedKey, null)?.TypedData
                ?? getLayoutDoc()?.GetDereferencedField<ListController<TextController>>(CollectionDBView.SelectedKey, null)?.TypedData;
        }
        
        private void SetSelected(string query)
        {
            var value = query.Equals("") ? new ListController<TextController>(new TextController()) : new ListController<TextController>(new TextController(query));
            getDataDoc()?.SetField(CollectionDBView.SelectedKey, value, true);
        }

        private string getReadableText()
        {
            xRichEditBox.Document.GetText(TextGetOptions.UseObjectText, out string allText);
            return allText;
        }

        private string getRtfText()
        { 
            xRichEditBox.Document.GetText(TextGetOptions.FormatRtf, out string allRtfText);
            var strippedRtf = allRtfText.Replace("\r\n\\pard\\tx720\\par\r\n", ""); // RTF editor adds a trailing extra paragraph when queried -- need to strip that off
            return new Regex("\\\\par[\r\n}\\\\]*\0").Replace(strippedRtf, "}\r\n\0");
        }

        #endregion

        #region eventhandlers

        /// <summary>
        /// This gets called every time the Dash binding changes.  So we need to update the RichEditBox here *unless* the change
        /// to the Dash binding was caused by editing this richEditBox (ie, the edit value == lastXamlRTFText), in which case we should do nothing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="dp"></param>
        private static void xRichTextView_TextChangedCallbackStatic(DependencyObject sender, DependencyPropertyChangedEventArgs dp)
        {
            if (!_searchHighlight)
            {
                var rtv = sender as RichTextView;
                var newRtFormatString = ((RichTextModel.RTD)dp.NewValue)?.RtfFormatString;
                if (newRtFormatString != null && newRtFormatString != rtv._lastXamlRTFText)
                {
                    rtv.xRichEditBox.Document.SetText(TextSetOptions.FormatRtf, ((RichTextModel.RTD)dp.NewValue)?.RtfFormatString); // setting the RTF text does not mean that the Xaml view will literally store an identical RTF string to what we passed
                    rtv._lastXamlRTFText = rtv.getRtfText(); // so we need to retrieve what Xaml actually stored and treat that as an 'alias' for the format string we used to set the text.
                }
                if (rtv.getSelected()?.FirstOrDefault() != null && rtv.getSelected().First().Data is string selected)
                {
                    rtv._prevQueryLength = selected.Length;
                }
                var documentView = rtv.GetFirstAncestorOfType<DocumentView>();
                if (documentView != null)
                {
                    if (rtv.xRichEditBox.Document.Selection.FindText(HyperlinkText, rtv.getRtfText().Length, FindOptions.Case) != 0)
                    {
                        var url = rtv.DataDocument.GetDereferencedField<TextController>(KeyStore.SourceUriKey, null)?.Data;
                        var title = rtv.DataDocument.GetDereferencedField<TextController>(KeyStore.SourceTitleKey, null)?.Data;

                        //this does better formatting/ parsing than the regex stuff can
                        var link = title ?? HtmlToDashUtil.GetTitlesUrl(url);

                        rtv.xRichEditBox.Document.Selection.CharacterFormat.Size = 9;
                        rtv.xRichEditBox.Document.Selection.FindText(HyperlinkMarker, rtv.getRtfText().Length, FindOptions.Case);
                        rtv.xRichEditBox.Document.Selection.CharacterFormat.Size = 8;
                        rtv.xRichEditBox.Document.Selection.Text = link;
                        rtv.xRichEditBox.Document.Selection.Link = "\"" + url + "\"";
                        rtv.xRichEditBox.Document.Selection.CharacterFormat.Underline = UnderlineType.Single;
                        rtv.xRichEditBox.Document.Selection.EndPosition = rtv.xRichEditBox.Document.Selection.StartPosition;
                    }
                }
            }
        }

        // determines the document controller of the region and calls on annotationManager to handle the linking procedure
        public async void RegionSelected(Point pointPressed)
        {
            var target = getHyperlinkTargetForSelection();
            if (target != null)
            {
                var theDoc = RESTClient.Instance.Fields.GetController<DocumentController>(target);
                if (theDoc != null)
                {
                    if (DataDocument.GetDereferencedField<ListController<DocumentController>>(KeyStore.RegionsKey, null)?.TypedData.Contains(theDoc) == true)
                    {
                        // get region doc
                        var region = theDoc.GetDataDocument().GetRegionDefinition();
                        _annotationManager.FollowRegion(theDoc, this.GetAncestorsOfType<ILinkHandler>(), pointPressed);
                    }
                }
                else if (target.StartsWith("http"))
                {
                    await Launcher.LaunchUriAsync(new Uri(target));
                }
            }
        }

        private DocumentView FindNearestDisplayedBrowser(Point where, string uri, bool onlyOnPage = true)
        {
            double dist = double.MaxValue;
            DocumentView nearest = null;
            foreach (var presenter in (this.GetFirstAncestorOfType<CollectionView>().CurrentView as CollectionFreeformView).GetItemsControl().ItemsPanelRoot.Children.Select(c => (c as ContentPresenter)))
            {
                var dvm = presenter.GetFirstDescendantOfType<DocumentView>();
                if (dvm.ViewModel.DataDocument.GetDereferencedField<TextController>(KeyStore.DataKey, null)?.Data == uri)
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

        private void xRichEditBox_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = false;
            RegionSelected(e.GetPosition(MainPage.Instance));
        }

        private void CursorToEnd()
        {
            xRichEditBox.Document.GetText(TextGetOptions.None, out string text);
            xRichEditBox.Document.Selection.StartPosition = text.Length;
        }

        private async void xRichEditBox_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.TryGetLoneDragDocAndView(out DocumentController dragDoc, out DocumentView view))
            {
                if (view != null && !MainPage.Instance.IsShiftPressed() && string.IsNullOrWhiteSpace(xRichEditBox.Document.Selection.Text))
                {
                    e.Handled = false;
                    return;
                }

                var dropRegion = dragDoc;
                if (KeyStore.RegionCreator[dragDoc.DocumentType] != null)
                    dropRegion = KeyStore.RegionCreator[dragDoc.DocumentType](view);
                linkDocumentToSelection(dropRegion, true);

                e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None ? DataPackageOperation.Link : e.DataView.RequestedOperation;
            }
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                linkDocumentToSelection(await FileDropHelper.GetDroppedFile(e), false);
            }
            e.Handled = true;
        }

        private void CreateActionMenu(RichEditBox sender)
        {
            var transformToVisual = sender.TransformToVisual(MainPage.Instance.xCanvas);
            var pos = transformToVisual.TransformPoint(new Point(0, ActualHeight));
            var menu = new ActionMenu(transformToVisual.TransformPoint(new Point(0, 0)))
            {
                Width = 400,
                Height = 500,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                UseFilterBox = false,
                RenderTransform = new TranslateTransform
                {
                    X = pos.X - 20,
                    Y = pos.Y + 20
                }
            };

            menu.ActionCommitted += Menu_ActionCommitted;

            sender.TextChanged += delegate
            {
                if (actionMenu != null)
                {
                    var fullText = GetParsedText();
                    actionMenu.FilterString = fullText;
                }
            };

            sender.LostFocus += delegate
            {
                MainPage.Instance.xCanvas.Children.Remove(actionMenu);
            };


            //menu.AddGroup("DATABASE", new List<ActionViewModel>
            //    {
            //        new ActionViewModel("Table", "Database Table", () => Debug.WriteLine("Table"), source),
            //        new ActionViewModel("Board", "Board", () => Debug.WriteLine("Board"), null),
            //        new ActionViewModel("Calendar", "Calendar", () => Debug.WriteLine("Calendar"), null),
            //    });
            //menu.AddGroup("TEST1", new List<ActionViewModel>
            //    {
            //        new ActionViewModel("Table", "Database Table", () => Debug.WriteLine("Table"), source),
            //        new ActionViewModel("Board", "Board", () => Debug.WriteLine("Board"), null),
            //        new ActionViewModel("Calendar", "Calendar", () => Debug.WriteLine("Calendar"), source),
            //    });
            //menu.AddGroup("TEST2", new List<ActionViewModel>
            //    {
            //        new ActionViewModel("Table", "Database Table", () => Debug.WriteLine("Table"), null),
            //        new ActionViewModel("Board", "Board", () => Debug.WriteLine("Board"), source),
            //        new ActionViewModel("Calendar", "Calendar", () => Debug.WriteLine("Calendar"), source),
            //    });

            var cfv = this.GetFirstAncestorOfType<CollectionFreeformView>();
            cfv?.AddToMenu(menu);

            ImageSource source = new BitmapImage(new Uri("ms-appx://Dash/Assets/Rightlg.png"));
            menu.AddAction("BASIC", new ActionViewModel("Title",  "Add title",           MakeTitleAction,  source));
            menu.AddAction("BASIC", new ActionViewModel("Center", "Align text to center",SetCenterAction,  source));
            menu.AddAction("BASIC", new ActionViewModel("To-Do",  "Create a todo note",  CreateTodoAction, source));
            menu.AddAction("BASIC", new ActionViewModel("Google", "Google Clip",         GoogleClip,       source));
            menu.AddAction("BASIC", new ActionViewModel("Bio",    "Google Bio",          GoogleBio,       source));
            MainPage.Instance.xCanvas.Children.Add(menu);
        }

        private Task<bool> MakeTitleAction(ActionFuncParams actionParams)
        {
            xRichEditBox.Document.SetText(TextSetOptions.FormatRtf, "TITLE\n");
            xRichEditBox.Document.Selection.StartPosition = 0;
            xRichEditBox.Document.Selection.EndPosition = getReadableText().Length;
            xRichEditBox.Document.Selection.CharacterFormat.Size = 20;
            this.Bold();
            xRichEditBox.Document.Selection.StartPosition = getReadableText().Length+1;
            xRichEditBox.Document.Selection.EndPosition = getReadableText().Length+2;
            xRichEditBox.Document.Selection.CharacterFormat.Size = 12;
            return Task.FromResult(false);
        }

        private Task<bool> SetCenterAction(ActionFuncParams actionParams)
        {
            xRichEditBox.Document.SetText(TextSetOptions.FormatRtf, "text");
            xRichEditBox.TextAlignment = TextAlignment.Center;
            xRichEditBox.Document.Selection.StartPosition = 0;
            xRichEditBox.Document.Selection.EndPosition = getReadableText().Length;
            return Task.FromResult(false);
        }

        private Task<bool> CreateTodoAction(ActionFuncParams actionParams)
        {
            var templatedText =
            "{\\rtf1\\fbidis\\ansi\\ansicpg1252\\deff0\\nouicompat\\deflang1033{\\fonttbl{\\f0\\fnil Century Gothic; } {\\f1\\fnil\\fcharset0 Century Gothic; } {\\f2\\fnil\\fcharset2 Symbol; } }" +
            "\r\n{\\colortbl;\\red51\\green51\\blue51; }\r\n{\\*\\generator Riched20 10.0.17134}\\viewkind4\\uc1 \r\n\\pard\\tx720\\cf1\\b{\\ul\\f0\\fs34 My\\~\\f1 Todo\\~List:}\\par" +
            "\r\n\\b0\\f0\\fs24\\par\r\n\r\n\\pard{\\pntext\\f2\\'B7\\tab}{\\*\\pn\\pnlvlblt\\pnf2\\pnindent0{\\pntxtb\\'B7}}\\tx720\\f1\\fs24 Item\\~1\\par" +
            "\r\n{\\pntext\\f2\\'B7\\tab}\\b0 Item\\~2\\par}";
            xRichEditBox.Document.SetText(TextSetOptions.FormatRtf, templatedText);
            return Task.FromResult(false);
        }
        private async Task<bool> GoogleClip(ActionFuncParams actionParams)
        {
            var templatedText = await QuerySnapshotOperator.QueryGoogle(actionParams.Params?.FirstOrDefault() ?? "");
            xRichEditBox.Document.SetText(TextSetOptions.FormatRtf, (actionParams.Params?.FirstOrDefault() ?? "") + "-> " + templatedText);
            return false;
        }
        private async Task<bool> GoogleBio(ActionFuncParams actionParams)
        {
            var bkey = KeyController.Get("Birthday");
            var wkey = KeyController.Get("Wikipedia");
            var birthday = await QuerySnapshotOperator.QueryGoogle("birthday " + (actionParams.Params?.FirstOrDefault() ?? ""));
            var wikipedia = "https://en.wikipedia.org/wiki/" + (actionParams.Params?.FirstOrDefault()?.Replace(" ", "_") ?? "");
            var stack = new CollectionNote(actionParams.Where, CollectionViewType.Stacking, 400,600).Document;
            var stackData = stack.GetDataDocument();
            stackData.SetTitle(actionParams.Params.FirstOrDefault() ?? "<null>");
            stackData.SetField<TextController>(bkey, birthday, true);
            stackData.SetField<TextController>(wkey, wikipedia, true);
            var html = new HtmlNote(wikipedia,size:new Size(double.NaN, 700)).Document;
            html.SetHorizontalAlignment(HorizontalAlignment.Stretch);
            var dbox = new DataBox(new DocumentReferenceController(stackData, bkey)).Document;
            dbox.SetField<NumberController>(KeyStore.FontSizeKey, 36, true);
            stackData.SetField(KeyStore.DataKey,
                new ListController<DocumentController>(
                new DocumentController[] { dbox, html}), true);
            this.GetFirstAncestorOfType<CollectionView>().ViewModel.AddDocument(stack);
            return true;
        }

        private void Menu_ActionCommitted(bool removeTextBox)
        {
            if (removeTextBox)
            {
                this.GetFirstAncestorOfType<DocumentView>()?.DeleteDocument();
            }
        }
        private void XRichEditBox_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key.Equals(VirtualKey.Escape))
            {
                if (actionMenu != null)
                {
                    CloseActionMenu();
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Create short cuts for the xRichEditBox (ctrl+I creates indentation by default, ctrl-Z will get rid of the indentation, showing only the italized text)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XRichEditBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            //handles batching for undo typing
            //TypeTimer.typeEvent();
            if (!this.IsCtrlPressed() && !this.IsAltPressed() && !this.IsShiftPressed())
            {
                getDataDoc().CaptureNeighboringContext();
            }

            if (e.Key == (VirtualKey)191) // 191 = '/' 
            {
                if (actionMenu == null)
                {
                    var rt = getReadableText();
                    if (rt == "" || rt == "#" || rt == "/" || (this.xRichEditBox.Document.Selection.EndPosition - this.xRichEditBox.Document.Selection.StartPosition) >= rt.Length)
                    {
                        CreateActionMenu(sender as RichEditBox);
                    }
                }
                else
                {
                    CloseActionMenu();
                }
            }

            if (e.Key.Equals(VirtualKey.Down))
            {

            }

            if (e.Key.Equals(VirtualKey.Enter))
            {
                if (actionMenu != null)
                {
                    CloseAndInputActionMenu();
                }
                else
                {
                    processMarkdown();
                }
            }

            if (e.Key.Equals(VirtualKey.Back))
            {
                if (actionMenu != null && string.IsNullOrEmpty(GetParsedText()))
                {
                    CloseActionMenu();
                }
            }

            if (this.IsShiftPressed() && !e.Key.Equals(VirtualKey.Shift) && e.Key.Equals(VirtualKey.Enter))
            {
                xRichEditBox.Document.Selection.MoveStart(TextRangeUnit.Character, -1);
                xRichEditBox.Document.Selection.Delete(TextRangeUnit.Character, 1);
                getDocView().HandleShiftEnter();
                e.Handled = true;
            }
            if (this.IsAltPressed() && !e.Key.Equals(VirtualKey.Menu) && e.Key.Equals(VirtualKey.Right))
            {
                if (xRichEditBox.Document.Selection.EndPosition < getReadableText().Length - 1)
                {
                    var clone = xRichEditBox.Document.Selection.CharacterFormat.GetClone();
                    xRichEditBox.Document.Selection.MoveEnd(TextRangeUnit.Character, -1);
                    xRichEditBox.Document.Selection.CharacterFormat.SetClone(clone);
                }
                var s1 = xRichEditBox.Document.Selection.StartPosition;
                xRichEditBox.Document.Selection.ParagraphFormat.Alignment = ParagraphAlignment.Right;
                xRichEditBox.Document.Selection.SetRange(s1, s1);
                e.Handled = true;
            }
            if (this.IsAltPressed() && !e.Key.Equals(VirtualKey.Menu) && e.Key.Equals(VirtualKey.Left))
            {
                var clone = xRichEditBox.Document.Selection.CharacterFormat.GetClone();
                xRichEditBox.Document.Selection.MoveStart(TextRangeUnit.Character, 1);
                xRichEditBox.Document.Selection.CharacterFormat.SetClone(clone);
                var s1 = xRichEditBox.Document.Selection.StartPosition;
                xRichEditBox.Document.Selection.ParagraphFormat.Alignment = ParagraphAlignment.Left;
                xRichEditBox.Document.Selection.SetRange(s1, s1);
                e.Handled = true;
            }


            if (e.Key.Equals(VirtualKey.Escape))
            {
                if (actionMenu == null)
                {
                    var tab = xRichEditBox.IsTabStop;
                    xRichEditBox.IsTabStop = false;
                    xRichEditBox.IsEnabled = false;
                    xRichEditBox.IsEnabled = true;
                    xRichEditBox.IsTabStop = tab;
                }
                else
                {
                    e.Handled = true;
                }
                ClearSearchHighlights();
                //SetSelected("");
                xSearchBoxPanel.Visibility = Visibility.Collapsed;
            }

            else if (this.IsTabPressed())
            {
                xRichEditBox.Document.Selection.TypeText("\t");
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Space)
            {
                xRichEditBox.Document.Selection.TypeText(Convert.ToChar(160).ToString());
                e.Handled = true;
            }
            else if (this.IsCtrlPressed())   // ctrl-B, ctrl-I, ctrl-U handled natively by the text editor
            {
                switch (e.Key)
                {
                case VirtualKey.H:
                    this.Highlight(Colors.Yellow); // using RIchTextFormattingHelper extenions
                    e.Handled = true;
                    break;
                case VirtualKey.F:
                    xSearchBoxPanel.Visibility = Visibility.Visible;
                    xSearchBoxPanel.UpdateLayout();
                    xSearchBox.GetFirstDescendantOfType<TextBox>()?.Focus(FocusState.Programmatic);
                    e.Handled = true;
                    break;
                case VirtualKey.L:
                    if (this.IsShiftPressed())
                    {
                        if (xRichEditBox.Document.Selection.ParagraphFormat.ListType == MarkerType.UppercaseEnglishLetter)
                        {
                            xRichEditBox.Document.Selection.ParagraphFormat.ListType = MarkerType.None;
                        }
                        e.Handled = true;
                    }
                    break;
                    //case VirtualKey.R:
                    //    xReplaceBox.Visibility = Visibility.Visible;
                    //    xReplaceBoxPanel.Visibility = Visibility.Visible;
                    //    break;
                }
            }    
        }

        private ActionMenu actionMenu => (ActionMenu)MainPage.Instance.xCanvas.Children.FirstOrDefault(fe => fe is ActionMenu);

        private void CloseActionMenu()
        {
            if (actionMenu != null)
            {
                MainPage.Instance.xCanvas.Children.Remove(actionMenu);
            }
        }

        private void CloseAndInputActionMenu()
        {
            if (actionMenu != null)
            {
                var transformToVisual = this.TransformToVisual(MainPage.Instance.xCanvas);
                var pos = transformToVisual.TransformPoint(new Point(0, 0));
                actionMenu.InvokeAction(GetParsedText(), pos);
            }
        }

        private string GetParsedText()
        {
            var fullText = getReadableText();
            if (fullText.Length == 0) return "";

            if (fullText[0].Equals('#'))
            {
                fullText = fullText.Substring(1);
            }

            if (fullText.Length == 0) return "";
            if (fullText[0].Equals('/'))
            {
                fullText = fullText.Substring(1);
            }
            fullText = fullText.Trim();

            return fullText;
        }

        private void processMarkdown()
        {
            var s1 = xRichEditBox.Document.Selection.StartPosition;
            var s2 = xRichEditBox.Document.Selection.EndPosition;
            var origFormat = xRichEditBox.Document.Selection.CharacterFormat.GetClone();
            var origAlign = xRichEditBox.Document.Selection.ParagraphFormat.Alignment;
            var align = origAlign;
            var hashcount = 0;
            var extracount = 0;

            for (int i = s1 - 2; i >= 0; i--)
            {
                xRichEditBox.Document.Selection.SetRange(i, i + 1);
                string text = xRichEditBox.Document.Selection.Text;
                if (text == "}")
                {
                    align = ParagraphAlignment.Right;
                    extracount++;
                }
                else if (text == "^")
                {
                    align = ParagraphAlignment.Center;
                    extracount++;
                }
                else if (text == "{")
                {
                    align = ParagraphAlignment.Left;
                    extracount++;
                }
                else if (text == "#")
                {
                    hashcount++;
                }
                else if (text == "\r")
                {
                    xRichEditBox.Document.Selection.SetRange(i + 1, i + 2);
                    break;
                }
                else
                {
                    extracount = hashcount = 0;
                    align = origAlign;
                }
            }
            if (hashcount > 0 || extracount > 0)
            {
                xRichEditBox.Document.Selection.SetRange(xRichEditBox.Document.Selection.StartPosition,
                                                         xRichEditBox.Document.Selection.StartPosition + hashcount + extracount);
                if (xRichEditBox.Document.Selection.StartPosition == 0)
                    CollectionFreeformBase.PreviewFormatString = xRichEditBox.Document.Selection.Text;
                xRichEditBox.Document.Selection.Text = "";
                xRichEditBox.Document.Selection.SetRange(xRichEditBox.Document.Selection.StartPosition, s2);
                xRichEditBox.Document.Selection.CharacterFormat.Bold = hashcount > 0 ? FormatEffect.On : origFormat.Bold;
                xRichEditBox.Document.Selection.ParagraphFormat.Alignment = align;
                xRichEditBox.Document.Selection.CharacterFormat.Size = origFormat.Size + hashcount * 5;
            }
            xRichEditBox.Document.Selection.SetRange(s1, s2);
            xRichEditBox.Document.Selection.CharacterFormat.Bold = FormatEffect.Off;
            xRichEditBox.Document.Selection.CharacterFormat.Size = origFormat.Size;
        }

        private async void Clipboard_ContentChanged(object sender, object e)
        {
            Clipboard.ContentChanged -= Clipboard_ContentChanged;
            var dataPackage = new DataPackage();
            DataPackageView clipboardContent = Clipboard.GetContent();
            dataPackage.SetText(await clipboardContent.GetTextAsync());
            //set RichTextView property to this view
            dataPackage.Properties[nameof(DocumentController)] = LayoutDocument;
            Clipboard.SetContent(dataPackage);
            Clipboard.ContentChanged += Clipboard_ContentChanged;
        }


        #endregion

        #region load/unload
        // Someone please find out why this is being called twice
        private void selectedFieldUpdatedHdlr(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs e)
        {
            _searchHighlight = true;
            MatchQuery(getSelected());
            // Dispatcher.RunIdleAsync((x) => MatchQuery(getSelected()));
        }

        private void UnLoaded(object s, RoutedEventArgs e)
        {
            ClearSearchHighlights(true);
            Application.Current.Suspending -= AppSuspending;
            SetSelected("");
            DataDocument?.RemoveFieldUpdatedListener(CollectionDBView.SelectedKey, selectedFieldUpdatedHdlr);
            SelectionManager.SelectionChanged -= SelectionManager_SelectionChanged;
        }

        public const string HyperlinkMarker = "<hyperlink marker>";
        public const string HyperlinkText = "\r Text from: " + HyperlinkMarker;
        public void AppSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs args)
        {
            return;
            ClearSearchHighlights();
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            if (DataDocument.GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null)?.Data == "/" && xRichEditBox == FocusManager.GetFocusedElement())
            {
                CreateActionMenu(xRichEditBox);
            }
            if (GetValue(TextProperty) is RichTextModel.RTD xamlText)
            {
                xRichEditBox.Document.SetText(TextSetOptions.FormatRtf, xamlText.RtfFormatString); // setting the RTF text does not mean that the Xaml view will literally store an identical RTF string to what we passed
            }
            _lastXamlRTFText = getRtfText(); // so we need to retrieve what Xaml actually stored and treat that as an 'alias' for the format string we used to set the text.

            DataDocument.AddFieldUpdatedListener(CollectionDBView.SelectedKey, selectedFieldUpdatedHdlr);
            Application.Current.Suspending += AppSuspending;

            SelectionManager.SelectionChanged += SelectionManager_SelectionChanged;
            var documentView = this.GetFirstAncestorOfType<DocumentView>();
            if (documentView != null)
            {
                xRichEditBox.Document.Selection.FindText(HyperlinkText, getRtfText().Length, FindOptions.Case);
                if (xRichEditBox.Document.Selection.StartPosition != xRichEditBox.Document.Selection.EndPosition)
                {
                    var url = DataDocument.GetDereferencedField<TextController>(KeyStore.SourceUriKey, null)?.Data;
                    var title = DataDocument.GetDereferencedField<TextController>(KeyStore.SourceTitleKey, null)?.Data;

                    //this does better formatting/ parsing than the regex stuff can
                    var link = title ?? HtmlToDashUtil.GetTitlesUrl(url);

                    xRichEditBox.Document.Selection.CharacterFormat.Size = 9;
                    xRichEditBox.Document.Selection.FindText(HyperlinkMarker, getRtfText().Length, FindOptions.Case);
                    xRichEditBox.Document.Selection.CharacterFormat.Size = 8;
                    xRichEditBox.Document.Selection.Text = link;
                    xRichEditBox.Document.Selection.Link = "\"" + url + "\"";
                    xRichEditBox.Document.Selection.CharacterFormat.Underline = UnderlineType.Single;
                    xRichEditBox.Document.Selection.EndPosition = xRichEditBox.Document.Selection.StartPosition;
                }
            }
        }

        #endregion


        #region hyperlink

        public DocumentController GetRegionDocument()
        {
            if (!string.IsNullOrEmpty(xRichEditBox.Document.Selection.Text))
            {
                using (UndoManager.GetBatchHandle())
                {
                    // get the document controller for the target hyperlink (region) document
                    if (createRTFHyperlink() is DocumentController dc)
                    {
                        DataDocument.AddToRegions(new List<DocumentController> { dc });
                        return dc;
                    }
                }
            }
            return LayoutDocument;
        }

        private string getHyperlinkTargetForSelection()
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

        private void linkDocumentToSelection(DocumentController theDoc, bool forceLocal)
        {
            var s1 = xRichEditBox.Document.Selection.StartPosition;
            var s2 = xRichEditBox.Document.Selection.EndPosition;

            using (UndoManager.GetBatchHandle())
            {
                if (string.IsNullOrEmpty(getSelected()?.First()?.Data))
                {
                    if (theDoc != null && s1 == s2) xRichEditBox.Document.Selection.Text = theDoc.Title;
                }


                var region = GetRegionDocument();
                region.Link(theDoc, LinkBehavior.Annotate);

                //convertXamlToDash();

                xRichEditBox.Document.Selection.SetRange(s1, s2);
            }
        }

        private DocumentController createRTFHyperlink()
        {
            var selectedText = xRichEditBox.Document.Selection.Text;
            var start = xRichEditBox.Document.Selection.StartPosition;
            var length = xRichEditBox.Document.Selection.EndPosition - start;
            string link = null;
            DocumentController targetRegionDocument = null;

            var origStart = start;

            while (true)
            {
                bool replaced = false;
                for (int i = 0; i <= length; i++)
                {
                    xRichEditBox.Document.Selection.SetRange(start + i, start + i + 1);
                    if (xRichEditBox.Document.Selection.Link != "")
                    {
                        if (xRichEditBox.Document.Selection.Link.StartsWith("\"http"))
                        {
                            length -= xRichEditBox.Document.Selection.FormattedText.Link.Length + " HYPERLINK".Length + 1;
                            xRichEditBox.Document.Selection.FormattedText.Link = "";
                            replaced = true;
                        }
                        else if (xRichEditBox.Document.Selection.Link.StartsWith("\"mailto"))
                        {
                            length -= xRichEditBox.Document.Selection.FormattedText.Link.Length + " HYPERLINK".Length + 1;
                            xRichEditBox.Document.Selection.FormattedText.Link = "";
                            replaced = true;
                        }
                    }
                }
                if (!replaced)
                    break;
            }

            for (int i = 0; i <= length; i++)
            {
                xRichEditBox.Document.Selection.StartPosition = start + i;
                xRichEditBox.Document.Selection.EndPosition = start + i + 1;
                if (xRichEditBox.Document.Selection.Link != "")
                {
                    var color = xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor;
                    var nextColor = color == Colors.LightCyan ? Colors.LightBlue : color == Colors.LightBlue ? Colors.DeepSkyBlue : Colors.Cyan;
                    xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = nextColor;
                    // maybe get the current link and add the new link doc to it?

                    var newstart = xRichEditBox.Document.Selection.EndPosition;
                    for (int j = 0; j < i; j++)
                    {
                        xRichEditBox.Document.Selection.StartPosition = start;
                        xRichEditBox.Document.Selection.EndPosition = start + i - j;
                        if (!xRichEditBox.Document.Selection.Text.Contains("HYPERLINK"))
                            break;
                    }
                    length -= (newstart - start);
                    if (link == null)
                    {
                        link = getTargetLink(selectedText, out targetRegionDocument);
                    }
                    var cursize = xRichEditBox.Document.Selection.EndPosition - xRichEditBox.Document.Selection.StartPosition;
                    xRichEditBox.Document.Selection.Link = link;
                    var newsize = xRichEditBox.Document.Selection.EndPosition - xRichEditBox.Document.Selection.StartPosition;
                    newstart += (newsize - cursize);
                    xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = Colors.LightCyan;
                    xRichEditBox.Document.Selection.SetRange(newstart, newstart + length);
                    start = newstart;
                    i = -1;
                }
            }
            var endpos = xRichEditBox.Document.Selection.EndPosition - 1;
            xRichEditBox.Document.Selection.SetRange(start, start + length);
            for (int j = 0; j < length; j++)
            {
                xRichEditBox.Document.Selection.SetRange(start, start + length - j);
                xRichEditBox.Document.Selection.SetRange(start, start + length - j);
                if (!xRichEditBox.Document.Selection.Text.Contains("HYPERLINK"))
                    break;
            }
            if (length > 0 && !string.IsNullOrEmpty(xRichEditBox.Document.Selection.Text) && !string.IsNullOrWhiteSpace(xRichEditBox.Document.Selection.Text))
            {
                if (link == null)
                {
                    link = getTargetLink(selectedText, out targetRegionDocument);
                }
                xRichEditBox.Document.Selection.SetRange(start, start + length);
                // set the hyperlink for the matched text
                xRichEditBox.Document.Selection.Link = link;
                endpos = xRichEditBox.Document.Selection.EndPosition;
                xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = Colors.LightCyan;
            }
            xRichEditBox.Document.Selection.SetRange(origStart, endpos);
            return targetRegionDocument;
        }

        private string getTargetLink(string selectedText, out DocumentController theDoc)
        {
            theDoc = new RichTextNote(selectedText).Document;
            theDoc.SetRegionDefinition(LayoutDocument);
            var link = "\"" + theDoc.Id + "\"";
            if (theDoc.GetDataDocument().DocumentType.Equals(HtmlNote.HtmlDocumentType) && (bool)theDoc.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DataKey, null)?.Data?.StartsWith("http"))
            {
                link = "\"" + theDoc.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DataKey, null).Data + "\"";
            }
            return link;
        }

        private string getHyperlinkText(int atPos, int s2)
        {
            xRichEditBox.Document.Selection.SetRange(atPos + 1, s2 - 1);
            string refText;
            xRichEditBox.Document.Selection.GetText(TextGetOptions.None, out refText);

            return refText;
        }

        private int findPreviousHyperlinkStartMarker(string allText, int s1)
        {
            xRichEditBox.Document.Selection.SetRange(0, allText.Length);
            var atPos = -1;
            while (xRichEditBox.Document.Selection.FindText("@", 0, FindOptions.None) > 0)
            {
                if (xRichEditBox.Document.Selection.StartPosition < s1)
                {
                    atPos = xRichEditBox.Document.Selection.StartPosition;
                    xRichEditBox.Document.Selection.SetRange(atPos + 1, allText.Length);
                }
                else break;
            }
            return atPos;
        }

        #endregion

        #region search

        private int _colorParamsCount;

        /// <summary>
        /// Searches through richtextbox for textcontrollers in queries- does so by storing the formatting/state of the textbox before the search was conducted
        /// and modifying the rtf directly, since the previous method of using iTextSelection was way too slow to be useful
        /// </summary>
        private void MatchQuery(List<TextController> queries)
        {
            if (getDocView() == null) // || FocusManager.GetFocusedElement() != xSearchBox.GetFirstDescendantOfType<TextBox>())
                return;
            ClearSearchHighlights();
            _originalRtfFormat = getRtfText();

            //_nextMatch = 0;
            //_prevQueryLength = queries?.FirstOrDefault() == null ? 0 : queries.First().Data.Length;
            //string text;
            //xRichEditBox.Document.GetText(TextGetOptions.None, out text);
            //var length = text.Length;
            //xRichEditBox.Document.Selection.StartPosition = 0;
            //xRichEditBox.Document.Selection.EndPosition = 0;
            //int i = 1;
            // find and highlight all matches

            if (queries == null || queries.Count == 0 || string.IsNullOrEmpty(queries?.First()?.Data))
                return;

            xRichEditBox.Document.GetText(TextGetOptions.None, out string actualText);

            _textLength = actualText.Length;

            var regList = queries.Select(t => new Regex((t.Data.Length - t.Data.TrimEnd('\\').Length) % 2 == 0 ? t.Data : t.Data.Substring(0, t.Data.Length - 1)));
            var matches = new List<string>();
            _queries = new List<string>();
            foreach (var reg in regList)
            {
                MatchCollection matchCol = reg.Matches(actualText);
                foreach (Match match in reg.Matches(actualText))
                {
                    if (match.Success && !string.IsNullOrEmpty(match.Value))
                    {
                        _queries.Add(match.Value);
                        if (!matches.Contains(match.Value))
                            matches.Add(match.Value);
                    }
                }
            }

            string currentRtf = _originalRtfFormat;
            string newRtf = currentRtf;

            foreach (var query in matches)
            {
                // Last field of Rtf format is font size specification
                int fs = currentRtf.IndexOf("\\fs");
                int textStart = currentRtf.IndexOf(" ", fs);

                int colorParams = currentRtf.IndexOf("\\colortbl");
                int colorParamsEnd = currentRtf.IndexOf('}', colorParams);
                _colorParamsCount = currentRtf.Substring(colorParams, colorParamsEnd - colorParams).Where(c => c == ';').Count();

                string rtfFormatting = currentRtf.Substring(0, textStart + 1);
                string text = currentRtf.Substring(textStart + 1);

                int defaultHighlight = rtfFormatting.IndexOf("\\highlight");
                if (defaultHighlight > 0)
                {
                    int secondDig = rtfFormatting[defaultHighlight + 11] - '0';
                    if (secondDig >= 0 && secondDig < 10)
                    {
                        _highlightNum = (_highlightNum = rtfFormatting[defaultHighlight + 10] - '0') * 10 + secondDig;
                    }
                    else
                        _highlightNum = rtfFormatting[defaultHighlight + 10] - '0';
                }
                else
                    _highlightNum = 0;
                string highlightedText = rtfFormatting + InsertHighlight(text, query.ToLower());

                highlightedText = highlightedText.Insert(colorParamsEnd - 1, ";\\red255\\green255\\blue0");

                // Splitting the text and reconstructing the string is due to the fact that \\highlight can't have spaces when combined with another escaped
                // rtf command, so we need to delete specifically those spaces, but leave spaces following \\highlight when next to normal text
                string[] split = highlightedText.Split(' ');
                for (int i = 0; i < split.Length - 1; i++)
                {
                    if (i == 0)
                    {
                        newRtf = split[0];
                    }
                    else if (!string.IsNullOrEmpty(split[i]) && (split[i].Remove(split[i].Length - 1).EndsWith("\\highlight") && split[i + 1].StartsWith("\\") && !split[i + 1].StartsWith("\\'")) ||
                        (split[i + 1].StartsWith($"\\highlight{_colorParamsCount}") && split[i].Contains("\\") && !(split[i].Contains("\n") || split[i].Contains("\t") || split[i].Contains("\r") || split[i].Contains("\\'"))))
                    {
                        newRtf += " " + split[i] + split[i + 1];
                        i++;
                    }
                    else
                    {
                        newRtf += " " + split[i];
                        // _originalCharFormat.Add(s,selectedText.CharacterFormat.BackgroundColor);
                    }
                }
                newRtf += " " + split[split.Length - 1];
                currentRtf = newRtf;

                //    while (i > 0 && !string.IsNullOrEmpty(query))
                //    {

                //        i = xRichEditBox.Document.Selection.FindText(query, length, FindOptions.None);
                //        var s = xRichEditBox.Document.Selection.StartPosition;
                //        var selectedText = xRichEditBox.Document.Selection;
                //        if (i > 0 && !_originalCharFormat.ContainsKey(s))
                //        {
                //            _originalCharFormat.Add(s, selectedText.CharacterFormat.GetClone());
                //        }
                //        if (selectedText != null)
                //        {
                //            selectedText.CharacterFormat.BackgroundColor = Colors.Yellow;
                //        }
                //        xRichEditBox.Document.Selection.Collapse(false);
                //    }
                //    xRichEditBox.Document.Selection.StartPosition = 0;
                //    xRichEditBox.Document.Selection.EndPosition = 0;
                //    i = 1;

            }
            xRichEditBox.Document.SetText(TextSetOptions.FormatRtf, newRtf);
        }

        /// <summary>
        /// Adds highlight tags to all strings that match the query in the text
        /// </summary>
        private int _highlightNum;
        private string InsertHighlight(string rtf, string query)
        {
            int[] modIndex = ModIndexOf(rtf.ToLower(), query);
            int i = modIndex[0];
            int len = modIndex[1];

            if (i >= 0)
            {
                string toHighlight = rtf.Substring(i, len);
                if (toHighlight.IndexOf("\\highlight") > 0)
                {
                    int highlightEnd = 12;
                    int secondDig = rtf[11] - '0';
                    if (secondDig >= 0 && secondDig < 10)
                    {
                        highlightEnd += 1;
                    }
                    toHighlight = toHighlight.Substring(0, i + 1) + toHighlight.Substring(highlightEnd);
                }
                return rtf.Substring(0, i) + $"\\highlight{_colorParamsCount} " + toHighlight + $"\\highlight{_highlightNum} " + InsertHighlight(rtf.Substring(i + len), query); ;
            }
            return rtf;
        }

        /// <summary>
        /// Gets the index of query while ignoring instances in escaped rtf.
        /// Also gets the length of the match, which will differ in the case that there is escaped rtf inside the query.
        /// </summary>
        private int[] ModIndexOf(string text, string query)
        {
            int len = query.Length;
            int matchCount = 0;
            int matchWithFormat = 0;
            int highlightIndex = 0;
            bool ignore = false;
            int pict = 0; //the number of curly braces (once the entire pict is closed, then we can continue search)
            int[] modIndex = new int[2];

            for (int i = 0; i < text.Length; i++)
            {
                if (pict > 0)
                {
                    if (text[i] == '{')
                        pict += 1;
                    else if (text[i] == '}')
                        pict -= 1;
                }
                else if (ignore)
                {
                    if (text.Length > i + 6 && text.Substring(i, 6).Equals("{\\pict"))
                    {
                        pict += 1;
                        ignore = false;
                    }
                    else if (highlightIndex == 9)
                    {
                        int secondDig = text[i + 1] - '0';
                        if (secondDig >= 0 && secondDig < 10)
                        {
                            _highlightNum = (text[i] - '0') * 10 + secondDig;
                        }
                        else
                            _highlightNum = text[i] - '0';
                        highlightIndex = 0;
                    }
                    else if ("highlight"[highlightIndex] == text[i])
                        highlightIndex += 1;
                    else
                        highlightIndex = 0;
                    if (matchCount > 0)
                        matchWithFormat += 1;
                    if (text[i] == ' ' || text[i] == '\n' || text[i] == '\r' || text[i] == '\t')
                        ignore = false;
                }
                else
                {
                    if (text.Length > i + 6 && text.Substring(i, 6).Equals("{\\pict"))
                        pict += 1;
                    else if (text[i] == '\\')
                    {
                        if (matchCount > 0)
                            matchWithFormat += 1;
                        ignore = true;
                    }
                    else if (text[i] == query[matchCount])
                    {
                        matchCount += 1;
                        matchWithFormat += 1;
                    }
                    else if (text[i] == query[0])
                    {
                        matchCount = 1;
                        matchWithFormat = 1;
                    }
                    else
                    {
                        matchCount = 0;
                        matchWithFormat = 0;
                    }

                    if (matchCount == len)
                    {

                        modIndex[0] = i - matchWithFormat + 1;
                        modIndex[1] = matchWithFormat;
                        return modIndex;
                    }
                }
            }
            modIndex[0] = -1;
            return modIndex;
        }

        /// <summary>
        /// Selects the next highlighted search result on enter in the xRichEditBox
        /// </summary>
        private void NextResult()
        {
            if (_queries.Count == 0)
                return;
            if (_queryIndex >= _queries.Count())
                _queryIndex = -1;
            _queryIndex += 1;
            if (_queryIndex == _queries.Count)
            {
                xRichEditBox.Document.Selection.StartPosition = 0;
                xRichEditBox.Document.Selection.EndPosition = 0;
            }
            else
            {
                xRichEditBox.Document.Selection.FindText(_queries.ElementAt(_queryIndex), _textLength, FindOptions.None);
                var s = xRichEditBox.Document.Selection.StartPosition;
                var selectedText = xRichEditBox.Document.Selection;
            }
            //if (selectedText != null)
            //{
            //    selectedText.CharacterFormat.BackgroundColor = Colors.Red;
            //}
            //xRichEditBox.Document.Selection.Collapse(false);


            //var keys = _originalCharFormat.Keys;
            //if (keys.Count != 0)
            //{
            //    var start = keys.ElementAt(_nextMatch);
            //    xRichEditBox.Document.Selection.StartPosition = start;
            //    xRichEditBox.Document.Selection.EndPosition = start + _prevQueryLength;
            //    xRichEditBox.Document.Selection.ScrollIntoView(PointOptions.None);
            //    if (_nextMatch < keys.Count - 1)
            //        _nextMatch++;
            //    else
            //        _nextMatch = 0;
            //}
        }

        /// <summary>
        /// Restores the original formatting of the richtextbox before the search was conducted
        /// </summary>
        private void ClearSearchHighlights(bool silent = false)
        {

            if (_originalRtfFormat == null)
                return;
            var s1 = xRichEditBox.Document.Selection.StartPosition;
            var s2 = xRichEditBox.Document.Selection.EndPosition;
            xRichEditBox.Document.SetText(TextSetOptions.FormatRtf, _originalRtfFormat);
            _queryIndex = -1;
            xRichEditBox.Document.Selection.StartPosition = s1;
            xRichEditBox.Document.Selection.EndPosition = s2;

            _originalRtfFormat = null;
            if (_searchHighlight)
            {
                _searchHighlight = false;
            }

            //xRichEditBox.SelectionHighlightColorWhenNotFocused = new SolidColorBrush(Colors.Transparent);
            //foreach (var tuple in _originalCharFormat)
            //{
            //    xRichEditBox.Document.Selection.StartPosition = tuple.Key;
            //    xRichEditBox.Document.Selection.EndPosition = tuple.Key + _prevQueryLength;
            //    xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = tuple.Value;
            //}
            //xRichEditBox.Document.Selection.Collapse(true);
            //if (!silent)
            //    UpdateDocumentFromXaml();
            //_originalCharFormat.Clear();
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
                Color clone;
                _originalCharFormat.TryGetValue(start, out clone);
                if (clone != null)
                {
                    xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = clone;
                    _originalCharFormat.Remove(start);
                    if (_nextMatch >= _originalCharFormat.Keys.Count || _nextMatch == 0)
                        _nextMatch = 0;
                    else
                        _nextMatch--;
                    NextResult();
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
            
        private void XReplaceModeButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //    noCollapse = true;
            //    replace = xReplaceBoxPanel.Visibility == Visibility.Collapsed;
            //    if (replace)
            //    {
            //        xReplaceBoxPanel.Visibility = Visibility.Visible;
            //        xReplaceModeButton.Content = "▲";

            //    } else
            //    {
            //        xReplaceBoxPanel.Visibility = Visibility.Collapsed;
            //        xReplaceModeButton.Content = "▼";
            //    }
        }
    }
}
