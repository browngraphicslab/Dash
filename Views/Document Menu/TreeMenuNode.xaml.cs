using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// NOTE: there's a couple of classes in this fiel, all related to the tree notes.
namespace Dash.Views.Document_Menu
{
    /// <summary>
    /// Represents the type of Add Menu Node: ie. Document, Operator, etc.
    /// </summary>
    public enum AddMenuTypes
    {
        Document,
        Operator,
        Collection,
        Source
    }

    /// <summary>
    /// Represents a draggable item in the item creation menu. Includes type, icon, and other
    /// display options.
    /// </summary>
    public class AddMenuItem : ViewModelBase
    {
        // == MEMBERS ==
        private string _docType;

        public String DocType
        {
            get => _docType;
            set => SetProperty(ref _docType, value);
        }

        public String IconText { get; set; }
        public AddMenuTypes Type { get; set; }
        public TappedEventHandler TapAction { get; set; }

        // == CONSTRUCTORS ==
        public AddMenuItem(String label, AddMenuTypes icon)
        {
            this.DocType = label;
            Type = icon;
        }
        

        public AddMenuItem(String label, AddMenuTypes icon, Func<DocumentController> action)
        {
            this.DocType = label;
            Type = icon;

            // handles clicking of the item in the menu
            void Tapped(object sender, TappedRoutedEventArgs e)
            {
                if (action != null)
                {
                    DocumentController docCont = action.Invoke();
                }
            }

            TapAction = Tapped;
        }

        public AddMenuItem(String label, String icon)
        {
            this.DocType = label;
            this.IconText = icon;
        }
        public AddMenuItem()
        {
        }

    }

    public class DocumentAddMenuItem : AddMenuItem , IDisposable
    {
        private KeyController _key;
        public DocumentController LayoutDoc;
        public DocumentAddMenuItem(string label, AddMenuTypes icon, Func<DocumentController> action, DocumentController layoutDoc, KeyController key) : base(label, icon, action)
        {
            _key = key;
            LayoutDoc = layoutDoc;
            var dataDoc = layoutDoc.GetDataDocument(null);
            // set the default title
            dataDoc.GetTitleFieldOrSetDefault(null);
            dataDoc.AddFieldUpdatedListener(key, TextChangedHandler);
            TextChangedHandler(dataDoc, null, null); 
        }

        public void Dispose()
        {
            LayoutDoc.RemoveFieldUpdatedListener(_key, TextChangedHandler);
        }

        private void TextChangedHandler(FieldControllerBase sender, FieldUpdatedEventArgs fieldUpdatedEventArgs, Context context)
        {
            var doc = (DocumentController) sender; 

            //var textController = documentController.GetField(_key) as TextFieldModelController;
            DocType = doc.GetDereferencedField<TextController>(KeyStore.TitleKey, null).Data ?? "";
        }
        
    }

    // defines the header style and indendation behavior
    public enum MenuDisplayType {
        Header,
        Subheader,
        Hierarchy
    }
    
    /// <summary>
    /// Converts a type enum into the corresponding document icon.
    /// </summary>
    public class MenuTypeToIcon : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, string language)
        {
            // given the enum, returns the corresponding icon as defined in App.xaml
            AddMenuTypes v = (AddMenuTypes) value;
            switch (v) {
                case AddMenuTypes.Collection: return App.Current.Resources["CollectionIcon"] as String;
                case AddMenuTypes.Document: return App.Current.Resources["DocumentPlainIcon"] as String;
                case AddMenuTypes.Operator: return App.Current.Resources["OperatorIcon"] as String;
                case AddMenuTypes.Source: return App.Current.Resources["DragOutIcon"] as String;

            }
            return "";
        }

        // No need to implement converting back on a one-way binding 
        public object ConvertBack(object value, Type targetType,
            object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Implements a hierarchical display of a single tree-style menu node, including a:
    /// - header (or subheader)
    /// - items (stored in a list view)
    /// </summary>
    public sealed partial class TreeMenuNode : UserControl
    {
        // == MEMBERS ==
        // two levels of headers for add menu: either top-level blue or subheader green
        private bool isSubHeader = false;
        //public ListView ItemsList { get { return xItemsList; } set { xItemsList = value; } } uncomment if use case arises

        // text that displays on the header
        public string HeaderLabel
        {
            get { return (string)GetValue(HeaderLabelProperty); }
            set { SetValue(HeaderLabelProperty, value); }
        }

        // optional: the icon on the header, generally want to set this
        public string HeaderIcon
        {
            get { return (string)GetValue(HeaderIconProperty); }
            set { SetValue(HeaderIconProperty, value); }
        }

        public MenuDisplayType DisplayType;

        // containing parent
        public TreeMenuNode TreeParent {get ; set; }
        public double ListWidth { get { return xItemContainer.Width; } set { xItemContainer.Width = value; } }
        
        #region Bindings
        // the text labelling the header
        // Using a DependencyProperty as the backing store for HeaderLabel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderLabelProperty =
            DependencyProperty.Register("HeaderLabel", typeof(string), typeof(TreeMenuNode), new PropertyMetadata(0));

        // Using a DependencyProperty as the backing store for HeaderLabel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderIconProperty =
            DependencyProperty.Register("HeaderIcon", typeof(string), typeof(TreeMenuNode), new PropertyMetadata(0));
        #endregion
        
        // == CONSTRUCTORS ==
        public TreeMenuNode()
        {
            this.InitializeComponent();

            // default values for testing
            HeaderLabel = "Document";
            HeaderIcon = App.Current.Resources["OperatorIcon"] as String;
        }

        public TreeMenuNode(MenuDisplayType DisplayType)
        {
            this.InitializeComponent();

            // default values for testing
            HeaderLabel = "Document";
            HeaderIcon = App.Current.Resources["OperatorIcon"] as String;

            this.DisplayType = DisplayType;

            // stylize depending on type
            if (DisplayType == MenuDisplayType.Subheader) {
                xHeader.Background = App.Current.Resources["AccentGreen"] as SolidColorBrush;
                xLeftIcon.Visibility = Visibility.Collapsed;
                xHeaderLabel.Style = App.Current.Resources["xMenuItem"] as Style;
            } else if (DisplayType == MenuDisplayType.Hierarchy)
            {
                xHeader.Background = new SolidColorBrush(Colors.Transparent);
                xItemContainer.Padding = new Thickness(10,0,0,0);
                xHeader.BorderThickness = new Thickness(0, 0, 0, 1);
                xHeaderLabel.FontWeight = FontWeights.Bold;
                xHeader.BorderBrush = Application.Current.Resources["BorderHighlight"] as SolidColorBrush;
            }
            
        }

        // == METHODS ==

        /// <summary>
        /// Returns the number of items currently in this tree's node list.
        /// </summary>
        public int itemCount()
        {
            return xItemsList.Items.Count;
        }
        


        /// <summary>
        /// Adds a menu item to the bottom of the item list. Adds the tapped event handler to
        /// the corresponding list view item.
        /// </summary>
        /// <param name="item"></param>
        public void Add(AddMenuItem item) {
            xItemsList.Items.Insert(0, item);
        }
        public void Remove(AddMenuItem item)
        {
            xItemsList.Items.Remove(item);
        }

        /// <summary>
        /// Adds a tree menu node in place of menu items.
        /// </summary>
        /// <param name="item"></param>
        public void Add(TreeMenuNode item)
        {
            item.TreeParent = this;
            xChildrenList.Children.Add(item);

        }
        
        // == EVENT HANDLERS ==
        /// <summary>
        /// Tapped event handler. Collapses/uncollapses list items and updates corresponding icons.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xHeader_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // TODO: change these to converters
            // TODO: this would be prettier with an animation
            if (xItemContainer.Visibility == Visibility.Visible)
            {
                xItemContainer.Visibility = Visibility.Collapsed;
                xCollapsedArrow.Text = App.Current.Resources["ContractArrowIcon"] as String;
            }
            else
            {
                xItemContainer.Visibility = Visibility.Visible;
                xCollapsedArrow.Text = App.Current.Resources["ExpandArrowIcon"] as String;
            }

            e.Handled = true;
        }

        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var item = (sender as Grid).DataContext as AddMenuItem;
            if (item.TapAction != null)
            {
                item.TapAction(sender, e);
                e.Handled = true;
            }
            xItemsList.SelectedItem = null;
        }

        private void TreeNodeOnDragStarting(UIElement uiElement, DragStartingEventArgs e)
        {
            var dc = (uiElement as FrameworkElement).DataContext as DocumentAddMenuItem;
            if (dc != null)
            {
                e.Data.RequestedOperation = DataPackageOperation.Copy;
                e.Data.Properties.Add(TreeNodeDragKey, dc.LayoutDoc);

                return;
            }
            e.Data.RequestedOperation = DataPackageOperation.None;;


        }

        public static readonly string TreeNodeDragKey = "5CD5E435-B5BF-4C85-B5D3-401D73CD8223";
    }
}
