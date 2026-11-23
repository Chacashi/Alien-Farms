using Unity.Cinemachine;
using UnityEngine;

namespace AlienFarms.Menu
{
    [DisallowMultipleComponent]
    public sealed class MenuCameraRig : MonoBehaviour
    {
        private const int LivePriority = 20;
        private const int IdlePriority = 0;

        private static readonly CinemachineBlendDefinition CutBlend = new CinemachineBlendDefinition
        {
            Style = CinemachineBlendDefinition.Styles.Cut,
            Time = 0f
        };

        [SerializeField, Tooltip("Cinemachine Brain on the Main Camera in MainMenu.")]
        private CinemachineBrain _brain;

        [SerializeField, Tooltip("CM_Sky shows the sky in attract mode.")]
        private CinemachineCamera _skyCamera;

        [SerializeField, Tooltip("CM_Menu shows the farm in the menu.")]
        private CinemachineCamera _menuCamera;

        private MenuCameraShot _currentShot;
        private bool _hasShot;

        public void Show(MenuCameraShot shot, CinemachineBlendDefinition blend)
        {
            if (_hasShot && _currentShot == shot)
            {
                return;
            }

            if (_brain != null)
            {
                _brain.DefaultBlend = blend;
            }

            SetPriorities(shot);
        }

        public void Cut(MenuCameraShot shot)
        {
            if (_brain != null)
            {
                _brain.DefaultBlend = CutBlend;
            }

            SetPriorities(shot);
            FinishBlend();
        }

        public void FinishBlend()
        {
            if (_brain != null)
            {
                _brain.ActiveBlend = null;
            }
        }

        public bool IsSettledOn(MenuCameraShot shot)
        {
            CinemachineCamera camera = shot == MenuCameraShot.Sky ? _skyCamera : _menuCamera;
            return _brain != null && camera != null && !_brain.IsBlending
                && ReferenceEquals(_brain.ActiveVirtualCamera, camera);
        }

        private void SetPriorities(MenuCameraShot shot)
        {
            SetPriority(_skyCamera, shot == MenuCameraShot.Sky ? LivePriority : IdlePriority);
            SetPriority(_menuCamera, shot == MenuCameraShot.Menu ? LivePriority : IdlePriority);
            _currentShot = shot;
            _hasShot = true;
        }

        private static void SetPriority(CinemachineCamera camera, int priority)
        {
            if (camera != null)
            {
                camera.Priority.Enabled = true;
                camera.Priority.Value = priority;
            }
        }

        private void OnValidate()
        {
            if (_brain == null || _skyCamera == null || _menuCamera == null)
            {
                Debug.LogWarning($"[{nameof(MenuCameraRig)}] Assign the Brain and both cameras on '{name}'.", this);
            }
            else if (_skyCamera == _menuCamera)
            {
                Debug.LogWarning($"[{nameof(MenuCameraRig)}] Assign distinct Sky and Menu cameras on '{name}'.", this);
            }
        }
    }
}
