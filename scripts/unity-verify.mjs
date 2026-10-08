// Unity 에디터가 프로젝트를 열고 있을 때 쓰는 검증용 사본(.verify/unity)을 만들고 실제 프로젝트와 맞추는 도구
import { copyFileSync, cpSync, existsSync, mkdirSync, readFileSync, readdirSync, rmSync, writeFileSync } from "node:fs"; // 파일 처리
import { dirname, extname, join } from "node:path"; // 경로 처리
import { fileURLToPath } from "node:url"; // 파일 주소 변환

const REPO = join(fileURLToPath(new URL(".", import.meta.url)), ".."); // 저장소 폴더
const REAL = join(REPO, "unity"); // 실제 프로젝트
const COPY = join(REPO, ".verify", "unity"); // 검증용 사본
const STATE = join("ProjectSettings", "AtelierVerseSetupState.txt"); // 일차별 셋업 적용 기록
const FOLDERS = ["Assets", "Packages", "ProjectSettings"]; // 사본에 넣는 폴더
const SOURCE = new Set([".cs", ".asmdef", ".inputactions", ".ttf", ".txt", ".md", ".json"]); // 사람이 고치는 파일
const GENERATED = new Set([".prefab", ".unity", ".asset", ".png", ".mat"]); // 셋업이 만드는 파일
const [command, ...options] = process.argv.slice(2); // 명령과 선택 값
const dry = options.includes("--dry"); // 무엇을 할지 보여 주기만 할지

function walk(folder, base, found) // 폴더 안의 모든 파일 찾기
{
    for (const entry of readdirSync(folder, { withFileTypes: true })) // 항목마다
    {
        const relative = join(base, entry.name); // 프로젝트 기준 경로
        if (entry.isDirectory()) walk(join(folder, entry.name), relative, found); // 하위 폴더로
        else found.push(relative); // 파일 기록
    }
    return found; // 찾은 목록
}

function same(first, second) // 두 파일의 내용이 같은지
{
    if (!existsSync(first) || !existsSync(second)) return false; // 한쪽이 없으면 다름
    return readFileSync(first).equals(readFileSync(second)); // 내용 비교
}

function copy(from, to, label, relative) // 파일 하나 복사
{
    console.log(`${label} ${relative}`); // 한 일 표시
    if (dry) return; // 보여 주기만
    mkdirSync(dirname(to), { recursive: true }); // 폴더 준비
    copyFileSync(from, to); // 복사
}

function kind(relative) // 메타 파일인지와 원래 확장자
{
    const isMeta = relative.endsWith(".meta"); // 메타 파일 여부
    return { isMeta, extension: extname(isMeta ? relative.slice(0, -5) : relative).toLowerCase() }; // 결과
}

function setState(root, id, present) // 셋업 적용 기록에 일차를 넣거나 빼기
{
    const file = join(root, STATE); // 기록 파일
    const done = new Set(existsSync(file) ? readFileSync(file, "utf8").split(/\r?\n/).map((line) => line.trim()).filter(Boolean) : []); // 적용된 일차
    if (present) done.add(id); // 넣기
    else done.delete(id); // 빼기
    writeFileSync(file, [...done].sort().join("\n") + "\n"); // 저장
}

function prepare() // 사본 만들기. 사본의 Library는 남겨 두어 다음 실행을 빠르게 한다
{
    for (const folder of FOLDERS) // 폴더마다
    {
        rmSync(join(COPY, folder), { recursive: true, force: true }); // 예전 사본 지우기
        cpSync(join(REAL, folder), join(COPY, folder), { recursive: true }); // 새로 복사
    }
    const hold = options[options.indexOf("--hold") + 1]; // 검증 전이라 미뤄 둘 일차
    if (options.includes("--hold") && hold) // 미룰 일차가 있으면
    {
        setState(REAL, hold, true); // 열려 있는 에디터에서는 자동 셋업이 돌지 않게 한다
        setState(COPY, hold, false); // 사본에서는 셋업이 돌게 한다
    }
    console.log(`사본을 만들었습니다: ${COPY}`); // 결과 표시
}

function toVerify() // 고친 소스를 실제 프로젝트에서 사본으로
{
    let count = 0; // 복사한 수
    for (const relative of walk(join(REAL, "Assets"), "Assets", [])) // 실제 프로젝트의 파일마다
    {
        const { isMeta, extension } = kind(relative); // 파일 종류
        if (!isMeta && !SOURCE.has(extension)) continue; // 소스와 메타만 다룬다
        if (same(join(REAL, relative), join(COPY, relative))) continue; // 같으면 건너뜀
        copy(join(REAL, relative), join(COPY, relative), isMeta ? "META" : "SRC ", relative); // 복사
        count++; // 수 세기
    }
    console.log(`${dry ? "(보기만) " : ""}${count}개`); // 결과 표시
}

function toReal() // 셋업이 만든 자산을 사본에서 실제 프로젝트로
{
    let count = 0; // 복사한 수
    const differentMeta = []; // 식별자가 서로 다른 메타
    for (const relative of walk(join(COPY, "Assets"), "Assets", [])) // 사본의 파일마다
    {
        const { isMeta, extension } = kind(relative); // 파일 종류
        const from = join(COPY, relative); // 사본 쪽
        const to = join(REAL, relative); // 실제 쪽
        if (existsSync(to) && isMeta) // 메타가 이미 있으면 실제 프로젝트의 것을 그대로 둔다
        {
            if (!same(from, to)) differentMeta.push(relative); // 다르면 기록
            continue; // 다음 파일
        }
        if (existsSync(to) && (!GENERATED.has(extension) || same(from, to))) continue; // 소스이거나 같은 자산이면 건너뜀
        copy(from, to, existsSync(to) ? "GEN " : "NEW ", relative); // 복사
        count++; // 수 세기
    }
    console.log(`${dry ? "(보기만) " : ""}${count}개`); // 결과 표시
    if (differentMeta.length) console.log(`메타가 서로 다릅니다(실제 프로젝트의 것을 유지):\n  ${differentMeta.join("\n  ")}`); // 주의 표시
}

if (command === "prepare") prepare(); // 사본 만들기
else if (command === "to-verify") toVerify(); // 소스 보내기
else if (command === "to-real") toReal(); // 자산 가져오기
else if (command === "clean") rmSync(join(REPO, ".verify"), { recursive: true, force: true }); // 사본 지우기
else console.log("사용법: node scripts/unity-verify.mjs prepare [--hold Day03] | to-verify [--dry] | to-real [--dry] | clean"); // 안내
