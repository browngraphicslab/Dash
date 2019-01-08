using System;
using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using DashShared;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

// ReSharper disable once CheckNamespace
namespace Dash
{
    //public class ReplLineViewModel
    //{
    //    public string LineText { get; set; }
    //    public string ResultText { get; set; }
    //    public FieldControllerBase Value { get; set; }
    //    public bool DisplayableOnly { get; set; }
    //    public int Indent { get; set; }
    //}

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
                    bool isError = doc.DocumentType.Equals(DashConstants.TypeStore.ErrorType);
                    bool isFieldNote = doc.DocumentType.Equals(DashConstants.TypeStore.FieldContentNote);
                    var fields = ViewModel.DisplayableOnly ? doc.EnumDisplayableFields() : doc.EnumFields(); 
                    foreach (var field in fields)
                    {
                        if (field.Key.Name.ToLower().Equals("width") || field.Key.Name.ToLower().Equals("height") || (isError || isFieldNote) && field.Key.Name.ToLower().Equals("title")) continue;
                        string indentOffset = field.Value is IListController list && list.Count == 0 ? "   " : "";
                        xChildren.Children.Add(new ReplLineNode { DataContext = new ReplLineViewModel
                        {
                            ResultText = indentOffset + Format(field, isError),
                            Value = field.Value,
                            DisplayableOnly = ViewModel.DisplayableOnly,
                            Indent = ViewModel.Indent
                        } });
                    }
                    break;
                case IListController list:
                    var i = 0;
                    foreach (FieldControllerBase element in list.AsEnumerable())
                    {
                        string index = $"[{i}] : ";
                        xChildren.Children.Add(new ReplLineNode { DataContext = new ReplLineViewModel
                        {
                            ResultText = $"{IndentOffset(element)}{index}" + element,
                            Value = element,
                            DisplayableOnly = ViewModel.DisplayableOnly,
                            Indent = ViewModel.Indent
                        } });
                        i++;
                    }
                    break;
                case ReferenceController r:
                    var rf = r.GetDocumentReference();
                    var deref = r.Dereference(null);
                    xChildren.Children.Add(new ReplLineNode { DataContext = new ReplLineViewModel
                    {
                        ResultText = "Reference : " + rf,
                        Value = rf,
                        DisplayableOnly = ViewModel.DisplayableOnly,
                        Indent = ViewModel.Indent
                    } });
                    xChildren.Children.Add(new ReplLineNode { DataContext = new ReplLineViewModel
                    {
                        ResultText = "   Key : " + r.FieldKey,
                        Value = r.FieldKey,
                        DisplayableOnly = ViewModel.DisplayableOnly,
                        Indent = ViewModel.Indent
                    } });
                    xChildren.Children.Add(new ReplLineNode { DataContext = new ReplLineViewModel
                    {
                        ResultText = $"{IndentOffset(deref)}Value : " + deref,
                        Value = deref,
                        DisplayableOnly = ViewModel.DisplayableOnly,
                        Indent = ViewModel.Indent
                    } });
                    break;
            }
        }

        private static string Format(KeyValuePair<KeyController, FieldControllerBase> kv, bool isError = false)
        {
            string value = kv.Value.ToString();

            if (!isError) value = value.Replace(")", "").Replace("(", "").Replace(",", ", ");
            if (kv.Value is IListController list && isError) value = "[...]";

            return $"{IndentOffset(kv.Value)}{kv.Key} : {value}";
        }

        private static string IndentOffset(FieldControllerBase element) => IsBaseCase(element) ? "   " : "";

        private void XArrowBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            ToggleArrowState();
            e.Handled = true;
        }

        private void ToggleArrowState()
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
        }

        public static bool IsBaseCase(FieldControllerBase value) => !(value is DocumentController) && !(value is IListController list && !list.ToString().Equals("[<empty>]")) && !(value is ReferenceController);

        private void XSnapshotArrowBlock_OnRightTapped(object sender, RightTappedRoutedEventArgs e) => CollapseAllChildren();

        private void CollapseAllChildren()
        {
            _arrowState = ArrowState.Open;
            ToggleArrowState();
            foreach (var uiElement in xChildren.Children)
            {
                ((ReplLineNode) uiElement)?.CollapseAllChildren();
            }
        }

        private ReplLineViewModel _oldViewModel;
        private void ReplLineNode_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (_oldViewModel == args.NewValue) return;
            if (_oldViewModel != null)
            {
                _oldViewModel.Updated -= ViewModelOnUpdated;
            }

            _oldViewModel = ViewModel;

            if (ViewModel == null) return;

            ViewModel.Updated += ViewModelOnUpdated;
            Update();
        }

        private void Update()
        {
            ReplLineViewModel vm = ViewModel;
            bool baseCase = IsBaseCase(vm.Value);
            xArrowBlock.Visibility = baseCase ? Visibility.Collapsed : Visibility.Visible;
            if (vm.Value is IListController list && list.Count == 0) xArrowBlock.Visibility = Visibility.Collapsed;
            xArrowBlock.Text = (string)Application.Current.Resources["ExpandArrowIcon"];

            Thickness m = xChildren.Margin;
            m.Left = vm.Indent * 10;
            xChildren.Margin = m;

            xChildren.Children.Clear();
            ChildrenPopulated = false;
        }

        private void ViewModelOnUpdated(object sender, EventArgs eventArgs) => Update();

        // If we want to add editing capabilities, implement here. Currently, triggered by right clicking on all nodes
        private void MenuFlyoutItem_OnClick(object sender, RoutedEventArgs e) => throw new NotImplementedException();

        private void XNode_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            if (!((sender as FrameworkElement)?.DataContext is ReplLineViewModel output))
            {
                args.Cancel = true;
                return;
            }
            var outputData = output.Value;
            if (outputData.GetType().BaseType.FullName == "Dash.BaseListController")
            {
                //make list output readable
                outputData = new TextController(outputData.ToString());

            }
            var dataBox = new DataBox(outputData).Document;
            dataBox.SetField<TextController>(KeyStore.ScriptSourceKey, output.LineText, true);
            if (outputData is IListController)
            {
                dataBox.SetField<TextController>(KeyStore.CollectionViewTypeKey, CollectionViewType.Schema.ToString(), true);
                dataBox.SetHeight(200.0);
            }
            dataBox.SetWidth(150.0);
            args.Data.SetDragModel(new DragDocumentModel(dataBox));
            args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
            args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;

            //args.handled = true;
        }
    }
}
