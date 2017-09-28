using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    class KeyStore
    {
        public static KeyController DocumentContextKey = new KeyController(DashConstants.KeyStore.DocumentContextKey);
        public static KeyController AbstractInterfaceKey = new KeyController(DashConstants.KeyStore.AbstractInterfaceKey);
        public static KeyController LayoutListKey = new KeyController(DashConstants.KeyStore.LayoutListKey);
        public static KeyController ActiveLayoutKey = new KeyController(DashConstants.KeyStore.ActiveLayoutKey);
        public static KeyController TitleKey = new KeyController(DashConstants.KeyStore.TitleKey);
        public static KeyController PrimaryKeyKey = new KeyController(DashConstants.KeyStore.PrimaryKeyKey);
        public static KeyController ThisKey = new KeyController(DashConstants.KeyStore.ThisKey);
        public static KeyController PrototypeKey = new KeyController(DashConstants.KeyStore.PrototypeKey);
        public static KeyController DelegatesKey = new KeyController(DashConstants.KeyStore.DelegatesKey);
        public static KeyController WidthFieldKey = new KeyController(DashConstants.KeyStore.WidthFieldKey);
        public static KeyController HeightFieldKey = new KeyController(DashConstants.KeyStore.HeightFieldKey);
        public static KeyController DataKey = new KeyController(DashConstants.KeyStore.DataKey);
        public static KeyController PositionFieldKey = new KeyController(DashConstants.KeyStore.PositionFieldKey);
        public static KeyController ScaleCenterFieldKey = new KeyController(DashConstants.KeyStore.ScaleCenterFieldKey);
        public static KeyController ScaleAmountFieldKey = new KeyController(DashConstants.KeyStore.ScaleAmountFieldKey);
        public static KeyController IconTypeFieldKey = new KeyController(DashConstants.KeyStore.IconTypeFieldKey);
        public static KeyController SystemUriKey = new KeyController(DashConstants.KeyStore.SystemUriKey);
        public static KeyController ThumbnailFieldKey = new KeyController(DashConstants.KeyStore.ThumbnailFieldKey);
    }
}
