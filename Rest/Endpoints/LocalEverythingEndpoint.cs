using DashShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dash
{
    //public class LocalEverythingEndpoint : LocalModelEndpoint<FieldModel>
    //{
    //    public override async Task GetDocument(string id, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
    //    {
    //        try
    //        {
    //            var doc = GetModel(id);
    //            var args = new RestRequestReturnArgs()
    //            {
    //                ReturnedObjects = new List<EntityBase>(await TrackDownReferences(doc.CreateObject<FieldModel>()))
    //            };
    //            await success?.Invoke(args);
    //        }
    //        catch (Exception e)
    //        {
    //            error?.Invoke(e);
    //        }
    //    }

    //    private async Task<IEnumerable<EntityBase>> TrackDownReferences(FieldModel model)
    //    {

    //        List<EntityBase> entities =
    //            new List<EntityBase>(ModelDictionary.Values.Select(i => i.CreateObject<FieldModel>()));

    //        async Task ff(RestRequestReturnArgs arg)
    //        {
    //            entities.AddRange(arg.ReturnedObjects);
    //        }

    //        return entities;
    //    }


    //    public override async Task GetDocumentsByQuery(IQuery<FieldModel> query, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
    //    {
    //        try
    //        {
    //            var entities = ModelDictionary.Values.Select(i => i.CreateObject<FieldModel>()).Where(query.Func);

    //            var list = new List<IEnumerable<EntityBase>>();
    //            foreach (var doc in entities)
    //            {
    //                list.Add(await TrackDownReferences(doc));
    //            }

    //            var args = new RestRequestReturnArgs(list.Distinct().SelectMany(k => k));

    //            await success?.Invoke(args);
    //        }
    //        catch (Exception e)
    //        {
    //            error?.Invoke(e);
    //        }
    //    }

    //    public override async Task GetDocuments(IEnumerable<string> ids, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
    //    {
    //        try
    //        {
    //            var list = new List<EntityBase>();
    //            foreach (var id in ids)
    //            {
    //                var text = GetModel(id);
    //                var doc = text.CreateObject<FieldModel>();
    //                list.AddRange(await TrackDownReferences(doc));
    //            }
    //            await success?.Invoke(new RestRequestReturnArgs(list));
    //        }
    //        catch (Exception e)
    //        {
    //            error?.Invoke(e);
    //        }
    //    }
    //}
}
