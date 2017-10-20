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

        private async Task<IEnumerable<EntityBase>> GetDocumentAndProps(string documentId)
        {
            if (!_modelDictionary.ContainsKey(documentId))
            {
                return new List<EntityBase>();
            }

            var doc = _modelDictionary[documentId].CreateObject<DocumentModel>();

            var list = new List<EntityBase>() {doc};

            async Task func(RestRequestReturnArgs args)
            {
                list.AddRange(args.ReturnedObjects);
            }

            await _keys.GetDocuments(doc.Fields.Keys, func, e => throw e);
            await _fields.GetDocuments(doc.Fields.Values, func, e => throw e);

            return list;
        }

        private async Task<IEnumerable<EntityBase>> GetFieldModels(IEnumerable<string> fieldModelIds)
        {
            List<EntityBase> toReturn = new List<EntityBase>();
            async Task f(RestRequestReturnArgs arg)
            {
                toReturn.AddRange(arg.ReturnedObjects);
            }

            await _fields.GetDocuments(fieldModelIds, f, e => throw e);
            return toReturn;
        }

        private async Task RecursiveGetDocument(string documentId, Dictionary<string, EntityBase> dict)
        {
            var docAndProps = await GetDocumentAndProps(documentId);
            var first = (docAndProps).Where(e => !dict.ContainsKey(e.Id));
            foreach (var entity in first)
            {
                dict.Add(entity.Id, entity);
            }

            var pointerFields = first.OfType<PointerReferenceFieldModel>().Select(pr => pr.ReferenceFieldModelId).ToArray(); //field ids
            var documentFields = first.OfType<DocumentReferenceFieldModel>().Select(dr => dr.DocumentId).ToArray(); // document ids
            var documentCollectionFields = first.OfType<DocumentCollectionFieldModel>().SelectMany(dc => dc.Data).ToArray(); // document ids
            var documentFieldModels = first.OfType<DocumentFieldModel>().Select(df => df.Data).ToArray(); // document ids

            foreach (var entity in await GetFieldModels(pointerFields))
            {
                if (!dict.ContainsKey(entity.Id))
                {
                    dict.Add(entity.Id, entity);
                }
            }

            foreach (var docId in documentFields.Concat(documentCollectionFields).Concat(documentFieldModels))
            {
                await RecursiveGetDocument(docId, dict);
            }
        }

        private async Task<IEnumerable<EntityBase>> TrackDownReferences(DocumentModel model)
        {
            
            List<EntityBase> entities = new List<EntityBase>(_modelDictionary.Values.Select(i => i.CreateObject<DocumentModel>()));

            async Task ff(RestRequestReturnArgs arg)
            {
                entities.AddRange(arg.ReturnedObjects);
            }

            await _keys.GetDocumentsByQuery(new EverythingQuery<KeyModel>(), ff, null);
            await _fields.GetDocumentsByQuery(new EverythingQuery<FieldModel>(), ff, null);

            return entities;
            
            var dict = new Dictionary<string, EntityBase>();
            await RecursiveGetDocument(model.Id, dict);
            var vals =  dict.Values;
            return vals;
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

                var args = new RestRequestReturnArgs(list.Distinct().SelectMany(k => k));

                await success?.Invoke(args);
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
    }
}
