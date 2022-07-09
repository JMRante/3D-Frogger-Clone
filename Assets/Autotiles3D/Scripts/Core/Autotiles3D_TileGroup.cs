using System.Collections.Generic;
using UnityEngine;

namespace Autotiles3D
{
    [CreateAssetMenu(menuName = "Autotiles3D/TileGroup")]
    public class Autotiles3D_TileGroup : ScriptableObject
    {
        public List<Autotiles3D_Tile> Tiles = new List<Autotiles3D_Tile>();
    }

}