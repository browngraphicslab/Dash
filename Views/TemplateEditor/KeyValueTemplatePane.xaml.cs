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
        public KeyValuePane KVP { get; set; }

		public KeyValueTemplatePane(TemplateEditorView editor)
		{
			this.InitializeComponent();

			this.FormatPane(editor);
		}

		private void FormatPane(TemplateEditorView editor)
		{
			KVP = new KeyValuePane(true)
			{
				DataContext = editor.LayoutDocument.GetDereferencedField<DocumentController>(KeyStore.DataKey, null),
				Width = 300
			};

			xGrid.Children.Add(KVP);
		}
    }
}
