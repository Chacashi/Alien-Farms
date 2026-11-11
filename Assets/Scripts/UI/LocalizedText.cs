using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace AlienFarms.UI
{
    /// <summary>
    /// Writes a localized string into the TMP text on this object and refreshes it when the locale changes.
    /// Only for static texts: never put it on a text that a view writes from code.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField, Tooltip("Table entry shown by this text (non-Smart).")]
        private LocalizedString _string = new LocalizedString();

        private TMP_Text _text;
        private LocalizedString.ChangeHandler _handler;

        /// <summary>The referenced table entry.</summary>
        public LocalizedString String => _string;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _handler = HandleStringChanged;
        }

        private void OnEnable()
        {
            if (_string != null && !_string.IsEmpty)
            {
                _string.StringChanged += _handler;
            }
        }

        private void OnDisable()
        {
            if (_string != null)
            {
                _string.StringChanged -= _handler;
            }
        }

        private void HandleStringChanged(string value)
        {
            if (value != null)
            {
                _text.SetText(value);
            }
        }

        private void OnValidate()
        {
            // Fully empty = freshly added, not configured yet: no warning. Half-filled = broken reference.
            if (_string == null)
            {
                return;
            }

            bool hasTable = _string.TableReference.ReferenceType != TableReference.Type.Empty;
            bool hasEntry = _string.TableEntryReference.ReferenceType != TableEntryReference.Type.Empty;
            if (hasTable != hasEntry)
            {
                Debug.LogWarning($"[{nameof(LocalizedText)}] Localized string on '{name}' has only a {(hasTable ? "table" : "entry")}; set both.", this);
            }
        }
    }
}
