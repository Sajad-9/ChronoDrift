using UnityEngine;
using System.Collections.Generic;

namespace ChronoDrift.Core
{
    /// <summary>
    /// Generates stylized procedural 3D meshes for ships, enemies, and geometric props
    /// so the project requires zero external model assets.
    /// </summary>
    public static class ProceduralMeshFactory
    {
        private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        public static Mesh GetPlayerShipMesh()
        {
            if (Cache.TryGetValue("PlayerShip", out var cached) && cached != null) return cached;

            var mesh = new Mesh { name = "PlayerShipMesh" };
            // Sleek delta-wing craft pointing +Z
            Vector3 nose = new Vector3(0f, 0.05f, 1.15f);
            Vector3 cockpitTop = new Vector3(0f, 0.42f, 0.1f);
            Vector3 tailCenter = new Vector3(0f, 0.08f, -0.65f);
            Vector3 leftWing = new Vector3(-0.95f, -0.05f, -0.85f);
            Vector3 rightWing = new Vector3(0.95f, -0.05f, -0.85f);
            Vector3 belly = new Vector3(0f, -0.22f, 0.0f);

            Vector3[] verts =
            {
                // Top left fuselage
                nose, leftWing, cockpitTop,
                // Top right fuselage
                nose, cockpitTop, rightWing,
                // Rear left
                cockpitTop, leftWing, tailCenter,
                // Rear right
                cockpitTop, tailCenter, rightWing,
                // Bottom left
                nose, belly, leftWing,
                // Bottom right
                nose, rightWing, belly,
                // Rear bottom left
                belly, tailCenter, leftWing,
                // Rear bottom right
                belly, rightWing, tailCenter
            };

            int[] tris = new int[verts.Length];
            for (int i = 0; i < tris.Length; i++) tris[i] = i;

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Cache["PlayerShip"] = mesh;
            return mesh;
        }

        public static Mesh GetTetrahedronMesh(float scale = 1f)
        {
            string key = $"Tetra_{scale:F2}";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var mesh = new Mesh { name = key };
            Vector3 tip = new Vector3(0f, 0f, 0.9f) * scale;
            Vector3 top = new Vector3(0f, 0.7f, -0.5f) * scale;
            Vector3 bl = new Vector3(-0.65f, -0.45f, -0.5f) * scale;
            Vector3 br = new Vector3(0.65f, -0.45f, -0.5f) * scale;

            Vector3[] verts =
            {
                tip, bl, top,
                tip, top, br,
                tip, br, bl,
                top, bl, br
            };
            int[] tris = new int[verts.Length];
            for (int i = 0; i < tris.Length; i++) tris[i] = i;

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Cache[key] = mesh;
            return mesh;
        }

        public static Mesh GetOctahedronMesh(float radius = 0.85f)
        {
            string key = $"Octa_{radius:F2}";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var mesh = new Mesh { name = key };
            Vector3 top = Vector3.up * radius;
            Vector3 bot = Vector3.down * radius;
            Vector3 f = Vector3.forward * radius;
            Vector3 b = Vector3.back * radius;
            Vector3 l = Vector3.left * radius;
            Vector3 r = Vector3.right * radius;

            Vector3[] verts =
            {
                top, f, r,
                top, r, b,
                top, b, l,
                top, l, f,
                bot, r, f,
                bot, b, r,
                bot, l, b,
                bot, f, l
            };
            int[] tris = new int[verts.Length];
            for (int i = 0; i < tris.Length; i++) tris[i] = i;

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Cache[key] = mesh;
            return mesh;
        }

        public static Mesh GetDiamondMesh(float length = 1.2f, float width = 0.55f)
        {
            string key = $"Diamond_{length:F2}_{width:F2}";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var mesh = new Mesh { name = key };
            Vector3 front = new Vector3(0f, 0f, length);
            Vector3 back = new Vector3(0f, 0f, -length * 0.7f);
            Vector3 up = new Vector3(0f, width, 0f);
            Vector3 down = new Vector3(0f, -width, 0f);
            Vector3 left = new Vector3(-width, 0f, 0f);
            Vector3 right = new Vector3(width, 0f, 0f);

            Vector3[] verts =
            {
                front, up, right,
                front, right, down,
                front, down, left,
                front, left, up,
                back, right, up,
                back, down, right,
                back, left, down,
                back, up, left
            };
            int[] tris = new int[verts.Length];
            for (int i = 0; i < tris.Length; i++) tris[i] = i;

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Cache[key] = mesh;
            return mesh;
        }
    }
}
