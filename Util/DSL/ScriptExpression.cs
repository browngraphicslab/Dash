using System.Threading.Tasks;

namespace Dash
{
    public abstract class ScriptExpression
    {
        public enum ControlFlowFlag
        {
            None,
            Return,
            Break,
            Continue
        } 

        public abstract Task<(FieldControllerBase, ControlFlowFlag)> Execute(Scope scope);

        public abstract FieldControllerBase CreateReference(Scope scope);

        public abstract DashShared.TypeInfo Type { get; }

    }


}
