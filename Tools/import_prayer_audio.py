"""Decode prayer samples from the original local FMOD banks without overwrites.

The Android extraction contains the newer combined Master Bank with DLC samples.
The older location export has an empty DLC directory and cannot supply these.
No FMOD runtime libraries or bank files are copied into the project.
"""
from pathlib import Path
import hashlib
import json
import re
import subprocess
import wave

ROOT = Path(__file__).resolve().parents[1]
EXE = ROOT / 'Tools/vgmstream/vgmstream-cli.exe'
ANDROID_BANKS = Path('D:/game/Blasphemous_Android_Extracted/assets')
LOCATION_BANKS = Path('D:/game/Bla_loc_map/ExportedProject/Assets/StreamingAssets')
DEST = ROOT / 'Assets/Brotherhood/Resources/Audio'
PREFIX = 'event:/SFX/Penitent/Prayers/'
ELM_PREFIX = 'event:/SFX/Level/ElmFireTrap/'
# Each alias records its actual bank sample. Variations/layers are retained as
# separate WAVs; runtime callers can select them without fabricating a sound.
SELECTION = {
    'GUARDIAN_APPEAR': ('GUARDIAN_APPEAR', PREFIX+'GuardianAppear'),
    'GUARDIAN_ATTACK': ('GUARDIAN_ATTACK', PREFIX+'GuardianAttack'),
    'GUARDIAN_PARRY': ('GUARDIAN_PARRY', PREFIX+'GuardianParry'),
    'GUARDIAN_MOVE': ('GUARDIAN_MOVEMENT_LOOP', PREFIX+'GuardianMovement'),
    'GUARDIAN_MOVEMENT': ('GUARDIAN_MOVEMENT_LOOP', PREFIX+'GuardianMovement'),
    'GUARDIAN_VANISH': ('GUARDIAN_DISAPPEAR', PREFIX+'GuardianDisappear'),
    'GUARDIAN_DISAPPEAR': ('GUARDIAN_DISAPPEAR', PREFIX+'GuardianDisappear'),
    'GUARDIAN_TURN': ('GUARDIAN_TURN', PREFIX+'GuardianTurn'),
    'GUARDIAN_MOVE_TO_ATTACK': ('GUARDIAN_MOVE_TO_ATTACK', PREFIX+'GuardianMovement'),
    'MIRIAM_APPEAR': ('MIRIAM_PRAYER_GLASS-002', PREFIX+'MiriamPrayerGlass'),
    'MIRIAM_GLASS': ('MIRIAM_PRAYER_GLASS-002', PREFIX+'MiriamPrayerGlass'),
    'MIRIAM_ATTACK': ('MIRIAM_PRAYER_EXECUTION', PREFIX+'MiriamPrayerExecution'),
    'MIRIAM_EXECUTION': ('MIRIAM_PRAYER_EXECUTION', PREFIX+'MiriamPrayerExecution'),
    'MIRIAM_VANISH': ('MIRIAM_PRAYER_GLASS_VANISH', PREFIX+'MiriamPrayerGlassVanish'),
    'MIRIAM_EXECUTION_GLASS': ('MIRIAM_PRAYER_GLASS_EXECUTION-002', PREFIX+'MiriamPrayerExecution'),
    'MIRIAM_SHARD_IN': ('PRAYER_SHARD_SPIKE_1', PREFIX+'MiriamPrayerShardIn'),
    'MIRIAM_SHARD_OUT': ('Prayer_Shard_SPIKE_T2_S1_vanish', PREFIX+'MiriamPrayerShardOut'),
    'ELM_FIRE_IDLE': ('ElmsFireTrap - IDLE', ELM_PREFIX+'ElmFireIdle'),
    'ELM_FIRE_CHARGE': ('ELMS_TRAP_CHARGE_LOOP_2', ELM_PREFIX+'ElmFireCharge'),
    'ELM_FIRE_SHOT': ('ElmFireTrap_SHOT_A_-001', ELM_PREFIX+'ElmFireShot'),
    'ELM_FIRE_SHOT_LAYER': ('ELMS_TRAP_SHOT_LAYER', ELM_PREFIX+'ElmFireShot'),
    'ELM_FIRE_IN': ('ElMFIRETRAP_IN', ELM_PREFIX+'ElmFireIn'),
    'ELM_FIRE_OUT': ('ElMFIRETRAP_OUT', ELM_PREFIX+'ElmFireOut'),
    'ELM_FIRE_IDLE_PULSES': ('ElMFIRETRAP_IDLE_PULSES_LOOP', ELM_PREFIX+'ElmFireIdlePulses'),
    'ELM_FIRE_CHARGE_B_LAYER': ('ELMS_TRAP_CHARGE_LOOP_2_B_LAYER', ELM_PREFIX+'ElmFireCharge'),
    'ELM_FIRE_CHARGE_C_LAYER': ('ELMS_TRAP_CHARGE_LOOP_2_C_LAYER', ELM_PREFIX+'ElmFireCharge'),
    'ELM_FIRE_CHARGE_D_LAYER': ('ELMS_TRAP_CHARGE_D_LAYER', ELM_PREFIX+'ElmFireCharge'),
    'BLOOD_CLOUDS': ('BLOOD_CLOUD', PREFIX+'BloodClouds'),
    'PENITENT_RAY_FIRE': ('PENITENT_RAY_FIRE', PREFIX+'PenitentRayFire'),
    'PRAYER_SHOT': ('MAGIC_SHOT', PREFIX+'PrayerShot'),
    'CHERUB_ATTACK': ('CHERUB_ATTACK', PREFIX+'PrayerShot'),
    'FIREBALL_FLY': ('FIREBALL_FLY', 'event:/SFX/Level/FireballTrapFireFly'),
    'FIREBALL_FLY_LOOP': ('FIREBALL_FLY_LOOP', 'event:/SFX/Level/FireballTrapFireFly'),
    'WATER_PRAYER': ('Water Prayer', PREFIX+'WaterPrayer'),
}
for variant in range(1, 4):
    for group in ['A', 'B']:
        SELECTION[f'ELM_FIRE_SHOT_{group}_{variant}'] = (f'ElmFireTrap_SHOT_{group}_-00{variant}', ELM_PREFIX+'ElmFireShot')
    SELECTION[f'ELM_FIRE_LIGHT_{variant}'] = (f'ElmFireTrap_LIGHT_-00{variant}', ELM_PREFIX+'ElmFireShot')
for variant in range(2, 5):
    SELECTION[f'MIRIAM_SHARD_IN_{variant}'] = (f'PRAYER_SHARD_SPIKE_{variant}', PREFIX+'MiriamPrayerShardIn')
    SELECTION[f'MIRIAM_SHARD_OUT_{variant}'] = (f'Prayer_Shard_SPIKE_T2_S{variant}_vanish', PREFIX+'MiriamPrayerShardOut')


def index_bank(bank):
    if bank == LOCATION_BANKS/'Master Bank.bank':
        cache = ROOT/'Temp/prayer-bank-index.json'
    elif bank == ANDROID_BANKS/'Master Bank.bank':
        cache = ROOT/'Temp/prayer-mobile-bank-index.json'
    else:
        key = hashlib.sha256(str(bank).encode()).hexdigest()[:12]
        cache = ROOT/'Temp'/f'prayer-bank-index-{key}.json'
    if cache.exists():
        entries = json.loads(cache.read_text(encoding='utf-8'))
        if isinstance(entries, list):
            return entries
    metadata = subprocess.check_output([str(EXE), '-m', '-s', '1', '-S', '0', str(bank)], text=True)
    entries = [{'stream':int(i), 'name':n} for i,n in re.findall(r'stream index: (\d+)\s+stream name: ([^\r\n]+)', metadata)]
    cache.parent.mkdir(parents=True, exist_ok=True)
    cache.write_text(json.dumps(entries), encoding='utf-8')
    return entries


def main():
    # Scan every available bank, including any DLC bank added to the exports.
    banks = sorted(ANDROID_BANKS.rglob('*.bank')) + sorted(LOCATION_BANKS.rglob('*.bank'))
    banks = [p for p in banks if not p.name.endswith('.strings.bank')]
    bank_index = {}
    scan_errors = []
    for bank in banks:
        try:
            bank_index[bank] = index_bank(bank)
        except (OSError, subprocess.CalledProcessError) as exc:
            scan_errors.append({'bank':str(bank), 'error':str(exc)})
    mobile_master = ANDROID_BANKS/'Master Bank.bank'
    order = [mobile_master] + [p for p in bank_index if p != mobile_master]
    DEST.mkdir(parents=True, exist_ok=True)
    provenance = []
    missing = []
    imported = 0
    for key, (name, event) in SELECTION.items():
        match = next(((bank, entry) for bank in order for entry in bank_index.get(bank, []) if entry['name'] == name), None)
        if match is None:
            missing.append({'runtimeKey':key, 'sourceEvent':event, 'expectedSample':name})
            continue
        bank, entry = match
        output = DEST/(key+'.wav')
        primary = ROOT/'Assets/Brotherhood/Audio'/(key+'.wav')
        status = 'existing'
        if not output.exists() and not primary.exists():
            subprocess.run([str(EXE), '-i', '-s', str(entry['stream']), '-o', str(output), str(bank)], stdout=subprocess.DEVNULL, check=True)
            status = 'imported'
            imported += 1
        file = output if output.exists() else primary
        with wave.open(str(file)) as audio:
            duration = audio.getnframes()/audio.getframerate()
            channels = audio.getnchannels()
            assert duration > 0 and channels > 0, key+' decoded an empty WAV'
        provenance.append({'runtimeKey':key, 'sourceEvent':event, 'sourceBank':str(bank), 'stream':entry['stream'], 'sourceSample':name, 'file':str(file.relative_to(ROOT)), 'status':status, 'duration':round(duration,6), 'channels':channels})
    # No exact sample/routing evidence exists for these aliases in the exports.
    # Do not substitute a physical hit or shield enemy sample for a spell.
    missing.extend([
        {'runtimeKey':'PENITENT_MAGIC_DAMAGE', 'sourceEvent':'event:/SFX/Penitent/Damage/PenitentMagicDamage', 'reason':'No exact named sample or exported FMOD event routing; all local bank metadata was searched.'},
        {'runtimeKey':'SHIELD_PRAYER', 'reason':'No source event/sample with this name; PenitentShieldSystem instead uses existing PenitentRangeAttackHit.'},
    ])
    data = {
        'sourceSelection':'Newest Android combined Master Bank contains the DLC samples. The location export DLC folder is empty.',
        'mappingLimit':'Runtime aliases use the actual named source sample; FMOD event mixes and random/layer routing are not exported. MAGIC_SHOT for PrayerShot and shard spike samples for MiriamPrayerShardIn/Out are inferred from the matching event/sample role. Separate source variants/layers are retained.',
        'scannedBanks':[str(p) for p in bank_index],
        'scanErrors':scan_errors,
        'samples':provenance,
        'missingAliases':missing,
    }
    (DEST/'prayers-audio-provenance.json').write_text(json.dumps(data,ensure_ascii=False,indent=2), encoding='utf-8')
    print(f'Prayer audio: {imported} new WAVs, {len(provenance)} aliases verified, {len(bank_index)} banks scanned, {len(missing)} unresolved aliases.')
    for entry in missing:
        print('Unresolved:', entry['runtimeKey'])


if __name__ == '__main__':
    main()
