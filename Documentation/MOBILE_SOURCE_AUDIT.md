# Mobile source and Unity migration audit

- Mobile source Unity: `2022.3.62f2`
- Target Unity: `6000.5.9f1`
- Required source/imported mobile assets present: `25/25`

## Authoritative mobile values

- DashMaxWalkingSpeed: `13`
- DashCooldownBase: `1`
- DashRideBase: `0.35`
- FervorBase: `60`
- LifeBase: `88`
- FlaskBase: `2`
- BeadSlotsBase: `2`

## Project migration settings

- colorSpace: mobile `0`, project `0`
- orientation: mobile `4`, project `2`
- minSdk: mobile `0`, project `26`
- targetSdk: mobile `0`, project `0`
- architectures: mobile `0`, project `2`

## Migration decisions

- **renderPipeline**: URP 2D unlit sprite material; source Built-in materials are not copied blindly
- **input**: Input System UI module plus independent pointer IDs for multitouch
- **audio**: FMOD runtime replaced by decoded source samples and pooled Unity AudioSources
- **serialization**: Source YAML is parsed into portable catalog/scenes; source MonoBehaviours are not loaded
- **android**: ARM64 and API 26 minimum retained for Unity 6000 compatibility; no APK built by audit
