# MODEL_ARROW_DARKSTINGER

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/sketbows_arrows.bmd

## Fragmento de CreateEffect

```cpp
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
            }
            break;

            case MODEL_SNOW1:
            {
                for (int j = 0; j < 2; j++)
                {
                    CreateEffectFpsChecked(MODEL_SNOW2 + rand() % 2, o->Position, o->Angle, o->Light);
                    CreateParticleFpsChecked(BITMAP_SMOKE, o->Position, o->Angle, o->Light);
                }
                PlayBuffer(SOUND_BREAK01);
            }
            break;
            case MODEL_WOOSISTONE:
                if (Range <= 30.f)
                {
                    for (int j = 0;
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_ARROW_DARKSTINGER(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        vec3_t Angle;
        vec3_t Position;
        float Matrix[3][4];
    {
        CheckClientArrow(o);

        BMD* pModel = &Models[o->Type];

        int iNumCreateFeather = rand() % 2;

        vec3_t vPos;

        Vector(0.6f, 0.7f, 0.9f, Light);
        pModel->Animation(BoneTransform, o->AnimationFrame, o->PriorAnimationFrame, o->PriorAction, o->Angle, o->HeadAngle, false, false);
        pModel->TransformByObjectBone(vPos, o, 0);

        float Matrix[3][4];
        vec3_t Angle;
        vec3_t Position;
        VectorCopy(o->Angle, Angle);
        AngleMatrix(Angle, Matrix);
        VectorRotate(o->Direction, Matrix, Position);
        VectorAdd(vPos, Position, vPos);

        CreateSprite(BITMAP_LIGHT, vPos, 3.0f, Light, o);
        CreateSprite(BITMAP_LIGHT, vPos, 2.0f, Light, o);

        if ((int)o->LifeTime == 30 || (int)o->LifeTime == 28 || (int)o->LifeTime == 26 || (int)o->LifeTime == 24)
        {
            int iNumCreateFeather = rand() % 3;
            Vector(0.6f, 0.7f, 0.9f, Light);
            pModel->TransformByObjectBone(vPos, o, 1);
            for (int i = 0; i < iNumCreateFeather; i++)
            {
                CreateEffectFpsChecked(MODEL_FEATHER, vPos, o->Angle, Light, 0, NULL, -1, 0, 0, 0, 0.6f);
                CreateEffectFpsChecked(MODEL_FEATHER, vPos, o->Angle, Light, 1, NULL, -1, 0, 0, 0, 0.6f);
            }
        }

        if ((int)o->LifeTime == 30)
        {
            pModel->TransformByObjectBone(vPos, o, 1);
            Vector(0.4f, 0.4f, 0.9f, Light);
            CreateJointFpsChecked(BITMAP_FLARE + 1, vPos, vPos, o->Angle, 18, o, 90.f, 40, 0, 0, -1, Light);
        }
    }
        return true;
    }
```
