using System;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace KeyErrorFinn.Karambit
{
    internal sealed class KarambitReplacer
    {
        private const string ReplacementName = "KeyErrorFinn Karambit";
        private static readonly MethodInfo ApplySkinToRendererMethod = AccessTools.Method(
            AccessTools.TypeByName("ShaderManager"), "ApplyItemSkin",
            new[] { typeof(ItemSkin), typeof(Renderer), typeof(bool) });
        private static readonly FieldInfo InHandHolderField = AccessTools.Field(typeof(Item), "_inHandHolder");
        private static readonly FieldInfo OutOfHandHolderField = AccessTools.Field(typeof(Item), "_outOfHandHolder");
        private static readonly FieldInfo HandModelRightField = AccessTools.Field(typeof(Item), "_handModelRight");
        private static readonly FieldInfo HandModelLeftField = AccessTools.Field(typeof(Item), "_handModelLeft");

        private readonly ManualLogSource _logger;
        private readonly ConfigEntry<bool> _enabled;
        private readonly ConfigEntry<Vector3> _position;
        private readonly ConfigEntry<Vector3> _rotation;
        private readonly ConfigEntry<float> _scale;
        private readonly ConfigEntry<bool> _useOriginalTexture;
        private readonly ConfigEntry<float> _skinUvScale;
        private readonly ConfigEntry<float> _skinUvRotation;
        private readonly ConfigEntry<Vector2> _skinUvOffset;

        internal KarambitReplacer(ManualLogSource logger, ConfigEntry<bool> enabled, ConfigEntry<Vector3> position,
            ConfigEntry<Vector3> rotation, ConfigEntry<float> scale, ConfigEntry<bool> useOriginalTexture,
            ConfigEntry<float> skinUvScale, ConfigEntry<float> skinUvRotation, ConfigEntry<Vector2> skinUvOffset)
        {
            _logger = logger;
            _enabled = enabled;
            _position = position;
            _rotation = rotation;
            _scale = scale;
            _useOriginalTexture = useOriginalTexture;
            _skinUvScale = skinUvScale;
            _skinUvRotation = skinUvRotation;
            _skinUvOffset = skinUvOffset;
        }

        internal void TryReplace(Item item)
        {
            if (!_enabled.Value || item == null || !IsKnife(item))
                return;
            if (HasReplacement(item.transform))
            {
                RefreshExistingSkin(item);
                return;
            }

            var animated = FindDeepChild(item.transform, "KnifeAnimated");
            var rightIk = animated != null ? FindDeepChild(animated, "r_IK") : null;
            var knifeBone = rightIk != null ? FindDirectChild(rightIk, "Knife") : null;
            if (knifeBone == null)
            {
                _logger.LogWarning($"Found knife item '{item.name}', but could not find KnifeAnimated/r_IK/Knife.");
                return;
            }

            var sourceRenderer = FindVanillaKnifeRenderer(animated);
            if (sourceRenderer == null)
            {
                _logger.LogWarning("Found the right knife bone, but no vanilla SkinnedMeshRenderer named Knife.");
                return;
            }

            var replacement = new GameObject(ReplacementName);
            replacement.hideFlags = HideFlags.DontSave;
            replacement.transform.SetParent(knifeBone, false);
            replacement.transform.localPosition = _position.Value;
            replacement.transform.localRotation = Quaternion.Euler(_rotation.Value);
            // The game's first-person knife skeleton is authored in a smaller
            // coordinate space. This is the matching scale for its Knife bone.
            replacement.transform.localScale = Vector3.one * 25f * Mathf.Max(0.01f, _scale.Value);
            var model = ObjKarambitModel.Create(replacement.transform, sourceRenderer.material, _useOriginalTexture.Value,
                _skinUvScale.Value, _skinUvRotation.Value, _skinUvOffset.Value);

            foreach (var vanillaRenderer in animated.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (string.Equals(vanillaRenderer.gameObject.name, "Knife", StringComparison.Ordinal))
                    vanillaRenderer.enabled = false;
            }
            if (!_useOriginalTexture.Value)
                ApplyCurrentSkin(item, model.BladeRenderer);
            _logger.LogInfo($"Replaced knife mesh on '{item.name}'. Blade shader: {model.BladeRenderer.material.shader.name}; handle shader: {model.HandleRenderer.material.shader.name}.");
        }

        internal void ApplyLiveTransform()
        {
            foreach (var transform in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (!string.Equals(transform.name, ReplacementName, StringComparison.Ordinal))
                    continue;
                transform.localPosition = _position.Value;
                transform.localRotation = Quaternion.Euler(_rotation.Value);
                transform.localScale = Vector3.one * 25f * Mathf.Max(0.01f, _scale.Value);
            }
        }

        internal void RefreshSkin(Item item, byte skinIndex)
        {
            if (!_enabled.Value || _useOriginalTexture.Value || item == null || !IsKnife(item))
                return;

            var bladeRenderer = FindDeepChild(item.transform, "Karambit Blade")?.GetComponent<Renderer>();
            if (bladeRenderer == null)
                return;

            if (ApplySkin(item, skinIndex, bladeRenderer))
            {
                var skin = item.SkinPreset.Skins[skinIndex];
                _logger.LogInfo($"Applied knife skin {skinIndex} '{skin.Name}' to karambit blade (UseSkin={skin.UseSkin}, Rainbow={skin.IsRainbowSkin}, Affects={skin.SkinAffects}).");
            }
        }

        internal void ApplyMaterialMode()
        {
            foreach (var item in Resources.FindObjectsOfTypeAll<Item>())
            {
                if (item != null && IsKnife(item) && HasReplacement(item.transform))
                    ApplyMaterialMode(item);
            }
        }

        internal void ApplyEnabledState()
        {
            foreach (var item in Resources.FindObjectsOfTypeAll<Item>())
            {
                if (item == null || !IsKnife(item))
                    continue;

                var animated = FindDeepChild(item.transform, "KnifeAnimated");
                var replacement = FindDeepChild(item.transform, ReplacementName);
                if (_enabled.Value)
                {
                    if (replacement == null)
                    {
                        TryReplace(item);
                        continue;
                    }

                    replacement.gameObject.SetActive(true);
                    SetVanillaKnifeRenderers(animated, false);
                    ApplyMaterialMode(item);
                }
                else
                {
                    if (replacement != null)
                        replacement.gameObject.SetActive(false);
                    SetVanillaKnifeRenderers(animated, true);
                }
            }
        }

        private void ApplyMaterialMode(Item item)
        {
            var animated = FindDeepChild(item.transform, "KnifeAnimated");
            var sourceRenderer = animated != null ? FindVanillaKnifeRenderer(animated) : null;
            var bladeRenderer = FindDeepChild(item.transform, "Karambit Blade")?.GetComponent<Renderer>();
            var handleRenderer = FindDeepChild(item.transform, "Karambit Handle")?.GetComponent<Renderer>();
            if (sourceRenderer == null || bladeRenderer == null || handleRenderer == null)
                return;

            ObjKarambitModel.ApplyMaterialMode(bladeRenderer, handleRenderer, sourceRenderer.material,
                _useOriginalTexture.Value, _skinUvScale.Value, _skinUvRotation.Value, _skinUvOffset.Value);
            if (!_useOriginalTexture.Value)
                ApplyCurrentSkin(item, bladeRenderer);
        }

        private static bool IsKnife(Item item)
        {
            return string.Equals(item.name, "Knife", StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.GetName(), "Knife", StringComparison.OrdinalIgnoreCase);
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (string.Equals(child.name, name, StringComparison.Ordinal))
                    return child;
            }
            return null;
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent == null)
                return null;
            foreach (Transform child in parent)
            {
                if (string.Equals(child.name, name, StringComparison.Ordinal))
                    return child;
                var nested = FindDeepChild(child, name);
                if (nested != null)
                    return nested;
            }
            return null;
        }

        private static bool HasReplacement(Transform root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(transform.name, ReplacementName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static SkinnedMeshRenderer FindVanillaKnifeRenderer(Transform animated)
        {
            foreach (var renderer in animated.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (string.Equals(renderer.gameObject.name, "Knife", StringComparison.Ordinal))
                    return renderer;
            }
            return null;
        }

        private static void SetVanillaKnifeRenderers(Transform animated, bool enabled)
        {
            if (animated == null)
                return;

            foreach (var renderer in animated.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (string.Equals(renderer.gameObject.name, "Knife", StringComparison.Ordinal))
                    renderer.enabled = enabled;
            }
        }

        private static void ApplyCurrentSkin(Item item, Renderer bladeRenderer)
        {
            ApplySkin(item, item.CurSkin, bladeRenderer);
        }

        private static bool ApplySkin(Item item, int skinIndex, Renderer bladeRenderer)
        {
            var skins = item.SkinPreset?.Skins;
            if (skins == null || skinIndex < 0 || skinIndex >= skins.Count || bladeRenderer == null)
                return false;
            // A second hot-reload callback can arrive while original-texture
            // mode is active. Never feed game-local shader keywords into the
            // unskinned comparison material.
            if (bladeRenderer.sharedMaterial == null ||
                !string.Equals(bladeRenderer.sharedMaterial.shader.name, "Shader Graphs/DefaultShader", StringComparison.Ordinal))
                return false;

            ApplySkinToRendererMethod?.Invoke(null, new object[] { skins[skinIndex], bladeRenderer, false });
            return ApplySkinToRendererMethod != null;
        }

        private static void RefreshExistingSkin(Item item)
        {
            var bladeRenderer = FindDeepChild(item.transform, "Karambit Blade")?.GetComponent<Renderer>();
            if (bladeRenderer == null)
                return;

            ApplyCurrentSkin(item, bladeRenderer);
        }
    }
}
