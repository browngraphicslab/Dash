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

namespace Dash
{
    public sealed partial class CompoundOperatorEditor : UserControl
    {
        private CompoundOperator _operatorFieldModelController;

        public CompoundOperatorEditor()
        {
            this.InitializeComponent();
            Unloaded += CompoundOperatorEditor_Unloaded;
        }

        public CompoundOperatorEditor(CompoundOperator operatorFieldModelController) : this()
        {
            _operatorFieldModelController = operatorFieldModelController;
        }

        private void CompoundOperatorEditor_Unloaded(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }


    }
}
