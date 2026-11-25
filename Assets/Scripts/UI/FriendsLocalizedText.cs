using TMPro;
using UnityEngine.Localization;

namespace AlienFarms.UI
{
    internal sealed class FriendsLocalizedText
    {
        private readonly LocalizedString _message = new LocalizedString();
        private TMP_Text _target;
        private string _key;
        private string _argument;

        public void Show(TMP_Text target, string key, string argument = null)
        {
            if (target == null || (_target == target && _key == key && _argument == argument))
            {
                return;
            }

            Clear();
            _target = target;
            _key = key;
            _argument = argument;
            _target.text = string.Empty;
            if (!string.IsNullOrEmpty(key))
            {
                _message.SetReference(LocalizationKeys.UiTable, key);
                _message.Arguments = argument == null ? null : new object[] { argument };
                _message.StringChanged += Apply;
            }
        }

        public void Clear()
        {
            _message.StringChanged -= Apply;
            _target = null;
            _key = null;
            _argument = null;
        }

        private void Apply(string value) => _target.text = value;
    }
}
