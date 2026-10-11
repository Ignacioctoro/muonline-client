#nullable enable
namespace Client.Main.Objects.Effects.Skills
{
    /// <summary>
    /// A skill can emit straight into ClassicFX's fixed pool without
    /// allocating or loading a duplicate legacy WorldObject.
    /// False permits the existing factory fallback.
    /// </summary>
    public interface IClassicFxSkillVisualEffect : ISkillVisualEffect
    {
        bool TryCreateClassicFx(SkillEffectContext context);
    }
}
