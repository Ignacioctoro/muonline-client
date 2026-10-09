# MODEL_SKILL_BLAST

Cliente: SIN MAPEO LOCAL
Llamadas directas: 5; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/Blast[index=1].bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_SKILL_BLAST:
                o->LifeTime = 30;
                o->BlendMesh = 0;
                o->Scale = (float)(rand() % 8 + 10) * 0.1f;
                o->Position[0] += (float)(rand() % 100 + 200);
                o->Position[1] += (float)(rand() % 100 - 50);
                o->Position[2] += (float)(rand() % 500 + 300);
                Vector(0.f, 0.f, -50.f - rand() % 50, o->Direction);
                Vector(0.f, 20.f, 0.f, o->Angle);
                VectorCopy(o->Position, o->EyeLeft);
                CreateJoint(BITMAP_JOINT_ENERGY, o->Position, o->Position, o->Angle, 5, o, 100.f);
                o->Weapon = CharacterMachine->PacketSerial;
                break;
            case MODEL_WAVE_FORCE:
                o->BlendMesh = -2;
                o->Scale = 0.9f;
                o->Velocity = 0.5f;
                o->LifeTime = 12;
                o->Skill = 0;
                o->PKKey = -1;
                o->Scale = PKKey / 100.f;
                o->BlendMeshLight = 0.1f;
                break;
            case MODEL_SKILL_INFERNO:
                o->BlendMesh = -2;
                o->Scale = 0.9f;
                o->Velocity = 0.5f;
                switch (o->SubType)
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_SKILL_BLAST(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        vec3_t Position;
        float Height;
        Height = RequestTerrainHeight(o->Position[0], o->Position[1]);
        if (o->Position[2] < Height)
        {
            o->Position[2] = Height;
            Vector(0.f, 0.f, 0.f, o->Direction);
            vec3_t Position;
            Vector(o->Position[0], o->Position[1], o->Position[2] + 80.f, Position);
            for (int j = 0; j < 6; j++)
            {
                if (rand_fps_check(1))
                {
                    CreateEffectFpsChecked(MODEL_STONE1 + rand() % 2, o->Position, o->Angle, o->Light);
                }
            }
            CreateParticleFpsChecked(BITMAP_SHINY + 4, Position, o->Angle, Light);
            CreateParticleFpsChecked(BITMAP_EXPLOTION, Position, o->Angle, Light);
            if (o->Owner == &Hero->Object)
                AttackCharacterRange(o->Skill, o->Position, 150.f, o->Weapon, o->PKKey);
            o->Live = false;
        }
        VectorCopy(o->Position, o->EyeLeft);
        //CreateSprite(BITMAP_SHINY+4,o->Position,2.5f,Light,o);
        Vector(Luminosity * 0.2f, Luminosity * 0.4f, Luminosity * 1.f, Light);
        AddTerrainLight(o->Position[0], o->Position[1], Light, 2, PrimaryTerrainLight);
        return true;
    }
```
