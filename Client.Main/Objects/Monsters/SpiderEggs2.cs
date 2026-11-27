using Client.Main.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Main.Objects.Monsters
{
    [NpcInfo(461, "Spider Eggs 2")]
    public class SpiderEggs2 : MonsterObject
    {
        public SpiderEggs2()
        {
            Scale = 0.3f;
        }

        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare($"Monster/Monster153.bmd");
            await base.Load();
        }
    }
}
