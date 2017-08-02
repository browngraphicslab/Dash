using System.Collections;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;
using DashShared;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.Foundation;
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
        private MenuFlyout _flyout;

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
            ioreference.FireDragStarted();
        }

        private void OnIoDragEnded(IOReference ioreference)
        {
            ioreference.FireDragEnded();
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

        #region expandoflyout
        private void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            e.Handled = true;
            var thisUi = this as UIElement;
            var position = e.GetPosition(thisUi);
            var menuFlyout = _flyout ?? (_flyout = InitializeFlyout());

            if (menuFlyout.Items.Count != 0)
            {
                menuFlyout.ShowAt(thisUi, position);
            }
        }

        private MenuFlyout InitializeFlyout()
        {
            _flyout = new MenuFlyout();
            var controller = (DataContext as DocumentFieldReference)?.DereferenceToRoot<OperatorFieldModelController>(null);
            Debug.Assert(controller != null);

            if (controller.IsCompound())
            {
                var expandItem = new MenuFlyoutItem { Text = "Expando" };
                var contractItem = new MenuFlyoutItem { Text = "Contracto" };
                expandItem.Click += ExpandView;
                contractItem.Click += ContractView;
                _flyout.Items?.Add(expandItem);
                _flyout.Items?.Add(contractItem);
            }



            return _flyout;

        }

        private void ContractView(object sender, RoutedEventArgs e)
        {
            

        }

        private void ExpandView(object sender, RoutedEventArgs e)
        {
            var documentCanvasViewModel = new FreeFormCollectionViewModel(false);
            //documentCanvasViewModel.AddDocument(OperatorDocumentModel.CreateOperatorDocumentModel(new DivideOperatorFieldModelController()), false);
            //documentCanvasViewModel.AddDocument(OperatorDocumentModel.CreateOperatorDocumentModel(new AddOperatorModelController()), false);
            var documentCanvasView = new CollectionFreeformView();
            XPresenter.Content = documentCanvasView;
            documentCanvasView.DataContext = documentCanvasViewModel;
        }

        #endregion


    }
}