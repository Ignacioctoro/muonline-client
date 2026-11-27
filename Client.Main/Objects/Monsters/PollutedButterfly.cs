using Client.Main.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Main.Objects.Monsters
{
    [NpcInfo(419, "Polluted Butterfly")]
    public class PollutedButterfly : MonsterObject
    {
        public PollutedButterfly()
        {
            Scale = 0.76f;
        }

        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare($"Monster/Monster130.bmd");
            await base.Load();
        }
    }
}
