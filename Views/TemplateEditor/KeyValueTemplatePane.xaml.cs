using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using DashShared;
using Microsoft.Toolkit.Uwp.UI.Extensions;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
	public sealed partial class KeyValueTemplatePane : UserControl
	{
		private KeyValuePane _kvPane;

		public KeyValueTemplatePane(TemplateEditorView editor)
		{
			this.InitializeComponent();

			this.FormatPane(editor);
			
		}

		private void FormatPane(TemplateEditorView editor)
		{
			var kvPane = new KeyValuePane()
			{
				DataContext = editor.Document.ViewModel.DocumentController,
				Width = 400
			};

			xGrid.Children.Add(kvPane);
		}
	}
}
