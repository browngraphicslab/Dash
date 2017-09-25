using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    //Abstract class from "KeyController<T>" should inherit.
    public class KeyControllerBase : IController<KeyModel>
    {

        public string Name
        {
            get => Model.Name;
            set
            {
                Model.Name = value;
                RESTClient.Instance.Keys.UpdateKey(Model, model =>
                {
                    // Yay!
                }, exception =>
                {
                    // Hayyyyy!
                });
            }
        }
        public KeyControllerBase(string guid) : this(new KeyModel(guid))
        {
        }

        public KeyControllerBase(string guid, string name) : this(new KeyModel(guid, name))
        {
        }

        public KeyControllerBase() : this(new KeyModel())
        {
        }

        public KeyControllerBase(KeyModel model) : base(model)
        {

        }
    }
}
