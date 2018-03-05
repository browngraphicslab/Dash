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
using System.Collections.Specialized;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using DashShared.Models;
using Dash.Models.DragModels;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class OperatorView : UserControl
    {
        private MenuFlyout _flyout;
        private CompoundOperatorEditor _compoundOpEditor;
        private bool _isCompound;
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



        public OperatorView()
        {
            this.InitializeComponent();
            this.Loaded += OperatorView_Loaded;
        }


        private void OutputEllipse_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            this.GetFirstAncestorOfType<DocumentView>().ManipulationMode = ManipulationModes.None;
            e.Handled = !e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
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

        private void FieldPreview_OnLoading(FrameworkElement sender, object args)
        {
            var preview = sender as FieldPreview;
            if (preview == null)
            {
                return;
            }
            preview.DocId = (DataContext as DocumentFieldReference)?.DocumentId;
        }

        private void OutputEllipse_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            args.AllowedOperations = DataPackageOperation.Copy | DataPackageOperation.Link;
            var el = sender as FrameworkElement;
            var docRef = DataContext as DocumentFieldReference;
            args.Data.Properties.Add(nameof(DragDocumentModel),
                new DragDocumentModel(docRef.GetDocumentController(null),
                ((DictionaryEntry?)el?.DataContext)?.Key as KeyController));
        }
    }
}