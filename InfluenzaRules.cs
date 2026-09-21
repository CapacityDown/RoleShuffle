using System;

namespace REPOJP.StageRoles;

internal static class InfluenzaRules
{
    internal const float IncubationSeconds = 30f;
    internal const int MaximumHealth = 75;
    internal const float SpeechSilenceSeconds = 0.75f;
    // Match vanilla voice investigation; each enemy applies its own hearing multiplier.
    internal const float SneezeInvestigateRadius = 5f;

    // Uniform interval between sorted bounds; defaults average 60 seconds.
    internal static float SneezeInterval(double sample, float minimum = 30f, float maximum = 90f) =>
        Math.Min(minimum, maximum) + Math.Abs(maximum - minimum) * (float)Math.Clamp(sample, 0d, 1d);

    internal static bool Infects(double sample, bool sneeze, double sneezePercent = 60d, double speechPercent = 30d)
    {
        double chance = Math.Clamp(sneeze ? sneezePercent : speechPercent, 0d, 100d);
        // Unity's Random.value can include 1; a configured 100% must include it.
        return sample >= 0d && sample <= 1d && (chance == 100d || sample < chance / 100d);
    }

    // Distance is three-dimensional; the requested left/right fan is horizontal.
    internal static bool InRange(double distanceSquared, double horizontalDot, bool sneeze,
        double sneezeRange = 5d, double speechRange = 3d, double sneezeAngle = 40d, double speechAngle = 60d)
    {
        double range = sneeze ? sneezeRange : speechRange;
        double halfAngle = (sneeze ? sneezeAngle : speechAngle) / 2d;
        return distanceSquared >= 0d && distanceSquared <= range * range &&
            horizontalDot + 0.000001d >= Math.Cos(halfAngle * Math.PI / 180d);
    }
}

internal sealed class InfluenzaState
{
    internal InfluenzaState(float infectedAt, float incubationSeconds = InfluenzaRules.IncubationSeconds) =>
        OnsetAt = infectedAt + incubationSeconds;
    internal float OnsetAt { get; }
    internal float NextSneezeAt { get; set; } = float.PositiveInfinity;
    internal bool SymptomsStarted { get; set; }
    internal bool HealthCaptured { get; private set; }
    private int _normalMaximumHealth;
    private int _healthUpgrade;
    private float _lastVoiceAt = float.NegativeInfinity;

    internal bool Symptomatic(float now) => now >= OnsetAt;

    internal bool ObserveVoice(float now, bool audible, float silenceSeconds = InfluenzaRules.SpeechSilenceSeconds)
    {
        if (!audible) return false;
        bool newSpeech = now - _lastVoiceAt >= silenceSeconds;
        _lastVoiceAt = now;
        return newSpeech && Symptomatic(now);
    }

    internal void CaptureHealth(int maximum, int healthUpgrade)
    {
        if (HealthCaptured) return;
        _normalMaximumHealth = maximum;
        _healthUpgrade = healthUpgrade;
        HealthCaptured = true;
    }

    internal int RestoredMaximum(int healthUpgrade) =>
        Math.Max(1, _normalMaximumHealth + 20 * (healthUpgrade - _healthUpgrade));
}
