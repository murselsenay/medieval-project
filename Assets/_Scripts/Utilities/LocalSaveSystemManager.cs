
using Modules.Logger;

namespace Utilities
{
    public partial class LocalSaveSystemManager
    {
        private static ES3Settings _settings = new ES3Settings()
        {
            encryptionType = ES3.EncryptionType.AES,
            encryptionPassword = "parola01!+",
        };
        public static void Save<T>(string key, T data)
        {
            if (string.IsNullOrEmpty(key) || data == null) return;
            try
            {
                ES3.Save(key, data, _settings);
            }
            catch (System.Exception e)
            {
                DebugLogger.LogError($"Error on {key} key : {e.Message}");
            }
        }

        public static bool KeyExists(string key)
        {
            return ES3.KeyExists(key, _settings);
        }

        public static void DeleteKey(string key)
        {
            if (KeyExists(key))
            {
                ES3.DeleteKey(key, _settings);
            }
        }

        public static T Load<T>(string key)
        {
            if (ES3.KeyExists(key))
            {
                return ES3.Load<T>(key, _settings);
            }
            return default(T);
        }
    }


}
