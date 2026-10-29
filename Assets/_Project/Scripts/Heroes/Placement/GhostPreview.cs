using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FateBastion.Heroes
{
    /// <summary>
    /// Placement preview (S4): a see-through copy of the hero's model plus a ring at its attack range, green when the
    /// spot is valid and red when not. Ghosts are built from the renderers of each deck hero's prefab while the match
    /// loads (no behaviour, no collider), so showing one never instantiates.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GhostPreview : MonoBehaviour
    {
        private const int RingSegments = 64;
        private const string UrpUnlitShader = "Universal Render Pipeline/Unlit";
        private const string FallbackShader = "Sprites/Default";

        [Tooltip("Transparent material for the ghost body. Empty = a transparent URP Unlit material made at load.")]
        [SerializeField] private Material _ghostMaterial;

        [Tooltip("Material of the range ring. Empty = same kind of material as the ghost.")]
        [SerializeField] private Material _ringMaterial;

        [Tooltip("Hero_Base (A): model used when a HeroData has no prefab yet.")]
        [SerializeField] private HeroController _fallbackPrefab;

        [SerializeField] private Color _validColor = new Color(0.35f, 1f, 0.45f, 0.45f);
        [SerializeField] private Color _invalidColor = new Color(1f, 0.3f, 0.25f, 0.45f);

        [Tooltip("Ring line width in metres.")]
        [SerializeField, Min(0.005f)] private float _ringWidth = 0.06f;

        [Tooltip("Ring height above the ground, to avoid z-fighting.")]
        [SerializeField, Min(0f)] private float _ringLift = 0.05f;

        private readonly Dictionary<HeroData, GameObject> _ghosts = new Dictionary<HeroData, GameObject>();
        private readonly Vector3[] _ringPoints = new Vector3[RingSegments];
        private Transform _root;
        private Material _bodyInstance;
        private Material _ringInstance;
        private LineRenderer _ring;
        private GameObject _current;
        private Mesh _capsuleMesh;
        private bool _hasValidity;
        private bool _lastValid;
        private float _lastRange = -1f;

        public bool IsVisible => _current != null && _current.activeSelf;

        /// <summary>For tests and scenes built in code.</summary>
        public void Configure(HeroController fallbackPrefab, Material ghostMaterial = null)
        {
            _fallbackPrefab = fallbackPrefab;
            _ghostMaterial = ghostMaterial;
        }

        /// <summary>Builds a ghost for every hero of the deck (load time).</summary>
        public void Prewarm(IReadOnlyList<HeroData> heroes)
        {
            EnsureRoot();
            if (heroes == null) return;

            for (int i = 0; i < heroes.Count; i++)
            {
                HeroData data = heroes[i];
                if (data != null && !_ghosts.ContainsKey(data)) _ghosts.Add(data, BuildGhost(data));
            }
        }

        /// <summary>Shows the ghost of <paramref name="data"/>; call <see cref="UpdatePose"/> each frame after.</summary>
        public void Show(HeroData data)
        {
            EnsureRoot();
            if (data == null) return;

            if (!_ghosts.TryGetValue(data, out GameObject ghost))
            {
                Debug.LogWarning($"{name}: no prewarmed ghost for '{data.id}'; building one during the match.", this);
                ghost = BuildGhost(data);
                _ghosts.Add(data, ghost);
            }

            if (_current != null && _current != ghost) _current.SetActive(false);
            _current = ghost;
            _hasValidity = false;
            _lastRange = -1f;
        }

        /// <summary>Moves the ghost; colour and ring only change when validity or range changes.</summary>
        public void UpdatePose(Vector3 position, float range, bool valid)
        {
            if (_current == null) return;

            _root.position = position;
            if (!_current.activeSelf) _current.SetActive(true);
            if (!_ring.enabled) _ring.enabled = true;

            if (!_hasValidity || valid != _lastValid)
            {
                Color color = valid ? _validColor : _invalidColor;
                _bodyInstance.color = color;
                _ringInstance.color = color; // URP Unlit ignores vertex colours.
                _ring.startColor = color;
                _ring.endColor = color;
                _lastValid = valid;
                _hasValidity = true;
            }

            if (!Mathf.Approximately(range, _lastRange))
            {
                SetRing(Mathf.Max(0f, range));
                _lastRange = range;
            }
        }

        /// <summary>The pointer is not over the ground: keep the mode, hide the visuals.</summary>
        public void HideVisuals()
        {
            if (_current != null && _current.activeSelf) _current.SetActive(false);
            if (_ring != null && _ring.enabled) _ring.enabled = false;
        }

        public void Hide()
        {
            HideVisuals();
            _current = null;
        }

        private void EnsureRoot()
        {
            if (_root != null) return;

            _root = new GameObject("GhostPreview_Root").transform;
            _root.SetParent(transform, false);

            _bodyInstance = CreateTransparentMaterial(_ghostMaterial, "M_Ghost (runtime)");
            _ringInstance = CreateTransparentMaterial(_ringMaterial != null ? _ringMaterial : _ghostMaterial, "M_GhostRing (runtime)");

            var ringObject = new GameObject("RangeRing");
            ringObject.transform.SetParent(_root, false);
            _ring = ringObject.AddComponent<LineRenderer>();
            _ring.useWorldSpace = false;
            _ring.loop = true;
            _ring.positionCount = RingSegments;
            _ring.widthMultiplier = _ringWidth;
            _ring.sharedMaterial = _ringInstance;
            _ring.shadowCastingMode = ShadowCastingMode.Off;
            _ring.receiveShadows = false;
            _ring.enabled = false;
        }

        private void SetRing(float radius)
        {
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i * (Mathf.PI * 2f / RingSegments);
                _ringPoints[i] = new Vector3(Mathf.Cos(angle) * radius, _ringLift, Mathf.Sin(angle) * radius);
            }

            _ring.SetPositions(_ringPoints);
        }

        private GameObject BuildGhost(HeroData data)
        {
            var ghost = new GameObject("Ghost_" + data.id);
            ghost.transform.SetParent(_root, false);
            ghost.SetActive(false);

            GameObject prefab = data.prefab != null ? data.prefab : _fallbackPrefab != null ? _fallbackPrefab.gameObject : null;
            int copied = prefab != null ? CopyRenderers(prefab.transform, ghost.transform) : 0;
            if (copied == 0) AddCapsule(ghost.transform);
            return ghost;
        }

        // Copies mesh renderers (skinned ones in bind pose) relative to the prefab root.
        private int CopyRenderers(Transform prefabRoot, Transform ghost)
        {
            int copied = 0;
            Matrix4x4 toRoot = prefabRoot.worldToLocalMatrix;
            foreach (Renderer source in prefabRoot.GetComponentsInChildren<Renderer>(true))
            {
                Mesh mesh = null;
                if (source is SkinnedMeshRenderer skinned) mesh = skinned.sharedMesh;
                else if (source is MeshRenderer && source.TryGetComponent(out MeshFilter filter)) mesh = filter.sharedMesh;
                if (mesh == null) continue;

                Matrix4x4 local = toRoot * source.transform.localToWorldMatrix;
                AddMeshPart(ghost, source.name, mesh, local.GetColumn(3), local.rotation, local.lossyScale);
                copied++;
            }

            return copied;
        }

        private void AddCapsule(Transform ghost)
        {
            if (_capsuleMesh == null)
            {
                GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _capsuleMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
                Destroy(primitive);
            }

            // Hero_Base is a 1 m capsule standing on the ground.
            AddMeshPart(ghost, "Capsule", _capsuleMesh, new Vector3(0f, 0.5f, 0f), Quaternion.identity, new Vector3(0.5f, 0.5f, 0.5f));
        }

        private void AddMeshPart(Transform ghost, string partName, Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var part = new GameObject(partName);
            Transform t = part.transform;
            t.SetParent(ghost, false);
            t.localPosition = position;
            t.localRotation = rotation;
            t.localScale = scale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = part.AddComponent<MeshRenderer>();
            int subMeshes = Mathf.Max(1, mesh.subMeshCount);
            var materials = new Material[subMeshes];
            for (int i = 0; i < subMeshes; i++) materials[i] = _bodyInstance;
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // STUB: C có thể làm vật liệu bóng mờ riêng (Art/Materials); tới lúc đó tạo URP Unlit trong suốt lúc nạp trận.
        private static Material CreateTransparentMaterial(Material template, string materialName)
        {
            if (template != null) return new Material(template) { name = materialName };

            Shader shader = Shader.Find(UrpUnlitShader);
            Material material;
            if (shader != null)
            {
                material = new Material(shader);
                material.SetFloat("_Surface", 1f); // Transparent
                material.SetFloat("_Blend", 0f);   // Alpha
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                material = new Material(Shader.Find(FallbackShader));
            }

            material.name = materialName;
            return material;
        }

        private void OnDestroy()
        {
            if (_bodyInstance != null) Destroy(_bodyInstance);
            if (_ringInstance != null) Destroy(_ringInstance);
        }
    }
}
