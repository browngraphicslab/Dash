using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public ObservableCollection<OperatorBuilder> ListContent { get; }

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

        /// <summary>
        /// Background color of the listview
        /// </summary>
        public Color ContentBackGround { get { return xGridBackground.Color; } set { xGridBackground.Color = value; } }
        
        /// <summary>
        /// ObservableCollection defines what is displayed list view and the action passed in defines what happens when an item is selected in the listview
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="action"></param>
        public SearchCategoryItem(string icon, string title, ObservableCollection<OperatorBuilder> content, Action<Func<DocumentController>> action)
        {
            this.InitializeComponent();
            Icon = icon;
            Title = title;
            ListContent = content;
            xList.DisplayMemberPath = nameof(OperatorBuilder.Name);
            ListDisplayMemberPath = xList.DisplayMemberPath;

            xList.Tapped += delegate
            {
                action?.Invoke((xList.SelectedItem as OperatorBuilder)?.OperationConstructor);
                MainPage.Instance.xCanvas.Children.Remove(OperatorSearchView.Instance);
            };
        }
    }
}
