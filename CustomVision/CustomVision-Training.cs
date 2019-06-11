using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Csv;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using Microsoft.Azure.Storage.File;

namespace CustomVision
{
    public class Img
    {
        public Uri url = null;
        public string filepath = null;
        public List<Tag> tags = new List<Tag>();
    }

    public class Training
    {
        // Replace your training key here
        private readonly static string trainingKey = "YOUR_TRAINING_KEY";
        // Change the endpoint to your region if necessary
        private readonly static string endpoint = "https://westeurope.api.cognitive.microsoft.com";
        // Replace your prediction resource ID here
        private readonly static string predictionResourceID = "YOUR_PREDICTION_RESOURCE_ID";

        private static readonly CustomVisionTrainingClient trainingApi = new CustomVisionTrainingClient()
        {
            ApiKey = trainingKey,
            Endpoint = endpoint
        };

        public static async Task TrainingPipeline(string projectName, string modelName, string resourcesPath, string tagsDataset, string imagesDataset, bool create = true, bool publish = true, string mode = "url", CloudFileShare share = null)
        {
            Project project = await ProjectSetupPipeline(projectName, resourcesPath, tagsDataset, create);

            if (create)
            {
                List<Img> images = await GetImagesFromDataset(project, resourcesPath, imagesDataset, share, mode);
                await UploadAndTagImages(project, images, mode);
            }

            if (publish)
            {
                Iteration iteration = TrainProject(project);
                PublishProject(project, modelName, iteration);
            }
            
        }

        public static async Task<Project> ProjectSetupPipeline(string projectName, string resourcesPath, string tagsDataset, bool create)
        {

            Project project = null;
            try
            {
                project = GetProject(projectName);
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n{e.GetType().Name}: Getting the Custom Vision project failed.");
            }

            // If create is true, we create tags for the project
            if (create)
            {
                List<string> tags = null;
                try
                {
                    tags = GetTagsFromDataset(resourcesPath, tagsDataset);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\n{e.GetType().Name}: Could not get tags.");
                }

                if (project != null && tags != null)
                {
                    List<Tag> projectTags = null;
                    try
                    {
                        projectTags = await CreateOrGetTagsFromList(project, tags);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"\n{e.GetType().Name}: Could not create tags for the project.");
                    }

                    if (projectTags != null)
                    {

                    }
                }
            }

            return project;
        }

        public static Project GetProject(string projectName)
        {
            Console.WriteLine("\nGetting project...");
            Project project = null;

            try
            {
                IList<Project> projects = trainingApi.GetProjects();
                foreach (Project p in projects)
                {
                    if (p.Name == projectName)
                    {
                        project = trainingApi.GetProject(p.Id);
                        Console.WriteLine($"\nProject '{projectName}' was found.");
                        return project;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n{e.GetType().Name}: Error getting projects.");
                throw;
            }

            // If a project with this name doesn't exist, a new project is created
            Console.WriteLine("\nCreating new project...");
            try
            {
                project = trainingApi.CreateProject(projectName);
                Console.WriteLine($"New Custom Vision project '{projectName}' was created.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n{e.GetType().Name}: Error creating new Custom Vision project.");
                throw;
            }
            
            return project;
        } 

        public static Iteration TrainProject(Project project)
        {
            Console.WriteLine("\nTraining model...");
            Iteration iteration = null;
            try
            {
                iteration = trainingApi.TrainProject(project.Id);
                while (iteration.Status == "Training")
                {
                    Thread.Sleep(1000);
                    iteration = trainingApi.GetIteration(project.Id, iteration.Id);
                    Console.WriteLine($"\nIteration status: {iteration.Status}");
                }
                Console.WriteLine($"\nSuccessfully trained Custom Vision model.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n{e.GetType().Name}: Could not train Custom Vision model.");
            }

            return iteration;
        }

        public static void PublishProject(Project project, string modelName, Iteration iteration = null)
        {
            Console.WriteLine("\nPublishing...");

            if (iteration == null)
            {
                IList<Iteration> iterations = trainingApi.GetIterations(project.Id);
                iteration = iterations[iterations.Count - 1];
            }

            try
            {
                trainingApi.PublishIteration(project.Id, iteration.Id, modelName, predictionResourceID);
                Console.WriteLine($"\nSuccessfully published trained model to Custom Vision project.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n{e.GetType().Name}: Could not publish trained model to Custom Vision project.");
            }
        }

        public static async Task<List<Tag>> CreateOrGetTagsFromList(Project project, List<string> tags)
        {
            List<Tag> projectTags = new List<Tag>();
            try
            {
                foreach (string t in tags)
                {
                        Tag tag = await CreateTag(project, t);
                        projectTags.Add(tag);
                }
                Console.WriteLine($"\n{projectTags.Count} tags successfully created or retrieved.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n{e.GetType().Name}: Error creating tags for the Custom Vision project.");
                throw;
            }

            return projectTags;
        }

        public static async Task<Tag> CreateTag(Project project, string tag)
        {
            Tag projectTag = null;
            try
            {
                projectTag = await trainingApi.CreateTagAsync(project.Id, tag);         
                Console.WriteLine($"\nTag '{tag}' successfully created.");
            }
            catch
            {
                try
                {
                    projectTag = await GetTag(project, tag);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\n{e.GetType().Name}: Error creating tag for the Custom Vision project.");
                    throw;
                }
            }

            return projectTag;
        }

        public static async Task<Tag> GetTag(Project project, string tag)
        {
            IList<Tag> projectTags = null;
            Tag projectTag = null;
            try
            {
                projectTags = await trainingApi.GetTagsAsync(project.Id);
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n{e.GetType().Name}: Error retrieving tags list from Custom Vision project.");
                throw;
            }

            if (projectTags != null)
            {
                foreach(Tag t in projectTags)
                {
                    if(t.Name == tag)
                    {
                        try
                        {
                            projectTag = await trainingApi.GetTagAsync(project.Id, t.Id);
                            return projectTag;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"\n{e.GetType().Name}: Error retrieving tag '{tag}' from Custom Vision project.");
                            throw;
                        }
                    }
                }
                Console.WriteLine($"\nNo tag '{tag}' was found in the Custom Vision project.");
            }

            return projectTag;
        }

        public static async Task UploadAndTagImages(Project project,List<Img> images, string mode = "url")
        {
            if (mode == "url")
            {
                var imageUrls = new List<ImageUrlCreateEntry>();
                foreach (Img img in images)
                {
                    imageUrls.Add(GetImageUrlEntry(img.url.ToString(), img.tags));
                }
                try
                {
                    int total = imageUrls.Count;
                    int batches = (int)Math.Floor(total / 64f) + 1;
                    for (int i = 0; i < batches - 1; i++)
                    {
                        int indexStart = i * 64;
                        var currentBatch = imageUrls.GetRange(indexStart, 64);
                        await trainingApi.CreateImagesFromUrlsAsync(project.Id, new ImageUrlCreateBatch(currentBatch));
                        Console.WriteLine($"\nImages successfully uploaded from urls in batch.");
                    }
                    if ((batches - 1) * 64 < total)
                    {
                        int count = total - (batches - 1) * 64;
                        var currentBatch = imageUrls.GetRange((batches - 1) * 64, count);
                        await trainingApi.CreateImagesFromUrlsAsync(project.Id, new ImageUrlCreateBatch(currentBatch));
                        Console.WriteLine($"\nImages successfully uploaded from urls in batch.");
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine($"\n{e.GetType().Name}: Could not upload images in batch from urls.");
                }
            }
            else
            {
                var imageFiles = new List<ImageFileCreateEntry>();
                foreach (Img img in images)
                {
                    imageFiles.Add(GetImageFileEntry(img.filepath, img.tags));
                }
                try
                {
                    
                    int total = imageFiles.Count;
                    int batches = (int)Math.Floor(total / 64f) + 1;
                    for(int i=0; i < batches - 1; i++)
                    {
                        int indexStart = i * 64;
                        var currentBatch = imageFiles.GetRange(indexStart, 64);
                        await trainingApi.CreateImagesFromFilesAsync(project.Id, new ImageFileCreateBatch(currentBatch));
                        Console.WriteLine($"\nImages successfully uploaded from files in batch.");
                    }
                    if((batches-1)*64 < total)
                    {
                        int count = total - (batches-1) * 64;
                        var currentBatch = imageFiles.GetRange((batches-1)*64, count);
                        await trainingApi.CreateImagesFromFilesAsync(project.Id, new ImageFileCreateBatch(currentBatch));
                        Console.WriteLine($"\nImages successfully uploaded from files in batch.");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\n{e.GetType().Name}: Could not upload images in batch from files.");
                }
            }
        }

        public static ImageUrlCreateEntry GetImageUrlEntry(string url, List<Tag> tags)
        {
            List<Guid> tagsIDs = new List<Guid>();
            ImageUrlCreateEntry imageUrl;

            foreach (Tag t in tags)
            {
                tagsIDs.Add(t.Id);
            }

            try
            {
                imageUrl = new ImageUrlCreateEntry(url, tagsIDs);
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n{e.GetType().Name}: Could not create image url entry for image '{url}'.");
                throw;
            }

            return imageUrl;
        }

        public static ImageFileCreateEntry GetImageFileEntry(string filepath, List<Tag> tags)
        {
            List<Guid> tagsIDs = new List<Guid>();
            ImageFileCreateEntry imageFile = null;

            foreach (Tag t in tags)
            {
                tagsIDs.Add(t.Id);
            }

            try
            {
                Console.WriteLine($"\nCreate image entry for file '{Path.GetFileName(filepath)}' located at '{filepath}'.");
                imageFile = new ImageFileCreateEntry(Path.GetFileName(filepath), File.ReadAllBytes(filepath), tagsIDs);
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n{e.GetType().Name}: Could not create image file entry for image '{filepath}'.");
            }

            return imageFile;
        }

        private static List<string> GetTagsFromDataset(string resourcesPath, string tagsDataset)
        {
            List<string> tags = new List<string>();

            IEnumerable<ICsvLine> dataset = null;

            try
            {
                string path = Path.GetFullPath(resourcesPath+tagsDataset);
                Console.WriteLine($"\nTags dataset: '{path}'");
                dataset = LoadDataset(path);
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n{e.GetType().Name}: Error loading tags dataset.");
                throw;
            }

            if (dataset != null)
            {
                try
                {
                    foreach (var line in dataset)
                    {
                        tags.Add(line[0]);
                    }
                    Console.WriteLine($"\nTags list created: {tags.Count} tags found.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\n{e.GetType().Name}: Error getting tags.");
                    throw;
                }

            }
            
            return tags;
        }

        private static async Task<List<Img>> GetImagesFromDataset(Project project, string resourcesPath, string imagesDataset, CloudFileShare share = null, string mode = "url")
        {
            List<Img> images = new List<Img>();

            IEnumerable<ICsvLine> dataset = null;

            try
            {
                string path = Path.GetFullPath(resourcesPath + imagesDataset);
                Console.WriteLine($"\nImages dataset: '{path}'");
                dataset = LoadDataset(path);
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n{e.GetType().Name}: Error loading image dataset.");
            }

            if (dataset != null)
            {
                try
                {
                    foreach (var line in dataset)
                    {
                        Img img = null;
                        if (mode == "url")
                        {
                            if (share == null)
                            {
                                Console.WriteLine($"\nNo file share was provided, upload images using files instead.");
                            }
                            else
                            {
                                img = await GetImageFromURL(project, line, share);
                            }
                        }
                        else
                        {
                            img = await GetImageFromFile(project, line, resourcesPath);
                        }
                        images.Add(img);
                    }
                    Console.WriteLine($"\nImage dataset created: {images.Count} images found.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\n{e.GetType().Name}: Error getting images.");
                }

            }
            

            return images;
        }

        private static async Task<Img> GetImageFromURL(Project project, ICsvLine line, CloudFileShare share)
        {

            Img img = new Img();
            string url = "";
            string tags = "";

            try
            {
                url = line["sourcefile"];
                img.url = AzureStorageHelpers.GetFileURL(url, share);
                tags = line["tags"];
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n{e.GetType().Name}: Could not get source file name and tags from csv file.");
                throw;
            }

            string[] tagsList = tags.Split(',');

            foreach (string t in tagsList)
            {
                try
                {
                    Tag tag = await GetTag(project, t);
                    img.tags.Add(tag);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\n{e.GetType().Name}: Could not get tag from project.");
                    throw;
                }
            }

            return img;

        }

        private static async Task<Img> GetImageFromFile(Project project, ICsvLine line, string resourcesPath)
        {

            Img img = new Img();
            string imgpath = "";
            string tags = "";

            try
            {
                imgpath = line["sourcefile"];
                img.filepath =  Path.GetFullPath(resourcesPath+"/trainingImages/"+imgpath);
                tags = line["tags"];
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n{e.GetType().Name}: Could not get source file name and tags from csv file.");
                throw;
            }

            string[] tagsList = tags.Split(',');

            foreach (string t in tagsList)
            {
                try
                {
                    Tag tag = await GetTag(project, t);
                    img.tags.Add(tag);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\n{e.GetType().Name}: Could not get tag from project.");
                    throw;
                }
            }

            return img;

        }

        private static IEnumerable<ICsvLine> LoadDataset(string filepath)
        {
            
            string csv = null;
            IEnumerable<ICsvLine> dataset = null;

            try
            {
                csv = File.ReadAllText(filepath);
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n{e.GetType().Name}: Could not read file.");
                throw;
            }

            if (csv != null)
            {
                try
                {
                    dataset = CsvReader.ReadFromText(csv);
                    Console.WriteLine($"\nCsv data retrieved from file.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\n{e.GetType().Name}: Could not read csv data from file.");
                    throw;
                }
               
            }

            return dataset;
        }


    }
}
