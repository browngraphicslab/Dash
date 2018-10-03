using DashShared;

namespace Dash
{
    /// <summary>
    /// Abstract class which contains the utilities for referencing fields on documents
    /// in different ways
    /// </summary>
    public abstract class FieldReference
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
            return GetDocumentController(context)?.Id;
        }

        public abstract FieldReference Copy();

        public abstract DocumentController GetDocumentController(Context context);

        public void SetField(FieldControllerBase field, Context c)
        {
            GetDocumentController(c).SetField(FieldKey, field, true);
        }

        /// <summary>
        /// Resolve this reference field model to the lowest delegate in the given context
        /// </summary>
        /// <param name="context">Context to look for delegates in</param>
        /// <returns>A new FieldModelController that points to the same field in the lowest delegate of the pointed to document</returns>
        public abstract FieldReference Resolve(Context context);


        public FieldControllerBase Dereference(Context context)
        {
            return GetDocumentController(context)?.GetField(FieldKey);
        }

        public FieldControllerBase DereferenceToRoot(Context context)
        {
            context = new Context(context);
            var documentController = GetDocumentController(context);
            if (documentController == null)
            {
                return null;
            }
            context.AddDocumentContext(documentController);
            FieldControllerBase reference = Dereference(context);
            while (reference is ReferenceController referenceController)
            {
                documentController = referenceController.GetDocumentController(context);
                if (documentController == null)
                {
                    return null;
                }
                context.AddDocumentContext(documentController);
                reference = reference.Dereference(context);
            }

            return reference;
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

        public abstract ReferenceController GetReferenceController();
    }
}
