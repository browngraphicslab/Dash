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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class ClipSettings : UserControl
    {
        private DocumentController _documentController;
        private Context _context;

        public ClipSettings()
        {
            this.InitializeComponent();
        }

        public ClipSettings(DocumentController editedLayoutDocument, Context context) : this()
        {
            _documentController = editedLayoutDocument;
            _context = context;
            BindClip(context);
        }

        private void BindClip(Context context)
        {
            var clipController =
                _documentController.GetDereferencedField(ImageBox.ClipKey, context) as RectFieldModelController;
            Debug.Assert(clipController != null);
            InitializeClip(clipController);
        }

        private void InitializeClip(RectFieldModelController clipController)
        {
            UpdateTextBoxes(clipController);
            clipController.FieldModelUpdated += (ss, args, cc) =>
            {
                UpdateTextBoxes(clipController);
            };
        }

        private void UpdateTextBoxes(RectFieldModelController clipController)
        {
            xClipXTextBox.Text = "" + clipController.Data.X;
            xClipYTextBox.Text = "" + clipController.Data.Y;
            xClipWidthTextBox.Text = "" + clipController.Data.Width;
            xClipHeightTextBox.Text = "" + clipController.Data.Height;
        }

        private RectFieldModelController ClipController()
        {
            return _documentController.GetDereferencedField(ImageBox.ClipKey, _context) as RectFieldModelController;
        }

        private void XClipXTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {

            var clipController = ClipController();
            Debug.Assert(clipController != null);
            double clipX;
            if (!double.TryParse(xClipXTextBox.Text, out clipX)) return;
            clipController.Data = new Rect(clipX, clipController.Data.Y, clipController.Data.Width,
                clipController.Data.Height);
        }

        private void XClipYTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            double clipY;
            if (!double.TryParse(xClipYTextBox.Text, out clipY)) return;
            var clipController = ClipController();
            Debug.Assert(clipController != null);
            clipController.Data = new Rect(clipController.Data.X, clipY, clipController.Data.Width, clipController.Data.Height);
        }

        private void XClipWidthTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            double clipWidth;
            if (!double.TryParse(xClipWidthTextBox.Text, out clipWidth)) return;
            var clipController = ClipController();
            Debug.Assert(clipController != null);
            clipController.Data = new Rect(clipController.Data.X, clipController.Data.Y, clipWidth, clipController.Data.Height);
        }

        private void XClipHeightTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            double clipHeight;
            if (!double.TryParse(xClipHeightTextBox.Text, out clipHeight)) return;
            var clipController = ClipController();
            Debug.Assert(clipController != null);
            clipController.Data = new Rect(clipController.Data.X, clipController.Data.Y, clipController.Data.Width, clipHeight);
        }
    }
}
