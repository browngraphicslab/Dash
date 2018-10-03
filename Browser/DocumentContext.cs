using System;
using System.IO;
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

        public BitmapImage GetImage()
        {
            // TODO this is such a hack if it stops working its might be cause we stopped saving all images with .jpg cause that was insane to begin with
            var uriSource = new Uri(ApplicationData.Current.LocalFolder.Path + "\\" + ImageId + ".jpg");
            //Debug.Assert(File.Exists(uriSource.LocalPath), "the webcontext either didn't save or the path is incorrect");
            if (!File.Exists(uriSource.LocalPath))
            {
                return null;
            }
            return new BitmapImage(uriSource);
        }

        //public async Task<DocumentController> GetImage()
        //{
        //    var util = new ImageToDashUtil();
        //    var path = ApplicationData.Current.LocalFolder.Path;
        //    var uri = new Uri(path + ImageId + ".jpg");

        //    var controller = await util.ParseFileAsync(new FileData()
        //    {

        //        File = await StorageFile.GetFileFromPathAsync(uri.AbsolutePath),
        //        FileUri = uri,
        //        Filetype = FileType.Image
        //    });

        //    return controller;
        //}
    }
}
