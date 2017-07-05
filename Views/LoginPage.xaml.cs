using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
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

        private void SwitchViewOnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_isLoginView)
            {
                xSwitchRegisterLoginText.Text = "Login";
                xLoginButton.Visibility = Visibility.Collapsed;
                xRegisterButton.Visibility = Visibility.Visible;
                xRegisterConfirmPasswordBox.Visibility = Visibility.Visible;
                xErrorText.Text = string.Empty;
            }
            else
            {
                xSwitchRegisterLoginText.Text = "Register";
                xLoginButton.Visibility = Visibility.Visible;
                xRegisterButton.Visibility = Visibility.Collapsed;
                xRegisterConfirmPasswordBox.Visibility = Visibility.Collapsed;
                xErrorText.Text = string.Empty;
            }

            _isLoginView = !_isLoginView;
        }
    }
}
