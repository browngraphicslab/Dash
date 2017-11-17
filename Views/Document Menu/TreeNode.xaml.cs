﻿using DashShared;
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
        public TreeNode(FieldControllerBase field) {
            this.InitializeComponent();
            this.fieldController = field;

            // re-render branch of tree only when collections are move around
            // this will change if we introduce field display to visual tree
            if (field.TypeInfo == DashShared.TypeInfo.Collection)
                field.FieldModelUpdated += Field_FieldModelUpdated;

            xDisplayName.Text = "<" + fieldController.TypeInfo.ToString() + "> " + fieldController.Id;
            Render();
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
        /// Renders the tree menu node.
        /// </summary>
        public void Render()
        {
            Debug.WriteLine(xDisplayName.Text);
            xChildrenPanel.Children.Clear(); // reset children for rerendering

            // TODO: pls dont use an if here, just use a generic method you pass in
            // if it's a list, loop through & display all items recursively
            if (fieldController.TypeInfo == DashShared.TypeInfo.List)
            {
                List<FieldControllerBase> items = (fieldController.DereferenceToRoot<BaseListFieldModelController>(null)).Data as List<FieldControllerBase>;
                foreach (FieldControllerBase field in items)
                {
                    xChildrenPanel.Children.Add(new TreeNode(field));
                }
            }

            // TODO: remove this when merged w/ tyler's updates, list case should catch this
            else if (fieldController.TypeInfo == DashShared.TypeInfo.Collection)
            {
                List<DocumentController> items = (fieldController.DereferenceToRoot<DocumentCollectionFieldModelController>(null)).Data as List<DocumentController>;
                foreach (DocumentController field in items)
                {
                    xChildrenPanel.Children.Add(new TreeNode(new DocumentFieldModelController(field)));
                }

            }

            // if it's a document, loop through & display all of its fields recursively
            else if (fieldController.TypeInfo == DashShared.TypeInfo.Document)
            {
                DocumentController docController = (fieldController.DereferenceToRoot<DocumentFieldModelController>(null)).Data as DocumentController;
                
                foreach (var field in docController.EnumDisplayableFields())
                {
                    //xChildrenPanel.Children.Add(new TreeNode(field.Value));
                }

                // update display name to bind to docucment's title

                var titleField = docController.GetField(ContentController<KeyModel>.GetController<KeyController>(DashConstants.KeyStore.TitleKey.Id));
                titleField.FieldModelUpdated += TitleField_FieldModelUpdated;
            }

            // otherwise, just display the leaf
            else
            {
                // TODO: some generic stringify method on fields would be helpful here
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
            DocumentController docController = (fieldController.DereferenceToRoot<DocumentFieldModelController>(null)).Data as DocumentController;
            xDisplayName.Text = "<Doc> " + (sender as TextFieldModelController).Data as string;
        }
    }

}