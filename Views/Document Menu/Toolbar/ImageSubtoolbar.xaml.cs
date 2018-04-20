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
	/**
	 * The subtoolbar that appears when an ImageBox is selected.
	 */
	public sealed partial class ImageSubtoolbar : UserControl
	{
		public ImageSubtoolbar()
		{
			this.InitializeComponent();
		}

		internal void SetMenuToolBarBinding(ImageBox image)
		{
			throw new NotImplementedException();
		}

		/**
		 * Prevents command bar from hiding labels on click by setting isOpen to true every time it begins to close.
		*/
		private void CommandBar_Closing(object sender, object e)
		{
			xImageCommandbar.IsOpen = true;
		}

		private void Crop_Click(object sender, RoutedEventArgs e)
		{
			//TODO: Implement cropping on the selected image
		}

		private void Replace_Click(object sender, RoutedEventArgs e)
		{
			//TODO: Implement replacing selected image
		}
	}
}
