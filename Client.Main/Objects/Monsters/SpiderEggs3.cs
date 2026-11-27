using Client.Main.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Main.Objects.Monsters
{
    [NpcInfo(462, "Spider Eggs 3")]
    public class SpiderEggs3 : MonsterObject
    {
        public SpiderEggs3()
        {
            Scale = 0.3f;
        }

        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare($"Monster/Monster154.bmd");
            await base.Load();
        }
    }
}
