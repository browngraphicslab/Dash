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
using static Dash.Controllers.Operators.DBSearchOperatorFieldModelController;
using System.Collections.Specialized;
using System.Linq;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class OperatorView : UserControl
    {
        private MenuFlyout _flyout;
        private CompoundOperatorEditor _compoundOpEditor;
        private bool _isCompound;
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

        private OperatorFieldModelController _operator;

        private object _lastDataContext = null;
        private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (args.NewValue == _lastDataContext)
            {
                return;
            }
            else
            {
                _lastDataContext = args.NewValue;
            }
            _operator = (DataContext as FieldReference).DereferenceToRoot<OperatorFieldModelController>(null);
            _isCompound = _operator.IsCompound();

            var inputsBinding = new Binding
            {
                Source = _operator.Inputs,
            };
            var outputsBinding = new Binding
            {
                Source = _operator.Outputs,
            };
            InputListView.SetBinding(ListView.ItemsSourceProperty, inputsBinding);
            OutputListView.SetBinding(ListView.ItemsSourceProperty, outputsBinding);

            if (_isCompound)
            {
                MakeCompoundEditor();
                XPresenter.Content = _compoundOpEditor;
                DoubleTapped += OnDoubleTapped;
                _compoundOpEditor.DoubleTapped += (s, e) => e.Handled = true; 

                var compoundFMCont = _operator as CompoundOperatorFieldController;

                InputListView.PointerReleased += (s, e) =>
                {
                    if (XPresenter.Content == null) return;
                    var ioRef = (XPresenter.Content as CompoundOperatorEditor)?.xFreeFormEditor.GetCurrentReference();
                    if (ioRef == null) return;
                    if (ioRef.IsOutput) return;
                    KeyController newInput = new KeyController(Guid.NewGuid().ToString(), "Input " + (compoundFMCont.Inputs.Count + 1));
                    compoundFMCont.Inputs.Add(newInput, ioRef.Type);
                    compoundFMCont.AddInputreference(newInput, ioRef.FieldReference);
                };

                OutputListView.PointerReleased += (s, e) =>
                {
                    if (XPresenter.Content == null) return;
                    var ioRef = (XPresenter.Content as CompoundOperatorEditor)?.xFreeFormEditor.GetCurrentReference();
                    if (ioRef == null) return;
                    if (!ioRef.IsOutput) return;
                    KeyController newOutput = new KeyController(Guid.NewGuid().ToString(), "Output " + (compoundFMCont.Outputs.Count + 1));
                    compoundFMCont.Outputs.Add(newOutput, ioRef.Type);
                    compoundFMCont.AddOutputreference(newOutput, ioRef.FieldReference);
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

            var type = isOutput ? _operator.Outputs[outputKey] : _operator.Inputs[outputKey];
            if (XPresenter.Content is CompoundOperatorEditor)
                if (view == ((CompoundOperatorEditor)XPresenter.Content).xFreeFormEditor) isOutput = !isOutput;
            var ioRef = new IOReference(null, null, new DocumentFieldReference(docId, outputKey), isOutput, type, e, el, el.GetFirstAncestorOfType<DocumentView>());
            view.CanLink = true;
            view.StartDrag(ioRef);
        }

        private void InputEllipseOnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
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
            var type = isOutput ? _operator.Outputs[outputKey] : _operator.Inputs[outputKey];
            bool isCompound = false;
            if (XPresenter.Content is CompoundOperatorEditor)
                if (isCompound = view == ((CompoundOperatorEditor)XPresenter.Content).xFreeFormEditor) isOutput = !isOutput;
            var ioRef = new IOReference(null, null, new DocumentFieldReference(docId, outputKey), isOutput, type, e, el, el.GetFirstAncestorOfType<DocumentView>());
            view.EndDrag(ioRef, isCompound);
        }

        private void InputEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            EndDraggedLink(sender, e, false, this.GetFirstAncestorOfType<CollectionFreeformView>());
        }

        private void OutputEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            EndDraggedLink(sender, e, true, this.GetFirstAncestorOfType<CollectionFreeformView>());
        }

        #region expandoflyout
        private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (XPresenter.Content == null) ExpandView(null, null);
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
            XPresenter.Content = null;
            XPresenter.Background = (SolidColorBrush)Application.Current.Resources["WindowsBlue"];
        }

        private void ExpandView(object sender, RoutedEventArgs e)
        {
            XPresenter.Content = _compoundOpEditor;
        }

        private void MakeCompoundEditor()
        {
            // TODO do we want to resolve this field reference
            var docId = (DataContext as DocumentFieldReference).DocumentId;
            var documentController = ContentController.GetController<DocumentController>(docId);
            var operatorFieldModelController = (DataContext as FieldReference)?.DereferenceToRoot<CompoundOperatorFieldController>(null);
            Debug.Assert(operatorFieldModelController != null);
            _compoundOpEditor = new CompoundOperatorEditor(documentController, operatorFieldModelController);
        }
        #endregion

        private void InputEllipse_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isCompound) return;
            var view = (XPresenter.Content as CompoundOperatorEditor).xFreeFormEditor;
            var ioRef = view.GetCurrentReference();
            if (ioRef == null) return;
            view.CancelDrag(ioRef.PointerArgs.Pointer);
            StartNewLink(sender, ioRef.PointerArgs, false, view);
            view.EndDrag(ioRef, true);
        }

        private void OutputEllipse_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isCompound || _currOutputRef == null) return;
            var view = (XPresenter.Content as CompoundOperatorEditor).xFreeFormEditor;
            EndDraggedLink(sender, null, true, view);
            view.CancelDrag(_currOutputRef.PointerArgs.Pointer);
        }
    }
}