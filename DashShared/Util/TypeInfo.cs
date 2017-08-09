using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    [Flags]
    public enum TypeInfo
    {
        None = 0x0,
        Number = 0x1,
        Text = 0x2,
        Image = 0x4,
        Collection = 0x8,
        Document = 0x10,
        Reference = 0x20,
        Operator = 0x40,
        Point = 0x80,
        List = 0x100,
        Ink = 0x200,
        RichTextField = 0x400
    }

}
