using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash.Utill
{
    class CopiedVars
    {

        public class Variables
        {
            public static Variables Instance1;

            public Variables()
            {
                Instance1 = this;
            }

            public DocumentController _copied;
            public DocumentView _copiedview;
            public static string var = "";
        }
    }
}
