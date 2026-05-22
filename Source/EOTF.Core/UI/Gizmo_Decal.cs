using System;
using HarmonyLib;
using Verse;

namespace EOTF.Core.DecalSystem
{
    [StaticConstructorOnStartup]
    public static class DecalBootstrap
    {
        //Harmony bootstrap, mainly here so I can tell if this shit actually loaded
        static DecalBootstrap()
        {
            try
            {
                new Harmony("EOTF.Decals").PatchAll();
                Log.Message("[EOTF] Decal System loaded successfully.");
            }
            catch (Exception e)
            {
                Log.Error("[EOTF] Decal System failed to load:\n" + e);
            }
        }
    }
    
}