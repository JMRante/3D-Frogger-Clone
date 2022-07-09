using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Autotiles3D
{
    public class Autotiles3D_Settings : ScriptableObject
    {
        private static Autotiles3D_Settings _settings;
        public const string SettingsPath = "Assets/Autotiles3D/Content/Autotiles3D_Settings.asset";

        public bool DontRegisterUndo = false;

#if UNITY_EDITOR
        public static Autotiles3D_Settings EditorInstance
        {
            get
            {
                if(_settings == null)
                {
                    var settings = AssetDatabase.LoadAssetAtPath<Autotiles3D_Settings>(SettingsPath);
                    if (settings == null)
                    {
                        settings = ScriptableObject.CreateInstance<Autotiles3D_Settings>();
                        AssetDatabase.CreateAsset(settings, SettingsPath);
                        AssetDatabase.SaveAssets();
                    }
                    _settings = settings;
                }
                return _settings;
            }
        }
        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(EditorInstance);
        }
#endif
    }
}
