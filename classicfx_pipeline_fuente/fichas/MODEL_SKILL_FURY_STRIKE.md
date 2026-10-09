# MODEL_SKILL_FURY_STRIKE

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case MODEL_SKILL_FURY_STRIKE:
            {
                VectorCopy(o->Angle, o->HeadAngle);
                VectorCopy(o->Position, o->StartPosition);

                o->LifeTime = 20;//18;
                o->SubType = rand() % 100;
                o->Angle[2] += 330.f;
                o->HeadAngle[0] += 80.f;
                o->HeadAngle[2] += 180.f;
                o->Gravity = 50.f;

                o->Weapon = CharacterMachine->PacketSerial++;
            }
            break;
            case MODEL_SKILL_FURY_STRIKE + 1:
                if (o->SubType == 0)
                {
                    o->LifeTime = 35;
                    o->Scale = PKKey / 100.f;
                    o->BlendMesh = 0;
                }
                else if (o->SubType == 1)
                {
                    o->LifeTime = 60;
                    o->Scale = PKKey / 100.f;
                    o->BlendMesh = 0;
                }
                break;
            case MODEL_SKILL_FURY_STRIKE + 2:
                if (o->SubType == 0 || o->SubType == 1)
                {
                    o->LifeTime = 20;
                    o->Scale = PKKey / 100.f;
                    o->BlendMesh = 0;
                    if (SubType =
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_SKILL_FURY_STRIKE(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        vec3_t Angle;
        vec3_t Position;
        float Matrix[3][4];
        vec3_t p;
    {
        vec3_t p, Position, Pos[5];
        vec3_t Angle;
        short  scale = 150;
        float  Matrix[3][4], ang[5];
        int    TargetX, TargetY;

        Vector(0.f, 0.f, 0.f, Angle);
        Vector(0.f, 0.f, 0.f, p);

        if ((int)o->LifeTime == 11)
        {
            vec3_t  light;
            Vector(1.f, 1.f, 1.f, light);
            Vector(0.f, 0.f, 0.f, Angle);

            if (o->Kind != 3)
            {
                Vector(-25.f, -80.f, 0.f, p);
                AngleMatrix(o->Owner->Angle, Matrix);
                VectorRotate(p, Matrix, Position);
                VectorAdd(Position, o->Position, o->StartPosition);
            }

            float AddHeight = 25.f;

            if (gMapManager.InHellas() == true)
            {
                if (o->Kind == 0 || o->Kind == 2)
                {
                    int PositionX = (int)(o->StartPosition[0] / TERRAIN_SCALE);
                    int PositionY = (int)(o->StartPosition[1] / TERRAIN_SCALE);
                    AddWaterWave(PositionX, PositionY, 2, -1000);
                }
                else
                {
                    AddHeight = 100.f;
                }
            }

            o->StartPosition[2] = RequestTerrainHeight(o->StartPosition[0], o->StartPosition[1]) + AddHeight;
            AddHeight = 3.f;

            CreateParticleFpsChecked(BITMAP_EXPLOTION, o->StartPosition, Angle, light, 0, 0.5f);

            if (o->Kind == 0)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (rand_fps_check(1))
                    {
                        Vector((float)(rand() % 60 - 60.f), 0.f, (float)(rand() % 30 + 90), Angle);
                        VectorAdd(Angle, o->Angle, Angle);
                        VectorCopy(o->StartPosition, Position);
                        Position[0] += rand() % 20 - 10;
                        Position[1] += rand() % 20 - 10;
                        CreateJointFpsChecked(BITMAP_JOINT_SPARK, Position, Position, Angle);
                        if (rand_fps_check(8)) CreateParticleFpsChecked(BITMAP_SPARK, Position, Angle, Light);
                    }
                }
            }
            Vector(0.f, 0.f, 0.f, Angle);

            if (o->Kind == 0)
                CreateEffectFpsChecked(MODEL_WAVE, o->StartPosition, Angle, o->Light);

            o->StartPosition[2] -= 27; //* FPS_ANIMATION_FACTOR;

            if (o->Owner != NULL)
            {
                if (o->Owner->Type != MODEL_WEREWOLF_HERO)
                {
                    CreateEffectFpsChecked(MODEL_SKILL_FURY_STRIKE + 3, o->StartPosition, Angle, o->Light, 0, o, scale);
                    CreateEffectFpsChecked(MODEL_SKILL_FURY_STRIKE + 1, o->StartPosition, Angle, o->Light, 0, o, scale);
                    CreateEffectFpsChecked(MODEL_SKILL_FURY_STRIKE + 2, o->StartPosition, Angle, o->Light, 0, o, scale);
                }
            }

            for (int i = 0; i < 5; ++i)
            {
                Vector(0.f, (float)(rand() % 150 + 100), 0.f, p);
                Vector(0.f, 0.f, (float)(o->SubType + (i * 72.f)), Angle);
                AngleMatrix(Angle, Matrix);
                VectorRotate(p, Matrix, Position);
                VectorAdd(Position, o->StartPosition, Position);

                Position[2] += 3;
                scale = rand() % 50 + 40;

                Vector(0.f, 0.f, 45 + (float)(rand() % 30 - 15), Angle);

                TargetX = (int)(Position[0] / TERRAIN_SCALE);
                TargetY = (int)(Position[1] / TERRAIN_SCALE);

                WORD wall = TerrainWall[TERRAIN_INDEX(TargetX, TargetY)];

                if ((wall & TW_NOMOVE) != TW_NOMOVE && (wall & TW_NOGROUND) != TW_NOGROUND && (wall & TW_WATER) != TW_WATER)
                {
                    if (gMapManager.InHellas() == true)
                    {
                        AddHeight = 100.f;
                    }
                    Position[2] = RequestTerrainHeight(Position[0], Position[1]) + AddHeight;

                    CreateEffectFpsChecked(MODEL_SKILL_FURY_STRIKE + 4, Position, Angle, o->Light, 0, o->Owner, scale);
                    CreateEffectFpsChecked(MODEL_SKILL_FURY_STRIKE + 5, Position, Angle, o->Light, 0, o->Owner, scale);
                }
            }
        }
        else if ((int)o->LifeTime == 10)
        {
            for (int j = 0; j < 5; ++j)
            {
                VectorCopy(o->StartPosition, Pos[j]);
                ang[j] = 0.f;
            }

            int count = 0;
            int random;

            for (int j = 0; j < 4; ++j)
            {
                Vector(0.f, rand() % 15 + 85.f, 0.f, p);

                if (j >= 3) count = rand();

                for (int i = 0; i < 5; ++i)
                {
                    if ((count % 2) == 0) random = rand() % 30 + 50;
                    else                random = -(rand() % 30 + 50);

                    ang[i] += random;
                    Angle[2] = ang[i] + (i * (rand() % 10 + 62));

                    AngleMatrix(Angle, Matrix);
                    VectorRotate(p, Matrix, Position);
                    VectorAdd(Position, Pos[i], Pos[i]);

                    TargetX = (int)(Pos[i][0] / TERRAIN_SCALE);
     
```
