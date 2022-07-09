using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Autotiles3D
{
    public interface IHierarchyUpdate
    {
#if UNITY_EDITOR
        void OnHierachyUpdate();
#endif
    }

    [System.Serializable]
    public class InternalNode
    {
        public Autotiles3D_Tile Tile;
        public Autotiles3D_TileLayer Layer;
        public Autotiles3D_BlockBehaviour Block; //optional

        public Vector3Int InternalPosition; // integer position inside the grid != local position of the transform
        public Quaternion LocalRotation = Quaternion.identity;

        public GameObject Prefab;
        [SerializeField] private GameObject _instance;
        public Collider EditorCollider;
        public GameObject Instance
        {
            get { return _instance; }
            set
            {
                _instance = value;
                if (_instance != null)
                {
                    Block = _instance.GetComponent<Autotiles3D_BlockBehaviour>();
                    if (Block == null)
                    {
                        Block = _instance.AddComponent<Autotiles3D_BlockBehaviour>();
                        Block.View = _instance;
                    }
                }
            }
        }
        public InternalNode(Autotiles3D_TileLayer layer, Autotiles3D_Tile tile, Vector3Int internalPosition, Quaternion localRotation, GameObject instance = null)
        {
            Layer = layer;
            Tile = tile;
            LocalRotation = localRotation;
            InternalPosition = internalPosition;

            if (instance != null)
            {
                Instance = instance;
            }
        }
    }

    public class Autotiles3D_TileLayer : MonoBehaviour, ISerializationCallbackReceiver , IHierarchyUpdate
    {
        public string LayerName;
        public Dictionary<Vector3Int, InternalNode> InternalNodes = new Dictionary<Vector3Int, InternalNode>();
        public Dictionary<int, Autotiles3D_Anchor> Anchors = new Dictionary<int, Autotiles3D_Anchor>(); // tileID, anchor

        public Autotiles3D_Anchor GetAnchor(int tileID)
        {
            if (Anchors.ContainsKey(tileID))
                return Anchors[tileID];
            return null;
        }
        public Autotiles3D_TileGroup Group;
        public List<Autotiles3D_Tile> Tiles => Group != null ? Group.Tiles : new List<Autotiles3D_Tile>();
        public Autotiles3D_Tile ActiveTile
        {
            get
            {
                if (Tiles.Count > 0 && _ActiveTileSelection < Tiles.Count)
                    return Tiles[_ActiveTileSelection];
                return null;
            }
        }

        public int _ActiveTileSelection;

        private Autotiles3D_Grid _Grid;
        public Autotiles3D_Grid Grid
        {
            get
            {
                if (_Grid == null)
                    _Grid = GetComponentInParent<Autotiles3D_Grid>();
                return _Grid;
            }
            set
            {
                _Grid = value;
            }
        }


        #region Serialization
        [SerializeField] private List<Vector3Int> _NodesKeys = new List<Vector3Int>();
        [SerializeField] private List<InternalNode> _NodesValues = new List<InternalNode>();
        [SerializeField] private List<int> _AnchorKeys = new List<int>();
        [SerializeField] private List<Autotiles3D_Anchor> _AnchorValues = new List<Autotiles3D_Anchor>();
        public void OnBeforeSerialize()
        {
            _NodesKeys = InternalNodes.Keys.ToList();
            _NodesValues = InternalNodes.Values.ToList();
            _AnchorKeys = Anchors.Keys.ToList();
            _AnchorValues = Anchors.Values.ToList();
        }

        public void OnAfterDeserialize()
        {
            InternalNodes = new Dictionary<Vector3Int, InternalNode>();
            for (int i = 0; i < _NodesKeys.Count; i++)
            {
                InternalNodes.Add(_NodesKeys[i], _NodesValues[i]);
            }
            Anchors = new Dictionary<int, Autotiles3D_Anchor>();
            for (int i = 0; i < _AnchorKeys.Count; i++)
            {
                Anchors.Add(_AnchorKeys[i], _AnchorValues[i]);
            }
        }
        #endregion

#if UNITY_EDITOR

        public static bool IS_EDITING;
        public static bool SHOW_LAYER_OUTLINE;
        public static bool SHOW_HOVER_GIZMO;

        public List<Autotiles3D_TileGroup> LoadedGroups = new List<Autotiles3D_TileGroup>();
        private Dictionary<Vector3Int, InternalNode> _nodeRefreshs = new Dictionary<Vector3Int, InternalNode>();
        private Dictionary<Vector3, GameObject> _tempCachedInstances = new Dictionary<Vector3, GameObject>();
        public Dictionary<Vector3Int, InternalNode> NodeRefreshs => _nodeRefreshs;
        public Dictionary<Vector3, GameObject> TempCachedInstances => _tempCachedInstances;


        #region HOVER
        private (Autotiles3D_Tile tile, GameObject instance) _hoverInstance;
        public (Autotiles3D_Tile tile, GameObject instance) HoverInstance { get => _hoverInstance; set => _hoverInstance = value; }

        public GameObject HoverPrefabObject; //the object of the rule that won, not the instance
        public Vector3Int LocalHoverPosition;
        public Vector3Int PrevLocalHoverPosition;

        #endregion
        public bool HideGridOutlineRenderer { get; set; }
        public Vector3Int SearchedInternalPosition;
        public InternalNode SearchedInternalNode;

        public void OnHierachyUpdate()
        {
            VerifyAnchors();
            VerifyNodes();
        }

        //public void Verify()
        //{
        //    VerifyAnchors();
        //    VerifyNodes();
        //}

        public void VerifyNodes()
        {
            //remove deleted blocks from nodes
            foreach (var internalNode in InternalNodes.ToArray())
            {
                if (internalNode.Value.Instance == null)
                    InternalNodes.Remove(internalNode.Key);
            }

            //add blocks to nodes 
            foreach (var entry in Anchors)
            {
                var anchor = entry.Value;
                if (anchor == null)
                    continue;

                anchor.Blocks = anchor.GetComponentsInChildren<Autotiles3D_BlockBehaviour>(true).ToList();

                foreach (var block in anchor.Blocks)
                {
                    if (!InternalNodes.ContainsKey(block.InternalPosition))
                    {
                        InternalNodes.Add(block.InternalPosition, new InternalNode(this, block.Tile, block.InternalPosition, block.LocalRotation, block.gameObject));
                        if (!Anchors.ContainsKey(block.Tile.TileID))
                            this.EnsureAnchor(block.Tile);
                    }
                }

            }
        }
        public void VerifyAnchors()
        {
            Anchors.Clear();
            var anchors = GetComponentsInChildren<Autotiles3D_Anchor>();
            foreach (var anchor in anchors.ToList())
            {
                if (!Anchors.ContainsKey(anchor.TileID))
                {
                    Anchors.Add(anchor.TileID, anchor);
                }
                anchor.UpdateBakeCount();
            }
        }

        public void UpdateNeighborsCubeAlgorithm(HashSet<Vector3Int> internalPositions)
        {
            (int minX, int maxX) = ((int)internalPositions.Min(l => l.x), (int)internalPositions.Max(l => l.x));
            (int minY, int maxY) = ((int)internalPositions.Min(l => l.y), (int)internalPositions.Max(l => l.y));
            (int minZ, int maxZ) = ((int)internalPositions.Min(l => l.z), (int)internalPositions.Max(l => l.z));

            var myNeighbors = new List<Vector3Int>();
            Vector3Int iteration;
            for (int x = minX - 1; x <= maxX + 1; x++)
            {
                for (int y = minY - 1; y <= maxY + 1; y++)
                {
                    for (int z = minZ -1; z <= maxZ + 1; z++)
                    {
                        iteration = new Vector3Int(x, y, z);

                        if (InternalNodes.ContainsKey(iteration))
                            RefreshNode(InternalNodes[iteration]);
                    }
                }
            }
        }
        void UpdateNeighbors(Vector3Int originalInternalPosition) //slow if called repeatly while overlapping
        {
            var neighbors = this.GetNeighborsPosition(originalInternalPosition);
            foreach (var neighbor in neighbors)
            {
                if (InternalNodes.ContainsKey(neighbor))
                    RefreshNode(InternalNodes[neighbor]);
            }
        }


        #region EXPOSED API

        //DISCLAIMER:
        //Manipulating Internal Nodes manually via code can very easily lead to bugs. 
        //Please proceed at your own risk!!!
        //Please understand that any TryPlacing/TryUnplacing/RefreshNode calls only update the internal data and require a final call
        //to the function “public void Refresh()” to translate any changes made to the layer’s internal nodes to the scene.
        //This is done for performance reasons, so that multiple changes of intern nodes (fast) can be “stacked” internally until the
        //next call of Refresh() which updates the scene by instantiating the correct gameobjects etc (slow) at once. Refresh() is called once every OnSceneGUI update.

        /// <summary>
        /// Adds a single node to the layer
        /// </summary>
        /// <param name="internalPosition"></param>
        /// <param name="localRotation"></param>
        /// <param name="tile"></param>
        public void TryPlacementSingle(Vector3Int internalPosition, Quaternion localRotation, Autotiles3D_Tile tile = null)
        {
            if (tile == null)
                tile = ActiveTile;
            AddNodeInternal(tile, internalPosition, localRotation);
            UpdateNeighbors(internalPosition);
        }
        /// <summary>
        /// Adds multiple nodes to the layer
        /// </summary>
        /// <param name="localPositions"></param>
        /// <param name="localRotations"></param>
        /// <param name="tiles"></param>
        public void TryPlacementMany(List<Vector3Int> localPositions, List<Quaternion> localRotations, List<Autotiles3D_Tile> tiles)
        {
            for (int i = 0; i < localPositions.Count; i++)
                AddNodeInternal(tiles[i], localPositions[i], localRotations[i]);

            HashSet<Vector3Int> hashedPositions = new HashSet<Vector3Int>(localPositions);
            UpdateNeighborsCubeAlgorithm(hashedPositions);

        }

        /// <summary>
        /// Removes a single node from the layer
        /// </summary>
        /// <param name="internalPosition"></param>
        public void TryUnplacingSingle(Vector3Int internalPosition)
        {
            RemoveNodeInternal(internalPosition);
            UpdateNeighbors(internalPosition);
        }

        /// <summary>
        /// Removes multiple nodes together (better performance than removing one by one)
        /// </summary>
        /// <param name="internalPosition"></param>
        /// <param name="waitForDestroy">used internally for extruding/inv. extruding nodes and should NOT be used if you're not knowing what it does</param>
        public void TryUnplacingMany(List<Vector3Int> internalPosition, bool waitForDestroy = false)
        {
            for (int i = 0; i < internalPosition.Count; i++)
            {
                RemoveNodeInternal(internalPosition[i], waitForDestroy);
            }
            HashSet<Vector3Int> hashedPositions = new HashSet<Vector3Int>(internalPosition);
            UpdateNeighborsCubeAlgorithm(hashedPositions);

        }

        /// <summary>
        /// Refreshes a single node which checks the neighboring conditions anew and updates the gameobject instance accordingly.
        /// </summary>
        /// <param name="node"></param>
        public void RefreshNode(InternalNode node)
        {
            if (node.Block != null)
            {
                if (node.Block.IsBaked) //don't allow update for baked nodes
                    return;
            }

            if (!NodeRefreshs.ContainsKey(node.InternalPosition))
                NodeRefreshs.Add(node.InternalPosition, node);
        }

        /// <summary>
        /// Refreshes all nodes of the corresponding anchor.
        /// </summary>
        /// <param name="anchor"></param>
        /// <param name="forceImmediateRefresh"> if true, the update will happen immediately</param>
        public void RefreshAll(Autotiles3D_Anchor anchor, bool forceImmediateRefresh = false)
        {
            if (anchor == null)
                return;

            VerifyNodes();

            foreach (var block in anchor.Blocks)
            {
                if (InternalNodes.ContainsKey(block.InternalPosition))
                {
                    InternalNode node = InternalNodes[block.InternalPosition];
                    if (forceImmediateRefresh)
                        node.UpdateInstance();
                    else
                        RefreshNode(node);
                }
            }

            anchor.UpdateBakeCount();
        }

        #endregion

        /// <summary>
        /// this is the refresh update that will actually update any internal node that has been modified. 
        /// called once per OnSceneGUI Update
        /// </summary>
        public void Refresh()
        {
            foreach (var node in NodeRefreshs)
            {
                if (node.Value.Tile == null)
                    continue;
                node.Value.UpdateInstance();
            }
            NodeRefreshs.Clear();
        }


        private void RemoveNodeInternal(Vector3Int internalPosition, bool waitForDestroy = false)
        {
            if (InternalNodes.ContainsKey(internalPosition))
            {
                //dont allow remove of baked nodes
                if (InternalNodes[internalPosition].Block.IsBaked)
                {
                    Debug.Log("Autotiles3D: Will not remove baked blocks.");
                    return;
                }

                if (NodeRefreshs.ContainsKey(internalPosition))
                    NodeRefreshs.Remove(internalPosition);

                if (!waitForDestroy)
                    InternalNodes[internalPosition].DeleteInstance();
                else
                {
                    if (!TempCachedInstances.ContainsKey(internalPosition)) //cached instance so the same instance be reused if immediately drawn before destroy callback happens
                    {
                        TempCachedInstances.Add(internalPosition, InternalNodes[internalPosition].Instance);
                        InternalNodes[internalPosition].DisableInstance();
                    }
                }
                InternalNodes.Remove(internalPosition);
            }
        }


        private void AddNodeInternal(Autotiles3D_Tile tile, Vector3Int internalPosition, Quaternion localRotation)
        {
            if (!InternalNodes.ContainsKey(internalPosition))
            {
                InternalNodes.Add(internalPosition, new InternalNode(this, tile, internalPosition, localRotation));
                //check if instance was saved temporarily
                if (TempCachedInstances.ContainsKey(internalPosition))
                {
                    InternalNodes[internalPosition].Instance = TempCachedInstances[internalPosition];
                    InternalNodes[internalPosition].EnableInstance();
                    TempCachedInstances.Remove(internalPosition);
                }

                if (!Anchors.ContainsKey(tile.TileID))
                    this.EnsureAnchor(tile);

                RefreshNode(InternalNodes[internalPosition]);
            }
        }



        public void DestroyHoverInstance()
        {
            if (_hoverInstance.instance != null)
            {
                DestroyImmediate(_hoverInstance.instance);
                _hoverInstance.tile = null;
            }
        }
        public void ToggleView(Autotiles3D_Tile tile, bool enable)
        {
            var matches = InternalNodes.Where(p => p.Value.Tile.IsEqual(tile)).ToList();
            foreach (var match in matches)
                match.Value.Block?.ToggleView(enable);
        }
        public void RemoveAllBlocks(Autotiles3D_Tile tile)
        {
            var matches = InternalNodes.Where(p => p.Value.Tile.IsEqual(tile)).ToList();
            foreach (var match in matches)
                RemoveNodeInternal(match.Key);
        }

        public void RemoveAll()
        {
            foreach (var placement in InternalNodes.ToArray())
                RemoveNodeInternal(placement.Key);
        }

        #region Helper

        public Color PullColor = Color.white;
        void OnDrawGizmosSelected()
        {
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(Grid.transform.position, Grid.transform.rotation, Vector3.one * Grid.Unit /*Grid.transform.lossyScale*/);
            Gizmos.matrix = rotationMatrix;

            if (HideGridOutlineRenderer)
                return;

            Gizmos.color = PullColor;
            Gizmos.DrawWireCube(LocalHoverPosition, Vector3.one);
        }

        #endregion

        #region hotkeys

        void OnGUI()
        {
            var e = Event.current;
            if (e?.isKey == true)
            {
                if(e.type == EventType.KeyDown)
                {
                    HotKeySelection(e.keyCode);
                }
            }
        }

        public bool HotKeySelection(KeyCode keycode)
        {
            switch (keycode)
            {
                case KeyCode.Alpha1:
                    return TryHotkey(0);
                case KeyCode.Alpha2:
                    return TryHotkey(1);
                case KeyCode.Alpha3:
                    return TryHotkey(2);
                case KeyCode.Alpha4:
                    return TryHotkey(3);
                case KeyCode.Alpha5:
                    return TryHotkey(4);
                case KeyCode.Alpha6:
                    return TryHotkey(5);
                case KeyCode.Alpha7:
                    return TryHotkey(6);
                case KeyCode.Alpha8:
                    return TryHotkey(7);
                case KeyCode.Alpha9:
                    return TryHotkey(8);
                case KeyCode.Alpha0:
                    return TryHotkey(9);
            }
            return false;
        }

        private bool TryHotkey(int hotkey)
        {
            if (hotkey < Tiles.Count && hotkey >= 0)
            {
                _ActiveTileSelection = hotkey;
                return true;
            }
            return false;
        }
        #endregion

#endif

    }

}