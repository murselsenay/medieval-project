#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Utilities
{
    public class ScriptableSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    string resourcePath = $"Scriptables/Singletons/{typeof(T).Name}";
                    _instance = Resources.Load<T>(resourcePath);

#if UNITY_EDITOR
                    if (_instance == null)
                    {
                        // Editor fallback: try to find the asset anywhere in the project to help debugging
                        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
                        if (guids != null && guids.Length > 0)
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                            _instance = AssetDatabase.LoadAssetAtPath<T>(path);
                            Debug.LogWarning($"[ScriptableSingleton] Resources.Load failed for 'Resources/{resourcePath}.asset'. Editor fallback loaded '{path}'.",
                                _instance);
                        }
                    }
#endif
                    if (_instance == null)
                    {
                        Debug.LogError(
                            $"[ScriptableSingleton] Could not find Scriptable singleton asset at 'Resources/{resourcePath}.asset'. Make sure an asset named '{typeof(T).Name}.asset' exists under Assets/Resources/Scriptables/Singletons.");
                        return null;
                    }
                }

                if (_instance != null)
                    _instance.hideFlags = HideFlags.DontUnloadUnusedAsset;

                return _instance;
            }
        }
    }

#if UNITY_EDITOR
    public class ScriptableEditorSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = (T)EditorGUIUtility.Load($"Scriptables/Singletons/{typeof(T).Name}.asset");
                }

                return _instance;
            }
        }
    }
#endif
}
