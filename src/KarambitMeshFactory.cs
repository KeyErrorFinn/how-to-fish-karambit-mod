using System.Collections.Generic;
using UnityEngine;

namespace KeyErrorFinn.Karambit
{
    internal static class KarambitMeshFactory
    {
        internal static Mesh Create()
        {
            var root = new Mesh { name = "KeyErrorFinn Karambit Mesh" };
            Combine(root, CreateCurvedBlade(), CreateBox(-0.075f, 0.03f, 0.075f, 0.32f, 0.075f),
                CreateTorus(0.085f, 0.022f, 20, 7, new Vector3(0f, 0.02f, 0f)),
                CreateBox(-0.105f, 0.285f, 0.105f, 0.335f, 0.075f));
            root.RecalculateNormals();
            root.RecalculateBounds();
            var bounds = root.bounds;
            var uvs = new Vector2[root.vertexCount];
            for (var index = 0; index < root.vertexCount; index++)
            {
                var point = root.vertices[index];
                uvs[index] = new Vector2(
                    Mathf.InverseLerp(bounds.min.x, bounds.max.x, point.x),
                    Mathf.InverseLerp(bounds.min.y, bounds.max.y, point.y));
            }
            root.uv = uvs;
            return root;
        }

        private static Mesh CreateCurvedBlade()
        {
            var points = new[]
            {
                new Vector2(0f, 0.29f), new Vector2(0.20f, 0.40f), new Vector2(0.34f, 0.58f),
                new Vector2(0.32f, 0.77f), new Vector2(0.16f, 0.94f), new Vector2(-0.08f, 1.02f),
                new Vector2(-0.28f, 0.93f)
            };
            const float width = 0.055f;
            const float depth = 0.035f;
            var vertices = new Vector3[points.Length * 4];
            var triangles = new List<int>();
            for (var index = 0; index < points.Length; index++)
            {
                var tangent = index == 0 ? points[1] - points[0]
                    : index == points.Length - 1 ? points[index] - points[index - 1]
                    : points[index + 1] - points[index - 1];
                tangent.Normalize();
                var normal = new Vector2(-tangent.y, tangent.x);
                var taper = Mathf.Lerp(1f, 0.12f, index / (float)(points.Length - 1));
                var left = points[index] + normal * width * taper;
                var right = points[index] - normal * width * taper;
                var baseIndex = index * 4;
                vertices[baseIndex] = new Vector3(left.x, left.y, -depth * 0.5f);
                vertices[baseIndex + 1] = new Vector3(right.x, right.y, -depth * 0.5f);
                vertices[baseIndex + 2] = new Vector3(left.x, left.y, depth * 0.5f);
                vertices[baseIndex + 3] = new Vector3(right.x, right.y, depth * 0.5f);
            }
            for (var index = 0; index < points.Length - 1; index++)
            {
                var a = index * 4;
                var b = a + 4;
                AddQuad(triangles, a, b, b + 1, a + 1);
                AddQuad(triangles, a + 2, a + 3, b + 3, b + 2);
                AddQuad(triangles, a, a + 2, b + 2, b);
                AddQuad(triangles, a + 1, b + 1, b + 3, a + 3);
            }
            AddQuad(triangles, 0, 1, 3, 2);
            var end = (points.Length - 1) * 4;
            AddQuad(triangles, end, end + 2, end + 3, end + 1);
            return Finish("Karambit Curved Blade", vertices, triangles);
        }

        private static Mesh CreateBox(float xMin, float yMin, float xMax, float yMax, float depth)
        {
            var vertices = new[]
            {
                new Vector3(xMin, yMin, -depth / 2), new Vector3(xMax, yMin, -depth / 2), new Vector3(xMax, yMax, -depth / 2), new Vector3(xMin, yMax, -depth / 2),
                new Vector3(xMin, yMin, depth / 2), new Vector3(xMax, yMin, depth / 2), new Vector3(xMax, yMax, depth / 2), new Vector3(xMin, yMax, depth / 2)
            };
            var triangles = new List<int>();
            AddQuad(triangles, 0, 3, 2, 1); AddQuad(triangles, 4, 5, 6, 7);
            AddQuad(triangles, 0, 4, 7, 3); AddQuad(triangles, 1, 2, 6, 5);
            AddQuad(triangles, 3, 7, 6, 2); AddQuad(triangles, 0, 1, 5, 4);
            return Finish("Karambit Handle", vertices, triangles);
        }

        private static Mesh CreateTorus(float radius, float tube, int segments, int sides, Vector3 offset)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (var segment = 0; segment < segments; segment++)
            {
                var major = segment / (float)segments * Mathf.PI * 2f;
                for (var side = 0; side < sides; side++)
                {
                    var minor = side / (float)sides * Mathf.PI * 2f;
                    var ring = radius + tube * Mathf.Cos(minor);
                    vertices.Add(offset + new Vector3(Mathf.Cos(major) * ring, Mathf.Sin(major) * ring, tube * Mathf.Sin(minor)));
                }
            }
            for (var segment = 0; segment < segments; segment++)
            for (var side = 0; side < sides; side++)
            {
                var a = segment * sides + side;
                var b = ((segment + 1) % segments) * sides + side;
                var c = ((segment + 1) % segments) * sides + (side + 1) % sides;
                var d = segment * sides + (side + 1) % sides;
                AddQuad(triangles, a, b, c, d);
            }
            return Finish("Karambit Finger Ring", vertices.ToArray(), triangles);
        }

        private static Mesh Finish(string name, Vector3[] vertices, List<int> triangles)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.vertices = vertices;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddQuad(List<int> triangles, int a, int b, int c, int d)
        {
            triangles.Add(a); triangles.Add(b); triangles.Add(c);
            triangles.Add(a); triangles.Add(c); triangles.Add(d);
        }

        private static void Combine(Mesh destination, params Mesh[] parts)
        {
            var combines = new CombineInstance[parts.Length];
            for (var index = 0; index < parts.Length; index++) combines[index] = new CombineInstance { mesh = parts[index] };
            destination.CombineMeshes(combines, true, false);
            foreach (var part in parts) Object.Destroy(part);
        }
    }
}
