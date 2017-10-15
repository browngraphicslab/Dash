using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using Flurl.Util;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class SearchCategoryItem : UserControl
    {
        /// <summary>
        /// All objects under this category
        /// </summary>
        public ObservableCollection<string> ListContent { get; }

        /// <summary>
        /// Returns the list view used to display objects
        /// </summary>
        public ListView List { get { return xList; } }

        /// <summary>
        /// Display path of objects in the listview (replace with ToString overried?) 
        /// </summary>
        public string ListDisplayMemberPath { get; set; }

        /// <summary>
        /// Icon for this category
        /// </summary>
        public string Icon { get; }

        /// <summary>
        /// Title for this category
        /// </summary>
        public string Title { get; }
        public object SelectedItem;
        private Dictionary<string, Func<DocumentController>> _titleToFuncDictionary;

        /// <summary>
        /// ObservableCollection defines what is displayed list view and the action passed in defines what happens when an item is selected in the listview
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="action"></param>
        public SearchCategoryItem(string icon, string title, Dictionary<string, Func<DocumentController>> content)
        {
            this.InitializeComponent();
            _titleToFuncDictionary = new Dictionary<string, Func<DocumentController>>();
            ListContent = new ObservableCollection<string>();
            foreach (KeyValuePair<string, Func<DocumentController>> kvp in content)
            {
                AddToList(kvp.Value, kvp.Key);
            }
            xList.Tapped += XList_Tapped;
        }


        /// <summary>
        /// Adds an item to the list
        /// </summary>
        /// <param name="func"></param>
        /// <param name="name"></param>
        public void AddToList(Func<DocumentController> func, string name = "")
        {
            // if _titleToFuncDictionary already contains the name, it's most likely because the document/collection/operator we're adding has the same DisplayName
            // must differentiate the key before adding to _titleToFuncDictionary or ListContent  
            if (_titleToFuncDictionary.ContainsKey(name))
            {
                string newName = name;
                int i = 1;
                while (_titleToFuncDictionary.ContainsKey(newName))
                    newName = name + i++;
                name = newName;
            }
            _titleToFuncDictionary[name] = func;
            ListContent.Add(name);
        }

        public void RemoveFromList(Func<DocumentController> func, string name = "")
        {
            if (_titleToFuncDictionary[name] != func)
            {
                string newName = name;
                int i = 1;
                while (_titleToFuncDictionary[newName] != func) 
                    newName = name + i++;
                name = newName;
            }
            _titleToFuncDictionary.Remove(name); 
            ListContent.Remove(name); 
        }

        private void XList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ActivateItem();
        }

        public void ActivateItem()
        {
            if (xList.SelectedIndex == -1) return;
            var name = xList.SelectedItem as string;
            var func = _titleToFuncDictionary[name];
            if (func != null)
            {
                var docCont = func.Invoke(); 
                if (docCont != null)
                    Actions.AddDocFromFunction(docCont);
            }

            MainPage.Instance.xCanvas.Children.Remove(TabMenu.Instance);
        }

        public void ActivateItem(object selectedItem)
        {
            if (selectedItem == null)
            {
                ActivateItem();
                return;
            }
            var name = selectedItem as string;
            var func = _titleToFuncDictionary[name];
            if (func != null)
            {
                var docCont = func.Invoke();
                if (docCont != null)
                    Actions.AddDocFromFunction(docCont);
            }

            MainPage.Instance.xCanvas.Children.Remove(TabMenu.Instance);
        }
        private void XList_OnLoaded(object sender, RoutedEventArgs e)
        {
            //Util.FixListViewBaseManipulationDeltaPropagation(xList);
        }
    }
}
