using System;
using System.Collections.Generic;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{

    [Serializable]
    public class MaterialStatus
    {
        public Material material;
        public bool isTarget;
        public BakedProperties preset;

        public MaterialStatus()
        {
            material = null;
            isTarget = true;
            preset = null;
        }

        public MaterialStatus(Material mat)
        {
            material = mat;
            isTarget = true;
            preset = null;
        }
    }

    [Serializable]
    public class MaterialStatusList
    {
        public Renderer _renderer;
        public List<MaterialStatus> _materialStatusList = new List<MaterialStatus>();

        public MaterialStatusList(Renderer ren)
        {
            _renderer = ren;
        }

        public MaterialStatusList()
        {
            _renderer = null;
        }
    }
    
    public class MaterialGroups: MonoBehaviour
    {
        [SerializeField] private BakedProperties _defaultProfile;
        
        // レンダラーのインデックス、マテリアルのインデックス、マテリアルの状態
        [SerializeField] private List<MaterialStatusList> _materialStatusListList = new List<MaterialStatusList>();

        public BakedProperties DefaultProfile
        {
            get => _defaultProfile;
            set => _defaultProfile = value;
        }
        public List<MaterialStatusList> MaterialStatusListList => _materialStatusListList;


        public int GetIndex(int ri, int mi)
        {
            int index = 0;
            for(int i = 0; i < ri; i++)
            {
                var r = _materialStatusListList[i]._renderer;
                if(r == null) continue;
                
                var matNum = r.sharedMaterials.Length;
                index += matNum;
            }

            index += mi;
            return index;
        }
        
        public void SetPropertyBlock(in MaterialPropertyBlock mpb)
        {
            for (int lli = 0; lli < _materialStatusListList.Count; lli++)
            {
                var list = _materialStatusListList[lli];
                var ren = list._renderer;
                for (int li = 0; li < list._materialStatusList.Count; li++)
                {
                    var matStatus = list._materialStatusList[li];
                    if (matStatus.isTarget)
                    {
                        ren.SetPropertyBlock(mpb, li);
                    }
                }
            }
        }

    }
}
