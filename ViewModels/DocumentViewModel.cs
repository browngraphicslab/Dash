using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Point = Windows.Foundation.Point;
using System.Linq;

namespace Dash
{
    public enum IconTypeEnum { Document, Collection, Api } // on super-collapse, what icon is displayed?

    public class DocumentViewModel : ViewModelBase, IDisposable
    {
        public event EventHandler DeleteRequested;
        // == MEMBERS, GETTERS, SETTERS ==
        private TransformGroupData _normalGroupTransform = new TransformGroupData(new Point(), new Point(1, 1));
        private Thickness          _searchHighlightState = DocumentViewModel.UnHighlighted;
        private FrameworkElement   _content = null;
        private SolidColorBrush    _searchHighlightBrush;
        private DocumentController _documentController;

        public static Thickness    Highlighted = new Thickness(8), UnHighlighted = new Thickness(0);
        public bool                IsSelected => SelectionManager.SelectedDocViewModels.Contains(this);

        // == CONSTRUCTOR ==
        public DocumentViewModel(DocumentController documentController) : base()
        {
            _documentController = documentController;
            DocumentController.AddWeakFieldUpdatedListener(this, KeyStore.XamlKey,         (dvm, ctrl, e) => Content=null);
            DocumentController.AddWeakFieldUpdatedListener(this, KeyStore.DocumentTypeKey, (dvm, ctrl, e) => Content=null);
            // DocumentController.AddWeakFieldUpdatedListener(this, KeyStore.DataKey, (dvm, ctrl, e) => ???);
            SearchHighlightBrush = ColorConverter.HexToBrush("#fffc84");

            if (LayoutDocument.GetDereferencedField<NumberController>(KeyStore.IconTypeFieldKey, null) == null)
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

        public DocumentController DocumentController => _documentController;
        public DocumentController DataDocument       => DocumentController.GetDataDocument();
        public DocumentController LayoutDocument     => DocumentController;
        public bool               IsHighlighted      => SearchHighlightState == Highlighted;
        public bool               ResizersVisible     { get; set; } = true;
        public bool               InsetDecorations    { get; set; }
        public bool               DragAllowed         { get; set; } = true;
        public bool               IsDimensionless     { get; set; }
        public Thickness          SearchHighlightState
        {
            get => _searchHighlightState;
            private set => SetProperty(ref _searchHighlightState, value);
        }
        public SolidColorBrush    SearchHighlightBrush
        {
            get => _searchHighlightBrush;
            set => SetProperty(ref _searchHighlightBrush, value);
        }
        public void               SetSearchHighlightState(bool? highlight)
        {
            SearchHighlightState = (highlight ?? !IsHighlighted) ? Highlighted:UnHighlighted;
        }

        /// <summary>
        /// The displayed XAML content for the document.  This gets created when it is accessed.
        /// Setting to null generates property changed callbacks which will cause someone to re-inquire the Content which will recompute it.
        /// </summary>
        public FrameworkElement Content
        {
            get => _content ?? (_content = LayoutDocument.MakeViewUI());
            private set => SetProperty(ref _content, value);
        }

        public void    RequestDelete()
        {
            if (DeleteRequested != null)
            {
                DeleteRequested.Invoke(null, null);
            }
        }
        public void    Dispose()  { System.Diagnostics.Debug.WriteLine("Diposing dvm:" + DocumentController?.Tag); }

        protected bool         Equals(DocumentViewModel other)
        {
            return Equals(LayoutDocument, other.LayoutDocument);
        }
        public override bool   Equals(object obj)
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
        public override int    GetHashCode()
        {
            return LayoutDocument.GetHashCode();
        }
    }
}
