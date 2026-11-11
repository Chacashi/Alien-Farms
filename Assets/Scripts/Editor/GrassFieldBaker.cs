using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace AlienFarms.Editor
{
    /// <summary>
    /// Authoring window: bakes a chunked grass-blade field into a mesh asset under
    /// Assets/Art/Models/Generated/ and optionally instantiates it in the scene under "Env_Grass".
    /// </summary>
    public sealed class GrassFieldBaker : EditorWindow
    {
        private const string MenuPath = "Tools/Alien Farms/Bake Grass Field";
        private const string OutputFolder = "Assets/Art/Models/Generated";
        private const string DefaultMaterialPath = "Assets/Materials/Environment/M_GrassBlades.mat";
        private const string SceneRootName = "Env_Grass";
        private const long VertexWarningThreshold = 400000;

        [SerializeField] private GrassBakeSettings _settings = new GrassBakeSettings();
        [SerializeField] private GameObject _areaReference;
        [SerializeField] private Material _grassMaterial;
        [SerializeField] private bool _createInScene;
        [SerializeField] private ShadowCastingMode _castShadows = ShadowCastingMode.Off;
        [SerializeField] private int _layer;

        private SerializedObject _serializedWindow;
        private SerializedProperty _settingsProperty;
        private Vector2 _scroll;

        [MenuItem(MenuPath)]
        private static void Open()
        {
            GrassFieldBaker window = GetWindow<GrassFieldBaker>();
            window.titleContent = new GUIContent("Grass Field Baker");
            window.minSize = new Vector2(360f, 480f);
            window.Show();
        }

        private void OnEnable()
        {
            if (_grassMaterial == null)
            {
                _grassMaterial = AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterialPath);
            }

            _serializedWindow = new SerializedObject(this);
            _settingsProperty = _serializedWindow.FindProperty(nameof(_settings));
        }

        private void OnGUI()
        {
            _serializedWindow.Update();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("Area Source", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _areaReference = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("Reference", "Collider (preferred) or Renderer whose world bounds define the field. Empty = Manual Area."),
                    _areaReference, typeof(GameObject), true);
                if (GUILayout.Button("Use Selection", GUILayout.Width(100f)))
                {
                    _areaReference = Selection.activeGameObject;
                }
            }

            bool hasArea = TryGetArea(out Bounds area);
            EditorGUILayout.HelpBox(hasArea
                ? $"Area: center {area.center:F1}, size {area.size:F1} (grass on y = {area.max.y:F2})"
                : "Reference has no Collider or Renderer.", hasArea ? MessageType.None : MessageType.Warning);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_settingsProperty, new GUIContent("Settings"), true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene Output (optional)", EditorStyles.boldLabel);
            _createInScene = EditorGUILayout.Toggle(
                new GUIContent("Create In Scene", "Instantiate the chunks under '" + SceneRootName + "' in the reference's scene (or the active scene)."),
                _createInScene);
            using (new EditorGUI.DisabledScope(!_createInScene))
            {
                _grassMaterial = (Material)EditorGUILayout.ObjectField("Grass Material", _grassMaterial, typeof(Material), false);
                _castShadows = (ShadowCastingMode)EditorGUILayout.EnumPopup(
                    new GUIContent("Cast Shadows", "Off is recommended for split-screen performance."), _castShadows);
                _layer = EditorGUILayout.LayerField("Layer", _layer);
            }

            _serializedWindow.ApplyModifiedProperties();

            EditorGUILayout.Space();
            if (hasArea)
            {
                long vertices = GrassMeshBuilder.EstimateVertexCount(_settings, area);
                long triangles = vertices / _settings.VerticesPerBlade * _settings.TrianglesPerBlade;
                EditorGUILayout.HelpBox($"Estimated: ~{vertices:N0} vertices, ~{triangles:N0} triangles.",
                    vertices > VertexWarningThreshold ? MessageType.Warning : MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(!hasArea || string.IsNullOrWhiteSpace(_settings.FieldName)))
            {
                if (GUILayout.Button("Bake Grass Field", GUILayout.Height(32f)))
                {
                    Bake(area);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private bool TryGetArea(out Bounds area)
        {
            if (_areaReference == null)
            {
                area = _settings.ManualArea;
                return true;
            }

            if (_areaReference.TryGetComponent(out Collider areaCollider))
            {
                area = areaCollider.bounds;
                return true;
            }

            if (_areaReference.TryGetComponent(out Renderer areaRenderer))
            {
                area = areaRenderer.bounds;
                return true;
            }

            area = default;
            return false;
        }

        private void Bake(Bounds area)
        {
            var origin = new Vector3(area.center.x, area.max.y, area.center.z);
            List<GrassMeshBuilder.Chunk> chunks;
            try
            {
                chunks = GrassMeshBuilder.Build(_settings, area, origin,
                    p => EditorUtility.DisplayCancelableProgressBar("Grass Field Baker", "Generating blades...", p));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (chunks.Count == 0)
            {
                Debug.LogWarning("[GrassFieldBaker] No grass generated: check the area, density and masks.");
                return;
            }

            int blades = 0;
            int vertices = 0;
            foreach (GrassMeshBuilder.Chunk chunk in chunks)
            {
                blades += chunk.BladeCount;
                vertices += chunk.Mesh.vertexCount;
            }

            // SaveMeshes may destroy the temporary chunk meshes (copied into existing chunk assets).
            string fieldFolder = $"{OutputFolder}/GrassField_{_settings.FieldName.Trim()}";
            List<Mesh> meshes = SaveMeshes(fieldFolder, chunks);

            Debug.Log($"[GrassFieldBaker] Baked {blades:N0} blades / {vertices:N0} vertices in {chunks.Count} chunks -> {fieldFolder}/");

            if (_createInScene)
            {
                CreateInScene(meshes, origin);
            }
        }

        /// <summary>
        /// Writes one mesh asset per chunk into <paramref name="folder"/>. Existing chunk assets are overwritten
        /// in place (keeping GUIDs, so scene references survive a re-bake); chunks that became empty are deleted.
        /// </summary>
        private static List<Mesh> SaveMeshes(string folder, List<GrassMeshBuilder.Chunk> chunks)
        {
            EnsureFolder(folder);

            var stalePaths = new HashSet<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { folder }))
            {
                stalePaths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            var result = new List<Mesh>(chunks.Count);
            foreach (GrassMeshBuilder.Chunk chunk in chunks)
            {
                string chunkPath = $"{folder}/{chunk.Name}.asset";
                stalePaths.Remove(chunkPath);

                Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(chunkPath);
                if (existing != null)
                {
                    CopyMeshData(chunk.Mesh, existing);
                    EditorUtility.SetDirty(existing);
                    DestroyImmediate(chunk.Mesh);
                    result.Add(existing);
                }
                else
                {
                    AssetDatabase.CreateAsset(chunk.Mesh, chunkPath);
                    result.Add(chunk.Mesh);
                }
            }

            foreach (string stalePath in stalePaths)
            {
                AssetDatabase.DeleteAsset(stalePath);
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        /// <summary>Overwrites <paramref name="target"/> in place so its asset identity (and scene references) is preserved.</summary>
        private static void CopyMeshData(Mesh source, Mesh target)
        {
            var vector3Buffer = new List<Vector3>(source.vertexCount);
            var vector2Buffer = new List<Vector2>(source.vertexCount);
            var colorBuffer = new List<Color32>(source.vertexCount);

            target.Clear();
            target.indexFormat = source.indexFormat;

            source.GetVertices(vector3Buffer);
            target.SetVertices(vector3Buffer);
            source.GetNormals(vector3Buffer);
            target.SetNormals(vector3Buffer);
            source.GetUVs(0, vector2Buffer);
            target.SetUVs(0, vector2Buffer);
            source.GetUVs(1, vector3Buffer);
            target.SetUVs(1, vector3Buffer);
            source.GetColors(colorBuffer);
            target.SetColors(colorBuffer);
            target.SetTriangles(source.GetTriangles(0), 0, false);
            target.bounds = source.bounds;
        }

        private void CreateInScene(List<Mesh> meshes, Vector3 origin)
        {
            if (_grassMaterial == null)
            {
                Debug.LogWarning($"[GrassFieldBaker] No grass material assigned (expected {DefaultMaterialPath}). Scene objects not created.");
                return;
            }

            Scene scene = _areaReference != null ? _areaReference.scene : SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                scene = SceneManager.GetActiveScene();
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Bake Grass Field");

            GameObject root = FindOrCreateRoot(scene);
            string fieldObjectName = "Grass_" + _settings.FieldName.Trim();

            Transform previous = root.transform.Find(fieldObjectName);
            if (previous != null)
            {
                Undo.DestroyObjectImmediate(previous.gameObject);
            }

            var field = new GameObject(fieldObjectName);
            Undo.RegisterCreatedObjectUndo(field, "Create Grass Field");
            Undo.SetTransformParent(field.transform, root.transform, "Parent Grass Field");
            field.transform.SetPositionAndRotation(origin, Quaternion.identity);
            field.transform.localScale = Vector3.one;
            field.layer = _layer;

            foreach (Mesh mesh in meshes)
            {
                var chunkObject = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
                Undo.RegisterCreatedObjectUndo(chunkObject, "Create Grass Chunk");
                chunkObject.layer = _layer;
                chunkObject.transform.SetParent(field.transform, false);

                chunkObject.GetComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer meshRenderer = chunkObject.GetComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = _grassMaterial;
                meshRenderer.shadowCastingMode = _castShadows;
                meshRenderer.receiveShadows = true;
                meshRenderer.lightProbeUsage = LightProbeUsage.Off;
                meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                meshRenderer.allowOcclusionWhenDynamic = true;
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = field;
        }

        private static GameObject FindOrCreateRoot(Scene scene)
        {
            foreach (GameObject candidate in scene.GetRootGameObjects())
            {
                if (candidate.name == SceneRootName)
                {
                    return candidate;
                }
            }

            var root = new GameObject(SceneRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create " + SceneRootName);
            if (root.scene != scene)
            {
                SceneManager.MoveGameObjectToScene(root, scene);
            }

            return root;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }
    }
}
