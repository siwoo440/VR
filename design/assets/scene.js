(function () // 블록 장면 모듈
{
    const TILE_W = 44; // 타일 가로 길이
    const TILE_H = 22; // 타일 세로 길이
    const UNIT_Z = 24; // 블록 한 칸 높이
    const COLORS = { gold: "var(--av-gold)", blue: "var(--av-blue)", clay: "var(--av-clay)", leaf: "var(--av-leaf)", paper: "var(--av-block)", wood: "var(--av-line-strong)", stone: "var(--av-line)" }; // 블록 색 이름표
    const WALLS = ["paper", "gold", "blue", "clay"]; // 벽 색 후보
    const BODIES = ["gold", "blue", "clay", "leaf"]; // 캐릭터 몸 색 후보

    function createCanvas() // 그림 범위 계산기 만들기
    {
        return { minX: Infinity, minY: Infinity, maxX: -Infinity, maxY: -Infinity, parts: [] }; // 범위와 그림 조각
    }

    function project(canvas, x, y, z) // 공간 좌표를 화면 좌표로 바꾸기
    {
        const screenX = (x - y) * TILE_W / 2; // 화면 가로 위치
        const screenY = (x + y) * TILE_H / 2 - z * UNIT_Z; // 화면 세로 위치
        canvas.minX = Math.min(canvas.minX, screenX); // 왼쪽 범위
        canvas.maxX = Math.max(canvas.maxX, screenX); // 오른쪽 범위
        canvas.minY = Math.min(canvas.minY, screenY); // 위쪽 범위
        canvas.maxY = Math.max(canvas.maxY, screenY); // 아래쪽 범위
        return [screenX, screenY]; // 화면 좌표 반환
    }

    function points(canvas, corners) // 꼭짓점 목록을 문자열로 바꾸기
    {
        return corners.map((corner) => project(canvas, corner[0], corner[1], corner[2]).map((value) => value.toFixed(1)).join(",")).join(" "); // 다각형 좌표 문자열
    }

    function shade(color, amount) // 면의 어둡기 계산
    {
        return amount === 0 ? color : `color-mix(in srgb, ${color} ${100 - amount}%, var(--av-shadow))`; // 그림자 색 섞기
    }

    function face(canvas, corners, fill, extra) // 면 하나 그리기
    {
        canvas.parts.push(`<polygon points="${points(canvas, corners)}" style="fill:${fill};stroke:var(--av-ink);stroke-width:1;stroke-linejoin:round${extra || ""}"/>`); // 다각형 추가
    }

    function drawBox(canvas, box, extra) // 상자 하나의 세 면 그리기
    {
        const width = box.w ?? 1; // 가로 크기
        const depth = box.d ?? 1; // 세로 크기
        const height = box.h ?? 1; // 높이
        const fill = COLORS[box.color] || box.color; // 상자 색
        const x = box.x; // 가로 위치
        const y = box.y; // 세로 위치
        const z = box.z ?? 0; // 높이 위치
        face(canvas, [[x, y + depth, z + height], [x + width, y + depth, z + height], [x + width, y + depth, z], [x, y + depth, z]], shade(fill, 18), extra); // 왼쪽 앞면
        face(canvas, [[x + width, y, z + height], [x + width, y + depth, z + height], [x + width, y + depth, z], [x + width, y, z]], shade(fill, 34), extra); // 오른쪽 앞면
        face(canvas, [[x, y, z + height], [x + width, y, z + height], [x + width, y + depth, z + height], [x, y + depth, z + height]], shade(fill, 0), extra); // 윗면
    }

    function sortBoxes(boxes) // 뒤에서 앞으로 그리는 순서 정하기
    {
        const depthOf = (box) => box.x + (box.w ?? 1) / 2 + box.y + (box.d ?? 1) / 2; // 화면 깊이
        return boxes.slice().sort((a, b) => depthOf(a) - depthOf(b) || (a.z ?? 0) - (b.z ?? 0)); // 깊이 다음 높이 순서
    }

    function random(seed) // 씨앗으로 같은 순서의 난수 만들기
    {
        let state = 2166136261; // 해시 시작 값
        for (const letter of String(seed)) // 씨앗 글자 순회
        {
            state = Math.imul(state ^ letter.codePointAt(0), 16777619); // 글자 섞기
        }
        return function next() // 다음 난수
        {
            state = state + 0x6D2B79F5 | 0; // 상태 증가
            let mixed = Math.imul(state ^ state >>> 15, 1 | state); // 첫 섞기
            mixed = mixed + Math.imul(mixed ^ mixed >>> 7, 61 | mixed) ^ mixed; // 둘째 섞기
            return ((mixed ^ mixed >>> 14) >>> 0) / 4294967296; // 0과 1 사이 값
        };
    }

    function buildScene(seed, size, names) // 씨앗으로 블록 공간 만들기
    {
        const next = random(seed); // 난수 함수
        const pick = (list) => list[Math.floor(next() * list.length)]; // 목록에서 하나 고르기
        const taken = new Set(); // 사용한 칸
        const boxes = []; // 상자 목록
        const labels = []; // 이름표 목록
        const isFree = (x, y, width, depth) => // 빈 칸 확인
        {
            if (x < 0 || y < 0 || x + width > size || y + depth > size) // 바닥 밖 확인
            {
                return false; // 바닥 밖은 사용 불가
            }
            for (let i = 0; i < width; i++) // 가로 순회
            {
                for (let j = 0; j < depth; j++) // 세로 순회
                {
                    if (taken.has(`${x + i},${y + j}`)) // 사용 여부 확인
                    {
                        return false; // 이미 쓴 칸
                    }
                }
            }
            return true; // 모두 빈 칸
        };
        const take = (x, y, width, depth) => // 칸 사용 표시
        {
            for (let i = 0; i < width; i++) // 가로 순회
            {
                for (let j = 0; j < depth; j++) // 세로 순회
                {
                    taken.add(`${x + i},${y + j}`); // 사용 기록
                }
            }
        };
        const place = (width, depth, allow) => // 빈 자리 찾기
        {
            for (let attempt = 0; attempt < 60; attempt++) // 여러 번 시도
            {
                const x = Math.floor(next() * (size - width + 1)); // 가로 후보
                const y = Math.floor(next() * (size - depth + 1)); // 세로 후보
                if (isFree(x, y, width, depth) && (!allow || allow(x, y))) // 빈 자리와 조건 확인
                {
                    take(x, y, width, depth); // 자리 사용
                    return { x, y }; // 자리 반환
                }
            }
            return null; // 자리 없음
        };
        (names || []).forEach((name, index) => // 방 안 사람 순회
        {
            const spot = place(1, 1, (x, y) => x + y >= size); // 앞쪽 사람 자리
            if (spot) // 자리 확인
            {
                boxes.push({ x: spot.x + 0.3, y: spot.y + 0.3, z: 0, w: 0.4, d: 0.4, h: 0.55, color: BODIES[index % BODIES.length] }); // 몸
                boxes.push({ x: spot.x + 0.24, y: spot.y + 0.24, z: 0.55, w: 0.52, d: 0.52, h: 0.48, color: "paper" }); // 머리
                labels.push({ name, x: spot.x + 0.5, y: spot.y + 0.5, z: 1.45 }); // 이름표
            }
        });
        const crowded = (names || []).length > 0; // 사람이 있는 방 여부
        const buildings = 2 + Math.floor(next() * 2); // 건물 수
        for (let count = 0; count < buildings; count++) // 건물 순회
        {
            const width = 2 + Math.floor(next() * 2); // 건물 가로
            const depth = 2 + Math.floor(next() * 2); // 건물 세로
            const height = 1 + Math.floor(next() * 3); // 건물 층수
            const spot = place(width, depth, crowded ? (x, y) => x + width + y + depth <= size + 1 : null); // 사람을 가리지 않는 건물 자리
            if (!spot) // 자리 확인
            {
                continue; // 자리 없으면 건너뜀
            }
            const wall = pick(WALLS); // 벽 색
            const roof = pick(["clay", "blue", "gold"].filter((color) => color !== wall)); // 지붕 색
            for (let i = 0; i < width; i++) // 가로 순회
            {
                for (let j = 0; j < depth; j++) // 세로 순회
                {
                    for (let k = 0; k < height; k++) // 층 순회
                    {
                        boxes.push({ x: spot.x + i, y: spot.y + j, z: k, color: wall }); // 벽 블록
                    }
                    boxes.push({ x: spot.x + i, y: spot.y + j, z: height, h: 0.35, color: roof }); // 지붕 판
                }
            }
        }
        const trees = 3 + Math.floor(next() * 3); // 나무 수
        for (let count = 0; count < trees; count++) // 나무 순회
        {
            const spot = place(1, 1); // 나무 자리
            if (spot) // 자리 확인
            {
                boxes.push({ x: spot.x + 0.32, y: spot.y + 0.32, z: 0, w: 0.36, d: 0.36, h: 0.9, color: "wood" }); // 나무 줄기
                boxes.push({ x: spot.x + 0.08, y: spot.y + 0.08, z: 0.9, w: 0.84, d: 0.84, h: 0.84, color: "leaf" }); // 나무 잎
            }
        }
        const loose = 2 + Math.floor(next() * 3); // 낱개 블록 수
        for (let count = 0; count < loose; count++) // 낱개 블록 순회
        {
            const spot = place(1, 1); // 블록 자리
            if (spot) // 자리 확인
            {
                const stack = 1 + Math.floor(next() * 2); // 쌓은 수
                const color = pick(WALLS); // 블록 색
                for (let k = 0; k < stack; k++) // 쌓기 순회
                {
                    boxes.push({ x: spot.x, y: spot.y, z: k, color }); // 낱개 블록
                }
            }
        }
        for (let x = 0; x < size; x++) // 길 놓기 순회
        {
            const y = Math.floor(size / 2); // 길 세로 위치
            if (isFree(x, y, 1, 1)) // 빈 칸 확인
            {
                boxes.push({ x: x + 0.06, y: y + 0.06, z: 0, w: 0.88, d: 0.88, h: 0.07, color: "stone" }); // 길 판
            }
        }
        const spare = place(1, 1); // 고른 블록 자리
        const ghost = place(1, 1); // 놓으려는 블록 자리
        return { boxes, labels, spare, ghost }; // 장면 자료 반환
    }

    function drawFloor(canvas, size) // 모눈 바닥 그리기
    {
        face(canvas, [[0, size, 0], [size, size, 0], [size, size, -0.3], [0, size, -0.3]], shade("var(--av-surface)", 10)); // 바닥 왼쪽 옆면
        face(canvas, [[size, 0, 0], [size, size, 0], [size, size, -0.3], [size, 0, -0.3]], shade("var(--av-surface)", 24)); // 바닥 오른쪽 옆면
        face(canvas, [[0, 0, 0], [size, 0, 0], [size, size, 0], [0, size, 0]], "var(--av-paper)"); // 바닥 윗면
        for (let line = 1; line < size; line++) // 모눈 선 순회
        {
            const a = project(canvas, line, 0, 0); // 세로 선 시작
            const b = project(canvas, line, size, 0); // 세로 선 끝
            const c = project(canvas, 0, line, 0); // 가로 선 시작
            const d = project(canvas, size, line, 0); // 가로 선 끝
            canvas.parts.push(`<path d="M${a[0]} ${a[1]}L${b[0]} ${b[1]}M${c[0]} ${c[1]}L${d[0]} ${d[1]}" style="stroke:var(--av-grid);stroke-width:1;fill:none"/>`); // 모눈 선 추가
        }
    }

    function drawLabels(canvas, labels) // 사람 이름표 그리기
    {
        labels.forEach((label) => // 이름표 순회
        {
            const at = project(canvas, label.x, label.y, label.z); // 이름표 위치
            const width = [...label.name].length * 11 + 14; // 이름표 너비
            project(canvas, label.x, label.y, label.z + 0.5); // 위쪽 여유 확보
            canvas.parts.push(`<rect x="${(at[0] - width / 2).toFixed(1)}" y="${(at[1] - 16).toFixed(1)}" width="${width}" height="17" rx="6" style="fill:var(--av-paper);stroke:var(--av-ink);stroke-width:1"/>`); // 이름표 바탕
            canvas.parts.push(`<text x="${at[0].toFixed(1)}" y="${(at[1] - 4).toFixed(1)}" text-anchor="middle" style="fill:var(--av-ink);font:700 11px var(--av-font)">${label.name}</text>`); // 이름 글자
        });
    }

    function drawEditing(canvas, spare, ghost) // 편집 표시 그리기
    {
        if (ghost) // 놓으려는 블록 확인
        {
            drawBox(canvas, { x: ghost.x, y: ghost.y, z: 0, color: "blue" }, ";opacity:0.4;stroke-dasharray:4 3"); // 반투명 블록
        }
        if (!spare) // 고른 블록 확인
        {
            return; // 자리 없으면 건너뜀
        }
        drawBox(canvas, { x: spare.x, y: spare.y, z: 0, color: "gold" }); // 고른 블록
        const outline = [[spare.x, spare.y, 1], [spare.x + 1, spare.y, 1], [spare.x + 1, spare.y + 1, 1], [spare.x + 1, spare.y + 1, 0], [spare.x, spare.y + 1, 0], [spare.x, spare.y + 1, 1]]; // 선택 윤곽 꼭짓점
        canvas.parts.push(`<polygon points="${points(canvas, outline)}" style="fill:none;stroke:var(--av-blue);stroke-width:2.5;stroke-dasharray:6 4;stroke-linejoin:round"/>`); // 선택 윤곽
        const origin = project(canvas, spare.x + 0.5, spare.y + 0.5, 1); // 손잡이 기준점
        const handles = [[spare.x + 2, spare.y + 0.5, 1, "var(--av-clay)"], [spare.x + 0.5, spare.y + 2, 1, "var(--av-leaf)"], [spare.x + 0.5, spare.y + 0.5, 2.4, "var(--av-blue)"]]; // 세 방향 손잡이
        handles.forEach((handle) => // 손잡이 순회
        {
            const tip = project(canvas, handle[0], handle[1], handle[2]); // 손잡이 끝
            canvas.parts.push(`<path d="M${origin[0]} ${origin[1]}L${tip[0]} ${tip[1]}" style="stroke:${handle[3]};stroke-width:3;stroke-linecap:round"/>`); // 손잡이 선
            canvas.parts.push(`<circle cx="${tip[0]}" cy="${tip[1]}" r="5" style="fill:${handle[3]};stroke:var(--av-ink);stroke-width:1"/>`); // 손잡이 끝 점
        });
    }

    function toSvg(canvas, label, padding) // 그림 조각을 SVG로 묶기
    {
        const x = canvas.minX - padding; // 보기 영역 왼쪽
        const y = canvas.minY - padding; // 보기 영역 위쪽
        const width = canvas.maxX - canvas.minX + padding * 2; // 보기 영역 너비
        const height = canvas.maxY - canvas.minY + padding * 2; // 보기 영역 높이
        return `<svg viewBox="${x.toFixed(1)} ${y.toFixed(1)} ${width.toFixed(1)} ${height.toFixed(1)}" role="img" aria-label="${label}" preserveAspectRatio="xMidYMid meet">${canvas.parts.join("")}</svg>`; // SVG 문자열
    }

    function renderScene(element) // 장면 자리 하나 그리기
    {
        const size = Number(element.dataset.avSize) || 8; // 바닥 크기
        const names = element.dataset.avPeople ? element.dataset.avPeople.split(",") : []; // 방 안 사람 이름
        const scene = buildScene(element.dataset.avScene, size, names); // 장면 자료
        const canvas = createCanvas(); // 그림 범위 계산기
        drawFloor(canvas, size); // 바닥 그리기
        sortBoxes(scene.boxes).forEach((box) => drawBox(canvas, box)); // 상자 그리기
        if (element.hasAttribute("data-av-editing")) // 편집 화면 확인
        {
            drawEditing(canvas, scene.spare, scene.ghost); // 편집 표시 그리기
        }
        drawLabels(canvas, scene.labels); // 이름표 그리기
        element.innerHTML = toSvg(canvas, element.dataset.avLabel || "블록으로 만든 공간 미리보기", 12); // 그림 넣기
    }

    function renderAvatar(element) // 기본 캐릭터 하나 그리기
    {
        const kind = Number(element.dataset.avAvatar) || 0; // 캐릭터 종류
        const color = element.dataset.avColor || "gold"; // 캐릭터 색
        const canvas = createCanvas(); // 그림 범위 계산기
        const boxes = [{ x: 0.18, y: 0.18, z: 0, w: 0.64, d: 0.64, h: 0.7, color }, { x: 0, y: 0, z: 0.7, w: 1, d: 1, h: 0.9, color: "paper" }]; // 몸과 머리
        const extras = // 종류별 머리 장식
        [
            [], // 0번: 장식 없음
            [{ x: -0.05, y: -0.05, z: 1.6, w: 1.1, d: 1.1, h: 0.2, color }], // 1번: 납작 모자
            [{ x: 0.05, y: 0.62, z: 1.6, w: 0.32, d: 0.32, h: 0.32, color }, { x: 0.62, y: 0.05, z: 1.6, w: 0.32, d: 0.32, h: 0.32, color }], // 2번: 두 귀
            [{ x: 0.43, y: 0.43, z: 1.6, w: 0.14, d: 0.14, h: 0.4, color: "wood" }, { x: 0.34, y: 0.34, z: 2, w: 0.32, d: 0.32, h: 0.32, color }], // 3번: 더듬이
            [{ x: 0.1, y: 0.1, z: 1.6, w: 0.8, d: 0.8, h: 0.14, color: "gold" }, { x: 0.38, y: 0.38, z: 1.74, w: 0.24, d: 0.24, h: 0.3, color: "gold" }], // 4번: 왕관
            [{ x: 0.2, y: 0.2, z: 1.6, w: 0.6, d: 0.6, h: 0.5, color }, { x: 0.36, y: 0.36, z: 2.1, w: 0.28, d: 0.28, h: 0.2, color: "paper" }] // 5번: 털모자
        ]; // 머리 장식 끝
        boxes.concat(extras[kind % extras.length]).forEach((box) => drawBox(canvas, box)); // 몸·머리·장식 그리기
        [[1, 0.3, 1.22], [1, 0.7, 1.22]].forEach((eye) => // 두 눈 순회
        {
            const at = project(canvas, eye[0], eye[1], eye[2]); // 눈 위치
            canvas.parts.push(`<circle cx="${at[0].toFixed(1)}" cy="${at[1].toFixed(1)}" r="2.4" style="fill:var(--av-ink)"/>`); // 눈 점
        });
        element.innerHTML = toSvg(canvas, "", 3).replace(' role="img" aria-label=""', ' aria-hidden="true"'); // 장식 그림으로 넣기
    }

    function renderBlock(element) // 부품 아이콘 하나 그리기
    {
        const color = element.dataset.avColor || "gold"; // 부품 색
        const shapes = // 모양별 상자 목록
        {
            cube: [{ x: 0, y: 0, z: 0, color }], // 정육면체
            slab: [{ x: 0, y: 0, z: 0, h: 0.3, color }], // 납작한 판
            pillar: [{ x: 0.3, y: 0.3, z: 0, w: 0.4, d: 0.4, h: 1.4, color }], // 기둥
            wall: [{ x: 0, y: 0.38, z: 0, d: 0.24, h: 1.2, color }], // 벽
            stairs: [{ x: 0, y: 0, z: 0, h: 0.34, color }, { x: 0, y: 0, z: 0.34, d: 0.66, h: 0.33, color }, { x: 0, y: 0, z: 0.67, d: 0.33, h: 0.33, color }], // 계단
            tree: [{ x: 0.32, y: 0.32, z: 0, w: 0.36, d: 0.36, h: 0.9, color: "wood" }, { x: 0.08, y: 0.08, z: 0.9, w: 0.84, d: 0.84, h: 0.84, color: "leaf" }], // 나무
            chair: [{ x: 0.1, y: 0.1, z: 0, w: 0.8, d: 0.8, h: 0.45, color }, { x: 0.1, y: 0.1, z: 0.45, w: 0.8, d: 0.2, h: 0.6, color }], // 의자
            table: [{ x: 0.4, y: 0.4, z: 0, w: 0.2, d: 0.2, h: 0.6, color: "wood" }, { x: 0, y: 0, z: 0.6, h: 0.16, color }], // 탁자
            house: [{ x: 0, y: 0, z: 0, h: 0.8, color: "paper" }, { x: -0.06, y: -0.06, z: 0.8, w: 1.12, d: 1.12, h: 0.3, color }], // 작은 집
            lamp: [{ x: 0.44, y: 0.44, z: 0, w: 0.12, d: 0.12, h: 1.1, color: "wood" }, { x: 0.3, y: 0.3, z: 1.1, w: 0.4, d: 0.4, h: 0.36, color }] // 등불
        }; // 모양 목록 끝
        const canvas = createCanvas(); // 그림 범위 계산기
        (shapes[element.dataset.avBlock] || shapes.cube).forEach((box) => drawBox(canvas, box)); // 상자 그리기
        element.innerHTML = toSvg(canvas, "", 3).replace(' role="img" aria-label=""', ' aria-hidden="true"'); // 장식 그림으로 넣기
    }

    function refresh() // 아직 그리지 않은 자리 모두 그리기
    {
        document.querySelectorAll("[data-av-block]:not([data-av-done])").forEach((element) => // 부품 아이콘 자리 순회
        {
            renderBlock(element); // 부품 아이콘 그리기
            element.dataset.avDone = "1"; // 그림 완료 표시
        });
        document.querySelectorAll("[data-av-scene]:not([data-av-done])").forEach((element) => // 장면 자리 순회
        {
            renderScene(element); // 장면 그리기
            element.dataset.avDone = "1"; // 그림 완료 표시
        });
        document.querySelectorAll("[data-av-avatar]:not([data-av-done])").forEach((element) => // 아바타 자리 순회
        {
            renderAvatar(element); // 아바타 그리기
            element.dataset.avDone = "1"; // 그림 완료 표시
        });
    }

    window.AtelierScene = { refresh }; // 다른 모듈에 공개
    refresh(); // 처음 한 번 그리기
})(); // 블록 장면 모듈 끝
