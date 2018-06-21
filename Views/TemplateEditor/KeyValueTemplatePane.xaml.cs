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
using DashShared;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
	public sealed partial class KeyValueTemplatePane : UserControl
	{
		private KeyValuePane _kvPane;

		public KeyValueTemplatePane(TemplateEditorView editor)
		{
			this.InitializeComponent();
			//_kvPane = docController.GetKeyValueAlias();
			//_kvPane = new KeyValueDocumentBox.MakeView(docController, null);

			//_kvPane = new KeyValuePane();
			var box = KeyValueDocumentBox.MakeView(editor.Document.ViewModel.DocumentController, new Context(editor.Document.ViewModel.DocumentController));
			xGrid.Children.Add(box);
		}
	}
}
