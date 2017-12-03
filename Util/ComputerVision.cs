using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;

namespace Dash
{
    /// <summary>
    /// Contains static helper methods for using the ProjectOxford computervision API.
    /// </summary>
    public class ComputerVision
    {
        private static VisionServiceClient _visionServiceClient = new VisionServiceClient("ae6f33d9feb54a11a065ffce65a922a1", "https://westcentralus.api.cognitive.microsoft.com/vision/v1.0");

        /// <summary>
        /// Given the file path to an image on the disk, returns the AnalysisResult which contains the generated tags and other properties.
        /// </summary>
        /// <param name="imageFilePath"></param>
        /// <returns>the file path</returns>
        public static async Task<AnalysisResult> UploadAndAnalyzeImage(string imageFilePath)
        {
            using (Stream imageFileStream = File.OpenRead(imageFilePath))
            {
                VisualFeature[] visualFeatures = new VisualFeature[] { VisualFeature.Adult, VisualFeature.Categories, VisualFeature.Color, VisualFeature.Description, VisualFeature.Faces, VisualFeature.ImageType, VisualFeature.Tags };
                AnalysisResult analysisResult = await _visionServiceClient.AnalyzeImageAsync(imageFileStream, visualFeatures);
                return analysisResult;
            }
        }

        /// <summary>
        /// Given the URL of an image, returns the AnalysisResult which contains the generated tags and other properties.
        /// </summary>
        /// <param name="imageUrl"></param>
        /// <returns>the image URL</returns>
        public static async Task<AnalysisResult> AnalyzeUrl(string imageUrl)
        {
            VisualFeature[] visualFeatures = new VisualFeature[] { VisualFeature.Adult, VisualFeature.Categories, VisualFeature.Color, VisualFeature.Description, VisualFeature.Faces, VisualFeature.ImageType, VisualFeature.Tags };
            AnalysisResult analysisResult = await _visionServiceClient.AnalyzeImageAsync(imageUrl, visualFeatures);
            return analysisResult;
        }
    }
}
