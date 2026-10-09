# MODEL_ARROWSRE06

Cliente: MAPEADO - NO IMPLICA FIDELIDAD
Llamadas directas: 0; hijas: 4
Create metadata: SI; Move handler: NO DETECTADO
Modelo AccessModel: Effect/arrowsre06.bmd

## Fragmento de CreateEffect

```cpp
    case MODEL_ARROWSRE06:
    {
        if (o->SubType == 0)
        {
            if (o->Owner->Type != MODEL_PLAYER)
                break;

            BMD* pModel = &Models[o->Owner->Type];
            vec3_t vPos;
            pModel->TransformByObjectBone(vPos, o->Owner, o->PKKey);
            VectorCopy(vPos, o->Position);
            o->Scale -= (1.0f) * FPS_ANIMATION_FACTOR;
            if (o->LifeTime <= 15)
            {
                o->Scale += (0.5f) * FPS_ANIMATION_FACTOR;
            }
        }
        else if (o->SubType == 1)
        {
            if (o->Owner->Type != MODEL_PLAYER)
                break;

            BMD* pModel = &Models[o->Owner->Type];
            vec3_t vPos;
            pModel->TransformByObjectBone(vPos, o->Owner, o->PKKey);
            VectorCopy(vPos, o->Position);

            if (o->LifeTime >= 15)
            {
                o->Scale *= pow(1.05f, FPS_ANIMATION_FACTOR);
            }
            else
            {
                o->Scale *= pow(0.95f, FPS_ANIMATION_FACTOR);
            }

            CreateSprite(BITMAP_LIGHT, o->Position, o->Scale, o->Light, o->Owner);
            CreateSprite(BITMAP_LIGHT, o->Position, o->Scale * 0.8f, o->Light, o->Owne
```
