# MODEL_PIERCING

Cliente: SIN MAPEO LOCAL
Llamadas directas: 0; hijas: 5
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/Piercing.bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_PIERCING:
                o->LifeTime = 100;
                o->BlendMesh = 0;
                o->BlendMeshLight = 1.f;
                o->Gravity = 0;

                switch (SubType)
                {
                case 0:
                {
                    o->Scale = 12.f;
                    VectorCopy(o->Owner->Position, o->StartPosition);
                }
                break;
                case 1:
                    o->HiddenMesh = 0;
                    o->Scale = 24.f;
                    break;
                case 2:
                    o->HiddenMesh = 0;
                    o->Scale = 12.f;
                    o->LifeTime = 10;
                    break;
                case 3:
                {
                    o->Scale = 10.f;
                    Vector(0.9f, 0.4f, 0.6f, o->Light);
                    VectorCopy(o->Owner->Position, o->StartPosition);
                }
                break;
                }

                CreateJoint(BITMAP_FLARE + 1, o->Position, o->Position, o->Angle, 0, o, o->Scale, 30, SubType);
                CreateJoint(BITMAP_FLARE + 1, o->Position, o->Position, o->Angle, 1, o, o->Scale, 30, SubType);
                CreateJoint(BITMAP
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_PIERCING(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        if (o->Owner->Live == false)
        {
            o->LifeTime = -1;
            return true;
        }
        /*
                    Vector(Luminosity*0.9f,Luminosity*0.4f,Luminosity*0.6f,Light);
            AddTerrainLight(o->StartPosition[0],o->StartPosition[1],Light,2,PrimaryTerrainLight);
        */
        o->Gravity += (90.f) * FPS_ANIMATION_FACTOR;
        VectorCopy(o->Owner->Angle, o->Angle);
        VectorCopy(o->Owner->Angle, o->HeadAngle);
        VectorCopy(o->Position, o->StartPosition);
        VectorCopy(o->Owner->Position, o->Position);
        if (o->SubType == 1)
        {
            Vector(1.f, 1.f, 1.f, Light);

            CreateJointFpsChecked(BITMAP_JOINT_THUNDER, o->Position, o->Position, o->Angle, 3, NULL, 20.f, 7); //  전기
            CreateSprite(BITMAP_SHINY + 1, o->Position, (float)(rand() % 8 + 8) * 0.2f, Light, o, (float)(rand() % 360));
        }

        if (gMapManager.InHellas())
        {
            if (o->Owner != NULL && o->Owner->Owner != NULL && o->Owner->Owner == (&Hero->Object))
            {
                int PositionX = (int)(o->Position[0] / TERRAIN_SCALE);
                int PositionY = (int)(o->Position[1] / TERRAIN_SCALE);
                AddWaterWave(PositionX, PositionY, 2, -200);
            }
        }

        o->HeadAngle[1] = o->Gravity;
        return true;
    }
```
