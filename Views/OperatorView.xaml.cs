using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;
using DashShared;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class OperatorView : UserControl
    {
        public delegate void IODragEventHandler(IOReference ioReference);

        /// <summary>
        /// Event that gets fired when an ellipse is dragged off of and a connection should be started
        /// </summary>
        public event IODragEventHandler IoDragStarted;

        /// <summary>
        /// Event that gets fired when an ellipse is dragged on to and a connection should be ended
        /// </summary>
        public event IODragEventHandler IoDragEnded;

        /// <summary>
        /// Reference to either a field input or output with other information about the pointer
        /// </summary>
        public class IOReference
        {
            public ReferenceFieldModelController ReferenceFieldModel { get; set; }
            public bool IsOutput { get; set; }

            public PointerRoutedEventArgs PointerArgs { get; set; }

            public FrameworkElement FrameworkElement { get; set; }
            public DocumentView ContainerView { get; set; }

            public IOReference(ReferenceFieldModelController referenceFieldModel, bool isOutput, PointerRoutedEventArgs args, FrameworkElement e, DocumentView container)
            {
                ReferenceFieldModel = referenceFieldModel;
                IsOutput = isOutput;
                PointerArgs = args;
                FrameworkElement = e;
                ContainerView = container;
            }
        }

        public OperatorView()
        {
            this.InitializeComponent();
        }

        private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var reference = DataContext as ReferenceFieldModel;
            
            var opCont = ContentController.GetController<DocumentController>(reference.DocId).GetField(reference.FieldKey) as OperatorFieldModelController;
            Debug.Assert(opCont != null);
            InputListView.ItemsSource = opCont.InputKeys;
            OutputListView.ItemsSource = opCont.OutputKeys;
        }

        /// <summary>
        /// Can return the position of the click in screen space;
        /// Gets the OperatorFieldModel associated with the input ellipse that is clicked 
        /// </summary>
        private void InputEllipse_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                string docId = (DataContext as ReferenceFieldModel).DocId;
                Ellipse el = sender as Ellipse;
                Key outputKey = el.DataContext as Key;
                IOReference ioRef = new IOReference(new ReferenceFieldModelController(docId, outputKey), false, e, el, el.GetFirstAncestorOfType<DocumentView>());
                CollectionView view = this.GetFirstAncestorOfType<CollectionView>();
                view.StartDrag(ioRef);
                //OnIoDragStarted(ioRef);
            }
        }

        /// <summary>
        /// Can return the position of the click in screen space;
        /// Gets the OperatorFieldModel associated with the output ellipse that is clicked 
        /// </summary>
        private void OutputEllipse_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                string docId = (DataContext as ReferenceFieldModel).DocId;
                Ellipse el = sender as Ellipse;
                Key outputKey = el.DataContext as Key;
                IOReference ioRef = new IOReference(new ReferenceFieldModelController(docId, outputKey), true, e, el, el.GetFirstAncestorOfType<DocumentView>());
                CollectionView view = this.GetFirstAncestorOfType<CollectionView>();
                view.StartDrag(ioRef);
                //OnIoDragStarted(ioRef);
            }
        }

        /// <summary>
        /// Keep operator view from moving when you drag on an ellipse
        /// </summary>
        private void Ellipse_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Complete();
        }

        private void OnIoDragStarted(IOReference ioreference)
        {
            IoDragStarted?.Invoke(ioreference);
        }

        private void OnIoDragEnded(IOReference ioreference)
        {
            IoDragEnded?.Invoke(ioreference);
        }


        private void InputEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            string docId = (DataContext as ReferenceFieldModel).DocId;
            Ellipse el = sender as Ellipse;
            Key outputKey = el.DataContext as Key;
            IOReference ioRef = new IOReference(new ReferenceFieldModelController(docId, outputKey), false, e, el, el.GetFirstAncestorOfType<DocumentView>());
            CollectionView view = this.GetFirstAncestorOfType<CollectionView>();
            view.EndDrag(ioRef);
            //OnIoDragEnded(ioRef);
        }

        private void OutputEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            string docId = (DataContext as ReferenceFieldModel).DocId;
            Ellipse el = sender as Ellipse;
            Key outputKey = el.DataContext as Key;
            IOReference ioRef = new IOReference(new ReferenceFieldModelController(docId, outputKey), true, e, el, el.GetFirstAncestorOfType<DocumentView>());
            CollectionView view = this.GetFirstAncestorOfType<CollectionView>();
            view.EndDrag(ioRef);
            //OnIoDragEnded(ioRef);
        }
    }
}