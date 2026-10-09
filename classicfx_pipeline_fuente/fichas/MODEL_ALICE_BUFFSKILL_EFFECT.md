# MODEL_ALICE_BUFFSKILL_EFFECT

Cliente: SIN MAPEO LOCAL
Llamadas directas: 6; hijas: 0
Create metadata: SI; Move handler: SI
Modelo AccessModel: Effect/elshildring.bmd

## Fragmento de CreateEffect

```cpp
                case MODEL_ALICE_BUFFSKILL_EFFECT:
                case MODEL_ALICE_BUFFSKILL_EFFECT2:
                {
                    if (o->SubType == 0 || o->SubType == 1 || o->SubType == 2)
                    {
                        RenderObject(o);
                    }
                }
                break;
                case MODEL_RAKLION_BOSS_CRACKEFFECT:
                {
                    RenderObject(o);
                }
                break;
                case MODEL_RAKLION_BOSS_MAGIC:
                {
                    RenderObject(o);
                }
                break;
                case MODEL_LAVAGIANT_FOOTPRINT_R:
                case MODEL_LAVAGIANT_FOOTPRINT_V:
                {
                    EnableAlphaBlend();
                    if (o->Type == MODEL_LAVAGIANT_FOOTPRINT_R)
                    {
                        RenderTerrainAlphaBitmap(BITMAP_LAVAGIANT_FOOTPRINT_R, o->Position[0], o->Position[1], o->Scale, o->Scale, o->Light);
                    }
                    else
                    {
                        RenderTerrainAlphaBitmap(BITMAP_LAVAGIANT_FOOTPRINT_V, o->Position[0], o->Position[1], o->Scale, o->Scale, o->Light);
                    }
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_ALICE_BUFFSKILL_EFFECT(OBJECT* o, int index, float Luminosity)
    {
        float Matrix[3][4];
    {
        if (o->SubType == 0 || o->SubType == 1 || o->SubType == 2)
        {
            if (o->Owner == NULL || o->Owner->Live == false)
            {
                o->Live = false;
            }

            VectorCopy(o->Owner->Position, o->Position);
            o->Position[2] += (100) * FPS_ANIMATION_FACTOR;
            if (o->LifeTime > 20)
            {
                o->Alpha += (0.05f) * FPS_ANIMATION_FACTOR;
                o->BlendMeshLight += (0.05f) * FPS_ANIMATION_FACTOR;
            }
            else
            {
                o->Alpha -= (0.05f) * FPS_ANIMATION_FACTOR;
                o->BlendMeshLight -= (0.05f) * FPS_ANIMATION_FACTOR;
                if (o->Alpha < 0.f)
                {
                    o->Live = false;
                }
            }

            if (o->Type == MODEL_ALICE_BUFFSKILL_EFFECT)
            {
                o->Angle[2] += (8.f) * FPS_ANIMATION_FACTOR;
            }
            else if (o->Type == MODEL_ALICE_BUFFSKILL_EFFECT2)
            {
                o->Angle[2] -= (8.f) * FPS_ANIMATION_FACTOR;
            }

            o->Scale += (0.035f) * FPS_ANIMATION_FACTOR;

            float fRot = (WorldTime * 0.0006f) * 360.0f;
            vec3_t vLight;

            // flare01
            if (o->SubType == 0)
            {
                Vector(0.8f * o->Alpha, 0.1f * o->Alpha, 0.9f * o->Alpha, vLight);
            }
            else if (o->SubType == 1)
            {
                Vector(1.0f * o->Alpha, 1.0f * o->Alpha, 1.0f * o->Alpha, vLight);
            }
            else if (o->SubType == 2)
            {
                Vector(0.8f * o->Alpha, 0.5f * o->Alpha, 0.2f * o->Alpha, vLight);
            }

            if (o->SubType == 0 || o->SubType == 2)
            {
                CreateSprite(BITMAP_LIGHT, o->Position, 5.f, vLight, o, 0.f, 0);
                CreateSprite(BITMAP_LIGHT, o->Position, 5.f, vLight, o, 0.f, 0);
            }
            else if (o->SubType == 1)
            {
                CreateSprite(BITMAP_LIGHT, o->Position, 5.f, vLight, o, 0.f, 1);
                CreateSprite(BITMAP_LIGHT, o->Position, 5.f, vLight, o, 0.f, 1);
            }

            // shiny04
            if (o->SubType == 0)
            {
                Vector(0.7f * o->Alpha, 0.6f * o->Alpha, 0.9f * o->Alpha, vLight);
            }
            else if (o->SubType == 1)
            {
                Vector(1.0f * o->Alpha, 1.0f * o->Alpha, 1.0f * o->Alpha, vLight);
            }
            else if (o->SubType == 2)
            {
                Vector(0.8f * o->Alpha, 0.5f * o->Alpha, 0.2f * o->Alpha, vLight);
            }

            if (o->SubType == 0 || o->SubType == 2)
            {
                CreateSprite(BITMAP_SHINY + 5, o->Position, 2.0f, vLight, o, fRot);
                CreateSprite(BITMAP_SHINY + 5, o->Position, 1.0f, vLight, o, -fRot);
            }
            else if (o->SubType == 1)
            {
                CreateSprite(BITMAP_SHINY + 5, o->Position, 2.0f, vLight, o, fRot, 1);
                CreateSprite(BITMAP_SHINY + 5, o->Position, 1.0f, vLight, o, -fRot, 1);
            }

            vec3_t vAngle, vPos, vWorldPos;
            float Matrix[3][4];
            Vector(0.f, -200.f, 0.f, vPos);
            for (int i = 0; i < 3; ++i)
            {
                Vector((float)(rand() % 90), 0.f, (float)(rand() % 360), vAngle);
                AngleMatrix(vAngle, Matrix);
                VectorRotate(vPos, Matrix, vWorldPos);
                VectorSubtract(o->Position, vWorldPos, vWorldPos);

                if (o->SubType == 0)
                {
                    Vector(0.7f, 0.5f, 0.7f, vLight);
                }
                else if (o->SubType == 1)
                {
                    Vector(1.0f, 1.0f, 1.0f, vLight);
                }
                else if (o->SubType == 2)
                {
                    Vector(0.8f, 0.5f, 0.2f, vLight);
                }

                if (o->SubType == 0 || o->SubType == 2)
                {
                    CreateJointFpsChecked(BITMAP_JOINT_HEALING, vWorldPos, o->Position, vAngle, 15, o, 5.f, 0, 0, 0, 0, vLight);
                }
                else if (o->SubType == 1)
                {
                    CreateJointFpsChecked(BITMAP_JOINT_HEALING, vWorldPos, o->Position, vAngle, 16, o, 5.f, 0, 0, 0, 0, vLight);
                }
            }
        }
        else if (o->SubType == 3 || o->SubType == 4)
        {
            if (o->Owner == NULL || o->Owner->Live == false)
            {
                o->Live = false;
            }
            else
            {
                o->LifeTime = 100;

                BMD* pModel = &Models[o->Owner->Type];
                int iBone = rand() % pModel->NumBones;

                vec3_t vRelativePos, vWorldPos;
                Vector(0.f, 0.f, 0.f, vRelativePos);

                if (!pModel->Bones[iBone].Dummy)
                {
                    pModel->TransformPosition(o->Owner->BoneTransform[iBone], vRelativePos, vWorldPos, false);
                    VectorScale(vWorldPos, pModel->BodyScale, vWorldPos);
                    VectorAdd(vWorldPos, o->Owner->Position, vWorldPos);

                    if (o->SubType == 3)
                    {
                        CreateParticleFpsChecked(BITMAP_LIGHT + 2, vWorldPos, o->Angle, o->Light, 6, o->Scale);
                   
```
