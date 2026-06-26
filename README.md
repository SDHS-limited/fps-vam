REVFIELD
 
> Unity 6 기반 1인칭 슈팅 게임 — 6면 큐브 위를 자유롭게 이동하는 **커스텀 중력 시스템**과 쌍권총 전투를 구현한 프로젝트
 
---
 
##  프로젝트 개요
 
Unity 6 (URP)으로 제작된 3D FPS 게임입니다.  
가장 핵심적인 특징은 **중력 방향이 바뀌는 큐브 환경** — 플레이어와 적이 큐브의 6면 어디에든 붙어서 이동할 수 있으며, 공중에서 가까운 면을 감지하면 자동으로 중력이 전환됩니다.
 
---
 
##  기술 스택
 
| 항목 | 내용 |
|------|------|
| **엔진** | Unity 6 (6000.4.1f1) |
| **렌더 파이프라인** | URP (Universal Render Pipeline) 17.4 |
| **언어** | C# |
| **물리** | Unity Rigidbody + 커스텀 중력 (useGravity OFF) |
| **UI** | TextMeshPro, UI Animator |
| **기타 패키지** | Post Processing 3.5, AI Navigation 2.0, Input System 1.19 |
 
---
 
## 주요 구현 내용
 
### 1. 플레이어 커스텀 중력 시스템 (`GravityPlayerController`)
외부 중력을 끄고 Rigidbody에 직접 힘을 가하는 방식으로 **큐브 6면 자동 중력 전환**을 구현했습니다.
 
- **6방향 SphereCast** — 매 FixedUpdate마다 6방향으로 구체 레이를 쏴 가장 가까운 `Ground` 면 감지
- **Slerp 보간** — 중력 방향이 즉시 바뀌지 않고 부드럽게 전환되도록 구현
- **중력 분리 이동** — 이동 벡터를 수직(중력 방향) / 수평 성분으로 분리해 어느 면에서도 자연스러운 이동 제공
- **대시 시스템** — 우클릭 시 카메라 기준 방향으로 순간이동, 쿨다운 관리
- **바디 정렬** — `Quaternion.FromToRotation`으로 플레이어 몸통이 중력 반대 방향(Up)을 항상 향하도록 자동 회전
- **전환 쿨다운** — 연속적인 면 전환을 방지하는 `switchCooldown` 구현
```csharp
// 중력 방향에 수직인 평면으로 이동 벡터 투영
Vector3 fwd = Vector3.ProjectOnPlane(cameraTransform.forward, gravDir).normalized;
Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, gravDir).normalized;
```
 
### 2. 적 AI 커스텀 중력 (`EnemyGravity`)
플레이어와 동일하게 적도 큐브 6면에 붙어 이동합니다.
 
- **6방향 Raycast** — 가장 가까운 Ground 면 감지, 우선순위 거리 기반 정렬
- **스폰 딜레이** — 생성 직후 일정 시간은 강제 하강 (천장 홀에서 떨어지는 연출)
- **표면 이탈 타임아웃** — 감지 실패 시 마지막 방향 유지, 완전 이탈 시 `CubeCenter` 방향 Fallback
- **표면 법선 정렬** — `Quaternion.FromToRotation`으로 발이 면에 붙도록 자동 회전
- **GravityDir 프로퍼티** — 이동 스크립트와 중력 정보 공유
### 3. 적 AI 행동 (`Enemy`)
- **플레이어 추적 이동** — 중력 평면에 투영된 방향으로 이동, `Quaternion.Slerp`로 부드러운 회전
- **피격 리액션 시스템** — `stunDuration` 동안 이동 중단, `Color.Lerp`로 피격 색상 복구 연출
- **넉백** — 플레이어 방향의 반대로 순간 이동, 중력 평면 투영으로 바닥 뚫김 방지
- **스코어 연동** — 사망 시 `ScoreManager.AddScore()` 호출
### 4. 웨이브 기반 적 스포너 (`EnemySpawner`)
- **시간에 따른 난이도 상승** — 일정 틱마다 스폰 간격 비율 감소 (`intervalDecreaseRate`)
- **소형(Runner) / 대형(Brute) 혼합** — 시간이 지날수록 Brute 확률 증가
- **동시 존재 수 제한** — `maxEnemyCount` 초과 시 스폰 스킵
- **이벤트 시스템** — `OnEnemySpawned`, `OnIntervalChanged` 콜백으로 외부 UI 연동 가능
### 5. 쌍권총 발사 시스템 (`DoubleMagnumShooter`)
- **교대 발사** — 왼쪽/오른쪽 총구를 번갈아 발사 (`currentBarrel` 토글)
- **오브젝트 풀링** — 총알을 매번 생성/파괴하지 않고 풀에서 재사용, GC 최소화
- **총알 물리** — `Update`에서 매 프레임 속도 적분 + 중력 적용, Raycast로 충돌 선행 감지
- **발사 쪽 팔 반동** — 발사한 방향의 `ArmRecoil`만 선택 호출
### 6. 팔 반동 & 카메라 흔들림
- **ArmRecoil** — 발사 시 X축 회전 `targetRot` 순간 이동 → `Vector3.Lerp`로 반동·복구 애니메이션
- **CameraShake** — 코루틴으로 랜덤 오프셋을 매 프레임 적용, 지속시간·진폭 파라미터화
### 7. 블랙홀 수류탄 (`BlackHoleGrenade`)
- **폭발 전 흡입** — `explosionDelay` 동안 매 프레임 반경 내 적을 중심 방향으로 당김
- **범위 폭발 데미지** — `OverlapSphere`로 `explosionRadius` 내 적에게 일괄 데미지
- **G키 투척** — `GrenadeThrower`가 `Rigidbody.AddForce`로 물리 투척
### 8. 타이머 & 아이템 시스템
- **카운트다운 타이머** — `TimerUI`에서 `mm:ss` 포맷으로 실시간 표시
- **시간 보너스 아이템** — `TimeItem` 충돌 시 타이머에 시간 추가 + 애니메이터 트리거로 UI 팝업
---
 
## 프로젝트 구조
 
```
Assets/Scripts/
├── camera.cs                          # 마우스 입력 기반 FPS 카메라 (Yaw/Pitch 분리)
├── ArmRecoil.cs                       # 팔·총 반동 애니메이션
├── CameraShake.cs                     # 발사 시 카메라 흔들림
├── Dash.cs                            # 대시 (미완성 모듈)
│
├── Manager/
│   ├── GravityPlayerController.cs     # 플레이어 커스텀 중력 + 이동 + 점프 + 대시
│   ├── DoubleMagnumShooter.cs         # 쌍권총 발사, 오브젝트 풀, 팔 반동 연동
│   ├── ScoreManager.cs                # 점수 관리 및 UI 표시
│   └── TimeItemSpawner.cs             # 시간 보너스 아이템 주기적 생성
│
├── Enemy/
│   ├── Enemy.cs                       # 적 AI (추적, 피격 리액션, 넉백, 사망)
│   ├── EnemyGravity.cs                # 적 전용 커스텀 중력 (6면 감지, 보간)
│   └── EnemySpawner.cs                # 웨이브 기반 스포너 (난이도 자동 증가)
│
├── Weapon/
│   ├── Bullet.cs                      # 총알 물리, 충돌 처리, 풀 반환
│   └── Item/
│       ├── BlackHoleGrenade.cs        # 블랙홀 수류탄 (흡입 → 폭발)
│       └── GrenadeThrower.cs          # G키 수류탄 투척
│
└── UI/
    ├── TimerUI.cs                     # 카운트다운 타이머 UI
    ├── TimeItem.cs                    # 시간 보너스 아이템 충돌 처리
    ├── hitUI.cs                       # 피격 시 화면 빨간 플래시 효과
    └── CubeFace.cs                    # 큐브 면 타입 정의 (Phase, Ability 등)
```
 
---
 
## 실행 방법
 
1. Unity 6 (6000.4.1f1 이상) 설치
2. 프로젝트 폴더를 Unity Hub로 열기
3. `Assets/Scenes/` 에서 씬 선택 후 Play
---
 
## 기술적 도전과 해결
 
**큐브 면 전환 시 플레이어가 튕기는 문제**  
→ `Slerp` 보간 + `switchCooldown` + 중력 방향 속도 성분 0.6배 감쇠로 해결
 
**적이 스폰 직후 천장에 붙는 문제**  
→ `spawnDropDelay` 동안 표면 감지를 건너뛰고 강제 하강하도록 처리
 
**총알 고속 이동 시 벽 통과 문제**  
→ `Physics.Raycast`로 다음 프레임 이동 경로를 선행 검사해 충돌 감지
 
**오브젝트 풀링으로 GC 최소화**  
→ 총알을 미리 `poolSize`만큼 생성해두고 `SetActive`로 재사용
 
---

## 스크린샷

### 인게임 화면
현재 까지 구현 내용
<img width="1919" height="1079" alt="스크린샷 2026-06-23 100152" src="https://github.com/user-attachments/assets/dc818992-880e-42e0-ba8d-df749fc4dab2" />
<img width="1905" height="1079" alt="스크린샷 2026-06-23 100215" src="https://github.com/user-attachments/assets/982f7507-d9e1-4b63-9033-c7f089c7eb94" />


