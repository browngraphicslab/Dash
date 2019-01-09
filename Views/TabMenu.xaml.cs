using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TabMenu : UserControl
    {
        #region STATIC VARIABLES 
        /// <summary>
        /// private backing variable of the tab menu singleton
        /// </summary>
        private static TabMenu _instance;

        /// <summary>
        /// Tab menu singleton, this is always the single instance of the tab menu that ever exists
        /// </summary>
        public static TabMenu Instance => _instance ?? (_instance = new TabMenu());

        // The CollectionFreeformView to which items from tab menu will be added 
        private static CollectionFreeformView _addsToThisCollection;
        // The position relative to the collectionfreeformview in which items from tab menu will be added 
        private static Point _whereToAdd;
        #endregion
        private TabMenu()
        {
            InitializeComponent();
            GetSearchItems();
            this.HorizontalAlignment = HorizontalAlignment.Left;
            this.VerticalAlignment = VerticalAlignment.Top;

            xSearch.TextChanged += XSearch_TextChanged;
            xSearch.QuerySubmitted += XSearch_QuerySubmitted;
            xSearch.Loaded += (sender, args) => SetTextBoxFocus();

            // hide the tab menu when we lose focus
            LostFocus += (sender, args) => Hide();
        }

        // returns the relative point of WhereToAdd to the collectionfreeformview it is added in  
        public Point GetRelativePoint()
        {
            return Util.GetCollectionFreeFormPoint(_addsToThisCollection, _whereToAdd);
        }

        public void AddToFreeform(DocumentController controller)
        {
            _addsToThisCollection.ViewModel.AddDocument(controller);
        }

        // TODO comment this is the public interface to the tab menu thats it! maybe change the signature and pass in
        // the correct args from coreWindowOnKeyUp
        public static void ConfigureAndShow(CollectionFreeformView col, Point p, Panel canvas, bool isTouch = false)
        {
            _addsToThisCollection = col;
            _whereToAdd = p;
            ShowAt(canvas, isTouch);
        }



        private static void ShowAt(Panel canvas, bool isTouch = false)
        {
            if (Instance != null)
            {
                if (!canvas.Children.Contains(Instance))
                    canvas.Children.Add(Instance);

                if (isTouch) Instance.ConfigureForTouch();
                Canvas.SetLeft(Instance, _whereToAdd.X);
                Canvas.SetTop(Instance, _whereToAdd.Y);
                Instance.ResetList();
                Instance.SetTextBoxFocus();
            }
        }

        // private backing fields
        private List<ITabItemViewModel> _allTabItems;
        private List<ITabItemViewModel> _displayedTabItems;
        private int _selectedIndex = -1;

        /// <summary>
        /// All the tab items in the list they are automatically sorted by Title
        /// </summary>
        public List<ITabItemViewModel> AllTabItems
        {
            get => _allTabItems;
            set
            {
                // whenever we set all the tab items we immediately sort them by their titles
                value.Sort((x, y) => x.Title.ToLowerInvariant().CompareTo(y.Title.ToLowerInvariant()));
                _allTabItems = value;
            }
        }

        /// <summary>
        /// The items displayed in the tab menu, setting this updates the tab menu list automatically
        /// </summary>
        private List<ITabItemViewModel> DisplayedTabItems
        {
            get => _displayedTabItems;
            set
            {
                // whenever we set the displayed tab items we update the item source of the list
                // using an observable collection doesn't work here caues it has to be reassigned as the user
                // types and reassignment or recreation of an observable collection breaks bindings
                _displayedTabItems = value;
                xListView.ItemsSource = _displayedTabItems.Any() // use the passed in value, otherwise use a NoResults placeholder
                    ? _displayedTabItems
                    : new List<ITabItemViewModel>() { new NoResultTabViewModel() };
            }
        }



        /// <summary>
        /// Create the list of items to be displayed in the tab menu
        /// </summary>
        private void GetSearchItems()
        {
            var list = new List<ITabItemViewModel>
            {
                new CreateOpTabItemViewModel("Group", Util.AdornmentDoc),
                new CreateOpTabItemViewModel("Collection", Util.BlankCollection),
                new CreateOpTabItemViewModel("Note", Util.BlankNote)
            };
            // Add all the operators to the tab menu
            list.AddRange(OperationCreationHelper.Operators.Select(op => new CreateOpTabItemViewModel(op.Key, op.Value.OperationDocumentConstructor)));
            AllTabItems = list;
        }

        #region xSEARCH
        private void SetTextBoxFocus()
        {
            xSearch.Focus(FocusState.Programmatic);
        }

        private void XSearch_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            //UpdateList(args.QueryText);   // commented out; handlekeyup should technically be enough 
        }

        /// <summary>
        /// Fired whenever text is changed in the search textbox
        /// </summary>
        private void XSearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // if the user typed the change then prompt action  
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                UpdateList(sender.Text);
        }

        private List<ITabItemViewModel> _allDocItems;
        ///Adds documentViews to tabmenu so we can navigate to them 
        
        public void AddGoToTabItems()
        {/*
            _allDocItems = new List<ITabItemViewModel>();
            foreach (TreeMenuNode treeNode in AddMenu.Instance.ViewToMenuItem.Values)
            {
                foreach (AddMenuItem menuItem in treeNode.ItemsList)
                {
                    var docMenuItem = menuItem as DocumentAddMenuItem;
                    if (docMenuItem != null)
                    {
                        _allDocItems.Add(new GoToTabItemViewModel(docMenuItem.DocType, docMenuItem.Action, docMenuItem.DataDoc));
                    }
                }
            }*/
        }

        public bool DocHasMatchingKey(DocumentController doc, string keyName)
        {
            return string.IsNullOrWhiteSpace(keyName) ? false:
                doc.EnumDisplayableFields().Any((k) => k.Key.Name.ToLowerInvariant().Contains(keyName.ToLowerInvariant()));
        }

        /// <summary>
        /// Updates items source of the current listview to diplay results relevant to the passed in query
        /// </summary>
        private void UpdateList(string query)
        {
            query.Trim();
            // if the input text is null or whitespace display everything
            if (string.IsNullOrWhiteSpace(query))
            {
                ResetList();
            }
            // otherwise display the tab items
            else if (query.StartsWith("@"))
            {
                query = query.Substring(1, query.Length - 1);
                var strings = query.Split('.');
                var docName = strings[0];
                if (strings.Count() == 1)       // "@<DocumentName>" --> tabmenu shows documents with the name 
                {
                    DisplayedTabItems = _allDocItems.Where(t => t.Title.ToLowerInvariant().Contains(docName.ToLowerInvariant())).ToList();
                }
                else if (strings.Count() == 2)  // "@<DocumentName>.<FieldName>" --> tabmenu shows documents containing that field 
                {
                    var fieldName = strings[1];
                    var newTabItems = new List<ITabItemViewModel>();
                    foreach (ITabItemViewModel item in _allDocItems)
                    {
                        var nameMatches = item.Title.ToLowerInvariant().Contains(docName.ToLowerInvariant());
                        if (!nameMatches)
                            continue;

                        var data = (item as GoToTabItemViewModel).Document.GetDataDocument();
                        var containsField = DocHasMatchingKey(data, fieldName);
                        if (containsField)
                            newTabItems.Add(item);
                    }
                    DisplayedTabItems = newTabItems;
                }
            }
            else
            {
                DisplayedTabItems = AllTabItems
                    .Where(t => t.Title.ToLowerInvariant().Contains(query.ToLowerInvariant())).ToList();
            }
            _selectedIndex = -1;
        }


        /// <summary>
        /// Reset the list to an unmodified state, call this on open or if an empty query is submitted
        /// </summary>
        private void ResetList()
        {
            xListView.SelectedIndex = -1;
            DisplayedTabItems = AllTabItems;
            xSearch.Text = string.Empty;
        }

        #endregion


        private void XMainGrid_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Hide();
        }

        /// <summary>
        /// Hides the tab menu
        /// </summary>
        private static void Hide()
        {
            MainPage.Instance.xTabCanvas.Children.Remove(Instance);
        }



        private void ConfigureForTouch()
        {
            xListView.ItemContainerStyle = this.Resources["TouchStyle"] as Style;
        }

        /// <summary>
        /// Move the current selection in the list view down
        /// </summary>
        public void MoveSelectedDown()
        {
            // if the selected index is -1 (nothing selected) then make it 0
            if (xListView.SelectedIndex < 0)
            {
                xListView.SelectedIndex = 0;
            }
            // if the selected index is not the last item in the list
            else if (xListView.SelectedIndex != xListView.Items.Count - 1)
            {
                // increment the selected index
                xListView.SelectedIndex = xListView.SelectedIndex + 1;
            }

            // scroll the newly selected item into view
            xListView.ScrollIntoView(xListView.SelectedItem);
            _selectedIndex = xListView.SelectedIndex;
        }

        /// <summary>
        /// Move the current selection in the list view up
        /// </summary>
        public void MoveSelectedUp()
        {
            // make sure the selected index is greater than or
            // equal to zero
            if (xListView.SelectedIndex <= 0)
            {
                xListView.SelectedIndex = 0;
            }
            else
            {
                // otherwise decrement the selected index
                xListView.SelectedIndex = xListView.SelectedIndex - 1;
            }
            // scroll the newly selected item into view
            xListView.ScrollIntoView(xListView.SelectedItem);
            _selectedIndex = xListView.SelectedIndex;
        }

        private void xListView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ExecuteSelectedElement();
        }

        /// <summary>
        /// Execute the currently selected elemtn in the xListView
        /// </summary>
        private void ExecuteSelectedElement()
        {
            var selectedItem = xListView.SelectedIndex < 0 && _selectedIndex > 0
                ? xListView.Items.ElementAt(_selectedIndex) as ITabItemViewModel
                : xListView.SelectedItem as ITabItemViewModel;
            selectedItem?.ExecuteFunc();
            Hide();
        }

        /// <summary>
        /// When the key up event is fired we handle it, this method is
        /// manually routed from the MainPage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void HandleKeyUp(CoreWindow sender, KeyEventArgs e)
        {
            if (e.VirtualKey == VirtualKey.Escape)
            {
                Hide();
            }
            if (e.VirtualKey == VirtualKey.Enter)
            {
                ExecuteSelectedElement();
            }
        }

        public void HandleKeyDown(CoreWindow sender, KeyEventArgs e)
        {
            if (e.VirtualKey == VirtualKey.Down)
            {
                MoveSelectedDown();
            }

            if (e.VirtualKey == VirtualKey.Up)
            {
                MoveSelectedUp();
            }
        }
    }
}
