# MODEL_SWORD_FORCE

Cliente: MAPEADO - NO IMPLICA FIDELIDAD
Llamadas directas: 2; hijas: 0
Create metadata: SI; Move handler: SI
Modelo AccessModel: Skill/SwordForce.bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_SWORD_FORCE:
            {
                BMD* b = &Models[o->Type];
                b->CurrentAction = o->CurrentAction;
                b->PlayAnimation(&o->AnimationFrame, &o->PriorAnimationFrame, &o->PriorAction, o->Velocity, o->Position, o->Angle);
                MoveParticle(o, true);
            }
            break;
            default:
                MoveParticle(o, true);
                break;
            }
        }
    }
    o->LifeTime -= FPS_ANIMATION_FACTOR;
    if (o->LifeTime <= 0)
    {
        EffectDestructor(o);
    }
    else
    {
        switch (o->Type)
        {
            case BITMAP_LIGHT:
                if (o->SubType == 0 && rand_fps_check(2))
                {
                    MoveEffect(o, iIndex);
                }
                break;
            case MODEL_FIRE:
                if (o->SubType == 3 && rand_fps_check(2))
                {
                    MoveEffect(o, iIndex);
                }
                break;
        }
    }
}

void MoveEffects()
{
    if (SceneFlag == MAIN_SCENE)
    {
        g_pCatapultWindow->SetCameraPos();
    }

    for (int i = 0; i < MAX_EFFECTS; i++)
    {
        OBJECT* o = &Effects[i];
        if (o->Live)
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_SWORD_FORCE(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        vec3_t Angle;
        vec3_t Position;
        if (o->SubType == 0 || o->SubType == 2)
        {
            if (o->LifeTime > 12)
            {
                o->Scale += (0.9f) * FPS_ANIMATION_FACTOR;

                o->Direction[1] -= (2.f) * FPS_ANIMATION_FACTOR;
                if (o->SubType == 2)
                {
                    CreateEffectFpsChecked(MODEL_SWORD_FORCE, o->Position, o->Angle, o->Light, 3, o);
                }
                else
                    CreateEffectFpsChecked(MODEL_SWORD_FORCE, o->Position, o->Angle, o->Light, 1, o);
            }
            else
            {
                o->Scale -= (0.05f) * FPS_ANIMATION_FACTOR;
                o->BlendMeshLight = (float)o->LifeTime / 18.f;
                o->Alpha = o->BlendMeshLight;
                o->Light[0] = o->Alpha;
                o->Light[1] = o->Alpha;
                o->Light[2] = o->Alpha;

                o->Direction[1] -= (2.f) * FPS_ANIMATION_FACTOR;

                VectorCopy(o->Position, Position);
                Position[0] += rand() % 30 - 15.f;
                Position[1] += rand() % 30 - 15.f;
                Position[2] -= 100.f;
                for (int i = 0; i < 4; i++)
                {
                    Vector((float)(rand() % 60 + 60 + 90), 0.f, o->Angle[2], Angle);
                    CreateJointFpsChecked(BITMAP_JOINT_SPARK, Position, Position, Angle);
                    if (o->SubType == 2)
                    {
                        CreateParticleFpsChecked(BITMAP_FIRE, Position, Angle, o->Light, 18, 1.5f);
                    }
                    else
                        CreateParticleFpsChecked(BITMAP_FIRE, Position, Angle, o->Light, 2, 1.5f);
                }
            }
            Vector(1.f, 0.8f, 0.6f, Light);
            AddTerrainLight(o->Position[0], o->Position[1], Light, 1, PrimaryTerrainLight);
        }
        else if (o->SubType == 1 || o->SubType == 3)
        {
            o->BlendMeshLight = (float)o->LifeTime / 10.f;
            o->Alpha = o->BlendMeshLight;
            o->Light[0] = o->Alpha;
            o->Light[1] = o->Alpha;
            o->Light[2] = o->Alpha;
        }
        return true;
    }
```
