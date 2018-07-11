using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class ReplLineViewModel
    {
        public string LineText { get; set; }
        public string ResultText { get; set; }
        public FieldControllerBase Value { get; set; }
        public bool DisplayableOnly { get; set; }
    }

    public sealed partial class ReplLineNode
    {
        private ArrowState _arrowState = ArrowState.Closed;

        public bool ChildrenPopulated { get; set; }

        public ReplLineViewModel ViewModel => DataContext as ReplLineViewModel;

        public enum ArrowState
        {
            Open,
            Closed
        }

        public ReplLineNode()
        {
            InitializeComponent();
        }

        private void BuildTreeFromResult(FieldControllerBase value)
        {
            switch (value)
            {
                //Default, i.e. treated as base case
                default:
                    xArrowBlock.Visibility = Visibility.Collapsed;
                    break;
                //Recursive cases
                case DocumentController doc:
                    var fields = ViewModel.DisplayableOnly ? doc.EnumDisplayableFields() : doc.EnumFields(); 
                    foreach (var field in fields)
                    {
                        if (field.Key.Name.ToLower().Equals("width") || field.Key.Name.ToLower().Equals("height")) continue;
                        string indentOffset = field.Value is BaseListController list && list.Count == 0 ? "   " : "";
                        xChildren.Children.Add(new ReplLineNode { DataContext = new ReplLineViewModel { ResultText = indentOffset + Format(field), Value = field.Value, DisplayableOnly = ViewModel.DisplayableOnly} });
                    }
                    break;
                case BaseListController list:
                    var i = 0;
                    foreach (var element in list.Data)
                    {
                        xChildren.Children.Add(new ReplLineNode { DataContext = new ReplLineViewModel { ResultText = $"{IndentOffset(element)}[{i}] : " + element, Value = element, DisplayableOnly = ViewModel.DisplayableOnly } });
                        i++;
                    }
                    break;
                case ReferenceController r:
                    var rf = r.GetReference();
                    var deref = r.Dereference(null);
                    xChildren.Children.Add(new ReplLineNode { DataContext = new ReplLineViewModel { ResultText = "Reference : " + rf, Value = rf, DisplayableOnly = ViewModel.DisplayableOnly } });
                    xChildren.Children.Add(new ReplLineNode { DataContext = new ReplLineViewModel { ResultText = "   Key : " + r.FieldKey, Value = r.FieldKey, DisplayableOnly = ViewModel.DisplayableOnly } });
                    xChildren.Children.Add(new ReplLineNode { DataContext = new ReplLineViewModel { ResultText = $"{IndentOffset(deref)}Value : " + deref, Value = deref, DisplayableOnly = ViewModel.DisplayableOnly } });
                    break;
            }
        }

        private static string Format(KeyValuePair<KeyController, FieldControllerBase> kv)
        {
            string value = kv.Value.ToString().Replace(")", "").Replace("(", "").Replace(",", ", ");
            return $"{IndentOffset(kv.Value)}{kv.Key} : {value}";
        }

        private static string IndentOffset(FieldControllerBase element) => IsBaseCase(element) ? "   " : "";

        private void XArrowBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            _arrowState = _arrowState == ArrowState.Open ? ArrowState.Closed : ArrowState.Open;
            if (_arrowState == ArrowState.Open && !ChildrenPopulated)
            {
                BuildTreeFromResult(ViewModel.Value);
                ChildrenPopulated = true;
            }
            if (xArrowBlock.Visibility == Visibility.Visible) xArrowBlock.Text = _arrowState == ArrowState.Open
                ? (string)Application.Current.Resources["ContractArrowIcon"]
                : (string)Application.Current.Resources["ExpandArrowIcon"];
            xChildren.Visibility = _arrowState == ArrowState.Open ? Visibility.Visible : Visibility.Collapsed;
            e.Handled = true;
        }

        private static bool IsBaseCase(FieldControllerBase value) => !(value is DocumentController) && !(value is BaseListController) && !(value is ReferenceController);

        private void XSnapshotArrowBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MenuFlyoutItem_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void XTextBlock_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private ReplLineViewModel _oldViewModel;
        private void ReplLineNode_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (_oldViewModel == args.NewValue) return;
            _oldViewModel = ViewModel;

            var vm = ViewModel;
            xArrowBlock.Visibility = IsBaseCase(vm.Value) ? Visibility.Collapsed : Visibility.Visible;
            if (vm.Value is BaseListController list && list.Count == 0) xArrowBlock.Visibility = Visibility.Collapsed; 
            xArrowBlock.Text = (string) Application.Current.Resources["ExpandArrowIcon"];

            ChildrenPopulated = false;
        }
    }
}
