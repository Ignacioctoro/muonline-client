# MODEL_ARROW

Cliente: SIN MAPEO LOCAL
Llamadas directas: 5; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/Arrow[index=1].bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_ARROW:
            case MODEL_ARROW_STEEL:
            case MODEL_ARROW_THUNDER:
            case MODEL_ARROW_LASER:
            case MODEL_ARROW_V:
            case MODEL_ARROW_SAW:
            case MODEL_ARROW_NATURE:
            case MODEL_ARROW_WING:
            case MODEL_ARROW_BOMB:
            case MODEL_LACEARROW:
            case MODEL_DARK_SCREAM:
            case MODEL_DARK_SCREAM_FIRE:
            case MODEL_ARROW_SPARK:
            case MODEL_ARROW_RING:
            case MODEL_ARROW_TANKER:
            case MODEL_ARROW_DARKSTINGER:
            case MODEL_ARROW_GAMBLE:
                break;
            case MODEL_FIRE:
                if (o->SubType == 1)
                {
                    for (int j = 0; j < 2; j++)
                        CreateEffectFpsChecked(MODEL_STONE1 + rand() % 2, o->Position, o->Angle, o->Light);
                }
                break;

            case BITMAP_ENERGY:
                CreateParticleFpsChecked(BITMAP_SPARK + 1, o->Position, o->Angle, Light, 1, 6.f);
                break;

            case MODEL_LIGHTNING_ORB:
            {
                CreateEffectFpsChecked(MODEL_LIGHTNING_ORB, o->Position, o->Angle, o->Light, 1);
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_ARROW(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        float Height;
        Vector(Luminosity * 0.8f, Luminosity * 0.5f, Luminosity * 0.2f, Light);
        AddTerrainLight(o->Position[0], o->Position[1], Light, 2, PrimaryTerrainLight);

        if (o->SubType == 3 || o->SubType == 4)
        {
            if (o->SubType == 3)
            {
                o->Angle[0] += (o->Gravity) * FPS_ANIMATION_FACTOR;
            }
            if (o->Angle[0] > 50)
            {
                o->Angle[0] = 50.f;
            }

            int PositionX = (int)(o->Position[0] / TERRAIN_SCALE);
            int PositionY = (int)(o->Position[1] / TERRAIN_SCALE);
            int WallIndex = TERRAIN_INDEX_REPEAT(PositionX, PositionY);
            int Wall = TerrainWall[WallIndex] & TW_NOGROUND;

            if (Wall != TW_NOGROUND)
            {
                float Height = RequestTerrainHeight(o->Position[0], o->Position[1]);
                if (o->Position[2] < Height)
                {
                    o->Position[2] = Height + 10;
                    Vector(0.f, 0.f, 0.f, o->Direction);
                }
            }
            CreateParticleFpsChecked(BITMAP_FIRE, o->Position, o->Angle, Light, 5);
        }
        else if (o->SubType == 5)
        {
            Vector(Luminosity * 0.1f, Luminosity * 0.6f, Luminosity * 0.3f, Light);
            CreateParticleFpsChecked(BITMAP_FIRE, o->Position, o->Angle, Light);
            CheckClientArrow(o);
        }
        else
        {
            CreateParticleFpsChecked(BITMAP_FIRE, o->Position, o->Angle, Light);
            CheckClientArrow(o);
        }
        return true;
    }
```
