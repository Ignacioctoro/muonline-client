using Client.Main.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Main.Objects.Monsters
{
    [NpcInfo(412, "Dark Elf (Trainee Soldier)")]
    public class DarkElf2 : MonsterObject
    {
        public DarkElf2()
        {
            Scale = 1.7f;
        }

        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare($"Monster/Monster127.bmd");
            await base.Load();
        }
    }
}
