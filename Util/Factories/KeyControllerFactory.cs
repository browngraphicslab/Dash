using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Flurl.Util;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    public class KeyControllerFactory : BaseControllerFactory
    {
        public static KeyControllerBase CreateFromModel(KeyModel model)
        {
            return null;
        }
    }
}
