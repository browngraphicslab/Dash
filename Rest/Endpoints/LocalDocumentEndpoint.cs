using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using DashShared;
using DashShared.Models;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Dash
{
    public class LocalDocumentEndpoint : LocalModelEndpoint<DocumentModel>
    {
        private IModelEndpoint<KeyModel> _keys = App.Instance.Container.GetRequiredService<IModelEndpoint<KeyModel>>();

        private IModelEndpoint<FieldModel> _fields = App.Instance.Container.GetRequiredService<IModelEndpoint<FieldModel>>();
        public override async Task GetDocument(string id, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
        {
            try
            {
                var doc = _modelDictionary[id];
                var args = new RestRequestReturnArgs()
                {
                    ReturnedObjects = new List<EntityBase>(await TrackDownReferences(doc.CreateObject<DocumentModel>()))
                };
                await success?.Invoke(args);
            }
            catch (Exception e)
            {
                error?.Invoke(e);
            }
        }

        private Dictionary<string, DocumentModel> models
        {
            get { return _modelDictionary.ToDictionary(k => k.Key, v => v.Value.CreateObject<DocumentModel>()); }
        }

        public override void AddDocument(DocumentModel newDocument, Action<DocumentModel> success, Action<Exception> error)
        {
            base.AddDocument(newDocument, success, error);
        }

        private async Task<IEnumerable<EntityBase>> TrackDownReferences(DocumentModel model, IEnumerable<string> idsToIgnore = null)
        {
            idsToIgnore = idsToIgnore ?? new List<string>();

            Dictionary<string,EntityBase> map = new Dictionary<string, EntityBase> { {model.Id, model} };

            void error(Exception e)
            {
                Debug.WriteLine(e);
            }


            List<EntityBase> entities = new List<EntityBase>(_modelDictionary.Values.Select( i => i.CreateObject<DocumentModel>()));

            async Task ff(RestRequestReturnArgs arg)
            {
                entities.AddRange(arg.ReturnedObjects);
            }

            await _keys.GetDocumentsByQuery(new EverythingQuery<KeyModel>(), ff, null);
            await _fields.GetDocumentsByQuery(new EverythingQuery<FieldModel>(), ff, null);

            return entities;


            // Declare a local function.
            async Task func(RestRequestReturnArgs arg)
            {
                var objs = arg.ReturnedObjects.Where(i => !map.ContainsKey(i.Id)).ToArray();

                idsToIgnore = idsToIgnore.Concat(map.Keys).ToArray();

                foreach (var obj in objs)
                {
                    map.Add(obj.Id, obj);
                } 

                Debug.WriteLine(string.Join(", ", objs.Select( k => k.Id)));

                var pointerFields = objs.OfType<PointerReferenceFieldModel>().ToArray();
                var documentFields = objs.OfType<DocumentReferenceFieldModel>().ToArray();
                var documentCollectionFields = objs.OfType<DocumentCollectionFieldModel>().ToArray();
                var documentFieldModels = objs.OfType<DocumentFieldModel>().ToArray();

                if (pointerFields.Any())
                {
                    await _fields.GetDocuments(pointerFields.Select(f => f.ReferenceFieldModelId).Except(idsToIgnore).ToArray(), func, error);
                    await _keys.GetDocuments(pointerFields.Select(f => f.KeyId).Except(idsToIgnore).ToArray(), func, error);
                }

                if (documentFields.Any())
                {
                    await GetDocumentsExcept(documentFields.Select(f => f.DocumentId).Except(idsToIgnore).ToArray(), func, error, map.Keys);
                    await _keys.GetDocuments(documentFields.Select(f => f.KeyId).Except(idsToIgnore).ToArray(), func, error);
                }



                if (documentCollectionFields.Any())
                {
                    await GetDocumentsExcept(documentCollectionFields.SelectMany(i => i.Data).Distinct().Except(idsToIgnore).ToArray(), func, error, map.Keys);
                }

                if (documentFieldModels.Any())
                {
                    await GetDocumentsExcept(documentFieldModels.Select(i => i.Data).Distinct().Except(idsToIgnore).ToArray(), func, error, map.Keys);
                }
            }

            await _keys.GetDocuments(model.Fields.Keys.Except(idsToIgnore).ToArray(), func, error);
            await _fields.GetDocuments(model.Fields.Values.Except(idsToIgnore).ToArray(), func, error);

            return map.Values;
        }
        /*
        private class DocumentComparer : IEqualityComparer<EntityBase>
        {
            public bool Equals(EntityBase x, EntityBase y)
            {
                return x.Id == y.Id;
            }

            public int GetHashCode(EntityBase obj)
            {
                return obj.Id.GetHashCode();
            }
        }*/


        public override async Task GetDocumentsByQuery(IQuery<DocumentModel> query, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
        {
            try
            {
                var entities = _modelDictionary.Values.Select(i => i.CreateObject<DocumentModel>()).Where(query.Func);

                var list = new List<IEnumerable<EntityBase>>();
                foreach (var doc in entities)
                {
                    list.Add(await TrackDownReferences(doc));
                }

                await success?.Invoke(new RestRequestReturnArgs(list.Distinct().SelectMany(k => k)));
            }
            catch (Exception e)
            {
                error?.Invoke(e);
            }
        }

        public override async Task GetDocuments(IEnumerable<string> ids, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
        {
            try
            {
                var list = new List<EntityBase>();
                foreach (var id in ids)
                {
                    var text = _modelDictionary[id];
                    var doc = text.CreateObject<DocumentModel>();
                    list.AddRange(await TrackDownReferences(doc));
                }
                await success?.Invoke(new RestRequestReturnArgs(list));
            }
            catch (Exception e)
            {
                error?.Invoke(e);
            }
        }

        private async Task GetDocumentsExcept(IEnumerable<string> ids, Func<RestRequestReturnArgs, Task> success, Action<Exception> error, IEnumerable<string> idsToIgnore)
        {
            try
            {
                var list = new List<EntityBase>();
                foreach (var id in ids)
                {
                    var text = _modelDictionary[id];
                    var doc = text.CreateObject<DocumentModel>();
                    list.AddRange(await TrackDownReferences(doc, idsToIgnore));
                }
                await success?.Invoke(new RestRequestReturnArgs(list));
            }
            catch (Exception e)
            {
                error?.Invoke(e);
            }
        }
    }
}
