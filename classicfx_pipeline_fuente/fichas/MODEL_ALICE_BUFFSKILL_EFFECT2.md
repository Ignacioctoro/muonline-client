# MODEL_ALICE_BUFFSKILL_EFFECT2

Cliente: SIN MAPEO LOCAL
Llamadas directas: 4; hijas: 0
Create metadata: SI; Move handler: NO DETECTADO
Modelo AccessModel: Effect/elshildring2.bmd

## Fragmento de CreateEffect

```cpp
                case MODEL_ALICE_BUFFSKILL_EFFECT2:
                {
                    if (o->SubType == 0 || o->SubType == 1 || o->SubType == 2)
                    {
                        RenderObject(o);
                    }
                }
                break;
                case MODEL_RAKLION_BOSS_CRACKEFFECT:
                {
                    RenderObject(o);
                }
                break;
                case MODEL_RAKLION_BOSS_MAGIC:
                {
                    RenderObject(o);
                }
                break;
                case MODEL_LAVAGIANT_FOOTPRINT_R:
                case MODEL_LAVAGIANT_FOOTPRINT_V:
                {
                    EnableAlphaBlend();
                    if (o->Type == MODEL_LAVAGIANT_FOOTPRINT_R)
                    {
                        RenderTerrainAlphaBitmap(BITMAP_LAVAGIANT_FOOTPRINT_R, o->Position[0], o->Position[1], o->Scale, o->Scale, o->Light);
                    }
                    else
                    {
                        RenderTerrainAlphaBitmap(BITMAP_LAVAGIANT_FOOTPRINT_V, o->Position[0], o->Position[1], o->Scale, o->Scale, o->Light);
                    }
                    DisableAlphaBlend();
                }
```
