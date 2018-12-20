using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using static Dash.DocumentController;
using Point = Windows.Foundation.Point;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public enum IconTypeEnum { Document, Collection, Api } // on super-collapse, what icon is displayed?

    public class DocumentViewModel : ViewModelBase, IDisposable
    {
        public event EventHandler DeleteRequested;
        // == MEMBERS, GETTERS, SETTERS ==
        private TransformGroupData _normalGroupTransform = new TransformGroupData(new Point(), new Point(1, 1));
        private bool               _showLocalContext;
        private Thickness          _searchHighlightState = DocumentViewModel.UnHighlighted;
        private FrameworkElement   _content = null;
        private SolidColorBrush    _searchHighlightBrush;

        public static Thickness    Highlighted = new Thickness(8), UnHighlighted = new Thickness(0);

        public double MinWidth = 25;
        public double MinHeight = 10;
        // == CONSTRUCTOR ==
        public DocumentViewModel(DocumentController documentController, Context context = null) : base()
        {
            DocumentController = documentController;
            DocumentController.AddWeakFieldUpdatedListener(this, KeyStore.XamlKey,
                (dvm, controller, arg3) => dvm.DocumentController_ActiveLayoutChanged(controller, arg3));

            DocumentController.AddWeakFieldUpdatedListener(this, KeyStore.DocumentTypeKey,
                (dvm, controller, arg3) => dvm.DocumentController_ActiveLayoutChanged(controller, arg3));
            DocumentController.AddWeakFieldUpdatedListener(this, KeyStore.DataKey,
                (dvm, controller, arg3) => dvm.LayoutDocument_DataChanged(controller, arg3));
            SearchHighlightBrush = ColorConverter.HexToBrush("#fffc84");

            if (IconTypeController == null)
            {
                LayoutDocument.SetField<NumberController>(KeyStore.IconTypeFieldKey, (int)(IconTypeEnum.Document), true);
            }
        }
        ~DocumentViewModel()
        {
            //System.Diagnostics.Debug.WriteLine("Finalize DocumentViewModel " + DocumentController?.Tag + " " + _lastLayout?.Tag);
            //System.Diagnostics.Debug.WriteLine(" ");
            _content = null;
        }

        public void RequestDelete() { if (DeleteRequested != null) DeleteRequested.Invoke(null, null); }

        public DocumentController DocumentController { get; }
        public DocumentController DataDocument => DocumentController.GetDataDocument();
        public DocumentController LayoutDocument => DocumentController;
        public NumberController IconTypeController => LayoutDocument.GetDereferencedField<NumberController>(KeyStore.IconTypeFieldKey, null);
        public bool ResizersVisible = true;
        public bool ShowLocalContext
        {
            get => _showLocalContext;
            set => SetProperty(ref _showLocalContext, value);
        }
        
        public bool Undecorated     { get; set; }
        public bool DragAllowed     { get; set; } = true;
        public bool IsDimensionless { get; set; }
        public bool AreContentsHitTestVisible
        {
            get => DocumentController.GetAreContentsHitTestVisible();
            set {
                DocumentController.SetAreContentsHitTestVisible(value);
                foreach (var rtv in Content.GetDescendantsOfType<RichEditBox>())
                {
                    rtv.IsHitTestVisible = DocumentController.GetAreContentsHitTestVisible();
                }
            }
        }
        /// <summary>
        /// The actual position of the document as written to the LayoutDocument  model
        /// </summary>
        public Point Position
        {
            get => LayoutDocument.GetPosition() ?? new Point();
            set => LayoutDocument.SetPosition(value);
        }
        public double XPos   
        {
            get => Position.X; // infinity causes problems with Bounds and other things expecting a number. double.PositiveInfinity;//Use inf so that sorting works reasonably
            set => Position = new Point(value, YPos);
        }
        public double YPos   
        {
            get => Position.Y; // infinity causes problems with Bounds and other things expecting a number. 
            set => Position = new Point(XPos, value);
        }
        public double Width  
        {
            get => IsDimensionless ? double.NaN : LayoutDocument.GetWidth();
            set {
                LayoutDocument.SetWidth(value);
                OnPropertyChanged("Width");
            }
        }
        public double Height 
        {
            get => IsDimensionless ? double.NaN : LayoutDocument.GetHeight();
            set => LayoutDocument.SetHeight(value);
        }
        public Rect   Bounds     => new TranslateTransform { X = XPos, Y = YPos}.TransformBounds(new Rect(0, 0, ActualSize.X, ActualSize.Y));
        public Point  ActualSize => LayoutDocument.GetActualSize() ?? new Point();


        public void Resize(Point delta, Point cumulativeDelta, bool isShiftPressed, bool shiftTop, bool shiftLeft, bool maintainAspectRatio)
        {
            var isImage = DocumentController.DocumentType.Equals(ImageBox.DocumentType) ||
                          (DocumentController.DocumentType.Equals(CollectionBox.DocumentType) && DocumentController.GetFitToParent()) ||
                          DocumentController.DocumentType.Equals(VideoBox.DocumentType);

            double extraOffsetX = 0;
            if (!double.IsNaN(Width))
            {
                extraOffsetX = ActualSize.X - Width;
            }


            double extraOffsetY = 0;

            if (!double.IsNaN(Height))
            {
                extraOffsetY = ActualSize.Y - Height;
            }

            var oldSize = new Size(ActualSize.X - extraOffsetX, ActualSize.Y - extraOffsetY);

            var oldPos = Position;

            // sets directions/weights depending on which handle was dragged as mathematical manipulations
            var cursorXDirection = shiftLeft ? -1 : 1;
            var cursorYDirection = shiftTop ? -1 : 1;
            var moveXScale = shiftLeft ? 1 : 0;
            var moveYScale = shiftTop ? 1 : 0;

            cumulativeDelta.X *= cursorXDirection;
            cumulativeDelta.Y *= cursorYDirection;

            var w = ActualSize.X - extraOffsetX;
            var h = ActualSize.Y - extraOffsetY;

            double diffX;
            double diffY;

            var aspect = w / h;
            var moveAspect = cumulativeDelta.X / cumulativeDelta.Y;

            bool useX = cumulativeDelta.X > 0 && cumulativeDelta.Y <= 0;
            if (cumulativeDelta.X <= 0 && cumulativeDelta.Y <= 0)
            {

                useX |= maintainAspectRatio ? moveAspect <= aspect : delta.X != 0;
            }
            else if (cumulativeDelta.X > 0 && cumulativeDelta.Y > 0)
            {
                useX |= maintainAspectRatio ? moveAspect > aspect : delta.X != 0;
            }

            var proportional = (!isImage && maintainAspectRatio)
                ? isShiftPressed : (isShiftPressed ^ maintainAspectRatio);
            if (useX)
            {
                aspect = 1 / aspect;
                diffX = cursorXDirection * delta.X;
                diffY = proportional
                    ? aspect * diffX
                    : cursorYDirection * delta.Y; // proportional resizing if Shift or Ctrl is presssed
            }
            else
            {
                diffY = cursorYDirection * delta.Y;
                diffX = proportional
                    ? aspect * diffY
                    : cursorXDirection * delta.X;
            }

            var newSize = new Size(Math.Max(w + diffX, MinWidth), Math.Max(h + diffY, MinHeight));
            // set the position of the doc based on how much it resized (if Top and/or Left is being dragged)
            var newPos = new Point(
                XPos - moveXScale * (newSize.Width - oldSize.Width),
                YPos - moveYScale * (newSize.Height - oldSize.Height));

            if (DocumentController.DocumentType.Equals(AudioBox.DocumentType))
            {
                newSize.Height = oldSize.Height;
                newPos.Y = YPos;
            }

            Position = newPos;
            if (newSize.Width != ActualSize.X)
            {
                Width = newSize.Width;
            }

            if (delta.Y != 0 || isShiftPressed || isImage)
            {
                if (newSize.Height != ActualSize.Y)
                {
                    Height = newSize.Height;
                }
            }
        }

        protected bool Equals(DocumentViewModel other)
        {
            return Equals(LayoutDocument, other.LayoutDocument);
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DocumentViewModel)obj);
        }
        public override string ToString()
        {
            return LayoutDocument.ToString();
        }
        public override int GetHashCode()
        {
            return LayoutDocument.GetHashCode();
        }

        public FrameworkElement Content
        {
            get => _content ?? (_content = LayoutDocument.MakeViewUI()); 
            private set  {
                _content = value; // content will be recomputed when someone accesses Content
                OnPropertyChanged(); // let everyone know that _content has changed
            }
        }

        public Thickness SearchHighlightState
        {
            get => _searchHighlightState;
            private set => SetProperty(ref _searchHighlightState, value);
        }

        public SolidColorBrush SearchHighlightBrush
        {
            get => _searchHighlightBrush;
            set => SetProperty(ref _searchHighlightBrush, value);
        }

        public bool IsHighlighted => SearchHighlightState == Highlighted;

        public void SetHighlight(bool highlight)
        {
            SearchHighlightState = highlight ? Highlighted : UnHighlighted;
        }

        public void ToggleHighlight()
        {
            SetHighlight(!IsHighlighted);
        }

        // == FIELD UPDATED EVENT HANDLERS == 
        // these update the view model's variables when the document's corresponding fields update
        /// <summary>
        /// Called whenever the contents (Data field) of the active Layout document have been changed.
        /// This causes the layout to be re-created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <param name="context"></param>
        private void LayoutDocument_DataChanged(DocumentController sender, DocumentFieldUpdatedEventArgs args)
        {
            // filter out callbacks on prototype from delegate
            // some updates to LayoutDocuments are not bound to the UI.  In these cases, we need to rebuild the UI.
            //   bcz: need some better mechanism than this....
            //if (LayoutDocument.DocumentType.Equals(DataBox.DocumentType) || //TODO Is this necessary? It causes major issues with the KVP - tfs
            //    //|| LayoutDocument.DocumentType.Equals(TemplateBox.DocumentType)
            //    )
            //{
            //    if (args != null && args.FieldArgs is ListController<DocumentController>.ListFieldUpdatedEventArgs largs &&
            //        (largs.ListAction == ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Content ))
            //        ;
            //    else
            //        Content = null; // forces layout to be recomputed by listeners who will access Content
            //}
            //else 
            if (LayoutDocument.DocumentType.Equals(CollectionBox.DocumentType))
            {
                //if (args?.FieldArgs is ListController<DocumentController>.ListFieldUpdatedEventArgs largs &&
                //   (largs.ListAction == ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Content ||
                //     largs.ListAction == ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add ||
                //     largs.ListAction == ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Remove))
                //    ;
                //else
                //    Content = null; // forces layout to be recomputed by listeners who will access Content
            }
        }

        /// <summary>
        /// Called when the ActiveLayout field of the Layout document has changed (or a field on the ActiveLayout).
        /// Such a change requires that the layout view be re-created.  
        /// If the layout was changed on a prototype and the instance doesn't mask the field, then this instance makes 
        /// a delegate of the prototype's activeLayout field. Otherwise, the instance would share the position, 
        /// size, etc of the prototype and changes to the instance would affect the prototype.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="args"></param>
        /// <param name="context"></param>
        private void DocumentController_ActiveLayoutChanged(DocumentController doc, DocumentFieldUpdatedEventArgs args)
        {
            Content = null;
            //if (args.Action == FieldUpdatedAction.Remove)
            //{
            //    Content = null;
            //    _lastLayout?.RemoveFieldUpdatedListener(KeyStore.DataKey, LayoutDocument_DataChanged);
            //    _lastLayout = LayoutDocument;
            //    LayoutDocument.AddFieldUpdatedListener(KeyStore.DataKey, LayoutDocument_DataChanged);
            //    LayoutDocument_DataChanged(null, null, new Context(DocumentController));
            //}
            //else
            //{
            //    var fargs = (args.FieldArgs as DocumentFieldUpdatedEventArgs)?.Reference.FieldKey;
            //    // test that the ActiveLayout field changed and not one of the fields on the ActiveLayout.
            //    // if a field of the activelayout changed, we ignore that here since it should update the layout directly
            //    // through bindings.
            //    if (fargs == null && _lastLayout != LayoutDocument)
            //    {
            //        var curActive = DocumentController.GetField(KeyStore.DocumentTypeKey, true) as DocumentController;
            //        if (curActive == null)
            //        {
            //            curActive = LayoutDocument.GetViewInstance(_lastLayout.GetPosition() ?? new Point());
            //            curActive.SetField(KeyStore.DocumentContextKey, DataDocument, true);
            //            DocumentController.SetField(KeyStore.DocumentTypeKey, curActive, true);
            //        }
            //        _lastLayout.RemoveFieldUpdatedListener(KeyStore.DataKey, LayoutDocument_DataChanged);
            //        _lastLayout = LayoutDocument;
            //        LayoutDocument.AddFieldUpdatedListener(KeyStore.DataKey, LayoutDocument_DataChanged);
            //        LayoutDocument_DataChanged(null, null, new Context(DocumentController));
            //    }
            //}
        }
        public void Dispose()
        {
            System.Diagnostics.Debug.WriteLine("Diposing dvm:" + DocumentController?.Tag);
        }
    }
}
