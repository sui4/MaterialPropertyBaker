using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    public class ChildRenderersAdderToMaterialGroup : MonoBehaviour
    {
        [SerializeField] private MaterialGroup _targetMaterialGroup;
        [SerializeField] private GameObject _targetParent;

        public void OnValidate()
        {
            if (_targetMaterialGroup == null || _targetParent == null) return;

            var renderers = _targetParent.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                if (_targetMaterialGroup.Renderers.Contains(renderer)) continue;
                _targetMaterialGroup.Renderers.Add(renderer);
            }

            _targetMaterialGroup.OnValidate();
        }
    }
}