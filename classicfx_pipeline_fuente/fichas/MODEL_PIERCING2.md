# MODEL_PIERCING2

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: SI; Move handler: SI
Modelo AccessModel: Skill/m_Piercing.bmd

## Fragmento de CreateEffect

```cpp
                case MODEL_PIERCING2:
                    if (o->SubType == 1 || o->SubType == 2)
                        break;
                    RenderObject(o);
                    break;
                case MODEL_PIERCING:
                    if (o->SubType == 3)
                        break;
                    RenderObject(o);
                    break;
                case MODEL_TOWER_GATE_PLANE:
                    RenderObject(o);

                    o->Position[2] = o->StartPosition[2] + 400.f - (o->Position[2] - o->StartPosition[2]);
                    RenderObject(o);
                    break;

                case BATTLE_CASTLE_WALL1:
                case BATTLE_CASTLE_WALL2:
                case BATTLE_CASTLE_WALL3:
                case BATTLE_CASTLE_WALL4:
                    RenderObject(o);
                    break;
                case MODEL_FENRIR_THUNDER:
                    RenderObject(o);
                    break;
                case MODEL_FALL_STONE_EFFECT:
                    RenderObject(o);
                    break;
                case MODEL_FENRIR_FOOT_THUNDER:
                {
                    EnableAlphaBlend();
                    RenderTerrainAlphaBitmap(BITMAP_FENRIR_FO
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_PIERCING2(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Position;
        float Matrix[3][4];
        AngleMatrix(o->Angle, Matrix);
        VectorRotate(o->Direction, Matrix, Position);
        //		VectorAdd(o->Position,Position,o->Position);

        o->Direction[1] += (12.f) * FPS_ANIMATION_FACTOR;
        if (o->Direction[1] >= 0)
        {
            o->Direction[1] = 0;
        }

        if (o->SubType == 1) {
            if (o->LifeTime > 1) {
                o->BlendMeshLight *= pow(1.0f / (1.6f), FPS_ANIMATION_FACTOR);
                CreateEffectFpsChecked(MODEL_WAVES, o->Position, o->Angle, o->Light, 2, NULL, o->LifeTime);
            }
        }
        else
            if (o->SubType == 2)
            {
                //			o->Direction[1] -= 10.f;
                if (o->LifeTime > 1)
                {
                    o->BlendMeshLight *= pow(1.0f / (1.6f), FPS_ANIMATION_FACTOR);
                    CreateEffectFpsChecked(MODEL_WAVES, o->Position, o->Angle, o->Light, 2, NULL, o->LifeTime);
                }
            }
            else {
                if (o->LifeTime > 5) {
                    o->BlendMeshLight *= pow(1.0f / (1.6f), FPS_ANIMATION_FACTOR);
                    CreateEffectFpsChecked(MODEL_WAVES, o->Position, o->Angle, o->Light, 2, NULL, o->LifeTime);
                }
            }
        return true;
    }
```
