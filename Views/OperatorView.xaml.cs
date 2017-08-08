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
using static Dash.Controllers.Operators.DBSearchOperatorFieldModelController;
using System.Collections.Specialized;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class OperatorView : UserControl
    {
        private MenuFlyout _flyout;
        private bool _isCompound;
        private IOReference _currInputRef;
        private IOReference _currOutputRef;

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
            _isCompound = opCont.IsCompound();

            var inputsBinding = new Binding
            {
                Source = opCont.Inputs,
            };
            var outputsBinding = new Binding
            {
                Source = opCont.Outputs,
            };
            InputListView.SetBinding(ListView.ItemsSourceProperty, inputsBinding);
            OutputListView.SetBinding(ListView.ItemsSourceProperty, outputsBinding);

            if (_isCompound)
            {
                var compoundFMCont = opCont as CompoundOperatorFieldController;
                InputListView.PointerReleased += (s, e) =>
                {
                    var freeform = (XPresenter.Content as CompoundOperatorEditor).xFreeFormEditor;
                    var ioRef = freeform.GetCurrentReference();
                    if (ioRef == null) return;
                    if (!compoundFMCont.Inputs.ContainsKey(ioRef.FieldReference.FieldKey) && !ioRef.IsOutput)
                    {
                        compoundFMCont.Inputs.Add(ioRef.FieldReference.FieldKey, TypeInfo.Operator);
                    }
                    _currInputRef = ioRef;
                };

                OutputListView.PointerReleased += (s, e) =>
                {
                    var freeform = (XPresenter.Content as CompoundOperatorEditor).xFreeFormEditor;
                    var ioRef = freeform.GetCurrentReference();
                    if (ioRef == null) return;
                    if (!compoundFMCont.Outputs.ContainsKey(ioRef.FieldReference.FieldKey) && ioRef.IsOutput)
                    {
                        compoundFMCont.Outputs.Add(ioRef.FieldReference.FieldKey, TypeInfo.Operator);
                    }
                    _currOutputRef = ioRef;
                };
            }

        }

        /// <summary>
        /// Envokes handler for starting a link by dragging on a link ellipse handle.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void StartNewLink(object sender, PointerRoutedEventArgs e, bool isOutput, CollectionFreeformView view)
        {
            var docId = (DataContext as DocumentFieldReference).DocumentId;
            var el = sender as FrameworkElement;
            var outputKey = ((DictionaryEntry)el.DataContext).Key as KeyController;
            var ioRef = new IOReference(null, null, new DocumentFieldReference(docId, outputKey), isOutput, e, el, el.GetFirstAncestorOfType<DocumentView>());
            view.CanLink = true;
            view.StartDrag(ioRef);
        }

        private void InputEllipseOnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_isCompound)
                StartNewLink(sender, e, true, (XPresenter.Content as CompoundOperatorEditor).xFreeFormEditor);
            else
                StartNewLink(sender, e, false, this.GetFirstAncestorOfType<CollectionFreeformView>());
        }

        private void OutputEllipseOnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            StartNewLink(sender, e, true, this.GetFirstAncestorOfType<CollectionFreeformView>());
        }

        /// <summary>
        /// Keep operator view from moving when you drag on an ellipse
        /// </summary>
        private void Ellipse_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Complete();
        }

        /// <summary>
        /// Envokes code to handle pointer released events on the link handle.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        void EndDraggedLink(object sender, PointerRoutedEventArgs e, bool isOutput, CollectionFreeformView view)
        {
            var docId = (DataContext as DocumentFieldReference).DocumentId;
            var el = sender as FrameworkElement;
            var outputKey = ((DictionaryEntry)el.DataContext).Key as KeyController;
            var ioRef = new IOReference(null, null, new DocumentFieldReference(docId, outputKey), isOutput, e, el, el.GetFirstAncestorOfType<DocumentView>());
            view.EndDrag(ioRef);
        }

        private void InputEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            EndDraggedLink(sender, e, false, this.GetFirstAncestorOfType<CollectionFreeformView>());
        }

        private void OutputEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isCompound)
                EndDraggedLink(sender, e, false, (XPresenter.Content as CompoundOperatorEditor).xFreeFormEditor);
            else
                EndDraggedLink(sender, e, true, this.GetFirstAncestorOfType<CollectionFreeformView>());
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
            XPresenter.Content = null;

        }

        private void ExpandView(object sender, RoutedEventArgs e)
        {
            // TODO do we want to resolve this field reference
            var docId = (DataContext as DocumentFieldReference).DocumentId;
            var documentController = ContentController.GetController<DocumentController>(docId);
            var operatorFieldModelController = (DataContext as FieldReference)?.DereferenceToRoot<CompoundOperatorFieldController>(null);
            Debug.Assert(operatorFieldModelController != null);
            XPresenter.Content = new CompoundOperatorEditor(documentController, operatorFieldModelController);
        }



        #endregion

        private void InputEllipse_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isCompound) return;
            var view = (XPresenter.Content as CompoundOperatorEditor).xFreeFormEditor;
            view.CancelDrag(_currInputRef.PointerArgs.Pointer);
            StartNewLink(sender, _currInputRef.PointerArgs, true, view);
            view.EndDrag(_currInputRef);
        }

        private void OutputEllipse_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isCompound) return;
            var view = (XPresenter.Content as CompoundOperatorEditor).xFreeFormEditor;
            //view.CancelDrag(_currOutputRef.PointerArgs.Pointer);
            //view.StartDrag(_currOutputRef);
            EndDraggedLink(sender, null, false, view);
            view.CancelDrag(_currOutputRef.PointerArgs.Pointer);
            //StartNewLink(sender, _currInputRef.PointerArgs, true, view);
            //view.EndDrag(_currInputRef);
        }
    }
}