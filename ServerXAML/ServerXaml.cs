using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// == WHAT IS THIS ==
/// This namespace contains several abstractions for XAML we use on the server side.
/// This are all copy/pasted from the Windows UI import. 
/// 
/// == HOW TO ADD NEW ONES ==
/// If you're making a view and
/// need to add a XAML import item, first, type the code in with the XAML imported.
/// Then put the clicker in the name you're using and press 'F12'. This will bring up
/// the windows definition for this item. Copy and paste it along with relevant
/// dependencies. Or, if there's an even simpler way to represent it, you can code your
/// own things. Copy/pasted code is not commented--see the Windows.UI.Xaml equivalent for
/// how to use.
/// </summary>
namespace Dash.ServerXAML {
    // == CUSTOM CLASS WRAPPERS ==

    /// <summary>
    /// Wrapper for the UIElement class. Add functions wrappers as we use them in models.
    /// </summary>
    public class UIElement {
        public double Width { get; set; }
        public double Height { get; set; }
        public Visibility Visibility { get; set; }

        // equivalent of Canvas.Top
        public double CanvasTop { get; set; }
        public double CanvasLeft { get; set; }
    }
    public class TextBlock : UIElement {
        public TextBlock() { }
        public FontWeight FontWeight { get; set; }
        public TextWrapping TextWrapping { get; set; }
        public String Text { get; set; }
    }

    public class Image : UIElement {
        public BitmapImage Source { get; set; }
    }

    public class BitmapImage {
        private Uri pURI;
        public Uri URI { get { return pURI; } set { pURI = value; } }
        public BitmapImage(Uri uri) {
            pURI = uri;
        }
    }

    public static class Canvas {
        public static void SetTop(UIElement e, double top) {
            e.CanvasTop = top; // CanvasTop is the equivalent of the Canvas.Top property
        }
        public static void SetLeft(UIElement e, double left) {
            e.CanvasLeft = left; // CanvasTop is the equivalent of the Canvas.Top property
        }
    }

    // == COPIED FROM UI.XAML ==
    public enum TextWrapping {
        NoWrap = 1,
        Wrap = 2,
        WrapWholeWords = 3
    }
    public enum Visibility {
        Visible = 0,
        Collapsed = 1
    }

    public struct FontWeight {
        public System.UInt16 Weight;
    }

    public sealed class FontWeights {
        public static FontWeight Black { get; }
        public static FontWeight Bold { get; }
        public static FontWeight ExtraBlack { get; }
        public static FontWeight ExtraBold { get; }
        public static FontWeight ExtraLight { get; }
        public static FontWeight Light { get; }
        public static FontWeight Medium { get; }
        public static FontWeight Normal { get; }
        public static FontWeight SemiBold { get; }
        public static FontWeight SemiLight { get; }
        public static FontWeight Thin { get; }
    }

    class ServerXAMLSet {
    }
}
