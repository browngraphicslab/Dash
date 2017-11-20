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
    public class ComputerVision
    {
        public static async Task<AnalysisResult> UploadAndAnalyzeImage(string imageFilePath)
        {
            VisionServiceClient VisionServiceClient = new VisionServiceClient("ae6f33d9feb54a11a065ffce65a922a1", "https://westcentralus.api.cognitive.microsoft.com/vision/v1.0");

            using (Stream imageFileStream = File.OpenRead(imageFilePath))
            {
                VisualFeature[] visualFeatures = new VisualFeature[] { VisualFeature.Adult, VisualFeature.Categories, VisualFeature.Color, VisualFeature.Description, VisualFeature.Faces, VisualFeature.ImageType, VisualFeature.Tags };
                AnalysisResult analysisResult = await VisionServiceClient.AnalyzeImageAsync(imageFileStream, visualFeatures);
                return analysisResult;
            }
        }
    }
}
