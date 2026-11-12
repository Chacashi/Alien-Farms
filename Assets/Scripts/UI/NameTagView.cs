using System;
using TMPro;
using UnityEngine;

namespace AlienFarms.UI
{
    /// <summary>
    /// World-space name tag (TextMeshPro 3D) over an avatar: the name and an optional smaller debug line. Pure view: a
    /// driver (e.g. the online NetworkNameTag) feeds it. The text is rebuilt only when the name or the detail changes;
    /// while visible it faces the assigned camera in LateUpdate (parallel to the view plane, no allocation).
    /// Lives on the tag's own child, never on the avatar root (the billboard rotates this transform).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NameTagView : MonoBehaviour
    {
        private const string DetailOpen = "\n<size=60%>";
        private const string DetailClose = "</size>";

        [Header("References")]
        [SerializeField, Tooltip("TextMeshPro (3D) of the tag: name on the first line, debug detail on the second.")]
        private TMP_Text _label;

        [SerializeField, Tooltip("Transform turned towards the camera (defaults to this transform).")]
        private Transform _billboard;

        private Camera _viewCamera;
        private string _name = string.Empty;
        private string _detail;
        private bool _visible = true;

        /// <summary>True while the tag is shown.</summary>
        public bool IsVisible => _visible;

        private void Awake()
        {
            if (_label == null)
            {
                TryGetComponent(out _label);
            }

            if (_billboard == null)
            {
                _billboard = transform;
            }
        }

        private void LateUpdate()
        {
            if (_viewCamera != null && _billboard != null)
            {
                _billboard.rotation = _viewCamera.transform.rotation;
            }
        }

        /// <summary>Name line (null shows nothing). Ignored when unchanged.</summary>
        public void SetName(string displayName)
        {
            displayName ??= string.Empty;
            if (string.Equals(_name, displayName, StringComparison.Ordinal))
            {
                return;
            }

            _name = displayName;
            Rebuild();
        }

        /// <summary>Debug line under the name (e.g. "client 1"); null or empty hides it. Ignored when unchanged.</summary>
        public void SetDetail(string detail)
        {
            if (string.IsNullOrEmpty(detail))
            {
                detail = null;
            }

            if (string.Equals(_detail, detail, StringComparison.Ordinal))
            {
                return;
            }

            _detail = detail;
            Rebuild();
        }

        public void SetColor(Color color)
        {
            if (_label != null && _label.color != color)
            {
                _label.color = color;
            }
        }

        /// <summary>Shows or hides the text; while hidden the billboard does not run.</summary>
        public void SetVisible(bool visible)
        {
            _visible = visible;
            if (_label != null && _label.enabled != visible)
            {
                _label.enabled = visible;
            }

            if (enabled != visible)
            {
                enabled = visible;
            }
        }

        /// <summary>Camera the tag faces (null: keeps its last rotation).</summary>
        public void SetViewCamera(Camera viewCamera)
        {
            _viewCamera = viewCamera;
        }

        // Name and detail only reach here from sanitized sources: '<' / '>' are stripped, so no rich text injection.
        private void Rebuild()
        {
            if (_label == null)
            {
                return;
            }

            _label.SetText(_detail == null ? _name : _name + DetailOpen + _detail + DetailClose);
        }

        private void OnValidate()
        {
            if (_label == null)
            {
                TryGetComponent(out _label);
            }

            if (_label == null)
            {
                Debug.LogWarning($"[{nameof(NameTagView)}] Missing TextMeshPro label on '{name}'.", this);
            }

            if (transform.parent == null)
            {
                Debug.LogWarning($"[{nameof(NameTagView)}] '{name}' must be a child of the avatar: the billboard rotates it.", this);
            }
        }
    }
}
