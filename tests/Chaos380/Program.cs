using Client.Main;
using Client.Main.Controls.UI;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Core.Utilities;
using Microsoft.Xna.Framework;
Constants.DataPath=args[0];
void Check(bool ok,string msg){if(!ok)throw new Exception(msg);}
InventoryItem Equipment(int level,int option=1,bool guardian=false,int requiredLevel=380) => new(new ItemDefinition(0,"380 equipment",1,1){Group=0,RequiredLevel=requiredLevel,BaseDurability=30},Point.Zero,new byte[]{0,0,(byte)level,30,(byte)(guardian?0x41:1),(byte)option},30);
InventoryItem Jewel(int id,int level=0) => new(new ItemDefinition(id,"Jewel",1,1){Group=14},Point.Zero,new byte[]{0xE0,(byte)id,(byte)level,1,0},1);
List<InventoryItem> Box(InventoryItem equipment) => new(){equipment,Jewel(31),Jewel(42)};
for(int level=0;level<=15;level++)
{
 var result=MixRecipeDatabase.EvaluateGuardianOption(Box(Equipment(level)));
 Check((result.CurrentRecipe?.MixId==36)==(level>=4),"380 minimum level");
 if(level>=4) Check(result.RequiredZen==10_000_000 && result.SuccessRate==(level<=6?50:level<=9?60:70),"OpenMU 380 rate and cost");
}
Check(MixRecipeDatabase.EvaluateGuardianOption(Box(Equipment(4,0))).CurrentRecipe==null,"Additional option required");
Check(MixRecipeDatabase.EvaluateGuardianOption(Box(Equipment(4,1,true))).CurrentRecipe==null,"Already has Guardian option");
Check(MixRecipeDatabase.EvaluateGuardianOption(Box(Equipment(4,1,false,100))).CurrentRecipe==null,"Unsupported equipment");
var missing=Box(Equipment(4));missing.RemoveAt(2);
Check(MixRecipeDatabase.EvaluateGuardianOption(missing).CurrentRecipe==null,"Missing Harmony");
var extra=Box(Equipment(4));extra.Add(Jewel(31));
Check(MixRecipeDatabase.EvaluateGuardianOption(extra).CurrentRecipe==null,"Excess Guardian jewel");
var upgraded=Box(Equipment(4));upgraded[1]=Jewel(31,1);
Check(MixRecipeDatabase.EvaluateGuardianOption(upgraded).CurrentRecipe==null,"Jewels must be +0");
Check(!MixRecipeDatabase.GetChaosMachineRecipes().Any(r=>r.MixId==36),"Regular menu excludes 380 recipe");
Check(RequestDialog.WrapMessage("Mix?\nChance: 90%\n\nFailure",100,s=>s.Length)=="Mix?\nChance: 90%\n\nFailure","Preserve explicit paragraphs");
Check(RequestDialog.WrapMessage("one two three",7,s=>s.Length)=="one two\nthree","Wrap words without losing text");
Console.WriteLine("PASS: 380 levels/rates/cost, ingredients, existing option, regular isolation and dialog line breaks.");
