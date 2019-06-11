using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CustomVision
{
    public class Prediction
    {
        // Replace your prediction key here
        private readonly static string predictionKey = "YOUR_PREDICTION_KEY";
        // Change the endpoint to your region if necessary
        private readonly static string predictionEndpoint = "https://westeurope.api.cognitive.microsoft.com";

        private static readonly CustomVisionPredictionClient endpoint = new CustomVisionPredictionClient()
        {
            ApiKey = predictionKey,
            Endpoint = predictionEndpoint
        };

        public static async Task PredictionPipeline(Guid projectID, string modelName, string resourcesPath, string testImagesFolder, string mode = "url", List<string> imagesList = null)
        {
            Console.WriteLine($"Predicting results for image...");
            ImagePrediction result = null;
            if (mode == "url") {
                foreach(string url in imagesList)
                {
                    result = await PredictImageURL(projectID,modelName,url);
                }
            }
            else
            {
                try
                {
                    string path = Path.GetFullPath(resourcesPath + testImagesFolder);
                    var files = Directory.EnumerateFiles(path);
                    foreach(string file in files)
                    {
                        result = await PredictImageFile(projectID, modelName, file);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\n{e.GetType().Name}: {e.Message} \nCould not retrieve test images.");
                }

            }


            
        }

        public static async Task<ImagePrediction> PredictImageURL(Guid projectID, string modelName, string url)
        {
            ImageUrl imageUrl = new ImageUrl(url);

            ImagePrediction result = null;

            try
            {
                result = await endpoint.ClassifyImageUrlAsync(projectID, modelName, imageUrl);
                Console.WriteLine($"\nSuccessfully retrieved predictions for image '{url}'.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n{e.GetType().Name}: {e.Message} \nCould not get prediction for image '{url}'.");
            }

            // Loop over each prediction and write out the results
            if (result != null)
            {
                foreach (var c in result.Predictions)
                {
                    Console.WriteLine($"\t{c.TagName}: {c.Probability:P1}");
                }
            }

            return result;
        }

        public static async Task<ImagePrediction> PredictImageFile(Guid projectID, string modelName, string file)
        {
            var img = new MemoryStream(File.ReadAllBytes(file));

            ImagePrediction result = null;

            try
            {
                result = await endpoint.ClassifyImageAsync(projectID, modelName, img);
                Console.WriteLine($"\nSuccessfully retrieved predictions for image '{file}'.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n{e.GetType().Name}: {e.Message} \nCould not get prediction for image '{file}'.");
            }

            // Loop over each prediction and write out the results
            if (result != null)
            {
                foreach (var c in result.Predictions)
                {
                    Console.WriteLine($"\t{c.TagName}: {c.Probability:P1}");
                }
            }

            return result;
        }


    }
}
