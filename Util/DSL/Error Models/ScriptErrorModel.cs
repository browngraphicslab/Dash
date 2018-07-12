using DashShared;

namespace Dash
{

        public abstract class ScriptErrorModel : EntityBase
        {
            public string ExtraInfo { get; set; }

            public abstract string GetHelpfulString();
        }


}
