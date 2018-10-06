using System.Threading.Tasks;

namespace Dash
{
    public abstract class ScriptExpression
    {
        public abstract Task<FieldControllerBase> Execute(Scope scope);

        public abstract FieldControllerBase CreateReference(Scope scope);

        public abstract DashShared.TypeInfo Type { get; }

    }


}
