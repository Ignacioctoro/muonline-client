# MODEL_SUMMONER_CASTING_EFFECT22

Cliente: MAPEADO - NO IMPLICA FIDELIDAD
Llamadas directas: 2; hijas: 0
Create metadata: SI; Move handler: NO DETECTADO
Modelo AccessModel: Effect/Suhwanzin22.bmd

## Fragmento de CreateEffect

```cpp
                case MODEL_SUMMONER_CASTING_EFFECT22:
                case MODEL_SUMMONER_CASTING_EFFECT222:
                case MODEL_SUMMONER_CASTING_EFFECT4:
                    RenderObject(o);
                    break;
                case MODEL_SUMMONER_SUMMON_SAHAMUTT:
                    RenderObject(o);
                    break;
                case MODEL_SUMMONER_SUMMON_NEIL:
                    RenderObject(o);
                    break;
                case MODEL_SUMMONER_SUMMON_NEIL_NIFE1:
                case MODEL_SUMMONER_SUMMON_NEIL_NIFE2:
                case MODEL_SUMMONER_SUMMON_NEIL_NIFE3:
                    RenderObject(o);
                    break;
                case MODEL_SUMMONER_SUMMON_NEIL_GROUND1:
                case MODEL_SUMMONER_SUMMON_NEIL_GROUND2:
                case MODEL_SUMMONER_SUMMON_NEIL_GROUND3:
                    RenderObject(o);
                    break;
                case MODEL_SUMMONER_SUMMON_LAGUL:
                    if (o->SubType == 1)
                    {
                        BMD* pModel = &Models[o->Type];
                        vec3_t vPos, vLight;

                        const int nBoneCount = 6;
                        int nBone[nBoneCount] = { 54, 55, 56, 57, 58
```

## Funcion MoveHandlers

```cpp
        case MODEL_SUMMONER_CASTING_EFFECT22:
            o->Angle[2] -= (3.0f) * FPS_ANIMATION_FACTOR;
            break;
        case MODEL_SUMMONER_CASTING_EFFECT222:
            o->Angle[2] += (3.0f) * FPS_ANIMATION_FACTOR;
            break;
        case MODEL_SUMMONER_CASTING_EFFECT4:
            o->Scale += (0.6f) * FPS_ANIMATION_FACTOR;
            break;
        }
    }
        return true;
    }

    // MODEL_SUMMONER_SUMMON_SAHAMUTT
    bool Move_MODEL_SUMMONER_SUMMON_SAHAMUTT(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Position;
        float Matrix[3][4];
    {
        vec3_t vTempPosition;
        VectorCopy(o->Position, vTempPosition);

        if (o->LifeTime < 20)
        {
            SetAction(o, 0);
            o->Alpha -= (0.05f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->CurrentAction == 0 && o->Alpha < 0.3f)
        {
            o->Alpha += (0.05f) * FPS_ANIMATION_FACTOR;
            {
                o->Angle[2] = CreateAngle2D(o->Position, o->HeadTargetAngle);
                float dx = o->HeadTargetAngle[0] - o->Position[0];
                float dy = o->HeadTargetAngle[1] - o->Position[1];
                o->Distance = sqrtf(dx * dx + dy * dy);
            }
        }
        else
        {
            SetAction(o, 1);
            if (o->AnimationFrame >= 12.0f)
            {
                o->AnimationFrame = 12.0f;
            }

            if (o->AnimationFrame >= 11.0f)
            {
                if (o->Alpha > 0)
                    o->Alpha -= (0.3f) * FPS_ANIMATION_FACTOR;
                else
                    o->Alpha = 0;
            }
            else if (o->AnimationFrame < 3.0f && o->Alpha < 0.7f)
            {
                o->Alpha += (0.05f) * FPS_ANIMATION_FACTOR;
            }

            if (o->AnimationFrame > 4.0f && o->AnimationFrame < 12.0f)
            {
                AngleMatrix(o->Angle, Matrix);
                vec3_t vMoveDir, Position;
                if (o->AnimationFrame < 10.0f)
                {
                    Vector(0, o->Distance / -13.0f, 0, vMoveDir);
                }
                else
                {
                    Vector(0, o->Distance / -45.0f, 0, vMoveDir);
                }
                VectorRotate(vMoveDir, Matrix, Position);
                VectorAddScaled(o->Position, Position, o->Position, FPS_ANIMATION_FACTOR);
            }
            if (o->AnimationFrame <= 4.0f)
            {
                o->Angle[2] = CreateAngle2D(o->Position, o->HeadTargetAngle);
                float dx = o->HeadTargetAngle[0] - o->Position[0];
                float dy = o->HeadTargetAngle[1] - o->Position[1];
                o->Distance = sqrtf(dx * dx + dy * dy);
            }
        }
        o->Position[2] = RequestTerrainHeight(o->Position[0], o->Position[1]);

        VectorSubtract(vTempPosition, o->Position, vTempPosition);

        if (o->AnimationFrame >= 11.0f)
        {
            CreateBomb3(o->Position, o->SubType);
            if (rand_fps_check(2))
                PlayBuffer(SOUND_SUMMON_EXPLOSION);
        }

        BMD* pModel = &Models[o->Type];
        pModel->Animation(BoneTransform, o->AnimationFrame, o->PriorAnimationFrame, o->PriorAction, o->Angle, o->HeadAngle);
        vec3_t vPos, vRelative, vLight;
        Vector(0.0f, 0.0f, 0.0f, vRelative);
        int iPositions[] = { 13, 23, 39, 49, 3, 4, 5, 61 };
        for (int i = 0; i < 8; ++i)
        {
            pModel->TransformPosition(BoneTransform[iPositions[i]], vRelative, vPos, false);
            VectorAdd(vPos, vTempPosition, vPos);
            Vector(o->Alpha * 0.3f, o->Alpha * 0.3f, o->Alpha * 0.3f, vLight);
            CreateParticleFpsChecked(BITMAP_FIRE_CURSEDLICH, vPos, o->Angle, vLight, 2, 5, o);
            CreateParticleFpsChecked(BITMAP_FIRE_CURSEDLICH, vPos, o->Angle, vLight, 2, 4, o);
            CreateParticleFpsChecked(BITMAP_FIRE_CURSEDLICH, vPos, o->Angle, vLight, 2, 3, o);
        }
    }
        return true;
    }

    // MODEL_SUMMONER_SUMMON_NEIL
    bool Move_MODEL_SUMMONER_SUMMON_NEIL(OBJECT* o, int index, float Luminosity)
    {
        float Matrix[3][4];
    {
        if (o->LifeTime < 20) o->Alpha -= 0.05f;
        else if (o->Alpha < 0.7f) o->Alpha += 0.04f;

        if (o->AnimationFrame > 8 && o->Skill == 0)
        {
            o->Skill = 1;
            CreateEffectFpsChecked(MODEL_SUMMONER_SUMMON_NEIL_NIFE1, o->HeadTargetAngle, o->Angle, o->Light, o->SubType);
            if (o->SubType >= 1)
                CreateEffectFpsChecked(MODEL_SUMMONER_SUMMON_NEIL_NIFE2, o->HeadTargetAngle, o->Angle, o->Light, o->SubType);
            if (o->SubType >= 2)
                CreateEffectFpsChecked(MODEL_SUMMONER_SUMMON_NEIL_NIFE3, o->HeadTargetAngle, o->Angle, o->Light, o->SubType);
        }
        if (o->AnimationFrame > 10 && o->Skill == 1)
        {
            o->Skill = 2;

            AngleMatrix(o->Angle, Matrix);
            vec3_t vMoveDir, vPosition;
            Vector(0, -60.0f, 0, vMoveDir);
            VectorRotate(vMoveDir, Matrix, vPosition);
            VectorAdd(o->Position, vPosition, vPosition);
            CreateEffectFpsChecked(MODEL_SUMMONER_SUMMON_NEIL_GROUND1, vPosition, o->Angle, o->Light, o->SubType, o);

            CreateEffectFpsChecked(MODEL_SUMMONER_SUMMON_NEIL_GROUND1, o->HeadTargetAngle, o->Angle, o->Light, o->SubType);
            if (o->SubType >= 1)
                CreateEffectFpsChecked(MODEL_SUMMONER_SUMMON_NEIL_GROUND2, o->HeadTargetAngle, o->Angle, o->Light, o->SubType);
            i
```
