using HarmonyLib;

namespace KeyErrorFinn.Karambit
{
    [HarmonyPatch(typeof(Item), "Awake")]
    internal static class ItemAwakePatch
    {
        private static void Postfix(Item __instance)
        {
            KarambitPlugin.Instance?.Replacer.TryReplace(__instance);
        }
    }

    [HarmonyPatch(typeof(Item), "OnPickUp")]
    internal static class ItemOnPickUpPatch
    {
        private static void Postfix(Item __instance)
        {
            // Some visual holders are enabled only when the item is picked up.
            KarambitPlugin.Instance?.Replacer.TryReplace(__instance);
        }
    }

    [HarmonyPatch(typeof(Item), "OnCurSkinChange")]
    internal static class ItemSkinPatch
    {
        private static void Postfix(Item __instance, byte next, bool asServer)
        {
            if (asServer)
                return;

            var replacer = KarambitPlugin.Instance?.Replacer;
            replacer?.TryReplace(__instance);
            // FishNet supplies the newly selected value as `next`; CurSkin may
            // still expose the previous value while this callback is running.
            replacer?.RefreshSkin(__instance, next);
        }
    }
}
