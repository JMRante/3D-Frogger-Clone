using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Linq;

namespace Autotiles3D
{
    [InitializeOnLoad]
    public static class Autotiles3D_HierarchyChange
    {
        public static bool IsLocked;
        static Autotiles3D_HierarchyChange()
        {
            if (!Application.isPlaying)
            {
                BeginListerningToHierarchyChanged();
            }
        }
        public static void BeginListerningToHierarchyChanged()
        {
            StopListerningToHierarchyChanged();
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }
        public static void StopListerningToHierarchyChanged()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        static void OnHierarchyChanged()
        {
            if (IsLocked || Application.isPlaying)
                return;

            if (Selection.activeTransform != null)
            {
                var layer = Selection.activeTransform.GetComponentInParent<Autotiles3D_TileLayer>();
                if (layer != null)
                {
                    var udpatables = layer.GetComponentsInChildren<IHierarchyUpdate>();
                    foreach (var updatable in udpatables)
                        updatable.OnHierachyUpdate();
                }
            }

        }
    }
}