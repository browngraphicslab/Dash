using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Controllers.Operators;
using System.Diagnostics;

namespace Dash
{
    public class RichEditView : RichEditBox
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(RichTextModel.RTD), typeof(RichEditView), new PropertyMetadata(default(RichTextModel.RTD), xRichEditView_TextChangedCallbackStatic));
        
        /// <summary>
        /// A dictionary of the original character formats of all of the highlighted search results
        /// </summary>
        private Dictionary<int, Color>    _originalCharFormat = new Dictionary<int, Color>();
        private ManipulationControlHelper _manipulator;
        private AnnotationManager         _annotationManager;
        private string                    _lastXamlRTFText = "";
        private Size                      _lastDesiredSize = new Size();
        private string                    _lastSizeRTFText = "";
        private Size                      _lastSizeAvailableSize = new Size();
        private bool                      _hackToIgnoreMeasuringWhenProcessingMarkdown = false;

        protected override Size MeasureOverride(Size availableSize)
        {
            if (_hackToIgnoreMeasuringWhenProcessingMarkdown)
                return _lastDesiredSize;
            if (!double.IsNaN(ViewModel.Width) && DesiredSize.Width >= ViewModel.Width)
            {
                GetChildrenInTabFocusOrder().OfType<Grid>().ToList().ForEach((fe) => { fe.Width = DesiredSize.Width; fe.Height = DesiredSize.Height; });
                return base.MeasureOverride(availableSize);
            }

            var text = getRtfText();
            var readable = getReadableText();
            //if (!string.IsNullOrEmpty(readable) && Document.Selection.EndPosition ==readable.Length && readable.Last() == '\r')
            //    Document.GetText(TextGetOptions.FormatRtf, out text);
            if (text != _lastSizeRTFText || _lastDesiredSize == new Size() || _lastSizeAvailableSize != availableSize)
            {
                var rtb = MainPage.Instance.RTBHackBox;
                rtb.Width = double.IsInfinity(availableSize.Width) ? double.NaN : availableSize.Width;
                rtb.Document.SetText(TextSetOptions.FormatRtf, text);
                rtb.Measure(availableSize);
                _lastSizeRTFText = text;
                _lastDesiredSize = new Size(rtb.DesiredSize.Width+10, rtb.DesiredSize.Height);
                _lastSizeAvailableSize = availableSize;
                GetChildrenInTabFocusOrder().OfType<Grid>().ToList().ForEach((fe) => { fe.Width = rtb.DesiredSize.Width; fe.Height = rtb.DesiredSize.Height; });
            } 
            return _lastDesiredSize;
        }

         ~RichEditView()
        {
            // Debug.WriteLine("Disposing RichEditView");
        }
        public RichEditView()
        {
            AllowDrop = true;
            CanDrag = true;
            Foreground = Application.Current.Resources["TextBrush"] as Brush;
            BorderThickness = new Thickness(0);
            TextWrapping = TextWrapping.Wrap;
            IsColorFontEnabled = true;
            SelectionHighlightColor = Application.Current.Resources["DarkWindowsBlue"] as SolidColorBrush;
            IsTextPredictionEnabled = true;
            Background = new SolidColorBrush(Colors.Transparent);
            Loaded += OnLoaded;
            Unloaded += UnLoaded;
            MinWidth = 1;

            AddHandler(PointerPressedEvent, new PointerEventHandler((s, e) =>
            {
                var docView = this.GetFirstAncestorOfType<DocumentView>();
                if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
                {
                    if (!SelectionManager.IsSelected(docView))
                    {
                        SelectionManager.Select(docView, false);
                        var selection = Document.Selection;
                        string boxText;
                        Document.GetText(TextGetOptions.None, out boxText);
                        var lenght = boxText.Length - 1;
                        selection.StartPosition = lenght;
                        selection.EndPosition = lenght;
                        Focus(FocusState.Keyboard);
                        MenuToolbar.Instance.Update(SelectionManager.GetSelectedDocs());
                    }
                       
                    SelectionManager.TryInitiateDragDrop(docView, e, null);
                }
                _manipulator = !e.IsRightPressed() ? null: new ManipulationControlHelper(this, e, (e.KeyModifiers & VirtualKeyModifiers.Shift) != 0, true);
                DocumentView.FocusedDocument = docView;
                e.Handled = true;
            }), true);
            AddHandler(TappedEvent, new TappedEventHandler(this_Tapped), true);
            AddHandler(PointerMovedEvent, new PointerEventHandler((s, e) => _manipulator?.PointerMoved(s, e)), true);
            AddHandler(PointerReleasedEvent, new PointerEventHandler((s, e) => _manipulator = null), true);

            AddHandler(KeyDownEvent, new KeyEventHandler(XRichEditBox_OnKeyDown), true);
            AddHandler(KeyUpEvent, new KeyEventHandler(XRichEditBox_OnKeyUp), true);

            Drop += (s, e) =>
            {
                if (!MainPage.Instance.IsAltPressed())
                {
                    e.Handled = true;
                    this_Drop(s, e);
                }
            };

            PointerWheelChanged += (s, e) => e.Handled = true;

            GotFocus += (s, e) =>
            {
                var docView = getDocView();
                if (docView != null)
                {
                    docView.CacheMode = null;
                    
                    Clipboard.ContentChanged += Clipboard_ContentChanged;
                }
            };

            TextChanged += (s, e) =>
            {
                var xamlRTF = getRtfText();
                if (xamlRTF != _lastXamlRTFText)
                {
                    _lastXamlRTFText = xamlRTF;  // save the Xaml since so we can know when we get called back about this change not to update the UI and get into a loop.
                    SetValue(TextProperty, new RichTextModel.RTD(xamlRTF)); // push the change to the Dash binding which will update all other views, etc
                }
            };

            LostFocus += (s, e) =>
            {
                if (getDocView() != null)
                {
                    getDocView().CacheMode = new BitmapCache();
                }
                Clipboard.ContentChanged -= Clipboard_ContentChanged;
                var readableText = getReadableText();
                if (string.IsNullOrEmpty(getReadableText()) &&  DataFieldKey.Equals(KeyStore.DataKey))
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
                    Document.SetText(TextSetOptions.None, readableText.Substring(1));
                }
            };

            ContextMenuOpening += (s, e) => e.Handled = true; // suppresses the Cut, Copy, Paste, Undo, Select All context menu from the native view

            SelectionHighlightColorWhenNotFocused = new SolidColorBrush(Colors.Gray) { Opacity = 0.5 };

            //var sizeBinding = new Binding
            //{
            //    Source = SettingsView.Instance,
            //    Path = new PropertyPath(nameof(SettingsView.Instance.NoteFontSize)),
            //    Mode = BindingMode.OneWay
            //};
            //SetBinding(FontSizeProperty, sizeBinding);

            _annotationManager = new AnnotationManager(this);

            SizeChanged += (sender, e) =>
            {
                // we always need to make sure that our own Height is NaN
                // after any kind of resize happens so that we can grow as needed.
                // Height = double.NaN;
                // if we're inside of a RelativePanel that was resized, we need to 
                // reset it to have NaN height so that it can grow as we type.
                //Height = e.NewSize.Height;
                if (Parent is RelativePanel relative)
                {
                    relative.Height = double.NaN;
                }
            };

        }

        private void SelectionManager_SelectionChanged(DocumentSelectionChangedEventArgs args)
        {
            if (DataFieldKey.Equals(KeyStore.DataKey) && string.IsNullOrEmpty(getReadableText()) && FocusManager.GetFocusedElement() != this)
            {
                var docView = getDocView();
                if (args.DeselectedViews.Contains(docView))
                {
                    docView.DeleteDocument();
                }
            }
        }
        public KeyController         DataFieldKey { get; set; }
        public DocumentController    DataDocument => ViewModel?.DataDocument;
        public DocumentController    LayoutDocument => ViewModel?.LayoutDocument;
        public DocumentViewModel     ViewModel => getDocView()?.ViewModel ?? DataContext as DocumentViewModel;  // DataContext as DocumentViewModel;  would prefer to use DataContext, but it can be null when getDocView() is not
        private DocumentView         getDocView() { return this.GetFirstAncestorOfType<DocumentView>(); }
        private IList<TextController> getSelected()
        {
            return DataDocument?.GetDereferencedField<ListController<TextController>>(CollectionDBView.SelectedKey, null)
                ?? LayoutDocument?.GetDereferencedField<ListController<TextController>>(CollectionDBView.SelectedKey, null);
        }
        
        private void SetSelected(string query)
        {
            var value = query.Equals("") ? new ListController<TextController>(new TextController()) : new ListController<TextController>(new TextController(query));
            DataDocument?.SetField(CollectionDBView.SelectedKey, value, true);
        }

        private string getReadableText()
        {
            Document.GetText(TextGetOptions.UseObjectText, out string allText);
            return allText;
        }

        private string getRtfText()
        { 
            Document.GetText(TextGetOptions.FormatRtf, out string allRtfText);
            var strippedRtf = allRtfText.Replace("\r\n\\pard\\tx720\\par\r\n", ""); // RTF editor adds a trailing extra paragraph when queried -- need to strip that off
            return new Regex("\\\\par[\r\n}\\\\]*\0").Replace(strippedRtf, "}\r\n\0");
        }

        #region eventhandlers
        /// <summary>
        /// This gets called every time the Dash binding changes.  So we need to update the RichEditBox here *unless* the change
        /// to the Dash binding was caused by editing this richEditBox (ie, the edit value == lastXamlRTFText), in which case we should do nothing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="dp"></param>
        private static void xRichEditView_TextWrappingChangedCallbackStatic(DependencyObject sender, DependencyPropertyChangedEventArgs dp)
        {
            var rtv = sender as RichEditView;
            rtv.TextWrapping = (TextWrapping) dp.NewValue;
        }

        /// <summary>
        /// This gets called every time the Dash binding changes.  So we need to update the RichEditBox here *unless* the change
        /// to the Dash binding was caused by editing this richEditBox (ie, the edit value == lastXamlRTFText), in which case we should do nothing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="dp"></param>
        private static void xRichEditView_TextChangedCallbackStatic(DependencyObject sender, DependencyPropertyChangedEventArgs dp)
        {
            var rtv = sender as RichEditView;
            var newRtFormatString = ((RichTextModel.RTD)dp.NewValue)?.RtfFormatString;
            if (newRtFormatString != null && newRtFormatString != rtv._lastXamlRTFText)
            {
                rtv.Document.SetText(TextSetOptions.FormatRtf, ((RichTextModel.RTD)dp.NewValue)?.RtfFormatString); // setting the RTF text does not mean that the Xaml view will literally store an identical RTF string to what we passed
                rtv._lastXamlRTFText = rtv.getRtfText(); // so we need to retrieve what Xaml actually stored and treat that as an 'alias' for the format string we used to set the text.
            }
            var documentView = rtv.GetFirstAncestorOfType<DocumentView>();
            if (documentView != null)
            {
                if (rtv.Document.Selection.FindText(HyperlinkText, rtv.getRtfText().Length, FindOptions.Case) != 0)
                {
                    var url = rtv.DataDocument.GetDereferencedField<TextController>(KeyStore.SourceUriKey, null)?.Data;
                    var title = rtv.DataDocument.GetDereferencedField<TextController>(KeyStore.SourceTitleKey, null)?.Data;

                    //this does better formatting/ parsing than the regex stuff can
                    var link = title ?? HtmlToDashUtil.GetTitlesUrl(url);

                    rtv.Document.Selection.CharacterFormat.Size = 9;
                    rtv.Document.Selection.FindText(HyperlinkMarker, rtv.getRtfText().Length, FindOptions.Case);
                    rtv.Document.Selection.CharacterFormat.Size = 8;
                    rtv.Document.Selection.Text = link;
                    rtv.Document.Selection.Link = "\"" + url + "\"";
                    rtv.Document.Selection.CharacterFormat.Underline = UnderlineType.Single;
                    rtv.Document.Selection.EndPosition = rtv.Document.Selection.StartPosition;
                }
            }
            if (double.IsNaN(rtv.Width))
            {
                rtv.InvalidateMeasure();
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
                    if (DataDocument.GetDereferencedField<ListController<DocumentController>>(KeyStore.RegionsKey, null)?.Contains(theDoc) == true)
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

        private void this_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = false;
            RegionSelected(e.GetPosition(MainPage.Instance));
        }

        private void CursorToEnd()
        {
            Document.GetText(TextGetOptions.None, out string text);
            Document.Selection.StartPosition = text.Length;
        }

        private async void this_Drop(object sender, DragEventArgs e)
        {
            e.Handled = false;
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
            

            var cfv = this.GetFirstAncestorOfType<CollectionFreeformView>();
            cfv?.AddToMenu(menu);

            ImageSource source = new BitmapImage(new Uri("ms-appx://Dash/Assets/Rightlg.png"));
            menu.AddAction("BASIC",      new ActionViewModel("Title",      "Add title",            MakeTitleAction,  source));
            menu.AddAction("BASIC",      new ActionViewModel("Center",     "Align text to center", SetCenterAction,  source));
            menu.AddAction("BASIC",      new ActionViewModel("To-Do",      "Create a todo note",   CreateTodoAction, source));
            menu.AddAction("BASIC",      new ActionViewModel("Google",     "Google Clip",          GoogleClip,       source));
            menu.AddAction("BASIC",      new ActionViewModel("Bio",        "Google Bio",           GoogleBio,        source));
            menu.AddAction("TRAVELOGUE", new ActionViewModel("Travelogue", "Create Travelogue",    CreateTravelogue, source));
            MainPage.Instance.xCanvas.Children.Add(menu);
        }

        private async Task<bool> CreateTravelogue(ActionFuncParams actionParams)
        {
            var (collections, tags) = await MainPage.Instance.PromptTravelogue();

            if (collections == null || tags == null)
            {
                return true;
            }

            bool useAll = tags.Contains("INCLUDE ALL TAGS");
            var events = EventManager.GetEvents();

            var eventDocs = new List<DocumentController>();
            foreach (var eventDoc in events)
            {
                if (collections.Contains(eventDoc.GetDataDocument()
                    .GetField<DocumentController>(KeyStore.EventCollectionKey)))
                {
                    var eventTags = eventDoc.GetDataDocument().GetField<TextController>(KeyStore.EventTagsKey).Data.ToUpper()
                        .Split(", ");
                    if (useAll || tags.Any(t => eventTags.Contains(t)))
                    {
                        eventDocs.Add(eventDoc);
                    }
                }
            }

            // create collection
            var collection = new CollectionNote(this.ViewModel.Position, CollectionViewType.Stacking, 500, 500,
                eventDocs);
            collection.Document.SetTitle("Travelogue Created " + DateTime.Now.ToLocalTime().ToString("f"));

            var cfv = this.GetFirstAncestorOfType<CollectionFreeformView>();
            cfv?.ViewModel.AddDocument(collection.Document);

            return true;
        }

        private Task<bool> MakeTitleAction(ActionFuncParams actionParams)
        {
            Document.SetText(TextSetOptions.FormatRtf, "TITLE\n");
            Document.Selection.StartPosition = 0;
            Document.Selection.EndPosition = getReadableText().Length;
            Document.Selection.CharacterFormat.Size = 20;
            this.Bold();
            Document.Selection.StartPosition = getReadableText().Length+1;
            Document.Selection.EndPosition = getReadableText().Length+2;
            Document.Selection.CharacterFormat.Size = 12;
            return Task.FromResult(false);
        }

        private Task<bool> SetCenterAction(ActionFuncParams actionParams)
        {
            Document.SetText(TextSetOptions.FormatRtf, "text");
            TextAlignment = TextAlignment.Center;
            Document.Selection.StartPosition = 0;
            Document.Selection.EndPosition = getReadableText().Length;
            return Task.FromResult(false);
        }

        private Task<bool> CreateTodoAction(ActionFuncParams actionParams)
        {
            var templatedText =
            "{\\rtf1\\fbidis\\ansi\\ansicpg1252\\deff0\\nouicompat\\deflang1033{\\fonttbl{\\f0\\fnil Century Gothic; } {\\f1\\fnil\\fcharset0 Century Gothic; } {\\f2\\fnil\\fcharset2 Symbol; } }" +
            "\r\n{\\colortbl;\\red51\\green51\\blue51; }\r\n{\\*\\generator Riched20 10.0.17134}\\viewkind4\\uc1 \r\n\\pard\\tx720\\cf1\\b{\\ul\\f0\\fs34 My\\~\\f1 Todo\\~List:}\\par" +
            "\r\n\\b0\\f0\\fs24\\par\r\n\r\n\\pard{\\pntext\\f2\\'B7\\tab}{\\*\\pn\\pnlvlblt\\pnf2\\pnindent0{\\pntxtb\\'B7}}\\tx720\\f1\\fs24 Item\\~1\\par" +
            "\r\n{\\pntext\\f2\\'B7\\tab}\\b0 Item\\~2\\par}";
            Document.SetText(TextSetOptions.FormatRtf, templatedText);
            return Task.FromResult(false);
        }
        private async Task<bool> GoogleClip(ActionFuncParams actionParams)
        {
            var templatedText = await QuerySnapshotOperator.QueryGoogle(actionParams.Params?.FirstOrDefault() ?? "");
            Document.SetText(TextSetOptions.FormatRtf, (actionParams.Params?.FirstOrDefault() ?? "") + "-> " + templatedText);
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
                new[] { dbox, html}), true);
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
        /// Create short cuts for the this (ctrl+I creates indentation by default, ctrl-Z will get rid of the indentation, showing only the italized text)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XRichEditBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (double.IsNaN(Width))
            {
                InvalidateMeasure();
            }
            //handles batching for undo typing
            //TypeTimer.typeEvent();
            if (!this.IsCtrlPressed() && !this.IsAltPressed() && !this.IsShiftPressed())
            {
                DataDocument.CaptureNeighboringContext();
            }

            if (e.Key == (VirtualKey)191) // 191 = '/' 
            {
                if (actionMenu == null)
                {
                    var rt = getReadableText();
                    if (rt == "" || rt == "#" || rt == "/" || (this.Document.Selection.EndPosition - this.Document.Selection.StartPosition) >= rt.Length)
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
            if (this.IsShiftPressed() && !e.Key.Equals(VirtualKey.Shift) && e.Key.Equals(VirtualKey.Tab))
            {
                var depth = DataDocument.GetDereferencedField<NumberController>(KeyController.Get("DiscussionDepth"), null)?.Data;
                if (depth is double dep)
                {
                    DataDocument.SetField<NumberController>(KeyController.Get("DiscussionDepth"), Math.Max(0, dep - 1), true);
                    DataDocument.SetField<TextController>(KeyStore.TitleKey, Math.Max(0, dep - 1).ToString(), true);
                    e.Handled = true;
                    getDocView().GetFirstAncestorOfType<DocumentView>().UpdateLayout();
                    return;
                }
                var precontainer = getDocView().GetFirstAncestorOfType<DocumentView>();
                var preprecontainer = precontainer?.GetFirstAncestorOfType<DocumentView>();
                var prereplies = precontainer?.ViewModel.DataDocument.GetDereferencedField<ListController<DocumentController>>(KeyController.Get("Replies"), null);
                var preprereplies = preprecontainer?.ViewModel.DataDocument.GetDereferencedField<ListController<DocumentController>>(KeyController.Get("Replies"), null);
                if (preprereplies != null && prereplies != null)
                {
                    prereplies.Remove(LayoutDocument);
                    preprereplies.Add(LayoutDocument);
                }

                e.Handled = true;
                return;
            }
            else if (e.Key.Equals(VirtualKey.Tab))
            {

                var depth = DataDocument.GetDereferencedField<NumberController>(KeyController.Get("DiscussionDepth"), null)?.Data;
                if (depth is double dep)
                {
                    DataDocument.SetField<NumberController>(KeyController.Get("DiscussionDepth"), dep + 1, true);
                    DataDocument.SetField<TextController>(KeyStore.TitleKey, (dep + 1).ToString(), true);
                    e.Handled = true;
                    getDocView().GetFirstAncestorOfType<DocumentView>().UpdateLayout();
                    return;
                }
            }

            if (this.IsShiftPressed() && !e.Key.Equals(VirtualKey.Shift) && e.Key.Equals(VirtualKey.Enter))
            {
                var depth = DataDocument.GetDereferencedField<NumberController>(KeyController.Get("DiscussionDepth"), null)?.Data;
                if (depth is double dep)
                {
                    var rt = new RichTextNote("").Document;
                    MainPage.Instance.SetForceFocusPoint(null, TransformToVisual(MainPage.Instance).TransformPoint(new Point(10, ActualHeight + 10)));
                    rt.GetDataDocument().SetField<NumberController>(KeyController.Get("DiscussionDepth"), dep, true);
                    var parent = getDocView().GetFirstAncestorOfType<DocumentView>();
                    var items = parent.ViewModel.DataDocument.GetDereferencedField<ListController<DocumentController>>(KeyController.Get("DiscussionItems"), null);
                    items.Insert(items.IndexOf(LayoutDocument) + 1, rt);
                    e.Handled = true;
                    return;
                }
                var xamlReplies = @"<Grid
                                    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                                    xmlns:dash=""using:Dash""
                                    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
                                    Background=""Beige"">
                                    <dash:HierarchicalText />
                                </Grid>";
                Document.Selection.MoveStart(TextRangeUnit.Character, -1);
                Document.Selection.Delete(TextRangeUnit.Character, 1);
                MainPage.Instance.SetForceFocusPoint(null, TransformToVisual(MainPage.Instance).TransformPoint(new Point(15, ActualHeight + 5)));
                var replies = DataDocument.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyController.Get("Replies"));
                var rtn = new RichTextNote("").Document;
                var xaml = LayoutDocument.GetDereferencedField<TextController>(KeyStore.XamlKey,null)?.Data;
                if (xaml == null)
                {
                    xaml = xamlReplies;
                    LayoutDocument.SetField<TextController>(KeyStore.XamlKey, xaml, true);
                }
                rtn.SetField<TextController>(KeyStore.XamlKey, xaml, true);
                replies.Add(rtn);
                var found = VisualTreeHelper.FindElementsInHostCoordinates((Point)MainPage.Instance.ForceFocusPoint, this).ToList().OfType<RichEditBox>();
                e.Handled = true;
            }
            if (this.IsAltPressed() && !e.Key.Equals(VirtualKey.Menu) && e.Key.Equals(VirtualKey.Right))
            {
                if (Document.Selection.EndPosition < getReadableText().Length - 1)
                {
                    var clone = Document.Selection.CharacterFormat.GetClone();
                    Document.Selection.MoveEnd(TextRangeUnit.Character, -1);
                    Document.Selection.CharacterFormat.SetClone(clone);
                }
                var s1 = Document.Selection.StartPosition;
                Document.Selection.ParagraphFormat.Alignment = ParagraphAlignment.Right;
                Document.Selection.SetRange(s1, s1);
                e.Handled = true;
            }
            if (this.IsAltPressed() && !e.Key.Equals(VirtualKey.Menu) && e.Key.Equals(VirtualKey.Left))
            {
                var clone = Document.Selection.CharacterFormat.GetClone();
                Document.Selection.MoveStart(TextRangeUnit.Character, 1);
                Document.Selection.CharacterFormat.SetClone(clone);
                var s1 = Document.Selection.StartPosition;
                Document.Selection.ParagraphFormat.Alignment = ParagraphAlignment.Left;
                Document.Selection.SetRange(s1, s1);
                e.Handled = true;
            }


            if (e.Key.Equals(VirtualKey.Escape))
            {
                if (actionMenu == null)
                {
                    var tab = IsTabStop;
                    IsTabStop = false;
                    IsEnabled = false;
                    IsEnabled = true;
                    IsTabStop = tab;
                }
                else
                {
                    e.Handled = true;
                }
            }

            else if (this.IsTabPressed())
            {
                Document.Selection.TypeText("\t");
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
                case VirtualKey.L:
                    if (this.IsShiftPressed())
                    {
                        if (Document.Selection.ParagraphFormat.ListType == MarkerType.UppercaseEnglishLetter)
                        {
                            Document.Selection.ParagraphFormat.ListType = MarkerType.None;
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
                var transformToVisual = TransformToVisual(MainPage.Instance.xCanvas);
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
            var s1 = Document.Selection.StartPosition;
            var s2 = Document.Selection.EndPosition;
            var origFormat = Document.Selection.CharacterFormat.GetClone();
            var origAlign = Document.Selection.ParagraphFormat.Alignment;
            var align = origAlign;
            var hashcount = 0;
            var extracount = 0;

            for (int i = s1 - 2; i >= 0; i--)
            {
                Document.Selection.SetRange(i, i + 1);
                string text = Document.Selection.Text;
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
                    Document.Selection.SetRange(i + 1, i + 2);
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
                _hackToIgnoreMeasuringWhenProcessingMarkdown = true;
                Document.Selection.SetRange(Document.Selection.StartPosition,
                                                         Document.Selection.StartPosition + hashcount + extracount);
                if (Document.Selection.StartPosition == 0)
                    CollectionFreeformBase.PreviewFormatString = Document.Selection.Text;
                Document.Selection.Text = "";
                Document.Selection.SetRange(Document.Selection.StartPosition, s2);
                Document.Selection.CharacterFormat.Bold = hashcount > 0 ? FormatEffect.On : origFormat.Bold;
                Document.Selection.ParagraphFormat.Alignment = align;
                Document.Selection.CharacterFormat.Size = origFormat.Size + hashcount * 5;

                var text = ViewModel.DataDocument.GetField<DateTimeController>(KeyStore.DateCreatedKey).Data.ToString("g") +
                           " | Created a text note:";
                var eventDoc = new RichTextNote(text).Document;
                var tags = "rich text, note, " + Document.Selection.Text.Substring(0, Document.Selection.Text.Length - 2);
                eventDoc.GetDataDocument().SetField<TextController>(KeyStore.EventTagsKey, tags, true);
                eventDoc.GetDataDocument().SetField(KeyStore.EventCollectionKey,
                    this.GetFirstAncestorOfType<DocumentView>().ParentCollection.ViewModel.ContainerDocument, true);
                eventDoc.SetField(KeyStore.EventDisplay1Key, ViewModel.DocumentController, true);
                var displayXaml =
                    @"<Grid
                            xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                            xmlns:dash=""using:Dash""
                            xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"">
                            <Grid.RowDefinitions>
                                <RowDefinition Height=""Auto""></RowDefinition>
                                <RowDefinition Height=""*""></RowDefinition>
                                <RowDefinition Height=""*""></RowDefinition>
                            </Grid.RowDefinitions>
                            <Border BorderThickness=""2"" BorderBrush=""CadetBlue"" Background=""White"">
                                <TextBlock x:Name=""xTextFieldData"" HorizontalAlignment=""Stretch"" Height=""Auto"" VerticalAlignment=""Top""/>
                            </Border>
                            <ScrollViewer Height=""200"" Grid.Row=""2"" VerticalScrollBarVisibility=""Visible"">
                                <StackPanel Orientation=""Horizontal"" Grid.Row=""2"">
                                    <dash:DocumentView x:Name=""xDocumentField_EventDisplay1Key""
                                        Foreground=""White"" HorizontalAlignment=""Stretch"" Grid.Row=""2""
                                        VerticalAlignment=""Top"" />
                                </StackPanel>
                            </ScrollViewer>
                            </Grid>";
                EventManager.EventOccured(eventDoc, displayXaml);
            }
            Document.Selection.SetRange(s1, s2);
            Document.Selection.CharacterFormat = origFormat;
            _hackToIgnoreMeasuringWhenProcessingMarkdown = false;
        }

        private async void Clipboard_ContentChanged(object sender, object e)
        {
            Clipboard.ContentChanged -= Clipboard_ContentChanged;
            var clipboardContent = Clipboard.GetContent();
            if (clipboardContent.Properties[nameof(DocumentController)]?.Equals(LayoutDocument) != true)
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(await clipboardContent.GetTextAsync());
                //set RichEditView property to this view
                dataPackage.Properties[nameof(DocumentController)] = LayoutDocument;
                Clipboard.SetContent(dataPackage);
            }
            Clipboard.ContentChanged += Clipboard_ContentChanged;
        }


        #endregion

        #region load/unload

        private void UnLoaded(object s, RoutedEventArgs e)
        {
            SetSelected("");
            SelectionManager.SelectionChanged -= SelectionManager_SelectionChanged;
        }

        public const string HyperlinkMarker = "<hyperlink marker>";
        public const string HyperlinkText = "\r Text from: " + HyperlinkMarker;

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        { 
            if (GetValue(TextProperty) is RichTextModel.RTD xamlText)
            {
                Document.SetText(TextSetOptions.FormatRtf, xamlText.RtfFormatString); // setting the RTF text does not mean that the Xaml view will literally store an identical RTF string to what we passed
            }
            _lastXamlRTFText = getRtfText(); // so we need to retrieve what Xaml actually stored and treat that as an 'alias' for the format string we used to set the text.

            //DataDocument.AddWeakFieldUpdatedListener(this, CollectionDBView.SelectedKey, (view, controller, arg3) => view.selectedFieldUpdatedHdlr(controller, arg3));
            if (MainPage.Instance.ForceFocusPoint != null && this.GetBoundingRect(MainPage.Instance).Contains((Windows.Foundation.Point)MainPage.Instance.ForceFocusPoint))
            {
                MainPage.Instance.ClearForceFocus();
                GotFocus += RichTextView_GotFocus;
                SelectionManager.SelectionChanged += SelectionManager_SelectionChanged;
                Focus(FocusState.Programmatic);
            }
            if (DataDocument.GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null)?.Data == "/" && this == FocusManager.GetFocusedElement())
            {
                CreateActionMenu(this);
            }
            var documentView = this.GetFirstAncestorOfType<DocumentView>();
            if (documentView != null)
            {
                Document.Selection.FindText(HyperlinkText, getRtfText().Length, FindOptions.Case);
                if (Document.Selection.StartPosition != Document.Selection.EndPosition)
                {
                    var url = DataDocument.GetDereferencedField<TextController>(KeyStore.SourceUriKey, null)?.Data;
                    var title = DataDocument.GetDereferencedField<TextController>(KeyStore.SourceTitleKey, null)?.Data;

                    //this does better formatting/ parsing than the regex stuff can
                    var link = title ?? HtmlToDashUtil.GetTitlesUrl(url);

                    Document.Selection.CharacterFormat.Size = 9;
                    Document.Selection.FindText(HyperlinkMarker, getRtfText().Length, FindOptions.Case);
                    Document.Selection.CharacterFormat.Size = 8;
                    Document.Selection.Text = link;
                    Document.Selection.Link = "\"" + url + "\"";
                    Document.Selection.CharacterFormat.Underline = UnderlineType.Single;
                    Document.Selection.EndPosition = Document.Selection.StartPosition;
                }
            }
        }
        private void RichTextView_GotFocus(object sender, RoutedEventArgs e)
        {
            GotFocus -= RichTextView_GotFocus;
            var text = MainPage.Instance.TextPreviewer?.Visibility == Visibility.Visible ? MainPage.Instance.TextPreviewer.PreviewTextBuffer : "";
            Document.Selection.SetRange(0, 0);
            Document.SetText(TextSetOptions.None, text);
            Document.Selection.CharacterFormat.Bold = FormatEffect.On;
            Document.Selection.SetRange(text.Length, text.Length);
            SelectionManager.Select(getDocView(), false);
        }

        #endregion

        #region hyperlink

        public DocumentController GetRegionDocument()
        {
            if (!string.IsNullOrEmpty(Document.Selection.Text))
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
            var s1 = Document.Selection.StartPosition;
            var s2 = Document.Selection.EndPosition;
            if (s1 == s2)
            {
                Document.Selection.SetRange(s1, s2 + 1);
            }

            string target = Document.Selection.Link.Length > 1 ? Document.Selection.Link.Split('\"')[1] : null;

            if (Document.Selection.EndPosition != s2)
                Document.Selection.SetRange(s1, s2);
            return target;
        }

        private void linkDocumentToSelection(DocumentController theDoc, bool forceLocal)
        {
            var s1 = Document.Selection.StartPosition;
            var s2 = Document.Selection.EndPosition;

            using (UndoManager.GetBatchHandle())
            {
                if (string.IsNullOrEmpty(getSelected()?.First()?.Data))
                {
                    if (theDoc != null && s1 == s2) Document.Selection.Text = theDoc.Title;
                }


                var region = GetRegionDocument();
                region.Link(theDoc, LinkBehavior.Annotate);

                //convertXamlToDash();

                Document.Selection.SetRange(s1, s2);
            }
        }

        private DocumentController createRTFHyperlink()
        {
            var selectedText = Document.Selection.Text;
            var start = Document.Selection.StartPosition;
            var length = Document.Selection.EndPosition - start;
            string link = null;
            DocumentController targetRegionDocument = null;

            var origStart = start;

            while (true)
            {
                bool replaced = false;
                for (int i = 0; i <= length; i++)
                {
                    Document.Selection.SetRange(start + i, start + i + 1);
                    if (Document.Selection.Link != "")
                    {
                        if (Document.Selection.Link.StartsWith("\"http"))
                        {
                            length -= Document.Selection.FormattedText.Link.Length + " HYPERLINK".Length + 1;
                            Document.Selection.FormattedText.Link = "";
                            replaced = true;
                        }
                        else if (Document.Selection.Link.StartsWith("\"mailto"))
                        {
                            length -= Document.Selection.FormattedText.Link.Length + " HYPERLINK".Length + 1;
                            Document.Selection.FormattedText.Link = "";
                            replaced = true;
                        }
                    }
                }
                if (!replaced)
                    break;
            }

            for (int i = 0; i <= length; i++)
            {
                Document.Selection.StartPosition = start + i;
                Document.Selection.EndPosition = start + i + 1;
                if (Document.Selection.Link != "")
                {
                    var color = Document.Selection.CharacterFormat.BackgroundColor;
                    var nextColor = color == Colors.LightCyan ? Colors.LightBlue : color == Colors.LightBlue ? Colors.DeepSkyBlue : Colors.Cyan;
                    Document.Selection.CharacterFormat.BackgroundColor = nextColor;
                    // maybe get the current link and add the new link doc to it?

                    var newstart = Document.Selection.EndPosition;
                    for (int j = 0; j < i; j++)
                    {
                        Document.Selection.StartPosition = start;
                        Document.Selection.EndPosition = start + i - j;
                        if (!Document.Selection.Text.Contains("HYPERLINK"))
                            break;
                    }
                    length -= (newstart - start);
                    if (link == null)
                    {
                        link = getTargetLink(selectedText, out targetRegionDocument);
                    }
                    var cursize = Document.Selection.EndPosition - Document.Selection.StartPosition;
                    Document.Selection.Link = link;
                    var newsize = Document.Selection.EndPosition - Document.Selection.StartPosition;
                    newstart += (newsize - cursize);
                    Document.Selection.CharacterFormat.BackgroundColor = Colors.LightCyan;
                    Document.Selection.SetRange(newstart, newstart + length);
                    start = newstart;
                    i = -1;
                }
            }
            var endpos = Document.Selection.EndPosition - 1;
            Document.Selection.SetRange(start, start + length);
            for (int j = 0; j < length; j++)
            {
                Document.Selection.SetRange(start, start + length - j);
                Document.Selection.SetRange(start, start + length - j);
                if (!Document.Selection.Text.Contains("HYPERLINK"))
                    break;
            }
            if (length > 0 && !string.IsNullOrEmpty(Document.Selection.Text) && !string.IsNullOrWhiteSpace(Document.Selection.Text))
            {
                if (link == null)
                {
                    link = getTargetLink(selectedText, out targetRegionDocument);
                }
                Document.Selection.SetRange(start, start + length);
                // set the hyperlink for the matched text
                Document.Selection.Link = link;
                endpos = Document.Selection.EndPosition;
                Document.Selection.CharacterFormat.BackgroundColor = Colors.LightCyan;
            }
            Document.Selection.SetRange(origStart, endpos);
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
            Document.Selection.SetRange(atPos + 1, s2 - 1);
            string refText;
            Document.Selection.GetText(TextGetOptions.None, out refText);

            return refText;
        }

        private int findPreviousHyperlinkStartMarker(string allText, int s1)
        {
            Document.Selection.SetRange(0, allText.Length);
            var atPos = -1;
            while (Document.Selection.FindText("@", 0, FindOptions.None) > 0)
            {
                if (Document.Selection.StartPosition < s1)
                {
                    atPos = Document.Selection.StartPosition;
                    Document.Selection.SetRange(atPos + 1, allText.Length);
                }
                else break;
            }
            return atPos;
        }

        #endregion
    }
}
