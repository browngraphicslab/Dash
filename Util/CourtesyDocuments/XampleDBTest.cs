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

        //public static DocumentType UmpType = new DocumentType("3CC1574C-A361-4543-9898-8E6435EF6930", "Umpire");
        //public static DocumentType GameType = new DocumentType("6830665B-8300-430D-9854-4DD13488A6CF", "Game");
        //public static DocumentType AssignmentType = new DocumentType("FBAD8901-2453-4ADC-9076-D5ED83F46B9B", "Assignment");
        //public static DocumentType VolunteerType = new DocumentType("CC865EF7-A0C3-4740-9E02-3D6E1ACCC7D1", "Volunteer");
        public static DocumentType WebType = new DocumentType("ED1EDECE-2434-4BDB-A8E8-3DF7A0CE4BB0", "Web Doc");

        public static KeyController NullDocNameKey = new KeyController("3E74836B-CDD2-4F0A-9031-6786B03A40A4");

        //public static KeyController GameDateKey = new KeyController("48A9F432-8757-4B8D-A2F4-51E1BAE44E5B", "GameDate");
        //public static KeyController GameTimeKey = new KeyController("0EF91257-92E2-44F6-8D12-A2D9AAFFD941", "GameTime");

        //public static KeyController AssigmentGameKey = new KeyController("2787E322-1E7B-4606-B892-CB3F2195E7E3", "AssignedGame");
        //public static KeyController AssigmentPersonKey = new KeyController("FF312C91-46D9-4DE1-A38D-1FC6323AF9E2", "AssignedPerson");

        //public static KeyController UmpAssignmentsKey = new KeyController("9BB856BE-D3C5-425E-A6EF-0F09B28414D3", "UmpAssignments");
        //public static KeyController UmpNameKey = new KeyController("462664D8-11B9-4561-B65B-AB3A2DAADB3B", "UmpName");
        //public static KeyController UmpNameLabelKey = new KeyController("69079F30-ACFE-442C-8ABE-9115B7B7C974", "_UmpNameLabel");
        //public static KeyController UmpPictureKey = new KeyController("6B9AD824-C82B-4E11-A216-E83FC87F98C6", "UmpPicture");
        //public static KeyController VolNameKey = new KeyController("3908F612-15FC-492C-A6E1-239EFCDC5ED5", "VolName");
        //public static KeyController VolNameLabelKey = new KeyController("FC0FCF99-CB77-4FF6-8AFF-D2E6BA72F8A0", "_VolNameLabel");
        //public static KeyController AgeLabelKey = new KeyController("C7724C9E-FB0A-4855-86C6-27461D0EF769", "_AgeLabel");
        //public static KeyController AgeKey = new KeyController("CEFAA1C9-C21D-4429-905B-AB5A68550F76", "Age");

        public static KeyController WebUrlKey = new KeyController("427B9FB5-C5DB-422E-882D-FFC9A17266C3", "WebUrl");

        //public static DocumentController PrototypeUmp = CreatePrototypeUmp();
        //public static DocumentController PrototypeGame = CreatePrototypeGame();
        //public static DocumentController PrototypeVol = CreatePrototypeVol();
        //public static DocumentController PrototypeAssign = CreatePrototypeAssignment();

        //public static DocumentController PrototypeUmpLayout = CreatePrototypeUmpLayout();
        //public static DocumentController PrototypeGameLayout = CreatePrototypeGameLayout();
        //public static DocumentController PrototypeVolLayout = CreatePrototypeVolLayout();
        //public static DocumentController PrototypeAssignmentLayout = CreatePrototypeAssignmentLayout();

        public static DocumentController DBNull = CreateNull();
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
            return null;
        }

        static DocumentController CreatePrototypeUmp()
        {
            //var fields = new Dictionary<KeyController, FieldControllerBase>();
            //fields.Add(UmpNameKey, new TextController("PrototypeUmpire"));
            //fields.Add(KeyStore.AbstractInterfaceKey, new TextController("Umpire Data API"));
            //var dc = new DocumentController(fields, UmpType);
            //dc.SetField(KeyStore.ThisKey, new DocumentController(dc), true);
            //dc.SetField(AgeKey, new NumberController(88), true);
            //var searchDoc = DBSearchOperatorController.CreateSearch(new DocumentReferenceFieldController(dc.GetId(), KeyStore.ThisKey),
            //                                                                  DBDoc, "AssignedPerson", "AssignedGame");
            //dc.SetField(UmpAssignmentsKey, new ReferenceController(searchDoc.GetId(), KeyStore.CollectionOutputKey), true);
            //dc.SetField(UmpNameLabelKey, new TextController("Umpire : "), true);
            //dc.SetField(AgeLabelKey, new TextController("Age : "), true);
            //dc.SetField(UmpPictureKey, new ImageController(new Uri("ms-appx://Dash/Assets/cat.jpg")), true);
            //dc.SetField(KeyStore.PrimaryKeyKey, new ListController<TextController>(
            //    new TextController[] { new TextController(UmpNameKey.Id) }), true);
            //return dc;
            return null;
        }
        static DocumentController CreatePrototypeVol()
        {
            //var fields = new Dictionary<KeyController, FieldControllerBase>();
            //fields.Add(VolNameKey, new TextController("Prototype Volunteer"));
            //fields.Add(KeyStore.AbstractInterfaceKey, new TextController("Volunteer Data API"));
            //var dc = new DocumentController(fields, VolunteerType);
            //dc.SetField(KeyStore.ThisKey, new DocumentController(dc), true);
            //dc.SetField(VolNameLabelKey, new TextController("Volunteer : "), true);
            //dc.SetField(AgeLabelKey, new TextController("Age : "), true);
            //dc.SetField(KeyStore.PrimaryKeyKey, new ListController<TextController>(
            //    new TextController[] { new TextController(VolNameKey.Id) }), true);
            //return dc;
            return null;
        }
        static DocumentController CreatePrototypeGame()
        {
            //var fields = new Dictionary<KeyController, FieldControllerBase>();
            //fields.Add(GameDateKey, new TextController("Prototype Game Date"));
            //fields.Add(GameTimeKey, new TextController("Prototype Game Time"));
            //fields.Add(KeyStore.AbstractInterfaceKey, new TextController("Game Data API"));
            //var dc = new DocumentController(fields, GameType);
            //dc.SetField(KeyStore.ThisKey, new DocumentController(dc), true);
            //dc.SetField(KeyStore.PrimaryKeyKey, new ListController<TextController>(
            //   new TextController[] { new TextController(GameDateKey.Id), new TextController(GameTimeKey.Id) }), true);
            //return dc;
            return null;
        }
        static DocumentController CreatePrototypeAssignment()
        {
            //var fields = new Dictionary<KeyController, FieldControllerBase>();
            //fields.Add(AssigmentGameKey,   new DocumentController(PrototypeGame));
            //fields.Add(AssigmentPersonKey, new DocumentController(PrototypeUmp));
            //fields.Add(KeyStore.AbstractInterfaceKey, new TextController("Assignment Data API"));
            //var dc = new DocumentController(fields, AssignmentType);
            //dc.SetField(KeyStore.ThisKey, new DocumentController(dc), true);
            //dc.SetField(KeyStore.PrimaryKeyKey, new ListController<TextController>(
            //    new TextController[] { new TextController(AssigmentGameKey.Id), new TextController(AssigmentPersonKey.Id) }), true);
            //return dc;
            return null;
        }

        static DocumentController CreatePrototypeUmpLayout()
        {
            //             set the default layout parameters on prototypes of field layout documents
            //             these prototypes will be overridden by delegates when an instance is created

            //            var prototypeUmpNameLabelLayout = new TextingBox(new DocumentReferenceFieldController(PrototypeUmp.GetId(), UmpNameLabelKey), 0, 0, 60, double.NaN, FontWeights.Bold);
            //            var prototypeUmpNameLayout      = new TextingBox(new DocumentReferenceFieldController(PrototypeUmp.GetId(), UmpNameKey), 0, 0, 75, double.NaN);
            //            var prototypeUmpAgeLayout       = new TextingBox(new DocumentReferenceFieldController(PrototypeUmp.GetId(), AgeKey), 0, 0, double.NaN, double.NaN);
            //            var prototypeUmpImageLayout     = new ImageBox(new DocumentReferenceFieldController(PrototypeUmp.GetId(), UmpPictureKey), 0, 0, 50, 50);
            //            var prototypeUmpLayout          = new StackLayout(new[] { prototypeUmpNameLabelLayout.Document, prototypeUmpNameLayout.Document, prototypeUmpAgeLayout.Document, prototypeUmpImageLayout.Document }, true);
            //            prototypeUmpLayout.Document.SetField(KeyStore.HeightFieldKey, new NumberController(50), true);

            //            var prototypeUmpAssignmentsLayout = new TextingBox(new ReferenceController(PrototypeUmp.GetId(), UmpAssignmentsKey), 0, 0, double.NaN, double.NaN);
            //<<<<<<< HEAD
            //            var prototypeUmpAssignmentsLayout = new CollectionBox(new ReferenceController(PrototypeUmp.GetId(), UmpAssignmentsKey), 0, 0, double.NaN, double.NaN);
            //            prototypeUmpAssignmentsLayout.Document.SetField(KeyStore.CollectionViewTypeKey, new TextController(CollectionView.CollectionViewType.Text.ToString()), true);
            //=======
            //            var prototypeUmpAssignmentsLayout = new CollectionBox(new DocumentReferenceFieldController(PrototypeUmp.GetId(), UmpAssignmentsKey), 0, 0, double.NaN, double.NaN);
            //            prototypeUmpAssignmentsLayout.Document.SetField(CollectionBox.CollectionViewTypeKey, new TextController(CollectionView.CollectionViewType.Text.ToString()), true);
            //>>>>>>> origin/local_integration

            //            var prototypeLayout = new StackLayout(new[] { prototypeUmpLayout.Document, prototypeUmpAssignmentsLayout.Document });
            //            prototypeLayout.Document.SetField(KeyStore.WidthFieldKey, new NumberController(200), true);
            //            prototypeLayout.Document.SetField(KeyStore.HeightFieldKey, new NumberController(200), true);
            //            prototypeLayout.Document.SetField(KeyStore.AbstractInterfaceKey, new TextController("Ump Layout API"), true);

            //            return prototypeLayout.Document;
            return null;

        }
        static DocumentController CreatePrototypeVolLayout()
        {
            // set the default layout parameters on prototypes of field layout documents
            // these prototypes will be overridden by delegates when an instance is created
            //var prototypeVolNameLabelLayout = new TextingBox(new DocumentReferenceFieldController(PrototypeVol.GetId(), VolNameLabelKey), 0, 0, 100, double.NaN, FontWeights.Bold);
            //var prototypeVolNameLayout = new TextingBox(new DocumentReferenceFieldController(PrototypeVol.GetId(), VolNameKey), 0, 0, 100, double.NaN);
            //var prototypeVolAgeLayout = new TextingBox(new DocumentReferenceFieldController(PrototypeVol.GetId(), AgeKey), 0, 0, double.NaN, double.NaN);
            //var prototypeLayout = new StackLayout(new[] { prototypeVolNameLabelLayout.Document, prototypeVolNameLayout.Document, prototypeVolAgeLayout.Document }, true);
            //prototypeLayout.Document.SetField(KeyStore.WidthFieldKey, new NumberController(200), true);
            //prototypeLayout.Document.SetField(KeyStore.HeightFieldKey, new NumberController(100), true);
            //prototypeLayout.Document.SetField(KeyStore.AbstractInterfaceKey, new TextController("Vol Layout API"), true);

            //return prototypeLayout.Document;
            return null;

        }
        static DocumentController CreatePrototypeGameLayout()
        {
            //var prototypeLayout = new KeyValueDocumentBox(new DocumentReferenceFieldController(PrototypeGame.GetId(), KeyStore.ThisKey));
            //prototypeLayout.Document.SetField(KeyStore.WidthFieldKey, new NumberController(300), true);
            //prototypeLayout.Document.SetField(KeyStore.HeightFieldKey, new NumberController(100), true);
            //prototypeLayout.Document.SetField(KeyStore.AbstractInterfaceKey, new TextController("Game Layout API"), true);

            //return prototypeLayout.Document;
            return null;

        }
        static DocumentController CreatePrototypeAssignmentLayout()
        {
            //var prototypeLayout = new KeyValueDocumentBox(new DocumentReferenceFieldController(PrototypeAssign.GetId(), KeyStore.ThisKey));
            //prototypeLayout.Document.SetField(KeyStore.WidthFieldKey, new NumberController(300), true);
            //prototypeLayout.Document.SetField(KeyStore.HeightFieldKey, new NumberController(100), true);
            //prototypeLayout.Document.SetField(KeyStore.AbstractInterfaceKey, new TextController("Assignment Layout API"), true);

            //return prototypeLayout.Document;
            return null;

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

            //var Ump1Doc = PrototypeUmp.MakeDelegate();
            //var Ump2Doc = PrototypeUmp.MakeDelegate();
            //var Vol1Doc = PrototypeVol.MakeDelegate();
            //var gameDoc = PrototypeGame.MakeDelegate();
            //var game2Doc = PrototypeGame.MakeDelegate();
            //var game3Doc = PrototypeGame.MakeDelegate();
            //var Ass1Doc = PrototypeAssign.MakeDelegate();


            //{
            //    Ass1Doc.SetField(KeyStore.ThisKey, new DocumentController(Ass1Doc), true);
            //    Ass1Doc.SetField(AssigmentGameKey, new DocumentController(gameDoc), true);
            //    Ass1Doc.SetField(AssigmentPersonKey, new DocumentController(Ump1Doc), true);
            //    var ass1Layout = PrototypeAssignmentLayout.MakeDelegate();
            //    ass1Layout.SetField(KeyStore.PositionFieldKey, new PointController(new Point(0, 0)), true);
            //    SetLayoutForDocument(Ass1Doc, ass1Layout, forceMask: true, addToLayoutList: true);
            //    Documents.Add(Ass1Doc);
            //}
            //{
            //    var Ass2Doc = PrototypeAssign.MakeDelegate();
            //    Ass2Doc.SetField(KeyStore.ThisKey, new DocumentController(Ass2Doc), true);
            //    Ass2Doc.SetField(AssigmentGameKey, new DocumentController(game2Doc), true);
            //    Ass2Doc.SetField(AssigmentPersonKey, new DocumentController(Ump1Doc), true);
            //    var ass2Layout = PrototypeAssignmentLayout.MakeDelegate();
            //    ass2Layout.SetField(KeyStore.PositionFieldKey, new PointController(new Point(0, 0)), true);
            //    SetLayoutForDocument(Ass2Doc, ass2Layout, forceMask: true, addToLayoutList: true);
            //    Documents.Add(Ass2Doc);
            //}

            //{
            //    var Ass4Doc = PrototypeAssign.MakeDelegate();
            //    Ass4Doc.SetField(KeyStore.ThisKey, new DocumentController(Ass4Doc), true);
            //    Ass4Doc.SetField(AssigmentGameKey, new DocumentController(game2Doc), true);
            //    Ass4Doc.SetField(AssigmentPersonKey, new DocumentController(Ump2Doc), true);
            //    var ass4Layout = PrototypeAssignmentLayout.MakeDelegate();
            //    ass4Layout.SetField(KeyStore.PositionFieldKey, new PointController(new Point(0, 0)), true);
            //    SetLayoutForDocument(Ass4Doc, ass4Layout, forceMask: true, addToLayoutList: true);
            //    Documents.Add(Ass4Doc);

            //}
            //{
            //    var Ass5Doc = PrototypeAssign.MakeDelegate();
            //    Ass5Doc.SetField(KeyStore.ThisKey, new DocumentController(Ass5Doc), true);
            //    Ass5Doc.SetField(AssigmentGameKey, new DocumentController(game3Doc), true);
            //    Ass5Doc.SetField(AssigmentPersonKey, new DocumentController(Vol1Doc), true);
            //    var ass4Layout = PrototypeAssignmentLayout.MakeDelegate();
            //    ass4Layout.SetField(KeyStore.PositionFieldKey, new PointController(new Point(0, 0)), true);
            //    SetLayoutForDocument(Ass5Doc, ass4Layout, forceMask: true, addToLayoutList: true);
            //    Documents.Add(Ass5Doc);
            //}
            //{
            //    Ump1Doc.SetField(UmpNameKey, new TextController("George"), true);
            //    Ump1Doc.SetField(KeyStore.ThisKey, new DocumentController(Ump1Doc), true);
            //    Ump1Doc.SetField(AgeKey, new NumberController(17), true);
            //    var ump1Layout = PrototypeUmpLayout.MakeDelegate();
            //    ump1Layout.SetField(KeyStore.PositionFieldKey, new PointController(new Point(0, 0)), true);
            //    SetLayoutForDocument(Ump1Doc, ump1Layout, forceMask: true, addToLayoutList: true);
            //    Documents.Add(Ump1Doc);
            //}

            //{
            //    Ump2Doc.SetField(UmpNameKey, new TextController("Matt"), true);
            //    Ump2Doc.SetField(KeyStore.ThisKey, new DocumentController(Ump2Doc), true);
            //    Ump2Doc.SetField(AgeKey, new NumberController(16), true);
            //    var ump2Layout = PrototypeUmpLayout.MakeDelegate();
            //    ump2Layout.SetField(KeyStore.PositionFieldKey, new PointController(new Point(0, 0)), true);
            //    SetLayoutForDocument(Ump2Doc, ump2Layout, forceMask: true, addToLayoutList: true);
            //    Documents.Add(Ump2Doc);
            //}

            //for (var i = 0; i < 1; i++)
            //{
            //    var udoc = PrototypeUmp.MakeDelegate();
            //    udoc.SetField(UmpNameKey, new TextController("Matt" + i), true);
            //    udoc.SetField(KeyStore.ThisKey, new DocumentController(udoc), true);
            //    udoc.SetField(AgeKey, new NumberController(16), true);
            //    var ump2Layout = PrototypeUmpLayout.MakeDelegate();
            //    ump2Layout.SetField(KeyStore.PositionFieldKey, new PointController(new Point(0, 0)), true);
            //    SetLayoutForDocument(udoc, ump2Layout, forceMask: true, addToLayoutList: true);
            //    Documents.Add(udoc);
            //}

            //{
            //    Vol1Doc.SetField(VolNameKey, new TextController("Bob"), true);
            //    Vol1Doc.SetField(KeyStore.ThisKey, new DocumentController(Vol1Doc), true);
            //    Vol1Doc.SetField(AgeKey, new NumberController(32), true);
            //    var vol1Layout = PrototypeVolLayout.MakeDelegate();
            //    vol1Layout.SetField(KeyStore.PositionFieldKey, new PointController(new Point(0, 0)), true);
            //    SetLayoutForDocument(Vol1Doc, vol1Layout, forceMask: true, addToLayoutList: true);
            //    Documents.Add(Vol1Doc);
            //}

            //{
            //    gameDoc.SetField(KeyStore.ThisKey, new DocumentController(gameDoc), true);
            //    gameDoc.SetField(GameTimeKey, new TextController("4:30"), true);
            //    gameDoc.SetField(GameDateKey, new TextController("July 11"), true);
            //    var game1Layout = PrototypeGameLayout.MakeDelegate();
            //    game1Layout.SetField(KeyStore.PositionFieldKey, new PointController(new Point(0, 0)), true);
            //    SetLayoutForDocument(gameDoc, game1Layout, forceMask: true, addToLayoutList: true);
            //    Documents.Add(gameDoc);
            //    gameDoc.SetField(new KeyController("AKEY", "AKEY"), new ListController<DocumentController>(new DocumentController[] { Ump1Doc, Ump2Doc }), true);
            //}
            //{
            //    game2Doc.SetField(KeyStore.ThisKey, new DocumentController(game2Doc), true);
            //    game2Doc.SetField(GameTimeKey, new TextController("5:30"), true);
            //    game2Doc.SetField(GameDateKey, new TextController("July 14"), true);
            //    var game2Layout = PrototypeGameLayout.MakeDelegate();
            //    game2Layout.SetField(KeyStore.PositionFieldKey, new PointController(new Point(0, 0)), true);
            //    SetLayoutForDocument(game2Doc, game2Layout, forceMask: true, addToLayoutList: true);
            //    Documents.Add(game2Doc);
            //}
            //{
            //    game3Doc.SetField(KeyStore.ThisKey, new DocumentController(game3Doc), true);
            //    game3Doc.SetField(GameTimeKey, new TextController("9:30"), true);
            //    game3Doc.SetField(GameDateKey, new TextController("July 16"), true);
            //    var game3Layout = PrototypeGameLayout.MakeDelegate();
            //    game3Layout.SetField(KeyStore.PositionFieldKey, new PointController(new Point(0, 0)), true);
            //    SetLayoutForDocument(game3Doc, game3Layout, forceMask: true, addToLayoutList: true);
            //    Documents.Add(game3Doc);
            //}
            //{
            //    Documents.Add(CreateWebPage("http://www.msn.com"));
            //}

            DBDoc.FieldModelUpdated -= DBDoc_DocumentFieldUpdated;
            DBDoc.FieldModelUpdated += DBDoc_DocumentFieldUpdated;
            return;


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