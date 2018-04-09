using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
                MenuGrid.Margin = new Thickness(0, -20, 0, 0);
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

        /// <summary>
        /// Runs a pop-in animation for each of the buttons in this menu. Called
        /// when the MainPage-level document is set to another menu.
        /// </summary>
        public void CreateAndRunInstantiationAnimation(bool isComposite) {
            if (_documentButtons == null) return;
            foreach (var item in _documentButtons)
                item.AnimateAppearance();
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

        public void AddAndPlayOpenAnimation()
        {
            foreach (var button in _documentButtons)
            {
                button.CreateAndRunInstantiationAnimation(false);
            }
            if (_collectionButtons != null)
            {
                foreach (var button in _collectionButtons)
                {
                    if (button.IsComposite)
                    {
                        button.CreateAndRunInstantiationAnimation(true);
                    }
                    else
                    {
                        button.CreateAndRunInstantiationAnimation(false);
                    }
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
