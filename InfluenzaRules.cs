using System;

namespace REPOJP.StageRoles;

internal static class InfluenzaRules
{
    internal const float IncubationSeconds = 30f;
    internal const int MaximumHealth = 75;
    internal const float SpeechSilenceSeconds = 0.75f;

    // Uniform 30–90 seconds: irregular, with an exact expected interval of 60.
    internal static float SneezeInterval(double sample) =>
        30f + 60f * (float)Math.Clamp(sample, 0d, 1d);

    internal static bool Infects(double sample, bool sneeze) =>
        sample >= 0d && sample < (sneeze ? 0.6d : 0.3d);

    // Distance is three-dimensional; the requested left/right fan is horizontal.
    internal static bool InRange(double distanceSquared, double horizontalDot, bool sneeze) =>
        distanceSquared <= (sneeze ? 25d : 9d) &&
        horizontalDot + 0.000001d >= Math.Cos((sneeze ? 20d : 30d) * Math.PI / 180d);
}

internal sealed class InfluenzaState
{
    internal InfluenzaState(float infectedAt) => OnsetAt = infectedAt + InfluenzaRules.IncubationSeconds;
    internal float OnsetAt { get; }
    internal float NextSneezeAt { get; set; } = float.PositiveInfinity;
    internal bool SymptomsStarted { get; set; }
    internal bool HealthCaptured { get; private set; }
    private int _normalMaximumHealth;
    private int _healthUpgrade;
    private float _lastVoiceAt = float.NegativeInfinity;

    internal bool Symptomatic(float now) => now >= OnsetAt;

    internal bool ObserveVoice(float now, bool audible)
    {
        if (!audible) return false;
        bool newSpeech = now - _lastVoiceAt >= InfluenzaRules.SpeechSilenceSeconds;
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
