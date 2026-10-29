using AlienFarms.Core;
using TMPro;
using UnityEngine;

namespace AlienFarms.UI
{
    /// <summary>
    /// Shows the pre-round countdown ("3", "2", "1") and a short "GO" message when play starts.
    /// The GO duration comes with <see cref="GameEvents.OnCountdownFinished"/> (single source: the match settings).
    /// Lives on an always-active object and toggles <see cref="_root"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CountdownView : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Text shown when the countdown ends.")]
        private string _goText = "¡YA!";

        [Header("References")]
        [SerializeField, Tooltip("Root shown while counting down (text lives in the scene). Must not contain this component.")]
        private GameObject _root;

        [SerializeField, Tooltip("Countdown text.")]
        private TMP_Text _text;

        // > 0 while the GO message is visible: Time.time at which to hide it.
        private float _hideAt;

        private void OnEnable()
        {
            GameEvents.OnCountdownTick += HandleCountdownTick;
            GameEvents.OnCountdownFinished += HandleCountdownFinished;
            GameEvents.OnMatchPhaseChanged += HandleMatchPhaseChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnCountdownTick -= HandleCountdownTick;
            GameEvents.OnCountdownFinished -= HandleCountdownFinished;
            GameEvents.OnMatchPhaseChanged -= HandleMatchPhaseChanged;
        }

        private void Update()
        {
            if (_hideAt > 0f && Time.time >= _hideAt)
            {
                Hide();
            }
        }

        private void HandleCountdownTick(int secondsLeft)
        {
            if (secondsLeft <= 0)
            {
                return;
            }

            _hideAt = 0f;
            if (_text != null)
            {
                _text.SetText("{0}", secondsLeft);
            }

            SetVisible(true);
        }

        private void HandleCountdownFinished(float goMessageDuration)
        {
            if (goMessageDuration <= 0f)
            {
                Hide();
                return;
            }

            if (_text != null)
            {
                _text.SetText(_goText);
            }

            SetVisible(true);
            // Never 0: 0 means "no pending hide".
            _hideAt = Mathf.Max(Time.time + goMessageDuration, Mathf.Epsilon);
        }

        private void HandleMatchPhaseChanged(MatchPhase previous, MatchPhase current)
        {
            // Countdown shows numbers; Playing keeps the GO message until its timer hides it.
            if (current != MatchPhase.Countdown && current != MatchPhase.Playing)
            {
                Hide();
            }
        }

        private void Hide()
        {
            _hideAt = 0f;
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (_root != null && _root.activeSelf != visible)
            {
                _root.SetActive(visible);
            }
        }

        private void OnValidate()
        {
            if (_text == null)
            {
                Debug.LogWarning($"[{nameof(CountdownView)}] Missing text on '{name}'.", this);
            }

            if (_root == null)
            {
                Debug.LogWarning($"[{nameof(CountdownView)}] Missing root on '{name}'.", this);
            }
            else if (_root == gameObject || transform.IsChildOf(_root.transform))
            {
                Debug.LogWarning($"[{nameof(CountdownView)}] Root on '{name}' contains this view; hiding it would stop listening to events.", this);
            }
        }
    }
}
