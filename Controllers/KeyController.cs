using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    //Abstract class from "KeyController<T>" should inherit.
    public class KeyController : IController<KeyModel>
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
        public KeyController(string guid) : this(new KeyModel(guid))
        {
        }

        public KeyController(string guid, string name) : this(new KeyModel(guid, name))
        {
        }

        public KeyController() : this(new KeyModel())
        {
        }

        public KeyController(KeyModel model) : base(model)
        {

        }
    }
}
