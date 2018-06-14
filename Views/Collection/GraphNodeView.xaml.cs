using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Dash.Annotations;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class GraphNodeView : UserControl, INotifyPropertyChanged
    {
        public GraphNodeViewModel ViewModel { get; private set; }
        public CollectionGraphView ParentGraph { get; private set; }
        public double ConstantRadiusWidth { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public GraphNodeView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += GraphNodeView_Loaded;
            Unloaded += GraphNodeView_Unloaded;
            ConstantRadiusWidth = 50;
        }

        private void GraphNodeView_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "XPosition":
                    (xGrid.RenderTransform as TranslateTransform).X = ViewModel.XPosition;
                    break;
                case "YPosition":
                    (xGrid.RenderTransform as TranslateTransform).Y = ViewModel.YPosition;
                    break;
            }
        }


        #region loading

        private void GraphNodeView_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ParentGraph = this.GetFirstAncestorOfType<CollectionGraphView>();

            var toConnections = ViewModel.DocumentViewModel.DataDocument.GetField<ListController<DocumentController>>(KeyStore.LinkToKey)?.Count + 1 ?? 1;
            var fromConnections = ViewModel.DocumentViewModel.DataDocument.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey)?.Count + 1 ?? 1;

            xEllipse.Width = toConnections + fromConnections * ConstantRadiusWidth;
            xEllipse.Height = xEllipse.Width;

            var title = ViewModel.DocumentController
                            .GetDereferencedField<TextController>(KeyStore.TitleKey, new Context())?.Data ??
                        "Untitled " + ViewModel.DocumentController.DocumentType.Type;

            xTitleBlock.Text = title;

            ViewModel.DocumentController.AddFieldUpdatedListener(KeyStore.TitleKey, DocumentController_TitleUpdated);

            TranslateTransform transformation = new TranslateTransform
            {
                X = ViewModel.XPosition,
                Y = ViewModel.YPosition
            };
            xGrid.RenderTransform = transformation;
        }

        private void DocumentController_TitleUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            xTitleBlock.Text =
                ViewModel.DocumentController.GetDereferencedField<TextController>(KeyStore.TitleKey, context)
                    .Data ?? "Untitled " + ViewModel.DocumentController.DocumentType.Type;
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var vm = DataContext as GraphNodeViewModel;
            Debug.Assert(vm != null);
            ViewModel = vm;
        }

        #endregion


        #region property changed

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private void Node_OnTapped(object sender, TappedRoutedEventArgs e)
        {

        }
    }
}
