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

        private async Task AddModelsToControllers(IEnumerable<EntityBase> models)
        {
            HashSet<FieldModel> entities = new HashSet<FieldModel>();//If not everything is a field model this should be changed
            foreach (var entityBase in models.Cast<FieldModel>())
            {
                entities.Add(entityBase);
                if (entityBase is FieldModel fieldModel)
                {
                    await TrackDownReferences(fieldModel, entities);
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
                await AddModelsToControllers(arg.ReturnedObjects);
                await success?.Invoke(arg);
            }

            return func;
        }

        private Func<RestRequestReturnArgs, Task> GetSuccessFunc<V>(Func<IEnumerable<V>, Task> success) where V:EntityBase
        {
            async Task func(RestRequestReturnArgs arg)
            {
                await AddModelsToControllers(arg.ReturnedObjects);
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

        protected async Task TrackDownReferences(FieldModel field, HashSet<FieldModel> fields)
        {
            fields.Add(field);
            switch (field)
            {
                case DocumentModel doc:
                    await TrackDownReferences(doc, fields);
                    break;
                case ListModel list:
                    await TrackDownReferences(list, fields);
                    break;
                case DocumentReferenceModel dref:
                    await TrackDownReferences(dref, fields);
                    break;
                case PointerReferenceModel pref:
                    await TrackDownReferences(pref, fields);
                    break;
            }
        }

        private async Task AddReferences(HashSet<FieldModel> fields, IEnumerable<string> ids)
        {
            await _endpoint.GetDocuments(ids, async (args) => {
                var results = args.ReturnedObjects.Cast<FieldModel>().ToList();//Even if there are other types of Entity bases, they should never be in a document if they aren't field models
                foreach (var res in results)
                {
                    if (fields.Contains(res)) continue;
                        await TrackDownReferences(res, fields);
                }
            },
            ex => throw ex);
        }

        private async Task TrackDownReferences(DocumentModel doc, HashSet<FieldModel> fields)
        {
            var subFields = new List<string>();
            subFields.AddRange(doc.Fields.Keys);
            subFields.AddRange(doc.Fields.Values);

            await AddReferences(fields, subFields);
        }

        private async Task TrackDownReferences(ListModel list, HashSet<FieldModel> fields)
        {
            await AddReferences(fields, list.Data);
        }

        private async Task TrackDownReferences(PointerReferenceModel pref, HashSet<FieldModel> fields)
        {
            await AddReferences(fields, new [] {pref.KeyId, pref.ReferenceFieldModelId});
        }

        private async Task TrackDownReferences(DocumentReferenceModel dref, HashSet<FieldModel> fields)
        {
            await AddReferences(fields, new[] {dref.KeyId, dref.DocumentId});
        }
    }
}
