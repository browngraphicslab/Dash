using System;
using System.Collections.Generic;
using Windows.Foundation;
using Dash;
using DashShared;
using Dash.Controllers.Operators;

namespace Dash
{
    public class DBTest
    {
        public static DocumentType UmpType  = new DocumentType("3CC1574C-A361-4543-9898-8E6435EF6930", "Umpire");
        public static DocumentType GameType = new DocumentType("6830665B-8300-430D-9854-4DD13488A6CF", "Game");
        public static DocumentType AssignmentType = new DocumentType("FBAD8901-2453-4ADC-9076-D5ED83F46B9B", "Assignment");
        public static DocumentType VolunteerType  = new DocumentType("CC865EF7-A0C3-4740-9E02-3D6E1ACCC7D1", "Volunteer");
        public static DocumentType WebType = new DocumentType("ED1EDECE-2434-4BDB-A8E8-3DF7A0CE4BB0", "Web Doc");

        public static KeyController NullDocNameKey = new KeyController("3E74836B-CDD2-4F0A-9031-6786B03A40A4");
        
        public static KeyController GameDateKey        = new KeyController("48A9F432-8757-4B8D-A2F4-51E1BAE44E5B", "GameDate");
        public static KeyController GameTimeKey        = new KeyController("0EF91257-92E2-44F6-8D12-A2D9AAFFD941", "GameTime");

        public static KeyController AssigmentGameKey   = new KeyController("2787E322-1E7B-4606-B892-CB3F2195E7E3", "AssignedGame");
        public static KeyController AssigmentPersonKey = new KeyController("FF312C91-46D9-4DE1-A38D-1FC6323AF9E2", "AssignedPerson");

        public static KeyController UmpAssignmentsKey  = new KeyController("9BB856BE-D3C5-425E-A6EF-0F09B28414D3", "UmpAssignments");
        public static KeyController UmpNameKey         = new KeyController("462664D8-11B9-4561-B65B-AB3A2DAADB3B", "UmpName");
        public static KeyController UmpNameLabelKey    = new KeyController("69079F30-ACFE-442C-8ABE-9115B7B7C974", "_UmpNameLabel");
        public static KeyController UmpPictureKey      = new KeyController("6B9AD824-C82B-4E11-A216-E83FC87F98C6", "UmpPicture");
        public static KeyController VolNameKey         = new KeyController("3908F612-15FC-492C-A6E1-239EFCDC5ED5", "VolName");
        public static KeyController VolNameLabelKey    = new KeyController("FC0FCF99-CB77-4FF6-8AFF-D2E6BA72F8A0", "_VolNameLabel");
        public static KeyController AgeLabelKey        = new KeyController("C7724C9E-FB0A-4855-86C6-27461D0EF769", "_AgeLabel");
        public static KeyController AgeKey             = new KeyController("CEFAA1C9-C21D-4429-905B-AB5A68550F76", "Age");

        public static KeyController WebUrlKey = new KeyController("427B9FB5-C5DB-422E-882D-FFC9A17266C3", "WebUrl");

        public static DocumentController DBNull = CreateNull();
        public static DocumentController DBDoc = CreateDB();

        public static DocumentController PrototypeUmp = CreatePrototypeUmp();
        public static DocumentController PrototypeGame = CreatePrototypeGame();
        public static DocumentController PrototypeVol = CreatePrototypeVol();
        public static DocumentController PrototypeAssign = CreatePrototypeAssignment();
        public static DocumentController PrototypeWeb = CreatePrototypeWeb();

        public static DocumentController PrototypeUmpLayout = CreatePrototypeUmpLayout();
        public static DocumentController PrototypeGameLayout = CreatePrototypeGameLayout();
        public static DocumentController PrototypeVolLayout = CreatePrototypeVolLayout();
        public static DocumentController PrototypeAssignmentLayout = CreatePrototypeAssignmentLayout();
        public static DocumentController PrototypeWebLayout = CreatePrototypeWebLayout();

        static DocumentController CreateNull()
        {
            var fields = new Dictionary<KeyController, FieldModelController>();
            var dc = new DocumentController(fields, new DocumentType("DBNull", "DBNull"));
            dc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(dc), true);
            dc.SetField(NullDocNameKey, new TextFieldModelController(""), true);
            dc.SetField(KeyStore.PrimaryKeyKey, new ListFieldModelController<TextFieldModelController>(
                new TextFieldModelController[] { new TextFieldModelController(NullDocNameKey.Id) }), true);
            return dc;
        }
        static DocumentController CreateDB()
        {
            var fields = new Dictionary<KeyController, FieldModelController>();
            fields.Add(KeyStore.DataKey, new DocumentCollectionFieldModelController());
            var dc = new DocumentController(fields, new DocumentType("DBDoc", "DBDoc"));
            dc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(dc), true);
            return dc;
        }
        static DocumentController CreatePrototypeUmp()
        {
            var fields = new Dictionary<KeyController, FieldModelController>();
            fields.Add(UmpNameKey, new TextFieldModelController("PrototypeUmpire"));
            fields.Add(KeyStore.AbstractInterfaceKey, new TextFieldModelController("Umpire Data API"));
            var dc = new DocumentController(fields, UmpType);
            dc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(dc), true);
            dc.SetField(AgeKey, new NumberFieldModelController(88), true);
            var searchDoc = DBSearchOperatorFieldModelController.CreateSearch(new ReferenceFieldModelController(dc.GetId(), KeyStore.ThisKey),
                                                                              DBDoc, "AssignedPerson", "AssignedGame");
            dc.SetField(UmpAssignmentsKey, new ReferenceFieldModelController(searchDoc.GetId(), DBSearchOperatorFieldModelController.ResultsKey), true);
            dc.SetField(UmpNameLabelKey, new TextFieldModelController("Umpire : "), true);
            dc.SetField(AgeLabelKey, new TextFieldModelController("Age : "), true);
            dc.SetField(UmpPictureKey, new ImageFieldModelController(new Uri("ms-appx://Dash/Assets/cat.jpg")), true);
            dc.SetField(KeyStore.PrimaryKeyKey, new ListFieldModelController<TextFieldModelController>(
                new TextFieldModelController[] { new TextFieldModelController(UmpNameKey.Id) }), true);
            return dc;
        }
        static DocumentController CreatePrototypeVol()
        {
            var fields = new Dictionary<KeyController, FieldModelController>();
            fields.Add(VolNameKey, new TextFieldModelController("Prototype Volunteer"));
            fields.Add(KeyStore.AbstractInterfaceKey, new TextFieldModelController("Volunteer Data API"));
            var dc = new DocumentController(fields, VolunteerType);
            dc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(dc), true);
            dc.SetField(VolNameLabelKey, new TextFieldModelController("Volunteer : "), true);
            dc.SetField(AgeLabelKey, new TextFieldModelController("Age : "), true);
            dc.SetField(KeyStore.PrimaryKeyKey, new ListFieldModelController<TextFieldModelController>(
                new TextFieldModelController[] { new TextFieldModelController(VolNameKey.Id) }), true);
            return dc;
        }
        static DocumentController CreatePrototypeGame()
        {
            var fields = new Dictionary<KeyController, FieldModelController>();
            fields.Add(GameDateKey, new TextFieldModelController("Prototype Game Date"));
            fields.Add(GameTimeKey, new TextFieldModelController("Prototype Game Time"));
            fields.Add(KeyStore.AbstractInterfaceKey, new TextFieldModelController("Game Data API"));
            var dc = new DocumentController(fields, GameType);
            dc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(dc), true);
            dc.SetField(KeyStore.PrimaryKeyKey, new ListFieldModelController<TextFieldModelController>(
               new TextFieldModelController[] { new TextFieldModelController(GameDateKey.Id), new TextFieldModelController(GameTimeKey.Id) }), true);
            return dc;
        }
        static DocumentController CreatePrototypeAssignment()
        {
            var fields = new Dictionary<KeyController, FieldModelController>();
            fields.Add(AssigmentGameKey,   new DocumentFieldModelController(PrototypeGame));
            fields.Add(AssigmentPersonKey, new DocumentFieldModelController(PrototypeUmp));
            fields.Add(KeyStore.AbstractInterfaceKey, new TextFieldModelController("Assignment Data API"));
            var dc = new DocumentController(fields, AssignmentType);
            dc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(dc), true);
            dc.SetField(KeyStore.PrimaryKeyKey, new ListFieldModelController<TextFieldModelController>(
                new TextFieldModelController[] { new TextFieldModelController(AssigmentGameKey.Id), new TextFieldModelController(AssigmentPersonKey.Id) }), true);
            return dc;
        }
        static DocumentController CreatePrototypeWeb()
        {
            var fields = new Dictionary<KeyController, FieldModelController>();
            fields.Add(WebUrlKey, new TextFieldModelController("http://www.cs.brown.edu"));
            fields.Add(KeyStore.AbstractInterfaceKey, new TextFieldModelController("Web Data API"));
            var dc = new DocumentController(fields, WebType);
            dc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(dc), true);
            dc.SetField(KeyStore.PrimaryKeyKey, new ListFieldModelController<TextFieldModelController>(
                new TextFieldModelController[] { new TextFieldModelController(WebUrlKey.Id) }), true);
            return dc;
        }

        static DocumentController CreatePrototypeUmpLayout()
        {
            // set the default layout parameters on prototypes of field layout documents
            // these prototypes will be overridden by delegates when an instance is created
            var prototypeUmpNameLabelLayout = new TextingBox(new ReferenceFieldModelController(PrototypeUmp.GetId(), UmpNameLabelKey), 0, 0, 60, double.NaN, FontWeights.Bold);
            var prototypeUmpNameLayout      = new TextingBox(new ReferenceFieldModelController(PrototypeUmp.GetId(), UmpNameKey), 0, 0, 75, double.NaN);
            var prototypeUmpAgeLayout       = new TextingBox(new ReferenceFieldModelController(PrototypeUmp.GetId(), AgeKey), 0, 0, double.NaN, double.NaN);
            var prototypeUmpImageLayout     = new ImageBox(new ReferenceFieldModelController(PrototypeUmp.GetId(), UmpPictureKey), 0, 0, 50, 50);
            var prototypeUmpLayout          = new StackLayout(new[] { prototypeUmpNameLabelLayout.Document, prototypeUmpNameLayout.Document, prototypeUmpAgeLayout.Document, prototypeUmpImageLayout.Document }, true);
            prototypeUmpLayout.Document.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(50), true);

            //var prototypeUmpAssignmentsLayout = new TextingBox(new ReferenceFieldModelController(PrototypeUmp.GetId(), UmpAssignmentsKey), 0, 0, double.NaN, double.NaN);
            var prototypeUmpAssignmentsLayout = new CollectionBox(new ReferenceFieldModelController(PrototypeUmp.GetId(), UmpAssignmentsKey), 0, 0, double.NaN, double.NaN);
            prototypeUmpAssignmentsLayout.Document.SetField(CollectionBox.CollectionViewTypeKey, new TextFieldModelController(CollectionView.CollectionViewType.Text.ToString()), true);


            var prototypeLayout = new StackLayout(new[] { prototypeUmpLayout.Document, prototypeUmpAssignmentsLayout.Document });
            prototypeLayout.Document.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(200), true);
            prototypeLayout.Document.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(200), true);
            prototypeLayout.Document.SetField(KeyStore.AbstractInterfaceKey, new TextFieldModelController("Ump Layout API"), true);

            return prototypeLayout.Document;
        }
        static DocumentController CreatePrototypeVolLayout()
        {
            // set the default layout parameters on prototypes of field layout documents
            // these prototypes will be overridden by delegates when an instance is created
            var prototypeVolNameLabelLayout = new TextingBox(new ReferenceFieldModelController(PrototypeVol.GetId(), VolNameLabelKey), 0, 0, 100, double.NaN, FontWeights.Bold);
            var prototypeVolNameLayout = new TextingBox(new ReferenceFieldModelController(PrototypeVol.GetId(), VolNameKey), 0, 0, 100, double.NaN);
            var prototypeVolAgeLayout = new TextingBox(new ReferenceFieldModelController(PrototypeVol.GetId(), AgeKey), 0, 0, double.NaN, double.NaN);
            var prototypeLayout = new StackLayout(new[] { prototypeVolNameLabelLayout.Document, prototypeVolNameLayout.Document, prototypeVolAgeLayout.Document }, true);
            prototypeLayout.Document.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(200), true);
            prototypeLayout.Document.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(100), true);
            prototypeLayout.Document.SetField(KeyStore.AbstractInterfaceKey, new TextFieldModelController("Vol Layout API"), true);

            return prototypeLayout.Document;
        }
        static DocumentController CreatePrototypeGameLayout()
        {
            var prototypeLayout = new KeyValueDocumentBox(new ReferenceFieldModelController(PrototypeGame.GetId(), KeyStore.ThisKey));
            prototypeLayout.Document.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(300), true);
            prototypeLayout.Document.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(100), true);
            prototypeLayout.Document.SetField(KeyStore.AbstractInterfaceKey, new TextFieldModelController("Game Layout API"), true);

            return prototypeLayout.Document;
        }
        static DocumentController CreatePrototypeAssignmentLayout()
        {
            var prototypeLayout = new KeyValueDocumentBox(new ReferenceFieldModelController(PrototypeAssign.GetId(), KeyStore.ThisKey));
            prototypeLayout.Document.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(300), true);
            prototypeLayout.Document.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(100), true);
            prototypeLayout.Document.SetField(KeyStore.AbstractInterfaceKey, new TextFieldModelController("Assignment Layout API"), true);

            return prototypeLayout.Document;
        }

        static DocumentController CreatePrototypeWebLayout()
        {
            // set the default layout parameters on prototypes of field layout documents
            // these prototypes will be overridden by delegates when an instance is created
            var prototypeLayout = new WebBox(new ReferenceFieldModelController(PrototypeWeb.GetId(), WebUrlKey), 0, 0, 200, 50);
            prototypeLayout.Document.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(400), true);
            prototypeLayout.Document.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(800), true);
            prototypeLayout.Document.SetField(KeyStore.AbstractInterfaceKey, new TextFieldModelController("Web Layout API"), true);

            return prototypeLayout.Document;
        }
        /// <summary>
        /// Sets the active layout on the <paramref name="dataDocument"/> to the passed in <paramref name="layoutDoc"/>
        /// </summary>
        protected static void SetLayoutForDocument(DocumentController dataDocument, DocumentController layoutDoc, bool forceMask, bool addToLayoutList)
        {
            dataDocument.SetActiveLayout(layoutDoc, forceMask: forceMask, addToLayoutList: addToLayoutList);
        }
        public List<DocumentController> Documents = new List<DocumentController>();

        public static DocumentController CreateWebPage(string target, Point ?where = null)
        {
            var WebDoc = DBTest.PrototypeWeb.MakeDelegate();
            WebDoc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(WebDoc), true);
            WebDoc.SetField(DBTest.WebUrlKey, new TextFieldModelController(target), true);
            WebDoc.SetField(KeyStore.PrimaryKeyKey, new ListFieldModelController<TextFieldModelController>(
                new TextFieldModelController[] { new TextFieldModelController(DBTest.WebUrlKey.Id) }), true);

            var webLayout = new WebBox(new ReferenceFieldModelController(WebDoc.GetId(), DBTest.WebUrlKey), 0, 0, 200, 50).Document;
            webLayout.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(400), true);
            webLayout.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(800), true);
            webLayout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(where == null ? new Point() : (Point)where), true);
            WebDoc.SetActiveLayout(webLayout, forceMask: true, addToLayoutList: true);
            return WebDoc;
        }

        public DBTest()
        {
            var Ump1Doc = PrototypeUmp.MakeDelegate();
            var Ump2Doc = PrototypeUmp.MakeDelegate();
            var Vol1Doc = PrototypeVol.MakeDelegate();
            var gameDoc = PrototypeGame.MakeDelegate();
            var game2Doc = PrototypeGame.MakeDelegate();
            var game3Doc = PrototypeGame.MakeDelegate();
            var Ass1Doc = PrototypeAssign.MakeDelegate();


            {
                Ass1Doc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(Ass1Doc), true);
                Ass1Doc.SetField(AssigmentGameKey, new DocumentFieldModelController(gameDoc), true);
                Ass1Doc.SetField(AssigmentPersonKey, new DocumentFieldModelController(Ump1Doc), true);
                var ass1Layout = PrototypeAssignmentLayout.MakeDelegate();
                ass1Layout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Ass1Doc, ass1Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Ass1Doc);
            }
            {
                var Ass2Doc = PrototypeAssign.MakeDelegate();
                Ass2Doc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(Ass2Doc), true);
                Ass2Doc.SetField(AssigmentGameKey, new DocumentFieldModelController(game2Doc), true);
                Ass2Doc.SetField(AssigmentPersonKey, new DocumentFieldModelController(Ump1Doc), true);
                var ass2Layout = PrototypeAssignmentLayout.MakeDelegate();
                ass2Layout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Ass2Doc, ass2Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Ass2Doc);
            }

            {
                var Ass4Doc = PrototypeAssign.MakeDelegate();
                Ass4Doc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(Ass4Doc), true);
                Ass4Doc.SetField(AssigmentGameKey, new DocumentFieldModelController(game2Doc), true);
                Ass4Doc.SetField(AssigmentPersonKey, new DocumentFieldModelController(Ump2Doc), true);
                var ass4Layout = PrototypeAssignmentLayout.MakeDelegate();
                ass4Layout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Ass4Doc, ass4Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Ass4Doc);

            }
            {
                var Ass5Doc = PrototypeAssign.MakeDelegate();
                Ass5Doc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(Ass5Doc), true);
                Ass5Doc.SetField(AssigmentGameKey, new DocumentFieldModelController(game3Doc), true);
                Ass5Doc.SetField(AssigmentPersonKey, new DocumentFieldModelController(Vol1Doc), true);
                var ass4Layout = PrototypeAssignmentLayout.MakeDelegate();
                ass4Layout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Ass5Doc, ass4Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Ass5Doc);
            }
            {
                Ump1Doc.SetField(UmpNameKey, new TextFieldModelController("George"), true);
                Ump1Doc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(Ump1Doc), true);
                Ump1Doc.SetField(AgeKey, new NumberFieldModelController(17), true);
                var ump1Layout = PrototypeUmpLayout.MakeDelegate();
                ump1Layout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Ump1Doc, ump1Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Ump1Doc);
            }

            {
                Ump2Doc.SetField(UmpNameKey, new TextFieldModelController("Matt"), true);
                Ump2Doc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(Ump2Doc), true);
                Ump2Doc.SetField(AgeKey, new NumberFieldModelController(16), true);
                var ump2Layout = PrototypeUmpLayout.MakeDelegate();
                ump2Layout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Ump2Doc, ump2Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Ump2Doc);
            }

            for (var i = 0; i < 1; i++)
            {
                var udoc = PrototypeUmp.MakeDelegate();
                udoc.SetField(UmpNameKey, new TextFieldModelController("Matt" + i), true);
                udoc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(udoc), true);
                udoc.SetField(AgeKey, new NumberFieldModelController(16), true);
                var ump2Layout = PrototypeUmpLayout.MakeDelegate();
                ump2Layout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(udoc, ump2Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(udoc);
            }

            {
                Vol1Doc.SetField(VolNameKey, new TextFieldModelController("Bob"), true);
                Vol1Doc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(Vol1Doc), true);
                Vol1Doc.SetField(AgeKey, new NumberFieldModelController(32), true);
                var vol1Layout = PrototypeVolLayout.MakeDelegate();
                vol1Layout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(Vol1Doc, vol1Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(Vol1Doc);
            }

            {
                gameDoc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(gameDoc), true);
                gameDoc.SetField(GameTimeKey, new TextFieldModelController("4:30"), true);
                gameDoc.SetField(GameDateKey, new TextFieldModelController("July 11"), true);
                var game1Layout = PrototypeGameLayout.MakeDelegate();
                game1Layout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(gameDoc, game1Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(gameDoc);
                gameDoc.SetField(new KeyController("AKEY", "AKEY"), new DocumentCollectionFieldModelController(new DocumentController[] { Ump1Doc, Ump2Doc }), true);
            }
            {
                game2Doc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(game2Doc), true);
                game2Doc.SetField(GameTimeKey, new TextFieldModelController("5:30"), true);
                game2Doc.SetField(GameDateKey, new TextFieldModelController("July 14"), true);
                var game2Layout = PrototypeGameLayout.MakeDelegate();
                game2Layout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(game2Doc, game2Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(game2Doc);
            }
            {
                game3Doc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(game3Doc), true);
                game3Doc.SetField(GameTimeKey, new TextFieldModelController("9:30"), true);
                game3Doc.SetField(GameDateKey, new TextFieldModelController("July 16"), true);
                var game3Layout = PrototypeGameLayout.MakeDelegate();
                game3Layout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                SetLayoutForDocument(game3Doc, game3Layout, forceMask: true, addToLayoutList: true);
                Documents.Add(game3Doc);
            }
            //{
            //    Documents.Add(CreateWebPage("http://www.msn.com"));
            //}

            DBDoc.DocumentFieldUpdated -= DBDoc_DocumentFieldUpdated;
            DBDoc.DocumentFieldUpdated += DBDoc_DocumentFieldUpdated;
                
        }

        private void DBDoc_DocumentFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            foreach (var d in (DBDoc.GetDereferencedField(KeyStore.DataKey, null) as DocumentCollectionFieldModelController).GetDocuments())
                if (d != MainPage.Instance.MainDocument && d != DBDoc) {
                    d.DocumentFieldUpdated -= D_DocumentFieldUpdated;
                    d.DocumentFieldUpdated += D_DocumentFieldUpdated;
                }
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
        static public void ResetCycleDetection()
        {
            if (DBTest.PrototypeUmp != null)
                BeenThere.Clear();
        }
        private void D_DocumentFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            foreach (var s in BeenThere)
                if (s.sender == sender)
                {
                    if (args.NewValue.CheckType(s.args.NewValue))
                    {
                        if (args.NewValue is DocumentCollectionFieldModelController)
                        {
                            var equal = true;
                            var d1 = (args.NewValue as DocumentCollectionFieldModelController).Data;
                            var d2 = (s.args.NewValue as DocumentCollectionFieldModelController).Data;
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
                    if (args.NewValue == s.args.NewValue)
                    {
                         return;
                    }
                }
            BeenThere.Add(new SeenIt(sender, args));
           // DBDoc.SetField(KeyStore.DataKey, new ReferenceFieldModelController(MainPage.Instance.MainDocument.GetId(), DocumentCollectionFieldModelController.CollectionKey), true);
        }
    }
}