using System;
using HarmonyLib;
using KMod;
using UnityEngine;

namespace SoundSwitch
{
    public class SoundSwitchMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            Debug.Log("[SoundSwitch] Mod geladen – Hover-Parameter korrigiert.");
        }
    }

    // "playAudio"
    [HarmonyPatch(typeof(KSelectable), "Hover")]
    public static class Patch_KSelectable_Hover
    {
        public static void Prefix(ref bool playAudio)
        {
            // das Audio für diesen Hover-Vorgang wird ausgeschaltet
            playAudio = false;
        }
    }

    // "new_selected"
    [HarmonyPatch(typeof(SelectTool), "Select", new Type[] { typeof(KSelectable), typeof(bool) })]
    public static class Patch_SelectTool_Select
    {
        public static void Postfix(KSelectable new_selected, bool skipSound)
        {
            try
            {
                if (new_selected != null && !skipSound)
                {
                    string hoverSoundPath = GlobalAssets.GetSound("object_mouseover", false);
                    if (!string.IsNullOrEmpty(hoverSoundPath))
                    {
                        KFMOD.PlayOneShot(hoverSoundPath, Vector3.zero, 1f);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SoundSwitch] Fehler im Select-Patch: " + e.Message);
            }
        }
    }
}
