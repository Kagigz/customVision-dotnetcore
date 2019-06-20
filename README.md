# Custom Vision Service .NET core console app (.NET SDK)

Azure Custom Vision Service client console app in .NET core (using .NET SDK)


## How to use

- Download or clone this repository
- Replace the prediction key with your own in *CustomVision-Prediction.cs*
- Replace the training key and prediction resource ID with your own in *CustomVision-Training.cs*
- If you plan on using images stored in Azure Storage, replace the account name, account key, file share name and sas token with your own in *AzureStorageHelpers.cs*

## How it works

With the default settings, a new Custom Vision project is created, all the tags in the dataset are added to the project, all the images in the dataset are uploaded, and the project is trained and published.
You can change the settings in *Program.cs*
