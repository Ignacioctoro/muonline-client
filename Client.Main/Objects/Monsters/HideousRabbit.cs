using Client.Main.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Main.Objects.Monsters
{
    [NpcInfo(420, "Hideous Rabbit")]
    public class HideousRabbit : MonsterObject
    {
        public HideousRabbit()
        {
            Scale = 0.95f;
        }

        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare($"Monster/Monster131.bmd");
            await base.Load();
        }
    }
}
