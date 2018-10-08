using DashShared;

namespace Dash
{
    [FieldModelType(TypeInfo.Html)]
    class HtmlModel : FieldModel
    {

        public HtmlModel(string html, string id = null) : base(id)
        {
            Data = html;
        }

        public string Data;
    }
}
