---
# Claude 작업 인수인계

이 문서는 다른 컴퓨터나 새 대화에서 Claude가 Atelier | Verse 개발을 바로 이어가기 위한 전달 문서입니다. 작업 기준은 이 파일이 포함된 `origin/main` 최신 커밋입니다.

- 마지막 갱신: 2026년 10월 8일
- 마지막 작업: Unity 6일차(블록 칠하기 가운데 단추·F, 화면 위 가운데의 알림 띠, 놓을 수 없는 까닭 구분)
- 마지막 검증: 편집 모드 테스트 62개 통과, 플레이 모드 테스트 56개 통과(화면 그림 찍기 5개 포함, `-captureDir` 없이 돌리면 51개 통과·5개 건너뜀). 에디터에서 직접 해 보는 확인, Windows 빌드, Quest 실행은 하지 않음
- 검증 환경: Windows 11, Unity `6000.3.21f1`, Node.js `24.19.0`
- 다음 작업: Unity 7일차(만들기 시점과 걸어 보기의 전환). 순서는 `docs/BACKLOG.md` 2절

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

에디터를 처음 열어도 일차별 셋업은 다시 실행되지 않습니다. 셋업이 만든 프리팹·씬·자산이 이미 커밋되어 있고, 적용 기록(`unity/ProjectSettings/AtelierVerseSetupState.txt`)에 `Day01`~`Day06`이 적혀 있기 때문입니다.

### Windows 환경 참고

- Unity 실행 파일의 기본 위치는 `C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe`입니다.
- 줄바꿈은 `unity/.gitattributes`가 정합니다(Unity YAML은 LF, 그림과 글꼴은 바이너리). 저장소의 다른 파일은 커밋할 때 "LF will be replaced by CRLF" 경고가 나오는데 문제가 아닙니다.
- 이전 작업 컴퓨터는 C 드라이브 여유가 10~15GB였습니다. `Library` 사본을 여러 개 두지 않습니다.

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

### 아직 동작하지 않는 것

- 맵이 이 기기에 하나뿐입니다. 새 맵, 맵 목록, 이름 바꾸기가 없고 계정에는 저장되지 않습니다.
- 블록 옮기기·돌리기가 없습니다. 되돌리기와 칠하기는 블록 하나가 기록 하나이며 되돌리기의 게임패드 키가 없습니다.
- 알림은 마지막 하나만 보입니다. 저장 실패 알림은 실제 실패 상황에서 확인하지 않았습니다.
- 블록 수 상한 500개와 블록 하나를 게임 오브젝트 하나로 두는 방식은 임시입니다. Quest에서 재 본 뒤에 정합니다.
- 바로가기의 내 작업실·맵 둘러보기·캐릭터·안전·신고는 "준비 중"으로 표시되고 누를 수 없습니다.
- 사람들 목록과 이름표에는 자기 자신("손님")만 나옵니다. 로그인, 저장, 여러 사람 접속이 없습니다.
- 대화(채팅) 화면이 없습니다.
- VR 리그가 없어 VR 기기에서 볼 수 없습니다.

---
## 최근 커밋 기록

| 커밋 | 내용 |
| --- | --- |
| (이 문서가 든 커밋) | 6일차 : 블록 칠하기와 알림 띠 |
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
  Art/Materials/    GridFloor, Block_Gold·Blue·Clay·Leaf·Ivory·Wood·Ink, Block_Ghost(미리 보기)
  Art/Textures/     Grid.png
  Art/UI/           Rounded, RoundedLine, Icon_*.png (셋업이 그린 그림)
  Data/             PartCatalog.asset (부품 목록)
  Editor/           Day1Setup ~ Day6Setup, GameUiBuilder, UiFactory, ProjectSetupRunner, DynamicFontGuard, DevCapture
  Input/            AtelierInput.inputactions (Player 맵, Game 맵)
  Prefabs/          Player_Desktop, GameUI, Block, BlockGhost
  Scenes/           Boot, Sandbox
  Scripts/Core/     AtelierPalette, GameSettings, RoomRules, AppExit, Bootstrap, Notice(+NoticeModel)
  Scripts/Player/   DesktopPlayerController, AvatarView, Nameplate, BlockBuilder
  Scripts/UI/       GameUi, HotbarModel, HotbarView, QuickMenuView, SettingsPanel, PeopleListView, NoticeBar
  Scripts/World/    GridMath, BlockMap, BlockWorld, PlacedBlock, PartCatalog, MapDocument, MapStorage, MapAutoSave, EditHistory
  Tests/EditMode/   GridMathTests, HotbarModelTests, ViewAndSettingsTests, BlockMapTests, MapDocumentTests, EditHistoryTests, NoticeModelTests
  Tests/PlayMode/   PlayTestBase, SandboxPlayTests, GameUiPlayTests, BuildPlayTests, SavePlayTests, UndoPlayTests, PaintPlayTests
unity/Assets/TextMesh Pro/   TextMesh Pro 기본 자산(Unity가 넣은 것)
unity/Assets/Settings/       URP 설정(PC용, 모바일용)
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

- `DesktopPlayerController`는 마우스를 잡았을 때만(`LookCaptured`) 시점을 돌립니다. `Cursor.lockState`는 창 없는 실행에서 믿을 수 없어 자체 상태로 판단합니다.
- 메뉴가 열리면 `GameUi`가 `SetInputBlocked(true)`로 조작을 막고, 닫으면 풀고 마우스를 다시 잡습니다.
- 화면의 단추는 마우스로만 누릅니다. `EventSystem`의 키보드 이동(`sendNavigationEvents`)을 꺼 두었습니다. 켜면 Space나 WASD가 단추를 누릅니다.
- 꾸미기용 그림과 글자는 `raycastTarget`을 꺼 두었습니다. 켜져 있으면 그 위를 눌렀을 때 마우스 잡기가 되지 않습니다.
- 게임 끝내기는 `AppExit.Request()`로 모입니다. 테스트는 `AppExit.Handler`를 바꿔 실제로 끝나지 않게 합니다.

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

# 플레이 모드 테스트. -captureDir을 주면 화면 그림 13장도 찍는다(2~6일차의 그림이 한 폴더에 나오므로 필요한 것만 일지에 남긴다)
Unity.exe -batchmode -projectPath <프로젝트> -runTests -testPlatform PlayMode -captureDir <그림 폴더> -testResults <결과.xml> -logFile <로그>

# 아직 적용하지 않은 일차의 셋업 적용
Unity.exe -batchmode -quit -projectPath <프로젝트> -executeMethod AtelierVerse.EditorTools.ProjectSetupRunner.RunFromCommandLine -logFile <로그>

# 특정 일차의 셋업 다시 적용
Unity.exe -batchmode -quit -projectPath <프로젝트> -executeMethod AtelierVerse.EditorTools.Day2Setup.Apply -logFile <로그>
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

실제 마우스 포인터로 단추가 눌리는지, 마우스 시점의 느낌, 점프, Windows 빌드, Quest에서의 실행입니다. 확인하지 않은 것은 확인하지 않았다고 일지와 보고에 적습니다.

---
## 사용자 결정이 필요한 항목

물었으나 아직 답을 받지 못한 것입니다.

1. **글꼴**: 임시로 나눔고딕을 넣었습니다. 웹 시안과 같은 Pretendard로 맞추려면 글꼴 파일을 내려받아야 하며, 내려받아도 되는지 물어 둔 상태입니다. 바꿀 때는 `Art/Fonts`의 파일과 `Day2Setup`의 `FontPath`·`FontAssetPath`를 바꾸고 셋업을 다시 적용합니다.
2. **사업 계획서 갱신**: 12.3.1의 VR 문장, 12.4.1의 "VR 입장(WebXR)", 12.6.1의 플랫폼 방식이 아직 WebXR로 적혀 있고, 12.10의 화면 표는 처음 11개만 있습니다. Unity 방식과 화면 17개로 고칠지 물어 둔 상태입니다.
3. **홈페이지 소개 페이지**: VR 문구가 아직 "검토하고 있습니다"입니다. 고칠지 물어 둔 상태입니다(홈페이지 저장소 작업).
4. **`docs/ROADMAP.md` 5절의 질문 네 가지**: VR에서는 구경과 이동만 할지, 내 작업실·둘러보기·소식을 앱에 둘지 웹에 둘지, 1단계가 끝날 때까지 뒤 단계 기능을 만들지 않을지, 비공개 시험의 판단 기준.

사용자가 직접 해야 하는 것입니다.

- Unity Hub에서 `6000.3.21f1`에 Android Build Support 설치(VR 리그를 만드는 일차 전)
- Meta 개발자 계정 등록과 Quest의 개발자 모드 켜기
- 에디터에서 Sandbox 씬을 실행해 걷기와 메뉴, 블록 놓기, 가운데 단추·F 칠하기, Ctrl+Z 되돌리기, 알림 띠, 껐다 켰을 때 블록이 남는지 직접 확인
- 통합 계정의 실제 연결(홈페이지 저장소의 `docs/UNIFIED-ACCOUNT.md`)

---
## 다음 작업

순서와 끝난 기준은 `docs/ROADMAP.md` 3절에, 남은 구현 요소의 전체 목록은 `docs/BACKLOG.md`에 있습니다. 지금은 0단계입니다. 다음 일차를 고를 때는 `docs/BACKLOG.md` 2절 "가장 먼저 할 것"을 위에서부터 봅니다.

1. **7일차: 만들기 시점.** 날아다니며 만드는 시점과 걸어 보기의 전환. 키(가안: V 또는 더블 Space), 날 때의 속도와 상하 이동(Space·Ctrl 또는 Shift), 전환할 때 캐릭터 위치 처리, 떨어지지 않는 상태의 저장 여부를 정합니다. `DesktopPlayerController`의 이동 코드를 건드리므로 1일차 걷기 테스트가 그대로 통과해야 합니다.
2. **Windows 빌드 확인.** 빌드 스크립트와 실행, 저장 폴더 생성 확인.
3. **VR 리그와 Quest 빌드.** Android Build Support가 설치된 뒤에 진행합니다. 이 에디터의 권장 버전은 XR Plugin Management 4.5.4, OpenXR 1.16.1, XR Interaction Toolkit 3.3.2, Meta OpenXR 2.3.1입니다. `GameUI`의 메뉴를 눈앞에 띄우는 방식으로 옮깁니다.
4. 그 뒤는 1-A(혼자 만들기)의 나머지: 모양이 다른 부품과 옮기기·돌리기, 여러 맵 다루기, 부품 수 상한 정하기(Quest에서 재기).

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
7. `unity/Devlogs/Day06/README.md`부터 거꾸로: 일차별로 한 일과 검사 결과

---
## 완료 보고 기준

- 무엇을 만들었는지, 무엇이 아직 안 되는지를 나누어 적습니다.
- 검사는 통과한 수와 실패한 수를 그대로 적고, 확인하지 않은 것을 따로 적습니다.
- 사본에서 검증했으면 그렇게 적습니다.
- 커밋 해시와 푸시 여부를 적습니다.
- 사용자가 정해야 할 것과 직접 해야 할 것을 마지막에 모읍니다.
