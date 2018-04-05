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
        Document = 0x10,
        PointerReference = 0x20,
        DocumentReference = 0x40,
        Operator = 0x80,
        Point = 0x100,
        List = 0x200,
        Ink = 0x300,
        RichText = 0x800,
        Rectangle = 0x1000,
        Key = 0x2000,
        Reference = PointerReference | DocumentReference,
        Any = Number | Text | Image | Document | Reference | Operator | Point | List | Ink | RichText | Rectangle | Key,
		Video = 0x5
	}

    [JsonConverter(typeof(StringEnumConverter))]
    public enum OperatorType
    {
        Add,
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
        ExecuteHtmlJavaScript,
        SentenceAnalyzer,
        ExtractKeywords,
        ImageRecognition,
        RichTextTitle,
        CollectionTitle,
        ImageToColorPalette,
        Quizlet
    }
}
