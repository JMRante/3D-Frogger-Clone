using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Autotiles3D
{

    public class Autotiles3D_SettingsWindow : EditorWindow
    {
        [MenuItem("Tools/Autotiles3D/Settings")]
        static void OpenSettingsWindow()
        {
#if UNITY_2020_1_OR_NEWER
            EditorWindow.CreateWindow<Autotiles3D_SettingsWindow>("Autotiles 3D Settings", typeof(SceneView));
#else

            var window = EditorWindow.CreateInstance<Autotiles3D_SettingsWindow>();// ("Autotiles 3D Settings", typeof(SceneView));
            window.Show();
#endif
        }

        private void OnGUI()
        {

            var tileGroups = LoadTileGroups();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField($"Found <color=yellow>{tileGroups.Count}</color> TileGroups in Resources", RichStyle);

            foreach (var tileGroup in tileGroups)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{tileGroup.name}");
                EditorGUILayout.LabelField($"Amount of tiles: {tileGroup.Tiles.Count}", GUILayout.Width(120));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Ping & View Scriptable Object"))
                {
                    EditorGUIUtility.PingObject(tileGroup);
                    Selection.activeObject = tileGroup;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create new TileGroup"))
            {
                EnsureFolders();
                var newTileGroup = CreateInstance<Autotiles3D_TileGroup>();
                string uniquepath = AssetDatabase.GenerateUniqueAssetPath("Assets/Autotiles3D/Resources/NewTileGroup.asset");
                AssetDatabase.CreateAsset(newTileGroup, uniquepath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(newTileGroup);
            }

            EditorGUILayout.EndHorizontal();


            EditorGUILayout.EndVertical();


        }

        public static List<Autotiles3D_TileGroup> LoadTileGroups()
        {
            EnsureFolders();
            return Resources.LoadAll<Autotiles3D_TileGroup>("").ToList();
        }

        public static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder($"Assets/Autotiles3D"))
                AssetDatabase.CreateFolder("Assets", "Autotiles3D");

            if (!AssetDatabase.IsValidFolder($"Assets/Autotiles3D/Resources"))
                AssetDatabase.CreateFolder("Assets/Autotiles3D", "Resources");
        }
        public static GUIStyle RichStyle
        {
            get
            {
                var style = new GUIStyle();
                style.richText = true;
                style.normal.textColor = Color.white;
                return style;
            }
        }

    }
}