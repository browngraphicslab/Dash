using System;
using System.Collections.Generic;
using Windows.Foundation;
using Dash;
using Dash.Controllers;
using DashShared;
using Dash.Controllers.Operators;

namespace Dash
{
    public class DBTest
    {


        public static DocumentType WebType = new DocumentType("ED1EDECE-2434-4BDB-A8E8-3DF7A0CE4BB0", "Web Doc");

        public static KeyController NullDocNameKey = new KeyController("3E74836B-CDD2-4F0A-9031-6786B03A40A4");


        public static KeyController WebUrlKey = new KeyController("427B9FB5-C5DB-422E-882D-FFC9A17266C3", "WebUrl");
        
        public static DocumentController DBDoc = CreateDB();




        static DocumentController CreateNull()
        {
            var fields = new Dictionary<KeyController, FieldControllerBase>();
            var dc = new DocumentController(fields, new DocumentType("DBNull", "DBNull"));
            dc.SetField(KeyStore.ThisKey, dc, true);
            dc.SetField(NullDocNameKey, new KeyController(), true);  // bcz: is this correct??
            dc.SetField(KeyStore.PrimaryKeyKey, new ListController<KeyController>(NullDocNameKey), true);
            return dc;
        }

        static DocumentController CreateDB()
        {
            var fields = new Dictionary<KeyController, FieldControllerBase>();
            fields.Add(KeyStore.DataKey, new ListController<DocumentController>());
            var dc = new DocumentController(fields, new DocumentType("DBDoc", "DBDoc"));
            dc.SetField(KeyStore.ThisKey, dc, true);
            return dc;
        }

        protected static void SetLayoutForDocument(DocumentController dataDocument, DocumentController layoutDoc, bool forceMask, bool addToLayoutList)
        {
            dataDocument.SetActiveLayout(layoutDoc, forceMask: forceMask, addToLayoutList: addToLayoutList);
        }
        public List<DocumentController> Documents = new List<DocumentController>();
        


        public DBTest()
        {
            if (instantiated) return;
            instantiated = true;

            DBDoc.FieldModelUpdated -= DBDoc_DocumentFieldUpdated;
            DBDoc.FieldModelUpdated += DBDoc_DocumentFieldUpdated;
        }

        private static bool instantiated;


        private void DBDoc_DocumentFieldUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
        {
            foreach (var d in (DBDoc.GetDereferencedField(KeyStore.DataKey, null) as ListController<DocumentController>).GetElements())
                if (!d.Equals(MainPage.Instance.MainDocument) && !d.Equals(DBDoc))
                {
                    d.FieldModelUpdated -= D_DocumentFieldUpdated;
                    d.FieldModelUpdated += D_DocumentFieldUpdated;
                }
            return;

        }

        public class SeenIt {
            public DocumentController sender;
            public DocumentController.DocumentFieldUpdatedEventArgs args;
            public SeenIt(DocumentController d, DocumentController.DocumentFieldUpdatedEventArgs a)
            {
                sender = d;
                args = a;
            }
            
        };
        static List<SeenIt> BeenThere = new List<SeenIt>();
        public static void ResetCycleDetection()
        {
            //return;
            //if (DBTest.PrototypeUmp != null)
            //    BeenThere.Clear();
            return;
        }
        private void D_DocumentFieldUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
        {
            var dargs = (DocumentController.DocumentFieldUpdatedEventArgs) args;
            foreach (var s in BeenThere)
                if (s.sender.Equals(sender))
                {
                    if (dargs.NewValue.CheckType(s.args.NewValue))
                    {
                        if (dargs.NewValue is ListController<DocumentController>)
                        {
                            var equal = true;
                            var d1 = (dargs.NewValue as ListController<DocumentController>).Data;
                            var d2 = (s.args.NewValue as ListController<DocumentController>).Data;
                            if (d1.Count == d2.Count)
                                foreach (var d in d1)
                                    if (!d2.Contains(d))
                                    {
                                        equal = false;
                                        break;
                                    }
                            if (equal)
                                return;
                        }
                    }
                    if (dargs.NewValue.Equals(s.args.NewValue))
                    {
                        return;
                    }
                }
            BeenThere.Add(new SeenIt((DocumentController)sender, dargs));
            DBDoc.SetField(KeyStore.DataKey, new DocumentReferenceController(MainPage.Instance.MainDocument.GetId(), KeyStore.CollectionKey), true);
            return;
        }
    }
}