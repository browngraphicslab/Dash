using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Dash
{
    public class CustomRichEdit: RichEditBox
    {
        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            var ctrlState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Control)
                .HasFlag(CoreVirtualKeyStates.Down);
            if (ctrlState)
            {
                if (e.Key.Equals(VirtualKey.I))
                {
                    // overrides default behavior for ctrl+I (which is to indent) to Italicize selection instead
                    if (this.Document.Selection.CharacterFormat.Italic == FormatEffect.On)
                    {
                        this.Document.Selection.CharacterFormat.Italic = FormatEffect.Off;
                    }
                    else
                    {
                        this.Document.Selection.CharacterFormat.Italic = FormatEffect.On;
                    }
                    e.Handled = true;
                }
                else
                {
                    base.OnKeyDown(e);
                }
            }
            else
            {
                base.OnKeyDown(e);
            }
        }
    }
}
