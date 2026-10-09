# BITMAP_JOINT_THUNDER

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: SI; Move handler: SI
Modelo AccessModel: no identificado

## Funcion MoveHandlers

```cpp
bool Move_BITMAP_JOINT_THUNDER(OBJECT* o, int index, float Luminosity)
    {
    {
        float fScale = (rand() % 400 + 100) / 10.0f;
        vec3_t vPosition, vStartPosition;
        VectorCopy(o->StartPosition, vStartPosition);
        VectorCopy(o->Position, vPosition);
        vStartPosition[0] += rand() % 100 - 50;
        vStartPosition[1] += rand() % 100 - 50;
        vPosition[0] += rand() % 100 - 50;
        vPosition[1] += rand() % 100 - 50;

        if (rand_fps_check(2))
        {
            if (o->SubType == 1)
            {
                CreateJointFpsChecked(BITMAP_JOINT_THUNDER, vStartPosition, vPosition, o->Angle, 33, NULL, fScale);
            }
            else
            {
                CreateJointFpsChecked(BITMAP_JOINT_THUNDER, vStartPosition, vPosition, o->Angle, 16, NULL, fScale);
            }
        }

        vec3_t vLight = { 0.45f, 0.45f, 0.7f };

        if (o->LifeTime > 10)
        {
            if (rand_fps_check(2))
                CreateEffectFpsChecked(BITMAP_MAGIC + 1, vPosition, o->Angle, vLight, 11, o);
        }

        if (rand_fps_check(4))
            CreateParticleFpsChecked(BITMAP_SMOKE, vPosition, o->Angle, vLight, 54, 2.8f);

        if (rand_fps_check(4))
        {
            CreateEffectFpsChecked(MODEL_STONE1 + rand() % 2, o->Position, o->Angle, vLight, 13);
        }

        if (o->LifeTime > 5)
        {
            Vector(0.15f, 0.15f, 0.4f, vLight);
            if (rand_fps_check(5))
                CreateEffectFpsChecked(BITMAP_CHROME_ENERGY2, vPosition, o->Angle, vLight, 0);
        }

        if (o->Owner != NULL && rand_fps_check(5) && o->Owner->BoneTransform != NULL)
        {
            BMD* pTargetModel = &Models[o->Owner->Type];
            int iNumBones = pTargetModel->NumBones;
            float fRandom;
            vec3_t vLight, vRelativePos, vPos, vAngle;
            Vector(0.0f, 0.0f, 0.0f, vRelativePos);
            for (int i = 0; i < iNumBones; ++i)
            {
                if (iNumBones > 100 && rand() % iNumBones > iNumBones / 10) continue;
                else if (iNumBones > 50 && rand() % iNumBones > iNumBones / 5) continue;
                else if (iNumBones > 20 && rand() % iNumBones > iNumBones / 2) continue;
                VectorCopy(o->Owner->Position, pTargetModel->BodyOrigin);
                pTargetModel->TransformPosition(o->Owner->BoneTransform[i], vRelativePos, vPos, true);

                Vector(0.2f, 0.2f, 0.8f, vLight);
                fRandom = 3.0f + ((float)(rand() % 20 - 10) * 0.1f);
                CreateParticleFpsChecked(BITMAP_LIGHT, vPos, vAngle, vLight, 5, fRandom);
            }
        }
    }
        return true;
    }
```
