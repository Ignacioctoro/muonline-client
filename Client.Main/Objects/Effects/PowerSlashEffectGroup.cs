#nullable enable

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Creates the five Power Slash projectiles used by
    /// the original MU client.
    /// </summary>
    public sealed class PowerSlashEffectGroup : WorldObject
    {
        private static readonly float[] SpreadAngles =
        {
            -40f,
            -20f,
             0f,
             20f,
             40f
        };

        public PowerSlashEffectGroup(WalkerObject caster)
        {
            foreach (float angle in SpreadAngles)
            {
                Children.Add(
                    new PowerSlashEffect(
                        caster,
                        angle));
            }
        }
    }
}