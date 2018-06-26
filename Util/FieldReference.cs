using DashShared;

namespace Dash
{
    /// <summary>
    /// Abstract class which contains the utilities for referencing fields on documents
    /// in different ways
    /// </summary>
    public abstract class FieldReference : IReference
    {
        /// <summary>
        /// The key for the field that is being referenced
        /// </summary>
        public KeyController FieldKey { get; set; }

        /// <summary>
        /// create a new field reference to some field associated with the passed in key controller
        /// </summary>
        /// <param name="fieldKey"></param>
        protected FieldReference(KeyController fieldKey)
        {
            FieldKey = fieldKey;
        }

        /// <summary>
        /// Returns the document id for the document which contains the passed in field
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public string GetDocumentId(Context context = null)
        {
            return GetDocumentController(context)?.GetId();
        }

        public abstract FieldReference Copy();

        public abstract DocumentController GetDocumentController(Context context);

        public void SetField(FieldControllerBase field, Context c)
        {
            ReferenceHelper.SetField(this, field, c);
        }

        public void SetField<T>(object value, Context c) where T : FieldControllerBase, new()
        {
            ReferenceHelper.SetField<T>(this, value, c);
        }

        /// <summary>
        /// Resolve this reference field model to the lowest delegate in the given context
        /// </summary>
        /// <param name="context">Context to look for delegates in</param>
        /// <returns>A new FieldModelController that points to the same field in the lowest delegate of the pointed to document</returns>
        public abstract IReference Resolve(Context context);


        public FieldControllerBase Dereference(Context c)
        {
            var doc = GetDocumentController(c);
            if (doc != null)
            {
                var newContext = doc.ShouldExecute(c, FieldKey, null, false);
                if (newContext.TryDereferenceToRoot(this, out var controller))
                {
                    return controller;
                }

                var fmc = GetDocumentController(newContext)?.GetField(FieldKey);

                return fmc;
            }
            return null;
        }

        public T Dereference<T>(Context c) where T : FieldControllerBase
        {
            return Dereference(c) as T;
        }

        public FieldControllerBase DereferenceToRoot(Context context)
        {
            return ReferenceHelper.DereferenceToRoot(this, context);
        }

        public T DereferenceToRoot<T>(Context context) where T : FieldControllerBase
        {
            return DereferenceToRoot(context) as T;
        }

        public TypeInfo GetFieldType(Context context)
        {
            return GetDocumentController(null).GetFieldType(FieldKey);
        }

        public TypeInfo GetRootFieldType()
        {
            return GetDocumentController(null).GetRootFieldType(FieldKey);
        }

        public override bool Equals(object obj)
        {
            FieldReference reference = obj as FieldReference;
            if (reference == null)
            {
                return false;
            }

            return reference.FieldKey.Equals(FieldKey);
        }

        public override int GetHashCode()
        {
            return FieldKey.GetHashCode();
        }

        public abstract ReferenceController ToReferenceController();
    }
}
