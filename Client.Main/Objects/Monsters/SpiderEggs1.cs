using Client.Main.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Main.Objects.Monsters
{
    [NpcInfo(460, "Spider Eggs 1")]
    public class SpiderEggs1 : MonsterObject
    {
        public SpiderEggs1()
        {
            Scale = 0.3f;
        }

        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare($"Monster/Monster152.bmd");
            await base.Load();
        }
    }
}
