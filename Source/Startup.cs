using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;
using Verse;

namespace LuxandraLust
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            var harmony = new Harmony("world87.luxandralust");
            harmony.PatchAll();

            Log.Message("[Luxandra Lust] loaded successfully");
        }
    }
}