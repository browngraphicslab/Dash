using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using DashShared;
using Windows.UI.Xaml.Media.Imaging;

namespace Dash
{
    public class DocumentContext : EntityBase
    {
        public double Scroll { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public double ViewDuration { get; set; }
        public long CreationTimeTicks { get; set; }
        public string ImageId { get; set; }

        public DateTime CreationTimeStamp => new DateTime(CreationTimeTicks);

        public override bool Equals(object obj)
        {
            if (obj is DocumentContext dc)
            {
                return dc.Url == Url &&
                       dc.Scroll == Scroll &&
                       dc.Title == Title;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public async Task<DocumentController> GetImage()
        {
            var util = new ImageToDashUtil();
            var path = ApplicationData.Current.LocalFolder.Path;
            var uri = new Uri(path + ImageId + ".jpg");

            var controller = await util.ParseFileAsync(new FileData()
            {

                File = await StorageFile.GetFileFromPathAsync(uri.AbsolutePath),
                FileUri = uri,
                Filetype = FileType.Image
            });

            return controller;
        }
    }
}
