using System.Collections.Concurrent;

namespace Repox
{
    internal static class ModelStore
    {
        // TODO: Change this to an Memory Cache
        private static ConcurrentDictionary<string, ModelInfo> _modelStore = new ConcurrentDictionary<string, ModelInfo>();

        //WAS => internal static ModelInfo GetInfoItem<T>(IRepoModel model) where T : class, IRepoModel // Changed 02.01.22 FXD
        internal static ModelInfo GetInfoItem<T>() where T : class, IRepoModel
        {
            // Get a cached ModelInfo object or create a ModelInfo object and store it in the cache
            var modelName = typeof(T).Name;

            // Check the dictionary for the object
            _modelStore.TryGetValue(modelName, out var modelVal);

            if (modelVal != null)
                return modelVal;

            // Process Model and Add to Dictionary
            modelVal = new ModelParser<T>().ModelInfo();

            _modelStore.TryAdd(modelName, modelVal);

            // Return Model
            return modelVal;
        }
    }
}