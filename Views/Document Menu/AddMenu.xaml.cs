using System;
using System.Collections.Generic;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views.Document_Menu
{
    public sealed partial class AddMenu : UserControl
    {
        // when you create new compound operators, add them to this tree in the appropriate subheading
        private TreeMenuNode OperatorTree;
        private TreeMenuNode DocumentTree;

        public AddMenu()
        {
            this.InitializeComponent();
            DocumentTree = xDocumentTree;
            OperatorTree = xOperatorTree;
            InitOperatorTree(xOperatorTree);

            // fetch functions
            List<string> categories = new List<string> {
                "Add", "Subtract", "Multiply",
                "Divide", "Union", "Intersection",
                "Zip", "UriToImage", "Map", "Api",
                "Concat", "Append", "Filter", "Compound"
            };

            var all = new Dictionary<string, Func<DocumentController>>();
            all["Document"] = Util.BlankDoc;
            all["Collection"] = Util.BlankCollection;
            all["Note"] = Util.BlankNote;
            foreach (string s in categories)
                all[s] = OperationCreationHelper.Operators[s].OperationDocumentConstructor;


        }

        /// <summary>
        /// Makes the OperatorTree subsection of the menu.
        /// </summary>
        /// <returns></returns>
        private void InitOperatorTree(TreeMenuNode OperatorTree)
        {
            // top-level tree initiation
            string opIcon = App.Current.Resources["OperatorIcon"] as string;
            OperatorTree.HeaderIcon = opIcon;
            OperatorTree.HeaderLabel = "Operators";
            OperatorTree.Add(new AddMenuItem("compound operator", AddMenuTypes.Operator));

            // set ops subheaders
            TreeMenuNode setOpsTree = new TreeMenuNode(true);
            SetOperatorMenuActions(setOpsTree,new List<string> { "Union", "Intersection", "Map", "Filter" });
           
            // set ops subheaders
            TreeMenuNode mathOpsTree = new TreeMenuNode(true);
            SetOperatorMenuActions(mathOpsTree, new List<string> { "Add","Multiply","Divide","Subtract" });

            // return final tree
            OperatorTree.Add(setOpsTree);
            OperatorTree.Add(mathOpsTree);
        }

        /// <summary>
        /// Given a TreeMenuNode and a list of operator string titles, populates the setOpsTree
        /// with the corresponding operator item and action. Generates an operation menu list from
        /// a string of operator names.
        /// </summary>
        /// <param name="setOpsTree">the operator tree to populate</param>
        /// <param name="items">the operations to add, titled by string</param>
        private void SetOperatorMenuActions(TreeMenuNode setOpsTree, List<string> items) {
            var all = new Dictionary<string, Func<DocumentController>>();
            foreach (string s in items)
            {
                all[s] = OperationCreationHelper.Operators[s].OperationDocumentConstructor;
                setOpsTree.Add(new AddMenuItem(s, AddMenuTypes.Operator, all[s]));
            }
        }
    }
}
