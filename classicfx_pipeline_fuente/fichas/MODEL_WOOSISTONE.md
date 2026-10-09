# MODEL_WOOSISTONE

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 1
Create metadata: NO; Move handler: NO DETECTADO
Modelo AccessModel: Skill/woositone.bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_WOOSISTONE:
                if (Range <= 30.f)
                {
                    for (int j = 0; j < 20; j++)
                    {
                        CreateEffectFpsChecked(MODEL_WOOSISTONE, o->Position, o->Angle, o->Light, 1);
                        CreateParticleFpsChecked(BITMAP_FIRE, o->Position, o->Angle, o->Light, 0, 1, o);
                    }
                    PlayBuffer(SOUND_BREAK01);
                }
                else
                {
                    o->LifeTime = 100;
                }
                break;
            }
        }
    }
}

void CreateEffectFpsChecked(int Type, vec3_t Position, vec3_t Angle, vec3_t Light, int SubType, OBJECT* Owner, short PKKey, WORD SkillIndex, WORD Skill, WORD SkillSerialNum, float Scale, short int sTargetIndex)
{
    if (rand_fps_check(1))
    {
        CreateEffect(Type, Position, Angle, Light, SubType, Owner, PKKey, SkillIndex, Skill, SkillSerialNum, Scale, sTargetIndex);
    }
}

void CreateEffect(int Type, vec3_t Position, vec3_t Angle, vec3_t Light, int SubType, OBJECT* Owner, short PKKey, WORD SkillIndex, WORD Skill, WORD SkillSerialNum, float Scale, short int sTargetIndex)
{
    for (int icntEffect = 0; icntEffect
```
