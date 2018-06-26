using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public interface IReference
    {
        KeyController FieldKey { get; set; }

        FieldControllerBase Dereference(Context context = null);
        T Dereference<T>(Context context = null) where T : FieldControllerBase;

        FieldControllerBase DereferenceToRoot(Context context = null);
        T DereferenceToRoot<T>(Context context = null) where T : FieldControllerBase;

        ReferenceController ToReferenceController();

        IReference Resolve(Context context);

        DocumentController GetDocumentController(Context context);

        void SetField(FieldControllerBase field, Context c);
        void SetField<T>(object value, Context c) where T : FieldControllerBase, new();

    }

    public static class ReferenceHelper
    {
        public static FieldControllerBase Dereference(IReference reference, Context c)
        {
            return reference.GetDocumentController(c).GetField(reference.FieldKey);
        }

        public static FieldControllerBase DereferenceToRoot(IReference reference, Context c)
        {
            c = new Context(c);
            var field = reference.Dereference(c);
            while (field is ReferenceController)
            {
                field = field.Dereference(c);
            }

            return field;
        }

        public static void SetField(IReference reference, FieldControllerBase value, Context c)
        {
            reference.GetDocumentController(c).SetField(reference.FieldKey, value, true);
        }

        public static void SetField<T>(IReference reference, object value, Context c) where T : FieldControllerBase, new()
        {
            reference.GetDocumentController(c).SetField<T>(reference.FieldKey, value, true);
        }

        public static TypeInfo GetRootFieldType(IReference reference)
        {
            return DereferenceToRoot(reference, null).TypeInfo;
        }
    }
}
