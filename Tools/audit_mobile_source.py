"""Read-only mobile-source parity and Unity migration audit."""
import json, pathlib, re

ROOT = pathlib.Path(__file__).resolve().parents[1]
MOBILE = pathlib.Path(r"D:/game/Bla_mobile_map/ExportedProject")

def read(path):
    return path.read_text(encoding="utf-8-sig", errors="ignore")

def field(text, name, default=""):
    match = re.search(r"^\s*" + re.escape(name) + r":\s*(.*?)\s*$", text, re.M)
    return match.group(1) if match else default

source_settings = read(MOBILE / "ProjectSettings/ProjectSettings.asset")
target_settings = read(ROOT / "ProjectSettings/ProjectSettings.asset")
source_version = field(read(MOBILE / "ProjectSettings/ProjectVersion.txt"), "m_EditorVersion")
target_version = field(read(ROOT / "ProjectSettings/ProjectVersion.txt"), "m_EditorVersion")

settings = {
    "colorSpace": (field(source_settings, "m_ActiveColorSpace"), field(target_settings, "m_ActiveColorSpace")),
    "orientation": (field(source_settings, "defaultScreenOrientation"), field(target_settings, "defaultScreenOrientation")),
    "minSdk": (field(source_settings, "AndroidMinSdkVersion"), field(target_settings, "AndroidMinSdkVersion")),
    "targetSdk": (field(source_settings, "AndroidTargetSdkVersion"), field(target_settings, "AndroidTargetSdkVersion")),
    "architectures": (field(source_settings, "AndroidTargetArchitectures"), field(target_settings, "AndroidTargetArchitectures")),
}

mobile_assets = MOBILE / "Assets"
required_mobile = [
    "Texture2D/Attack.png", "Texture2D/Dash.png", "Texture2D/Jump.png",
    "Texture2D/Parry.png", "Texture2D/Flask.png", "Texture2D/Prayer.png",
    "Texture2D/RangeAttack.png", "Texture2D/Map.png", "Texture2D/Inventory.png",
    "Texture2D/HD_BaseJoystick.png", "Texture2D/HD_ControlJoystick.png",
    "#Design/Scenes/UI/GenericElements.unity", "#Design/Scenes/UI/MainMenu_MAIN.unity",
    "Resources/core/Penitent.prefab",
]
source_presence = {name: (mobile_assets / name).exists() for name in required_mobile}

project_mobile = ROOT / "Assets/Brotherhood/Resources/MobileControls"
project_presence = {p.stem: p.exists() for p in [project_mobile / (name + ".png") for name in
                    ["Attack","Dash","Jump","Parry","Flask","Prayer","RangeAttack","Map","Inventory","HD_BaseJoystick","HD_ControlJoystick"]]}

penitent = read(mobile_assets / "Resources/core/Penitent.prefab")
tuning = {
    "DashMaxelse": None,
    "DashMaxWalkingSpeed": float(field(penitent, "DashMaxWalkingSpeed", "0")),
    "DashCooldownBase": float(field(penitent, "DashCooldownBase", "0")),
    "DashRideBase": float(field(penitent, "DashRideBase", "0")),
    "FervorBase": float(field(penitent, "FervorBase", "0")),
    "LifeBase": float(field(penitent, "LifeBase", "0")),
    "FlaskBase": float(field(penitent, "FlaskBase", "0")),
    "BeadSlotsBase": float(field(penitent, "BeadSlotsBase", "0")),
}
tuning.pop("DashMaxelse")

runtime = read(ROOT / "Assets/Brotherhood/Runtime/SourceGameplayTuning.cs")
runtime_tuning = {}
for name in ("DashSpeed", "DashRide", "DashCooldown"):
    match = re.search(r"\b" + name + r"\s*=\s*([0-9.]+)f", runtime)
    runtime_tuning[name] = float(match.group(1)) if match else None

report = {
    "sourceRoot": str(MOBILE), "sourceUnity": source_version, "targetUnity": target_version,
    "settings": {key: {"mobile": a, "project": b, "match": a == b} for key,(a,b) in settings.items()},
    "requiredMobileAssets": source_presence, "importedTouchAssets": project_presence,
    "mobilePenitentTuning": tuning, "runtimeTuning": runtime_tuning,
    "migrationDecisions": {
        "renderPipeline": "URP 2D unlit sprite material; source Built-in materials are not copied blindly",
        "input": "Input System UI module plus independent pointer IDs for multitouch",
        "audio": "FMOD runtime replaced by decoded source samples and pooled Unity AudioSources",
        "serialization": "Source YAML is parsed into portable catalog/scenes; source MonoBehaviours are not loaded",
        "android": "ARM64 and API 26 minimum retained for Unity 6000 compatibility; no APK built by audit",
    },
}

docs = ROOT / "Documentation"
docs.mkdir(exist_ok=True)
(docs / "mobile-source-audit.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

passed_assets = sum(source_presence.values()) + sum(project_presence.values())
total_assets = len(source_presence) + len(project_presence)
lines = [
    "# Mobile source and Unity migration audit", "",
    f"- Mobile source Unity: `{source_version}`", f"- Target Unity: `{target_version}`",
    f"- Required source/imported mobile assets present: `{passed_assets}/{total_assets}`", "",
    "## Authoritative mobile values", "",
]
for key,value in tuning.items(): lines.append(f"- {key}: `{value:g}`")
lines += ["", "## Project migration settings", ""]
for key,value in report["settings"].items(): lines.append(f"- {key}: mobile `{value['mobile']}`, project `{value['project']}`")
lines += ["", "## Migration decisions", ""]
for key,value in report["migrationDecisions"].items(): lines.append(f"- **{key}**: {value}")
(docs / "MOBILE_SOURCE_AUDIT.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
print(f"mobile audit: {passed_assets}/{total_assets} assets; {source_version} -> {target_version}")
