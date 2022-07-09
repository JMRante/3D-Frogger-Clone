using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditorInternal;
#endif

namespace Autotiles3D
{
    public class Autotiles3D_BlockBehaviour : MonoBehaviour
    {
        [HideInInspector] [SerializeField] private Vector3Int _internalPosition;
        [HideInInspector] [SerializeField] private Quaternion _localRotation;
        [HideInInspector] public string TileDisplayName;
        [HideInInspector] public Autotiles3D_Tile Tile;
        [HideInInspector] private int _tileID;

        private Autotiles3D_Grid _grid;
        private Autotiles3D_Anchor _anchor;

        public GameObject View;
        public Autotiles3D_Grid Grid
        {
            get
            {
                if (this == null)
                    return null;
                if (_grid == null)
                    _grid = transform.GetComponentInParent<Autotiles3D_Grid>();
                return _grid;
            }
        }
        public Autotiles3D_Anchor Anchor
        {
            get
            {
                if (this == null)
                    return null;
                if (_anchor == null)
                    _anchor = transform.GetComponentInParent<Autotiles3D_Anchor>();
                return _anchor;
            }
        }
        public int TileID => _tileID;
        public Vector3Int InternalPosition { get => _internalPosition; set => _internalPosition = value; }
        public Quaternion LocalRotation { get => _localRotation; set => _localRotation = value; }

        public void ToggleView(bool enable)
        {
            if (View != null)
                View.SetActive(enable);
        }

        [HideInInspector] [SerializeField] private bool _isBaked;
        public bool IsBaked
        {
            get
            {
                if (Anchor == null)
                    _isBaked = false;
                else if (Anchor.BakedParent == null)
                    _isBaked = false;
                return _isBaked;
            }
            set
            {
                _isBaked = value;
            }
        }
        public void OnInstanceUpdate(Autotiles3D_Tile tile, int tileID, string displayName, Vector3Int internalPosition, Quaternion localRotation)
        {
            this._tileID = tileID;
            this.Tile = tile;
            this.TileDisplayName = displayName;
            this.InternalPosition = internalPosition;
            this.LocalRotation = localRotation;
        }
    }

}


