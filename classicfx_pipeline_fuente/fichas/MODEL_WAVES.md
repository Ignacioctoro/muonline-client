# MODEL_WAVES

Cliente: SIN MAPEO LOCAL
Llamadas directas: 2; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/m_Waves.bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_WAVES:
                o->LifeTime = 20;
                o->Gravity = 0.1f;
                o->BlendMesh = -2;
                o->BlendMeshLight = 1.f;
                Vector(0.f, 0.f, 0.f, o->Angle);
                VectorCopy(o->Position, o->StartPosition);
                o->Position[2] += (50.f) * FPS_ANIMATION_FACTOR;

                if (o->SubType == 0)
                {
                    for (int j = 0; j < 60; ++j)
                    {
                        CreateJoint(BITMAP_LIGHT, o->Position, o->Position, o->Angle, 0, NULL, (float)(rand() % 40 + 70));
                    }
                }
                else if (o->SubType == 1)
                {
                    o->LifeTime = 15;
                    o->RenderType = RENDER_NODEPTH;
                    o->Position[2] += (80.f) * FPS_ANIMATION_FACTOR;
                    o->Scale = 0.1f + rand() % 50 / 100.f;
                    o->Gravity = 0.01f;

                    o->Angle[0] = 90.f;
                    o->Angle[2] = Angle[2];

                    for (int j = 0; j < 2; ++j)
                    {
                        CreateJoint(BITMAP_PIERCING, o->Position, o->Position, o->Angle, 0, NULL, (float)(rand() % 40
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_WAVES(OBJECT* o, int index, float Luminosity)
    {
        if (o->SubType == 0)
        {
            if (o->LifeTime > 4)
            {
                o->Scale += (o->Gravity) * FPS_ANIMATION_FACTOR;
                o->Gravity += (0.1f) * FPS_ANIMATION_FACTOR;
            }
            o->BlendMeshLight *= pow(1.0f / (1.4f), FPS_ANIMATION_FACTOR);
        }
        else if (o->SubType == 1)
        {
            o->Scale += (o->Gravity) * FPS_ANIMATION_FACTOR;
            o->Gravity += (0.07f) * FPS_ANIMATION_FACTOR;
            if (o->Scale > 2.f) o->Scale = 2.f;
            o->BlendMeshLight *= pow(1.0f / (1.5f), FPS_ANIMATION_FACTOR);
        }
        else if (o->SubType == 2)
        {
            o->Scale += (o->Gravity) * FPS_ANIMATION_FACTOR;
            o->Gravity += (0.01f) * FPS_ANIMATION_FACTOR;
            if (o->Scale > 1.5f) o->Scale = 1.5f;
            o->BlendMeshLight *= pow(1.0f / (1.5f), FPS_ANIMATION_FACTOR);
        }
        else if (o->SubType == 3)
        {
            o->Scale += (o->Gravity) * FPS_ANIMATION_FACTOR;
            o->Gravity += (0.005f) * FPS_ANIMATION_FACTOR;
            if (o->Scale > 1.5f) o->Scale = 1.5f;
            o->BlendMeshLight *= pow(1.0f / (1.3f), FPS_ANIMATION_FACTOR);
            o->Angle[1] += (45.f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 4)
        {
            o->Scale += (o->Gravity) * FPS_ANIMATION_FACTOR;
            o->Gravity += (0.002f) * FPS_ANIMATION_FACTOR;
            if (o->Scale > 2.5f) o->Scale = 2.5f;
            o->BlendMeshLight *= pow(1.0f / (1.2f), FPS_ANIMATION_FACTOR);
            o->Angle[1] += (45.f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 5)
        {
            o->Scale += (o->Gravity) * FPS_ANIMATION_FACTOR;
            o->Gravity += (0.015f) * FPS_ANIMATION_FACTOR;
            if (o->Scale > 2.5f) o->Scale = 2.5f;
            o->BlendMeshLight *= pow(1.0f / (1.3f), FPS_ANIMATION_FACTOR);
            o->Angle[1] += (45.f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 6)
        {
            o->Scale += (o->Gravity) * FPS_ANIMATION_FACTOR;
            o->Gravity += (0.015f) * FPS_ANIMATION_FACTOR;
            if (o->Scale > 2.5f) o->Scale = 2.5f;
            o->BlendMeshLight *= pow(1.0f / (1.3f), FPS_ANIMATION_FACTOR);
            o->Angle[1] += (45.f) * FPS_ANIMATION_FACTOR;
        }
        return true;
    }
```
