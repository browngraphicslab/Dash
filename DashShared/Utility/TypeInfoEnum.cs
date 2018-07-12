using System;
using System.Collections.Generic;

namespace DashShared
{
    [Flags]
    public enum TypeInfoEnum
    {
        None = 0x0,
        Number = 0x1,
        Text = 0x2,
        Image = 0x4,
		Video = 0x8,
        Document = 0x10,
        PointerReference = 0x20,
        DocumentReference = 0x40,
        Operator = 0x80,
        Point = 0x100,
        List = 0x200,
        Ink = 0x400,
        RichText = 0x800,
        Rectangle = 0x1000,
        Key = 0x2000,
        DateTime = 0x4000,
        Audio = 0x8000,
        Bool = 0x10000,
        AccessStream = 0x20000,
        Reference = PointerReference | DocumentReference,
        Any = Number | Text | Image | Document | Reference | Operator | Point | List | Ink | RichText | Rectangle | Key | Video | DateTime | Audio | Bool | AccessStream
    }

    public class FieldType
    {
        private FieldType _subType;

        private FieldType()
        {
        }
    }
}
