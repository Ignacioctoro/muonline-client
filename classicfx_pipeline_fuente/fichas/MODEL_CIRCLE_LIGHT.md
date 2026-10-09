# MODEL_CIRCLE_LIGHT

Cliente: SIN MAPEO LOCAL
Llamadas directas: 3; hijas: 0
Create metadata: SI; Move handler: SI
Modelo AccessModel: Skill/Circle[index=2].bmd

## Funcion MoveHandlers

```cpp
bool Move_MODEL_CIRCLE_LIGHT(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        vec3_t Angle;
        vec3_t Position;
        float Matrix[3][4];
        vec3_t p;
        if (o->SubType != 3 && o->SubType != 4)
        {
            int value = 4;

            if (o->LifeTime >= 30)
                o->BlendMeshLight = (40 - o->LifeTime) * 0.1f;
            else
                o->BlendMeshLight = o->LifeTime * 0.1f;
            o->BlendMeshTexCoordU = -(float)o->LifeTime * 0.01f;
            EarthQuake = (float)(rand() % 6 - 6) * 0.1f;

            if (rand_fps_check(value))
            {
                vec3_t p, Position;
                vec3_t Angle;
                float Matrix[3][4];
                Vector(0.f, (float)(rand() % 300), 0.f, p);
                Vector(0.f, 0.f, (float)(rand() % 360), Angle);
                AngleMatrix(Angle, Matrix);
                VectorRotate(p, Matrix, Position);
                VectorAdd(Position, o->Position, Position);
                CreateEffectFpsChecked(MODEL_STONE1 + rand() % 2, Position, o->Angle, o->Light, 1);
            }
            Vector(Luminosity * 1.f, Luminosity * 0.8f, Luminosity * 0.2f, Light);
            AddTerrainLight(o->Position[0], o->Position[1], Light, 4, PrimaryTerrainLight);
        }
        else if (o->SubType == 3)
        {
            int value = 2;

            if (o->LifeTime >= 240)
                o->BlendMeshLight = (250 - o->LifeTime) * 0.1f;
            else
                o->BlendMeshLight = o->LifeTime * 0.1f;

            o->BlendMeshLight = std::min<float>(0.5f, o->BlendMeshLight);
            o->BlendMeshTexCoordU = -(float)o->LifeTime * 0.01f;
            if (o->LifeTime > 30 && rand_fps_check(value))
            {
                vec3_t p, Position;
                vec3_t Angle;
                float Matrix[3][4];
                Vector(0.f, (float)(rand() % 200), 0.f, p);
                Vector(0.f, 0.f, (float)(rand() % 360), Angle);
                AngleMatrix(Angle, Matrix);
                VectorRotate(p, Matrix, Position);
                VectorAdd(Position, o->Position, Position);

                CreateParticleFpsChecked(BITMAP_FLARE_BLUE, Position, o->Angle, o->Light, 0);
                if (o->LifeTime > 40)
                {
                    Position[2] += 600.f;
                    Angle[2] = 45.f;
                    CreateJointFpsChecked(BITMAP_FLARE_BLUE, Position, Position, Angle, 19, NULL, 40);
                }
            }
            Vector(o->BlendMeshLight, o->BlendMeshLight, o->BlendMeshLight, Light);
            AddTerrainLight(o->Position[0], o->Position[1], Light, 4, PrimaryTerrainLight);
        }
        else if (o->SubType == 4)
        {
            int value = 5;

            if (o->LifeTime >= 10)
                o->BlendMeshLight = (20 - o->LifeTime) * 0.1f;
            else
                o->BlendMeshLight = o->LifeTime * 0.1f;

            o->BlendMeshLight = std::min<float>(0.5f, o->BlendMeshLight);
            o->BlendMeshTexCoordU = -(float)o->LifeTime * 0.01f;
            if (rand_fps_check(value) && o->LifeTime > 5)
            {
                vec3_t p, Position;
                vec3_t Angle;
                float Matrix[3][4];
                Vector(0.f, (float)(rand() % 100), 0.f, p);
                Vector(0.f, 0.f, (float)(rand() % 360), Angle);
                AngleMatrix(Angle, Matrix);
                VectorRotate(p, Matrix, Position);
                VectorAdd(Position, o->Position, Position);

                CreateParticleFpsChecked(BITMAP_FLARE_BLUE, Position, o->Angle, o->Light, 0);
                Position[2] += 600.f;
                Angle[2] = 45.f;
                CreateJointFpsChecked(BITMAP_FLARE_BLUE, Position, Position, Angle, 19, NULL, 40);
            }
            Vector(o->BlendMeshLight, o->BlendMeshLight, o->BlendMeshLight, Light);
            AddTerrainLight(o->Position[0], o->Position[1], Light, 4, PrimaryTerrainLight);
        }
        return true;
    }
```
