using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
using Dash;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class OverlayMenu : UserControl, IDisposable
    {

        private List<MenuButton> _collectionButtons;
        private List<MenuButton> _documentButtons;
        public OverlayMenu(List<MenuButton> collectionButtons, List<MenuButton> documentButtons)
        {
            this.InitializeComponent();
            _collectionButtons = collectionButtons;
            _documentButtons = documentButtons;
            if (collectionButtons == null)
            {
                this.MakeDocumentMenu();
            }
            else
            {
                this.MakeCollectionMenu();
            }
        }

        public void Dispose()
        {
            if(_collectionButtons != null)
            foreach (var item in _collectionButtons)
            {
                item.Dispose();
            }
            if(_documentButtons != null)
            foreach (var item in _documentButtons)
            {
                item.Dispose();
            }
        }

        public async void GoToDocumentMenu()
        {
            this.CollapseMenu(_collectionButtons, xCollectionButtonsStackPanel);
            await Task.Delay(500);
            xCollectionButtonsStackPanel.Visibility = Visibility.Collapsed;
            xDocumentButtonsStackPanel.Visibility = Visibility.Visible;
            this.ExpandMenu(_documentButtons, xDocumentButtonsStackPanel);
        }

        public async void BackToCollectionMenu()
        {
            this.CollapseMenu(_documentButtons, xDocumentButtonsStackPanel);
            await Task.Delay(500);
            xDocumentButtonsStackPanel.Visibility = Visibility.Collapsed;
            xCollectionButtonsStackPanel.Visibility = Visibility.Visible;
            this.ExpandMenu(_collectionButtons, xCollectionButtonsStackPanel);
        }

        private void MakeDocumentMenu()
        {
            foreach (var button in _documentButtons)
            {
                xDocumentButtonsStackPanel.Children.Add(button);
            }
            xCollectionButtonsStackPanel.Visibility = Visibility.Collapsed;
        }

        private void MakeCollectionMenu()
        {
            foreach (var button in _collectionButtons)
            {
                xCollectionButtonsStackPanel.Children.Add(button);
            }
            foreach (var button in _documentButtons)
            {
                xDocumentButtonsStackPanel.Children.Add(button);
            }
            //this.CollapseMenu(_documentButtons,xDocumentButtonsStackPanel);
            xDocumentButtonsStackPanel.Visibility = Visibility.Collapsed;
        }

        private void CollapseMenu(List<MenuButton> buttons, StackPanel parent)
        {
            foreach (var button in buttons)
            {
                if (!button.RotateOnTap)
                {
                    //var transform = button.TransformToVisual(parent);
                    //var yOffset = transform.TransformPoint(new Point(0, 0)).Y;
                    var yOffset = Util.PointTransformFromVisual(new Point(0, 0), button, parent).Y; 
                    button.AddAndRunCollapseAnimation(yOffset);
                }
                else
                {
                    button.AddAndRunRotateOutAnimation();
                }

            }
        }

        private void ExpandMenu(List<MenuButton> buttons, StackPanel parent)
        {
            foreach (var button in buttons)
            {
                if (!button.RotateOnTap)
                {
                    button.AddAndRunExpandAnimation();
                }
                else
                {
                    button.AddAndRunRotateInAnimation();
                }
            }
        }

        public void AddAndPlayCloseMenuAnimation()
        {
            foreach (var button in _documentButtons)
            {
                button.AddAndRunDeleteAnimation();
            }
            if (_collectionButtons != null)
            {
                foreach (var button in _collectionButtons)
                {
                    button.AddAndRunDeleteAnimation();
                }
            }
        }
    }
}
