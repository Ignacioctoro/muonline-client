using Client.Main.Content;
using System.Threading.Tasks;

namespace Client.Main.Objects.NPCS
{
    [NpcInfo(452, "Seed Master")]
    public class SeedMaster : NPCObject
    {
        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare("NPC/goblinmaster.bmd");
            Scale = 1.1f;
            await base.Load();
        }

        protected override void HandleClick()
        {
            var service = MuGame.Network?.GetCharacterService();
            if (service != null) _ = service.SendTalkToNpcRequestAsync(NetworkId);
        }
    }
}
