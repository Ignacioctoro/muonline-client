# BITMAP_PIN_LIGHT

Cliente: SIN MAPEO LOCAL
Llamadas directas: 4; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case BITMAP_PIN_LIGHT:
                switch (o->SubType)
                {
                case 3:
                case 0:	o->LifeTime = 40;	break;
                case 1:
                case 2:	o->LifeTime = 100;	break;
                case 4:
                {
                    o->Alpha = 1.0f;
                    o->LifeTime = 30;
                    o->Scale += ((float)(rand() % 5) / 10.0f) * FPS_ANIMATION_FACTOR;
                    o->Angle[1] = rand() % 360;
                }break;
                }
                break;
            case BITMAP_ORORA:
                switch (o->SubType)
                {
                case 0:
                case 1:	o->LifeTime = 100;	break;
                case 2:
                case 3:	o->LifeTime = 25;	break;
                }
                CreateParticle(o->Type, o->Position, o->Angle, o->Light, o->SubType, 1.0f, o->Owner);
                break;

            case BITMAP_GATHERING:
                o->LifeTime = 10;

                switch (o->SubType)
                {
                case 0:
                    Vector(0.f, -100.f, 0.f, p1);
                    AngleMatrix(o->Angle, Matrix);
                    VectorRotate(p1, Matrix, p2);
```

## Funcion MoveHandlers

```cpp
bool Move_BITMAP_PIN_LIGHT(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Position;
        switch (o->SubType)
        {
        case 3:
        case 0:
            Position[0] = o->Position[0] + (float)(rand() % 500 - 250);
            Position[1] = o->Position[1] + (float)(rand() % 500 - 250);
            Position[2] = o->Position[2] - (float)(rand() % 100) + 150.0f;
            if (rand_fps_check(2))
            {
                if (o->SubType == 3)
                    CreateParticleFpsChecked(o->Type, Position, o->Angle, o->Light, 1, o->Scale);
                else
                    CreateParticleFpsChecked(o->Type, Position, o->Angle, o->Light, 0, o->Scale);
            }
            break;
        case 1:
        case 2:
            if (o->Owner == NULL || o->Owner->Live == false)
                o->Live = false;
            else
            {
                o->LifeTime = 100;
                BMD* pModel = &Models[o->Owner->Type];
                int iBone = rand() % pModel->NumBones;
                vec3_t vRelativePos, vWorldPos;
                Vector(0.f, 0.f, 100.f, vRelativePos);
                if (!pModel->Bones[iBone].Dummy)
                {
                    pModel->TransformPosition(o->Owner->BoneTransform[iBone], vRelativePos, vWorldPos, false);
                    VectorScale(vWorldPos, pModel->BodyScale, vWorldPos);
                    VectorAdd(vWorldPos, o->Owner->Position, vWorldPos);
                    if (rand_fps_check(2))
                    {
                        vWorldPos[2] -= 20.f;
                        CreateParticleFpsChecked(o->Type, vWorldPos, o->Angle, o->Light, 0, o->Scale);
                    }
                }
            }
            break;
        case 4:
            if (o->Owner == NULL || o->Owner->Live == false || o->Alpha <= 0.0f)
            {
                o->Live = false;
            }
            else
            {
                o->Scale -= (0.02f) * FPS_ANIMATION_FACTOR;
                o->Alpha -= (0.001f) * FPS_ANIMATION_FACTOR;

                OBJECT* Owner = o->Owner;
                BMD* pModel = &Models[o->Owner->Type];
                vec3_t vWorldPos, vRelativePos;
                Vector(0.f, 0.f, 0.f, vRelativePos);

                if (!pModel->Bones[11].Dummy)
                {
                    pModel->BodyScale = Owner->Scale;
                    pModel->Animation(BoneTransform, Owner->AnimationFrame, Owner->PriorAnimationFrame, Owner->PriorAction, Owner->Angle, Owner->HeadAngle, false, false);
                    pModel->TransformByObjectBone(vWorldPos, Owner, 11);
                    VectorCopy(vWorldPos, o->Position);

                    CreateSprite(BITMAP_PIN_LIGHT, vWorldPos, o->Scale, o->Light, Owner, o->Angle[1]);
                }
            }break;
        }
        return true;
    }
```
