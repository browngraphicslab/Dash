using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Dash
{
    class AnnotationController
    {
        public DocumentController ParentController;
        public IRegionData RegionData;

        public AnnotationController(DocumentController parent, IRegionData region)
        {
            ParentController = parent;
            RegionData = region;
        }
    }

    // interface for all region data: currently only filled out for the images, but can be extended to cover text/video/audio as ewll
    public interface IRegionData
    {
        object GetRegion();
    }

    // implementation of IRegionData for images specifically
    public class ImageRegionData : IRegionData
    {
        public ImageRegion Data;

        public ImageRegionData(ImageRegion data)
        {
            Data = data;
        }

        public object GetRegion()
        {
            return Data;
        }
    }

    // a struct to be able to deliver an images' topleft point and bottom-right point at the same time
    public struct ImageRegion
    {
        public Point TopLeftPoint, BottomRightPoint;

        public ImageRegion(Point tl, Point br)
        {
            TopLeftPoint = tl;
            BottomRightPoint = br;
        }
    }
}
