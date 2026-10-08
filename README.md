# Atelier | Verse

3D 공간에서 자신의 맵을 만들고 꾸민 뒤 다른 사람을 초대해 함께 머무는 VR 샌드박스 서비스입니다.

- 운영: Palettra Games
- 현재 단계: 기획, 화면 시안, Unity 프로젝트 8일차(Windows 빌드)
- 기획 문서: 사업 계획서(Google 문서)의 "12. Atelier | Verse (VR 샌드박스 서비스)" 탭
- 다른 환경에서 이어서 작업할 때: [`CLAUDE-HANDOFF.md`](CLAUDE-HANDOFF.md)
- 서비스 이름은 가칭이며 상표·도메인 확인 전입니다.

## 지금 들어 있는 것

`design/` 폴더에 서비스 화면 시안과 공통 구성요소가 있습니다. 실제로 동작하는 서비스가 아니며, 화면의 이름·숫자·금액은 모두 예시입니다. 화면 사이의 흐름과 화면을 만드는 규칙은 [`docs/PAGES.md`](docs/PAGES.md)에 있습니다.

| 화면 | 파일 | 내용 | 도입 단계(가안) |
| --- | --- | --- | --- |
| 시안 목록 | `design/index.html` | 화면 목록과 시안에 반영한 결정 | - |
| 시작하기 | `design/signin.html` | 통합 계정 로그인, 만 14세 이상 확인 | 1단계 |
| 처음 안내 | `design/welcome.html` | 서비스에서 쓸 이름과 캐릭터 정하기 | 1단계 |
| 내 작업실 | `design/studio.html` | 로그인 뒤 첫 화면. 새 맵 만들기, 내 맵, 초대받은 방 | 1단계 |
| 에디터 | `design/editor.html` | 부품 고르기, 편집 화면, 속성, 성능 상한 | 1단계 |
| 방 안 | `design/room.html` | 최대 8명, 채팅, 참여자, VR 입장 안내 | 1단계 |
| 프로필 | `design/profile.html` | 기본 캐릭터 고르기, 공개 범위, 연결된 계정 | 1단계 |
| 설정 | `design/settings.html` | 화면·그래픽, 조작, VR 이동 방법, 계정 | 1단계 |
| 둘러보기 | `design/explore.html` | 공개 맵 검색, 지금 열린 방 | 2단계 |
| 맵 상세 | `design/map.html` | 맵 소개, 방 열기, 초대 링크, 리믹스, 신고 | 2단계 |
| 소식 | `design/feed.html` | 친구·제작자 소식, 알림 설정 | 2단계 |
| 알림 | `design/notifications.html` | 초대와 반응 알림 목록 | 2단계 |
| 검색 | `design/search.html` | 맵·조립품·제작자 검색 결과 | 2단계 |
| 신고 처리 | `design/reports.html` | 운영자 전용. 신고 확인과 처리 | 2단계 |
| 메시지 | `design/messages.html` | 1:1 대화, 그룹, 공지 채널 | 4단계 |
| 상점 | `design/shop.html` | 꾸미기 아이템, 제작자 마켓, 내 보관함 | 4~5단계 |
| 구독 | `design/plans.html` | 무료와 구독 요금제 비교 | 4단계 |
| 상태 화면 | `design/states.html` | 불러오는 중, 내용 없음, 오류, 제한 안내 모음 | 공통 |
| 구성요소 | `design/components.html` | 색, 버튼, 입력, 카드, 캐릭터, 부품 그림 모음 | 공통 |

## Unity 프로젝트

`unity/` 폴더가 실제 게임이 되는 Unity 프로젝트입니다(Unity `6000.3.21f1`, URP). 일차 단위로 진행하며 그날 한 일과 검사 결과는 `unity/Devlogs/DayNN/README.md`에 있습니다.

| 일차 | 내용 | 일지 |
| --- | --- | --- |
| 1일차 | 프로젝트 기반, 시험 장면, PC 걷기 | [`unity/Devlogs/Day01`](unity/Devlogs/Day01/README.md) |
| 2일차 | 블록 캐릭터, 1인칭·3인칭 시점, 게임 화면(늘 보이는 화면과 Esc 메뉴) | [`unity/Devlogs/Day02`](unity/Devlogs/Day02/README.md) |
| 3일차 | 부품 칸에서 고른 블록을 모눈에 놓고 지우기, 놓일 자리 미리 보기 | [`unity/Devlogs/Day03`](unity/Devlogs/Day03/README.md) |
| 4일차 | 맵 데이터 형식 1판, 놓은 블록을 이 기기에 자동 저장하고 다시 열면 불러오기 | [`unity/Devlogs/Day04`](unity/Devlogs/Day04/README.md) |
| 5일차 | 놓기·지우기의 되돌리기(Ctrl+Z)와 다시 실행(Ctrl+Y) | [`unity/Devlogs/Day05`](unity/Devlogs/Day05/README.md) |
| 6일차 | 놓인 블록을 고른 부품으로 칠하기, 화면 위 가운데의 알림 띠 | [`unity/Devlogs/Day06`](unity/Devlogs/Day06/README.md) |
| 7일차 | 날기(만들기 시점)와 걸어 보기의 전환 | [`unity/Devlogs/Day07`](unity/Devlogs/Day07/README.md) |
| 8일차 | Windows 빌드 스크립트, 실제 빌드와 실행 확인, 메뉴의 버전 표시 | [`unity/Devlogs/Day08`](unity/Devlogs/Day08/README.md) |

Unity Hub에서 `unity` 폴더를 열고 `Assets/_Project/Scenes/Sandbox` 씬에서 재생을 누르면 걸어 볼 수 있습니다. 놓은 블록은 이 기기의 맵 파일(`%USERPROFILE%\AppData\LocalLow\Palettra Games\Atelier Verse\maps\local.map.json`)에 자동으로 저장됩니다. 형식은 [`docs/MAP-FORMAT.md`](docs/MAP-FORMAT.md)에 있습니다.

| 조작 | 키 |
| --- | --- |
| 걷기, 달리기, 점프 | W A S D, Shift, Space |
| 날기 켜고 끄기 | V (날 때는 Space로 오르고 Shift로 내려옴) |
| 둘러보기 | 화면을 한 번 누른 뒤 마우스 |
| 1인칭·3인칭 | 마우스 휠 |
| 부품 고르기 | 1~9 (같은 키를 다시 누르면 풂) |
| 블록 놓기, 지우기 | 부품을 고른 뒤 마우스 왼쪽, 오른쪽 |
| 블록 칠하기 | 부품을 고르고 블록을 가리킨 뒤 마우스 가운데 또는 F |
| 되돌리기, 다시 실행 | Ctrl+Z, Ctrl+Y |
| 사람들 목록 | Tab |
| 메뉴 | Esc |

## Windows 빌드 만들기

Unity Hub에 프로젝트 버전(`6000.3.21f1`)의 에디터가 기본 위치에 설치되어 있으면 아래 한 줄로 실행 파일을 만듭니다. 결과는 `unity/Builds/Windows/AtelierVerse.exe`이며 저장소에는 들어가지 않습니다.

```bash
node scripts/build-windows.mjs
```

`--run`을 붙이면 만든 실행 파일을 10초 동안 실제로 띄웠다가 스스로 끝내고, 맵 파일이 만들어졌는지와 로그에 예외가 없는지, 화면 그림(`smoke.png`)을 확인합니다. `--development`는 개발용 빌드, `--out 폴더`는 다른 출력 폴더, `--skip-build --run`은 이미 만든 실행 파일만 다시 확인합니다. 에디터 메뉴 `Atelier Verse/Windows 빌드 만들기`로도 만들 수 있습니다.

실행 파일은 `-quitAfter 초`와 `-screenshotOut 경로` 인자를 알아듣습니다(자동 확인용).

## 시안 보는 방법

Node.js만 있으면 됩니다. 설치할 패키지는 없습니다.

```bash
node scripts/serve.mjs
```

브라우저에서 `http://127.0.0.1:3100/`을 엽니다. 포트를 바꾸려면 `PORT` 환경 변수를 줍니다.

## 시안에 반영한 결정 (2026-10-08)

| 항목 | 결정 |
| --- | --- |
| 만드는 방식 | Unity 앱 하나로 PC(Windows)와 VR(Meta Quest)이 같은 방에 입장. 처음의 브라우저(WebXR) 결정을 같은 날 바꿈 |
| 초대 | 링크(앱 실행)와 방 코드 |
| 방 인원 | 방당 최대 8명 |
| 첫 화면 | 내 작업실 |
| 그래픽 방향 | 블록·로우폴리 |
| 아바타 | 기본 캐릭터 선택 |
| 대상 연령 | 청소년 이상(만 14세 이상) |
| 계정 | 홈페이지 계정으로 로그인하는 통합 계정(이 서비스의 데이터베이스는 따로 두고 로그인만 통합) |
| 메시지 | 1:1 대화와 그룹·공지 채널 |
| 수익 모델 | 꾸미기 아이템 판매, 구독, 제작자 마켓 수수료 |
| 모바일 | 설치형 웹앱(PWA) |
| 개발 조건 | 1인, 주 10시간 이하, 웹(Three.js 등)과 Unity 경험 |

금액, 수수료율, 결제 수단, 나이 확인 방법, 약관 문구는 아직 정하지 않았습니다.

통합 계정의 구조와 서비스가 지킬 약속은 홈페이지 저장소의 `docs/UNIFIED-ACCOUNT.md`에 있습니다. 이 서비스는 구현할 때 그 문서의 4절을 따릅니다.

## 디자인 기준

테마는 "햇살 작업실(Sunlit Atelier)"입니다. 햇빛이 드는 작업실 책상 위에서 종이와 물감으로 무언가를 만드는 장면을 기준으로 삼습니다.

- 색과 치수: `design/assets/tokens.css`의 `--av-`로 시작하는 토큰. 밝은 화면과 어두운 화면을 함께 정의합니다.
- 기본 틀: `design/assets/base.css` (옆 메뉴, 상단 띠, 휴대폰 아래 메뉴)
- 구성요소: `design/assets/components.css` (버튼, 카드, 칩, 입력, 아바타, 말풍선 등)
- 화면별 배치: `design/assets/screens.css`
- 공통 동작: `design/assets/shell.js` (공통 틀 넣기, 아이콘, 화면 모드, 고르기 묶음, 대화상자)
- 블록 그림: `design/assets/scene.js` (맵 미리보기, 기본 캐릭터, 부품 그림을 SVG로 그림)

맵 미리보기와 캐릭터 그림은 이미지 파일이 아니라 `scene.js`가 그리는 그림입니다. 실제 3D 화면을 대신하는 자리 표시입니다.

## 코드 작성 규칙

- 중괄호는 다음 줄에 둡니다.
- 코드 줄마다 짧은 한국어 설명을 답니다.
- 구현되지 않은 기능을 구현된 것처럼 표시하지 않습니다. 이후 단계의 기능은 비활성 버튼이나 단계 표시로 나타냅니다.

## 다음 할 일

지금 부족한 부분과 구현 순서는 [`docs/ROADMAP.md`](docs/ROADMAP.md)에, 앞으로 넣어야 하거나 넣으면 좋을 구현 요소의 목록은 [`docs/BACKLOG.md`](docs/BACKLOG.md)에 있습니다. 가장 먼저 할 일은 0단계입니다.

1. 조작과 겉모습 나누기
2. Unity Hub에서 Android 빌드 구성 설치(사용자), VR 리그와 Quest 빌드 확인
3. 모양이 다른 부품과 옮기기·돌리기, 여러 맵 다루기
