# MODEL_CUNDUN_GHOST

Cliente: SIN MAPEO LOCAL
Llamadas directas: 0; hijas: 1
Create metadata: SI; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
                case MODEL_CUNDUN_GHOST:
                    RenderObject(o);
                    break;
                case MODEL_CURSEDTEMPLE_STATUE_PART1:
                case MODEL_CURSEDTEMPLE_STATUE_PART2:
                    RenderObject(o);
                    break;
                case MODEL_XMAS2008_SNOWMAN_HEAD:
                case MODEL_XMAS2008_SNOWMAN_BODY:
                    RenderObject(o);
                    break;
#ifdef PJH_ADD_PANDA_CHANGERING
                case MODEL_PANDA:
                    RenderObject(o);
                    break;
#endif //PJH_ADD_PANDA_CHANGERING
                case MODEL_TOTEMGOLEM_PART1:
                case MODEL_TOTEMGOLEM_PART2:
                case MODEL_TOTEMGOLEM_PART3:
                case MODEL_TOTEMGOLEM_PART4:
                case MODEL_TOTEMGOLEM_PART5:
                case MODEL_TOTEMGOLEM_PART6:
                    RenderObject(o);
                    break;
                case MODEL_SHADOW_PAWN_ANKLE_LEFT:		case MODEL_SHADOW_PAWN_ANKLE_RIGHT:
                case MODEL_SHADOW_PAWN_BELT:			case MODEL_SHADOW_PAWN_CHEST:
                case MODEL_SHADOW_PAWN_HELMET:
                case MODEL_SHADOW_PAWN_KNEE_LEFT:		case MODEL_SHADOW_PAWN_KNEE_RIGHT:
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_CUNDUN_GHOST(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        vec3_t Angle;
        vec3_t Position;
        if (o->Owner != NULL)
        {
            if (o->Owner->PKKey == 0)
            {
                o->AnimationFrame = 0;
            }
            else
            {
                o->Owner = NULL;
            }
        }
        else if (o->AnimationFrame > 6.0f)
        {
            o->PKKey += (1) * FPS_ANIMATION_FACTOR;
            o->Position[2] += ((o->PKKey * 0.8f)) * FPS_ANIMATION_FACTOR;

            VectorCopy(o->Position, Position);
            Position[0] += rand() % 120 - 60;
            Position[1] += rand() % 120 - 60;
            Position[2] += rand() % 60;
            Vector(1, 1, 1, Light);
            for (int i = 0; i < 3; ++i)
                CreateParticleFpsChecked(BITMAP_SMOKE, Position, o->Angle, Light, 20, 10.f);
        }
        else
        {
            o->Skill += (1) * FPS_ANIMATION_FACTOR;
            if (o->Angle[2] < 45.0f) o->Angle[2] += o->Skill * 0.3f;
            if (o->Angle[2] > 45.0f) o->Angle[2] -= o->Skill * 0.3f;

            if (o->Position[0] < Hero->Object.Position[0] + 300.0f) o->Position[0] += o->Skill * 0.2f;
            if (o->Position[0] > Hero->Object.Position[0] + 300.0f) o->Position[0] -= o->Skill * 0.2f;
            if (o->Position[1] < Hero->Object.Position[1] - 300.0f) o->Position[1] += o->Skill * 0.2f;
            if (o->Position[1] > Hero->Object.Position[1] - 300.0f) o->Position[1] -= o->Skill * 0.2f;

            Vector(1, 1, 1, Light);
            VectorCopy(o->Position, Position);
            Position[0] += rand() % 1200 - 600;
            Position[1] += rand() % 1200 - 600;
            vec3_t Angle;
            Vector(0.f, 0.f, rand() % 10 * 20.f, Angle);
            CreateEffectFpsChecked(MODEL_FIRE, Position, Angle, Light, 0, NULL, 0);

            o->Scale += (0.02f) * FPS_ANIMATION_FACTOR;
            //				o->Position[2] -= 0.5f;
            EarthQuake = (float)(rand() % 8 - 8) * 0.1f;
        }
        return true;
    }
```
