using HarmonyLib;
using UnityEngine;
using Verse;
using static LuxandraLust.GameComponent_LuxandraLust;

namespace LuxandraLust
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.Graphic), MethodType.Getter)]
    public static class Patch_Thing_Graphic_Getter
    {
        [HarmonyPostfix]
        public static void Postfix(Thing __instance, ref Graphic __result)
        {
            if (!LuxandraModSettings.enableMonumentKinkShift)
                return;

            var comp = __instance.TryGetComp<Comp_LuxandraMonument>();
            if (comp != null && __instance.def != null)
            {
                StorytellerKink activeKink = CurrentKink;

                // Check our local cache first
                if (!comp.graphicCache.TryGetValue(activeKink, out Graphic customGraphic))
                {
                    string targetPath = $"{__instance.def.graphicData.texPath}_{activeKink}";

                    // FIXED: Check if the file actually exists in the game files. 
                    // ContentFinder expects a path relative to the Textures folder without an extension.
                    if (!ContentFinder<Texture2D>.Get(targetPath, false))
                    {
                        // Fallback path if file is missing
                        targetPath = $"{__instance.def.graphicData.texPath}_None";
                    }

                    // Request the graphic (either the active kink or the _None fallback)
                    customGraphic = GraphicDatabase.Get<Graphic_Single>(
                        targetPath,
                        __instance.def.graphic.Shader,
                        __instance.def.graphicData.drawSize,
                        __instance.DrawColor
                    );

                    // Cache it so we don't have to check files again
                    comp.graphicCache[activeKink] = customGraphic;
                }

                // Overwrite the final output asset
                __result = customGraphic;
            }
        }
    }
}