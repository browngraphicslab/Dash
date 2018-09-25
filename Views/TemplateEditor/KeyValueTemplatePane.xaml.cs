using Windows.UI.Xaml.Controls;

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
