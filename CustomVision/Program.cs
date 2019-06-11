using System;
using Microsoft.Azure;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.File;
using Csv;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;

namespace CustomVision
{
    class Program
    {
        static void Main(string[] args)
        {

            CloudFileShare share = AzureStorageHelpers.Setup();

            // Change the name of the project here
            string projectName = "Project-test";

            // Change the name of the model here
            string modelName = "testModel";

            // Change the location of the Resources folder here (relative to the executable)
            string resourcesFolder = "../../../Resources/";

            // If you have already created the project, change to false
            bool newProject = true;

            // If you have already published the project, change to false
            bool publishProject = true;

            // Upload mode for training can either be file or url
            string uploadModeTrain = "file";

            // Upload mode for prediction can either be file or url
            string uploadModePredict = "file";

            string tagsFile = "tags.csv";
            string datasetFile = "dataset.csv";
            string testImagesFolder = "testImages";

            // Change the URLs to test here
            List<string> testImages = new List<string>() { "https://i.imgur.com/RuN1ETS.jpg","https://i.imgur.com/uTAGS71.jpg", "https://i.imgur.com/zjhD9ot.jpg", "https://i.imgur.com/LpJFxdB.jpg"};

            Training.TrainingPipeline(projectName, modelName, resourcesFolder, tagsFile, datasetFile, newProject, publishProject, uploadModeTrain, share).Wait();
            Project project = Training.GetProject(projectName);
            Prediction.PredictionPipeline(project.Id, modelName, resourcesFolder, testImagesFolder, uploadModePredict, testImages).Wait();

            Console.ReadLine();
        }

    }
}
