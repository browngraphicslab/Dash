using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class SplitManager : UserControl
    {
        public SplitManager()
        {
            this.InitializeComponent();
        }

        public void SetContent(DocumentController doc)
        {
            XSplitPane.Children.Clear();
            var frame = MakeFrame(doc);
            var splitDef = new SplitDefinition();
            SplitPane.SetSplitLocation(frame, splitDef);
            XSplitPane.SplitDefinition = splitDef;
            XSplitPane.Children.Add(frame);
        }

        public IEnumerable<SplitFrame> GetChildFrames()
        {
            return XSplitPane.Children.OfType<SplitFrame>();
        }

        public SplitFrame GetFrameWithDoc(DocumentController doc, bool matchDataDoc)
        {
            if (matchDataDoc)
            {
                var dataDoc = doc.GetDataDocument();
                return GetChildFrames().FirstOrDefault(sf => sf.ViewModel.DataDocument.Equals(dataDoc));
            }
            else
            {
                return GetChildFrames().FirstOrDefault(sf => sf.ViewModel.LayoutDocument.Equals(doc));
            }
        }

        public void Delete(SplitFrame splitFrame, SplitDefinition.JoinOption option = SplitDefinition.JoinOption.JoinMiddle)
        {
            var splitDef = SplitPane.GetSplitLocation(splitFrame);
            if (splitDef != null)
            {
                XSplitPane.RemoveSplit(splitDef, option);
            }

            if (!XSplitPane.Children.Contains(SplitFrame.ActiveFrame))
            {
                SplitFrame.ActiveFrame = (SplitFrame)XSplitPane.Children.First(ele => ele is SplitFrame);
            }
        }

        private SplitFrame MakeFrame(DocumentController doc)
        {
            return new SplitFrame() { DataContext = new DocumentViewModel(doc.GetViewCopy()) };
        }

        public DocumentController Split(SplitFrame frame, SplitDirection dir, DocumentController doc, bool autoSize)
        {
            SplitMode targetMode;
            if (dir == SplitDirection.Left || dir == SplitDirection.Right)
            {
                targetMode = SplitMode.Horizontal;
            }
            else
            {
                targetMode = SplitMode.Vertical;
            }
            var split = SplitPane.GetSplitLocation(frame);
            var par = split.Parent;
            var newFrame = MakeFrame(doc ?? frame.DocumentController);
            if (par != null && par.Mode == targetMode)
            {
                var offset = dir == SplitDirection.Up || dir == SplitDirection.Left ? 0 : 1;
                var index = par.Children.IndexOf(split) + offset;
                var newSplit = new SplitDefinition();
                if (autoSize)
                {
                    newSplit.Size = split.Size / 2;
                    split.Size /= 2;
                }
                else
                {
                    newSplit.Size = 0;
                }
                par.Add(newSplit, index);
                SplitPane.SetSplitLocation(newFrame, newSplit);
                XSplitPane.Children.Add(newFrame);
            }
            else
            {
                split.Mode = targetMode;
                var newSplit = new SplitDefinition();
                var newSplit2 = new SplitDefinition();
                if (!autoSize)
                {
                    newSplit2.Size = 0;
                }
                if (dir == SplitDirection.Up || dir == SplitDirection.Left)
                {
                    split.Add(newSplit2);
                    split.Add(newSplit);
                }
                else
                {
                    split.Add(newSplit);
                    split.Add(newSplit2);
                }

                SplitPane.SetSplitLocation(frame, newSplit);
                SplitPane.SetSplitLocation(newFrame, newSplit2);
                XSplitPane.Children.Add(newFrame);
            }

            return newFrame.ViewModel.DocumentController;
        }
    }
}
