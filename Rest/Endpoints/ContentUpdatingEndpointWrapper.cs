using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using DashShared.Models;

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
            foreach (var model in models)
            {
                if (model is DocumentModel)
                {
                    ContentController<DocumentModel>.AddController(((DocumentModel)model).NewController());
                }
                else if (model is KeyModel)
                {
                    ContentController<KeyModel>.AddController(((KeyModel)model).NewController());
                }
                else if (model is FieldModel)
                {
                    ContentController<FieldModel>.AddController(((FieldModel)model).NewController());
                }
                else
                {
                    throw new Exception("unsupported Model Type");
                }
            }
        }

        private Func<RestRequestReturnArgs, Task> GetSuccessFunc(Func<RestRequestReturnArgs, Task> success)
        {
            async Task func(RestRequestReturnArgs arg)
            {
                AddModelsToControllers(arg.ReturnedObjects);
                await success(arg);
            }

            return func;
        }

        public void AddDocument(T newDocument, Action<T> success, Action<Exception> error)
        {
            AddModelsToControllers(new List<EntityBase>(){newDocument});
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
    }
}
