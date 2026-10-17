using Client.Main;
using Client.Main.Controls.UI.Game;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Core.Utilities;
using Microsoft.Xna.Framework;
using System.Reflection;

if (args.Length > 0) Constants.DataPath=args[0];
void Check(bool value,string message) { if(!value) throw new Exception(message); }
InventoryItem Item(int group,int id,int level=0) => new(new ItemDefinition(id,"Test",1,1){Group=group},Point.Zero,new byte[]{(byte)id,(byte)(level<<3),1},1);
var items=new List<InventoryItem>();
var empty=MixRecipeDatabase.EvaluateTrainer(items);
Check(empty.CurrentRecipe==null,"Empty recipe must not be ready");
items.Add(Item(13,31,1));
var partial=MixRecipeDatabase.EvaluateTrainer(items);
Check(partial.MostSimilarRecipe.MixId==14,"Raven spirit selects raven requirements before completion");
var recipe=partial.MostSimilarRecipe;
Check(MixRecipeDatabase.GetRequirementCounts(recipe,items).SequenceEqual(new[]{1,0,0,0,0}),"Spirit only");
items.Add(Item(14,13));
Check(MixRecipeDatabase.GetRequirementCounts(recipe,items)[1]==1,"Partial bless count");
items.Add(Item(14,13));
items.Add(Item(14,14)); items.Add(Item(14,14)); items.Add(Item(12,15)); items.Add(Item(14,22));
Check(MixRecipeDatabase.EvaluateTrainer(items).CurrentRecipe?.MixId==14,"Complete raven recipe");
Check(MixRecipeDatabase.GetRequirementCounts(recipe,items).SequenceEqual(new[]{1,2,2,1,1}),"Complete counts");
var control=ChaosMixControl.Instance;
var flags=BindingFlags.Instance|BindingFlags.NonPublic;
control.GetType().GetField("_mixInventoryType",flags)!.SetValue(control,MixInventoryType.Trainer);
var uiItems=(List<InventoryItem>)control.GetType().GetField("_items",flags)!.GetValue(control)!;
var colors=(Dictionary<int,Color>)control.GetType().GetField("_requirementColors",flags)!.GetValue(control)!;
void Refresh() {uiItems.Clear();uiItems.AddRange(items);control.GetType().GetMethod("UpdateRecipeInfo",flags)!.Invoke(control,null);}
Refresh();
Check(colors.Count==5 && colors.Values.All(c=>c==Color.LimeGreen),"All complete requirement lines turn green");
items.RemoveAt(1);Refresh();
Check(colors.Values.Count(c=>c==Color.LimeGreen)==4,"Removing a bless clears its green state");
items.Add(Item(14,13));items.Add(Item(14,13));Refresh();
Check(colors.Values.Count(c=>c==Color.LimeGreen)==4,"Too many jewels must not show all green");
items.Clear();items.Add(Item(13,31));Refresh();
Check(MixRecipeDatabase.EvaluateTrainer(items).MostSimilarRecipe.MixId==13,"Horse spirit selects horse recipe");
items.Clear();Refresh();
Check(colors.Values.All(c=>c!=Color.LimeGreen),"Empty box resets all requirement colors");
Console.WriteLine("PASS: spirit selection, partial/complete/excess counts, green requirement lines, removal and empty reset.");

// OpenMU ItemCraftingRequiredItem level bounds default to 0, not 255.
// Exercise the actual extended packet representation used by this client.
InventoryItem Extended(int group,int id,int level=0) => new(new ItemDefinition(id,"Test",1,1){Group=group},Point.Zero,new byte[]{(byte)(group<<4),(byte)id,(byte)level,1,0},1);
for(int level=0;level<=15;level++)
foreach(int jewelCount in new[]{2,5})
{
    items.Clear();items.Add(Extended(13,31,level));
    for(int n=0;n<jewelCount;n++){items.Add(Extended(14,13));items.Add(Extended(14,14));}
    items.Add(Extended(12,15));items.Add(Extended(14,22));
    int? expected=level==0 && jewelCount==5 ? 13 : level==1 && jewelCount==2 ? 14 : null;
    Check(MixRecipeDatabase.EvaluateTrainer(items).CurrentRecipe?.MixId==expected,"Exact Spirit level and recipe match OpenMU");
    Refresh();
    if(expected==null) Check(colors.Values.Count(c=>c==Color.LimeGreen)<5,"Rejected ingredients cannot show all requirements green");
}
items.Clear();items.Add(Extended(13,31));
for(int n=0;n<5;n++){items.Add(Extended(14,13,n==0 ? 1 : 0));items.Add(Extended(14,14));}
items.Add(Extended(12,15));items.Add(Extended(14,22));
Check(MixRecipeDatabase.EvaluateTrainer(items).CurrentRecipe==null,"Edited +1 jewel cannot satisfy a +0 requirement");
Console.WriteLine("PASS: 32 Spirit level/recipe combinations and edited jewel level rejection.");

foreach(var (level,name) in new[]{(0,"Spirit of Dark Horse"),(1,"Spirit of Dark Raven")})
{
    var spirit=Extended(13,31,level);
    var tooltip=Client.Main.Core.Items.Tooltips.ClassicItemTooltipBuilder.Build(spirit);
    var fallback=Client.Main.Controls.UI.Game.Common.ItemUiHelper.BuildTooltipLines(spirit);
    Check(tooltip[0].text==name && fallback[0].text==name,"Spirit tooltip names match server level meaning");
    Check(ItemDatabase.GetItemName(spirit.RawData)==name,"Item details use the same Spirit name");
    Check(!tooltip[0].text.Contains("+1"),"Raven level is a variant, not an upgrade suffix");
}
var invalidTooltip=Client.Main.Core.Items.Tooltips.ClassicItemTooltipBuilder.Build(Extended(13,31,15));
Check(invalidTooltip[0].text.Contains("unsupported"),"Invalid edited levels are not labeled as valid pets");
Console.WriteLine("PASS: Spirit names in classic/fallback tooltips and item details, plus invalid level feedback.");
