# MODEL_SKILL_INFERNO

Cliente: SIN MAPEO LOCAL
Llamadas directas: 7; hijas: 2
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/Inferno[index=1].bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_SKILL_INFERNO:
                o->BlendMesh = -2;
                o->Scale = 0.9f;
                o->Velocity = 0.5f;
                switch (o->SubType)
                {
                case 0:
                    Vector(0.8f, 0.8f, 0.8f, o->Light);
                    o->LifeTime = 15;
                    break;
                case 1:
                    Vector(1.0f, .5f, .2f, o->Light);
                    o->LifeTime = 35;
                    break;
                case 2:
                    o->LifeTime = 12;
                    o->HiddenMesh = SkillIndex;
                    o->Skill = 0;
                    o->PKKey = -1;
                    o->Distance = PKKey;
                    o->Scale = PKKey / 100.f;
                    o->BlendMeshLight = 0.1f;
                    break;
                case 8:
                    o->LifeTime = 12;
                    o->HiddenMesh = SkillIndex;
                    o->Skill = 0;
                    o->PKKey = -1;
                    o->Distance = PKKey;
                    o->Scale = PKKey / 100.f;
                    o->BlendMeshLight = 0.1f;
                    o->Velocity *= 4;
                    break;
                case 3:
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_SKILL_INFERNO(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        if (o->SubType == 2)
        {
            o->Scale += (0.04f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 8)
        {
            o->Scale += (0.04f) * FPS_ANIMATION_FACTOR;
            if (o->LifeTime < 10)
            {
                o->Light[0] *= pow(0.8f, FPS_ANIMATION_FACTOR);
                o->Light[1] *= pow(0.8f, FPS_ANIMATION_FACTOR);
                o->Light[2] *= pow(0.8f, FPS_ANIMATION_FACTOR);
            }
        }
        else if (o->SubType == 3)
        {
            VectorCopy(o->Owner->Position, o->Position);
            o->Position[0] = o->Owner->Owner->Position[0] + 150.f;
        }
        else if (o->SubType == 4)
        {
            VectorCopy(o->Owner->Position, o->Position);

            o->Scale += (0.003f) * FPS_ANIMATION_FACTOR;
            o->Gravity += (0.8f * o->Scale * 30.f) * FPS_ANIMATION_FACTOR;
            o->Position[2] += (o->Gravity) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 5)
        {
            o->Position[2] += (2.f) * FPS_ANIMATION_FACTOR;
            o->Angle[2] += (20.f) * FPS_ANIMATION_FACTOR;
            o->BlendMeshLight = o->LifeTime / 20.f;
            Vector(Luminosity * 0.1f, Luminosity * 0.3f, Luminosity * 0.8f, Light);
            AddTerrainLight(o->Position[0], o->Position[1], Light, 5, PrimaryTerrainLight);
        }
        else if (o->SubType == 6)
        {
            o->Scale += (0.01f) * FPS_ANIMATION_FACTOR;
            o->BlendMeshLight = o->LifeTime / 5.f * 0.1f;
            Vector(Luminosity * 0.8f, Luminosity * 0.3f, Luminosity * 0.1f, Light);
            AddTerrainLight(o->Position[0], o->Position[1], Light, 2, PrimaryTerrainLight);
        }

        if (o->SubType < 4)
        {
            o->BlendMeshLight = o->LifeTime / 20.f;
            if (o->SubType != 2)
            {
                Vector(-Luminosity * 0.5f, -Luminosity * 0.5f, -Luminosity * 0.5f, Light);
                AddTerrainLight(o->Position[0], o->Position[1], Light, 5, PrimaryTerrainLight);
            }
        }
        else if (o->SubType == 8)
        {
            o->BlendMeshLight = o->LifeTime / 20.f;
        }
        else if (o->SubType == 9)
        {
            o->BlendMeshLight = o->LifeTime / 80.f;
            o->Position[2] += (o->Gravity) * FPS_ANIMATION_FACTOR;
            o->Light[0] *= pow(1.0f / (1.04f), FPS_ANIMATION_FACTOR);
            o->Light[2] *= pow(1.0f / (1.04f), FPS_ANIMATION_FACTOR);
            o->Alpha -= (0.01f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 10)
        {
            o->Scale += (0.04f) * FPS_ANIMATION_FACTOR;
            BMD* b = &Models[o->Type];
            VectorCopy(o->Light, b->BodyLight);
        }
        return true;
    }
```
