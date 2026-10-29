using AlienFarms.Animals;
using AlienFarms.Core;
using AlienFarms.Zones;
using UnityEngine;

namespace AlienFarms.Match
{
    /// <summary>
    /// Decides captures: when a holder voluntarily releases an animal inside their own zone during play, the animal
    /// is penned and <see cref="GameEvents.OnAnimalCaptured"/> is raised.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CaptureReferee : MonoBehaviour
    {
        private const int RequiredZones = 2;

        [Header("References")]
        [SerializeField, Tooltip("One PlayerZone per player (Player1 and Player2).")]
        private PlayerZone[] _playerZones = new PlayerZone[RequiredZones];

        private bool _capturesEnabled;

        private void OnEnable()
        {
            GameEvents.OnAnimalDropped += HandleAnimalDropped;
            GameEvents.OnMatchPhaseChanged += HandleMatchPhaseChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnAnimalDropped -= HandleAnimalDropped;
            GameEvents.OnMatchPhaseChanged -= HandleMatchPhaseChanged;
            _capturesEnabled = false;
        }

        private void HandleMatchPhaseChanged(MatchPhase previous, MatchPhase current)
        {
            _capturesEnabled = current == MatchPhase.Playing;
        }

        // Runs synchronously inside Animal.Release: RaiseAnimalDropped must remain the last statement of Release.
        private void HandleAnimalDropped(PlayerId holder, Animal animal, DropReason reason)
        {
            if (animal == null)
            {
                return;
            }

            PlayerZone zone = FindZone(holder);
            bool inside = zone != null && zone.Contains(animal.Position);
            PlayerId zoneOwner = zone != null ? zone.Owner : PlayerId.None;

            if (!CaptureRules.CountsAsCapture(reason, holder, zoneOwner, inside, _capturesEnabled))
            {
                return;
            }

            if (animal.TryCapture(holder, zone.Bounds))
            {
                GameEvents.RaiseAnimalCaptured(holder, animal);
            }
        }

        private PlayerZone FindZone(PlayerId owner)
        {
            if (owner == PlayerId.None || _playerZones == null)
            {
                return null;
            }

            for (int i = 0; i < _playerZones.Length; i++)
            {
                PlayerZone zone = _playerZones[i];
                if (zone != null && zone.Owner == owner)
                {
                    return zone;
                }
            }

            return null;
        }

        private void OnValidate()
        {
            if (_playerZones == null || _playerZones.Length == 0)
            {
                Debug.LogWarning($"[{nameof(CaptureReferee)}] No player zones assigned on '{name}'.", this);
                return;
            }

            int player1 = 0;
            int player2 = 0;
            for (int i = 0; i < _playerZones.Length; i++)
            {
                PlayerZone zone = _playerZones[i];
                if (zone == null)
                {
                    Debug.LogWarning($"[{nameof(CaptureReferee)}] Player zone {i} is empty on '{name}'.", this);
                    continue;
                }

                if (zone.Owner == PlayerId.Player1)
                {
                    player1++;
                }
                else if (zone.Owner == PlayerId.Player2)
                {
                    player2++;
                }
            }

            if (player1 != 1 || player2 != 1)
            {
                Debug.LogWarning($"[{nameof(CaptureReferee)}] '{name}' needs exactly one zone for Player1 and one for Player2.", this);
            }
        }
    }
}
