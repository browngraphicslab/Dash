using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class KeyStore
    {
        public static KeyControllerBase LayoutListKey = new KeyControllerBase(DashConstants.KeyStore.LayoutListKey);
        public static KeyControllerBase ActiveLayoutKey = new KeyControllerBase(DashConstants.KeyStore.ActiveLayoutKey);
        public static KeyControllerBase PrimaryKeyKey = new KeyControllerBase(DashConstants.KeyStore.PrimaryKeyKey);
        public static KeyControllerBase ThisKey = new KeyControllerBase(DashConstants.KeyStore.ThisKey);
        public static KeyControllerBase PrototypeKey = new KeyControllerBase(DashConstants.KeyStore.PrototypeKey);
        public static KeyControllerBase DelegatesKey = new KeyControllerBase(DashConstants.KeyStore.DelegatesKey);
        public static KeyControllerBase WidthFieldKey = new KeyControllerBase(DashConstants.KeyStore.WidthFieldKey);
        public static KeyControllerBase HeightFieldKey = new KeyControllerBase(DashConstants.KeyStore.HeightFieldKey);
        public static KeyControllerBase DataKey = new KeyControllerBase(DashConstants.KeyStore.DataKey);
        public static KeyControllerBase PositionFieldKey = new KeyControllerBase(DashConstants.KeyStore.PositionFieldKey);
        public static KeyControllerBase ScaleCenterFieldKey = new KeyControllerBase(DashConstants.KeyStore.ScaleCenterFieldKey);
        public static KeyControllerBase ScaleAmountFieldKey = new KeyControllerBase(DashConstants.KeyStore.ScaleAmountFieldKey);
        public static KeyControllerBase IconTypeFieldKey = new KeyControllerBase(DashConstants.KeyStore.IconTypeFieldKey);
    }
}
