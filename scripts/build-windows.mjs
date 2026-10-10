// Windows 실행 파일을 명령줄로 만들고, 원하면 바로 실행해 저장 파일과 로그로 확인하는 도구
// 사용법: node scripts/build-windows.mjs [--out 폴더] [--development] [--run] [--vr] [--quality low|normal|high] [--screen-check] [--quit-after 초] [--skip-build] [--unity Unity.exe 경로]
// --screen-check는 실행 중에 전체 화면으로 갔다가 창으로 돌아오게 해서, 로그에 적힌 실제 화면 방식과 크기를 확인한다(화면이 잠깐 전체 화면이 됨).
// --quality는 실행 확인을 그 화면 품질로 한다(이번 실행만, 설정은 바꾸지 않음). 그림은 smoke-품질.png로 저장하고, 로그에서 그 품질 단계로 그렸는지 확인한다.
// --vr은 실행 확인을 -vr로 한다. VR 프로그램이 없는 PC에서는 키보드·마우스로 돌아오는지를 보게 된다.
// 평소 실행(--vr 없음)은 VR 프로그램(OpenXR 런타임)을 한 번도 찾지 않아야 하며, 찾은 흔적이 로그에 있으면 실패로 본다.
import { spawnSync } from "node:child_process"; // 외부 프로그램 실행
import { existsSync, mkdirSync, readFileSync, readdirSync, statSync } from "node:fs"; // 파일 확인
import { homedir } from "node:os"; // 사용자 폴더
import { join, normalize, resolve } from "node:path"; // 경로 처리
import { fileURLToPath } from "node:url"; // 파일 주소 변환

const REPO = normalize(join(fileURLToPath(new URL(".", import.meta.url)), "..")); // 저장소 폴더
const PROJECT = join(REPO, "unity"); // Unity 프로젝트 폴더
const EXECUTABLE = "AtelierVerse.exe"; // 실행 파일 이름
const VR_LAUNCHER = "AtelierVerse-VR.bat"; // VR로 시작하는 배치 파일 이름
const BOOT_CONFIG = join("AtelierVerse_Data", "boot.config"); // 실행 파일의 시작 설정
const PRE_INIT_KEY = "xrsdk-pre-init-library"; // 시작 설정에 남아 있으면 안 되는 XR 사전 초기화 항목
const RUNTIME_CONTACT = /OpenXR-Loader|Loading OpenXR loader|XR_ERROR_|xrCreateInstance/; // 로그에서 VR 프로그램을 찾은 흔적
const QUALITY_LEVELS = { low: "PC Low", normal: "PC", high: "PC High" }; // 화면 품질마다의 품질 단계 이름(GraphicsQuality.LevelNames와 같아야 함)
const SAVE_FILE = join(homedir(), "AppData", "LocalLow", "Palettra Games", "Atelier Verse", "maps", "local.map.json"); // 실행 파일이 쓰는 맵 파일
const args = process.argv.slice(2); // 명령줄 인자

function option(name, fallback) // 이름 뒤의 값 읽기
{
    const index = args.indexOf(name); // 인자 위치
    return index >= 0 && index + 1 < args.length ? args[index + 1] : fallback; // 값 또는 기본값
} // option 끝

function flag(name) // 켜짐 여부 읽기
{
    return args.includes(name); // 포함 여부
} // flag 끝

function grep(file, pattern) // 로그에서 줄 찾기
{
    if (!existsSync(file)) return []; // 파일 없음
    return readFileSync(file, "utf8").split(/\r?\n/).filter((line) => pattern.test(line)); // 맞는 줄
} // grep 끝

function folderSize(folder) // 폴더 전체 크기(바이트)
{
    let total = 0; // 합계
    for (const entry of readdirSync(folder, { withFileTypes: true })) // 항목마다
    {
        const path = join(folder, entry.name); // 항목 경로
        total += entry.isDirectory() ? folderSize(path) : statSync(path).size; // 하위 폴더 또는 파일 크기
    }
    return total; // 합계 반환
} // folderSize 끝

function defaultUnity() // 프로젝트 버전에 맞는 Unity 실행 파일
{
    const text = readFileSync(join(PROJECT, "ProjectSettings", "ProjectVersion.txt"), "utf8"); // 버전 파일
    const version = /m_EditorVersion:\s*(\S+)/.exec(text)?.[1] ?? ""; // 에디터 버전
    return join("C:\\Program Files\\Unity\\Hub\\Editor", version, "Editor", "Unity.exe"); // 기본 설치 위치
} // defaultUnity 끝

const out = resolve(option("--out", join(PROJECT, "Builds", "Windows"))); // 출력 폴더
const unity = option("--unity", defaultUnity()); // Unity 실행 파일
const executable = join(out, EXECUTABLE); // 만들어질 실행 파일
mkdirSync(out, { recursive: true }); // 출력 폴더 준비

if (!flag("--skip-build")) // 빌드 단계
{
    if (!existsSync(unity)) // Unity 확인
    {
        console.error(`Unity 실행 파일이 없습니다: ${unity} (--unity 경로로 지정)`); // 안내
        process.exit(1); // 실패 종료
    }
    const log = join(out, "build.log"); // 빌드 로그
    const unityArgs = ["-batchmode", "-quit", "-projectPath", PROJECT, "-executeMethod", "AtelierVerse.EditorTools.BuildPlayer.BuildWindows", "-buildOut", out, "-logFile", log]; // Unity 인자
    if (flag("--development")) unityArgs.push("-development"); // 개발용 빌드
    console.log(`빌드 시작: ${out}`); // 시작 안내
    const started = Date.now(); // 시작 시각
    const build = spawnSync(unity, unityArgs, { stdio: "ignore" }); // Unity 실행(끝날 때까지 기다림)
    console.log(`빌드 종료 코드 ${build.status}, ${Math.round((Date.now() - started) / 1000)}초`); // 결과
    for (const line of grep(log, /\[Atelier Verse\] Windows 빌드/)) console.log(line.replace(/^.*?\[Atelier Verse\]/, "[Atelier Verse]")); // 요약 줄
    if (build.status !== 0 || !existsSync(executable)) // 실패 처리
    {
        for (const line of grep(log, /error CS|Exception|Error building/).slice(0, 10)) console.error(line); // 오류 줄
        console.error(`빌드가 끝나지 않았습니다. 로그: ${log}`); // 안내
        process.exit(build.status || 1); // 실패 종료
    }
}

if (!existsSync(executable)) // 실행 파일 확인
{
    console.error(`실행 파일이 없습니다: ${executable}`); // 안내
    process.exit(1); // 실패 종료
}
console.log(`실행 파일: ${executable} (폴더 전체 ${(folderSize(out) / 1024 / 1024).toFixed(0)}MB)`); // 크기 안내

const preInit = grep(join(out, BOOT_CONFIG), new RegExp(`^\\s*${PRE_INIT_KEY}=`)); // 시작 설정의 XR 사전 초기화 줄
const launcher = join(out, VR_LAUNCHER); // VR로 시작하는 파일
console.log(`시작 설정의 XR 사전 초기화: ${preInit.length ? "남아 있음" : "없음"}`); // 사전 초기화 확인
console.log(`VR로 시작하는 파일: ${existsSync(launcher) ? "있음" : "없음"} ${launcher}`); // 배치 파일 확인
if (preInit.length > 0 || !existsSync(launcher)) // 빌드 뒤 다듬기가 빠진 경우
{
    console.error("빌드 뒤 다듬기(XrBootConfig)가 적용되지 않았습니다. 평소 실행에도 VR 프로그램이 깨어날 수 있습니다."); // 안내
    process.exit(1); // 실패 종료
}

if (flag("--run")) // 실행 확인 단계
{
    const seconds = option("--quit-after", "10"); // 끝내기까지의 초
    const quality = option("--quality", ""); // 이번 실행의 화면 품질(없으면 설정을 따름)
    if (quality && !QUALITY_LEVELS[quality]) // 알 수 없는 품질
    {
        console.error(`--quality는 low, normal, high 가운데 하나여야 합니다: ${quality}`); // 안내
        process.exit(1); // 실패 종료
    }
    const screenshot = join(out, flag("--vr") ? "smoke-vr.png" : quality ? `smoke-${quality}.png` : "smoke.png"); // 화면 그림
    const log = join(out, "player.log"); // 실행 로그
    const exeArgs = ["-quitAfter", seconds, "-screenshotOut", screenshot, "-logFile", log, "-screen-fullscreen", "0", "-screen-width", "1280", "-screen-height", "720"]; // 실행 인자
    if (flag("--vr")) exeArgs.push("-vr"); // VR 화면으로 시작 요청
    if (quality) exeArgs.push("-quality", quality); // 이번 실행의 화면 품질
    if (flag("--screen-check")) exeArgs.push("-screenCheck"); // 화면 방식을 바꿔 보는 확인
    console.log(`실행 시작: ${seconds}초 뒤 스스로 끝남`); // 안내
    const run = spawnSync(executable, exeArgs, { stdio: "ignore", timeout: (Number(seconds) + 90) * 1000 }); // 실행(끝날 때까지 기다림)
    console.log(`실행 종료 코드 ${run.status}${run.error ? ` (${run.error.message})` : ""}`); // 결과
    for (const line of grep(log, /\[Atelier Verse\]/)) console.log(line); // 앱의 안내 줄
    const problems = grep(log, /Exception|NullReference|error CS/).filter((line) => !/\[Atelier Verse\]/.test(line)); // 예외 줄
    console.log(problems.length ? `로그의 예외 ${problems.length}줄:\n${problems.slice(0, 10).join("\n")}` : "로그에 예외 없음"); // 예외 안내
    console.log(`저장 파일: ${existsSync(SAVE_FILE) ? "있음" : "없음"} ${SAVE_FILE}`); // 저장 확인
    console.log(`화면 그림: ${existsSync(screenshot) ? "있음" : "없음"} ${screenshot}`); // 그림 확인
    const contacts = grep(log, RUNTIME_CONTACT).length; // VR 프로그램을 찾은 줄 수
    const quiet = flag("--vr") || contacts === 0; // 평소 실행은 한 번도 찾지 않아야 함
    console.log(`VR 프로그램을 찾은 흔적: ${contacts}줄${flag("--vr") ? " (-vr로 켰으므로 찾는 것이 정상)" : quiet ? "" : " (평소 실행인데 찾았음)"}`); // 흔적 안내
    const qualityLines = grep(log, /\[Atelier Verse\] 화면 품질/); // 화면 품질을 적용했다는 줄
    const qualityOk = qualityLines.length > 0 && (!quality || qualityLines.some((line) => line.includes(`(${QUALITY_LEVELS[quality]})`))); // 적용했고, 품질을 정했으면 그 단계여야 함
    console.log(`화면 품질 적용: ${qualityOk ? "확인" : qualityLines.length ? "정한 품질과 다름" : "적용했다는 줄이 없음"}`); // 품질 안내
    let screenOk = true; // 화면 방식 확인(부탁했을 때만 봄)
    if (flag("--screen-check")) // 화면 방식 확인 단계
    {
        const steps = grep(log, /화면 방식 확인 [123]\/3/); // 처음, 전체 화면으로, 창으로
        const full = steps.find((line) => line.includes("2/3")) ?? ""; // 전체 화면으로 간 뒤의 줄
        const back = steps.find((line) => line.includes("3/3")) ?? ""; // 창으로 돌아온 뒤의 줄
        const monitor = /모니터 (\d+x\d+)/.exec(full)?.[1] ?? ""; // 모니터의 크기
        screenOk = steps.length === 3 && full.includes(`: 전체 화면 ${monitor} `) && monitor !== "" && back.includes(": 창 1280x720 "); // 모니터 크기의 전체 화면이 되었다가 처음 크기의 창으로 돌아와야 함
        console.log(`화면 방식 확인: ${screenOk ? "전체 화면이 되었다가 창으로 돌아옴" : "기대와 다름"}`); // 결과 안내
    }
    if (run.status !== 0 || !existsSync(SAVE_FILE) || problems.length > 0 || !quiet || !qualityOk || !screenOk) process.exit(1); // 하나라도 틀리면 실패
}
