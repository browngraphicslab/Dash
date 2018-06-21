﻿using System;
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
		private DocumentController _kvPane;

		public KeyValueTemplatePane(DocumentController docController, Context context)
		{
			this.InitializeComponent();
			//_kvPane = docController.GetKeyValueAlias();
			//_kvPane = new KeyValueDocumentBox.MakeView(docController, null);

			_kvPane = docController.GetKeyValueAlias();

		}
	}
}
