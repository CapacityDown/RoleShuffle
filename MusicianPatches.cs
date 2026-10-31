using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

[HarmonyPatch]
internal static class MusicianPatches
{
    internal static bool Supported(ValuableMusicInstrument.Instrument instrument) => instrument is
        ValuableMusicInstrument.Instrument.Harmonica or ValuableMusicInstrument.Instrument.PanFlute or
        ValuableMusicInstrument.Instrument.MouthHarp;

    internal static bool HasInstrumentContext()
    {
        if (UnityEngine.Object.FindObjectsByType<MusicalValuableLogic>(FindObjectsInactive.Include,
            FindObjectsSortMode.InstanceID).Length > 0) return true;
        foreach (ValuableMusicInstrument instrument in UnityEngine.Object.FindObjectsByType<ValuableMusicInstrument>(
            FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
            if (Supported(instrument.instrument)) return true;
        return false;
    }

    // PlayKey runs only after vanilla has accepted the note RPC and key index.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(MusicalValuableLogic), "PlayKey")]
    private static void NotePlayed(int grabberID) =>
        StageRolesPlugin.Instance?.Controller?.InstrumentNotePlayed(grabberID);

    // Native SoundUpdate refreshes loudness on the host, including vanilla
    // guests' voice audio. Holding a silent or unattached instrument is not play.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ValuableMusicInstrument), "SoundUpdate")]
    private static void ContinuousPlayed(ValuableMusicInstrument __instance,
        ValuableMusicInstrument.State ___currentState, PlayerAvatar ___attachedPlayer,
        bool ___active, float ___currentLoudness)
    {
        if (__instance.isActiveAndEnabled && Supported(__instance.instrument) && ___active &&
            ___currentState == ValuableMusicInstrument.State.Attached && ___attachedPlayer != null &&
            ___currentLoudness > __instance.loudnessThreshold)
            StageRolesPlugin.Instance?.Controller?.InstrumentPlayed(___attachedPlayer);
    }
}
