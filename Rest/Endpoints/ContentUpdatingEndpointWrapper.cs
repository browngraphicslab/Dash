using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using DashShared.Models;
using TypeInfo = DashShared.TypeInfo;

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
            var entityBases = models as IList<EntityBase> ?? models.ToList();
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
    }
}
