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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Popups
{
	public sealed partial class YouTubeLinkPopup : UserControl
	{
		public double VerticalOffset { get => xPopup.VerticalOffset; set => xPopup.VerticalOffset = value; }
		public double HorizontalOffset { get => xPopup.HorizontalOffset; set => xPopup.HorizontalOffset = value; }

		public YouTubeLinkPopup()
		{
			this.InitializeComponent();
		}

		public Task<Uri> GetUri()
		{
			var tcs = new TaskCompletionSource<Uri>();
			xPopup.IsOpen = true;
			xConfirmButton.Tapped += xConfirmButton_OnTapped;
			xCancelButton.Tapped += xCancelButton_OnTapped;

			void xConfirmButton_OnTapped(object sender, TappedRoutedEventArgs e)
			{
				Uri uri;
				if (!Uri.TryCreate(xURLBox.Text, UriKind.Absolute, out uri)) return;
				if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return;
				if (new Uri(xURLBox.Text).Host != "www.youtube.com") return;

				tcs.SetResult(uri);
				xConfirmButton.Tapped -= xConfirmButton_OnTapped;
				xPopup.IsOpen = false;
			}

			void xCancelButton_OnTapped(object sender, TappedRoutedEventArgs e)
			{
				xPopup.IsOpen = false;
				tcs.SetResult(null);
				xCancelButton.Tapped -= xCancelButton_OnTapped;
			}

			return tcs.Task;
		}
	}
}
