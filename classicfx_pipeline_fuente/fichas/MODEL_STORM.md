# MODEL_STORM

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/Storm[index=1].bmd

## Fragmento de CreateEffect

```cpp
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
                    o->BlendMesh = 0;
                    Vector(0.f, -10.f, 0.f, o->Direction);
                    Vector(1.f, 1.f, 1.f, o->Light);
                    break;
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                {
                    o->LifeTi
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_STORM(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        vec3_t Angle;
        vec3_t Position;
        switch (o->SubType)
        {
        case 0:
            o->BlendMeshLight = o->LifeTime * 0.1f;
            o->BlendMeshTexCoordU = -(float)o->LifeTime * 0.1f;
            //VectorAdd(o->Position,o->Direction,o->Position);
            CreateParticleFpsChecked(BITMAP_SMOKE, o->Position, o->Angle, o->Light, 3);
            Vector(90.f, 0.f, o->Angle[2], Angle);
            if (rand_fps_check(2))
            {
                Vector(o->Position[0] - 200.f, o->Position[1], o->Position[2] + 700.f, Position);
                CreateJointFpsChecked(BITMAP_JOINT_THUNDER, Position, o->Position, Angle, 0, o, 10.f);
            }
            if (rand_fps_check(2))
            {
                Vector(o->Position[0] + 200.f, o->Position[1], o->Position[2] + 700.f, Position);
                CreateJointFpsChecked(BITMAP_JOINT_THUNDER, Position, o->Position, Angle, 0, o, 10.f);
            }
            o->Position[2] = RequestTerrainHeight(o->Position[0], o->Position[1]);
            if (rand_fps_check(4))
                CreateEffectFpsChecked(MODEL_STONE1 + rand() % 2, o->Position, o->Angle, o->Light, 2);
            Vector(-Luminosity * 0.4f, -Luminosity * 0.3f, -Luminosity * 0.2f, Light);
            AddTerrainLight(o->Position[0], o->Position[1], Light, 5, PrimaryTerrainLight);

            if (battleCastle::IsBattleCastleStart())
            {
                DWORD att = TERRAIN_ATTRIBUTE(o->Position[0], o->Position[1]);
                if ((att & TW_NOATTACKZONE) == TW_NOATTACKZONE)
                {
                    o->Velocity = 0.f;
                    Vector(0.f, 0.f, 0.f, o->Direction);
                    o->LifeTime *= pow(1.0f / (5.f), FPS_ANIMATION_FACTOR);
                    break;
                }
            }
            if ((int)o->LifeTime % 15 == 0)
                if (o->Owner == &Hero->Object)
                    AttackCharacterRange(o->Skill, o->Position, 150.f, o->Weapon, o->PKKey);
            break;

        case 1:
            o->BlendMeshLight = o->LifeTime * 0.01f;
            o->BlendMeshTexCoordU = -(float)o->LifeTime * 0.1f;
            o->Angle[2] += (rand() % 30 + 30.f) * FPS_ANIMATION_FACTOR;

            VectorCopy(o->Position, Position);
            Position[2] += 100.f;
            CreateParticleFpsChecked(BITMAP_SMOKE, Position, o->Angle, o->Light, 3);
            CreateParticleFpsChecked(BITMAP_BUBBLE, Position, o->Angle, o->Light, 3, 0.1f);

            Vector(-Luminosity * 0.1f, -Luminosity * 0.3f, -Luminosity * 1.f, Light);
            AddTerrainLight(o->Position[0], o->Position[1], o->Light, 5, PrimaryTerrainLight);
            break;

        case 2:
            o->BlendMeshLight = o->LifeTime * 0.1f;
            o->BlendMeshTexCoordU = -(float)o->LifeTime * 0.1f;

            o->Gravity = (float)(rand() % 360);

            VectorCopy(o->Position, Position);
            Position[2] += 100.f;
            CreateParticleFpsChecked(BITMAP_SMOKE, Position, o->Angle, o->Light, 3);
            CreateParticleFpsChecked(BITMAP_BUBBLE, Position, o->Angle, o->Light, 3, 0.1f);

            Vector(-Luminosity * 0.1f, -Luminosity * 0.3f, -Luminosity * 1.f, Light);
            AddTerrainLight(o->Position[0], o->Position[1], o->Light, 5, PrimaryTerrainLight);
            break;
        case 3:
        case 4:
        case 5:
        case 6:
        case 7:
            EarthQuake = (float)(rand() % 8 - 4) * 0.1f;
            CreateParticleFpsChecked(BITMAP_SMOKE, o->Position, o->Angle, o->Light, 28);
            CreateParticleFpsChecked(BITMAP_SMOKE, o->Position, o->Angle, o->Light, 29);
            CreateParticleFpsChecked(BITMAP_SMOKE, o->Position, o->Angle, o->Light, 30);
            if (rand_fps_check(2))
                CreateEffectFpsChecked(MODEL_STONE1 + rand() % 2, o->Position, o->Angle, o->Light, 2);
            break;
        case 8:
        {
            o->BlendMeshLight = o->LifeTime * 0.1f;
            o->BlendMeshTexCoordU = -(float)o->LifeTime * 0.1f;

            Vector(-Luminosity * 0.1f, -Luminosity * 0.3f, -Luminosity * 1.f, Light);
            AddTerrainLight(o->Position[0], o->Position[1], o->Light, 5, PrimaryTerrainLight);
        }
        break;
        }
        return true;
    }
```
