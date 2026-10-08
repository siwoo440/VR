---
# Claude 작업 인수인계

이 문서는 다른 컴퓨터나 새 대화에서 Claude가 Atelier | Verse 개발을 바로 이어가기 위한 전달 문서입니다. 작업 기준은 이 파일이 포함된 `origin/main` 최신 커밋입니다.

- 마지막 갱신: 2026년 10월 8일
- 마지막 작업: Unity 13일차(VR에서 만들기. 오른손 광선으로 가리켜 놓기·지우기·칠하기, 왼손 위의 부품 판 `XrHandPalette`. 블록 놓기는 `LocalPlayer.InputMapName`의 입력 묶음을 읽어 PC와 VR이 같은 코드를 씀. 기본은 키보드·마우스. 실제 헤드셋으로는 확인하지 못함)
- 마지막 검증: 편집 모드 테스트 97개 통과, 플레이 모드 테스트 107개 통과(그림 찍기 9개 포함, `-captureDir` 없이 돌리면 그 9개는 건너뜀). Windows 빌드 성공과 실행 파일의 자동 확인 통과(그냥 실행하면 VR 프로그램을 찾은 흔적 0줄, `-vr`은 기기가 없어 키보드·마우스로 돌아옴). **실제 헤드셋과 컨트롤러로는 확인하지 못함**(이 컴퓨터에 PC VR 런타임이 없음). 에디터와 실행 파일에서 직접 조작해 보는 확인, Quest 실행도 하지 않음
- 검증 환경: Windows 11, Unity `6000.3.21f1`, Node.js `24.19.0`
- 다음 작업: Unity 14일차. 사용자가 헤드셋으로 켜 본 결과가 있으면 그것부터, 없으면 모양이 다른 부품과 부품 고르는 창. 순서는 `docs/BACKLOG.md` 2절

---
## Claude에게 전달할 시작 문구

> GitHub의 `siwoo440/VR` 저장소에서 `main` 최신 커밋을 복제하고 Atelier | Verse 개발을 이어서 진행해주세요. 먼저 `CLAUDE-HANDOFF.md`, `README.md`, `docs/ROADMAP.md`, `docs/PAGES.md`, `unity/Devlogs`의 가장 최근 일지를 읽고, 이 문서의 "작업 원칙"을 지켜 주세요. 응답은 한국어로 해 주세요.

---
## 새 컴퓨터에서 첫 확인

1. `git clone https://github.com/siwoo440/VR.git`으로 저장소 복제
2. Unity Hub에서 에디터 `6000.3.21f1` 설치. Quest용으로 만들려면 설치 항목에서 **Android Build Support**(OpenJDK, Android SDK & NDK 포함)도 추가
3. Unity Hub에서 `unity` 폴더를 프로젝트로 추가해 열기. 처음 열면 `Library`를 새로 만드느라 몇 분 걸림
4. `Assets/_Project/Scenes/Sandbox` 씬에서 재생. 조작은 `README.md`의 "Unity 프로젝트" 절 참고
5. 화면 시안은 `node scripts/serve.mjs` 실행 뒤 `http://127.0.0.1:3100/`에서 확인(설치할 패키지 없음)
6. Windows 실행 파일은 `node scripts/build-windows.mjs --run`으로 만들고 자동 확인(1~2분, `unity/Builds/Windows/AtelierVerse.exe`). VR로 켜려면 그 옆의 `AtelierVerse-VR.bat`

에디터를 처음 열어도 일차별 셋업은 다시 실행되지 않습니다. 셋업이 만든 프리팹·씬·자산이 이미 커밋되어 있고, 적용 기록(`unity/ProjectSettings/AtelierVerseSetupState.txt`)에 `Day01`~`Day13`이 적혀 있기 때문입니다.

### Windows 환경 참고

- Unity 실행 파일의 기본 위치는 `C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe`입니다.
- 줄바꿈은 `unity/.gitattributes`가 정합니다(Unity YAML은 LF, 그림과 글꼴은 바이너리). 저장소의 다른 파일은 커밋할 때 "LF will be replaced by CRLF" 경고가 나오는데 문제가 아닙니다.
- 이전 작업 컴퓨터는 C 드라이브 여유가 10~15GB였습니다. `Library` 사본을 여러 개 두지 않습니다.
- Claude의 셸 도구로 보내는 명령 하나가 약 8천 자를 넘으면 잘려서 "unexpected EOF"로 실패합니다(12일차에 확인). 긴 파일은 파일 쓰기 도구로 만들고, 여러 군데를 고칠 때는 고치는 스크립트(Node)를 파일로 써서 실행합니다.

---
## 저장소 범위

| 폴더 | 내용 |
| --- | --- |
| `unity/` | 실제 게임이 되는 Unity 프로젝트(URP). 일차 단위로 진행 |
| `unity/Devlogs/DayNN/` | 일차별 개발 일지와 화면 그림 |
| `design/` | 서비스 화면 시안 17개와 구성요소 모음. 동작하지 않는 정적 화면 |
| `docs/ROADMAP.md` | 부족한 부분, 구현 단계, 정해진 것과 남은 질문 |
| `docs/PAGES.md` | 화면 흐름, 화면을 만드는 규칙, 앱 화면과 웹 화면의 구분, 게임 화면의 구성(4.1절) |
| `docs/BACKLOG.md` | 앞으로 넣어야 하거나 넣으면 좋을 구현 요소의 목록. 영역별 표와 "가장 먼저 할 것" |
| `docs/MAP-FORMAT.md` | 맵 파일 형식 1판. 구조, 읽을 때의 검사, 저장 위치, 판을 올리는 방법 |
| `scripts/serve.mjs` | 화면 시안 미리보기 서버 |
| `scripts/build-windows.mjs` | Windows 빌드와 자동 실행 확인(`--run`) |
| `scripts/unity-verify.mjs` | 에디터가 열려 있을 때 쓰는 검증용 사본 도구(아래 "검증 방법") |

이 저장소 밖에 있는 것은 다음과 같습니다.

| 대상 | 위치 | 이 저장소와의 관계 |
| --- | --- | --- |
| 홈페이지 | `siwoo440/HomePage` | 소개 페이지(`/atelier-verse.html`)와 통합 계정의 홈페이지 쪽 코드. 통합 계정 설명은 그 저장소의 `docs/UNIFIED-ACCOUNT.md` |
| Mate \| Verse | 별도 저장소(챗봇 서비스) | 같은 통합 계정을 쓸 예정. 이 저장소에서 고치지 않음 |
| 사업 계획서 | Google 문서의 "12. Atelier \| Verse (VR 샌드박스 서비스)" 탭 | 기획 원본. 문서 주소와 탭 식별자는 저장소에 적지 않으며 필요하면 사용자에게 받음 |
| Project-Mu | 사용자의 다른 Unity 게임 저장소(`siwoo440/Project-Mu`) | 일지·커밋·셋업 스크립트 방식을 여기서 따옴. 참고만 하고 고치지 않음 |

---
## 현재 완료 범위

### 기획과 화면 시안

- 사업 계획서 12번 탭(12.1~12.10)에 서비스 개요, 이용자 제작 콘텐츠 구조, 기술 구성, 개발 계획, 설계 원칙, 질문지, 소식·알림, 디자인 테마, 시장 조사, 화면 시안이 있습니다.
- `design/`에 화면 17개(시작하기, 처음 안내, 내 작업실, 에디터, 방 안, 프로필, 설정, 둘러보기, 검색, 맵 상세, 소식, 알림, 신고 처리, 메시지, 상점, 구독, 상태 화면)와 시안 목록, 구성요소 모음이 있습니다.
- 테마는 "햇살 작업실"이며 색 값은 `design/assets/tokens.css`에 있습니다. Unity에서는 `AtelierPalette`가 같은 값을 가집니다.

### Unity 1일차

- 프로젝트(URP 17.3.0, Input System 1.20.0), 모눈 바닥과 블록을 놓은 `Sandbox` 씬, `Boot` 씬, 키보드·마우스 걷기, 모눈 계산(`GridMath`).

### Unity 2일차

- 블록 캐릭터(`AvatarView`)와 머리 위 이름표(`Nameplate`).
- 마우스 휠로 1인칭과 3인칭을 오가는 카메라. 뒤에 벽이 있으면 벽 앞에서 멈춤.
- 게임 화면(`GameUI` 프리팹). 사용자 결정에 따라 로블록스와 VRChat을 섞음.
  - 늘 보이는 화면: 왼쪽 위 메뉴 단추, 방 이름과 인원, 오른쪽 위 사람들 목록, 아래 부품 칸 9개, 키 안내.
  - Esc 메뉴: 탭 4개(바로가기·사람들·설정·도움말)와 아래 큰 단추 3개(게임 끝내기·시작 위치로·돌아가기).
- 개인 설정 저장(`GameSettings`: 마우스 감도, 시야각, 사람들 목록 표시).
- 한글 글꼴(나눔고딕)과 TextMesh Pro.

### Unity 3일차

- 부품을 고르고 조준한 칸에 블록 놓기(마우스 왼쪽)와 지우기(마우스 오른쪽). 놓일 칸에 반투명 미리 보기 블록이 보임.
- 블록의 기록(`BlockMap`)과 화면(`BlockWorld`), 부품 목록(`PartCatalog`). 1일차의 집·단·계단 블록 19개를 블록 세계로 옮겨 똑같이 지울 수 있음.
- 부품 칸 위의 놓기 안내와 블록 수 표시(`블록 19/500`).
- 3인칭에서 카메라가 어깨 너머로 비켜서 조준점이 머리에 가리지 않음.

### Unity 4일차

- 맵 데이터 형식 1판(`docs/MAP-FORMAT.md`, `MapDocument`): 부품은 저장용 이름으로 적고, 조립품과 동작 규칙은 빈 자리만 둠. 형식 이름·판 번호 검사, 옛 판을 올려 읽는 길, 모르는 부품·범위 밖·중복·상한 초과 건너뛰기.
- 이 기기의 맵 파일 하나(`persistentDataPath/maps/local.map.json`)에 자동 저장(`MapAutoSave`, `MapStorage`): 블록이 바뀌고 1초 뒤, 앱을 끝내거나 씬을 떠날 때. 파일이 없으면 씬의 블록 19개를 첫 맵으로 저장하고, 깨진 파일은 옆으로 옮겨 둠.
- 화면 오른쪽 아래의 저장 표시("저장했습니다", "저장 대기 중", "불러왔습니다", "블록 N개를 읽지 못했습니다", "저장하지 못했습니다").

### Unity 5일차

- 기록 층 `EditHistory`(`IBlockStore` 위에서 동작): 놓기·지우기를 "칸, 이전 부품, 이후 부품"으로 기록해 최근 100번까지 되돌리고 다시 실행. `BlockBuilder`의 편집은 `world.History`를 거침.
- 입력 `Game` 맵에 `Undo`(Ctrl+Z)·`Redo`(Ctrl+Y) 추가. 메뉴가 열려 있으면 동작하지 않음. 놓기 안내와 도움말에 키 표시.

### Unity 6일차

- 칠하기: 부품을 고르고 블록을 가리킨 채 마우스 가운데 단추나 F(`Player` 맵의 `Paint`). `EditHistory.Replace`와 `IBlockStore.Replace`로 기록하고 되돌림.
- 알림 띠: `Notice.Post` → `NoticeBar`(화면 위 가운데, 2.5초). 놓을 수 없는 까닭(`BlockBuilder.Blocked`: 범위 밖·이미 있음·상한·겹침), 칠하기 안내, 되돌릴 것 없음, 읽지 못한 블록 수, 깨진 파일, 저장 실패.
- 테스트 틀: `SandboxPlayTests`도 `PlayTestBase`를 쓰고, `TearDown`이 떠 있는 씬의 남은 변경을 먼저 저장.

### Unity 7일차

- 날기(9일차부터 `CharacterMotor.IsFlying`, `SetFlying`): V(`Player` 맵의 `Fly`)로 켜고 끔. 중력 없이 Space(Jump)로 오르고 Shift(Sprint)로 내려오며 속도 7m/s. 블록과 부딪힘. 시작 위치로 돌아가면 꺼짐. 저장하지 않음.
- 오른쪽 아래 걷기·날기 표시(`GameUi.modeLabel`·`modeDot`), 켜고 끌 때 알림, 키 안내와 도움말의 V.

### Unity 8일차

- `Editor/BuildPlayer.cs`: Windows 64비트 빌드(Mono, 빌드 설정의 씬). 명령줄 `-executeMethod AtelierVerse.EditorTools.BuildPlayer.BuildWindows -buildOut 폴더 [-development]`, 메뉴 `Atelier Verse/Windows 빌드 만들기`.
- `scripts/build-windows.mjs`: 빌드 뒤 `--run`으로 실행 파일을 `-quitAfter 10 -screenshotOut`으로 띄워 종료 코드·로그 예외·맵 파일·화면 그림을 확인. 2026-10-08 실제로 104MB·61초 빌드, 실행 확인 통과.
- `Bootstrap`이 `-quitAfter 초`와 `-screenshotOut 경로`를 읽음(자동 확인 전용, 인자가 없으면 전과 같음).
- 플레이어 설정: 창 모드(1600×900, 크기 조절), 뒤에서도 실행, Mono. Esc 메뉴에 `v{Application.version}` 표시(`GameUi.brandLabel`).

### Unity 9일차

- `DesktopPlayerController`가 함께 맡던 일을 셋으로 나눔. `CharacterMotor`(걷기·달리기·점프·중력·날기·시작 위치로, 입력을 모름), `ViewRig`(위아래 시선·1인칭과 3인칭 거리·어깨 너머·벽 앞 멈춤·시야각, PC 전용), `DesktopPlayerController`(키보드·마우스를 읽어 둘에 전함, 마우스 잡기와 조작 막기).
- `GameUi`와 `BlockBuilder`는 옮겨 간 것을 `player.Motor`·`player.Rig`에서 읽음. 옛 멤버를 `DesktopPlayerController`에 남겨 두지 않음.
- `Editor/PlayerWiring.cs`가 세 스크립트를 붙이고 잇는 일을 맡고 1·2·9일차 셋업이 함께 씀.
- 조작 스크립트 없이 몸만 있는 캐릭터가 걷고 달리고 뛰고 나는 것을 `MotorPlayTests`로 확인.

### Unity 10일차

- 조작 방식 고르기 `PlayerModeSwitch`: 평소에는 키보드·마우스, VR 화면이 켜져 있으면(`XRSettings.isDeviceActive`) VR 조작으로 시작. `SetMode`로 실행 중에 바꿀 수 있음. `Forced`는 테스트와 개발 확인용.
- 추적 리그 `XrRig`: PC용 카메라를 추적 기준점(`XrOrigin`) 아래로 옮겨 머리로 쓰고, 두 손(작은 블록)을 컨트롤러 자리에 놓음. 몸은 숨김. 꺼지면 카메라를 돌려놓음.
- VR 조작 `XrPlayerController`: 왼쪽 스틱 걷기(머리가 보는 쪽), 스틱 누르기 달리기, 오른쪽 스틱 좌우 45도 끊어 돌기, 오른손 첫째 단추 점프, 둘째 단추 날기, 날 때 오른쪽 스틱 위아래. 머리가 몸에서 벗어나면 몸이 따라옴(`CharacterMotor.Shift`).
- 입력 자산의 `XR` 묶음(동작 11개)은 컨트롤러의 공통 쓰임새(`{Primary2DAxis}` 등)로 묶음.
- VR에서는 블록 놓기를 끄고, 화면 요소는 헤드셋 안에 보이지 않음.

### Unity 11일차

- 패키지: XR Plugin Management 4.5.4, OpenXR Plugin 1.16.1(따라온 것: XR Core Utilities 2.6.0, XR Legacy Input Helpers 2.1.13). 사용자가 내려받기를 허락함(2026-10-08).
- `Day11Setup`: PC(Standalone)의 XR 설정에 OpenXR 로더를 넣고 "시작할 때 XR 켜기"를 끔. 컨트롤러 프로파일 여섯 개(Oculus Touch, Meta Quest Touch Plus·Pro, Valve Index, HTC Vive, Microsoft Motion)를 켬. 설정 자산은 `Assets/XR`.
- `XrSession`(`Scripts/Core`): 실행 인자에 `-vr`이 있을 때만 첫 씬이 열리기 전에 OpenXR 로더를 켬. 추적 기준을 바닥으로 맞추고, 켜지 못하면 키보드·마우스로 시작하며 알림 띠에 까닭을 알림. 에디터에서는 메뉴 `Atelier Verse/재생할 때 VR 켜기`.
- `XrBootConfig`(`Editor`, 빌드가 끝나면 자동): 실행 파일의 시작 설정(`boot.config`)에서 XR 사전 초기화 줄을 빼고, 실행 파일 옆에 `AtelierVerse-VR.bat`(`-vr`을 붙여 켬)을 씀.
- `scripts/build-windows.mjs`: `--vr`(실행 확인을 `-vr`로), 시작 설정과 배치 파일 검사, 평소 실행의 로그에 VR 프로그램을 찾은 흔적이 있으면 실패.
- 이 컴퓨터에는 VR 프로그램이 없어 `-vr` 실행은 "VR 기기를 찾지 못해 키보드·마우스로 시작합니다"로 돌아오는 것까지만 확인함.

### Unity 12일차

- 이 기기의 캐릭터 `LocalPlayer`(`Scripts/Player`): 화면과 도구가 조작 방식을 직접 알지 않고 거치는 한곳. 조작 방식(`Mode`, `IsVr`), 조작 막기(`SetInputBlocked`, `ResumeControl`), 조준(`IsAiming`, `TryGetAim`), 시점(`IsFirstPerson`, `CanToggleView`, `ToggleView`). `GameUi`와 `BlockBuilder`가 이것을 씀.
- 입력 자산: `UI` 묶음(화면을 누르는 입력. 마우스 쪽은 입력 시스템의 기본 정의와 같고 VR은 오른손의 조준 자세와 방아쇠만), `Game/Menu`에 왼손의 메뉴 단추와 둘째 단추, `XR` 묶음에 오른손이 가리키는 자세.
- 눈앞의 판 `XrUiPanel`(`Scripts/UI`, 캔버스에 붙음): VR에서 캔버스를 월드 공간으로 바꿈. 메뉴가 닫혀 있으면 머리를 따라와 알림만 보이고(1m 앞), 메뉴가 열리면 그때의 눈앞 1.6m에 놓여 머묾. 앞이 막히면 벽 앞으로 당기고 크기를 거리에 비례해 줄임. 늘 보이는 화면(`Hud`)과 메뉴 뒤의 막(`Scrim`)은 VR에서 감춤.
- 손 광선: `XrRig`가 오른손이 가리키는 자세에 선과 끝의 작은 블록을 놓음(`ShowPointer`, `SetPointerLength`, `TryGetPointerRay`). 메뉴가 열려 있을 때만 보임. 누르는 일은 입력 시스템의 `TrackedDeviceRaycaster`와 `InputSystemUIInputModule`이 맡음(새 패키지 없음).
- 메뉴의 VR용 안내: 도움말은 VR 조작, 시점 타일은 "PC 전용"으로 누를 수 없음, 설정에 안내 문장, 키 딱지 감춤(`QuickMenuView.ShowControlMode`).
- 알림 띠를 `Hud` 밖(`Canvas/Notice`)으로 옮김. VR 시작 알림에 메뉴 여는 법을 더함.
- `Day12Setup`: 캐릭터 프리팹에 `LocalPlayer`와 오른손 광선(`XrOrigin/RightPointer`), 게임 화면 프리팹 다시 조립, 재질 `PointerRay`.

### Unity 13일차

- VR에서 만들기: 부품을 고른 뒤 오른손으로 가리켜 방아쇠로 놓기, 오른손 옆 단추로 지우기, 왼손 방아쇠로 칠하기. 입력 자산의 `XR` 묶음에 `Place`·`Remove`·`Paint`(PC의 `Player` 묶음과 같은 이름).
- `BlockBuilder`는 두 조작에서 모두 켜져 있고, 입력을 `LocalPlayer.InputMapName`이 알려 주는 묶음에서 찾음. 조작 방식이 바뀌면 다시 찾음. `PlayerModeSwitch`는 더는 블록 놓기를 켜고 끄지 않음.
- 방아쇠 하나로 누르기와 놓기: 오른손 광선이 화면(메뉴, 부품 판)에 닿아 있으면 `LocalPlayer.IsAiming`이 false라 블록이 놓이지 않음(`XrRig.IsPointerOverUi`).
- 부품 판 `XrHandPalette`(`Scripts/UI`): 왼손 위 14cm에 떠서 머리 쪽을 봄. 부품 칸(화면 아래의 것과 `HotbarView.Bind`로 같은 선택 상태), 고른 부품의 이름, 블록 수, 걷기·날기 표시, 되돌리기·다시 실행 단추. VR이고 메뉴가 닫혀 있고 왼손 컨트롤러가 있을 때만 보임.
- 오른손 광선은 VR에서 늘 보이고 `GameUi.UpdatePointer`가 길이를 정함: 화면에 닿으면 그 자리, 놓을 자리를 가리키면 그 자리, 할 일이 없으면 30cm. 끝의 블록은 거리에 비례.
- 되돌리기·다시 실행을 `GameUi.Undo`·`Redo`로 모음(키와 부품 판의 단추가 함께 씀). 메뉴 도움말의 VR 안내에 만들기 조작을 더함.
- `Day13Setup`: 게임 화면 프리팹 다시 조립, 캐릭터 프리팹 다시 잇기.

### 아직 동작하지 않는 것

- **VR은 실제 헤드셋으로 확인하지 못했습니다.** VR 조작(10일차), OpenXR 연결(11일차), VR 메뉴(12일차), VR에서 만들기(13일차)는 가상 기기와 VR 프로그램이 없는 PC로만 확인했습니다. 부품 판과 메뉴의 크기·자리는 그림만 보고 정했고, 판은 블록에 가릴 수 있습니다. 오른손으로 가리키고 왼손에 부품 판을 드는 것으로 고정되어 있습니다. VR에서는 저장 표시가 보이지 않고 놓을 때의 진동과 소리가 없습니다. Quest 단독 빌드도 없습니다(XR 설정은 PC용뿐).
- 다른 사람의 캐릭터는 아직 없습니다.
- 맵이 이 기기에 하나뿐입니다. 새 맵, 맵 목록, 이름 바꾸기가 없고 계정에는 저장되지 않습니다.
- 블록 옮기기·돌리기가 없습니다. 되돌리기와 칠하기는 블록 하나가 기록 하나이며 되돌리기의 게임패드 키가 없습니다.
- 날기 속도는 하나(7m/s)이고 벽을 통과하지 못합니다.
- 빌드는 Mono이며 아이콘·시작 화면은 Unity 기본값입니다. 실행 파일에서 사람이 직접 조작해 본 적은 없습니다.
- 알림은 마지막 하나만 보입니다. 저장 실패 알림은 실제 실패 상황에서 확인하지 않았습니다.
- 블록 수 상한 500개와 블록 하나를 게임 오브젝트 하나로 두는 방식은 임시입니다. Quest에서 재 본 뒤에 정합니다.
- 바로가기의 내 작업실·맵 둘러보기·캐릭터·안전·신고는 "준비 중"으로 표시되고 누를 수 없습니다.
- 사람들 목록과 이름표에는 자기 자신("손님")만 나옵니다. 로그인, 저장, 여러 사람 접속이 없습니다.
- 대화(채팅) 화면이 없습니다.

---
## 최근 커밋 기록

| 커밋 | 내용 |
| --- | --- |
| (이 문서가 든 커밋) | 13일차 : VR에서 만들기 |
| `3ce4915` | 12일차 : VR 메뉴 |
| `39d8d32` | 11일차 : OpenXR 연결 |
| `d95ae1b` | 10일차 : VR 조작과 추적 리그 |
| `12ac372` | 9일차 : 조작과 겉모습 나누기 |
| `0e5408f` | 8일차 : Windows 빌드 확인 |
| `838d9ed` | 7일차 : 날기와 걸어 보기 |
| `2335df9` | 6일차 : 블록 칠하기와 알림 띠 |
| `1d8dd2c` | 5일차 : 되돌리기와 다시 실행 |
| `5603bce` | 4일차 : 맵 데이터 형식과 저장·불러오기 |
| `dae7792` | 앞으로 구현할 요소 문서 추가 |
| `2afca66` | 3일차 : 블록 놓고 지우기 |
| `e91444b` | 2일차 : 블록 캐릭터, 1인칭·3인칭 시점, 게임 화면. 인수인계 문서와 검증용 사본 도구 포함 |
| `8268c63` | 1일차 : Unity 프로젝트 기반 구성과 PC 걷기 |
| `d72754c` | 구현 방식을 Unity 앱으로 바꾼 결정에 맞춰 문서와 화면 시안 문구 수정 |
| `7fababc` | 빠진 화면 여섯 개 추가와 탭 구조·누르기 크기 수정 |
| `dbf992a` | 점검 결과와 구현 단계 문서 추가 |
| `72e4d05` | Atelier \| Verse 화면 시안과 공통 구성요소 추가 |

---
## 사용자가 정한 것

모두 2026년 10월 8일까지의 결정입니다. 더 자세한 표는 `README.md`의 "시안에 반영한 결정"과 `docs/ROADMAP.md` 5절에 있습니다.

| 항목 | 결정 |
| --- | --- |
| 만드는 방식 | Unity 앱 하나로 PC(Windows)와 VR(Meta Quest)을 함께 지원. 같은 날 먼저 골랐던 브라우저(WebXR) 방식을 대체 |
| VR 기기 | Meta Quest. 사용자가 기기를 가지고 있음 |
| 조작의 기본 | PC의 키보드·마우스가 기본. VRChat처럼 VR 기기가 있는 사람은 추적과 장비로 할 수 있는 기능이 되게 함(2026-10-08). "VR에서는 구경과 이동만"이라는 앞의 기본안을 대체 |
| Unity 프로젝트 위치 | 이 저장소의 `unity/` 폴더 |
| 진행 방식 | Project-Mu와 같게: 일차별 일지, "N일차 : 내용" 커밋, 에디터를 열면 적용되는 일차별 셋업 스크립트 |
| 게임 화면 | 로블록스와 VRChat을 섞음 |
| 방 인원 | 최대 8명 |
| 그래픽 | 블록·로우폴리 |
| 캐릭터 | 기본 캐릭터 가운데 고르기 |
| 대상 연령 | 청소년 이상(만 14세 이상) |
| 계정 | 홈페이지·Mate \| Verse와 같은 통합 계정. 서비스마다 데이터베이스는 따로 두고 로그인만 홈페이지로 통합 |
| 초대 | 링크(앱을 엶)와 방 코드 |
| 개발 조건 | 1인, 주 10시간 이하 |

Claude가 판단해서 정한 것도 있습니다. 사용자가 바꾸자고 하면 바꿉니다.

- 게임 화면은 uGUI와 TextMesh Pro로 만듭니다. VR에서 같은 화면을 눈앞에 띄우는 방법이 가장 널리 검증되어 있기 때문입니다.
- 시점은 기본 1인칭, 휠로 3인칭입니다.
- 글꼴은 임시로 나눔고딕입니다(아래 "사용자 결정이 필요한 항목").

---
## 구조상 꼭 알아야 할 부분

### Unity 폴더

```
unity/Assets/_Project/
  Art/Fonts/        NanumGothic.ttf, 라이선스 전문, NanumGothic SDF(글꼴 자산)
  Art/Materials/    GridFloor, Block_Gold·Blue·Clay·Leaf·Ivory·Wood·Ink, Block_Ghost(미리 보기), PointerRay(오른손 광선)
  Art/Textures/     Grid.png
  Art/UI/           Rounded, RoundedLine, Icon_*.png (셋업이 그린 그림)
  Data/             PartCatalog.asset (부품 목록)
  Editor/           Day1Setup ~ Day13Setup, GameUiBuilder, UiFactory, PlayerWiring, ProjectSetupRunner, DynamicFontGuard, DevCapture, BuildPlayer, XrBootConfig, XrEditorMenu
  Input/            AtelierInput.inputactions (Player 맵, Game 맵, XR 맵, UI 맵)
  Prefabs/          Player_Desktop, GameUI, Block, BlockGhost
  Scenes/           Boot, Sandbox
  Scripts/Core/     AtelierPalette, GameSettings, RoomRules, AppExit, Bootstrap, Notice(+NoticeModel), XrSession
  Scripts/Player/   CharacterMotor, ViewRig, DesktopPlayerController, XrRig, XrPlayerController, PlayerModeSwitch, LocalPlayer, AvatarView, Nameplate, BlockBuilder
  Scripts/UI/       GameUi, HotbarModel, HotbarView, QuickMenuView, SettingsPanel, PeopleListView, NoticeBar, XrUiPanel, XrHandPalette
  Scripts/World/    GridMath, BlockMap, BlockWorld, PlacedBlock, PartCatalog, MapDocument, MapStorage, MapAutoSave, EditHistory
  Tests/EditMode/   GridMathTests, HotbarModelTests, ViewAndSettingsTests, BlockMapTests, MapDocumentTests, EditHistoryTests, NoticeModelTests, FlyMoveTests, BuildPlayerTests, BootstrapArgsTests, ControlModeTests, XrSessionTests, XrBootConfigTests, XrUiPanelTests, UiInputTests, XrHandPaletteTests
  Tests/PlayMode/   PlayTestBase, SandboxPlayTests, GameUiPlayTests, BuildPlayTests, SavePlayTests, UndoPlayTests, PaintPlayTests, FlyPlayTests, VersionPlayTests, MotorPlayTests, XrPlayTestBase, XrPlayTests, XrMenuPlayTests, XrBuildPlayTests
unity/Assets/TextMesh Pro/   TextMesh Pro 기본 자산(Unity가 넣은 것)
unity/Assets/Settings/       URP 설정(PC용, 모바일용)
unity/Assets/XR/             XR 설정(OpenXR 로더, 컨트롤러 프로파일). 11일차 셋업이 만든 것
```

### 일차별 셋업

- 프리팹·씬·재질·그림은 손으로 만들지 않고 `Editor/DayNSetup.cs`가 만듭니다. 여러 번 실행해도 결과가 같게 작성합니다.
- `ProjectSetupRunner`가 에디터를 열 때 아직 적용하지 않은 일차를 실행하고 `ProjectSettings/AtelierVerseSetupState.txt`에 기록합니다. 저장하지 않은 씬이 있으면 먼저 묻습니다. 명령줄에서는 `RunFromCommandLine`을 부릅니다.
- 새 일차를 만들면 `ProjectSetupRunner`의 `Steps`에 한 줄을 더하고, 셋업을 적용한 결과(프리팹·씬 등)를 함께 커밋합니다.
- 화면을 고칠 때는 프리팹을 직접 고치지 말고 `GameUiBuilder`·`UiFactory`를 고친 뒤 가장 최근 일차의 셋업을 다시 적용합니다. 직접 고치면 다음 셋업에서 덮어씁니다. 화면에 요소가 늘면 `GameUiBuilder`에 더하고 그 일차의 셋업이 다시 조립합니다.
- 셋업 안에서 씬을 새로 열면(`OpenScene`) 그 전에 만들어 변수에 들고 있던 자산 참조가 끊길 수 있습니다. 씬을 연 뒤에 `AssetDatabase.LoadAssetAtPath`로 다시 불러옵니다(3일차에 겪음).

### 블록

- 맵의 블록은 `BlockMap`(칸 → 부품 번호)이 기록하고 `BlockWorld`가 화면의 블록과 맞춥니다. 블록을 놓거나 지울 때는 `BlockWorld.Place`·`Remove`만 씁니다. 씬에 블록을 직접 놓으면 기록에 오르지 않습니다.
- 씬에 미리 놓는 블록은 `BlockWorld`의 자식으로 두고 `PlacedBlock`에 칸과 부품 번호를 적습니다. 시작할 때 기록에 오릅니다.
- 부품 목록(`PartCatalog`)의 순서가 부품 칸의 순서이고 부품 번호입니다. 부품의 `id`(`block.gold` 등)는 저장에 쓸 이름이라 바꾸지 않습니다.
- `BlockBuilder`는 화면 가운데에서 광선을 쏘아 닿은 면의 바깥쪽 칸에 놓습니다. 캐릭터는 `Ignore Raycast` 층이라 광선에 걸리지 않고, 겹침 검사(`CheckBox`)에는 걸려서 자기가 선 칸에는 놓이지 않습니다.
- 부품을 고르거나 마우스를 잡은 바로 그 프레임에는 놓지 않습니다(한 프레임 기다림). 마우스를 잡는 누름으로 블록이 놓이는 것을 막기 위해서입니다.

### 되돌리기

- 이용자의 편집은 `BlockWorld.History`(`EditHistory`)를 거칩니다. `BlockWorld.Place`·`Remove`는 기록하지 않는 바탕 동작이라 직접 부르면 되돌릴 수 없습니다. 새 도구(칠하기, 옮기기)는 `EditHistory`에 동작을 더해 같은 기록을 쓰게 합니다.
- 기록은 "칸, 이전 부품, 이후 부품" 하나입니다. 되돌릴 때 칸이 이미 그 상태면 성공으로 보고, 상한이 가득 차 놓지 못하면 기록을 남긴 채 실패합니다.
- 파일에서 불러오거나(`Import`) 모두 지우면(`Clear`) 기록이 비워집니다.
- 키는 입력 자산의 `Game` 맵에 OneModifier 묶음(`<Keyboard>/ctrl` + `z`, `y`)으로 들어 있습니다. 테스트에서는 `TapWithCtrl`로 Ctrl을 먼저 누릅니다. Ctrl+Shift+Z를 Redo에 더하면 Ctrl+Z와 함께 울리므로 넣지 않았습니다.
- 칠하기는 `EditHistory.Replace`(이전 부품 → 새 부품)로 기록합니다. 되돌릴 때는 `IBlockStore.Replace`로 부품만 바꾸고 블록을 지우고 다시 만들지 않습니다.

### 알림 띠

- 알림은 `Notice.Post(문구, NoticeKind)`로 올립니다. 화면을 모르는 코드(`BlockBuilder`, `MapAutoSave`, `GameUi`)가 올리고 `NoticeBar`가 받습니다. 새 알림은 올려야 할 곳에서 `Notice.Post`만 부르면 됩니다.
- `NoticeBar`는 `GameUI` 프리팹의 `Hud/Notice`에 있고 띠(`Bar`)는 평소 꺼져 있습니다. 보이는 시간은 `NoticeModel`이 `Time.unscaledTime`으로 셉니다(메뉴로 시간을 멈춰도 사라짐).
- 띠의 너비는 실행 중에 글자 너비로 맞추므로, 문구를 길게 써도 잘리지 않지만 화면 폭(1920 기준)을 넘지 않게 씁니다.
- 테스트 틀: 앞 테스트의 씬에 남은 편집이 다음 테스트로 새지 않도록 `PlayTestBase.TearDown`이 남은 변경을 저장합니다. 새 플레이 모드 테스트 클래스는 반드시 `PlayTestBase`를 상속합니다(`SandboxPlayTests`가 상속하지 않아 6일차에 걷기 테스트가 앞 테스트의 블록에 막힌 적이 있음).

### 맵 저장

- 파일 형식은 `docs/MAP-FORMAT.md`가 기준입니다. 항목을 더하거나 바꾸면 `MapDocument.CurrentVersion`을 올리고 `Upgrade`에 옛 판을 바꾸는 단계를 더한 뒤 문서를 고칩니다. 부품의 `id`를 바꾸면 저장된 파일을 읽지 못하므로 바꾸지 않습니다.
- `MapAutoSave`는 Sandbox 씬의 `BlockWorld` 오브젝트에 붙어 있습니다. 블록은 `BlockWorld.Place`·`Remove`로만 바꿔야 `Changed`가 울리고 저장이 예약됩니다.
- 저장 폴더는 `MapStorage.Directory`로 바꿀 수 있습니다. 플레이 모드 테스트는 `PlayTestBase`가 테스트마다 임시 폴더로 바꿔 이 기기의 실제 파일을 건드리지 않습니다. 씬을 열기 전에 맵 파일을 미리 써 두는 테스트는 `PrepareMapDirectory()`를 먼저 불러야 합니다(앞 테스트의 씬이 내려가며 저장하는 시점 때문).
- `MapAutoSave`는 씬이 내려갈 때(`OnDisable`) 남은 변경을 저장합니다. 테스트에서 씬을 다시 열 때 앞 씬의 블록이 따라오는 것처럼 보이면 이 때문입니다.

### 캐릭터와 화면의 연결

- 캐릭터는 세 스크립트로 나뉩니다(9일차). 몸은 `CharacterMotor`, PC 카메라는 `ViewRig`, 키보드·마우스는 `DesktopPlayerController`입니다. 조작은 매 프레임 몸의 의도(`MoveDirection`, `Sprint`, `Ascend`, `Descend`, `Jump()`)를 채우고, 몸은 그 의도대로만 움직입니다. 조작이 막히거나 꺼지면 `Motor.Stop()`을 부릅니다.
- 몸은 `DefaultExecutionOrder(50)`으로 다른 스크립트보다 늦게 움직입니다. 조작이 같은 프레임에 의도를 먼저 정하게 하기 위해서이며, 이 순서를 바꾸면 조작이 한 프레임 늦습니다.
- VR 조작은 `XrPlayerController`이고 PC 조작과 같은 몸을 씁니다. 새 조작(다른 사람의 캐릭터 등)도 `DesktopPlayerController`를 고치지 않고 `CharacterMotor`의 의도를 채우는 스크립트를 따로 만듭니다. `ViewRig`는 PC 전용이고 VR의 카메라는 `XrRig`가 맡습니다.
- 어느 조작을 켤지는 `PlayerModeSwitch`만 정합니다. 다른 스크립트가 조작 스크립트의 `enabled`를 직접 바꾸지 않습니다. 프리팹에는 VR 쪽이 꺼진 채로 저장되어 있습니다.
- `XrRig`는 카메라의 부모를 `XrOrigin`으로 바꿉니다. 카메라의 부모를 `CameraPivot`으로 가정하는 코드는 PC 조작일 때만 맞습니다. 카메라가 필요하면 `player.Rig.ViewCamera`를 쓰면 두 조작에서 같은 카메라입니다.
- 꺼져 있는 입력 묶음의 동작에 `controls`를 묻지 않습니다. 물으면 입력 시스템이 연결을 새로 계산해 상태를 만들어 두는데, 테스트 사이에 이것이 일어나면 다음 테스트의 입력이 전부 닿지 않습니다(10일차에 겪음). `action.enabled`를 먼저 봅니다.
- VR 테스트는 가상 기기를 씁니다. `InputSystem.AddDevice<XRHMD>()`와, `XRController`를 넓힌 시험용 배치(스틱·단추에 공통 쓰임새를 붙인 JSON)를 `InputSystem.RegisterLayout`으로 등록해 붙입니다(`XrPlayTests`). 씬을 열기 전에 `PlayerModeSwitch.Forced = ControlMode.Vr`를 넣고, `PlayTestBase`가 테스트마다 null로 돌려놓습니다.
- **화면과 도구는 조작 스크립트를 직접 알지 않고 `LocalPlayer`를 거칩니다**(12일차). `GameUi.Player`와 `BlockBuilder`의 `player`는 `LocalPlayer`입니다. 조작 방식에 따라 달라지는 것(조작 막기, 조준, 시점)은 `LocalPlayer`에 더하고, `GameUi`나 도구에서 `IsVr`로 갈라 쓰는 곳을 늘리지 않습니다. `LocalPlayer`는 같은 뿌리의 조작과 리그를 처음 쓸 때 찾으므로 프리팹에서 이을 것이 없습니다.
- 도구가 읽는 입력은 `LocalPlayer.InputMapName`의 묶음에서 찾습니다(PC는 `Player`, VR은 `XR`). 새 도구 동작은 두 묶음에 **같은 이름으로** 넣습니다(`UiInputTests`가 `Place`·`Remove`·`Paint`를 검사). `PlayerModeSwitch`는 도구를 켜고 끄지 않으며, 도구는 `IsAiming`과 `InputBlocked`로 지금 쓸 수 있는지 판단합니다.
- VR의 오른손 방아쇠는 화면 누르기(`UI/Click`)와 블록 놓기(`XR/Place`)에 함께 묶여 있습니다. 광선이 화면에 닿아 있으면 `LocalPlayer.IsAiming`이 false가 되어 도구가 쉬고, 다시 조준이 되어도 한 프레임을 기다린 뒤에 놓습니다(`BlockBuilder`의 `wasActive`). 이 순서를 바꾸면 부품 판을 누른 방아쇠로 블록이 놓입니다(`XrBuildPlayTests`가 검사).
- 테스트의 `player`(`PlayTestBase`)는 지금도 `DesktopPlayerController`입니다. 몸은 `player.Motor`(날기, 시작 위치로, 속도, 겉모습), PC 카메라는 `player.Rig`(카메라, 머리 위치, 1인칭 여부, 시점 거리)에서 씁니다. `DesktopPlayerController`에는 `InputBlocked`, `LookCaptured`, `SetInputBlocked`, `CaptureLook`, `SetLook`만 남았습니다. `Motor`와 `Rig`는 처음 쓸 때 찾으므로 다른 스크립트의 `OnEnable`에서 써도 됩니다.
- 캐릭터 프리팹의 구성을 바꿀 때는 `Editor/PlayerWiring.cs`를 고칩니다. `Apply`(몸·PC 카메라·PC 조작)는 1·2·9일차, `ApplyXr`(VR 리그·VR 조작·조작 방식 고르기)는 10일차, `ApplyLocal`(`LocalPlayer`와 오른손 광선)은 12일차 셋업이 씁니다.
- `DesktopPlayerController`는 마우스를 잡았을 때만(`LookCaptured`) 시점을 돌립니다. `Cursor.lockState`는 창 없는 실행에서 믿을 수 없어 자체 상태로 판단합니다.
- 걷기와 날기는 `CharacterMotor`의 두 갈래이며 `IsFlying`으로 나뉩니다. 날 때는 Jump·Sprint 동작이 위·아래가 됩니다. 이동 코드를 고치면 1일차 걷기 테스트(`SandboxPlayTests`), 7일차 날기 테스트, 9일차 `MotorPlayTests`가 함께 통과해야 합니다.
- 메뉴가 열리면 `GameUi`가 `LocalPlayer.SetInputBlocked(true)`로 두 조작을 함께 막고, 닫으면 `ResumeControl()`로 풉니다(PC에서는 마우스를 다시 잡음). 막힌 채로 조작 방식이 바뀌어도 그대로 막혀 있습니다.
- 화면의 단추는 마우스와 VR의 오른손 광선으로만 누릅니다. `EventSystem`의 키보드 이동(`sendNavigationEvents`)을 꺼 두었습니다. 켜면 Space나 WASD가 단추를 누릅니다.
- 꾸미기용 그림과 글자는 `raycastTarget`을 꺼 두었습니다. 켜져 있으면 그 위를 눌렀을 때 마우스 잡기가 되지 않습니다.
- 게임 끝내기는 `AppExit.Request()`로 모입니다. 테스트는 `AppExit.Handler`를 바꿔 실제로 끝나지 않게 합니다.

### VR의 게임 화면

- 화면은 하나(`GameUI` 프리팹)입니다. VR용 화면을 따로 만들지 않고, `XrUiPanel`이 같은 캔버스를 월드 공간으로 바꿔 눈앞의 판으로 띄웁니다. 조작 방식에 맞춰 놓는 일은 `GameUi.ApplyControlMode`가 시작할 때와 조작 방식이 바뀔 때 합니다.
- VR에서 감출 것은 `XrUiPanel`의 `screenOnly`(지금은 `Hud`와 메뉴의 `Scrim`)에 넣습니다. VR에서도 보여야 하는 것은 `Hud` 밖(캔버스 바로 아래)에 둡니다. 알림 띠가 그렇게 `Canvas/Notice`에 있습니다.
- 메뉴 안에서 한쪽 조작에만 맞는 안내는 `GameUiBuilder`에서 `DesktopOnly`·`VrOnly` 목록에 넣습니다. `QuickMenuView.ShowControlMode`가 켜고 끕니다.
- 화면을 누르는 입력은 `AtelierInput.inputactions`의 `UI` 묶음입니다. 프리팹의 `InputSystemUIInputModule`은 자산 안의 동작 참조를 가리켜야 저장됩니다(`GameUiBuilder.SetAction`). `UI` 묶음의 동작은 그 모듈이 켜고 끄므로 다른 스크립트가 켜고 끄지 않습니다.
- 보이는 광선(`XrRig`, `XR` 묶음의 `RightPointerPosition`·`RightPointerRotation`)과 누르는 광선(화면 입력, `UI` 묶음의 `TrackedDevicePosition`·`TrackedDeviceOrientation`)은 같은 연결 경로를 써야 어긋나지 않습니다(`UiInputTests`가 검사). 컨트롤러의 자세는 추적 공간 기준이라 `inputModule.xrTrackingOrigin`에 `XrOrigin`을 넣습니다(`XrUiPanel.ShowInWorld`).
- `TrackedDeviceRaycaster`는 `canvas.worldCamera`가 있어야 동작하고, `raycastTarget`을 보지 않고 캔버스의 모든 그림에 닿습니다. 닿은 것의 부모에서 누를 것을 찾으므로 글자 위를 가리켜도 단추가 눌립니다. 캔버스 전체를 덮는 그림을 VR에서 켜 두면 그 뒤의 것이 눌리지 않습니다(메뉴의 `Scrim`을 VR에서 감추는 까닭 가운데 하나).
- 판의 크기와 거리는 `XrUiPanel`의 직렬화 값입니다(`metersPerPixel` 0.0013, `menuDistance` 1.6, `followDistance` 1). 스크립트의 기본값을 바꾸면 가장 최근 일차의 셋업을 다시 적용해야 프리팹에 들어갑니다. 실제 헤드셋에서 보고 맞춰야 하는 값입니다.
- 부품 판은 `GameUI/HandPalette`(늘 켜진 바깥 오브젝트, `XrHandPalette`가 붙음) 아래의 `PaletteCanvas`(월드 공간의 작은 캔버스, 자체 `TrackedDeviceRaycaster`)입니다. 보이고 감추는 것은 안쪽의 `PaletteCanvas`입니다. 무엇을 보일지는 `GameUi`가 알려 줍니다(`ShowTitle`, `ShowBlockCount`, `ShowMode`). PC의 화면 아래에 있는 정보를 VR에서도 보이려면 부품 판에 더하고 `GameUi`가 두 곳에 함께 알리게 합니다.
- 부품 칸이 두 곳에 있습니다(화면 아래, 부품 판). 선택 상태는 `HotbarView.Bind`로 하나의 `HotbarModel`을 함께 씁니다. 칸은 `GameUiBuilder.AddSlots`가 양쪽에 만듭니다. 화면의 요소 이름이 겹치면 테스트의 `Find`가 앞의 것을 찾으므로 부품 판의 요소는 `Palette`로 시작하는 이름을 씁니다.
- 오른손 광선은 VR에서 늘 보이고 길이는 `GameUi.UpdatePointer`가 매 프레임 정합니다. 광선 끝의 블록은 `XrRig.SetPointerLength`가 거리에 비례해 키웁니다.
- VR 테스트는 `XrPlayTestBase`를 상속합니다. `LoadVr`, `OpenMenuWithController`, `PointRightHandAt(월드의 한 점)`, `PullTrigger`, `CenterOf(화면 요소)`가 있습니다. 판이 떠 있으면 `BeginCapture`는 캔버스를 건드리지 않고 그대로 찍습니다. 글자를 확인하는 그림은 카메라의 시야각을 줄여 찍습니다(`XrMenuPlayTests`의 그림 찍기).

- 플레이 모드 테스트에서 `WaitForFixedUpdate` 바로 다음에 가상 기기에 값을 넣으면(`Set`, `Press`) "does not have an associated state" 예외가 납니다. 물리 갱신 시점에는 입력 상태를 쓸 수 없기 때문입니다. `Frames(2)`로 프레임을 넘긴 뒤에 넣습니다(13일차에 겪음).
- `Vector3.Angle`은 0도 근처에서 0.03도쯤의 오차가 납니다. 방향을 검사할 때의 허용치는 0.1도 이상으로 둡니다.

### VR 화면(OpenXR)

- VR 화면은 `XrSession`만 켜고 끕니다. XR 설정의 "시작할 때 XR 켜기"(`InitManagerOnStart`)는 꺼 둡니다. 켜면 VR 프로그램이 깔린 PC에서 실행할 때마다 헤드셋 프로그램이 뜹니다(`XrSessionTests`가 검사).
- VR로 시작하는 길은 실행 인자 `-vr`(실행 파일 옆의 `AtelierVerse-VR.bat`이 붙여 줌)과 에디터 메뉴 `Atelier Verse/재생할 때 VR 켜기`입니다. 메뉴 설정은 이 컴퓨터의 에디터 설정에만 저장되고, 명령줄(창 없는) 실행에서는 무시합니다.
- `XrSession`은 첫 씬이 열리기 전에 켭니다. 그래야 `PlayerModeSwitch`가 처음부터 VR 조작을 고릅니다. 켜지 못하면 안내 문구를 남기고, `PlayerModeSwitch.Start`가 알림 띠에 올립니다.
- 빌드가 끝나면 `XrBootConfig`가 `<이름>_Data/boot.config`에서 `xrsdk-pre-init-library` 줄을 뺍니다. 이 줄이 있으면 `-vr` 없이 켜도 화면이 뜨기 전에 OpenXR 런타임을 찾아갑니다(11일차에 로그로 확인). `build-windows.mjs`가 이 줄이 남았는지와 평소 실행의 로그를 검사합니다.
- **그 항목을 실행 인자로 주는 명령을 실행하지 않습니다.** Microsoft Defender가 `Exploit:Win32/CVE-2025-59489`(Unity의 보안 문제)로 보고 실행을 막으며 사용자 PC에 경보 기록이 남습니다. 셸 명령의 글자에 그 인자 모양이 들어 있기만 해도 셸 실행이 거부됩니다(11일차에 겪음). 문서와 코드에 항목 이름을 적는 것은 괜찮습니다.
- OpenXR 런타임이 없는 PC에서 `-vr`로 켜면 "VR 기기를 찾지 못해 키보드·마우스로 시작합니다"로 돌아옵니다. 이 컴퓨터가 그런 PC입니다.
- 컨트롤러 프로파일을 더하려면 `Day11Setup`의 `ControllerProfiles`에 넣고 셋업을 다시 적용합니다. XR 설정 자산(`Assets/XR`)을 손으로 고치지 않습니다.
- XR 설정은 PC(Standalone)용뿐입니다. Quest 단독 빌드를 할 때 Android용 설정을 셋업에 더합니다.

### 글꼴

- 글꼴 자산은 필요한 글자를 쓸 때마다 채우는 방식(Dynamic)입니다. 이용자가 입력한 이름도 표시하기 위해서입니다.
- 채워진 글자 그림이 저장되면 자산이 수 MB로 커집니다. `DynamicFontGuard`가 저장 직전에 비웁니다. 커밋 전에 `NanumGothic SDF.asset`이 수 KB인지 확인합니다.
- Noto Sans KR 가변 글꼴은 Unity에서 가장 가는 굵기로만 나옵니다. 가변 글꼴은 쓰지 않습니다.

### 알려진 함정

- `CharacterController`의 최소 이동 거리 기본값(0.001) 때문에 초당 프레임 수가 아주 높으면 캐릭터가 움직이지 않습니다. 프리팹에서 0으로 두었습니다.
- 창 없이 실행하면 초당 수천 프레임으로 돕니다. 시간에 기대는 플레이 모드 테스트는 `Application.targetFrameRate`를 60으로 맞춥니다.
- `InputTestFixture`로 넣은 키는 다음 프레임에 반영됩니다. 누르고 두 프레임 뒤에 떼는 `Tap` 도우미를 씁니다.
- 테스트는 개인 설정(`GameSettings`)을 바꾸므로 시작할 때 값을 저장하고 끝날 때 되돌립니다.
- 화면에 겹쳐 그리는 캔버스는 카메라 그림에 찍히지 않습니다. 화면 그림은 `GameUiPlayTests`의 `화면_그림을_찍는다`가 캔버스를 잠시 카메라 앞에 붙여서 찍습니다.

---
## 검증 방법

에디터 창 없이 명령줄로 검증합니다. 아래에서 `Unity.exe`는 설치된 에디터의 실행 파일이고 `<프로젝트>`는 `unity` 폴더의 전체 경로입니다.

```bash
# 편집 모드 테스트 (-quit을 붙이지 않는다)
Unity.exe -batchmode -projectPath <프로젝트> -runTests -testPlatform EditMode -testResults <결과.xml> -logFile <로그>

# 플레이 모드 테스트. -captureDir을 주면 화면 그림 23장도 찍는다(2~13일차의 그림이 한 폴더에 나오므로 필요한 것만 일지에 남긴다)
Unity.exe -batchmode -projectPath <프로젝트> -runTests -testPlatform PlayMode -captureDir <그림 폴더> -testResults <결과.xml> -logFile <로그>

# 아직 적용하지 않은 일차의 셋업 적용
Unity.exe -batchmode -quit -projectPath <프로젝트> -executeMethod AtelierVerse.EditorTools.ProjectSetupRunner.RunFromCommandLine -logFile <로그>

# 특정 일차의 셋업 다시 적용
Unity.exe -batchmode -quit -projectPath <프로젝트> -executeMethod AtelierVerse.EditorTools.Day2Setup.Apply -logFile <로그>

# Windows 빌드와 자동 실행 확인(저장소 폴더에서). 빌드만 하려면 --run을 뺀다
node scripts/build-windows.mjs --run

# 이미 만든 실행 파일을 -vr로 켜서 확인(VR 프로그램이 없는 PC에서는 키보드·마우스로 돌아오는지를 본다)
node scripts/build-windows.mjs --skip-build --run --vr
```

- 결과는 `<결과.xml>`의 `test-run` 줄(통과·실패 수)과 로그의 `error CS`로 확인합니다.
- 한 번 실행에 1~2분 걸립니다. 백그라운드로 실행하고 끝나기를 기다립니다. PowerShell에서 `Start-Process -Wait`는 Unity가 띄운 라이선스 클라이언트까지 기다려 10분 넘게 걸릴 수 있으므로, `-PassThru`로 받은 프로세스에 `WaitForExit()`를 씁니다.
- 패키지를 처음 받을 때 "Cancelled resolving packages"로 한 번 실패한 적이 있습니다. 다시 실행하면 됩니다.
- 화면 그림은 반드시 열어서 눈으로 확인합니다.

### 에디터가 열려 있을 때

사용자가 Unity 에디터로 프로젝트를 열어 두면 같은 폴더에서는 "another Unity instance is running"으로 실패합니다. 에디터를 끄지 않고 검증용 사본에서 진행합니다.

```bash
node scripts/unity-verify.mjs prepare --hold Day04   # 사본(.verify/unity) 만들기. Day04는 검증 전이라 미뤄 둘 일차
# 사본을 <프로젝트>로 삼아 위의 명령으로 셋업과 테스트를 실행
node scripts/unity-verify.mjs to-verify              # 소스를 고쳤으면 실제 → 사본으로 보냄
node scripts/unity-verify.mjs to-real --dry          # 가져올 자산 미리 보기
node scripts/unity-verify.mjs to-real                # 셋업이 만든 자산을 사본 → 실제로 가져옴
node scripts/unity-verify.mjs clean                  # 끝나면 사본 지우기
```

- 소스(`.cs` 등)는 항상 실제 프로젝트에서 고칩니다. 사본에서는 고치지 않습니다.
- `--hold`는 실제 프로젝트의 적용 기록에 그 일차를 미리 적어, 열려 있는 에디터에서 검증 전의 셋업이 자동으로 돌지 않게 합니다. 사본의 기록에서는 빼서 사본에서 셋업이 돌게 합니다.
- 스크립트의 `.meta`(식별자)는 실제 프로젝트의 것이 기준입니다. 프리팹이 스크립트를 식별자로 가리키기 때문입니다. `to-verify`는 새 스크립트에 `.meta`가 없으면 실제 프로젝트에 먼저 만들어 양쪽이 같은 식별자를 쓰게 하고, 실제 프로젝트에서 지우거나 이름을 바꾼 소스는 사본에서도 지웁니다.
- 새 일차의 코드를 쓰기 전에 `prepare --hold`부터 실행합니다. 그래야 열려 있는 에디터가 검증 전의 셋업을 자동으로 돌리지 않습니다.
- 사본은 처음에 `Library`를 새로 만드느라 5~7분 걸리고 2GB쯤 차지합니다. 끝나면 지웁니다.
- 사본은 저장소 바로 아래에 둡니다. 경로가 길면 Unity가 패키지 파일을 열지 못합니다.

### 명령줄로 확인할 수 없는 것

실제 마우스 포인터로 단추가 눌리는지, 마우스 시점의 느낌, 점프, 실행 파일에서의 직접 조작, 헤드셋 안의 화면과 컨트롤러, Quest에서의 실행입니다. Windows 빌드는 `build-windows.mjs --run`이 "열리고, 저장하고, 끝난다"까지만 확인합니다. 확인하지 않은 것은 확인하지 않았다고 일지와 보고에 적습니다.

---
## 사용자 결정이 필요한 항목

물었으나 아직 답을 받지 못한 것입니다.

1. **글꼴**: 임시로 나눔고딕을 넣었습니다. 웹 시안과 같은 Pretendard로 맞추려면 글꼴 파일을 내려받아야 하며, 내려받아도 되는지 물어 둔 상태입니다. 바꿀 때는 `Art/Fonts`의 파일과 `Day2Setup`의 `FontPath`·`FontAssetPath`를 바꾸고 셋업을 다시 적용합니다.
2. **사업 계획서 갱신**: 12.3.1의 VR 문장, 12.4.1의 "VR 입장(WebXR)", 12.6.1의 플랫폼 방식이 아직 WebXR로 적혀 있고, 12.10의 화면 표는 처음 11개만 있습니다. Unity 방식과 화면 17개로 고칠지 물어 둔 상태입니다.
3. **홈페이지 소개 페이지**: VR 문구가 아직 "검토하고 있습니다"입니다. 고칠지 물어 둔 상태입니다(홈페이지 저장소 작업).
4. **`docs/ROADMAP.md` 5절의 질문 세 가지**: 내 작업실·둘러보기·소식을 앱에 둘지 웹에 둘지, 1단계가 끝날 때까지 뒤 단계 기능을 만들지 않을지, 비공개 시험의 판단 기준.

사용자가 직접 해야 하는 것입니다.

- 헤드셋으로 확인하기: Meta Quest Link 또는 SteamVR을 설치해 헤드셋을 PC에 잇고, `unity/Builds/Windows/AtelierVerse-VR.bat`을 열어 화면·두 손·스틱 이동·눈높이, 왼손 메뉴 단추로 뜨는 메뉴(글자가 읽히는지, 판의 거리와 크기, 광선의 방향, 방아쇠로 눌리는지), 왼손 위의 부품 판과 블록 놓기·지우기·칠하기(판의 크기와 자리, 가리킨 곳에 놓이는지)를 확인(방법은 `unity/Devlogs`의 `Day11`·`Day12`·`Day13` 일지에 있는 "직접 확인하는 방법"). 이 컴퓨터에는 OpenXR 런타임이 없음
- Unity Hub에서 `6000.3.21f1`에 Android Build Support 설치(Quest 단독 빌드 전)
- Meta 개발자 계정 등록과 Quest의 개발자 모드 켜기
- 에디터에서 Sandbox 씬을 실행해 걷기와 메뉴, 블록 놓기, 가운데 단추·F 칠하기, Ctrl+Z 되돌리기, 알림 띠, V 날기, 껐다 켰을 때 블록이 남는지 직접 확인
- `unity/Builds/Windows/AtelierVerse.exe`를 직접 열어 창 크기, 조작, 메뉴의 "게임 끝내기"를 확인
- 통합 계정의 실제 연결(홈페이지 저장소의 `docs/UNIFIED-ACCOUNT.md`)

---
## 다음 작업

순서와 끝난 기준은 `docs/ROADMAP.md` 3절에, 남은 구현 요소의 전체 목록은 `docs/BACKLOG.md`에 있습니다. 지금은 0단계입니다. 다음 일차를 고를 때는 `docs/BACKLOG.md` 2절 "가장 먼저 할 것"을 위에서부터 봅니다.

1. **헤드셋으로 켜 본 결과가 있으면 그것부터.** 사용자가 `AtelierVerse-VR.bat`으로 켜 본 결과(화면이 나오는지, 두 손, 눈높이, 스틱, 메뉴의 글자와 판의 거리·크기, 광선의 방향, 부품 판의 크기와 자리, 가리킨 곳에 블록이 놓이는지)를 받아 고칩니다. VR 화면이 켜지지 않으면 실행 로그(`%USERPROFILE%\AppData\LocalLow\Palettra Games\Atelier Verse\Player.log`)의 `[XR]` 줄을 봅니다. 그래픽 장치가 둘인 PC에서만 켜지지 않으면 사전 초기화를 남긴 VR 전용 빌드를 검토합니다(`docs/BACKLOG.md` 3.4).
2. **14일차 후보 가: 모양이 다른 부품과 부품 고르는 창.** 반 블록·경사·기둥·계단, 방향(`facing`)과 돌리기, 부품이 아홉 칸을 넘을 때의 고르는 창. 부품이 차지하는 칸이 한 칸을 넘으면 맵 형식의 판을 올립니다(`docs/MAP-FORMAT.md` 5절). 부품 고르는 창은 PC의 화면과 VR의 부품 판 양쪽에서 열 수 있어야 하고, 방향을 돌리는 입력도 두 조작의 묶음에 같은 이름으로 넣습니다.
3. **VR의 나머지.** 부품 판 다듬기(크기, 손 위의 자리, 손목에 붙이기나 놓아두기), VR 메뉴와 부품 판을 늘 위에 그리기(지금은 블록에 가릴 수 있음), 판의 크기와 거리 설정, 주로 쓰는 손 고르기, 놓을 때의 진동과 소리, 순간 이동과 부드럽게 돌기, 시야 좁히기. Quest 단독 빌드는 Android Build Support가 설치된 뒤입니다(`C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Data\PlaybackEngines`에 `AndroidPlayer` 폴더가 있는지 확인). 그때 Android용 XR 설정을 셋업에 더합니다.
4. 그 뒤는 1-A(혼자 만들기)의 나머지: 여러 맵 다루기, 시작 위치와 맵 정보, 부품 수 상한 정하기(Quest에서 재기).

화면 시안 쪽의 다음 작업은 `docs/PAGES.md` 6절에 있습니다.

---
## 작업 원칙

- 응답은 한국어로 합니다.
- 커밋과 PR에 `Co-Authored-By: Claude` 같은 공동 작성자 줄을 넣지 않습니다.
- `main`에서 작업하고 `main`으로 푸시합니다.
- **Unity 일차 작업의 커밋**: 제목은 `N일차 : 내용`, 본문은 `- ~추가`, `- ~수정`처럼 한 줄씩 적은 목록입니다.
- **그 밖의 커밋**(화면 시안, 문서): 제목은 접두어 없는 한국어 한 줄, 본문은 같은 방식의 목록입니다.
- **일지**: 일차마다 `unity/Devlogs/DayNN/README.md`를 씁니다. 날짜·단계·목표·검사 기준·결과, 오늘 한 일 요약, 작업 내역, 검사 표, 알아 둘 점, 다음 일차 순서이며 "~했다"체로 씁니다. 화면이 바뀌면 그림을 넣습니다.
- **Unity 코드**: 네임스페이스(`AtelierVerse.*`), 클래스와 공개 멤버에 한국어 `<summary>` 주석, 줄마다 주석은 달지 않습니다. 직렬화하는 값은 `[SerializeField] private`으로 둡니다.
- **화면 시안과 `scripts/`의 코드**: 중괄호는 다음 줄에 두고, 코드 줄마다 짧은 한국어 설명을 답니다.
- 구현되지 않은 기능을 구현된 것처럼 보이게 하지 않습니다. 누를 수 없게 하고 "준비 중"이나 도입 단계를 표시합니다.
- 화면의 이름·숫자·금액이 예시이면 예시라고 밝힙니다.
- 키와 비밀 값은 대화·문서·커밋에 적지 않습니다.
- 파일을 내려받기 전에는 사용자에게 파일 이름과 출처를 알리고 허락을 받습니다.
- 사용자의 Unity 에디터를 끄지 않습니다. 열려 있으면 검증용 사본을 씁니다.
- 일차가 끝나면 `docs/BACKLOG.md`에서 끝낸 요소를 지우고 "가장 먼저 할 것"을 다시 정합니다. 만들다가 새로 필요해진 것은 그 문서에 더합니다.
- 기획이 바뀌면 이 저장소의 `README.md`·`docs/ROADMAP.md`·`docs/PAGES.md`·`docs/BACKLOG.md`, 이 문서, 홈페이지 저장소의 `CLAUDE-HANDOFF.md`("홈페이지 밖에서 진행 중인 작업" 절)를 함께 고칩니다. 사업 계획서는 사용자가 고치라고 할 때 고칩니다.

---
## 우선 확인 문서

1. `CLAUDE-HANDOFF.md` (이 문서)
2. `README.md`: 들어 있는 것, 조작, 반영한 결정
3. `docs/ROADMAP.md`: 부족한 부분과 구현 단계
4. `docs/BACKLOG.md`: 앞으로 구현할 요소
5. `docs/PAGES.md`: 화면 구성과 게임 화면(4.1절)
6. `docs/MAP-FORMAT.md`: 맵 파일 형식
7. `unity/Devlogs/Day13/README.md`부터 거꾸로: 일차별로 한 일과 검사 결과

---
## 완료 보고 기준

- 무엇을 만들었는지, 무엇이 아직 안 되는지를 나누어 적습니다.
- 검사는 통과한 수와 실패한 수를 그대로 적고, 확인하지 않은 것을 따로 적습니다.
- 사본에서 검증했으면 그렇게 적습니다.
- 커밋 해시와 푸시 여부를 적습니다.
- 사용자가 정해야 할 것과 직접 해야 할 것을 마지막에 모읍니다.
