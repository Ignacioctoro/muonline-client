using Client.Main;
using Client.Main.Controls.UI.Game;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Core.Utilities;
using Microsoft.Xna.Framework;
using System.Reflection;
Constants.DataPath=args[0];
void Check(bool ok,string message) {if(!ok) throw new Exception(message);}
var renderer=typeof(BmdPreviewRenderer);
var propsType=renderer.GetNestedType("ItemRenderProperties",BindingFlags.NonPublic)!;
var props=Activator.CreateInstance(propsType)!;
string Key(ItemDefinition def)=>(string)renderer.GetMethod("BuildCacheKey",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,new object[]{def,32,32,0f,props})!;
foreach(short id in new short[]{30,31,136,137,138,139,140,141,142,143})
{
    var def=ItemDatabase.GetItemDefinition(12,id);
    Check(def!=null && def.Group==12 && def.Id==id,"Packed identity");
    Check(def!.Width==1 && def.Height==1,"Server footprint");
    Check(File.Exists(Path.Combine(Constants.DataPath,def.TexturePath)),"Jewel model exists");
    Check(def.PackedJewelPreviewSource!=null,"Fallback bundle model source");
    Check(Key(def)!=Key(def.PackedJewelPreviewSource),"Single and bundle previews cannot share a cache entry");
    for(byte level=0;level<3;level++)
    {
        var bytes=new byte[]{0xC0,(byte)id,level,1,0};
        var parsed=ItemDatabase.GetItemDefinition(bytes);
        Check(ReferenceEquals(parsed,def),"Extended pack bytes must not fall through to a weapon");
        Check(ItemDatabase.ParseItemDetails(bytes).Level==level,"Pack of 10/20/30 keeps level");
    }
}
var window=ChaosMixControl.Instance;
var flags=BindingFlags.Instance|BindingFlags.NonPublic;
void Layout(MixInventoryType mode) {window.GetType().GetField("_mixInventoryType",flags)!.SetValue(window,mode);window.GetType().GetMethod("BuildLayoutMetrics",flags)!.Invoke(window,null);}
Rectangle Rect(string name)=>(Rectangle)window.GetType().GetField(name,flags)!.GetValue(window)!;
Layout(MixInventoryType.GoblinNormal);
Check(Rect("_gridRect").Size==new Point(256,128),"Chaos grid is 8x4 at inventory scale");
Check(Rect("_gridRect").Top>170 && Rect("_infoRect").Top>Rect("_gridRect").Bottom,"Reference ordering");
Check(Rect("_infoRect").Bottom<Rect("_mixButtonRect").Top,"Requirements do not overlap combine button");
Check(Rect("_mixButtonRect").Bottom<690,"Button fits panel");
Layout(MixInventoryType.Trainer);
Check(Rect("_gridRect").Size==new Point(232,116),"Pet Trainer layout preserved");
Console.WriteLine("PASS: ten packed-jewel definitions, thirty extended identities, model files, preview cache isolation, Chaos layout and Trainer layout.");
