using Client.Main.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Main.Objects.Monsters
{
    [NpcInfo(148, "Rogue Centurion")]
    public class LordCenturion : MonsterObject
    {
        public LordCenturion()
        {
            Scale = 1.0f;
        }

        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare($"Monster/Monster68.bmd");
            await base.Load();
        }
    }
}
