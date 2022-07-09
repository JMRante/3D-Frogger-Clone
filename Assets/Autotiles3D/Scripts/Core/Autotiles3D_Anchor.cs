using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace Autotiles3D
{
    public class Autotiles3D_Anchor : MonoBehaviour
    {
        [HideInInspector] public int TileID;

        public List<Autotiles3D_BlockBehaviour> Blocks = new List<Autotiles3D_BlockBehaviour>();

        public GameObject BakedParent;
        public int Childcount => Blocks.Count;
        public int BakeCount;

        public void ToggleViews(bool enable, bool includeBaked = false)
        {
            foreach (var block in Blocks)
            {
                if (block.View == null)
                    continue;
                if (enable)
                {
                    if(includeBaked)
                        block.View.SetActive(true);
                    else if(!block.IsBaked)
                        block.View.SetActive(true);
                }
                else
                {
                    if (includeBaked)
                        block.View.SetActive(false);
                    else if (!block.IsBaked)
                        block.View.SetActive(false);
                }
            }
        }

        public void UpdateBakeCount()
        {
             BakeCount =  Blocks.Where(b => b.IsBaked).ToList().Count;
        }

    }
}

