using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public class CollectionDBSchemaColumnViewModel : ViewModelBase
    {
        private double       _width;
        private ScrollViewer _viewModelScrollViewer;
        public KeyController Key { get; } // GET ONLY FOR SAFETY

        private ObservableCollection<DocumentController> CollectionDocs { get; } // GET ONLY FOR SAFETY

        public ObservableCollection<EditableScriptViewModel> EditableViewModels { get; } // GET ONLY FOR SAFETY

        public ScrollViewer ViewModelScrollViewer
        {
            get => _viewModelScrollViewer;
            set => SetProperty(ref _viewModelScrollViewer, value);
        }

        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        public Thickness BorderThickness { get; set; }


        public CollectionDBSchemaColumnViewModel(KeyController key, ObservableCollection<DocumentController> collectionDocs, CollectionDBSchemaHeader.HeaderViewModel headerViewModel = null)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            CollectionDocs = collectionDocs ?? throw new ArgumentNullException(nameof(collectionDocs));

            // create observable collection of editable script view models and add all the current collection docs to it
            EditableViewModels = new ObservableCollection<EditableScriptViewModel>();
            foreach (var documentController in CollectionDocs)
            {
                EditableViewModels.Add(new EditableScriptViewModel(new DocumentFieldReference(documentController.GetDataDocument(), Key)));
            }
            if (headerViewModel != null)
            {
                BorderThickness = headerViewModel.HeaderBorder.BorderThickness; // not expected to change at run-time, so not registering for callbacks
                Width = BorderThickness.Left + BorderThickness.Right + headerViewModel.Width;
                headerViewModel.PropertyChanged += OnHeaderViewModelOnPropertyChanged;
            }
            else
            {
                Width = double.NaN;
            }

            collectionDocs.CollectionChanged += CollectionDocs_CollectionChanged;
        }
        private void OnHeaderViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is CollectionDBSchemaHeader.HeaderViewModel hvm)
            {
                if (e.PropertyName == nameof(hvm.Width))
                {
                    Width = BorderThickness.Left + BorderThickness.Right + hvm.Width;
                }
            }
        }

        private void CollectionDocs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var documentController in e.NewItems.Cast<DocumentController>())
                    {
                        EditableViewModels.Add(new EditableScriptViewModel(new DocumentFieldReference(documentController, Key)));
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (var documentController in e.OldItems.Cast<DocumentController>())
                    {
                        var docToRemove = EditableViewModels.First(evm =>
                            evm.Reference.GetDocumentController(null).Equals(documentController));
                        EditableViewModels.Remove(docToRemove);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    break;
            }
        }
    }

    public sealed partial class CollectionDBSchemaColumn : UserControl
    {

        public CollectionDBSchemaColumnViewModel ViewModel { get; set; }
        
        public CollectionDBSchemaColumn()
        {
            this.InitializeComponent();
            DataContextChanged += CollectionDBSchemaColumn_DataContextChanged;
        }

        private void CollectionDBSchemaColumn_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is CollectionDBSchemaColumnViewModel newViewModel)
            {
                if (newViewModel.Equals(ViewModel))
                {
                    return;
                }

                ViewModel = newViewModel;
            }
        }
        public static ScrollViewer GetScrollViewer(DependencyObject element)
        {
            if (element is ScrollViewer)
            {
                return (ScrollViewer)element;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);

                var result = GetScrollViewer(child);
                if (result != null) return result;
            }

            return null;
        }

        private void XListView_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
                ViewModel.ViewModelScrollViewer = GetScrollViewer(xListView);
        }

        private void XListView_OnUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.ViewModelScrollViewer = null;
        }
    }
}
