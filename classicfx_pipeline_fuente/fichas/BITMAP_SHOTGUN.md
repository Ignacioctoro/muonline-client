# BITMAP_SHOTGUN

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case BITMAP_SHOTGUN:
            {
                o->LifeTime = 10;
                o->Velocity = 1.f;
                Vector(0.f, -30.f, 0.f, o->Direction);
                AngleMatrix(o->Angle, Matrix);
                Vector(0.f, -20.f, 50.f, p1);
                VectorRotate(p1, Matrix, p2);
                VectorAddScaled(o->Position, p2, o->Position, FPS_ANIMATION_FACTOR);

                vec3_t  Angle, Pos, p3;
                Vector(-20.f, -20.f, 60.f, p1);
                VectorRotate(p1, Matrix, p2);
                VectorAdd(Position, p2, Pos);
                VectorCopy(o->Angle, Angle);
                for (int i = 0; i < 20; ++i)
                {
                    Angle[0] = o->Angle[0] + rand() % 20 + 5;
                    Angle[1] += i * 18;
                    CreateJoint(BITMAP_JOINT_SPARK, Pos, Pos, Angle, 1);
                }

                Vector(30.f, -20.f, 60.f, p3);
                VectorRotate(p3, Matrix, p2);
                VectorAdd(Position, p2, Pos);
                VectorCopy(o->Angle, Angle);

                for (int i = 0; i < 20; ++i)
                {
                    Angle[0] = o->Angle[0] + rand() % 20 + 5;
                    Angle[1] += i * 18;
```

## Funcion MoveHandlers

```cpp
bool Move_BITMAP_SHOTGUN(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        vec3_t Angle;
        vec3_t Position;
        float Matrix[3][4];
        vec3_t p;
    {
        Vector(Luminosity * 0.5f, Luminosity * 0.5f, Luminosity * 0.8f, Light);
        AddTerrainLight(o->Position[0], o->Position[1], Light, 2, PrimaryTerrainLight);

        Vector(1.f, 1.f, 1.f, Light);
        VectorCopy(o->Angle, Angle);
        Angle[2] += 90.f;

        o->Scale = ((15 - o->LifeTime) / 20.f) * (rand() % 3 + 2);

        AngleMatrix(Angle, Matrix);
        Vector(0.f, 20.f, 0.f, p);
        VectorRotate(p, Matrix, Position);
        VectorAdd(o->Position, Position, o->StartPosition);
        CreateParticleFpsChecked(BITMAP_FIRE + 2, o->StartPosition, o->Angle, Light, 10, o->Scale);

        Vector(0.f, -20.f, 0.f, p);
        VectorRotate(p, Matrix, Position);
        VectorAdd(o->Position, Position, o->StartPosition);
        CreateParticleFpsChecked(BITMAP_FIRE + 2, o->StartPosition, o->Angle, Light, 10, o->Scale);

        if ((int)o->LifeTime == 1)
        {
            CreateBomb2(o->Position, false);
        }
    }
        return true;
    }
```
