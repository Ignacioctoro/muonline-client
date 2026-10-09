# BITMAP_SHINY+6

Cliente: SIN MAPEO LOCAL
Llamadas directas: 4; hijas: 0
Create metadata: SI; Move handler: NO DETECTADO
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
    case BITMAP_SHINY + 6:
        switch (o->SubType)
        {
        case 0:
            Position[0] = o->Position[0] + (float)(rand() % 500 - 250);
            Position[1] = o->Position[1] + (float)(rand() % 500 - 250);
            Position[2] = o->Position[2] - (float)(rand() % 100) + 150.0f;
            CreateParticleFpsChecked(o->Type, Position, o->Angle, o->Light, 0, o->Scale);
            break;
        case 1:
        case 2:
            if (o->Owner == NULL || o->Owner->Live == false)
                o->Live = false;
            else
            {
                o->LifeTime = 100;
                BMD* pModel = &Models[o->Owner->Type];
                int iBone = rand() % pModel->NumBones;
                vec3_t vRelativePos, vWorldPos;
                Vector(0.f, 0.f, 100.f, vRelativePos);
                if (!pModel->Bones[iBone].Dummy)
                {
                    pModel->TransformPosition(o->Owner->BoneTransform[iBone], vRelativePos, vWorldPos, false);
                    VectorScale(vWorldPos, pModel->BodyScale, vWorldPos);
                    VectorAdd(vWorldPos, o->Owner->Position, vWorldPos);
                    if (rand_fps_check(2))
                    {
```
