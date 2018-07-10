using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

// ReSharper disable once CheckNamespace
namespace Dash
{
    public sealed partial class ReplLineNode
    {
        private FieldControllerBase _outputValue;
        private FieldControllerBase _value;
        private ArrowState _arrowState = ArrowState.Closed;

        public enum ArrowState
        {
            Open,
            Closed
        }

        public ReplLineNode(string lineText, FieldControllerBase value, FieldControllerBase outputValue)
        {
            InitializeComponent();
            xArrowBlock.Text = (string) Application.Current.Resources["ExpandArrowIcon"];
            _outputValue = outputValue;
            LineText = lineText;
            LineValueText = value;
            _value = value;
        }

        private object ComputeTreeFromResult(FieldControllerBase value)
        {
            throw new NotImplementedException();
        }

        public object LineValueText { get; set; }

        public string LineText { get; set; }

        private void MenuFlyoutItem_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void XTextBlock_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void XArrowBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            //Toggle visibility
            _arrowState = _arrowState == ArrowState.Closed ? ArrowState.Open : ArrowState.Closed;
            xArrowBlock.Text = _arrowState == ArrowState.Open
                ? (string) Application.Current.Resources["ExpandArrowIcon"]
                : (string) Application.Current.Resources["ContractArrowIcon"];
        }

        private void XSnapshotArrowBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
