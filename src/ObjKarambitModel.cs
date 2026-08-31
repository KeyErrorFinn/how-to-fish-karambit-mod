using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace KeyErrorFinn.Karambit
{
    internal sealed class KarambitModel
    {
        internal Renderer BladeRenderer;
        internal Renderer HandleRenderer;
    }

    internal static class ObjKarambitModel
    {
        private static Texture2D _originalTexture;
        private static ObjData _objData;

        internal static KarambitModel Create(Transform parent, Material bladeMaterial, bool useOriginalTexture,
            float skinUvScale, float skinUvRotation, Vector2 skinUvOffset)
        {
            var data = GetObjData();
            var result = new KarambitModel();
            // The game changes skin values directly on renderer.material. Give the
            // blade its own material instance so applying a skin cannot bleed into
            // the handle or another knife.
            result.BladeRenderer = CreatePart("Karambit Blade", parent, data.BladeTriangles, data, null,
                !useOriginalTexture, skinUvScale, skinUvRotation, skinUvOffset);
            result.HandleRenderer = CreatePart("Karambit Handle", parent, data.HandleTriangles, data, null,
                false, 1f, 0f, Vector2.zero);
            ApplyMaterialMode(result.BladeRenderer, result.HandleRenderer, bladeMaterial, useOriginalTexture,
                skinUvScale, skinUvRotation, skinUvOffset);
            return result;
        }

        internal static void ApplyMaterialMode(Renderer blade, Renderer handle, Material source, bool useOriginalTexture,
            float skinUvScale, float skinUvRotation, Vector2 skinUvOffset)
        {
            RebuildBladeMesh(blade, !useOriginalTexture, skinUvScale, skinUvRotation, skinUvOffset);
            if (useOriginalTexture)
            {
                var original = CreateOriginalTextureMaterial();
                blade.sharedMaterial = original;
                handle.sharedMaterial = original;
                return;
            }

            blade.sharedMaterial = CreateSkinnableBladeMaterial(source);
            handle.sharedMaterial = CreateOriginalTextureMaterial();
        }

        private static Material CreateSkinnableBladeMaterial(Material source)
        {
            var material = new Material(source) { name = "Karambit Blade Skin Material" };
            var neutralNormal = CreateSolidTexture("Karambit Neutral Normal", new Color(0.5f, 0.5f, 1f, 1f));
            SetTextureIfPresent(material, "_Normal_Map", neutralNormal);
            return material;
        }

        private static Material CreateOriginalTextureMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("UI/Default");
            if (shader == null)
                throw new InvalidOperationException("Could not find a shader for the original karambit texture.");

            if (_originalTexture == null)
            {
                var assetDirectory = ResolveAssetDirectory();
                _originalTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    name = "Original Karambit Texture",
                    hideFlags = HideFlags.DontSave,
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Bilinear
                };
                var imageConversion = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
                var loadImage = imageConversion?.GetMethod("LoadImage", BindingFlags.Public | BindingFlags.Static, null,
                    new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) }, null);
                var loaded = loadImage != null && (bool)loadImage.Invoke(null,
                    new object[] { _originalTexture, File.ReadAllBytes(Path.Combine(assetDirectory, "karambit.png")), false });
                if (!loaded)
                    throw new InvalidDataException("Could not decode assets/karambit.png.");
            }

            var material = new Material(shader) { name = "Original Karambit Material", color = Color.white };
            material.mainTexture = _originalTexture;
            SetTextureIfPresent(material, "_BaseMap", _originalTexture);
            SetTextureIfPresent(material, "_MainTex", _originalTexture);
            SetColorIfPresent(material, "_BaseColor", Color.white);
            SetColorIfPresent(material, "_Color", Color.white);
            SetFloatIfPresent(material, "_Metallic", 0.35f);
            SetFloatIfPresent(material, "_Smoothness", 0.45f);
            return material;
        }

        private static string ResolveAssetDirectory()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
                var besideAssembly = Path.Combine(assemblyDirectory, "assets");
                if (File.Exists(Path.Combine(besideAssembly, "karanbit.obj")))
                    return besideAssembly;
            }

            var scriptsAssets = Path.Combine(Paths.BepInExRootPath, "scripts", "assets");
            if (File.Exists(Path.Combine(scriptsAssets, "karanbit.obj")))
                return scriptsAssets;

            var pluginAssets = Path.Combine(Paths.PluginPath, "KeyErrorFinn-Karambit", "assets");
            if (File.Exists(Path.Combine(pluginAssets, "karanbit.obj")))
                return pluginAssets;

            throw new DirectoryNotFoundException("Could not find the Karambit assets directory.");
        }

        private static ObjData GetObjData()
        {
            return _objData ?? (_objData = ObjData.Load(Path.Combine(ResolveAssetDirectory(), "karanbit.obj")));
        }

        internal static void RebuildBladeMesh(Renderer blade, bool useKnifeSkinUv, float skinUvScale,
            float skinUvRotation, Vector2 skinUvOffset)
        {
            var filter = blade.GetComponent<MeshFilter>();
            if (filter == null)
                return;
            var data = GetObjData();
            var oldMesh = filter.sharedMesh;
            filter.sharedMesh = data.BuildMesh("Karambit Blade", data.BladeTriangles, useKnifeSkinUv,
                skinUvScale, skinUvRotation, skinUvOffset);
            if (oldMesh != null)
                UnityEngine.Object.Destroy(oldMesh);
        }

        private static Texture2D CreateSolidTexture(string name, Color color)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = name,
                hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            texture.SetPixels(new[] { color, color, color, color });
            texture.Apply(false, true);
            return texture;
        }

        private static void SetTextureIfPresent(Material material, string property, Texture texture)
        {
            if (material.HasProperty(property))
                material.SetTexture(property, texture);
        }

        private static void SetFloatIfPresent(Material material, string property, float value)
        {
            if (material.HasProperty(property))
                material.SetFloat(property, value);
        }

        private static void SetColorIfPresent(Material material, string property, Color value)
        {
            if (material.HasProperty(property))
                material.SetColor(property, value);
        }

        private static Renderer CreatePart(string name, Transform parent, List<ObjTriangle> triangles, ObjData data,
            Material material, bool useKnifeSkinUv, float skinUvScale, float skinUvRotation, Vector2 skinUvOffset)
        {
            var part = new GameObject(name);
            part.hideFlags = HideFlags.DontSave;
            part.transform.SetParent(parent, false);
            part.layer = parent.gameObject.layer;
            var filter = part.AddComponent<MeshFilter>();
            filter.sharedMesh = data.BuildMesh(name, triangles, useKnifeSkinUv,
                skinUvScale, skinUvRotation, skinUvOffset);
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

    }

    internal sealed class ObjData
    {
        internal readonly List<Vector3> Positions = new List<Vector3>();
        internal readonly List<Vector2> Uvs = new List<Vector2>();
        internal readonly List<ObjTriangle> BladeTriangles = new List<ObjTriangle>();
        internal readonly List<ObjTriangle> HandleTriangles = new List<ObjTriangle>();

        internal static ObjData Load(string path)
        {
            var data = new ObjData();
            var blade = true;
            foreach (var rawLine in File.ReadAllLines(path))
            {
                var parts = rawLine.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;
                if (parts[0] == "v" && parts.Length >= 4)
                    data.Positions.Add(new Vector3(Parse(parts[1]), Parse(parts[2]), Parse(parts[3])));
                else if (parts[0] == "vt" && parts.Length >= 3)
                    data.Uvs.Add(new Vector2(Parse(parts[1]), Parse(parts[2])));
                else if (parts[0] == "usemtl" && parts.Length >= 2)
                    blade = string.Equals(parts[1], "blade", StringComparison.OrdinalIgnoreCase);
                else if (parts[0] == "f" && parts.Length >= 4)
                {
                    var face = new ObjIndex[parts.Length - 1];
                    for (var index = 1; index < parts.Length; index++) face[index - 1] = ObjIndex.Parse(parts[index]);
                    for (var index = 1; index < face.Length - 1; index++)
                    {
                        var triangle = new ObjTriangle(face[0], face[index], face[index + 1]);
                        (blade ? data.BladeTriangles : data.HandleTriangles).Add(triangle);
                    }
                }
            }
            if (data.BladeTriangles.Count == 0 || data.HandleTriangles.Count == 0)
                throw new InvalidDataException("The karambit OBJ must contain both blade and karambit material groups.");
            return data;
        }

        internal Mesh BuildMesh(string name, List<ObjTriangle> triangles, bool useKnifeSkinUv,
            float skinUvScale, float skinUvRotation, Vector2 skinUvOffset)
        {
            var map = new Dictionary<ObjIndex, int>();
            var vertices = new List<Vector3>();
            var uv0 = new List<Vector2>();
            var uv1 = new List<Vector2>();
            var uv2 = new List<Vector2>();
            var uv3 = new List<Vector2>();
            var indices = new List<int>();
            foreach (var triangle in triangles)
            {
                Add(triangle.A, map, vertices, uv0, uv1, uv2, uv3, indices, useKnifeSkinUv,
                    skinUvScale, skinUvRotation, skinUvOffset);
                Add(triangle.B, map, vertices, uv0, uv1, uv2, uv3, indices, useKnifeSkinUv,
                    skinUvScale, skinUvRotation, skinUvOffset);
                Add(triangle.C, map, vertices, uv0, uv1, uv2, uv3, indices, useKnifeSkinUv,
                    skinUvScale, skinUvRotation, skinUvOffset);
            }
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv0);
            mesh.SetUVs(1, uv1);
            mesh.SetUVs(2, uv2);
            mesh.SetUVs(3, uv3);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void Add(ObjIndex source, Dictionary<ObjIndex, int> map, List<Vector3> vertices,
            List<Vector2> uv0, List<Vector2> uv1, List<Vector2> uv2, List<Vector2> uv3,
            List<int> indices, bool useKnifeSkinUv,
            float skinUvScale, float skinUvRotation, Vector2 skinUvOffset)
        {
            int index;
            if (!map.TryGetValue(source, out index))
            {
                index = vertices.Count;
                map.Add(source, index);
                vertices.Add(Positions[source.Position - 1]);
                var originalUv = source.Uv > 0 ? Uvs[source.Uv - 1] : Vector2.zero;
                // The game's 32x32 colour atlas encodes material type in UV rows.
                // Its vanilla blade uses row 18 at the last column. Point the
                // whole custom blade/ring at that metallic row so OnlyMetallic
                // skins affect every face instead of a tiny accidental patch.
                if (useKnifeSkinUv)
                {
                    // These are the main metallic blade values from the vanilla
                    // knife. The 0.5776 row used previously is its plastic
                    // handle group, so OnlyMetallic skins correctly ignored it.
                    uv0.Add(new Vector2(0.9858f, 0.6968f));
                    uv1.Add(new Vector2(0.9775f, 0.6334f));
                    uv2.Add(new Vector2(0.9858f, 0.5171f));

                    // Normalize the OBJ blade island, apply the configured transform,
                    // then pack it into the exact UV3 region used by the vanilla
                    // knife blade. UV3 is the shader's procedural pattern input.
                    var normalized = new Vector2(
                        Mathf.InverseLerp(0.04f, 0.94f, originalUv.x),
                        Mathf.InverseLerp(0.09f, 0.80f, originalUv.y));
                    var transformed = TransformPatternUv(normalized, skinUvScale, skinUvRotation, skinUvOffset);
                    uv3.Add(new Vector2(
                        Mathf.LerpUnclamped(0.45f, 0.54f, transformed.x),
                        Mathf.LerpUnclamped(0.55f, 0.85f, transformed.y)));
                }
                else
                {
                    uv0.Add(originalUv);
                    uv1.Add(originalUv);
                    uv2.Add(originalUv);
                    uv3.Add(originalUv);
                }
            }
            indices.Add(index);
        }

        private static Vector2 TransformPatternUv(Vector2 uv, float scale, float rotationDegrees, Vector2 offset)
        {
            var centered = (uv - new Vector2(0.5f, 0.5f)) * Mathf.Max(0.01f, scale);
            var radians = rotationDegrees * Mathf.Deg2Rad;
            var cosine = Mathf.Cos(radians);
            var sine = Mathf.Sin(radians);
            var rotated = new Vector2(
                centered.x * cosine - centered.y * sine,
                centered.x * sine + centered.y * cosine);
            return rotated + new Vector2(0.5f, 0.5f) + offset;
        }

        private static float Parse(string value) => float.Parse(value, CultureInfo.InvariantCulture);
    }

    internal struct ObjIndex : IEquatable<ObjIndex>
    {
        internal int Position;
        internal int Uv;

        internal static ObjIndex Parse(string value)
        {
            var parts = value.Split('/');
            return new ObjIndex { Position = int.Parse(parts[0], CultureInfo.InvariantCulture), Uv = parts.Length > 1 && parts[1].Length > 0 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : 0 };
        }

        public bool Equals(ObjIndex other) => Position == other.Position && Uv == other.Uv;
        public override bool Equals(object obj) => obj is ObjIndex && Equals((ObjIndex)obj);
        public override int GetHashCode() => Position * 397 ^ Uv;
    }

    internal struct ObjTriangle
    {
        internal ObjIndex A;
        internal ObjIndex B;
        internal ObjIndex C;
        internal ObjTriangle(ObjIndex a, ObjIndex b, ObjIndex c) { A = a; B = b; C = c; }
    }
}
