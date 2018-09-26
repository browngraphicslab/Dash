﻿using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using static Dash.DocumentController;
using Point = Windows.Foundation.Point;

namespace Dash
{
    public enum IconTypeEnum { Document, Collection, Api } // on super-collapse, what icon is displayed?

    public class DocumentViewModel : ViewModelBase, IDisposable
    {
        // == MEMBERS, GETTERS, SETTERS ==
        private DocumentController _lastLayout = null;
        private TransformGroupData _normalGroupTransform = new TransformGroupData(new Point(), new Point(1, 1));
        private bool               _showLocalContext;
        private bool               _decorationState = false;
        private Thickness          _searchHighlightState = DocumentViewModel.UnHighlighted;
        private FrameworkElement   _content = null;

        public static Thickness    Highlighted = new Thickness(8), UnHighlighted = new Thickness(0);

        // == CONSTRUCTOR ==
        public DocumentViewModel(DocumentController documentController, Context context = null) : base()
        {
            DocumentController = documentController;
            _lastLayout = LayoutDocument;

            SearchHighlightBrush = ColorConverter.HexToBrush("#fffc84");

            if (IconTypeController == null)
            {
                LayoutDocument.SetField<NumberController>(KeyStore.IconTypeFieldKey, (int)(IconTypeEnum.Document), true);
            }
        }
        
        public void Load()
        {
            //UnLoad();
            DocumentController.AddFieldUpdatedListener(KeyStore.ActiveLayoutKey, DocumentController_ActiveLayoutChanged);
            _lastLayout.AddFieldUpdatedListener(KeyStore.DataKey, LayoutDocument_DataChanged);
        }

        public void UnLoad()
        {
            DocumentController.RemoveFieldUpdatedListener(KeyStore.ActiveLayoutKey, DocumentController_ActiveLayoutChanged);
            _lastLayout.RemoveFieldUpdatedListener(KeyStore.DataKey, LayoutDocument_DataChanged);
        }


        public DocumentController DocumentController { get; set; }
        public DocumentController DataDocument => DocumentController.GetDataDocument();
        public DocumentController LayoutDocument => DocumentController?.GetActiveLayout() ?? DocumentController;
        public NumberController IconTypeController => LayoutDocument.GetDereferencedField<NumberController>(KeyStore.IconTypeFieldKey, null);
        public bool ResizersVisible = true;
        public bool ShowLocalContext
        {
            get => _showLocalContext;
            set => SetProperty(ref _showLocalContext, value);
        }

        private SolidColorBrush _searchHighlightBrush;
        private bool _isNotBackgroundPinned = true;

        public bool IsDimensionless = false;
        public bool IsNotBackgroundPinned
        {
            get => _isNotBackgroundPinned;
            set => SetProperty(ref _isNotBackgroundPinned, value);
        }
        public bool IsAdornmentGroup
        {
            get => DocumentController.GetIsAdornment();
            set => DocumentController.SetIsAdornment(value);
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
            get => IsDimensionless ? double.NaN : LayoutDocument.GetDereferencedField<NumberController>(KeyStore.WidthFieldKey, null)?.Data ?? 100;
            set => LayoutDocument.SetWidth(value);
        }
        public double Height
        {
            get => IsDimensionless ? double.NaN : LayoutDocument.GetDereferencedField<NumberController>(KeyStore.HeightFieldKey, null).Data;
            set => LayoutDocument.SetHeight(value);
        }
        public Point Scale
        {
            get => LayoutDocument.GetDereferencedField<PointController>(KeyStore.ScaleAmountFieldKey, null)?.Data ?? new Point(1, 1);
            set => LayoutDocument.SetField<PointController>(KeyStore.ScaleAmountFieldKey, value, true);
        }
        public bool DragWithinParentBounds;
        public Rect Bounds => new TranslateTransform { X = XPos, Y = YPos}.TransformBounds(new Rect(0, 0, ActualSize.X * Scale.X, ActualSize.Y * Scale.Y));
        public Point ActualSize { get => LayoutDocument.GetActualSize() ?? new Point(); }
        public string Title { get => DataDocument.Title; }

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
            get => _content ?? (_content = LayoutDocument.MakeViewUI(new Context(DataDocument))); 
            private set  {
                _content = value; // content will be recomputed when someone accesses Content
                OnPropertyChanged(nameof(Content)); // let everyone know that _content has changed
            }
        }

        public bool Undecorated { get; set; }
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
        void LayoutDocument_DataChanged(DocumentController sender, DocumentFieldUpdatedEventArgs args, Context context)
        {
            // filter out callbacks on prototype from delegate
            // some updates to LayoutDocuments are not bound to the UI.  In these cases, we need to rebuild the UI.
            //   bcz: need some better mechanism than this....
            if (LayoutDocument.DocumentType.Equals(StackLayout.DocumentType) ||
                //LayoutDocument.DocumentType.Equals(DataBox.DocumentType) || //TODO Is this necessary? It causes major issues with the KVP - tfs
                LayoutDocument.DocumentType.Equals(GridLayout.DocumentType)
                //|| LayoutDocument.DocumentType.Equals(TemplateBox.DocumentType)
                )
            {
                if (args != null && args.FieldArgs is ListController<DocumentController>.ListFieldUpdatedEventArgs largs &&
                    (largs.ListAction == ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Content ))
                    ;
                else
                    Content = null; // forces layout to be recomputed by listeners who will access Content
            }
            else if (LayoutDocument.DocumentType.Equals(CollectionBox.DocumentType))
            {
                if (args?.FieldArgs is ListController<DocumentController>.ListFieldUpdatedEventArgs largs &&
                   (largs.ListAction == ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Content ||
                     largs.ListAction == ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add ||
                     largs.ListAction == ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Remove))
                    ;
                else
                    Content = null; // forces layout to be recomputed by listeners who will access Content
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
        void DocumentController_ActiveLayoutChanged(DocumentController doc, DocumentFieldUpdatedEventArgs args, Context context)
        {
            if (args.Action == FieldUpdatedAction.Remove)
            {
                Content = null;
                _lastLayout?.RemoveFieldUpdatedListener(KeyStore.DataKey, LayoutDocument_DataChanged);
                _lastLayout = LayoutDocument;
                LayoutDocument.AddFieldUpdatedListener(KeyStore.DataKey, LayoutDocument_DataChanged);
                LayoutDocument_DataChanged(null, null, new Context(DocumentController));
            }
            else
            {
                var fargs = (args.FieldArgs as DocumentFieldUpdatedEventArgs)?.Reference.FieldKey;
                // test that the ActiveLayout field changed and not one of the fields on the ActiveLayout.
                // if a field of the activelayout changed, we ignore that here since it should update the layout directly
                // through bindings.
                if (fargs == null && _lastLayout != LayoutDocument)
                {
                    var curActive = DocumentController.GetField(KeyStore.ActiveLayoutKey, true) as DocumentController;
                    if (curActive == null)
                    {
                        curActive = LayoutDocument.GetViewInstance(_lastLayout.GetPosition() ?? new Point());
                        curActive.SetField(KeyStore.DocumentContextKey, DataDocument, true);
                        DocumentController.SetField(KeyStore.ActiveLayoutKey, curActive, true);
                    }
                    _lastLayout.RemoveFieldUpdatedListener(KeyStore.DataKey, LayoutDocument_DataChanged);
                    _lastLayout = LayoutDocument;
                    LayoutDocument.AddFieldUpdatedListener(KeyStore.DataKey, LayoutDocument_DataChanged);
                    LayoutDocument_DataChanged(null, null, new Context(DocumentController));
                }
            }
        }
        ~DocumentViewModel()
        {
            System.Diagnostics.Debug.WriteLine("Finalize DocumentViewModel " + DocumentController?.Tag + " " + _lastLayout?.Tag);
            System.Diagnostics.Debug.WriteLine(" ");
            _content = null;
        }
        public void Dispose()
        {
            System.Diagnostics.Debug.WriteLine("Diposing dvm:" + DocumentController?.Tag);
            UnLoad();
        }
    }
}
