# MODEL_MAGIC2

Cliente: SIN MAPEO LOCAL
Llamadas directas: 8; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/Magic[index=2].bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_MAGIC2:
                o->BlendMesh = 0;
                o->LifeTime = 20;
                Vector(0.f, -60.f, 0.f, o->Direction);
                if (o->SubType == 2)
                {
                    o->Weapon = CharacterMachine->PacketSerial++;
                }
                break;
            case MODEL_STORM:
                switch (o->SubType)
                {
                case 0:
                    o->LifeTime = 59;
                    o->BlendMesh = 0;
                    Vector(0.f, -10.f, 0.f, o->Direction);
                    //Vector(0.f,-50.f,0.f,o->Direction);
                    o->Position[2] = RequestTerrainHeight(o->Position[0], o->Position[1]);
                    o->Weapon = CharacterMachine->PacketSerial++;
                    break;

                case 1:
                    o->LifeTime = 30;
                    o->HiddenMesh = -2;
                    o->Scale = 1.f;
                    o->BlendMesh = 0;
                    Vector(0.f, 0.f, 0.f, o->Direction);
                    break;

                case 2:
                    o->LifeTime = 59;
                    o->HiddenMesh = -2;
                    o->Scale = 1.f;
                    o->BlendM
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_MAGIC2(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        vec3_t Angle;
        vec3_t Position;
        o->BlendMeshLight = o->LifeTime * 0.1f;
        o->BlendMeshTexCoordU = -(float)o->LifeTime * 0.2f;
        //VectorAdd(o->Position,o->Direction,o->Position);

        for (int j = 0; j < 4; j++)
            if (rand_fps_check(1))
                CreateParticleFpsChecked(BITMAP_SMOKE, o->Position, o->Angle, o->Light, 3);

        Vector(Luminosity * 0.3f, Luminosity * 0.6f, Luminosity, Light);
        AddTerrainLight(o->Position[0], o->Position[1], Light, 3, PrimaryTerrainLight);

        if (o->SubType == 2)
        {
            o->HiddenMesh = 0;
            vec3_t Light2;
            VectorCopy(o->Angle, Angle);
            VectorCopy(o->Position, Position);
            Angle[2] += rand() % 10 - 5;
            if (rand_fps_check(1))
                CreateParticleFpsChecked(BITMAP_SMOKE, o->Position, o->Angle, Light, 11, (float)(rand() % 32 + 80) * 0.015f);

            Vector(1.f, 1.f, 1.f, Light2);
            Position[2] += 50.f;
            CreateSprite(BITMAP_SHINY + 1, Position, 1.5f, Light2, NULL, (float)(rand() % 360));
            CreateSprite(BITMAP_SHINY + 1, Position, 1.5f, Light2, NULL, (float)(rand() % 360));

            CreateSprite(BITMAP_LIGHT, Position, 3.5f, Light, NULL, (float)(rand() % 360));
        }
        return true;
    }
```
