using Client.Main.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Main.Objects.Monsters
{
    [NpcInfo(425, "Captain Grizzly")]
    public class CaptainGrizzly : MonsterObject
    {
        public CaptainGrizzly()
        {
            Scale = 1.235f;
        }

        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare($"Monster/Monster136.bmd");
            await base.Load();
        }
    }
}
