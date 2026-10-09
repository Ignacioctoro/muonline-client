# MODEL_FENRIR_THUNDER

Cliente: SIN MAPEO LOCAL
Llamadas directas: 6; hijas: 1
Create metadata: NO; Move handler: SI
Modelo AccessModel: Effect/lightning_type01.bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_FENRIR_THUNDER:
            {
                if (o->SubType == 0)
                {
                    o->LifeTime = 100;
                    o->Scale = 0.3f + (float)(rand() % 100) * 0.002f;
                    o->m_iAnimation = 0;
                    o->Alpha = 0.7f;

                    o->Angle[0] = rand() % 360;
                    o->Angle[1] = rand() % 360;
                    o->Angle[2] = rand() % 360;

                    VectorCopy(Light, o->Light);

                    vec3_t vPos;
                    Vector(0.0f, 0.0f, 0.0f, vPos);
                    BMD* p_b = &Models[o->Owner->Type];
                    int irandom = rand() % 30;

                    if (irandom >= 1 && irandom <= 2)
                    {
                        p_b->TransformPosition(BoneTransform[10], vPos, o->Position, false);
                    }
                    else if (irandom == 3)
                    {
                        o->Scale -= (0.2f) * FPS_ANIMATION_FACTOR;
                        p_b->TransformPosition(BoneTransform[14], vPos, o->Position, false);
                    }
                    else if (irandom >= 4 && irandom <= 5)
                    {
                        p_b->TransformP
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_FENRIR_THUNDER(OBJECT* o, int index, float Luminosity)
    {
        if (o->SubType == 0)
        {
            if (o->Live)
            {
                if (o->m_iAnimation == 0)
                {
                    o->Alpha += (0.3f) * FPS_ANIMATION_FACTOR;
                    if (o->Alpha >= 1.0f)
                    {
                        o->m_iAnimation = 1;
                        o->Alpha = 1.0f;
                    }

                    if (o->Alpha < 0.0f)
                    {
                        o->Alpha = 1.0f;
                    }
                }
                else if (o->m_iAnimation == 1)
                {
                    o->Alpha -= (0.3f) * FPS_ANIMATION_FACTOR;
                    if (o->Alpha <= 0.0f)
                    {
                        o->Alpha = 0.0f;
                        o->Live = false;
                    }
                }
                o->Angle[0] += (0.15f) * FPS_ANIMATION_FACTOR;
                o->Angle[1] += (0.15f) * FPS_ANIMATION_FACTOR;
                o->Angle[2] += (0.15f) * FPS_ANIMATION_FACTOR;
            }
        }
        else if (o->SubType == 1)
        {
            if (o->Live)
            {
                if (o->m_iAnimation == 0)
                {
                    o->Alpha += (0.3f) * FPS_ANIMATION_FACTOR;
                    if (o->Alpha >= 1.0f)
                    {
                        o->m_iAnimation = 1;
                        o->Alpha = 1.0f;
                    }

                    if (o->Alpha < 0.0f)
                    {
                        o->Alpha = 1.0f;
                    }
                }
                else if (o->m_iAnimation == 1)
                {
                    o->Alpha -= (0.3f) * FPS_ANIMATION_FACTOR;
                    if (o->Alpha <= 0.0f)
                    {
                        o->Alpha = 0.0f;
                        o->Live = false;
                    }
                }
                o->Angle[0] += (0.15f) * FPS_ANIMATION_FACTOR;
                o->Angle[1] += (0.15f) * FPS_ANIMATION_FACTOR;
                o->Angle[2] += (0.15f) * FPS_ANIMATION_FACTOR;
            }
        }
        return true;
    }
```
