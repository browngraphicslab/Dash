using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash.Controllers.Functions.Operators
{
    public static class SplitFunctions
    {
        public static void SplitHorizontal()
        {
            SplitFrame.ActiveFrame.Split(SplitDirection.Right, autosize: true);
        }

        public static void SplitVertical()
        {
            SplitFrame.ActiveFrame.Split(SplitDirection.Down, autosize: true);
        }

        public static void CloseSplit()
        {
            SplitFrame.ActiveFrame.Delete();
        }

        public static void FrameHistoryBack()
        {
            SplitFrame.ActiveFrame.GoBack();
        }

        public static void FrameHistoryForward()
        {
            SplitFrame.ActiveFrame.GoForward();
        }
    }
}
