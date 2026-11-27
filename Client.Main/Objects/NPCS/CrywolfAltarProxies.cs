using System.Threading.Tasks;
using Client.Main.Content;

namespace Client.Main.Objects.NPCS
{
    // These passive server objects overlay map objects. Original ZzzCharacter
    // keeps their character meshes hidden; the map supplies the visible geometry.
    public abstract class CrywolfAltarProxy : NPCObject
    {
        protected virtual string ModelPath => "Object35/Object57.bmd";
        protected CrywolfAltarProxy() { HiddenMesh = -2; }

        protected override void HandleClick()
        {
            var service = MuGame.Network?.GetCharacterService();
            if (service != null) _ = service.SendTalkToNpcRequestAsync(NetworkId);
        }
        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare(ModelPath);
            await base.Load();
        }
    }
    [NpcInfo(204, "Wolf Statue")]
    public sealed class CrywolfStatueProxy : CrywolfAltarProxy
    {
        protected override string ModelPath => "Object35/Object82.bmd";
    }
    [NpcInfo(205, "Wolf Altar 1")]
    public sealed class CrywolfAltarOne : CrywolfAltarProxy { }
    [NpcInfo(206, "Wolf Altar 2")]
    public sealed class CrywolfAltarTwo : CrywolfAltarProxy { }
    [NpcInfo(207, "Wolf Altar 3")]
    public sealed class CrywolfAltarThree : CrywolfAltarProxy { }
    [NpcInfo(208, "Wolf Altar 4")]
    public sealed class CrywolfAltarFour : CrywolfAltarProxy { }
    [NpcInfo(209, "Wolf Altar 5")]
    public sealed class CrywolfAltarFive : CrywolfAltarProxy { }
}
