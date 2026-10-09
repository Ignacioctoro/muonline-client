# BITMAP_LIGHT_MARKS

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: SI; Move handler: NO DETECTADO
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
                case BITMAP_LIGHT_MARKS:
                {
                    float fLight = 1.035f;
                    if (o->LifeTime >= 35)
                    {
                        o->Light[0] *= pow(1.0f / (fLight), FPS_ANIMATION_FACTOR);
                        o->Light[1] *= pow(1.0f / (fLight), FPS_ANIMATION_FACTOR);
                        o->Light[2] *= pow(1.0f / (fLight), FPS_ANIMATION_FACTOR);
                    }
                    else
                    {
                        o->Light[0] *= pow(fLight, FPS_ANIMATION_FACTOR);
                        o->Light[1] *= pow(fLight, FPS_ANIMATION_FACTOR);
                        o->Light[2] *= pow(fLight, FPS_ANIMATION_FACTOR);
                    }
                    BMD* pModel = &Models[o->Owner->Type];
                    vec3_t vPos;
                    const int nBoneCount = 14;
                    int nBone[nBoneCount]
                        = { 20, 20, 19, 18, 17, 2, 35, 26, 36, 27, 37, 28, 39, 30 };
                    float fScale[nBoneCount]
                        = { 1.5f, 1.5f, 0.6f, 1.1f, 0.9f, 0.8f, 0.6f, 0.6f,
                        0.8f, 0.8f, 0.8f, 0.8f, 0.7f, 0.7f };
                    for (int i = 0; i < nBoneCount; ++i)
```
