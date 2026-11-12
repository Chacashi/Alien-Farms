using System.Collections.Generic;
using System.Reflection;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AlienFarms.Tests
{
    public sealed class PenInventoryViewTests
    {
        private const int SlotCount = 3;
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;

        private readonly List<Object> _ownedObjects = new List<Object>();

        private GameObject _root;
        private PenInventoryView _view;
        private PenInventorySlotView[] _slots;
        private TMP_Text[] _texts;
        private Image[] _images;
        private bool _enabled;

        [SetUp]
        public void SetUp()
        {
            ResetSnapshot();

            _root = new GameObject("PenInventoryView");
            _slots = new PenInventorySlotView[SlotCount];
            _texts = new TMP_Text[SlotCount];
            _images = new Image[SlotCount];

            for (int i = 0; i < SlotCount; i++)
            {
                var slotObject = new GameObject($"Slot{i}");
                slotObject.transform.SetParent(_root.transform);

                var iconObject = new GameObject("Icon");
                iconObject.transform.SetParent(slotObject.transform);
                _images[i] = iconObject.AddComponent<Image>();

                var countObject = new GameObject("Count");
                countObject.transform.SetParent(slotObject.transform);
                _texts[i] = countObject.AddComponent<TextMeshProUGUI>();

                _slots[i] = slotObject.AddComponent<PenInventorySlotView>();
                var serializedSlot = new SerializedObject(_slots[i]);
                serializedSlot.FindProperty("_icon").objectReferenceValue = _images[i];
                serializedSlot.FindProperty("_count").objectReferenceValue = _texts[i];
                serializedSlot.ApplyModifiedPropertiesWithoutUndo();
            }

            _view = _root.AddComponent<PenInventoryView>();
            var serializedView = new SerializedObject(_view);
            serializedView.FindProperty("_player").intValue = (int)PlayerId.Player1;
            SerializedProperty slots = serializedView.FindProperty("_slots");
            slots.arraySize = SlotCount;
            for (int i = 0; i < SlotCount; i++)
            {
                slots.GetArrayElementAtIndex(i).objectReferenceValue = _slots[i];
            }

            serializedView.ApplyModifiedPropertiesWithoutUndo();

            // Edit Mode does not run the lifecycle of non-ExecuteAlways components.
            Invoke("Awake");
            Invoke("OnEnable");
            _enabled = true;
        }

        [TearDown]
        public void TearDown()
        {
            if (_enabled)
            {
                Invoke("OnDisable");
                _enabled = false;
            }

            Object.DestroyImmediate(_root);

            // Reverse order: sprites before their textures, entries' data before the settings holding them.
            for (int i = _ownedObjects.Count - 1; i >= 0; i--)
            {
                if (_ownedObjects[i] != null)
                {
                    Object.DestroyImmediate(_ownedObjects[i]);
                }
            }

            _ownedObjects.Clear();
            ResetSnapshot();
        }

        [Test]
        public void InitialCounts_AreZero()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                Assert.That(_slots[i].Count, Is.EqualTo(0));
                Assert.That(_texts[i].text, Is.EqualTo("0"));
            }
        }

        [Test]
        public void OwnPlayerEvent_UpdatesMatchingSlotOnly()
        {
            GameEvents.RaisePenInventoryChanged(PlayerId.Player1, 2, 5);

            Assert.That(_texts[2].text, Is.EqualTo("5"));
            Assert.That(_slots[2].Count, Is.EqualTo(5));
            Assert.That(_texts[0].text, Is.EqualTo("0"));
            Assert.That(_texts[1].text, Is.EqualTo("0"));
        }

        [Test]
        public void OtherPlayerEvent_IsIgnored()
        {
            GameEvents.RaisePenInventoryChanged(PlayerId.Player2, 0, 7);

            Assert.That(_texts[0].text, Is.EqualTo("0"));
            Assert.That(_slots[0].Count, Is.EqualTo(0));
        }

        [Test]
        public void OutOfRangeIndex_IsIgnored()
        {
            Assert.DoesNotThrow(() => GameEvents.RaisePenInventoryChanged(PlayerId.Player1, SlotCount, 3));
            Assert.DoesNotThrow(() => GameEvents.RaisePenInventoryChanged(PlayerId.Player1, -1, 3));

            for (int i = 0; i < SlotCount; i++)
            {
                Assert.That(_texts[i].text, Is.EqualTo("0"));
            }
        }

        [Test]
        public void ZeroCountEvent_ResetsSlot()
        {
            GameEvents.RaisePenInventoryChanged(PlayerId.Player1, 1, 4);
            GameEvents.RaisePenInventoryChanged(PlayerId.Player1, 1, 0);

            Assert.That(_texts[1].text, Is.EqualTo("0"));
            Assert.That(_slots[1].Count, Is.EqualTo(0));
        }

        [Test]
        public void ResetCounts_SetsEverySlotToZero()
        {
            GameEvents.RaisePenInventoryChanged(PlayerId.Player1, 0, 2);
            GameEvents.RaisePenInventoryChanged(PlayerId.Player1, 2, 9);

            _view.ResetCounts();

            for (int i = 0; i < SlotCount; i++)
            {
                Assert.That(_texts[i].text, Is.EqualTo("0"));
            }
        }

        [Test]
        public void DisabledView_IgnoresEvents()
        {
            Invoke("OnDisable");
            _enabled = false;

            GameEvents.RaisePenInventoryChanged(PlayerId.Player1, 0, 6);

            Assert.That(_texts[0].text, Is.EqualTo("0"));
        }

        [Test]
        public void SetIconIfEmpty_KeepsSceneSpriteAndFillsEmptyOne()
        {
            Sprite first = CreateSprite();
            Sprite second = CreateSprite();

            _slots[0].SetIconIfEmpty(first);
            _slots[0].SetIconIfEmpty(second);

            Assert.That(_images[0].sprite, Is.SameAs(first));
        }

        [Test]
        public void AssignIcons_FillsSlotsFromSpawnSettingsAndLeavesExtraSlotsUntouched()
        {
            Sprite chicken = CreateSprite();
            Sprite sheep = CreateSprite();
            AnimalSpawnSettings settings = CreateSpawnSettings(chicken, sheep);

            var serializedView = new SerializedObject(_view);
            serializedView.FindProperty("_spawnSettings").objectReferenceValue = settings;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            Invoke("AssignIcons");

            Assert.That(_images[0].sprite, Is.SameAs(chicken));
            Assert.That(_images[1].sprite, Is.SameAs(sheep));
            Assert.That(_images[2].sprite, Is.Null, "Slot without a spawn entry must stay untouched.");
        }

        [Test]
        public void AssignIcons_KeepsSpriteAlreadySetInScene()
        {
            Sprite sceneSprite = CreateSprite();
            Sprite dataSprite = CreateSprite();
            _images[0].sprite = sceneSprite;
            AnimalSpawnSettings settings = CreateSpawnSettings(dataSprite);

            var serializedView = new SerializedObject(_view);
            serializedView.FindProperty("_spawnSettings").objectReferenceValue = settings;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            Invoke("AssignIcons");

            Assert.That(_images[0].sprite, Is.SameAs(sceneSprite));
        }

        [Test]
        public void Snapshot_RaiseIsReadBackPerPlayerAndType()
        {
            GameEvents.RaisePenInventoryChanged(PlayerId.Player1, 1, 4);
            GameEvents.RaisePenInventoryChanged(PlayerId.Player2, 1, 9);

            Assert.That(PenInventorySnapshot.Get(PlayerId.Player1, 1), Is.EqualTo(4));
            Assert.That(PenInventorySnapshot.Get(PlayerId.Player2, 1), Is.EqualTo(9));
            Assert.That(PenInventorySnapshot.Get(PlayerId.Player1, 0), Is.EqualTo(0));
            Assert.That(PenInventorySnapshot.Get(PlayerId.None, 1), Is.EqualTo(0));
            Assert.That(PenInventorySnapshot.Get(PlayerId.Player1, -1), Is.EqualTo(0));
        }

        [Test]
        public void Snapshot_GrowsForHigherIndexAndIgnoresIndexOverCap()
        {
            const int highIndex = 40;
            GameEvents.RaisePenInventoryChanged(PlayerId.Player1, highIndex, 3);
            GameEvents.RaisePenInventoryChanged(PlayerId.Player1, PenInventorySnapshot.MaxTypeCount, 5);

            Assert.That(PenInventorySnapshot.Get(PlayerId.Player1, highIndex), Is.EqualTo(3));
            Assert.That(PenInventorySnapshot.Get(PlayerId.Player1, PenInventorySnapshot.MaxTypeCount), Is.EqualTo(0));
        }

        [Test]
        public void Snapshot_ResetClearsCounts()
        {
            GameEvents.RaisePenInventoryChanged(PlayerId.Player1, 0, 6);

            ResetSnapshot();

            Assert.That(PenInventorySnapshot.Get(PlayerId.Player1, 0), Is.EqualTo(0));
        }

        [Test]
        public void ViewEnabledLate_ShowsCachedCounts()
        {
            Invoke("OnDisable");
            _enabled = false;

            GameEvents.RaisePenInventoryChanged(PlayerId.Player1, 0, 2);
            GameEvents.RaisePenInventoryChanged(PlayerId.Player1, 2, 7);
            GameEvents.RaisePenInventoryChanged(PlayerId.Player2, 1, 5);
            Assert.That(_texts[0].text, Is.EqualTo("0"), "Disabled view must not update.");

            Invoke("OnEnable");
            _enabled = true;

            Assert.That(_texts[0].text, Is.EqualTo("2"));
            Assert.That(_texts[1].text, Is.EqualTo("0"), "Other player's count must not leak in.");
            Assert.That(_texts[2].text, Is.EqualTo("7"));
            Assert.That(_slots[2].Count, Is.EqualTo(7));
        }

        private Sprite CreateSprite()
        {
            var texture = new Texture2D(2, 2);
            _ownedObjects.Add(texture);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.zero);
            _ownedObjects.Add(sprite);
            return sprite;
        }

        private AnimalSpawnSettings CreateSpawnSettings(params Sprite[] icons)
        {
            var settings = ScriptableObject.CreateInstance<AnimalSpawnSettings>();
            _ownedObjects.Add(settings);

            var serializedSettings = new SerializedObject(settings);
            SerializedProperty entries = serializedSettings.FindProperty("_entries");
            entries.arraySize = icons.Length;
            for (int i = 0; i < icons.Length; i++)
            {
                var data = ScriptableObject.CreateInstance<AnimalData>();
                _ownedObjects.Add(data);
                var serializedData = new SerializedObject(data);
                serializedData.FindProperty("_icon").objectReferenceValue = icons[i];
                serializedData.ApplyModifiedPropertiesWithoutUndo();

                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_data").objectReferenceValue = data;
                entry.FindPropertyRelative("_weight").intValue = 1;
            }

            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }

        private void Invoke(string method)
        {
            MethodInfo info = typeof(PenInventoryView).GetMethod(method, PrivateInstance);
            Assert.That(info, Is.Not.Null, method);
            info.Invoke(_view, null);
        }

        private static void ResetSnapshot()
        {
            // Internal reset run by GameEvents' SubsystemRegistration reset; only the cache is cleared here.
            MethodInfo reset = typeof(PenInventorySnapshot).GetMethod("Reset", PrivateStatic);
            Assert.That(reset, Is.Not.Null, "PenInventorySnapshot.Reset");
            reset.Invoke(null, null);
        }
    }
}
