using DashShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

/// <summary>
/// Implements a node in the Tree Menu using a FieldModelController as
/// data reference.
/// </summary>
namespace Dash.Views.Document_Menu
{
    public sealed partial class TreeNode : UserControl
    {
        // == MEMBERS ==
        private FieldControllerBase fieldController;
        private string DisplayName = "Document";
        private KeyController key = null;
        private TreeNode parent;
        public int LevelsToRoot; // how many branches up you need to go to hit the root
        public TreeNode Parent { get; }

        // == CONSTRUCTORS ==
        public TreeNode()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Constructs a new TreeNode with data bound to the given FieldControllerBase.
        /// Renders the TreeNodeElement and recursively renders all child elements.
        /// </summary>
        /// <param name="field"></param>
        public TreeNode(FieldControllerBase field, TreeNode parent, KeyController key = null) {
            this.InitializeComponent();
            this.fieldController = field;
            this.parent = parent;

            // re-render branch of tree only when collections are move around
            // this will change if we introduce field display to visual tree
            if (field.TypeInfo == DashShared.TypeInfo.List)
                field.FieldModelUpdated += Field_FieldModelUpdated;

            // this is the root case
            if (parent == null) LevelsToRoot = 0;
            else LevelsToRoot = parent.LevelsToRoot + 1;

            xDisplayName.Text = GetDislayNameFromField(field, key);
            Render();
        }
        
        /// <summary>
        /// Generates the textual TreeNode display of a FieldModelController's data
        /// </summary>
        /// <param name="field"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private string GetDislayNameFromField(FieldControllerBase field, KeyController key = null)
        {
            string keyString = "";

            if (key != null)
            {
                if (key.Name == DashShared.DashConstants.KeyStore.TitleKey.Id)
                    keyString = "Title";
                else
                    keyString = key.Name;
            }


            return "<" + fieldController.GetTypeAsString() + "> " + keyString + ": " + fieldController.ToString();
        }

        /// <summary>
        /// Rerenders the nodes when its children/data changes in the tree.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <param name="context"></param>
        private void Field_FieldModelUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            switch (args.Action)
            {
                case DocumentController.FieldUpdatedAction.Add:
                    Debug.WriteLine("add to tree view");
                    break;
                    
                case DocumentController.FieldUpdatedAction.Remove:
                    Debug.WriteLine("remove from tree view");
                    break;
                    
                case DocumentController.FieldUpdatedAction.Replace:
                    Debug.WriteLine("replace from tree view");
                    break;

                case DocumentController.FieldUpdatedAction.Update:
                    Render();
                    break;
            }
        }
        
        // == METHODS ==

        /// <summary>
        /// Sets the icon that displays on the tree node item.
        /// </summary>
        public void SetDisplayIcon(string text)
        {
            xLeftIcon.Text = text;
        }

        /// <summary>
        /// Adds a child item to the given TreeNode.
        /// </summary>
        /// <param name="item"></param>
        private void AddChild(TreeNode item)
        {
            xChildrenPanel.Children.Add(item);
        }

        /// <summary>
        /// Renders the tree menu node.
        /// </summary>
        public void Render()
        {
            xChildrenPanel.Children.Clear(); // reset children for rerendering

            if (this.parent == null)
                xChildrenPanel.Padding = new Thickness(0);

            // TODO: pls dont use an if here, just use a generic method you pass in
            // if it's a list, loop through & display all items recursively
            if (fieldController is BaseListController)
            {
                var listController = (fieldController.DereferenceToRoot<BaseListController>(null));
                List<FieldControllerBase> items = listController.Data;

                // special case: if it's a List:Docs, update parent's icon 
                var isCollection = listController.ListSubTypeInfo == TypeInfo.Document;
                if (isCollection && this.parent != null)
                {
                    parent.SetDisplayIcon(App.Current.Resources["CollectionIcon"] as string);
                }
                foreach (FieldControllerBase field in items)
                {
                    // this code special cases collectionViews s.t. items in the main collection are on equal
                    // level with the document containing the collection's fields
                    if (!isCollection || parent == null)
                        xChildrenPanel.Children.Add(new TreeNode(field, this));
                    else
                    {
                        parent.AddChild(new TreeNode(field, parent));
                    }
                }
                if (parent == null || isCollection)
                    xHeader.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }

            // if it's a document, loop through & display all of its fields recursively
            else if (fieldController is DocumentController)
            {
                // apply unique doc styles => todo, for frick's sake do this not here
                xHeaderBackground.Background = App.Current.Resources["WindowsBlue"] as SolidColorBrush;

                // makes nested header bg colors more faded
                double maxLevels = 5.0;
                SolidColorBrush s = xHeader.Background as SolidColorBrush;
                s.Opacity = (maxLevels - LevelsToRoot) / maxLevels;
                xHeaderBackground.Opacity = s.Opacity;

                xLeftIcon.Visibility = Windows.UI.Xaml.Visibility.Visible;
                
                DocumentController docController = (fieldController.DereferenceToRoot<DocumentController>(null));
                foreach (var field in docController.EnumDisplayableFields())
                {
                    xChildrenPanel.Children.Add(new TreeNode(field.Value,this,field.Key));
                }

                // update display name to correspond to docucment's title
                xDisplayName.Text = "" + (docController?.GetTitleFieldOrSetDefault()?.Data ?? "");
                var titleField = docController.GetTitleFieldOrSetDefault();
                titleField.FieldModelUpdated += TitleField_FieldModelUpdated;
            }

            // otherwise, just display the leaf
            else if (fieldController is OperatorController) // TODO: these if cases should also be dereferenced to root
            {
                OperatorController opController = (fieldController.DereferenceToRoot<OperatorController>(null));

                // updates parent to have operator icon
                if (this.parent != null)
                {
                    parent.SetDisplayIcon(App.Current.Resources["OperatorIcon"] as string);
                }

                foreach (var ioref in opController.Inputs)
                {
                }

            }
            else {
                // for non-expandable types, hide the expand button & have no click action
                xCollapsedArrow.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                xHeader.Tapped -= xHeader_Tapped;
            }

        }
        
        /// <summary>
        /// On modification of title field, updates display of field in TreeNode item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <param name="context"></param>
        private void TitleField_FieldModelUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            DocumentController docController = (fieldController.DereferenceToRoot<DocumentController>(null));
            xDisplayName.Text =  (sender as TextController).Data as string;
        }


        /// <summary>
        /// TEMPORARY show/hide children method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Border_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (xChildrenPanel.Visibility == Windows.UI.Xaml.Visibility.Visible)
                xChildrenPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            else
                xChildrenPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }


        /// <summary>
        /// Tapped event handler. Collapses/uncollapses list items and updates corresponding icons.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xHeader_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // TODO: change these to converters
            // TODO: this would be prettier with an animation
            if (xChildrenPanel.Visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                xChildrenPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                xCollapsedArrow.Text = App.Current.Resources["ContractArrowIcon"] as String;
            }
            else
            {
                xChildrenPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
                xCollapsedArrow.Text = App.Current.Resources["ExpandArrowIcon"] as String;
            }

            e.Handled = true;
        }
    }

}