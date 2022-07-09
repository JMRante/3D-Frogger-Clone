using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Autotiles3D
{
    [CustomEditor(typeof(Autotiles3D_BlockBehaviour), true)]
    public class Autotiles3D_BlockBehaviourInspector : Editor
    {
        private Autotiles3D_BlockBehaviour _baseBlock;
        public virtual void OnEnable()
        {
            _baseBlock = (Autotiles3D_BlockBehaviour)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            string isBaked = _baseBlock.IsBaked ? "(IS BAKED)" : "";
            EditorGUILayout.LabelField($"Tile:{_baseBlock.TileDisplayName} {isBaked}");
            EditorGUILayout.LabelField($"Position:{_baseBlock.InternalPosition}");
            EditorGUILayout.LabelField($"Rotation:{_baseBlock.LocalRotation}");
            EditorGUILayout.EndVertical();
        }
    }

}