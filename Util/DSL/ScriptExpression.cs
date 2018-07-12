namespace Dash
{
    public abstract class ScriptExpression
    {
        public abstract FieldControllerBase Execute(Scope scope);

        public abstract FieldControllerBase CreateReference(Scope scope);

        public abstract DashShared.TypeInfo Type { get; }

    }


}
