using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using System.Linq;
using Chess_Club.Models;
using Windows.Storage;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;

namespace Chess_Club.DAL {
    public class ChessClubContext {
        // Variables for use in each context
        /// <summary>
        /// Stores the model repositories
        /// </summary>
        private readonly List<IModelRepository> modelRepositories = new List<IModelRepository>();
        /// <summary>
        /// Stores all games in the system
        /// </summary>
        public List<Game> Games = new List<Game>();
        /// <summary>
        /// Stores all members in the system
        /// </summary>
        public List<Member> Members = new List<Member>();
        /// <summary>
        /// Stores all sessions in the system
        /// </summary>
        public List<Session> Sessions = new List<Session>();
        /// <summary>
        /// Stores all tournaments in the system
        /// </summary>
        public List<Tournament> Tournaments = new List<Tournament>();
        /// <summary>
        /// Stores all tournament rounds in the system
        /// </summary>
        public List<TournamentRound> TournamentRounds = new List<TournamentRound>();
        
        /// <summary>
        /// Constructor; create all repositories ready for population. One each for games, members, sessions, etc.
        /// </summary>
        public ChessClubContext() {
            modelRepositories.Add(CreateRepository(() => Games));
            modelRepositories.Add(CreateRepository(() => Members));
            modelRepositories.Add(CreateRepository(() => Sessions));
            modelRepositories.Add(CreateRepository(() => Tournaments));
            modelRepositories.Add(CreateRepository(() => TournamentRounds));
        }

        /// <summary>
        /// Load all data from files
        /// </summary>
        public async Task LoadData() {
            StorageFolder folder = await GetRootFolder();
            // Loop each repository and ask it to load all it's data from the root folder
            foreach (IModelRepository modelRepository in modelRepositories) {
                await modelRepository.Load(folder);
            }
        }

        /// <summary>
        /// Save all data to files
        /// </summary>
        /// <param name="txtStatus">Optional textbox to update with the data (Notable use on login screen)</param>
        public async Task SaveData(TextBlock txtStatus = null) {
            StorageFolder folder = await GetRootFolder();
            // Loop each repository and ask it to save all it's data to the root folder
            foreach (IModelRepository modelRepository in modelRepositories) {
                if (txtStatus != null) {
                    // Update the status textbox if it has been given as a parameter
                    txtStatus.Text = "Loading... Saving " + modelRepository.GetName() + "s";
                }
                // And ask the repository to save
                await modelRepository.Save(folder);
            }
        }

        /// <summary>
        /// Asks all repositories to clear all of their data
        /// </summary>
        public async Task ClearData() {
            StorageFolder folder = await GetRootFolder();
            // Loop the repositories to clear them all
            foreach (IModelRepository modelRepository in modelRepositories) {
                await modelRepository.Clear(folder);
            }
        }

        /// <summary>
        /// Ask for a new ID for a specific model
        /// </summary>
        /// <param name="type">The type of the object to get an ID for (i.e. Member, Game)</param>
        /// <returns>int: The new ID</returns>
        public async Task<int> GetUniqueID(Type type) {
            StorageFolder folder = await GetRootFolder();
            
            // Open folder for this type
            StorageFolder typeFolder = await folder.CreateFolderAsync(type.Name + "s", CreationCollisionOption.OpenIfExists);
            StorageFile sFile = await typeFolder.CreateFileAsync("ID", CreationCollisionOption.OpenIfExists);
            // The file should be storing an integer in plaintext
            string idString = await FileIO.ReadTextAsync(sFile);
            int id;
            bool success = int.TryParse(idString, out id);
            if (!success) // File was empty or not previously updated; return 1.
                id = 1;
            // Delete the file
            await sFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
            // And create a new file with the new ID
            sFile = await typeFolder.CreateFileAsync("ID", CreationCollisionOption.OpenIfExists);
            // Write the next ID to the new file
            await FileIO.WriteTextAsync(sFile, (id + 1).ToString());
            // And return the current ID
            return id;
        }

        /// <summary>
        /// Gets the root folder for data storage
        /// </summary>
        /// <returns>StorageFolder: Root folder object</returns>
        public async static Task<StorageFolder> GetRootFolder() {
            // While it looks like this method may be creating a new folder every time
            //   the argument "CreationCollisionOption.OpenIfExists" ensures that if it
            //   already exists, it will just return it instead of creating a new folder.
            return await ApplicationData.Current.RoamingFolder.CreateFolderAsync("DataRepository", CreationCollisionOption.OpenIfExists);
        }

        /// <summary>
        /// Create a repository given a type and a lambda expression that returns a model list of that type
        /// </summary>
        /// <typeparam name="T">Type of object to create a repository for</typeparam>
        /// <param name="modelProperty">A lambda expression (Inline function) that returns a list of the model type</param>
        /// <returns></returns>
        private static IModelRepository CreateRepository<T>(Func<List<T>> modelProperty) where T : Model, new() {
            return new ModelRepository<T>(modelProperty);
        }
    }



    // Model repo code for loading / saving to abstract file handling


    /// <summary>
    /// This interface allows a list of different model repositories to be stored
    /// Since a list cannot contain e.g. a ModelRepository of Member and a ModelRepository of Game,
    ///   however it can contain a list this parent object
    /// </summary>
    interface IModelRepository
    {
        /// <summary>
        /// Asynchronous save method to be implemented by the model repository
        /// </summary>
        /// <param name="rootFolder">Folder that contains all data</param>
        Task Save(StorageFolder rootFolder);

        /// <summary>
        /// Asynchronous load method to be implemented by the model repository
        /// </summary>
        /// <param name="rootFolder">Folder that contains all data</param>
        Task Load(StorageFolder rootFolder);

        /// <summary>
        /// Asynchronous save method to be implemented by the model repository
        /// </summary>
        /// <param name="rootFolder">Folder that contains all data</param>
        Task Clear(StorageFolder folder);

        /// <summary>
        /// A method to be implemented by the model property
        /// </summary>
        /// <returns>The repository name</returns>
        String GetName();
    }

    /// <summary>
    /// ModelRepository class allows ModelRepositories of any type to be handled
    /// </summary>
    /// <typeparam name="T">The type that this repository will store</typeparam>
    class ModelRepository<T> : IModelRepository where T : Model, new() {
        /// <summary>
        /// The lambda expression that will return the type of the model
        /// </summary>
        private readonly Func<List<T>> modelProperty;
        /// <summary>
        /// The name of the model
        /// </summary>
        public readonly static string ModelName = typeof(T).Name;

        /// <summary>
        /// Constructor; set the modelProperty.
        /// </summary>
        /// <param name="modelProperty">A lambda that returns a list of the model type</param>
        public ModelRepository(Func<List<T>> modelProperty) {
            this.modelProperty = modelProperty;
        }

        /// <summary>
        /// Returns ModelName; important due to use of polymorphism
        /// </summary>
        public string GetName() {
            return ModelName;
        }

        /// <summary>
        /// Saves all data that this repository has
        /// </summary>
        /// <param name="rootFolder">Folder in which to save the data</param>
        public async Task Save(StorageFolder rootFolder) {
            StorageFolder modelFolder = await GetFolder(rootFolder);
            // If all data was deleted then delete all the files
            if (modelProperty().Count == 0)
                // Create a new folder for this model, deleting the old one while it does
                modelFolder = await rootFolder.CreateFolderAsync(ModelName + "s", CreationCollisionOption.ReplaceExisting);

            // Loop each instance in the list
            foreach (T model in modelProperty()) {
                // Create an XmlSerializer for serializing the data
                XmlSerializer xmlSerializer = new XmlSerializer(model.GetType());
                // Create a file for the model
                // We replace existing files so that it overwrites old data
                StorageFile file = await modelFolder.CreateFileAsync(model.ID.ToString(), CreationCollisionOption.ReplaceExisting);
                // A random access stream is used to push data to the file
                IRandomAccessStream fileRandomAccessStream = await file.OpenAsync(FileAccessMode.ReadWrite);
                // Used to disposing and flushing data which helps for memory conservation
                IOutputStream fileOutputStream = fileRandomAccessStream.GetOutputStreamAt(0);
                // Serialize the data to the file
                xmlSerializer.Serialize(fileRandomAccessStream.AsStreamForWrite(), model);
                // Dispose of and flush the file streams (Do flush asynchronously so it doesn't jam the thread)
                fileRandomAccessStream.Dispose();
                await fileOutputStream.FlushAsync();
                fileOutputStream.Dispose();
            }
        }

        /// <summary>
        /// Loads all model data into the list
        /// </summary>
        /// <param name="rootFolder">Folder that data is saved in</param>
        public async Task Load(StorageFolder rootFolder) {
            StorageFolder modelFolder = await GetFolder(rootFolder);
            // Empty the model property incase we're reloading (Otherwise we get duplicates loaded)
            modelProperty().Clear();
            // Loop each file
            foreach (StorageFile file in await modelFolder.GetFilesAsync()) {
                // If the name is ID then we ignore it since the ID file is used to store the incrementing unique ID value
                if (file.DisplayName != "ID")
                    // And load the file item
                    await LoadItem(file);
            }
        }

        /// <summary>
        /// Permanent clear all the items in the model
        /// </summary>
        /// <param name="rootFolder">Folder that data is saved in</param>
        public async Task Clear(StorageFolder rootFolder) {
            StorageFolder modelFolder = await GetFolder(rootFolder);
            // Clear the list
            modelProperty().Clear();
            // And delete every file
            foreach (StorageFile file in await modelFolder.GetFilesAsync()) {
                await file.DeleteAsync();
            }
        }

        /// <summary>
        /// Get the folder of a model given the root folder
        /// </summary>
        /// <param name="rootFolder">Folder that stores all application data</param>
        public async static Task<StorageFolder> GetFolder(StorageFolder rootFolder) {
            // Open the folder if it exists, create if not.
            return await rootFolder.CreateFolderAsync(ModelName + "s", CreationCollisionOption.OpenIfExists);
        }

        /// <summary>
        /// Load an item given the file
        /// </summary>
        /// <param name="serializedFile">File with model data to load in it</param>
        protected async Task LoadItem(StorageFile serializedFile) {
            // Create a new XmlSerializer with the type of this repository's stored type
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            // Open an input stream with the file
            IInputStream sessionInputStream = await serializedFile.OpenReadAsync();
            // And finally we ask the serializer to deserializer the object and add it to the list of model instances
            T loaded = (T) xmlSerializer.Deserialize(sessionInputStream.AsStreamForRead());
            modelProperty().Add(loaded);
        }
    }
}
