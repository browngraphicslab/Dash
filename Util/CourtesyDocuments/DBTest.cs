using System;
using System.Collections.Generic;
using Windows.Foundation;
using Dash;
using DashShared;
using Dash.Controllers.Operators;

namespace Dash
{
    public class DBTest : CourtesyDocument
    {
        public static DocumentType UmpType  = new DocumentType("3CC1574C-A361-4543-9898-8E6435EF6930", "Umpire");
        public static DocumentType GameType = new DocumentType("6830665B-8300-430D-9854-4DD13488A6CF", "Game");
        public static DocumentType AssignmentType = new DocumentType("FBAD8901-2453-4ADC-9076-D5ED83F46B9B", "Assignment");
        public static DocumentType VolunteerType  = new DocumentType("CC865EF7-A0C3-4740-9E02-3D6E1ACCC7D1", "Volunteer");
        public static Key GameDateKey        = new Key("48A9F432-8757-4B8D-A2F4-51E1BAE44E5B", "GameDate");
        public static Key GameTimeKey        = new Key("0EF91257-92E2-44F6-8D12-A2D9AAFFD941", "GameTime");
        public static Key AssigmentGameKey   = new Key("2787E322-1E7B-4606-B892-CB3F2195E7E3", "AssignedGame");
        public static Key AssigmentPersonKey = new Key("FF312C91-46D9-4DE1-A38D-1FC6323AF9E2", "AssignedPerson");
        public static Key UmpAssignmentsKey  = new Key("9BB856BE-D3C5-425E-A6EF-0F09B28414D3", "UmpAssignments");
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
            var dc = new DocumentController(fields, UmpType);
            dc.SetField(DashConstants.KeyStore.ThisKey, new DocumentFieldModelController(dc), true);
            dc.SetField(UmpAssignmentsKey, DBSearchOperatorFieldModelController.CreateSearch(new ReferenceFieldModelController(dc.GetId(), DashConstants.KeyStore.ThisKey), "AssignedGame.GameDate"), true);
            return dc;
        }
        static DocumentController CreatePrototypeVol()
        {
            var fields = new Dictionary<Key, FieldModelController>();
            fields.Add(VolNameKey, new TextFieldModelController("Prototype Volunteer"));
            var dc = new DocumentController(fields, VolunteerType);
            dc.SetField(DashConstants.KeyStore.ThisKey, new DocumentFieldModelController(dc), true);
            return dc;
        }
        static DocumentController CreatePrototypeGame()
        {
            var fields = new Dictionary<Key, FieldModelController>();
            fields.Add(GameDateKey, new TextFieldModelController("Prototype Game Date"));
            fields.Add(GameTimeKey, new TextFieldModelController("Prototype Game Time"));
            var dc = new DocumentController(fields, GameType);
            dc.SetField(DashConstants.KeyStore.ThisKey, new DocumentFieldModelController(dc), true);
            return dc;
        }
        static DocumentController CreatePrototypeAssignment()
        {
            var fields = new Dictionary<Key, FieldModelController>();
            fields.Add(AssigmentGameKey,   new DocumentFieldModelController(PrototypeGame));
            fields.Add(AssigmentPersonKey, new DocumentFieldModelController(PrototypeUmp));
            var dc = new DocumentController(fields, AssignmentType);
            dc.SetField(DashConstants.KeyStore.ThisKey, new DocumentFieldModelController(dc), true);
            return dc;
        }

        static DocumentController CreatePrototypeUmpLayout()
        {
            // set the default layout parameters on prototypes of field layout documents
            // these prototypes will be overridden by delegates when an instance is created
            var prototypeUmpNameLayout = new TextingBox(new ReferenceFieldModelController(PrototypeUmp.GetId(), UmpNameKey), 0, 0, 200, 50);
            var prototypeUmpAssignmentsLayout = new TextingBox(new ReferenceFieldModelController(PrototypeUmp.GetId(), UmpAssignmentsKey), 0, 50, 200, 50);
            var prototypeLayout = new StackingPanel(new[] { prototypeUmpNameLayout.Document, prototypeUmpAssignmentsLayout.Document }, true);
            prototypeLayout.Document.SetField(DashConstants.KeyStore.WidthFieldKey, new NumberFieldModelController(200), true);
            prototypeLayout.Document.SetField(DashConstants.KeyStore.HeightFieldKey, new NumberFieldModelController(100), true);

            return prototypeLayout.Document;
        }
        static DocumentController CreatePrototypeVolLayout()
        {
            // set the default layout parameters on prototypes of field layout documents
            // these prototypes will be overridden by delegates when an instance is created
            var prototypeVolNameLayout = new TextingBox(new ReferenceFieldModelController(PrototypeVol.GetId(), VolNameKey), 0, 0, 200, 100);
            var prototypeLayout = new StackingPanel(new[] { prototypeVolNameLayout.Document }, true);
            prototypeLayout.Document.SetField(DashConstants.KeyStore.WidthFieldKey, new NumberFieldModelController(200), true);
            prototypeLayout.Document.SetField(DashConstants.KeyStore.HeightFieldKey, new NumberFieldModelController(100), true);

            return prototypeLayout.Document;
        }
        static DocumentController CreatePrototypeGameLayout()
        {
            // set the default layout parameters on prototypes of field layout documents
            // these prototypes will be overridden by delegates when an instance is created
            var prototypeGameDateLayout = new TextingBox(new ReferenceFieldModelController(PrototypeGame.GetId(), GameDateKey), 0, 0, 200, 100);
            var prototypeGameTimeLayout = new TextingBox(new ReferenceFieldModelController(PrototypeGame.GetId(), GameTimeKey), 0, 50, 200, 100);
            var prototypeLayout = new StackingPanel(new[] { prototypeGameDateLayout.Document, prototypeGameTimeLayout.Document }, true);
            prototypeLayout.Document.SetField(DashConstants.KeyStore.WidthFieldKey, new NumberFieldModelController(200), true);
            prototypeLayout.Document.SetField(DashConstants.KeyStore.HeightFieldKey, new NumberFieldModelController(100), true);

            return prototypeLayout.Document;
        }
        static DocumentController CreatePrototypeAssignmentLayout()
        {
            // set the default layout parameters on prototypes of field layout documents
            // these prototypes will be overridden by delegates when an instance is created
            var prototypeAssignmentGameLayout   = new DocumentBox(new ReferenceFieldModelController(PrototypeAssign.GetId(), AssigmentGameKey), 0,  0, 200, 100);
            var prototypeAssignmentPersonLayout = new DocumentBox(new ReferenceFieldModelController(PrototypeAssign.GetId(), AssigmentPersonKey), 0, 100, 200, 100);
            var prototypeLayout = new StackingPanel(new[] { prototypeAssignmentGameLayout.Document, prototypeAssignmentPersonLayout.Document }, true);
            prototypeLayout.Document.SetField(DashConstants.KeyStore.WidthFieldKey, new NumberFieldModelController(200), true);
            prototypeLayout.Document.SetField(DashConstants.KeyStore.HeightFieldKey, new NumberFieldModelController(200), true);

            return prototypeLayout.Document;
        }

        public List<DocumentController> Documents = new List<DocumentController>();
        public DBTest()
        {
            
            var Ump1Doc = PrototypeUmp.MakeDelegate();
            var Ump2Doc = PrototypeUmp.MakeDelegate();
            var Vol1Doc = PrototypeVol.MakeDelegate();
            var gameDoc = PrototypeGame.MakeDelegate();
            var game2Doc = PrototypeGame.MakeDelegate();
            var game3Doc = PrototypeGame.MakeDelegate();

            {
                Ump1Doc.SetField(UmpNameKey, new TextFieldModelController("George"), true);
                Ump1Doc.SetField(DashConstants.KeyStore.ThisKey, new DocumentFieldModelController(Ump1Doc), true);
                var ump1Layout = PrototypeUmpLayout.MakeDelegate();
                ump1Layout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Ump1Doc, ump1Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Ump1Doc);
            }

            {
                Ump2Doc.SetField(UmpNameKey, new TextFieldModelController("Matt"), true);
                Ump2Doc.SetField(DashConstants.KeyStore.ThisKey, new DocumentFieldModelController(Ump2Doc), true);
                var ump2Layout = PrototypeUmpLayout.MakeDelegate();
                ump2Layout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Ump2Doc, ump2Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Ump2Doc);
            }

            {
                Vol1Doc.SetField(VolNameKey, new TextFieldModelController("Bob"), true);
                Vol1Doc.SetField(DashConstants.KeyStore.ThisKey, new DocumentFieldModelController(Vol1Doc), true);
                var vol1Layout = PrototypeVolLayout.MakeDelegate();
                vol1Layout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Vol1Doc, vol1Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Vol1Doc);
            }

            {
                var Ass1Doc = PrototypeAssign.MakeDelegate();
                Ass1Doc.SetField(DashConstants.KeyStore.ThisKey, new DocumentFieldModelController(Ass1Doc), true);
                Ass1Doc.SetField(AssigmentGameKey, new DocumentFieldModelController(gameDoc), true);
                Ass1Doc.SetField(AssigmentPersonKey, new DocumentFieldModelController(Ump1Doc), true);
                var ass1Layout = PrototypeAssignmentLayout.MakeDelegate();
                ass1Layout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Ass1Doc, ass1Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Ass1Doc);
            }

            {
                var Ass2Doc = PrototypeAssign.MakeDelegate();
                Ass2Doc.SetField(DashConstants.KeyStore.ThisKey, new DocumentFieldModelController(Ass2Doc), true);
                Ass2Doc.SetField(AssigmentGameKey, new DocumentFieldModelController(game2Doc), true);
                Ass2Doc.SetField(AssigmentPersonKey, new DocumentFieldModelController(Ump1Doc), true);
                var ass2Layout = PrototypeAssignmentLayout.MakeDelegate();
                ass2Layout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Ass2Doc, ass2Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Ass2Doc);
            }

            {
                var Ass4Doc = PrototypeAssign.MakeDelegate();
                Ass4Doc.SetField(DashConstants.KeyStore.ThisKey, new DocumentFieldModelController(Ass4Doc), true);
                Ass4Doc.SetField(AssigmentGameKey, new DocumentFieldModelController(game2Doc), true);
                Ass4Doc.SetField(AssigmentPersonKey, new DocumentFieldModelController(Ump2Doc), true);
                var ass4Layout = PrototypeAssignmentLayout.MakeDelegate();
                ass4Layout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Ass4Doc, ass4Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Ass4Doc);

            }
            {
                var Ass5Doc = PrototypeAssign.MakeDelegate();
                Ass5Doc.SetField(DashConstants.KeyStore.ThisKey, new DocumentFieldModelController(Ass5Doc), true);
                Ass5Doc.SetField(AssigmentGameKey, new DocumentFieldModelController(game3Doc), true);
                Ass5Doc.SetField(AssigmentPersonKey, new DocumentFieldModelController(Vol1Doc), true);
                var ass4Layout = PrototypeAssignmentLayout.MakeDelegate();
                ass4Layout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Ass5Doc, ass4Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Ass5Doc);
            }
            {
                gameDoc.SetField(DashConstants.KeyStore.ThisKey, new DocumentFieldModelController(gameDoc), true);
                gameDoc.SetField(GameTimeKey, new TextFieldModelController("4:30"), true);
                gameDoc.SetField(GameDateKey, new TextFieldModelController("July 11"), true);
                var game1Layout = PrototypeGameLayout.MakeDelegate();
                game1Layout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(gameDoc, game1Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(gameDoc);
            }
            {
                game2Doc.SetField(DashConstants.KeyStore.ThisKey, new DocumentFieldModelController(game2Doc), true);
                game2Doc.SetField(GameTimeKey, new TextFieldModelController("5:30"), true);
                game2Doc.SetField(GameDateKey, new TextFieldModelController("July 14"), true);
                var game2Layout = PrototypeGameLayout.MakeDelegate();
                game2Layout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(game2Doc, game2Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(game2Doc);
            }
            {
                game3Doc.SetField(DashConstants.KeyStore.ThisKey, new DocumentFieldModelController(game3Doc), true);
                game3Doc.SetField(GameTimeKey, new TextFieldModelController("9:30"), true);
                game3Doc.SetField(GameDateKey, new TextFieldModelController("July 16"), true);
                var game3Layout = PrototypeGameLayout.MakeDelegate();
                game3Layout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(game3Doc, game3Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(game3Doc);
            }
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