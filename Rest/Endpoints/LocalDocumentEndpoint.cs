using System;
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
                await success(args);
            }
            catch (Exception e)
            {
                error(e);
            }
        }

        private async Task<IEnumerable<EntityBase>> TrackDownReferences(DocumentModel model)
        {
            List<EntityBase> list = new List<EntityBase>(){model};

            Func<RestRequestReturnArgs, Task> func = async (arg) =>
            {
                var objs = arg.ReturnedObjects.ToList();

                var refFields = objs.OfType<ReferenceFieldModel>().ToArray();
                var docFields = objs.OfType<DocumentFieldModel>().ToArray();

                foreach (var field in refFields)
                {
                    objs.Add(field.Reference)
                }

                list.AddRange();
            };

            await _keys.GetDocuments(model.Fields.Keys, func, null);
            await _fields.GetDocuments(model.Fields.Values, func, null);
           
            return list;
        }

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

                await success(new RestRequestReturnArgs(list.SelectMany(k => k)));
            }
            catch (Exception e)
            {
                error(e);
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
                await success(new RestRequestReturnArgs(list));
            }
            catch (Exception e)
            {
                error(e);
            }
        }
    }
}
