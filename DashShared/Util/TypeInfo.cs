using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
        PointerReference = 0x20,
        DocumentReference = 0x1000,
        Operator = 0x40,
        Point = 0x80,
        List = 0x100,
        Ink = 0x200,
        RichTextField = 0x400,
        Rectangle = 0x800,
        Key = 0x2000,
        Reference = PointerReference | DocumentReference,
        Any = Number | Text | Image | Collection | Document | Reference | Operator | Point | List | Ink | RichTextField | Rectangle
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum OperatorType
    {
        Add,
        DBfilter,
        Zip,
        Filter,
        CollectionMap,
        Intersection,
        Union,
        Map,
        ImageToUri,
        DocumentAppend,
        Concat,
        Divide,
        Search,
        Api,
        Compound,
        Subtract,
        Multiply,
        Regex,
        Melt,
        ExtractSentences,
        ExtractKeyWords
    }
}
