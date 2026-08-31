using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace KeyErrorFinn.Karambit
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class KarambitPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.keyerrorfinn.karambit";
        public const string PluginName = "How to Karambit";
        public const string PluginVersion = "1.0.0";

        internal static KarambitPlugin Instance { get; private set; }
        internal KarambitReplacer Replacer { get; private set; }
        private ConfigEntry<Vector3> _position;
        private ConfigEntry<Vector3> _rotation;
        private ConfigEntry<float> _scale;
        private ConfigEntry<bool> _enabled;
        private ConfigEntry<bool> _alternatePose;
        private ConfigEntry<KeyboardShortcut> _menuToggleHotkey;
        private ConfigEntry<bool> _useOriginalTexture;
        private bool _showMenu;

        private void Awake()
        {
            Instance = this;
            Replacer = new KarambitReplacer(
                Logger,
                _enabled = Config.Bind("Karambit", "Enabled", true, "Replace the held knife mesh with the karambit."),
                _position = Config.Bind("Karambit", "Position", new Vector3(1.06f, -1.76f, 1.74f), "Karambit position relative to the original knife mesh."),
                _rotation = Config.Bind("Karambit", "Rotation", new Vector3(183.49f, -442.97f, 200f), "Karambit rotation relative to the original knife mesh."),
                _scale = Config.Bind("Karambit", "Scale", 0.875f, "Karambit scale relative to the original knife mesh."),
                _useOriginalTexture = Config.Bind("Karambit", "Use original texture", false, "Use the model's original texture on the whole karambit instead of applying the selected knife skin to its blade and ring."),
                Config.Bind("Skin pattern", "UV scale", 1f, "Scale applied to the knife skin pattern."),
                Config.Bind("Skin pattern", "UV rotation", -60f, "Rotation applied to the knife skin pattern."),
                Config.Bind("Skin pattern", "UV offset", new Vector2(0.20f, 0.20f), "Offset applied to the knife skin pattern."));
            _alternatePose = Config.Bind("Karambit", "Use alternate pose", false, "Use the alternate karambit grip pose.");
            _menuToggleHotkey = Config.Bind("Karambit", "Toggle menu hotkey", new KeyboardShortcut(KeyCode.F10), "Open or close the karambit settings menu.");

            new Harmony(PluginGuid).PatchAll(typeof(KarambitPlugin).Assembly);
            SetPose(_alternatePose.Value, false);
            Replacer.ApplyMaterialMode();
            Replacer.ApplyEnabledState();
            Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }

        private void Update()
        {
            if (_menuToggleHotkey.Value.IsDown())
                _showMenu = !_showMenu;

            if (!_showMenu || !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
                return;

            if (Input.GetKeyDown(KeyCode.RightArrow))
                SetPose(!_alternatePose.Value, true);
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
                SetMaterialMode(!_useOriginalTexture.Value, true);
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                SetEnabled(!_enabled.Value, true);
        }

        private void OnGUI()
        {
            if (!_showMenu)
                return;

            var left = (Screen.width - 360f) * 0.5f;
            GUI.Box(new Rect(left, 20f, 360f, 224f), "Karambit settings (F10 closes)");
            GUI.Label(new Rect(left + 14f, 50f, 320f, 20f), "Pose: Ctrl+Right Arrow");
            if (GUI.Button(new Rect(left + 14f, 74f, 150f, 28f), _alternatePose.Value ? "Blade Down" : "Blade Down  ✓"))
                SetPose(false, true);
            if (GUI.Button(new Rect(left + 174f, 74f, 150f, 28f), _alternatePose.Value ? "Blade Up  ✓" : "Blade Up"))
                SetPose(true, true);

            GUI.Label(new Rect(left + 14f, 110f, 320f, 20f), "Material: Ctrl+Left Arrow");
            if (GUI.Button(new Rect(left + 14f, 134f, 150f, 28f), _useOriginalTexture.Value ? "Skins" : "Skins  ✓"))
                SetMaterialMode(false, true);
            if (GUI.Button(new Rect(left + 174f, 134f, 150f, 28f), _useOriginalTexture.Value ? "Model Default  ✓" : "Model Default"))
                SetMaterialMode(true, true);

            GUI.Label(new Rect(left + 14f, 170f, 320f, 20f), "Knife: Ctrl+Down Arrow");
            if (GUI.Button(new Rect(left + 14f, 194f, 150f, 28f), _enabled.Value ? "Karambit  ✓" : "Karambit"))
                SetEnabled(true, true);
            if (GUI.Button(new Rect(left + 174f, 194f, 150f, 28f), _enabled.Value ? "Default Knife" : "Default Knife  ✓"))
                SetEnabled(false, true);
        }

        private void SetPose(bool alternate, bool save)
        {
            _alternatePose.Value = alternate;
            _position.Value = alternate ? new Vector3(0.22f, 1.51f, 2.75f) : new Vector3(1.06f, -1.76f, 1.74f);
            _rotation.Value = alternate ? new Vector3(183.70f, -260.97f, 0f) : new Vector3(183.49f, -442.97f, 200f);
            _scale.Value = 0.875f;
            Replacer?.ApplyLiveTransform();
            if (save) Config.Save();
        }

        private void SetMaterialMode(bool useOriginalTexture, bool save)
        {
            _useOriginalTexture.Value = useOriginalTexture;
            Replacer?.ApplyMaterialMode();
            Logger.LogInfo(useOriginalTexture ? "Karambit material: original model texture." : "Karambit material: original textured handle with selected knife skin on blade and ring.");
            if (save) Config.Save();
        }

        private void SetEnabled(bool enabled, bool save)
        {
            _enabled.Value = enabled;
            Replacer?.ApplyEnabledState();
            Logger.LogInfo(enabled ? "Knife model: karambit." : "Knife model: game default.");
            if (save) Config.Save();
        }
    }
}
