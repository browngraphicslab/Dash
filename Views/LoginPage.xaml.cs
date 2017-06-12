using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Dash
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page
    {

        private LoginViewModel _vm;

        private bool _isLoginView = true;

        public LoginPage()
        {
            this.InitializeComponent();

            // we get the datacontext through dependency injection, if it is null then it throws an error
            _vm = App.Instance.Container.GetRequiredService<LoginViewModel>();
            DataContext = _vm;
            xFadeOutRegisterConfirmPasswordBox.Begin();
        }

        private async void LoginButtonTapped(object sender, TappedRoutedEventArgs e)
        {
            // determine whether we want to remember the login
            var rememberLogin = xRememberMeCheckbox.IsChecked ?? false;

            var result = await _vm.TryLogin(xLoginUserBox.Text, XLoginPasswordBox.Password, rememberLogin);

            if (result.IsSuccess)
            {
                this.Frame.Navigate(typeof(HomePage));
            }
            else
            {
                xErrorText.Text = result.ErrorMessage;
            }


        }

        private async void RegisterButtonTapped(object sender, TappedRoutedEventArgs e)
        {
            // determine whether we want to remember the login
            var rememberLogin = xRememberMeCheckbox.IsChecked ?? false;

            var result = await _vm.TryRegister(xLoginUserBox.Text, XLoginPasswordBox.Password, xRegisterConfirmPasswordBox.Password, rememberLogin);

            if (result.IsSuccess)
            {
                this.Frame.Navigate(typeof(HomePage));
            }
            else
            {
                xErrorText.Text = result.ErrorMessage;
            }
        }

        private async void SwitchViewOnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_isLoginView)
            {
                xSwitchRegisterLoginText.Text = "Login";
                xLoginButton.Visibility = Visibility.Collapsed;
                xRegisterButton.Visibility = Visibility.Visible;
                xRegisterConfirmPasswordBox.Visibility = Visibility.Visible;
                xErrorText.Text = string.Empty;
                xRegisterButton.IsEnabled = false;
                xAnimateInRegisterConfirmPasswordBox.Begin();
            }
            else
            {
                xSwitchRegisterLoginText.Text = "Register";
                xFadeOutRegisterConfirmPasswordBox.Begin();
                await Task.Delay(100);
                xAnimateOutRegisterConfirmPasswordBox.Begin();
                xLoginButton.Visibility = Visibility.Visible;
                xRegisterButton.Visibility = Visibility.Collapsed;
                xRegisterConfirmPasswordBox.Visibility = Visibility.Collapsed;
                xErrorText.Text = string.Empty;
            }

            _isLoginView = !_isLoginView;
        }

        /// <summary>
        /// Enable Register button only when all three fields (username, password, confirm password) are filled in
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            xRegisterButton.IsEnabled = XLoginPasswordBox.Password == xRegisterConfirmPasswordBox.Password && XLoginPasswordBox.Password != string.Empty && xLoginUserBox.Text != string.Empty? true: false;
        }

        /// <summary>
        /// Enable Register button only when all three fields (username, password, confirm password) are filled in
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xLoginUserBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            xRegisterButton.IsEnabled = XLoginPasswordBox.Password == xRegisterConfirmPasswordBox.Password && XLoginPasswordBox.Password != string.Empty && xLoginUserBox.Text != string.Empty ? true : false;
        }
    }
}
