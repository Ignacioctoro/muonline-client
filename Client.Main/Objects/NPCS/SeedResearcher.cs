using Client.Main.Content;
using System.Threading.Tasks;

namespace Client.Main.Objects.NPCS
{
    [NpcInfo(453, "Seed Researcher")]
    public class SeedResearcher : NPCObject
    {
        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare("NPC/seedgoblin.bmd");
            Scale = 0.9f;
            await base.Load();
        }

        protected override void HandleClick()
        {
            var service = MuGame.Network?.GetCharacterService();
            if (service != null) _ = service.SendTalkToNpcRequestAsync(NetworkId);
        }
    }
}
