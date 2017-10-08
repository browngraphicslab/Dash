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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class HierarchicalMenu : UserControl
    {
        private bool _isCloseAnimationPlaying;
        private bool _isOpenAnimationPlaying;
        private static HierarchicalMenu _instance;
        private HierarchicalViewItem _previouslySelected;

        public static HierarchicalMenu Instance => _instance ?? (_instance = new HierarchicalMenu());
        private static Dictionary<DocumentController, HierarchicalViewItem> _itemsDictionary = new Dictionary<DocumentController, HierarchicalViewItem>();
        private ObservableCollection<HierarchicalViewItem> ListItemSource;

        public bool IsPaneVisible
        {
            get
            {
                if (xPaneGrid.Visibility == Visibility.Visible)
                    return true;
                return false;
                ;
            }
            set
            {
                if (value)
                    xPaneGrid.Visibility = Visibility.Visible;
                else
                    xPaneGrid.Visibility = Visibility.Collapsed;
            }
        }
        public HierarchicalMenu()
        {
            this.InitializeComponent();
            ListItemSource = new ObservableCollection<HierarchicalViewItem>();
        }

        public void AddToListItemSource(DocumentController controller)
        {
                var item = new HierarchicalViewItem(controller);
                _itemsDictionary.Add(controller, item);
                ListItemSource.Add(item);
        }

        private List<DocumentController> FindAllPrototypes(DocumentController controller)
        {
            var parents = new List<DocumentController>();
            var currentParent = controller;
            parents.Add(currentParent);
            while (currentParent.GetPrototype() != null)
            {
                currentParent = currentParent.GetPrototype();
                parents.Add(currentParent);
            }
            return parents;
        }

        public void RemoveFromListSource(DocumentController controller)
        {
            var parents = this.FindAllPrototypes(controller);
            var topLevelParent = parents[parents.Count() - 1];
            if (_itemsDictionary.ContainsKey(controller))
            {
                ListItemSource.Remove(_itemsDictionary[controller]);
                _itemsDictionary.Remove(controller);
            }
            else if (!topLevelParent.Equals(controller) && _itemsDictionary.ContainsKey(topLevelParent))
            {
                var count = parents.Count() - 1;
                var list = _itemsDictionary[topLevelParent].List;
                while (count > 1)
                {
                    list = list.Items[parents[count - 1]].List;
                    count -= 1;
                }
                list.RemoveItem(controller);
            }
        }

        public void MakeDelegate(DocumentController parent, DocumentController child)
        {
            var parents = this.FindAllPrototypes(parent);
            var topLevelParent = parents[parents.Count() - 1];
            if (_itemsDictionary.ContainsKey(parent) && _itemsDictionary.ContainsKey(child))
            {
                _itemsDictionary[parent].IsChild = false;
                _itemsDictionary[parent].AddChild(_itemsDictionary[child]);
                ListItemSource.Remove(_itemsDictionary[child]);
                _itemsDictionary.Remove(child);
            } else if (!topLevelParent.Equals(parent) && _itemsDictionary.ContainsKey(topLevelParent) && _itemsDictionary.ContainsKey(child))
            {
                var count = parents.Count() - 1;
                var list = _itemsDictionary[topLevelParent].List.Items;
                // there are issues if a prototype is removed but the delegates are not?
                while (count > 1)
                {
                    list = list[parents[count - 1]].List.Items;
                    count -= 1;
                }
                list[parent].IsChild = false;
                list[parent].AddChild(_itemsDictionary[child]);
                ListItemSource.Remove(_itemsDictionary[child]);
                _itemsDictionary.Remove(child);
            }
        }

        public void SetDocViewIcon(DocumentController controller)
        {
            var parents = this.FindAllPrototypes(controller);
            var topLevelParent = parents[parents.Count() - 1];
            if (_itemsDictionary.ContainsKey(controller))
            {
                _itemsDictionary[controller].SetDocViewIcon();
            } else if (!topLevelParent.Equals(controller) && _itemsDictionary.ContainsKey(topLevelParent))
            {
                var count = parents.Count() - 1;
                var item = _itemsDictionary[topLevelParent];
                while (count > 0)
                {
                    item = item.List.Items[parents[count - 1]];
                    count -= 1;
                }
                item.SetDocViewIcon();
            }
        }

        public void SelectItem(HierarchicalViewItem item)
        {
            if (_previouslySelected != null)
            {
                if (DocumentView.DocumentViews.ContainsKey(_previouslySelected.DocController))
                {
                    DocumentView.DocumentViews[_previouslySelected.DocController].OuterGrid.BorderBrush = new SolidColorBrush(Colors.Transparent);
                    DocumentView.DocumentViews[_previouslySelected.DocController].OuterGrid.BorderThickness = new Thickness(0);
                }
                if (_previouslySelected.IsActive)
                    _previouslySelected.Deselect();
            }

            _previouslySelected = item;
            _previouslySelected.Select();

            if (DocumentView.DocumentViews.ContainsKey(item.DocController))
            {
                DocumentView.DocumentViews[_previouslySelected.DocController].OuterGrid.BorderBrush = new SolidColorBrush(Colors.DarkBlue) {Opacity = 0.5};
                DocumentView.DocumentViews[_previouslySelected.DocController].OuterGrid.BorderThickness = new Thickness(3);

            }
        }

        private void UIElement_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var deltaX = e.Delta.Translation.X;
            var newWidth = xPaneGrid.Width + deltaX;
            if (newWidth > 100)
            {
                xPaneGrid.Width = newWidth;
            }
            e.Handled = true;
        }

        private double ListViewTranslateX = 0;
        private void XPaneGrid_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (ListViewTranslateX < -0.3 * xPaneGrid.Width)
            {
                if (!_isCloseAnimationPlaying)
                {
                    this.CreateAndPlayClosePaneAnimation();
                }
            }
            else
            {
                var deltaX = e.Delta.Translation.X;
                if (e.Position.X < xPaneGrid.Width - 10 && deltaX < 0)
                {
                    xPaneGridTranslateTransform.X += deltaX;
                    ListViewTranslateX += deltaX;
                }
            }
            e.Handled = true;
        }

        private void XPaneGrid_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            xPaneGridTranslateTransform.X = 0;
            ListViewTranslateX = 0;
            MainPage.Instance.xLeftRect.IsHitTestVisible = true;
        }

        private void CreateAndPlayClosePaneAnimation()
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(0.5));

            var storyboard = new Storyboard();
            var doubleAnimation = new DoubleAnimation();
            doubleAnimation.Duration = duration;
            doubleAnimation.SpeedRatio = 2;
            doubleAnimation.EnableDependentAnimation = true;
            doubleAnimation.From = xPaneGridTranslateTransform.X;
            doubleAnimation.To = -xPaneGrid.Width;
            Storyboard.SetTargetProperty(doubleAnimation, "X");
            Storyboard.SetTarget(doubleAnimation, xPaneGridTranslateTransform);
            storyboard.Children.Add(doubleAnimation);
            storyboard.Begin();
            _isCloseAnimationPlaying = true;
            storyboard.Completed += delegate
            {
                IsPaneVisible = false;
                xPaneGridTranslateTransform.X = 0;
                _isCloseAnimationPlaying = false;
            };
        }

        private void CreateAndPlayOpenPaneAnimation()
        {
            xPaneGrid.Visibility = Visibility.Visible;

            Duration duration = new Duration(TimeSpan.FromSeconds(0.5));

            var storyboard = new Storyboard();
            var doubleAnimation = new DoubleAnimation();
            doubleAnimation.Duration = duration;
            doubleAnimation.SpeedRatio = 2;
            doubleAnimation.EnableDependentAnimation = true;
            doubleAnimation.From = -xPaneGrid.Width;
            doubleAnimation.To = 0;
            Storyboard.SetTargetProperty(doubleAnimation, "X");
            Storyboard.SetTarget(doubleAnimation, xPaneGridTranslateTransform);
            storyboard.Children.Add(doubleAnimation);
            storyboard.Begin();
            _isOpenAnimationPlaying = true;
            storyboard.Completed += delegate
            {
                _isOpenAnimationPlaying = false;
            };
        }

        public void OpenPane()
        {
            if (!_isOpenAnimationPlaying && !IsPaneVisible)
                this.CreateAndPlayOpenPaneAnimation();
            MainPage.Instance.xLeftRect.IsHitTestVisible = false;
        }

        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (!_isCloseAnimationPlaying)
                this.CreateAndPlayClosePaneAnimation();
            MainPage.Instance.xLeftRect.IsHitTestVisible = true;
        }
    }
}
