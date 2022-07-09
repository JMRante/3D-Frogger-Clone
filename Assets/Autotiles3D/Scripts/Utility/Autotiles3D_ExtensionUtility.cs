#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using UnityEditor;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Autotiles3D
{
    public static class Autotiles3D_ExtensionUtility
    {
        public static void DeleteInstance(this InternalNode node)
        {
            if (node.Instance != null)
            {
                node.Instance.SetActive(true);
                if (Autotiles3D_Settings.EditorInstance.DontRegisterUndo)
                    GameObject.DestroyImmediate(node.Instance);
                else
                    Undo.DestroyObjectImmediate(node.Instance);
            }
        }

        public static void DisableInstance(this InternalNode node)
        {
            if (node.Instance != null)
                node.Instance.SetActive(false);
        }
        public static void EnableInstance(this InternalNode node)
        {
            if (node.Instance != null)
                node.Instance.SetActive(true);
        }

        public static void UpdateInstance(this InternalNode node)
        {
            GameObject prefab = node.Tile.Default;
            bool[] neighbors = node.Layer.GetNeighborsBoolSelfSpace(node.InternalPosition, node.LocalRotation);

            var rule = node.Tile.GetRule(neighbors, out int[] addedRotation);
            if (rule != null)
                prefab = rule.Object;

            if (prefab == null)
                return;

            if (addedRotation[0] > -1) //if rotated around an axis, add the rotation 
            {
                Vector3 axis = Vector3.right;
                if (addedRotation[0] == 1)
                    axis = Vector3.up;
                else if (addedRotation[0] == 2)
                    axis = Vector3.forward;
                node.LocalRotation *= Quaternion.AngleAxis(addedRotation[1], axis);
            }

            if (node.Instance == null)
            {
                //DebugNeighbors(node, neighbors); //useful for debugging
                node.Instance = PrefabUtility.InstantiatePrefab(prefab, node.Layer.Anchors[node.Tile.TileID].transform) as UnityEngine.GameObject;
                if (!Autotiles3D_Settings.EditorInstance.DontRegisterUndo)
                    Undo.RegisterCreatedObjectUndo(node.Instance, "InstanceUpdate");

            }
            else if (PrefabUtility.GetCorrespondingObjectFromSource(node.Instance) != prefab)
            {
                var newInstance = PrefabUtility.InstantiatePrefab(prefab, node.Layer.Anchors[node.Tile.TileID].transform) as UnityEngine.GameObject;

                var newBlock = newInstance.GetComponent<Autotiles3D_BlockBehaviour>();
                if (node.Block != null && newBlock != null)
                {
                    var viewCache = newBlock.View;
                    CopyComponent(node.Block, newBlock);
                    newBlock.View = viewCache;
                }

                if (!Autotiles3D_Settings.EditorInstance.DontRegisterUndo)
                    Undo.DestroyObjectImmediate(node.Instance);
                else
                    GameObject.DestroyImmediate(node.Instance);

                node.Instance = newInstance;
                if (!Autotiles3D_Settings.EditorInstance.DontRegisterUndo)
                    Undo.RegisterCreatedObjectUndo(node.Instance, "InstanceUpdate");
            }

            if (node.Block != null)
            {
                node.Block.OnInstanceUpdate(node.Tile, node.Tile.TileID, node.Tile.DisplayName, node.InternalPosition, node.LocalRotation);
            }

            if (node.Instance.name != prefab.name)
                node.Instance.name = prefab.name;

            node.UpdateInstanceTransformOnly();
        }

        #region DEBUGGING
        private static void DebugNeighbors(InternalNode node, bool[] neighbors)
        {
            string middle = node.InternalPosition.ToString();
            middle += $"\n{neighbors[9]}  {neighbors[10]}  {neighbors[11]}";
            middle += $"\n{neighbors[12]}  {neighbors[13]}  {neighbors[14]}";
            middle += $"\n{neighbors[15]}  {neighbors[16]}  {neighbors[17]}";
            Debug.Log(middle);
        }
        #endregion

        public static void UpdateInstanceTransformOnly(this InternalNode node)
        {
            if (node.Instance != null)
            {
                node.Instance.transform.position = node.Layer.Grid.ToWorldPoint((Vector3)node.InternalPosition * node.Layer.Grid.Unit);
                node.Instance.transform.rotation = node.Layer.Grid.transform.rotation * node.LocalRotation;
            }
        }


        public static bool IsEqual(this Autotiles3D_Tile tile, Autotiles3D_Tile compare)
        {
            if (tile == compare)
                return true;
            if (tile.TileID == compare.TileID)
                return true;
            if (tile.DisplayName == compare.DisplayName)
                return true;
            return false;
        }

        public static void CopyComponent<T>(this T original, T destination) where T : Component
        {
            System.Type originalType = original.GetType();
            System.Type destinationType = destination.GetType();
            if (destinationType.IsSubclassOf(originalType))
            {
                FieldInfo[] originalFields = originalType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (FieldInfo field in originalFields)
                    field.SetValue(destination, field.GetValue(original));
            }
            else if (originalType.IsSubclassOf(destinationType))
            {
                FieldInfo[] destinationFields = destinationType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (FieldInfo field in destinationFields)
                    field.SetValue(destination, field.GetValue(original));

            }
        }

        /// <summary>
        /// utility function for deepcloing any serializable class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T DeepClone<T>(this T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
            }
        }

        public static Autotiles3D_Anchor EnsureAnchor<T>(this Autotiles3D_TileLayer layer, T tile) where T : Autotiles3D_Tile
        {
            Type type = tile.GetType();
            if (!layer.Anchors.ContainsKey(tile.TileID))
            {
                var anchorObject = new GameObject("Anchor " + tile.DisplayName);
                anchorObject.transform.SetParent(layer.transform);
                var anchor = anchorObject.AddComponent<Autotiles3D_Anchor>();
                anchor.TileID = tile.TileID;

                if (!layer.Anchors.ContainsKey(tile.TileID))
                    layer.Anchors.Add(tile.TileID, anchor);
                EditorUtility.SetDirty(layer);
            }

            layer.Anchors[tile.TileID].transform.localPosition = Vector3.zero;
            layer.Anchors[tile.TileID].transform.localRotation = Quaternion.identity;
            return layer.Anchors[tile.TileID];
        }


        public static List<Vector3Int> GetNeighborsPosition(this Autotiles3D_TileLayer layer, Vector3Int internalPosition)
        {
            var myNeighbors = new List<Vector3Int>();
            Vector3Int iteration;
            for (int x = (int)internalPosition.x - 1; x <= (int)internalPosition.x + 1; x++)
            {
                for (int y = (int)internalPosition.y - 1; y <= (int)internalPosition.y + 1; y++)
                {
                    for (int z = (int)internalPosition.z - 1; z <= (int)internalPosition.z + 1; z++)
                    {
                        iteration = new Vector3Int(x, y, z);
                        if (iteration == internalPosition)
                            continue;
                        if (layer.InternalNodes.ContainsKey(iteration))
                            myNeighbors.Add(iteration);
                    }
                }
            }
            return myNeighbors;
        }

        public static bool[] GetNeighborsBoolSelfSpace(this Autotiles3D_TileLayer layer, Vector3Int internalPosition, Quaternion localRotation)
        {
            bool[] neighbors = new bool[27];
            Vector3Int iteration;
            int deltax, deltay, deltaz;
            deltay = 0;
            for (int y = -1; y <= 1; y++)
            {
                deltaz = 0;
                for (int z = 1; z >= -1; z--)
                {
                    deltax = 0;
                    for (int x = -1; x <= 1; x++)
                    {
                        iteration = Vector3Int.RoundToInt(internalPosition + localRotation * new Vector3Int(x, y, z));

                        if (iteration != internalPosition)
                        {
                            if (layer.InternalNodes.ContainsKey(iteration))
                                neighbors[deltay * 9 + deltaz * 3 + deltax] = true;
                        }
                        deltax++;
                    }
                    deltaz++;
                }
                deltay++;
            }
            return neighbors;
        }
    }

}

#endif
