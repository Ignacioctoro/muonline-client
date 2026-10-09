# MODEL_INFINITY_ARROW3

Cliente: SIN MAPEO LOCAL
Llamadas directas: 0; hijas: 1
Create metadata: SI; Move handler: NO DETECTADO
Modelo AccessModel: Skill/arrowsre[index=4].bmd

## Fragmento de CreateEffect

```cpp
                case MODEL_INFINITY_ARROW3:
                case MODEL_INFINITY_ARROW4:
                    RenderObject(o);
                    break;

                case MODEL_ARROW_BEST_CROSSBOW:
                    RenderObject(o);
                    break;

                case MODEL_ALICE_BUFFSKILL_EFFECT:
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
                        RenderTerrainAlphaBitmap(BITMAP_LAVAGIANT_FOOTPRINT_R, o->Position[0], o->Position[1], o->S
```
