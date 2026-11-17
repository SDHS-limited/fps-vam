# REVFIELD — Original Enemy Hit SFX Pack

This pack contains 15 newly synthesized sound effects (16-bit PCM WAV, 48 kHz, mono), ready for Unity. All sounds are original procedural synthesis, NOT extracted from any commercial game. No third-party sample license is required for these generated WAV files.

## Files / 사용 용도
01. Hit_Normal_01.wav (0.185 s) — 연속 사격용 짧은 육중한 피격음
02. Hit_Normal_02.wav (0.204 s) — 빠르고 날카로운 명중 확인음
03. Hit_Normal_03.wav (0.232 s) — 연속 사격용 짧은 육중한 피격음
04. Hit_Armor_01.wav (0.285 s) — 단단한 장갑판에 총알이 부딪히는 쇳소리
05. Hit_Armor_02.wav (0.34 s) — 튕겨 나가는 리코셰와 금속 잔향
06. Hit_Shield_01.wav (0.3 s) — 에너지 실드 피격음
07. Hit_Shield_02.wav (0.47 s) — 실드가 깨지는 큰 방전음
08. Hit_Critical_01.wav (0.32 s) — 치명타 명중: 날카로운 고음과 둔중한 타격
09. Hit_Critical_02.wav (0.39 s) — 치명타 명중: 아케이드 보상감이 있는 울림
10. Hit_Heavy_01.wav (0.35 s) — 매그넘 탄착용 묵직한 강타
11. Hit_Heavy_02.wav (0.44 s) — 샷건 다중 펠릿용 퍼지는 강타
12. Hit_Kill_01.wav (0.43 s) — 적 처치 확정: 강한 저역 충격과 짧은 마무리
13. Hit_Kill_02.wav (0.53 s) — 적 처치 확정: SF 잔향 버전
14. Hit_Fever_01.wav (0.29 s) — 피버 상태에서 더 빛나는 타격감
15. Hit_Fever_02.wav (0.36 s) — 피버 상태에서 강조된 크리티컬

## Unity recommended setup
* Import -> Force To Mono: already mono / Load Type: Decompress On Load (for short hit SFX)
* Spatial Blend: 3D for impacts at the enemy position, or 2D for low-volume confirmed hit feedback.
* `PlayOneShot` per actual hit; choose randomly among 3 normal variations, avoid the exact same sample repeating.
* Slightly randomize pitch: 0.94–1.06 for normal hits. Use 1.0 for critical and shield break for consistent recognition.
* Adjust spatial rolloff and sound priority so repeated pellets do not overpower weapon fire; cap impact sources/voices.
* For shotguns, play one `Hit_Heavy_02` per attack/target, not once per pellet.
* `Hit_Kill_*` should be an additional subtle cue or replace the last hit, not fire at full volume simultaneously with all other sound effects.
* Suggested SFX relative volume: Normal -8 dB, Armor -7 dB, Shield -7 dB, Critical -5 dB, Heavy -5 dB, Kill -5 dB; tune against the gunshot mix.

## Preview
Open `REVFIELD_Hit_SFX_Preview.wav` for a sequential audition with ~0.42 s gaps; order follows the file list above.

## Original audio reference approach
* DOOM Eternal: weight and a distinctive impact transient.
* ULTRAKILL: readable light/heavy impact tiers and quick reward feedback.
* Sci-fi arena FPS: electrical shielding/resonance separated from physical impacts.
Do not confuse these creative references with exact recreations or sampled audio.

Designed for REVFIELD. Free to use and modify in the user's game project.
