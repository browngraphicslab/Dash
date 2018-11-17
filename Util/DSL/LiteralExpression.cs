using System.Threading.Tasks;

namespace Dash
{

    public class LiteralExpression : ScriptExpression
    {
        private readonly FieldControllerBase _field;

        public LiteralExpression(FieldControllerBase field)
        {
            _field = field;
        }

        public override Task<(FieldControllerBase, ControlFlowFlag)> Execute(Scope scope)
        {
            return Task.FromResult((_field?.Copy(), ControlFlowFlag.None));
        }

        public FieldControllerBase GetField()
        {
            return _field.Copy();
        }

        public override FieldControllerBase CreateReference(Scope scope)
        {
            return _field;
        }

        public override DashShared.TypeInfo Type => _field.TypeInfo;

    }
}
