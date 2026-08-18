using UnityEngine;

namespace BladeAndTitan.TitanShifting;

public static class FootprintStamp
{
    private struct Rectangle
    {
        public Vector2 position;
        public Vector2 size;
        public float rotation;
        public float depth;

        public Rectangle(
            Vector2 position,
            Vector2 size,
            float rotation,
            float depth)
        {
            this.position = position;
            this.size = size;
            this.rotation = rotation;
            this.depth = depth;
        }
    }

    // 60m titan footprint.
    // Local +Y points toward the toes.
    private static readonly Rectangle[] Foot =
    {
        // Main foot: 5m long
        new Rectangle(
            new Vector2(0f, 0f),
            new Vector2(2.2f, 5.0f),
            0f,
            0.18f
        ),

        // Big toe
        new Rectangle(
            new Vector2(-0.9f, 2.85f),
            new Vector2(0.45f, 1.0f),
            -8f,
            0.22f
        ),

        // Toe
        new Rectangle(
            new Vector2(-0.45f, 3.0f),
            new Vector2(0.5f, 1.1f),
            -4f,
            0.22f
        ),

        // Middle toe
        new Rectangle(
            new Vector2(0f, 3.05f),
            new Vector2(0.55f, 1.2f),
            0f,
            0.25f
        ),

        // Toe
        new Rectangle(
            new Vector2(0.45f, 3.0f),
            new Vector2(0.5f, 1.1f),
            4f,
            0.22f
        ),

        // Little toe
        new Rectangle(
            new Vector2(0.9f, 2.85f),
            new Vector2(0.45f, 1.0f),
            8f,
            0.22f
        )
    };

    public static void Stamp(
        Terrain terrain,
        Vector3 worldPosition,
        float worldRotation)
    {
        if (terrain == null)
            return;

        TerrainData data = terrain.terrainData;

        int resolution =
            data.heightmapResolution;

        Vector3 terrainPosition =
            terrain.transform.position;

        Vector3 terrainSize =
            data.size;

        Quaternion worldRot =
            Quaternion.Euler(
                0f,
                worldRotation,
                0f
            );

        // --------------------------------------------------
        // Find combined world-space bounds
        // --------------------------------------------------

        Bounds bounds = new Bounds(
            worldPosition,
            Vector3.zero
        );

        foreach (Rectangle rect in Foot)
        {
            Quaternion rectRotation =
                worldRot *
                Quaternion.Euler(
                    0f,
                    rect.rotation,
                    0f
                );

            Vector3 center =
                worldPosition +
                worldRot *
                new Vector3(
                    rect.position.x,
                    0f,
                    rect.position.y
                );

            Vector2 half =
                rect.size * 0.5f;

            Vector3 c1 =
                center +
                rectRotation *
                new Vector3(
                    -half.x,
                    0f,
                    -half.y
                );

            Vector3 c2 =
                center +
                rectRotation *
                new Vector3(
                    -half.x,
                    0f,
                    half.y
                );

            Vector3 c3 =
                center +
                rectRotation *
                new Vector3(
                    half.x,
                    0f,
                    -half.y
                );

            Vector3 c4 =
                center +
                rectRotation *
                new Vector3(
                    half.x,
                    0f,
                    half.y
                );

            bounds.Encapsulate(c1);
            bounds.Encapsulate(c2);
            bounds.Encapsulate(c3);
            bounds.Encapsulate(c4);
        }

        // --------------------------------------------------
        // Convert world bounds -> heightmap coordinates
        // --------------------------------------------------

        int minX = Mathf.FloorToInt(
            (bounds.min.x - terrainPosition.x) /
            terrainSize.x *
            (resolution - 1)
        );

        int maxX = Mathf.CeilToInt(
            (bounds.max.x - terrainPosition.x) /
            terrainSize.x *
            (resolution - 1)
        );

        int minZ = Mathf.FloorToInt(
            (bounds.min.z - terrainPosition.z) /
            terrainSize.z *
            (resolution - 1)
        );

        int maxZ = Mathf.CeilToInt(
            (bounds.max.z - terrainPosition.z) /
            terrainSize.z *
            (resolution - 1)
        );

        minX = Mathf.Clamp(
            minX,
            0,
            resolution - 1
        );

        maxX = Mathf.Clamp(
            maxX,
            0,
            resolution - 1
        );

        minZ = Mathf.Clamp(
            minZ,
            0,
            resolution - 1
        );

        maxZ = Mathf.Clamp(
            maxZ,
            0,
            resolution - 1
        );

        int width =
            maxX - minX + 1;

        int height =
            maxZ - minZ + 1;

        if (width <= 0 || height <= 0)
            return;

        // --------------------------------------------------
        // Get terrain heights ONCE
        // --------------------------------------------------

        float[,] heights =
            data.GetHeights(
                minX,
                minZ,
                width,
                height
            );

        // --------------------------------------------------
        // Stamp every rectangle into same buffer
        // --------------------------------------------------

        foreach (Rectangle rect in Foot)
        {
            StampRectangle(
                heights,
                minX,
                minZ,
                terrainPosition,
                terrainSize,
                resolution,
                worldPosition,
                worldRot,
                rect
            );
        }

        // --------------------------------------------------
        // Apply terrain ONCE
        // --------------------------------------------------

        data.SetHeights(
            minX,
            minZ,
            heights
        );
    }

    private static void StampRectangle(
        float[,] heights,
        int minX,
        int minZ,
        Vector3 terrainPosition,
        Vector3 terrainSize,
        int resolution,
        Vector3 worldPosition,
        Quaternion worldRotation,
        Rectangle rect)
    {
        Quaternion rectRotation =
            worldRotation *
            Quaternion.Euler(
                0f,
                rect.rotation,
                0f
            );

        Quaternion inverseRotation =
            Quaternion.Inverse(
                rectRotation
            );

        Vector3 center =
            worldPosition +
            worldRotation *
            new Vector3(
                rect.position.x,
                0f,
                rect.position.y
            );

        Vector2 half =
            rect.size * 0.5f;

        float normalizedDepth =
            rect.depth /
            terrainSize.y;

        // We only iterate the rectangle's
        // bounding area inside our already
        // allocated heightmap region.

        Vector3[] corners =
        {
            center + rectRotation *
            new Vector3(-half.x, 0f, -half.y),

            center + rectRotation *
            new Vector3(-half.x, 0f, half.y),

            center + rectRotation *
            new Vector3(half.x, 0f, -half.y),

            center + rectRotation *
            new Vector3(half.x, 0f, half.y)
        };

        float minWorldX = corners[0].x;
        float maxWorldX = corners[0].x;
        float minWorldZ = corners[0].z;
        float maxWorldZ = corners[0].z;

        for (int i = 1; i < 4; i++)
        {
            minWorldX =
                Mathf.Min(
                    minWorldX,
                    corners[i].x
                );

            maxWorldX =
                Mathf.Max(
                    maxWorldX,
                    corners[i].x
                );

            minWorldZ =
                Mathf.Min(
                    minWorldZ,
                    corners[i].z
                );

            maxWorldZ =
                Mathf.Max(
                    maxWorldZ,
                    corners[i].z
                );
        }

        int startX = Mathf.Max(
            minX,
            Mathf.FloorToInt(
                (minWorldX - terrainPosition.x) /
                terrainSize.x *
                (resolution - 1)
            )
        );

        int endX = Mathf.Min(
            minX + heights.GetLength(1) - 1,
            Mathf.CeilToInt(
                (maxWorldX - terrainPosition.x) /
                terrainSize.x *
                (resolution - 1)
            )
        );

        int startZ = Mathf.Max(
            minZ,
            Mathf.FloorToInt(
                (minWorldZ - terrainPosition.z) /
                terrainSize.z *
                (resolution - 1)
            )
        );

        int endZ = Mathf.Min(
            minZ + heights.GetLength(0) - 1,
            Mathf.CeilToInt(
                (maxWorldZ - terrainPosition.z) /
                terrainSize.z *
                (resolution - 1)
            )
        );

        // --------------------------------------------------
        // Stamp
        // --------------------------------------------------

        for (int z = startZ; z <= endZ; z++)
        {
            float worldZ =
                terrainPosition.z +
                z /
                (float)(resolution - 1) *
                terrainSize.z;

            for (int x = startX; x <= endX; x++)
            {
                float worldX =
                    terrainPosition.x +
                    x /
                    (float)(resolution - 1) *
                    terrainSize.x;

                Vector3 worldPoint =
                    new Vector3(
                        worldX,
                        center.y,
                        worldZ
                    );

                Vector3 local =
                    inverseRotation *
                    (worldPoint - center);

                float dx =
                    Mathf.Abs(local.x);

                float dz =
                    Mathf.Abs(local.z);

                if (dx > half.x ||
                    dz > half.y)
                    continue;

                // Soft edge.
                float edge =
                    Mathf.Min(
                        half.x - dx,
                        half.y - dz
                    );

                // 10cm-ish soft edge.
                float fadeWidth = 0.15f;

                float fade =
                    Mathf.Clamp01(
                        edge / fadeWidth
                    );

                // Smoothstep.
                fade =
                    fade * fade *
                    (3f - 2f * fade);

                int localX =
                    x - minX;

                int localZ =
                    z - minZ;

                heights[localZ, localX] -=
                    normalizedDepth * fade;
            }
        }
    }
}