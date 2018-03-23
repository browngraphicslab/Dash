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
        private DocumentView _documentView;

        /// <summary>
        /// The operator field model controller backing this operator view
        /// </summary>
        private OperatorController _operator;

        /// <summary>
        /// Used to cache the last datacontext so that we don't rebind unecessarily
        /// </summary>
        private object _lastDataContext;


        public DocumentView DocumentView => _documentView;

        /// <summary>
        /// The optional innner content of the operator, it is almost always going to be a <see cref="FrameworkElement"/>
        /// </summary>
        public object OperatorContent
        {
            get => xOpContentPresenter.Content;
            set => xOpContentPresenter.Content = value;
        }


        void mymethod()
        {

        }


        public OperatorView()
        {
            this.InitializeComponent();
            this.Loaded += OperatorView_Loaded;
        }


        private void OutputEllipse_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            this.GetFirstAncestorOfType<DocumentView>().ManipulationMode = ManipulationModes.None;
        }


        private void OperatorView_Loaded(object sender, RoutedEventArgs e)
        {
            _documentView = this.GetFirstAncestorOfType<DocumentView>();
            if (_documentView == null)
                return;
            _documentView.StyleOperator((Double)Application.Current.Resources["InputHandleWidth"] / 2, _operator.GetOperatorType());
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
        }

        private void FieldPreview_OnLoading(FrameworkElement sender, object args)
        {
            if (!(sender is FieldPreview preview))
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