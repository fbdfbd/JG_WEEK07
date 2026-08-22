# NEMO

정보 전달 방식을 선택해 아이의 성장과 이야기의 흐름을 바꾸는 육성 시뮬레이션입니다.

![NEMO gameplay](docs/images/nemo-gameplay.png)

## 개요

플레이어는 매주 인물, 장소, 사건에 관한 정보 카드를 확인하고 전달 방식을 선택합니다.

- `Direct`: 원문에 가깝게 직접 전달
- `Modified`: 내용을 수정하거나 순화해서 전달
- `Blocked`: 정보를 전달하지 않음

선택 결과는 캐릭터의 능력치, 인물별 만남 횟수와 정보 선택 이력에 누적됩니다. 현재 상태에 따라 낮·밤 이벤트와 대화가 달라지며, 마지막 주차에는 성향·인물 관계·친밀도를 조합해 엔딩을 결정합니다.

## 개발 환경

| 구분 | 내용 |
| --- | --- |
| Unity | `6000.3.9f1` |
| Render Pipeline | Universal Render Pipeline `17.3.0` |
| UI | UGUI, TextMeshPro |
| Input | Input System `1.18.0` |
| Animation | DOTween / DOTween Pro |
| Target | PC Standalone |

## 실행 방법

1. Unity Hub에 Unity `6000.3.9f1`을 설치합니다.
2. 저장소 루트를 Unity 프로젝트로 엽니다.
3. `Assets/Scenes/Main/Title.unity`를 엽니다.
4. Play Mode를 실행합니다.

Build Settings에는 다음 씬이 순서대로 등록되어 있습니다.

1. `Assets/Scenes/Main/Title.unity`
2. `Assets/Scenes/Main/InGame.unity`

## 조작법

| 입력 | 동작 |
| --- | --- |
| 마우스 왼쪽 클릭 | 버튼 선택 및 타이틀 화면 진행 |
| `Space` | 대화·결과 화면 한 단계 진행 또는 현재 전환 건너뛰기 |
| `Left Ctrl` / `Right Ctrl` | 누르고 있는 동안 대화·결과 화면 연속 진행 |

화면에 별도의 계속·건너뛰기 버튼이 표시되는 경우 마우스로도 같은 동작을 수행할 수 있습니다.

## 게임 진행 흐름

```text
주차 시작
  → 정보 카드 및 전달 방식 선택
  → 선택 결과 계산
  → 능력치·정보 선택 이력에 맞는 이벤트 결정
  → 낮 이벤트와 밤 대화 진행
  → 주간 결과 및 능력치 변화 확인
  → 다음 주차 진행
  → 최종 엔딩 판정
```

## 데이터 구성

원본 콘텐츠 데이터는 `Assets/Data/CSV`에 있습니다. 런타임에서는 CSV를 직접 읽지 않고, 변환된 ScriptableObject를 참조합니다.

주요 데이터 파일은 다음과 같습니다.

| 파일 | 용도 |
| --- | --- |
| `weeks.csv`, `week_cards.csv` | 주차와 주차별 카드 구성 |
| `cards.csv`, `card_options.csv` | 정보 카드와 전달 방식별 선택지 |
| `interactions.csv` | 능력치 변경 등 선택 결과 |
| `events.csv`, `event_steps.csv` | 이벤트 정의와 진행 스텝 |
| `event_*_conditions.csv` | 이벤트 발생 조건 |
| `event_choices.csv` | 이벤트 선택지와 다음 스텝 |
| `event_*_dialogue_lines.csv` | 이벤트 및 선택 결과 대사 |
| `weeklytalk.csv` | 주차별 일반 대화 |
| `cutscene_sequences.csv` | 데이터 기반 컷신 명령 |

### CSV 반영 방법

1. `Assets/Data/CSV`의 데이터를 수정합니다.
2. Unity 메뉴에서 `Tools > CSV Import > Validate CSV`를 실행합니다.
3. 검증이 통과하면 `Tools > CSV Import > Import All`을 실행합니다.
4. 생성 결과와 경고를 Console에서 확인합니다.

기본 출력 경로는 `Assets/Data/Generated/CSVImport`입니다. 생성된 ScriptableObject를 직접 수정하면 다음 임포트에서 덮어써질 수 있으므로 원본 CSV를 수정합니다.

검증 단계에서는 ID 중복, 데이터 간 참조, enum 값, 이벤트 스텝 연결과 그룹 내 표시 순서 중복 등을 확인합니다.

엔딩 데이터는 현재 CSV 임포트 대상이 아닙니다. `Assets/Data/Ending`의 ScriptableObject에서 관리합니다.

## 런타임 구조

| 구성 요소 | 역할 |
| --- | --- |
| `WeekFlowController` | 입력 이벤트 연결과 전체 진행 조정 |
| `WeekFlowCommandHandler` | 카드 선택, 주차 실행과 상태 초기화 처리 |
| `WeekFlowRuntimeState` | 캐릭터 상태와 현재 이벤트, 주간 기록 보관 |
| `WeekRunner` | 선택된 카드를 바탕으로 주차 결과 계산 |
| `WeekEventConditionEvaluator` | 능력치·정보 이력 조건 판정 |
| `WeekNarrativeResolver` | 낮 이벤트와 밤 대화 선별 |
| `EndingResolver` | 성향·인물 관계·친밀도 기반 엔딩 구성 |
| `WeekFlowPresenter` | 런타임 상태를 UI 표현 데이터로 변환 |
| `WeekFlowViewBase` | UGUI 화면과 사용자 입력 연결 |

카드 및 이벤트 결과는 `SO_CardInteractionDefinition`을 기반으로 구성합니다. 능력치 변경, 조건부 변경과 반응 로그 추가 등의 구현이 있으며, `GameplayInteractionExecutor`가 공통 실행 경로를 제공합니다.

## 디렉터리 구조

```text
Assets/
├─ Art/                              # 배경 및 UI 이미지
├─ Data/
│  ├─ CSV/                           # 원본 콘텐츠 데이터
│  └─ Generated/CSVImport/           # CSV에서 생성된 ScriptableObject
├─ Editor/CSVImport/                 # CSV 검증 및 임포트 코드
├─ Scenes/Main/
│  ├─ Title.unity
│  └─ InGame.unity
└─ Scripts/
   ├─ Controller/                    # 타이틀 등 씬 흐름
   ├─ Data/                          # 카드·주차·이벤트·엔딩 정의
   ├─ Manager/                       # 공용 오브젝트 관리
   ├─ Runtime/
   │  ├─ Analytics/                  # 플레이 로그 기록
   │  ├─ Cutscene/                   # 이벤트 컷신 처리
   │  ├─ Nemo/                       # 캐릭터 상태와 대화
   │  └─ Week/                       # 주차 진행, 판정과 상태
   └─ UI/                            # UGUI 화면 및 전환
```
