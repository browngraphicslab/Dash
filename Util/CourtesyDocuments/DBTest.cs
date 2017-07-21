using System;
using System.Collections.Generic;
using Windows.Foundation;
using Dash;
using DashShared;

namespace Dash
{
    public class DBTest : CourtesyDocument
    {
        public static DocumentType UmpType  = new DocumentType("3CC1574C-A361-4543-9898-8E6435EF6930", "Umpire");
        public static DocumentType GameType = new DocumentType("6830665B-8300-430D-9854-4DD13488A6CF", "Game");
        public static DocumentType AssignmentType = new DocumentType("FBAD8901-2453-4ADC-9076-D5ED83F46B9B", "Assignment");
        public static DocumentType VolunteerType  = new DocumentType("CC865EF7-A0C3-4740-9E02-3D6E1ACCC7D1", "Volunteer");
        public static Key GameDateKey        = new Key("48A9F432-8757-4B8D-A2F4-51E1BAE44E5B", "GameDate");
        public static Key AssigmentDateKey   = new Key("2787E322-1E7B-4606-B892-CB3F2195E7E3", "AssignedDate");
        public static Key AssigmentPersonKey = new Key("FF312C91-46D9-4DE1-A38D-1FC6323AF9E2", "AssignedPerson");
        public static Key UmpNameKey         = new Key("462664D8-11B9-4561-B65B-AB3A2DAADB3B", "UmpName");
        public static Key VolNameKey         = new Key("FC0FCF99-CB77-4FF6-8AFF-D2E6BA72F8A0", "VolName");

        public static DocumentController PrototypeUmp = CreatePrototypeUmp();
        public static DocumentController PrototypeGame = CreatePrototypeGame();
        public static DocumentController PrototypeVol = CreatePrototypeVol();
        public static DocumentController PrototypeAssign = CreatePrototypeAssignment();

        public static DocumentController PrototypeUmpLayout = CreatePrototypeUmpLayout();
        public static DocumentController PrototypeGameLayout = CreatePrototypeGameLayout();
        public static DocumentController PrototypeVolLayout = CreatePrototypeVolLayout();
        public static DocumentController PrototypeAssignmentLayout = CreatePrototypeAssignmentLayout();

        static DocumentController CreatePrototypeUmp()
        {
           var fields = new Dictionary<Key, FieldModelController>();
            fields.Add(UmpNameKey, new TextFieldModelController("Prototype Umpire"));
          //  fields.Add(UmpAssignmentsKey, new TextFieldModelController("Prototype Dates"));
            return new DocumentController(fields, UmpType);
        }
        static DocumentController CreatePrototypeVol()
        {
            var fields = new Dictionary<Key, FieldModelController>();
            fields.Add(VolNameKey, new TextFieldModelController("Prototype Volunteer"));
            return new DocumentController(fields, VolunteerType);
        }
        static DocumentController CreatePrototypeGame()
        {
            var fields = new Dictionary<Key, FieldModelController>();
            fields.Add(GameDateKey, new TextFieldModelController("Prototype Game"));
            return new DocumentController(fields, GameType);
        }
        static DocumentController CreatePrototypeAssignment()
        {
            var fields = new Dictionary<Key, FieldModelController>();
            fields.Add(AssigmentDateKey,   new TextFieldModelController("Prototype Assigned Date"));
            fields.Add(AssigmentPersonKey, new DocumentFieldModelController(PrototypeUmp));
            return new DocumentController(fields, AssignmentType);
        }

        static DocumentController CreatePrototypeUmpLayout()
        {
            // set the default layout parameters on prototypes of field layout documents
            // these prototypes will be overridden by delegates when an instance is created
            var prototypeUmpNameLayout = new TextingBox(new DocumentReferenceController(PrototypeUmp.GetId(), UmpNameKey), 0, 0, 100, 100);
            var prototypeLayout = new StackingPanel(new[] { prototypeUmpNameLayout.Document }, true);
            prototypeLayout.Document.SetField(DashConstants.KeyStore.HeightFieldKey, new NumberFieldModelController(50), true);
            prototypeLayout.Document.SetField(DashConstants.KeyStore.WidthFieldKey, new NumberFieldModelController(200), true);

            return prototypeLayout.Document;
        }
        static DocumentController CreatePrototypeVolLayout()
        {
            // set the default layout parameters on prototypes of field layout documents
            // these prototypes will be overridden by delegates when an instance is created
            var prototypeVolNameLayout = new TextingBox(new DocumentReferenceController(PrototypeVol.GetId(), VolNameKey), 0, 0, 100, 100);
            var prototypeLayout = new StackingPanel(new[] { prototypeVolNameLayout.Document }, true);
            prototypeLayout.Document.SetField(DashConstants.KeyStore.HeightFieldKey, new NumberFieldModelController(50), true);
            prototypeLayout.Document.SetField(DashConstants.KeyStore.WidthFieldKey, new NumberFieldModelController(100), true);

            return prototypeLayout.Document;
        }
        static DocumentController CreatePrototypeGameLayout()
        {
            // set the default layout parameters on prototypes of field layout documents
            // these prototypes will be overridden by delegates when an instance is created
            var prototypeGameDateLayout = new TextingBox(new DocumentReferenceController(PrototypeGame.GetId(), GameDateKey), 0, 0, 100, 100);
            var prototypeLayout = new StackingPanel(new[] { prototypeGameDateLayout.Document }, true);
            prototypeLayout.Document.SetField(DashConstants.KeyStore.HeightFieldKey, new NumberFieldModelController(50), true);
            prototypeLayout.Document.SetField(DashConstants.KeyStore.WidthFieldKey, new NumberFieldModelController(100), true);

            return prototypeLayout.Document;
        }
        static DocumentController CreatePrototypeAssignmentLayout()
        {
            // set the default layout parameters on prototypes of field layout documents
            // these prototypes will be overridden by delegates when an instance is created
            var prototypeAssignmentDateLayout = new TextingBox(new DocumentReferenceController(PrototypeAssign.GetId(), AssigmentDateKey), 0,  0, 200, 25);
            var prototypeAssignmentPersonLayout = new DocumentBox(new DocumentReferenceController(PrototypeAssign.GetId(), AssigmentPersonKey), 0, 35, 200, 25);
            var prototypeLayout = new StackingPanel(new[] { prototypeAssignmentDateLayout.Document, prototypeAssignmentPersonLayout.Document }, true);
            prototypeLayout.Document.SetField(DashConstants.KeyStore.HeightFieldKey, new NumberFieldModelController(60), true);
            prototypeLayout.Document.SetField(DashConstants.KeyStore.WidthFieldKey, new NumberFieldModelController(200), true);

            return prototypeLayout.Document;
        }

        public List<DocumentController> Documents = new List<DocumentController>();
        public DBTest()
        {
            var Ump1Doc = PrototypeUmp.MakeDelegate();
            var Ump2Doc = PrototypeUmp.MakeDelegate();
            var Vol1Doc = PrototypeVol.MakeDelegate();
            {
                Ump1Doc.SetField(UmpNameKey, new TextFieldModelController("George"), true);
                var ump1Layout = PrototypeUmpLayout.MakeDelegate();
                ump1Layout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Ump1Doc, ump1Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Ump1Doc);
            }

            {
                Ump2Doc.SetField(UmpNameKey, new TextFieldModelController("Matt"), true);
                var ump2Layout = PrototypeUmpLayout.MakeDelegate();
                ump2Layout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Ump2Doc, ump2Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Ump2Doc);
            }

            {
                Vol1Doc.SetField(VolNameKey, new TextFieldModelController("Bob"), true);
                var vol1Layout = PrototypeVolLayout.MakeDelegate();
                vol1Layout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Vol1Doc, vol1Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Vol1Doc);
            }

            {
                var Ass1Doc = PrototypeAssign.MakeDelegate();
                Ass1Doc.SetField(AssigmentDateKey, new TextFieldModelController("July 11"), true);
                Ass1Doc.SetField(AssigmentPersonKey, new DocumentFieldModelController(Ump1Doc), true);
                var ass1Layout = PrototypeAssignmentLayout.MakeDelegate();
                ass1Layout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Ass1Doc, ass1Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Ass1Doc);
            }

            {
                var Ass2Doc = PrototypeAssign.MakeDelegate();
                Ass2Doc.SetField(AssigmentDateKey, new TextFieldModelController("July 14"), true);
                Ass2Doc.SetField(AssigmentPersonKey, new DocumentFieldModelController(Ump1Doc), true);
                var ass2Layout = PrototypeAssignmentLayout.MakeDelegate();
                ass2Layout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Ass2Doc, ass2Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Ass2Doc);
            }

            {
                var Ass3Doc = PrototypeAssign.MakeDelegate();
                Ass3Doc.SetField(AssigmentDateKey, new TextFieldModelController("July 14"), true);
                Ass3Doc.SetField(AssigmentPersonKey, new DocumentFieldModelController(Ump2Doc), true);
                var ass3Layout = PrototypeAssignmentLayout.MakeDelegate();
                ass3Layout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Ass3Doc, ass3Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Ass3Doc);

            }
            {
                var Ass4Doc = PrototypeAssign.MakeDelegate();
                Ass4Doc.SetField(AssigmentDateKey, new TextFieldModelController("July 16"), true);
                Ass4Doc.SetField(AssigmentPersonKey, new DocumentFieldModelController(Vol1Doc), true);
                var ass4Layout = PrototypeAssignmentLayout.MakeDelegate();
                ass4Layout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Ass4Doc, ass4Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Ass4Doc);
            }

           // Documents.Add(PrototypeUmp);
        }
        protected override DocumentController GetLayoutPrototype()
        {
            throw new NotImplementedException();
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            throw new NotImplementedException();
        }
    }
}