using System;
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
using Dash.Views;
using static Dash.Controllers.Operators.DBSearchOperatorController;
using System.Collections.Specialized;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using DashShared.Models;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class OperatorView : UserControl
    {
        private MenuFlyout _flyout;
        private CompoundOperatorEditor _compoundOpEditor;
        private bool _isCompound;
        private IOReference _currOutputRef;
        private Dictionary<KeyController, FrameworkElement> _keysToFrameworkElements;
        public Dictionary<KeyController, FrameworkElement> KeysToFrameworkElements { get { return _keysToFrameworkElements; } }
        private DocumentView documentView;

        /// <summary>
        /// The operator field model controller backing this operator view
        /// </summary>
        private OperatorController _operator { get; set; }

        /// <summary>
        /// Used to cache the last datacontext so that we don't rebind unecessarily
        /// </summary>
        private object _lastDataContext { get; set; } = null;


        public DocumentView DocumentView { get { return documentView; } }

        /// <summary>
        /// The optional innner content of the operator, it is almost always going to be a <see cref="FrameworkElement"/>
        /// </summary>
        public object OperatorContent
        {
            get => xOpContentPresenter.Content;
            set => xOpContentPresenter.Content = value;
        }



        public OperatorView(Dictionary<KeyController, FrameworkElement> keysToFrameworkElements = null)
        {
            this.InitializeComponent();
            _keysToFrameworkElements = keysToFrameworkElements;
            this.Loaded += OperatorView_Loaded;

        }



        private void OperatorView_Loaded(object sender, RoutedEventArgs e)
        {
            documentView = this.GetFirstAncestorOfType<DocumentView>();
            if (documentView == null)
                return;
            documentView.StyleOperator((Double)Application.Current.Resources["InputHandleWidth"] / 2, _operator.GetOperatorType());
        }



        /// <summary>
        /// Called whenever the datacontext for the entire view changes
        /// The datacontext should be a <see cref="DocumentFieldReference"/> to a <see cref="OperatorController"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnUserControlDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            // cache datacontext updates so we don't update the operator when the data context
            // updates to itself
            if (DataContext == _lastDataContext)
            {
                return;
            }
            _lastDataContext = DataContext;

            // get the operator field model controller form the data context
            _operator = (DataContext as DocumentFieldReference)?.DereferenceToRoot<OperatorController>(null);
            _isCompound = _operator.IsCompound();

            // bind the input and output lists (the things we link to)
            var inputsBinding = new Binding
            {
                Source = _operator.Inputs,
            };
            InputListView.SetBinding(ItemsControl.ItemsSourceProperty, inputsBinding);
            var outputsBinding = new Binding
            {
                Source = _operator.Outputs,
            };
            OutputListView.SetBinding(ItemsControl.ItemsSourceProperty, outputsBinding);

            // if the operator isn't a compound operator then we're done
            if (!_isCompound)
            {
                return;
            }
            // otherwise set up the compound operator
            OnDataContextChangedIfCompound();
        }

        /// <summary>
        /// Helper method to help finish setting up the view when the operator is a compound operator
        /// </summary>
        private void OnDataContextChangedIfCompound()
        {
            MakeCompoundEditor();
            xOpContentPresenter.Content = _compoundOpEditor;
            DoubleTapped -= OnCompoundOperatorDoubleTapped;
            DoubleTapped += OnCompoundOperatorDoubleTapped;
            _compoundOpEditor.DoubleTapped += (s, e) => e.Handled = true;

            var compoundFMCont = _operator as CompoundOperatorController;

            InputListView.PointerReleased += (s, e) =>
            {
                if (xOpContentPresenter.Content == null) return;
                var ioRef =
                    (xOpContentPresenter.Content as CompoundOperatorEditor)?.xFreeFormEditor.GetCurrentReference();
                if (ioRef == null) return;
                if (ioRef.IsOutput) return;
                KeyController newInput = new KeyController(Guid.NewGuid().ToString(),
                    "Input " + (compoundFMCont.Inputs.Count + 1));
                compoundFMCont.AddInputreference(newInput, ioRef);
            };

            OutputListView.PointerReleased += (s, e) =>
            {
                if (xOpContentPresenter.Content == null) return;
                var ioRef =
                    (xOpContentPresenter.Content as CompoundOperatorEditor)?.xFreeFormEditor.GetCurrentReference();
                if (ioRef == null) return;
                if (!ioRef.IsOutput) return;
                KeyController newOutput = new KeyController(Guid.NewGuid().ToString(),
                    "Output " + (compoundFMCont.Outputs.Count + 1));
                compoundFMCont.AddOutputreference(newOutput, ioRef);
                _currOutputRef = ioRef;
            };
        }



        /// <summary>
        /// Envokes handler for starting a link by dragging on a link ellipse handle.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void StartNewLink(object sender, PointerRoutedEventArgs e, bool isOutput, CollectionFreeformView view)
        {
            return;
            var docId = (DataContext as DocumentFieldReference).DocumentId;
            var el = sender as FrameworkElement;
            var outputKey = ((DictionaryEntry)el.DataContext).Key as KeyController;

            var type = isOutput ? _operator.Outputs[outputKey] : _operator.Inputs[outputKey].Type;
            if (xOpContentPresenter.Content is CompoundOperatorEditor)
                if (view == ((CompoundOperatorEditor)xOpContentPresenter.Content).xFreeFormEditor)
                    isOutput = !isOutput;
            var ioRef = new IOReference(new DocumentFieldReference(docId, outputKey), isOutput, type, e, el,
                this.GetFirstAncestorOfType<DocumentView>());
            view.CanLink = true;
            view.StartDrag(ioRef);
        }

        /// <summary>
        /// Fired whenever an input link is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InputEllipseOnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //StartNewLink(sender, e, false, this.GetFirstAncestorOfType<CollectionFreeformView>());
        }

        /// <summary>
        /// Fired whenever an ouput link is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OutputEllipseOnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //StartNewLink(sender, e, true, this.GetFirstAncestorOfType<CollectionFreeformView>());
        }

        /// <summary>
        /// Keep operator view from moving when you drag on an ellipse
        /// </summary>
        private void Ellipse_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            //e.Complete();
        }

        /// <summary>
        /// Invokes code to handle pointer released events on the link handle.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void EndDraggedLink(object sender, PointerRoutedEventArgs e, bool isOutput, CollectionFreeformView view)
        {
            return;
            // get the id of the document this operator is on
            var docRef = DataContext as DocumentFieldReference;
            var docId = docRef.DocumentId;

            // get the keycontroller for where the end of this link was dropped
            var el = sender as FrameworkElement;
            var linkedKey = ((DictionaryEntry)el.DataContext).Key as KeyController;

            // get the type of the key which was linked <see OperatorType>
            var type = isOutput ? _operator.Outputs[linkedKey] : _operator.Inputs[linkedKey].Type;

            var isCompound = false;
            if (xOpContentPresenter.Content is CompoundOperatorEditor)
                if (isCompound = view == ((CompoundOperatorEditor)xOpContentPresenter.Content).xFreeFormEditor)
                    isOutput = !isOutput;

            // create a new ioref representing the entire action
            var ioRef = new IOReference(new DocumentFieldReference(docId, linkedKey), isOutput, type, e, el,
                this.GetFirstAncestorOfType<DocumentView>());

            view.EndDrag(ioRef, isCompound);
        }


        private void InputEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            return;
            var view = (xOpContentPresenter.Content as CompoundOperatorEditor)?.xFreeFormEditor;
            var ioref = view?.GetCurrentReference();
            if (ioref != null)
            {
                view.CancelDrag(ioref.PointerArgs.Pointer);
                StartNewLink(sender, ioref.PointerArgs, false, view);
                view.EndDrag(ioref, true);
                var key = ((DictionaryEntry)(sender as FrameworkElement).DataContext).Key as KeyController;
                (_operator as CompoundOperatorController).AddInputreference(key, ioref);
            }
            else
            {
                EndDraggedLink(sender, e, false, this.GetFirstAncestorOfType<CollectionFreeformView>());
            }
        }

        private void OutputEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            return;
            EndDraggedLink(sender, e, true, this.GetFirstAncestorOfType<CollectionFreeformView>());
        }

        #region expandoflyout

        /// <summary>
        /// Fired if the operator view is for a compound operator and the view is double tapped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCompoundOperatorDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (xOpContentPresenter.Content == null) ExpandView(null, null);
            else ContractView(null, null);
        }

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

            if (_isCompound)
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
            xOpContentPresenter.Content = null;
            xOpContentPresenter.Background = (SolidColorBrush)Resources["WindowsBlue"];
        }

        private void ExpandView(object sender, RoutedEventArgs e)
        {
            xOpContentPresenter.Content = _compoundOpEditor;
        }

        /// <summary>
        /// Create the compound operator editor view in the center of the operator
        /// </summary>
        /// <param name="collectionField"></param>
        private void MakeCompoundEditor(FieldControllerBase collectionField = null)
        {
            // TODO do we want to resolve this field reference
            var docId = (DataContext as DocumentFieldReference).DocumentId;
            var documentController = ContentController<FieldModel>.GetController<DocumentController>(docId);
            var operatorFieldModelController = (DataContext as FieldReference)
                ?.DereferenceToRoot<CompoundOperatorController>(null);
            Debug.Assert(operatorFieldModelController != null);
            _compoundOpEditor = new CompoundOperatorEditor();
        }

        #endregion

        private void InputEllipse_Loaded(object sender, RoutedEventArgs e)
        {
            return;
            var el = sender as FrameworkElement;
            var key = ((DictionaryEntry?)el?.DataContext)?.Key as KeyController;
            if (key != null && _keysToFrameworkElements != null) _keysToFrameworkElements[key] = el;

            if (!_isCompound) return;
            var view = (xOpContentPresenter.Content as CompoundOperatorEditor).xFreeFormEditor;
            var ioRef = view.GetCurrentReference();
            if (ioRef == null) return;
            view.CancelDrag(ioRef.PointerArgs.Pointer);
            StartNewLink(sender, ioRef.PointerArgs, false, view);
            view.EndDrag(ioRef, true);
        }

        private void OutputEllipse_Loaded(object sender, RoutedEventArgs e)
        {
            return;
            var el = sender as FrameworkElement;
            var key = ((DictionaryEntry?)el?.DataContext)?.Key as KeyController;
            if (key != null && _keysToFrameworkElements != null) _keysToFrameworkElements[key] = el;

            if (!_isCompound || _currOutputRef == null) return;
            var view = (xOpContentPresenter.Content as CompoundOperatorEditor).xFreeFormEditor;
            EndDraggedLink(sender, null, true, view);
            view.CancelDrag(_currOutputRef.PointerArgs.Pointer);
        }

        private void FieldPreview_OnLoading(FrameworkElement sender, object args)
        {
            var preview = sender as FieldPreview;
            if (preview == null)
            {
                return;
            }
            preview.DocId = (DataContext as DocumentFieldReference)?.DocumentId;
        }

    }
}