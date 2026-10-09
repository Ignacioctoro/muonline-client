# MODEL_EFFECT_FIRE_HIK3_MONO

Cliente: SIN MAPEO LOCAL
Llamadas directas: 5; hijas: 0
Create metadata: NO; Move handler: NO DETECTADO
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case MODEL_EFFECT_FIRE_HIK3_MONO:
            {
                int iRandNum = (rand() % 100);

                if (iRandNum > 20)
                {
                    CreateParticle(BITMAP_FIRE_HIK3_MONO, o->Position, o->Angle, o->Light, 1, o->Scale);
                }
            }
            break;
            case MODEL_DOOR_CRUSH_EFFECT_PIECE01:
            case MODEL_DOOR_CRUSH_EFFECT_PIECE02:
            case MODEL_DOOR_CRUSH_EFFECT_PIECE03:
            case MODEL_DOOR_CRUSH_EFFECT_PIECE04:
            case MODEL_DOOR_CRUSH_EFFECT_PIECE05:
            case MODEL_DOOR_CRUSH_EFFECT_PIECE06:
            case MODEL_DOOR_CRUSH_EFFECT_PIECE07:
            case MODEL_DOOR_CRUSH_EFFECT_PIECE08:
            case MODEL_DOOR_CRUSH_EFFECT_PIECE09:
            case MODEL_DOOR_CRUSH_EFFECT_PIECE11:
            case MODEL_DOOR_CRUSH_EFFECT_PIECE12:
            case MODEL_DOOR_CRUSH_EFFECT_PIECE13:
            case MODEL_STATUE_CRUSH_EFFECT_PIECE01:
            case MODEL_STATUE_CRUSH_EFFECT_PIECE02:
            case MODEL_STATUE_CRUSH_EFFECT_PIECE03:
            {
                o->LifeTime = 30 + (rand() % 30);
                o->Velocity = 0.f;
                o->PKKey = -1;
                o->Owner = Owner;
```
