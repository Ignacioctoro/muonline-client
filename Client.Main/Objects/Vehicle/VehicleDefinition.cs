using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Vehicle;

public class VehicleDefinition
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string TexturePath { get; set; }


    /// <summary>
    /// Base animation speed for this vehicle.
    /// Multiplies the action PlaySpeed.
    /// </summary>
    public float AnimationSpeed { get; set; } =
        25f;


    /// <summary>
    /// Action indices used by this vehicle model.
    /// </summary>
    public int IdleActionIndex { get; set; } =
        0;

    public int RunActionIndex { get; set; } =
        2;

    public int SkillActionIndex { get; set; } =
        4;


    /// <summary>
    /// Vertical offset applied to the rider.
    /// </summary>
    public float RiderHeightOffset { get; set; } =
        0f;


    /// <summary>
    /// Global animation speed multiplier.
    /// </summary>
    public float AnimationSpeedMultiplier { get; set; } =
        1.0f;


    public float IdleAnimationSpeedMultiplier { get; set; } =
        1.0f;

    public float RunAnimationSpeedMultiplier { get; set; } =
        1.0f;

    public float SkillAnimationSpeedMultiplier { get; set; } =
        1.0f;


    /// <summary>
    /// Absolute PlaySpeed overrides per animation/action.
    /// </summary>
    public Dictionary<int, float> ActionPlaySpeedOverrides { get; set; }


    // ================================================================
    // CLASSIC MU VEHICLE MATERIALS
    // ================================================================


    /// <summary>
    /// Meshes which reproduce the original MU StreamMesh behaviour.
    ///
    /// These meshes bypass normal terrain/body lighting and render with
    /// BodyLight = (1,1,1).
    ///
    /// Original Fenrir:
    ///
    ///     b->StreamMesh = 0;
    ///
    /// Therefore mesh 0 is fullbright while mesh 1 keeps normal lighting.
    /// </summary>
    public int[] FullBrightMeshes { get; set; } =
        Array.Empty<int>();


    /// <summary>
    /// Meshes which receive the classic RENDER_CHROME overlay.
    ///
    /// Original Fenrir:
    ///
    /// Gold:
    ///     mesh 0
    ///
    /// Black / Blue / Red:
    ///     mesh 1
    /// </summary>
    public int[] ClassicChromeMeshes { get; set; } =
        Array.Empty<int>();
    /// <summary>
    /// Classic Chrome meshes which are rendered only while the vehicle
    /// is executing its configured SkillActionIndex.
    ///
    /// Season 6 Fenrir:
    ///
    /// Black / Blue / Red:
    ///     mesh 1 only during FENRIR_ATTACK_SKILL.
    ///
    /// Gold does not use this because its Chrome is permanent.
    /// </summary>
    public int[] ClassicSkillChromeMeshes { get; set; } =
        Array.Empty<int>();


    /// <summary>
    /// BodyLight/color used by the Chrome pass.
    /// </summary>
    public Vector3 ClassicChromeColor { get; set; } =
        Vector3.One;


    /// <summary>
    /// Strength of the Chrome overlay.
    /// 1.0 reproduces the normal classic additive pass.
    /// </summary>
    public float ClassicChromeIntensity { get; set; } =
        1.0f;


    /// <summary>
    /// Color used by the Fenrir thunder/electrical effects.
    /// </summary>
    public Vector3 FenrirThunderColor { get; set; } =
        Vector3.One;
}