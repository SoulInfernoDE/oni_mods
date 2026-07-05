using System;
using HarmonyLib;
using KMod;
using Newtonsoft.Json;
using UnityEngine;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace SoundSwitch
{
    // =========================================================================
    // 1. KONFIGURATION MIT PLIB-MENÜ
    // =========================================================================
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile("config.json", true, true)]
    public class SoundSwitchConfig
    {
        [Option("Hover-Sound deaktivieren", "Schaltet das Rascheln und Piepen beim Überfliegen von Objekten mit der Maus aus.")]
        [JsonProperty]
        public bool DisableMouseoverSound { get; set; } = true;

        [Option("Klick-Sound deaktivieren (Komplett lautlos)", "Schaltet den Klick-Sound komplett aus. HINWEIS: Wenn dies aktiv ist, wird kein Ersatz-Sound abgespielt!")]
        [JsonProperty]
        public bool DisableClickSound { get; set; } = false;

        [Option("Klick-Sound durch Hover ersetzen", "Spielt beim Anklicken stattdessen den Hover-Sound ab (Greift nur, wenn Klick-Sound nicht komplett deaktiviert ist).")]
        [JsonProperty]
        public bool ReplaceClickWithMouseover { get; set; } = true;
    }

    // =========================================================================
    // 2. HAUPTKLASSE
    // =========================================================================
    public class SoundSwitchMod : UserMod2
    {
        public static SoundSwitchConfig Settings { get; private set; }

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);

            PUtil.InitLibrary(false);
            new POptions().RegisterOptions(this, typeof(SoundSwitchConfig));

            Settings = POptions.ReadSettings<SoundSwitchConfig>() ?? new SoundSwitchConfig();

            Debug.Log("[SoundSwitch v1.2.1] Erfolgreich geladen – Logik strikt getrennt!");
        }
    }

    // =========================================================================
    // 3. HARMONY PATCHES MIT STRIKTER LOGIK-TRENNUNG
    // =========================================================================
    [HarmonyPatch(typeof(KSelectable), "Hover")]
    public static class Patch_KSelectable_Hover
    {
        public static void Prefix(ref bool playAudio)
        {
            if (SoundSwitchMod.Settings != null && SoundSwitchMod.Settings.DisableMouseoverSound)
            {
                playAudio = false;
            }
        }
    }

    [HarmonyPatch(typeof(SelectTool), "Select", new Type[] { typeof(KSelectable), typeof(bool) })]
    public static class Patch_SelectTool_Select
    {
        public static void Prefix(ref bool skipSound, out bool __state)
        {
            __state = !skipSound;

            // Der originale Klick von Klei wird unterdrückt, wenn wir ENT WEDER komplett stumm sein wollen 
            // ODER den Sound ersetzen möchten:
            if (SoundSwitchMod.Settings != null && 
               (SoundSwitchMod.Settings.DisableClickSound || SoundSwitchMod.Settings.ReplaceClickWithMouseover))
            {
                skipSound = true;
            }
        }

        public static void Postfix(KSelectable new_selected, bool __state)
        {
            try
            {
                // DER ENTSCHEIDENDE FIX:
                // Wir spielen den Ersatz-Sound NUR ab, wenn:
                // 1. "Ersetzen" in den Optionen auf TRUE steht
                // 2. UND "Klick deaktivieren" auf FALSE steht (Stille hat immer Vorfahrt!)
                if (new_selected != null && __state && SoundSwitchMod.Settings != null)
                {
                    bool wantReplace = SoundSwitchMod.Settings.ReplaceClickWithMouseover;
                    bool absoluteSilence = SoundSwitchMod.Settings.DisableClickSound;

                    if (wantReplace && !absoluteSilence)
                    {
                        string hoverSoundPath = GlobalAssets.GetSound("object_mouseover", false);
                        if (!string.IsNullOrEmpty(hoverSoundPath))
                        {
                            KFMOD.PlayOneShot(hoverSoundPath, Vector3.zero, 1f);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SoundSwitch] Fehler im Postfix: " + e.Message);
            }
        }
    }
}
