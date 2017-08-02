using System.Collections;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;
using DashShared;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Dash.Controllers.Operators;
using static Dash.Controllers.Operators.DBSearchOperatorFieldModelController;

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
            public FieldReference FieldReference { get; set; }
            public bool IsOutput { get; set; }
            //public bool IsReference { get; set; }

            public PointerRoutedEventArgs PointerArgs { get; set; }

            public FrameworkElement FrameworkElement { get; set; }
            public DocumentView ContainerView { get; set; }

            public FieldModelController FMController { get;  set;}

            public Key FieldKey { get; set; }
            public IOReference(Key fieldKey, FieldModelController controller, FieldReference fieldReference, bool isOutput, PointerRoutedEventArgs args, FrameworkElement e, DocumentView container)
            {
                FieldKey = fieldKey; 
                FMController = controller; 
                FieldReference = fieldReference;
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

        public object OperatorContent
        {
            get { return XPresenter.Content; }
            set { XPresenter.Content = value; }
        }

        private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var opCont = (DataContext as FieldReference).DereferenceToRoot<OperatorFieldModelController>(null);

           
            Binding inputsBinding = new Binding
            {
                Source = opCont.Inputs,
            };
            Binding outputsBinding = new Binding
            {
                Source = opCont.Outputs,
            };
            InputListView.SetBinding(ListView.ItemsSourceProperty, inputsBinding);
            OutputListView.SetBinding(ListView.ItemsSourceProperty, outputsBinding);
            //InputListView.ItemsSource = opCont.Inputs.Keys;
            //OutputListView.ItemsSource = opCont.Outputs.Keys;
        }

        /// <summary>
        /// Envokes handler for starting a link by dragging on a link ellipse handle.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void holdPointerOnEllipse(object sender, PointerRoutedEventArgs e, bool isOutput)
        {
            string docId = (DataContext as DocumentFieldReference).DocumentId;
            FrameworkElement el = sender as FrameworkElement;
            Key outputKey = ((DictionaryEntry)el.DataContext).Key as Key;
            IOReference ioRef = new IOReference(null, null, new DocumentFieldReference(docId, outputKey), isOutput, e, el, el.GetFirstAncestorOfType<DocumentView>()/*, true*/);
            CollectionView view = this.GetFirstAncestorOfType<CollectionView>();
            (view.CurrentView as CollectionFreeformView).CanLink = true;
            (view.CurrentView as CollectionFreeformView).StartDrag(ioRef);
        }


        private void InputEllipse_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            holdPointerOnEllipse(sender, e, false);
        }

        private void OutputEllipse_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            holdPointerOnEllipse(sender, e, true);
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

        /// <summary>
        /// Envokes code to handle pointer released events on the link handle.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        
        void releasePointerOnEllipse(object sender, PointerRoutedEventArgs e, bool isOutput)
        {
            string docId = (DataContext as DocumentFieldReference).DocumentId;
            FrameworkElement el = sender as FrameworkElement;
            Key outputKey = ((DictionaryEntry)el.DataContext).Key as Key;
            IOReference ioRef = new IOReference(null, null, new DocumentFieldReference(docId, outputKey), isOutput, e, el, el.GetFirstAncestorOfType<DocumentView>()/*, true*/);
            CollectionView view = this.GetFirstAncestorOfType<CollectionView>();
            (view.CurrentView as CollectionFreeformView).EndDrag(ioRef);

        }

        private void InputEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            releasePointerOnEllipse(sender, e, false);
        }

        private void OutputEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            releasePointerOnEllipse(sender, e, true);
        }

        /// <summary>
        /// Updates the background circle and rectangle height to accomodate new sizes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            xBackgroundBorder.Margin = new Thickness(0, 0, xViewbox.ActualWidth - 1, 0);
        }

        private void UIElement_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
        }
    }
}