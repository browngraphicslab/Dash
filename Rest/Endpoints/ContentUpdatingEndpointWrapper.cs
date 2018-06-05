using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class ContentUpdatingEndpointWrapper<T> : IModelEndpoint<T> where T : EntityBase
    {
        private IModelEndpoint<T> _endpoint;

        public ContentUpdatingEndpointWrapper(IModelEndpoint<T> endpoint)
        {
            _endpoint = endpoint;
        }

        private void AddModelsToControllers(IEnumerable<EntityBase> models)
        {
            HashSet<EntityBase> entities = new HashSet<EntityBase>();
            foreach (var entityBase in models)
            {
                entities.Add(entityBase);
                if (entityBase is FieldModel fieldModel)
                {
                    entities.UnionWith(TrackDownReferences(fieldModel));
                }
            }

            var entityBases = entities.ToList();
            var docs = entityBases.OfType<DocumentModel>().ToArray();

            foreach (var model in docs)
            {
                if (!ContentController<FieldModel>.HasController(model.Id))
                {
                    model.NewController();
                }
            }

            foreach (var model in entityBases.OfType<KeyModel>())
            {
                if (!ContentController<FieldModel>.HasController(model.Id))
                {
                    model.NewController();
                }
            }

            var fields = entityBases.OfType<FieldModel>();
            var fieldModels = fields as IList<FieldModel> ?? fields.ToList();
            var allLists = fieldModels.OfType<ListModel>().ToList();

            foreach (var model in fieldModels.Where(i => !(i is ListModel)))
            {
                if (!ContentController<FieldModel>.HasController(model.Id))
                {
                    model.NewController();
                }
            }


            foreach (var model in allLists)
            {
                if (!ContentController<FieldModel>.HasController(model.Id))
                {
                    model.NewController();
                }
            }

            var modelList = models.ToList();


            //modelList.OfType<KeyModel>().ToList().ForEach(i => i.GetController().Init());
            //modelList.OfType<FieldModel>().ToList().ForEach(i => i.GetController().CreateReferences());
            //modelList.OfType<FieldModel>().ToList().ForEach(i => i.GetController().Init());
            var refs = modelList.OfType<ReferenceModel>().ToList();
            refs.ForEach(i => i.GetController().Init());
            modelList.OfType<FieldModel>().Except(refs).ToList().ForEach(i => i.GetController().Init());
            //modelList.OfType<DocumentModel>().ToList().ForEach(i => i.GetController().Init());
        }

        private Func<RestRequestReturnArgs, Task> GetSuccessFunc(Func<RestRequestReturnArgs, Task> success)
        {
            async Task func(RestRequestReturnArgs arg)
            {
                AddModelsToControllers(arg.ReturnedObjects);
                await success?.Invoke(arg);
            }

            return func;
        }

        private Func<RestRequestReturnArgs, Task> GetSuccessFunc<V>(Func<IEnumerable<V>, Task> success) where V:EntityBase
        {
            async Task func(RestRequestReturnArgs arg)
            {
                AddModelsToControllers(arg.ReturnedObjects);
                await success?.Invoke(arg.ReturnedObjects.OfType<V>());
            }

            return func;
        }

        public void AddDocument(T newDocument, Action<T> success, Action<Exception> error)
        {
            //AddModelsToControllers(new List<EntityBase>(){newDocument});
            _endpoint.AddDocument(newDocument, success, error);
        }

        public void UpdateDocument(T documentToUpdate, Action<T> success, Action<Exception> error)
        {
            _endpoint.UpdateDocument(documentToUpdate, success, error);
        }

        public async Task GetDocument(string id, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
        {
            await _endpoint.GetDocument(id, GetSuccessFunc(success), error);
        }

        public async Task GetDocuments(IEnumerable<string> ids, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
        {
            await _endpoint.GetDocuments(ids, GetSuccessFunc(success), error);
        }

        public void DeleteDocument(T document, Action success, Action<Exception> error)
        {
            ContentController<T>.RemoveController(document.Id);
             _endpoint.DeleteDocument(document, success, error);
        }

        public void DeleteAllDocuments(Action success, Action<Exception> error)
        {
            ContentController<T>.ClearAllControllersAndModels();
            _endpoint.DeleteAllDocuments(success, error);
        }

        public async Task GetDocumentsByQuery(IQuery<T> query, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
        {
            await _endpoint.GetDocumentsByQuery(query, GetSuccessFunc(success), error);
        }

        public Task GetDocuments<V>(IEnumerable<string> ids, Func<IEnumerable<V>, Task> success, Action<Exception> error) where V : EntityBase
        {
            return _endpoint.GetDocuments(ids, GetSuccessFunc(success), error);
        }

        public Task GetDocumentsByQuery<V>(IQuery<T> query, Func<IEnumerable<V>, Task> success, Action<Exception> error) where V : EntityBase
        {
            return _endpoint.GetDocumentsByQuery(query, GetSuccessFunc(success), error);
        }

        public async Task Close()
        {
        }

        public void HasDocument(T model, Action<bool> success, Action<Exception> error)
        {
            _endpoint.HasDocument(model, success, error);
        }

        public bool CheckAllDocuments(IEnumerable<T> documents)
        {
            return _endpoint.CheckAllDocuments(documents);
        }

        public Dictionary<string, string> GetBackups()
        {
            return _endpoint.GetBackups();
        }

        protected HashSet<FieldModel> TrackDownReferences(FieldModel field)
        {
            switch (field)
            {
                case DocumentModel doc:
                    return TrackDownReferences(doc);
                case ListModel list:
                    return TrackDownReferences(list);
                case DocumentReferenceModel dref:
                    return TrackDownReferences(dref);
                case PointerReferenceModel pref:
                    return TrackDownReferences(pref);
                default:
                    return new HashSet<FieldModel> { field };
            }
        }

        private HashSet<FieldModel> TrackDownReferences(DocumentModel doc)
        {
            var fields = new HashSet<FieldModel> {doc};

            foreach (var kvp in doc.Fields)
            {
                fields.Add(kvp.Key.CreateObject<KeyModel>());
                var field = kvp.Value.CreateObject<FieldModel>();
                fields.Add(field);
                fields.UnionWith(TrackDownReferences(field));
            }

            return fields;
        }

        private HashSet<FieldModel> TrackDownReferences(ListModel list)
        {
            var fields = new HashSet<FieldModel> {list};

            foreach (var f in list.Data)
            {
                var field = f.CreateObject<FieldModel>();
                fields.Add(field);
                fields.UnionWith(TrackDownReferences(field));
            }

            return fields;
        }

        private HashSet<FieldModel> TrackDownReferences(PointerReferenceModel pref)
        {
            var fields = new HashSet<FieldModel> {pref};

            fields.Add(pref.KeyId.CreateObject<KeyModel>());
            fields.UnionWith(TrackDownReferences(pref.ReferenceFieldModelId.CreateObject<FieldModel>()));

            return fields;
        }

        private HashSet<FieldModel> TrackDownReferences(DocumentReferenceModel dref)
        {
            var fields = new HashSet<FieldModel> {dref};

            fields.Add(dref.KeyId.CreateObject<KeyModel>());
            fields.UnionWith(TrackDownReferences(dref.DocumentId.CreateObject<FieldModel>()));

            return fields;
        }
    }
}
