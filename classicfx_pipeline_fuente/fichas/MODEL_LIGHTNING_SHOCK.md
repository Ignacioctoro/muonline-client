# MODEL_LIGHTNING_SHOCK

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case MODEL_LIGHTNING_SHOCK:
            {
                if (o->SubType == 0)
                {
                    o->LifeTime = 20;
                    o->Position[2] += (280.f) * FPS_ANIMATION_FACTOR;
                    o->Velocity = 0.0f;
                    o->Gravity = 1.0f;
                }
                else if (o->SubType == 1)
                {
                    o->LifeTime = 12;
                    vec3_t vLight;
                    Vector(1.0f, 0.8f, 0.5f, vLight);
                    CreateEffect(BITMAP_DAMAGE_01_MONO, o->Position, o->Angle, vLight, 1);
                    Vector(1.0f, 0.0f, 0.0f, vLight);
                    CreateEffect(BITMAP_DAMAGE_01_MONO, o->Position, o->Angle, vLight, 1);

                    // magic_ground
                    vec34_t Matrix;
                    vec3_t vAngle, vDirection, vPosition;
                    float fAngle;
                    Vector(1.0f, 0.2f, 0.05f, vLight);
                    for (int i = 0; i < 5; ++i)
                    {
                        Vector(0.f, 150.f, 0.f, vDirection);
                        fAngle = o->Angle[2] + i * 72.f;
                        Vector(0.f, 0.f, fAngle, vAngle);
                        AngleMatrix(
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_LIGHTNING_SHOCK(OBJECT* o, int index, float Luminosity)
    {
        float Matrix[3][4];
        float Height;
    {
        vec3_t vLight;

        if (o->SubType == 0)
        {
            if (o->Owner->AnimationFrame > 6.0f || o->LifeTime < 15)
            {
                Vector(0.f, -20.f, -75.f - o->Velocity, o->Direction);
                o->Gravity += (0.1f) * FPS_ANIMATION_FACTOR;
                o->Velocity += (o->Gravity) * FPS_ANIMATION_FACTOR;
            }

            OBJECT* pObject = o;
            OBJECT* pOwner = pObject->Owner;
            BMD* pOwnerModel = &Models[pOwner->Type];

            float fRot = (WorldTime * 0.0006f) * 360.0f;
            float fScale = 0.8f;

            // shiny
            Vector(1.0f, 0.4f, 0.4f, vLight);
            CreateSprite(BITMAP_SHINY + 1, o->Position, 4.0f * fScale, vLight, o, fRot);
            CreateSprite(BITMAP_SHINY + 1, o->Position, 3.0f * fScale, vLight, o, -fRot);
            // magic_ground
            Vector(1.0f, 0.2f, 0.2f, vLight);
            CreateSprite(BITMAP_MAGIC, o->Position, 1.0f * fScale, vLight, o, fRot);
            CreateSprite(BITMAP_MAGIC, o->Position, 0.5f * fScale, vLight, o, -fRot);
            // pin_light
            Vector(1.0f, 0.4f, 0.4f, vLight);
            CreateSprite(BITMAP_PIN_LIGHT, o->Position, 2.0f * fScale, vLight, o, (float)(rand() % 360));
            CreateSprite(BITMAP_PIN_LIGHT, o->Position, 2.0f * fScale, vLight, o, (float)(rand() % 360));

            Vector(1.0f, 0.3f, 0.3f, vLight);
            CreateParticleFpsChecked(BITMAP_MAGIC, o->Position, o->Angle, vLight, 0, 1.f * fScale);

            vec3_t vPos, vLightFlare, vRelative;
            Vector(1.0f, 0.7f, 0.4f, vLight);
            Vector(1.0f, 0.2f, 0.1f, vLightFlare);
            for (int i = 0; i < 11; ++i)
            {
                fScale = (float)(rand() % 80 + 32) * 0.01f * 1.0f;
                Vector(o->Position[0] + (rand() % 70 - 35) * 1.0f, o->Position[1] + (rand() % 70 - 35) * 1.0f,
                    o->Position[2] + (rand() % 70 - 35) * 1.0f, vPos);
                CreateSprite(BITMAP_LIGHT, vPos, 2.2f, vLightFlare, pObject);
                if (rand() % 3 > 0) continue;
                CreateParticleFpsChecked(BITMAP_LIGHTNING_MEGA1 + rand() % 3, vPos, pObject->Angle, vLight, 0, fScale);
            }

            Vector(1.0f, 0.5f, 0.4f, vLight);

            for (int i = 0; i < 2; ++i)
            {
                fScale = (float)(rand() % 60 + 22) * 0.01f * 1.0f;
                int iBone = rand() % 41;
                Vector((rand() % 30 - 15) * 1.0f, (rand() % 30 - 15) * 1.0f, (rand() % 30 - 15) * 1.0f, vRelative);
                pOwnerModel->TransformByObjectBone(vPos, pOwner, iBone, vRelative);
                CreateParticleFpsChecked(BITMAP_LIGHTNING_MEGA1 + rand() % 3, vPos, pObject->Angle, vLight, 0, fScale);
            }

            float Height = RequestTerrainHeight(o->Position[0], o->Position[1]);
            if (o->Position[2] < Height)
            {
                CreateEffectFpsChecked(MODEL_LIGHTNING_SHOCK, o->Position, o->Angle, o->Light, 1, o);
                EffectDestructor(o);
            }
        }
        else if (o->SubType == 1)
        {
            OBJECT* pObject = o;

            float fScale = 0.8f;
            vec3_t vPos, vLightFlare;
            Vector(1.0f, 0.7f, 0.4f, vLight);
            Vector(1.0f, 0.2f, 0.1f, vLightFlare);
            for (int i = 0; i < 11; ++i)
            {
                fScale = (float)(rand() % 80 + 32) * 0.01f * 1.0f;
                Vector(o->Position[0] + (rand() % 70 - 35) * 1.0f, o->Position[1] + (rand() % 70 - 35) * 1.0f,
                    o->Position[2] + (rand() % 70 - 35) * 1.0f, vPos);
                CreateParticleFpsChecked(BITMAP_LIGHTNING_MEGA1 + rand() % 3, vPos, pObject->Angle, vLight, 0, fScale);	// 전기
            }

            vec34_t Matrix;
            vec3_t vAngle, vDirection, vPosition;
            float fAngle;
            Vector(1.0f, 0.0f, 0.0f, vLight);

            for (int i = 0; i < 6; ++i)
            {
                fScale = (float)(rand() % 60 + 22) * 0.01f * 1.0f;
                Vector(0.f, rand() % 400, 0.f, vDirection);
                fAngle = o->Angle[2] + rand() % 360;
                Vector(0.f, 0.f, fAngle, vAngle);
                AngleMatrix(vAngle, Matrix);
                VectorRotate(vDirection, Matrix, vPosition);
                VectorAdd(vPosition, o->Position, vPosition);
                vPosition[2] = RequestTerrainHeight(vPosition[0], vPosition[1]) + 20;

                CreateParticleFpsChecked(BITMAP_LIGHTNING_MEGA1 + rand() % 3, vPosition, pObject->Angle, vLight, 0, fScale);	// 전기
            }

            VectorCopy(o->Position, vPosition);
            vPosition[2] = RequestTerrainHeight(vPosition[0], vPosition[1]) + 10;

            Vector(1.0f, 0.0f, 0.0f, vLight);

            for (int i = 0; i < 2; i++)
                CreateParticleFpsChecked(BITMAP_SMOKE, vPosition, o->Angle, vLight, 58);

            if (rand_fps_check(2))
            {
                Vector(1.0f, 0.0f, 0.0f, vLight);
                CreateParticleFpsChecked(BITMAP_SMOKE, vPosition, o->Angle, vLight, 54, 2.8f);
            }

            CreateEffectFpsChecked(MODEL_STONE1 + rand() % 2, vPosition, o->Angle, vLight, 13, o);
        }
        else if (o->SubType == 2)
        {
            if (o->Owner != NULL && o->Owner->BoneTransform != NULL)
            {
                BMD* pTarge
```
