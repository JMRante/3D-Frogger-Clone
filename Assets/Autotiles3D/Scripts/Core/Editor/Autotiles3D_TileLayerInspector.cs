using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;

namespace Autotiles3D
{
    [CustomEditor(typeof(Autotiles3D_TileLayer))]
    public class Autotiles3D_TileLayerInspector : Editor
    {
        Autotiles3D_TileLayer _tileLayer;
        Autotiles3D_Grid Grid => _tileLayer.Grid;
        public float Unit => Grid.Unit;
        private int _LayerIndex => Grid.LayerIndex;

        private int _TileRotation;
        private bool _WasPreviousShift;

        private int _ControlID = 0;
        private Vector3 _MousePositionGUI;
        private Ray _MouseRay;
        private const int _sp1 = 120;
        private const int _sp2 = 150;

        private bool _ResetHover;
        private bool _HasRenderedHover;

        enum PullMode
        {
            Face,
            Plane
        }
        private PullMode _PullMode;

        private bool _OutOfBounds;
        private List<TilePreviewData> _PreviewData = new List<TilePreviewData>();
        public class TilePreviewData
        {
            public Type Type;
            public string ID;
            public Texture2D Thumbnail;
            public TilePreviewData(Type type, string id, Texture2D thumbnail)
            {
                Type = type;
                ID = id;
                Thumbnail = thumbnail;
            }
        }
        private class DynamicPlaceData
        {
            public Vector3Int InternalPos;
            public Quaternion InternalRot;
            public Autotiles3D_Tile Tile;
            public DynamicPlaceData(Vector3Int internalPos, Quaternion internalRot, Autotiles3D_Tile tile)
            {
                this.InternalPos = internalPos;
                this.InternalRot = internalRot;
                this.Tile = tile;
            }
        }

        private List<DynamicPlaceData> _place = new List<DynamicPlaceData>();
        private List<Vector3Int> _unplace = new List<Vector3Int>();

        //Push Pull
        private List<InternalNode> _ppNodes = new List<InternalNode>();
        private List<Vector3Int> _cantDelete = new List<Vector3Int>();
        private Dictionary<Vector3Int, DynamicPlaceData> _canBuild = new Dictionary<Vector3Int, DynamicPlaceData>();
        private Vector3Int _faceNormalInternal;
        private Vector3 _faceNormalWorld , _ppOffset;
        private Plane _ppPlane;
        private bool _isPulling, _isPushing, _ShiftLock;
        private int _pIndex;

        //gridraycast 
        public List<Vector3> visits = new List<Vector3>(); //(used only for debugging)

        private void OnEnable()
        {
            _tileLayer = (Autotiles3D_TileLayer)target;
            _tileLayer.Grid = _tileLayer.GetComponentInParent<Autotiles3D_Grid>();
            Autotiles3D_TileLayer.IS_EDITING = true;

            if (!Application.isPlaying)
            {
                foreach (Transform t in _tileLayer.transform)
                {
                    if (t.gameObject.name == "HoverInstance")
                        DestroyImmediate(t.gameObject);
                }
            }

            _tileLayer.LoadedGroups = Autotiles3D_SettingsWindow.LoadTileGroups();

            if (_tileLayer.Group == null && _tileLayer.LoadedGroups.Count > 0)
                _tileLayer.Group = _tileLayer.LoadedGroups[0];

            FetchThumbnails();


            Tools.hidden = true;
        }
        private void OnDisable()
        {
            _tileLayer.DestroyHoverInstance();
            Tools.hidden = false;
        }
        public void OnMouseEnterSceneWindow()
        {
        }
        public void OnMouseExitSceneWindow()
        {
            _tileLayer.DestroyHoverInstance();
        }
        public void  DrawPlane(Vector3 position, Vector3 normal)
        {

            Vector3 v3;

            if (normal.normalized != Vector3.forward)
                v3 = Vector3.Cross(normal, Vector3.forward).normalized * normal.magnitude;
            else
                v3 = Vector3.Cross(normal, Vector3.up).normalized * normal.magnitude; ;

            var corner0 = position + v3;
            var corner2 = position - v3;
            var q = Quaternion.AngleAxis(90.0f, normal);
            v3 = q * v3;
            var corner1 = position + v3;
            var corner3 = position - v3;

            Debug.DrawLine(corner0, corner2, Color.green,0.01f);
            Debug.DrawLine(corner1, corner3, Color.green,0.01f);
            Debug.DrawLine(corner0, corner1, Color.green,0.01f);
            Debug.DrawLine(corner1, corner2, Color.green,0.01f);
            Debug.DrawLine(corner2, corner3, Color.green,0.01f);
            Debug.DrawLine(corner3, corner0, Color.green, 0.01f);
            Debug.DrawRay(position, normal, Color.red);
        }
        private void OnSceneGUI()
        {
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(Grid.transform.position, Grid.transform.rotation, Vector3.one * Unit);
            Handles.matrix = rotationMatrix;

            _ResetHover = false;
            _HasRenderedHover = false;
            _tileLayer.transform.localPosition = Vector3.zero;
            _tileLayer.transform.localRotation = Quaternion.identity;

            //render layer outline
            if (Autotiles3D_TileLayer.SHOW_LAYER_OUTLINE)
            {
                foreach (var internalPosition in _tileLayer.InternalNodes.Keys)
                {
                    Handles.DrawWireCube(internalPosition, Vector3.one);
                }
            }

            if (!Autotiles3D_TileLayer.IS_EDITING)
                return;

            _ControlID = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive);
            _MousePositionGUI = Event.current.mousePosition;
            _MouseRay = HandleUtility.GUIPointToWorldRay(_MousePositionGUI);
            Event e = Event.current;

            if (e.type == EventType.MouseLeaveWindow)
                _tileLayer.DestroyHoverInstance();

            if (Tools.hidden)
                Tools.hidden = true;

            if (e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(_ControlID);
            }
            if (e.type == EventType.ScrollWheel)
            {
                int scroll = (e.delta.y > 0) ? -1 : 1;
                if (e.alt)
                {
                    Grid.SetLayerIndex(_LayerIndex + scroll);
                    Event.current.Use();
                }
                else if (e.control)
                {
                    int delta = (scroll > 0) ? 90 : -90;
                    _TileRotation += delta;
                    _ResetHover = true;
                    Event.current.Use();
                }
                else if (e.shift)
                {
                }
            }

            if (Grid.GridSize == LevelSize.Finite)
                Grid.SetLayerIndex(Mathf.Clamp(_LayerIndex, 0, Grid.Height - 1));

            Grid.DrawLevelGrid(_ControlID);
            DrawHoverSurroundGrid(_tileLayer.LocalHoverPosition);

            //Grid Selection Calculation
            var normal = Grid.transform.TransformDirection(Vector3Int.up).normalized;
            var planeposition = Grid.transform.TransformPoint(new Vector3(0, (_LayerIndex-0.0f) * Unit , 0));
            Plane plane = new Plane(normal, planeposition);
            //DrawPlane(planeposition, normal);

            plane.Raycast(_MouseRay, out float distance);
            Vector3 worldHit = _MouseRay.GetPoint(distance);

            //_TileLayer.LocalHoverPosition = Vector3Int.RoundToInt(Grid.transform.InverseTransformPoint(worldHit /** Unit*/));
            _tileLayer.LocalHoverPosition = Vector3Int.RoundToInt(Grid.transform.InverseTransformPoint(worldHit) / Unit);

            if (e.shift && (!_isPulling && !_isPushing))
            {
                bool succesfullHit = false;
                if (_tileLayer.InternalNodes.Count > 0)
                {
                    if (Autotiles3D_GridRaycast.GridRayCast(_tileLayer, _MouseRay.origin, _MouseRay.GetPoint(50), out Vector3Int internalHit, out Vector3Int internalHitNormal, out visits))
                    {
                        if (_tileLayer.InternalNodes.ContainsKey(internalHit))
                        {
                            Autotiles3D_BlockBehaviour block = _tileLayer.InternalNodes[internalHit].Block;

                            if (block == null)
                                return;
                            if (block.gameObject.name == "HoverInstance")
                                return;

                            succesfullHit = true;
                            _ppNodes.Clear();
                            _faceNormalInternal = internalHitNormal;
                            _faceNormalWorld = Grid.ToWorldDirection(_faceNormalInternal);

                            if (_faceNormalInternal != Vector3.up && _faceNormalInternal != Vector3.down && _faceNormalInternal != Vector3.right && _faceNormalInternal != Vector3.left && _faceNormalInternal != Vector3.forward && _faceNormalInternal != Vector3.back)
                            {
                                Debug.LogError("not aligned facenormal : " + _faceNormalInternal);
                            }

                            //render face/plane selection
                            if (e.type == EventType.ScrollWheel)
                            {
                                _PullMode = (PullMode)1 - (int)_PullMode;
                                Event.current.Use();
                            }
                            if(e.isMouse && e.button == 2)
                            {
                                Grid.SetLayerIndex(internalHit.y);
                            }

                            if (_PullMode == PullMode.Face)
                            {
                                if (_tileLayer.InternalNodes.ContainsKey(internalHit))
                                {
                                    Autotiles3D_Tile tile = _tileLayer.InternalNodes[internalHit].Tile;
                                    if (tile != null)
                                    {
                                        _ppNodes.Add(_tileLayer.InternalNodes[internalHit]);
                                        Handles.DrawWireDisc(_ppNodes[0].InternalPosition + (Vector3)_faceNormalInternal * 0.5f, _faceNormalInternal, 0.5f);
                                    }
                                }
                            }
                            else if (_PullMode == PullMode.Plane)
                            {
                                //getall neighbors in plane
                                var nodes = Autotiles3D_EditorUtility.GetAllUnblockedContAdjacentNodesDepthFirst(_tileLayer, internalHit, _faceNormalInternal).ToList();
                                foreach (var node in nodes)
                                {
                                    if (_tileLayer.InternalNodes.ContainsKey(node))
                                    {
                                        Autotiles3D_Tile tile = _tileLayer.InternalNodes[node].Tile;
                                        if (tile != null)
                                        {
                                            _ppNodes.Add(_tileLayer.InternalNodes[node]);
                                            Handles.DrawWireDisc(node + (Vector3)_faceNormalInternal * 0.5f, _faceNormalInternal, 0.5f);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Debug.LogError("Autotiles: InternalNode GridRaycast went wrong.");
                        }
                    }
                }

                if (e.type == EventType.MouseDown)
                {
                    if (e.button == 0 || e.button == 1 && _ppNodes.Count > 0)
                    {
                        _ShiftLock = true;
                        if (succesfullHit)
                        {
                            Vector3 zeroInternalPosition = _ppNodes[0].InternalPosition;
                            Vector3 perpendicular = Vector3.Cross(_faceNormalWorld, _MouseRay.origin - Grid.transform.TransformPoint(zeroInternalPosition));
                            _ppPlane = new Plane(Grid.transform.TransformPoint(zeroInternalPosition), Grid.transform.TransformPoint(zeroInternalPosition + _faceNormalInternal), Grid.transform.TransformPoint(zeroInternalPosition) + perpendicular);
                            _ppPlane.Raycast(_MouseRay, out float enter);
                            var pullHit = _MouseRay.GetPoint(enter);
                            _ppOffset = Vector3.Project(pullHit, _faceNormalWorld);

                            if (e.button == 0)
                            {
                                _isPulling = true;
                                _tileLayer.PullColor = Color.blue;
                            }
                            else if (e.button == 1)
                            {
                                _isPushing = true;
                                _tileLayer.PullColor = Color.red;
                            }
                            _pIndex = 0;
                            _cantDelete.Clear();
                            _canBuild.Clear();
                            if (_tileLayer.TempCachedInstances.Count > 0)
                                DestroyTempInstances();
                            _tileLayer.TempCachedInstances.Clear();

                            Autotiles3D_HierarchyChange.IsLocked = true;

                            e.Use();
                        }
                    }

                }
            }

            if (!e.shift && _WasPreviousShift)
            {
                _ResetHover = true;
                if (_tileLayer.TempCachedInstances.Count > 0)
                    DestroyTempInstances();
            }

            if (_isPulling || _isPushing)
            {
                _ppPlane.Raycast(_MouseRay, out float enter);
                var ppWorldHit = _MouseRay.GetPoint(enter);
                //make sure user can only pull "positive" face for pull, "negative" face for push
                var ppDelta = Vector3.Project(ppWorldHit, _faceNormalWorld);

                int index = 0;
                if (_isPulling)
                {
                    if (Vector3.Dot(Vector3.Project(ppWorldHit, _faceNormalWorld) - _ppOffset, _faceNormalWorld) >= 0)
                    {
                        ppDelta -= _ppOffset;
                        index = (int)((ppDelta.magnitude / Unit) +0.5f);
                    }
                }
                else if (_isPushing)
                {
                    if (Vector3.Dot(Vector3.Project(ppWorldHit, _faceNormalWorld) - _ppOffset, _faceNormalWorld) <= 0)
                    {
                        ppDelta -= _ppOffset;
                        index = (int)((ppDelta.magnitude / Unit) +0.5f);
                    }
                }

                bool hasIndexChanged = false;
                if (_pIndex != index)
                    hasIndexChanged = true;

                if (hasIndexChanged)
                {
                    _place.Clear();
                    _unplace.Clear();

                    for (int i = 0; i < _ppNodes.Count; i++)
                    {
                        if (_isPulling)
                        {
                            if (index > _pIndex)
                            {
                                for (int j = _pIndex + 1; j <= index; j++)
                                {
                                    var pos = _ppNodes[i].InternalPosition + _faceNormalInternal * j;
                                    if (!Grid.IsExceedingLevelGrid(pos))
                                    {
                                        if (_tileLayer.InternalNodes.ContainsKey(pos))
                                        {
                                            if (!_cantDelete.Contains(pos))
                                                _cantDelete.Add(pos);
                                            break;
                                        }
                                        else
                                        {
                                            _place.Add(new DynamicPlaceData(pos, _ppNodes[i].LocalRotation, _ppNodes[i].Tile));
                                        }
                                    }

                                }
                            }
                            else
                            {
                                for (int j = _pIndex; j > index; j--)
                                {
                                    var pos = _ppNodes[i].InternalPosition + _faceNormalInternal * j;
                                    if (!Grid.IsExceedingLevelGrid(pos))
                                    {
                                        if (!_cantDelete.Contains(pos))
                                        {
                                            _unplace.Add(pos);
                                        }

                                    }
                                }
                            }
                        }
                        else if (_isPushing)
                        {
                            if (index > _pIndex)
                            {
                                for (int j = _pIndex; j < index; j++)
                                {
                                    var pos = _ppNodes[i].InternalPosition - _faceNormalInternal * j;
                                    if (!Grid.IsExceedingLevelGrid(pos))
                                    {
                                        if (_tileLayer.InternalNodes.ContainsKey(pos))
                                        {
                                            //make sure baked nodes cant be unplaced or removed
                                            if (!_tileLayer.InternalNodes[pos].Block.IsBaked)
                                            {
                                                _unplace.Add(pos);
                                                if (!_canBuild.ContainsKey(pos))
                                                {
                                                    _canBuild.Add(pos, new DynamicPlaceData(pos, _tileLayer.InternalNodes[pos].LocalRotation, _tileLayer.InternalNodes[pos].Tile));
                                                }
                                            }
                                            else
                                            {
                                                Debug.Log("Autotiles: Will not remove baked blocks");
                                            }
                                        }

                                    }
                                }
                            }
                            {
                                for (int j = _pIndex; j >= index; j--)
                                {
                                    var pos = _ppNodes[i].InternalPosition - _faceNormalInternal * j;
                                    if (!Grid.IsExceedingLevelGrid(pos))
                                    {
                                        if (_canBuild.ContainsKey(pos))
                                        {
                                            _place.Add(new DynamicPlaceData(pos, _canBuild[pos].InternalRot, _canBuild[pos].Tile));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (_place.Count > 0)
                    {
                        _tileLayer.TryPlacementMany(_place.Select(p => p.InternalPos).ToList(), _place.Select(p => p.InternalRot).ToList() , _place.Select(p => p.Tile).ToList());

                    }

                    if (_unplace.Count > 0)
                    {
                        _tileLayer.TryUnplacingMany(_unplace, waitForDestroy: true);
                    }
                    EditorUtility.SetDirty(_tileLayer);
                }
                _pIndex = index;

            }
            else
            {
                if (e.isKey)
                {
                    if (_tileLayer.HotKeySelection(e.keyCode))
                    {
                        _ResetHover = true;
                        e.Use();
                    }
                    if (e.keyCode == KeyCode.Escape)
                        ExitEditingMode();
                }

                if (!_ShiftLock)
                {
                    if (!e.shift)
                    {
                        RenderHoverInstance(_tileLayer.LocalHoverPosition, Quaternion.AngleAxis(_TileRotation, Vector3.up));
                    }

                    if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag || e.type == EventType.MouseUp)
                    {
                        GridSelection(e.type, e, _tileLayer.LocalHoverPosition);
                        Grid.OnGridSelection?.Invoke(e.type, e, _tileLayer.LocalHoverPosition);
                    }
                }
                _tileLayer.PullColor = Color.white;
                Autotiles3D_HierarchyChange.IsLocked = false;
            }

            if (e.type == EventType.MouseUp)
            {
                _ShiftLock = false;
                if (e.button == 0)
                    _isPulling = false;
                if (e.button == 1)
                    _isPushing = false;
            }

            if (e.type == EventType.MouseEnterWindow)
            {
                OnMouseEnterSceneWindow();
            }
            else if (e.type == EventType.MouseLeaveWindow)
            {
                OnMouseExitSceneWindow();
            }

            if (!_HasRenderedHover)
                _tileLayer.DestroyHoverInstance();

            // REFRESHING ANY CHANGES MADE TO INTERNAL NODES
            if (_tileLayer.NodeRefreshs.Count > 0)
                _tileLayer.Refresh();

            _WasPreviousShift = e.shift;
            _tileLayer.PrevLocalHoverPosition = _tileLayer.LocalHoverPosition;

            _tileLayer.HideGridOutlineRenderer = (e.shift || (!_isPulling && !_isPushing)) ? true : false;


            if (Autotiles3D_TileLayer.SHOW_HOVER_GIZMO)
            {
                if (_tileLayer.HoverInstance.instance != null)
                {
                    //revert handle matrix
                    Handles.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * Unit );
                    var position = _tileLayer.HoverInstance.instance.transform.position;
                    float size = 0.5f * Unit;
                    Color cache = Handles.color;
                    Handles.color = Color.red;
                    Handles.ArrowHandleCap(_ControlID, position,  Quaternion.LookRotation(_tileLayer.HoverInstance.instance.transform.right), size, EventType.Repaint);
                    Handles.color = Color.green;
                    Handles.ArrowHandleCap(_ControlID, position, Quaternion.LookRotation(_tileLayer.HoverInstance.instance.transform.up), size, EventType.Repaint);
                    Handles.color = Color.blue;
                    Handles.ArrowHandleCap(_ControlID, position, Quaternion.LookRotation(_tileLayer.HoverInstance.instance.transform.forward), size, EventType.Repaint);
                    Handles.color = cache;
                }
            }
        }

        private void DestroyTempInstances()
        {
            foreach (var instance in _tileLayer.TempCachedInstances)
            {
                if (Autotiles3D_Settings.EditorInstance.DontRegisterUndo)
                {
                    GameObject.DestroyImmediate(instance.Value);
                }
                else
                {
                    Undo.RegisterCompleteObjectUndo(_tileLayer, "Destroy Temp");
                    instance.Value.SetActive(true);
                    Undo.DestroyObjectImmediate(instance.Value);
                }
            }
            _tileLayer.TempCachedInstances.Clear();
        }
       

        public static GUIStyle RichStyle
        {
            get
            {
                var style = new GUIStyle(GUI.skin.label);
                style.wordWrap = true;
                style.richText = true;
                style.normal.textColor = Color.white;
                style.alignment = TextAnchor.MiddleLeft;
                return style;
            }
        }

        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            FetchThumbnails();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
      
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Layer Name", GUILayout.Width(_sp1));
            string displayName = EditorGUILayout.DelayedTextField( _tileLayer.LayerName, GUILayout.Width(_sp1));
            EditorGUIUtility.labelWidth = 0;
            if (displayName != _tileLayer.LayerName)
            {
                _tileLayer.gameObject.name = "Tile Layer: " + displayName;
                _tileLayer.LayerName = displayName;
                FetchThumbnails();
            }

            if (GUILayout.Button("Refresh", GUILayout.Width(_sp1)))
            {
                foreach (var anchor in _tileLayer.Anchors)
                {
                    _tileLayer.RefreshAll(anchor.Value);
                }
            }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();


            if (Autotiles3D_TileLayer.SHOW_HOVER_GIZMO)
            {
                if (GUILayout.Button("Hide Hover Gizmo", GUILayout.Width(_sp1)))
                {
                    Autotiles3D_TileLayer.SHOW_HOVER_GIZMO = false;
                }
            }
            else
            {
                if (GUILayout.Button("Show Hover Gizmo", GUILayout.Width(_sp1)))
                {
                    Autotiles3D_TileLayer.SHOW_HOVER_GIZMO = true;
                }
            }
            if (Autotiles3D_TileLayer.SHOW_LAYER_OUTLINE)
            {
                if (GUILayout.Button("Hide Outline", GUILayout.Width(_sp1)))
                {
                    Autotiles3D_TileLayer.SHOW_LAYER_OUTLINE = false;
                }
            }
            else
            {
                if (GUILayout.Button("Show Outline",GUILayout.Width(_sp1)))
                {
                    Autotiles3D_TileLayer.SHOW_LAYER_OUTLINE = true;
                }
            }
            if (!Autotiles3D_TileLayer.IS_EDITING)
            {
                if (GUILayout.Button("Enter Edit Mode", GUILayout.Width(_sp1)))
                    Autotiles3D_TileLayer.IS_EDITING = true;
            }
            else
            {
                if (GUILayout.Button("Exit Edit Mode", GUILayout.Width(_sp1)))
                    ExitEditingMode();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();


            bool hasClickedDialogueBox = false;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                var groupNames = _tileLayer.LoadedGroups.Select(g => g.name).ToList();
                var activeGroupName = _tileLayer.Group != null ? _tileLayer.Group.name : "";
                int index = Math.Max(groupNames.IndexOf(activeGroupName), 0);
                EditorGUILayout.BeginHorizontal();
                GUILayout.ExpandWidth(false);
                int newIndex = EditorGUILayout.Popup(index, groupNames.ToArray(),GUILayout.Width(_sp1));
                if (index != newIndex)
                {
                    _tileLayer.Group = _tileLayer.LoadedGroups[newIndex];
                    FetchThumbnails();
                }
                if (GUILayout.Button("Show", GUILayout.Width(_sp1)))
                {
                    Selection.activeObject = _tileLayer.Group;
                }
                if (GUILayout.Button("Settings", GUILayout.Width(_sp1)))
                {
                    EditorWindow.GetWindow<Autotiles3D_SettingsWindow>("Autotiles 3D Settings", typeof(SceneView));
                }
                EditorGUILayout.EndHorizontal();


                #region Search debug of internal nodes. Use if neeed

                /*
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                _TileLayer.SearchedInternalPosition = EditorGUILayout.Vector3IntField("Search Layer", _TileLayer.SearchedInternalPosition);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                _TileLayer.SearchedInternalNode = null;
                if (_TileLayer.InternalNodes.ContainsKey(_TileLayer.SearchedInternalPosition))
                    _TileLayer.SearchedInternalNode = _TileLayer.InternalNodes[_TileLayer.SearchedInternalPosition];

                if (_TileLayer.SearchedInternalNode == null)
                    EditorGUILayout.LabelField("Not existing");
                else
                {
                    EditorGUILayout.LabelField($"{_TileLayer.SearchedInternalNode.Tile.DisplayName}");
                    if (_TileLayer.SearchedInternalNode.Instance == null)
                        GUI.enabled = false;
                    if (GUILayout.Button("Ping"))
                        Selection.activeGameObject = _TileLayer.SearchedInternalNode.Instance;
                    GUI.enabled = true;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                */

                #endregion

                foreach (var tile in _tileLayer.Tiles.ToArray())
                {
                    EditorGUILayout.BeginHorizontal();
                    int tileID = tile.TileID;
                    var anchor = _tileLayer.GetAnchor(tileID);
                    GUI.enabled = anchor != null ? true : false;

                    int amount = 0;
                    if (anchor != null)
                    {
                        amount = anchor.Childcount -  anchor.BakeCount;
                        if (amount < 0)
                        {
                            anchor.BakeCount = 0;
                            EditorUtility.SetDirty(anchor);
                        }
                    }

                    EditorGUILayout.LabelField($" {amount}  <color=yellow> {tile.DisplayName} </color>", RichStyle);//, GUILayout.Width(_sp2));
                

                    GUILayout.FlexibleSpace();

                    if (_tileLayer.Anchors.ContainsKey(tileID))
                    {
                        if (GUILayout.Button("Hide All"))
                        {
                            _tileLayer.Anchors[tileID].ToggleViews(false);
                        }
                        if (GUILayout.Button("Show All"))
                        {
                            _tileLayer.Anchors[tileID].ToggleViews(true);
                        }
                    }

                    
                    string msg = "Bake";
                    if (anchor != null)
                    {
                        if (anchor.BakedParent != null)
                        {
                            msg = "Rebake";
                            if (GUILayout.Button("Clear bake"))
                            {
                                if (!Application.isPlaying)
                                {
                                    Undo.RegisterCompleteObjectUndo(_tileLayer, "Clear Bake");
                                    Undo.DestroyObjectImmediate(anchor.BakedParent);
                                }

                                anchor.Blocks.ForEach((b) => b.IsBaked = false);

                                //refresh all nodes
                                _tileLayer.RefreshAll(anchor);

                                //turn all blocks visible
                                anchor.ToggleViews(true, true);
                            }
                        }
                    }
                    if (GUILayout.Button(msg))
                    {
                        if (EditorUtility.DisplayDialog($"Bake all {tile.DisplayName} meshes?", $"Do you want to bake all {tile.DisplayName} meshes into a single one? \nAll baked meshes will be disabled, but not deleted. You can always re-enable them later or even rebake your meshes after more changes to the layer have been made.", "Yes", "No"))
                        {
                            hasClickedDialogueBox = true;
                            if (anchor.BakedParent != null)
                            {
                                Undo.RegisterCompleteObjectUndo(_tileLayer, "Bake");
                                Undo.DestroyObjectImmediate(anchor.BakedParent);
                            }

                            _tileLayer.VerifyAnchors();
                            //force refresh all nodes once (since neighbors could have changed since last bake)
                            _tileLayer.RefreshAll(anchor, forceImmediateRefresh: true);
                            _tileLayer.VerifyNodes();


                            if (anchor != null && anchor.Childcount > 0)
                            {
                                anchor.BakedParent = new GameObject("BakedParent");
                                anchor.BakedParent.transform.SetParent(anchor.transform);
                                anchor.BakedParent.transform.SetSiblingIndex(0);

                                string path = "Assets/Autotiles3D/Content/CombinedMeshes";
                                Autotiles3D_MeshCombiner.CombineMeshes(anchor.Blocks.Select(c => c.gameObject).ToList(), ref anchor.BakedParent, path);

                                //disable view of backed blocks
                                foreach (var block in anchor.Blocks)
                                {
                                    if (block != null)
                                    {
                                        block.View.SetActive(false);
                                        block.IsBaked = true;
                                    }
                                }

                                anchor.UpdateBakeCount();
                            }
                        }
                    }
                    if (GUILayout.Button("Clear All"))
                    {
                        _tileLayer.RemoveAllBlocks(tile);
                    }

                    if(!hasClickedDialogueBox)
                        EditorGUILayout.EndHorizontal();
                    GUI.enabled = true;

                    if (amount >= 500)
                    {
                        EditorGUILayout.LabelField($"<color=red> {amount} </color> <color=yellow> {tile.DisplayName} </color> Tiles. This amount is very high! Consider", RichStyle, GUILayout.Width(_sp2));
                        EditorGUILayout.LabelField($"a) <color=red>reducing </color> amount of tiles, \nb)<color=red> baking </color> meshes or \nc) working with <color=red> multiple layers </color> to improve perfomance.",RichStyle, GUILayout.Width(_sp2) );
                        GUILayout.Space(EditorGUIUtility.singleLineHeight);
                    }
                }
            }
            if(!hasClickedDialogueBox)
                EditorGUILayout.EndVertical();

            MiniProjectSelection(ref _tileLayer._ActiveTileSelection, _PreviewData.Select(d => d.ID).ToList(), _PreviewData.Select(d => d.Thumbnail).ToList());
            serializedObject.ApplyModifiedProperties();
        }


        private void ExitEditingMode()
        {
            Autotiles3D_TileLayer.IS_EDITING = false;
            _tileLayer.DestroyHoverInstance();
            OnInspectorGUI();
        }

    
        private void DrawHoverSurroundGrid(Vector3 pos)
        {
            //might be useful, use if needed
            /*
            Vector3 a, b;
            pos = pos + new Vector3(-0.5f, -0.5f, -0.5f);
            Vector3[] rf = { Vector3.right, Vector3.forward };
            for (int i = 0; i < 2; i++)
            {
                a = pos - rf[i];
                b = pos + 2 * rf[i];
                Handles.DrawLine(a, b);
                Handles.DrawLine(a + rf[1 - i], b + rf[1 - i]);
            }
            */
        }

        public void RenderHoverInstance(Vector3Int internalPosition, Quaternion localRotation)
        {
            if (_tileLayer.ActiveTile == null)
            {
                _tileLayer.DestroyHoverInstance();
                return;
            }

            if (_tileLayer.InternalNodes.ContainsKey(internalPosition))
            {
                _tileLayer.DestroyHoverInstance();
                Handles.DrawWireCube(internalPosition, Vector3.one);
                return;
            }

            _OutOfBounds = Grid.GridSize == LevelSize.Infinite ? false : Grid.IsExceedingLevelGrid(internalPosition);

            if (_tileLayer.PrevLocalHoverPosition != internalPosition || _ResetHover)
            {
                GameObject prefab = _tileLayer.ActiveTile.Default;
                
                var rule = _tileLayer.ActiveTile.GetRule(_tileLayer.GetNeighborsBoolSelfSpace(internalPosition, localRotation), out int[] addedRotation);
                if (rule != null)
                    prefab = rule.Object;
                

                if (_OutOfBounds)
                {
                    _tileLayer.DestroyHoverInstance();
                }
                else if (prefab != null)
                {
                    if (_tileLayer.HoverPrefabObject != prefab || _tileLayer.HoverInstance.instance == null)
                    {
                        _tileLayer.DestroyHoverInstance();
                        _tileLayer.HoverInstance = (_tileLayer.ActiveTile, PrefabUtility.InstantiatePrefab(prefab, _tileLayer.transform) as UnityEngine.GameObject);
                        _tileLayer.HoverInstance.instance.gameObject.name = "HoverInstance";
                        _tileLayer.HoverPrefabObject = prefab;
                    }
                    _tileLayer.HoverInstance.instance.transform.position = Grid.ToWorldPoint((Vector3)internalPosition * Grid.Unit);
                    _tileLayer.HoverInstance.instance.gameObject.layer = 1 << 0;

                    if(addedRotation[0] > -1)
                    {
                        Vector3 axis = Vector3.right;
                        if (addedRotation[0] == 1)
                            axis = Vector3.up;
                        else if (addedRotation[0] == 2)
                            axis = Vector3.forward;
                        localRotation *= Quaternion.AngleAxis(addedRotation[1], axis);
                    }
                    _tileLayer.HoverInstance.instance.transform.rotation = Grid.transform.rotation * localRotation;
                }
            }

         

            _HasRenderedHover = true;
        }

        public void GridSelection(EventType eventType, Event e, Vector3Int internalPosition)
        {
            if (_OutOfBounds)
                return;

            if (!e.alt && !e.control)
            {
                if (!e.shift)
                {
                    if (eventType == EventType.MouseDown || eventType == EventType.MouseDrag)
                    {
                        if (e.button == 0)
                        {
                            if (!_tileLayer.InternalNodes.ContainsKey(internalPosition))
                            {
                                _tileLayer.TryPlacementSingle(internalPosition, Quaternion.AngleAxis(_TileRotation, Vector3.up));
                            }
                            e.Use();
                        }
                        else if (e.button == 1)
                        {
                            if (_tileLayer.InternalNodes.ContainsKey(internalPosition))
                                _tileLayer.TryUnplacingSingle(internalPosition);
                            e.Use();
                        }
                    }
                }
                else
                {

                }
            }
        }
        public void MiniProjectSelection(ref int selection, List<string> names, List<Texture2D> thumbnails)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            List<GUIContent> contents = new List<GUIContent>();
            for (int i = 0; i < names.Count; i++)
            {
                contents.Add(new GUIContent(names[i], thumbnails[i]));
            }

            GUIStyle contentStyle = new GUIStyle();
            contentStyle.imagePosition = ImagePosition.ImageAbove;
            contentStyle.clipping = TextClipping.Clip;
            contentStyle.wordWrap = false;
            contentStyle.margin = new RectOffset(7, 7, 0, 0);
            contentStyle.alignment = TextAnchor.MiddleCenter;
            contentStyle.onHover.textColor = Color.black;
            contentStyle.onNormal.textColor = Color.green;


            selection = GUILayout.SelectionGrid(selection, contents.ToArray(), 5, contentStyle, GUILayout.Height(150));
            EditorGUILayout.EndVertical();
        }

        private void FetchThumbnails()
        {
            _PreviewData = new List<TilePreviewData>();
            foreach (var tile in _tileLayer.Tiles)
            {
                Texture2D tex = Resources.Load("Icons/cube") as Texture2D;
                if (tile.Default != null)
                    tex = AssetPreview.GetAssetPreview(tile.Default);

                _PreviewData.Add(new TilePreviewData(tile.GetType(), tile.DisplayName, tex));
            }
        }
    }
}