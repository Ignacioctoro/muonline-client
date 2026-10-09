# MODEL_EFFECT_FLAME_STRIKE

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Effect/FlameStrike.bmd

## Fragmento de CreateEffect

```cpp
    case MODEL_EFFECT_FLAME_STRIKE:
        RemoveObjectBlurs(o, 1);
        RemoveObjectBlurs(o, 2);
        RemoveObjectBlurs(o, 3);
        break;
    case MODEL_SUMMONER_SUMMON_LAGUL:
        for (int i = 48; i <= 53; ++i)
        {
            DeleteJoint(BITMAP_JOINT_ENERGY, o, i);
        }
        break;
    }

    o->Live = false;
    o->Owner = NULL;
}

void TerminateOwnerEffectObject(int iOwnerObjectType)
{
    if (iOwnerObjectType < 0)
    {
        return;
    }

    for (int i = 0; i < MAX_EFFECTS; i++)
    {
        OBJECT* o = &Effects[i];

        if (o->Owner != NULL &&
            o->Type == MODEL_AIR_FORCE &&
            o->Owner->Type == iOwnerObjectType)
        {
            o->Owner = NULL;
        }
    }
}

bool DeleteEffect(int Type, OBJECT* Owner, int iSubType)
{
    bool bDelete = false;
    for (int i = 0; i < MAX_EFFECTS; i++)
    {
        OBJECT* o = &Effects[i];
        if (o->Live && o->Type == Type)
        {
            if (iSubType == -1 || iSubType == o->SubType)
            {
                if (o->Owner == Owner)
                {
                    EffectDestructor(o);
                    bDelete = true;
                }
            }
        }
    }

    if (bDelete == fa
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_EFFECT_FLAME_STRIKE(OBJECT* o, int index, float Luminosity)
    {
        vec3_t p;
    {
        if (o->SubType == 0)
        {
            if ((o->LifeTime < 20 && o->Alpha < 0.1f) || o->Owner == NULL)
            {
                EffectDestructor(o);
                return true;
            }
            if (o->LifeTime < 20) o->Alpha -= 0.1f;
            else if (o->Alpha < 1.0f) o->Alpha += 0.1f;

            OBJECT* pObject = o;
            BMD* pModel = &Models[pObject->Type];
            OBJECT* pOwner = pObject->Owner;
            BMD* pOwnerModel = &Models[pOwner->Type];

            // blur
            float Start_Frame = 5.0f;
            float End_Frame = 13.0f;

            if (pOwner->AnimationFrame > End_Frame || pObject->AI == 1)
            {
                pObject->AI = 1;
            }
            else if (pOwner->CurrentAction == PLAYER_SKILL_FLAMESTRIKE)
            {
                pObject->AnimationFrame = pOwner->AnimationFrame;

                pOwnerModel->BodyScale = pOwner->Scale;
                pOwnerModel->CurrentAction = pOwner->CurrentAction;
                VectorCopy(pOwner->Angle, pOwnerModel->BodyAngle);
                VectorCopy(pOwner->Position, pOwnerModel->BodyOrigin);

                pModel->BodyScale = pObject->Scale;
                pModel->CurrentAction = pObject->CurrentAction;
                VectorCopy(pObject->Angle, pModel->BodyAngle);
                VectorCopy(pObject->Position, pModel->BodyOrigin);
                pModel->CurrentAnimation = pObject->AnimationFrame;
                pModel->CurrentAnimationFrame = (int)pObject->AnimationFrame;

                vec3_t  vLight;
                Vector(1.0f, 1.0f, 1.0f, vLight);

                vec3_t StartPos, StartRelative;
                vec3_t EndPos, EndRelative;

                float fOwnerActionSpeed = pOwnerModel->Actions[pOwnerModel->CurrentAction].PlaySpeed * FPS_ANIMATION_FACTOR;
                float fOwnerSpeedPerFrame = fOwnerActionSpeed / 10.f;
                float fOwnerAnimationFrame = pOwner->AnimationFrame - fOwnerActionSpeed;

                float fActionSpeed = pObject->Velocity * FPS_ANIMATION_FACTOR;
                float fSpeedPerFrame = fActionSpeed / 10.f;
                float fAnimationFrame = pObject->AnimationFrame - fActionSpeed;

                for (int i = 0; i < 10; i++)
                {
                    pOwnerModel->Animation(BoneTransform,
                        fOwnerAnimationFrame, (int)fOwnerAnimationFrame - 1,
                        pOwner->PriorAction, pOwner->Angle, pOwner->HeadAngle, false, false);
                    pOwnerModel->RotationPosition(BoneTransform[33], p, p);	// ParentMatrix

                    pModel->Animation(BoneTransform, fAnimationFrame,
                        (int)fAnimationFrame - 1, pObject->PriorAction,
                        pObject->Angle, pObject->HeadAngle, true, true);	// BoneTransform

                    if (fOwnerAnimationFrame >= Start_Frame && fOwnerAnimationFrame <= End_Frame)
                    {
                        Vector(0.f, 0.f, 0.f, StartRelative);
                        Vector(0.f, 0.f, 0.f, EndRelative);
                        pModel->TransformPosition(BoneTransform[9], StartRelative, StartPos, true);
                        pModel->TransformPosition(BoneTransform[6], EndRelative, EndPos, true);
                        CreateObjectBlur(pObject, StartPos, EndPos, vLight, 2, false, o->m_iAnimation + 1);
                        CreateObjectBlur(pObject, StartPos, EndPos, vLight, 2, false, o->m_iAnimation + 2);

                        pModel->TransformPosition(BoneTransform[8], StartRelative, StartPos, true);
                        pModel->TransformPosition(BoneTransform[5], EndRelative, EndPos, true);
                        CreateObjectBlur(pObject, StartPos, EndPos, vLight, 5, false, o->m_iAnimation + 3);
                    }

                    fOwnerAnimationFrame += fOwnerSpeedPerFrame;
                    fAnimationFrame += fSpeedPerFrame;
                }
            }
        }
    }
        return true;
    }
```
