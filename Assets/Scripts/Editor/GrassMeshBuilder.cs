using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AlienFarms.Editor
{
    /// <summary>
    /// Deterministically generates chunked grass-blade meshes for the AlienFarms/GrassBlades shader.
    /// Vertex layout: UV0 = (across, height 0..1), UV1 = blade root (mesh space),
    /// COLOR = (height, clump random, blade random, 1), NORMAL = blade facing biased up.
    /// </summary>
    public static class GrassMeshBuilder
    {
        public readonly struct Chunk
        {
            public Chunk(string name, Mesh mesh, int bladeCount)
            {
                Name = name;
                Mesh = mesh;
                BladeCount = bladeCount;
            }

            public string Name { get; }
            public Mesh Mesh { get; }
            public int BladeCount { get; }
        }

        private const float NormalUpBias = 0.65f;
        private const float GroundRayPadding = 50f;

        private sealed class Buffers
        {
            public readonly List<Vector3> Positions = new List<Vector3>(4096);
            public readonly List<Vector3> Normals = new List<Vector3>(4096);
            public readonly List<Vector2> Uv0 = new List<Vector2>(4096);
            public readonly List<Vector3> Uv1 = new List<Vector3>(4096);
            public readonly List<Color32> Colors = new List<Color32>(4096);
            public readonly List<int> Indices = new List<int>(8192);

            public void Clear()
            {
                Positions.Clear();
                Normals.Clear();
                Uv0.Clear();
                Uv1.Clear();
                Colors.Clear();
                Indices.Clear();
            }
        }

        /// <summary>Usable area (edge margin applied) in world space.</summary>
        public static Rect GetPlacementRect(Bounds area, float edgeMargin)
        {
            float width = Mathf.Max(0f, area.size.x - edgeMargin * 2f);
            float depth = Mathf.Max(0f, area.size.z - edgeMargin * 2f);
            return new Rect(area.center.x - width * 0.5f, area.center.z - depth * 0.5f, width, depth);
        }

        public static long EstimateVertexCount(GrassBakeSettings settings, Bounds area)
        {
            Rect rect = GetPlacementRect(area, settings.EdgeMargin);
            float averageBlades = (settings.MinBladesPerClump + settings.MaxBladesPerClump) * 0.5f;
            float coverage = 1f - settings.Patchiness * 0.6f;
            double blades = rect.width * rect.height * settings.ClumpsPerSquareMeter * averageBlades * coverage;
            return (long)(blades * settings.VerticesPerBlade);
        }

        /// <summary>
        /// Builds one mesh per non-empty chunk. Vertices are relative to <paramref name="origin"/>,
        /// so every chunk renderer sits at the same transform (the origin).
        /// </summary>
        /// <param name="progress">Optional callback (0..1); return true to cancel.</param>
        public static List<Chunk> Build(GrassBakeSettings settings, Bounds area, Vector3 origin,
                                        System.Func<float, bool> progress = null)
        {
            var chunks = new List<Chunk>();
            Rect rect = GetPlacementRect(area, settings.EdgeMargin);
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return chunks;
            }

            Physics.SyncTransforms();

            float chunkSize = Mathf.Max(2f, settings.ChunkSize);
            int chunksX = Mathf.CeilToInt(rect.width / chunkSize);
            int chunksZ = Mathf.CeilToInt(rect.height / chunkSize);
            int total = chunksX * chunksZ;
            var buffers = new Buffers();

            for (int cz = 0; cz < chunksZ; cz++)
            {
                for (int cx = 0; cx < chunksX; cx++)
                {
                    int index = cz * chunksX + cx;
                    if (progress != null && progress((float)index / total))
                    {
                        return chunks;
                    }

                    var chunkRect = Rect.MinMaxRect(
                        rect.xMin + cx * chunkSize,
                        rect.yMin + cz * chunkSize,
                        Mathf.Min(rect.xMin + (cx + 1) * chunkSize, rect.xMax),
                        Mathf.Min(rect.yMin + (cz + 1) * chunkSize, rect.yMax));

                    buffers.Clear();
                    int blades = FillChunk(settings, area, chunkRect, origin, ChunkSeed(settings.Seed, cx, cz), buffers);
                    if (blades == 0)
                    {
                        continue;
                    }

                    string name = $"GrassChunk_{cx:00}_{cz:00}";
                    chunks.Add(new Chunk(name, CreateMesh(name, buffers, settings), blades));
                }
            }

            return chunks;
        }

        private static int FillChunk(GrassBakeSettings settings, Bounds area, Rect chunkRect, Vector3 origin,
                                     int seed, Buffers buffers)
        {
            var random = new System.Random(seed);
            float chunkArea = chunkRect.width * chunkRect.height;
            int clumpCount = Mathf.RoundToInt(chunkArea * settings.ClumpsPerSquareMeter);
            float noiseOffset = (settings.Seed & 0xFFFF) * 0.137f;
            QueryTriggerInteraction triggers = settings.IncludeTriggers
                ? QueryTriggerInteraction.Collide
                : QueryTriggerInteraction.Ignore;
            int blades = 0;

            for (int i = 0; i < clumpCount; i++)
            {
                // Always draw the same amount of random numbers per clump so skipping stays deterministic.
                float x = chunkRect.xMin + Next(random) * chunkRect.width;
                float z = chunkRect.yMin + Next(random) * chunkRect.height;
                float patchJitter = (Next(random) - 0.5f) * 0.12f;
                float clumpRandom = Next(random);
                int bladeCount = random.Next(settings.MinBladesPerClump, settings.MaxBladesPerClump + 1);
                int bladeSeed = random.Next();

                if (settings.Patchiness > 0f)
                {
                    float noise = Mathf.PerlinNoise((x + noiseOffset) * settings.PatchScale,
                                                    (z + noiseOffset) * settings.PatchScale);
                    if (noise < settings.Patchiness * 0.75f + patchJitter)
                    {
                        continue;
                    }
                }

                if (!TryGetSurfaceHeight(settings, area, x, z, triggers, out float y))
                {
                    continue;
                }

                if (settings.ExclusionMask.value != 0 && settings.ExclusionRadius > 0f)
                {
                    var probe = new Vector3(x, y + settings.ExclusionRadius + 0.02f, z);
                    if (Physics.CheckSphere(probe, settings.ExclusionRadius, settings.ExclusionMask, triggers))
                    {
                        continue;
                    }
                }

                var bladeRandom = new System.Random(bladeSeed);
                var clumpCenter = new Vector3(x, y, z) - origin;
                for (int b = 0; b < bladeCount; b++)
                {
                    AddBlade(settings, bladeRandom, clumpCenter, clumpRandom, buffers);
                    blades++;
                }
            }

            return blades;
        }

        private static bool TryGetSurfaceHeight(GrassBakeSettings settings, Bounds area, float x, float z,
                                                QueryTriggerInteraction triggers, out float height)
        {
            height = area.max.y;
            if (!settings.SnapToGround)
            {
                return true;
            }

            int mask = settings.GroundMask.value | settings.ExclusionMask.value;
            var rayOrigin = new Vector3(x, area.max.y + GroundRayPadding, z);
            float rayLength = area.size.y + GroundRayPadding * 2f;
            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, mask, triggers))
            {
                return false;
            }

            int hitLayerBit = 1 << hit.collider.gameObject.layer;
            if ((settings.GroundMask.value & hitLayerBit) == 0)
            {
                return false;
            }

            height = hit.point.y;
            return true;
        }

        private static void AddBlade(GrassBakeSettings settings, System.Random random, Vector3 clumpCenter,
                                     float clumpRandom, Buffers buffers)
        {
            float angle = Next(random) * Mathf.PI * 2f;
            float radius = Mathf.Sqrt(Next(random)) * settings.ClumpRadius;
            Vector3 root = clumpCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            float yaw = Next(random) * Mathf.PI * 2f;
            float height = settings.BladeHeight * (1f + (Next(random) * 2f - 1f) * settings.HeightVariation);
            float width = settings.BladeWidth * (1f + (Next(random) * 2f - 1f) * settings.WidthVariation);
            float lean = Next(random) * settings.MaxLean * height;
            float bladeRandom = Next(random);

            var facing = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
            var side = new Vector3(-facing.z, 0f, facing.x);
            Vector3 normal = Vector3.Lerp(facing, Vector3.up, NormalUpBias).normalized;

            byte clumpByte = (byte)(clumpRandom * 255f);
            byte bladeByte = (byte)(bladeRandom * 255f);
            int segments = settings.SegmentsPerBlade;
            int baseIndex = buffers.Positions.Count;

            for (int s = 0; s < segments; s++)
            {
                float t = (float)s / segments;
                float halfWidth = width * 0.5f * (1f - t * t * 0.6f);
                Vector3 center = root + Vector3.up * (height * t) + facing * (lean * t * t);
                AddVertex(buffers, center - side * halfWidth, normal, new Vector2(0f, t), root, t, clumpByte, bladeByte);
                AddVertex(buffers, center + side * halfWidth, normal, new Vector2(1f, t), root, t, clumpByte, bladeByte);
            }

            Vector3 tip = root + Vector3.up * height + facing * lean;
            AddVertex(buffers, tip, normal, new Vector2(0.5f, 1f), root, 1f, clumpByte, bladeByte);

            for (int s = 0; s < segments - 1; s++)
            {
                int left = baseIndex + s * 2;
                int right = left + 1;
                int nextLeft = left + 2;
                int nextRight = left + 3;
                buffers.Indices.Add(left);
                buffers.Indices.Add(nextLeft);
                buffers.Indices.Add(right);
                buffers.Indices.Add(right);
                buffers.Indices.Add(nextLeft);
                buffers.Indices.Add(nextRight);
            }

            int lastLeft = baseIndex + (segments - 1) * 2;
            buffers.Indices.Add(lastLeft);
            buffers.Indices.Add(baseIndex + segments * 2);
            buffers.Indices.Add(lastLeft + 1);
        }

        private static void AddVertex(Buffers buffers, Vector3 position, Vector3 normal, Vector2 uv, Vector3 root,
                                      float height, byte clumpRandom, byte bladeRandom)
        {
            buffers.Positions.Add(position);
            buffers.Normals.Add(normal);
            buffers.Uv0.Add(uv);
            buffers.Uv1.Add(root);
            buffers.Colors.Add(new Color32((byte)(height * 255f), clumpRandom, bladeRandom, 255));
        }

        private static Mesh CreateMesh(string name, Buffers buffers, GrassBakeSettings settings)
        {
            var mesh = new Mesh { name = name };
            mesh.indexFormat = buffers.Positions.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(buffers.Positions);
            mesh.SetNormals(buffers.Normals);
            mesh.SetUVs(0, buffers.Uv0);
            mesh.SetUVs(1, buffers.Uv1);
            mesh.SetColors(buffers.Colors);
            mesh.SetTriangles(buffers.Indices, 0, false);
            mesh.RecalculateBounds();

            Bounds bounds = mesh.bounds;
            float pad = settings.BladeHeight * (1f + settings.HeightVariation) * settings.WindBoundsPadding;
            bounds.Expand(new Vector3(pad * 2f, 0f, pad * 2f));
            mesh.bounds = bounds;
            return mesh;
        }

        private static int ChunkSeed(int seed, int cx, int cz)
        {
            unchecked
            {
                int hash = (int)2166136261;
                hash = (hash ^ seed) * 16777619;
                hash = (hash ^ cx) * 16777619;
                hash = (hash ^ cz) * 16777619;
                return hash;
            }
        }

        private static float Next(System.Random random)
        {
            return (float)random.NextDouble();
        }
    }
}
