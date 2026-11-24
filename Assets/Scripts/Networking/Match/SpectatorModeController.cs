using System;
using AlienFarms.Networking.Data;
using AlienFarms.UI;
using UnityEngine;

namespace AlienFarms.Networking.Match
{
    /// <summary>Inert placeholder for serialized scene references. Removed in C5.</summary>
    [Obsolete("Removed in C5.")]
    [DisallowMultipleComponent]
    public sealed class SpectatorModeController : MonoBehaviour
    {
        // Keep the serialized field names and types until scene cleanup in C5.
#pragma warning disable CS0169, CS0649
        [SerializeField] private NetworkRuntimeAnchor _anchor;
        [SerializeField] private NetworkSeatPresence _presence;
        [SerializeField] private GameObject _spectatorRig;
        [SerializeField] private Camera _spectatorCamera;
        [SerializeField] private SpectatorPanelView _panel;
#pragma warning restore CS0169, CS0649

        private void Awake() => enabled = false;
    }
}
