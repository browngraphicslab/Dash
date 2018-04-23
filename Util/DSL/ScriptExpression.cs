namespace Dash
{
    public abstract class ScriptExpression
    {
        public abstract FieldControllerBase Execute(ScriptState state);

        public abstract FieldControllerBase CreateReference(ScriptState state);

        public abstract DashShared.TypeInfo Type { get; }

    }


}
