# MODEL_ALICE_DRAIN_LIFE

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: SI; Move handler: SI
Modelo AccessModel: no identificado

## Funcion MoveHandlers

```cpp
bool Move_MODEL_ALICE_DRAIN_LIFE(OBJECT* o, int index, float Luminosity)
    {
    {
        int iNumBones = 0;

        vec3_t vSourcePos, vTargetPos, vLight, vRelativePos;
        Vector(0.0f, 0.0f, 0.0f, vRelativePos);

        OBJECT* pSourceObj = o->Owner;
        OBJECT* pTargetObj = pSourceObj != NULL ? pSourceObj->Owner : NULL;
        if (pSourceObj == NULL || pTargetObj == NULL ||
            pSourceObj->Live == false || pTargetObj->Live == false)
        {
            return true;
        }
        BMD* pSourceModel = &Models[pSourceObj->Type];
        BMD* pTargetModel = &Models[pTargetObj->Type];

        int iRandom = rand() % 10;
        int iCnt = 0;
        switch (iRandom)
        {
        case 0:
            iCnt = 0;
            break;
        case 1:
        case 2:
        case 3:
        case 4:
        case 5:
        case 6:
        case 7:
            iCnt = 1;
            break;
        case 8:
        case 9:
            iCnt = 2;
            break;
        }

        for (int i = 0; i < iCnt; i++)
        {
            VectorCopy(pSourceObj->Position, vSourcePos);
            vSourcePos[0] += ((float)((rand() % 80 - 40)));
            vSourcePos[1] += ((float)((rand() % 60 - 30)));
            vSourcePos[2] += (80.0f + ((float)((rand() % 180 - 100))));
            Vector(1.0f, 0.2f, 0.2f, vLight);
            CreateParticleFpsChecked(BITMAP_LIGHT + 2, vSourcePos, pSourceObj->Angle, vLight, 7, 1.8f);
        }

        if (o->LifeTime <= 60)
        {
            for (int i = 0; i < iCnt; i++)
            {
                VectorCopy(pTargetObj->Position, vTargetPos);
                vTargetPos[0] += ((float)((rand() % 80 - 40)));
                vTargetPos[1] += ((float)((rand() % 60 - 30)));
                vTargetPos[2] += (80.0f + ((float)((rand() % 180 - 100))));
                Vector(1.0f, 0.2f, 0.2f, vLight);
                CreateParticleFpsChecked(BITMAP_LIGHT + 2, vTargetPos, pTargetObj->Angle, vLight, 7, 1.8f);
            }
        }

        if (o->LifeTime <= 70 && o->LifeTime >= 66)
        {
            int iRandom = rand() % 10;
            int iCnt2 = 0;
            switch (iRandom)
            {
            case 0:
                iCnt2 = 0;
                break;
            case 1:
            case 2:
            case 3:
                iCnt2 = 1;
                break;
            case 4:
            case 5:
                iCnt2 = 2;
            case 6:
            case 7:
            case 8:
                iCnt2 = 3;
                break;
            case 9:
                iCnt2 = 4;
                break;
            }

            float fMatrix[3][4];
            vec3_t vDir;
            for (int i = 0; i < iCnt2; i++)
            {
                VectorCopy(pSourceObj->Position, pSourceModel->BodyOrigin);
                pSourceModel->TransformPosition(pSourceObj->BoneTransform[18], vRelativePos, vSourcePos, true);

                AngleMatrix(pSourceObj->Angle, fMatrix);
                vDir[0] = fMatrix[0][1];
                vDir[1] = fMatrix[1][1];
                vDir[2] = fMatrix[2][1];

                VectorNormalize(vDir);

                vSourcePos[0] = vSourcePos[0] + ((vDir[0]) * 100.0f) + (float)((rand() % 10) * 5);
                vSourcePos[1] = vSourcePos[1] + ((vDir[1]) * 100.0f) + (float)((rand() % 10) * 5);
                vSourcePos[2] += (float)((rand() % 10) * 5);

                VectorCopy(pTargetObj->Position, vTargetPos);
                vTargetPos[2] += 100.f + (float)(((rand() % 10 - 5)) * 4);		// 80~120

                Vector(0.8f, 0.1f, 0.2f, vLight);
                CreateJointFpsChecked(BITMAP_DRAIN_LIFE_GHOST, vSourcePos, vTargetPos, o->Angle, 0, pSourceObj, 40.f, 0, 0, 0, -1, vLight);
            }
        }

        if ((int)o->LifeTime == 64)
        {
            VectorCopy(pSourceObj->Position, pSourceModel->BodyOrigin);
            pSourceModel->TransformPosition(pSourceObj->BoneTransform[18], vRelativePos, vSourcePos, true);

            iNumBones = pTargetModel->NumBones;

            for (int i = 0; i < iNumBones; i++)
            {
                VectorCopy(pTargetObj->Position, pTargetModel->BodyOrigin);
                pTargetModel->TransformPosition(pTargetObj->BoneTransform[i], vRelativePos, vTargetPos, true);

                Vector(0.4f, 0.4f, 0.8f, vLight);
                //CreateParticle( BITMAP_LIGHT, vTargetPos, pTargetObj->Angle, vLight, 14, 3.5f);

                if (rand_fps_check(2))
                {
                    VectorCopy(pSourceObj->Position, vSourcePos);
                    vSourcePos[2] += 80.0f;
                    Vector(1.0f, 0.0f, 0.1f, vLight);
                    CreateJointFpsChecked(BITMAP_JOINT_ENERGY, vTargetPos, vSourcePos, pTargetObj->Angle, 45, pSourceObj, 10.0f, -1, 0, 0, -1, vLight);
                }
            }
        }
    }
        return true;
    }
```
