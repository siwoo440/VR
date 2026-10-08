(function () // 공통 틀 모듈
{
    const STORAGE_KEY = "av-color-mode"; // 화면 모드 저장 이름
    const ICONS = // 아이콘 그림 모음
    {
        studio: '<path d="M4 11l8-6 8 6v8a1 1 0 0 1-1 1h-4v-6H9v6H5a1 1 0 0 1-1-1z"/>', // 내 작업실
        compass: '<circle cx="12" cy="12" r="9"/><path d="M15.5 8.5l-2 5-5 2 2-5z"/>', // 둘러보기
        bell: '<path d="M6 16v-5a6 6 0 0 1 12 0v5l1.5 2h-15zM10 20a2 2 0 0 0 4 0"/>', // 소식
        chat: '<path d="M4 6a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2h-7l-4 4v-4H6a2 2 0 0 1-2-2z"/>', // 메시지
        bag: '<path d="M5 8h14l-1 12H6zM9 8V6a3 3 0 0 1 6 0v2"/>', // 상점
        user: '<circle cx="12" cy="8" r="4"/><path d="M4 20a8 8 0 0 1 16 0"/>', // 프로필
        grid: '<path d="M4 4h7v7H4zM13 4h7v7h-7zM4 13h7v7H4zM13 13h7v7h-7z"/>', // 시안 목록
        search: '<circle cx="11" cy="11" r="6"/><path d="M20 20l-4.5-4.5"/>', // 검색
        plus: '<path d="M12 5v14M5 12h14"/>', // 더하기
        link: '<path d="M10 14a4 4 0 0 0 5.7 0l3-3a4 4 0 0 0-5.7-5.7l-1 1M14 10a4 4 0 0 0-5.7 0l-3 3a4 4 0 0 0 5.7 5.7l1-1"/>', // 초대 링크
        heart: '<path d="M12 20s-7-4.4-7-10a4 4 0 0 1 7-2.5A4 4 0 0 1 19 10c0 5.6-7 10-7 10z"/>', // 좋아요
        copy: '<path d="M9 9h10v10H9zM5 15V5h10"/>', // 리믹스·복사
        flag: '<path d="M5 21V4M5 5h11l-2 4 2 4H5"/>', // 신고
        users: '<circle cx="9" cy="9" r="3.5"/><path d="M2.5 20a6.5 6.5 0 0 1 13 0M16 5.5a3.5 3.5 0 0 1 0 7M18 14.5a6.5 6.5 0 0 1 3.5 5.5"/>', // 참여자
        mic: '<path d="M12 4a3 3 0 0 1 3 3v4a3 3 0 0 1-6 0V7a3 3 0 0 1 3-3zM6 11a6 6 0 0 0 12 0M12 17v3"/>', // 마이크
        smile: '<circle cx="12" cy="12" r="9"/><path d="M8.5 14a4 4 0 0 0 7 0M9 10h.01M15 10h.01"/>', // 감정 표현
        camera: '<path d="M4 8h3l1.5-2h7L17 8h3v11H4z"/><circle cx="12" cy="13" r="3.5"/>', // 사진
        vr: '<path d="M3 9a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v6a2 2 0 0 1-2 2h-3.5l-2-2.5h-3L8.5 17H5a2 2 0 0 1-2-2z"/><path d="M7.5 12h.01M16.5 12h.01"/>', // VR 헤드셋
        exit: '<path d="M10 4H5v16h5M14 8l4 4-4 4M18 12H9"/>', // 나가기
        gear: '<circle cx="12" cy="12" r="3"/><path d="M12 3v3M12 18v3M3 12h3M18 12h3M5.6 5.6l2.1 2.1M16.3 16.3l2.1 2.1M5.6 18.4l2.1-2.1M16.3 7.7l2.1-2.1"/>', // 설정
        undo: '<path d="M9 7l-4 4 4 4M5 11h9a5 5 0 0 1 0 10h-3"/>', // 실행 취소
        redo: '<path d="M15 7l4 4-4 4M19 11h-9a5 5 0 0 0 0 10h3"/>', // 다시 실행
        play: '<path d="M8 5l11 7-11 7z"/>', // 걸어 보기
        cursor: '<path d="M6 4l12 7-5 1.5L11 18z"/>', // 선택 도구
        cube: '<path d="M12 3l8 4.5v9L12 21l-8-4.5v-9zM4 7.5l8 4.5 8-4.5M12 12v9"/>', // 부품
        move: '<path d="M12 3v18M3 12h18M12 3l-3 3M12 3l3 3M12 21l-3-3M12 21l3-3M3 12l3-3M3 12l3 3M21 12l-3-3M21 12l-3 3"/>', // 이동 도구
        rotate: '<path d="M20 12a8 8 0 1 1-2.6-5.9M20 4v4h-4"/>', // 회전 도구
        scale: '<path d="M4 20h7M4 20v-7M4 20l7-7M20 4h-7M20 4v7M20 4l-7 7"/>', // 크기 도구
        brush: '<path d="M15 4l5 5-9 9H6v-5zM13 6l5 5"/>', // 칠하기 도구
        eraser: '<path d="M4 16l9-9 6 6-6 6H8zM9 20h11"/>', // 지우기 도구
        sun: '<circle cx="12" cy="12" r="4"/><path d="M12 2v2.5M12 19.5V22M2 12h2.5M19.5 12H22M4.9 4.9l1.8 1.8M17.3 17.3l1.8 1.8M4.9 19.1l1.8-1.8M17.3 6.7l1.8-1.8"/>', // 밝은 화면
        moon: '<path d="M20 14.5A8 8 0 0 1 9.5 4 8 8 0 1 0 20 14.5z"/>', // 어두운 화면
        check: '<path d="M5 12.5l4.5 4.5L19 7.5"/>', // 확인
        lock: '<path d="M6 11h12v9H6zM8.5 11V8a3.5 3.5 0 0 1 7 0v3"/>', // 비공개
        globe: '<circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3a14 14 0 0 1 0 18M12 3a14 14 0 0 0 0 18"/>', // 공개
        star: '<path d="M12 4l2.5 5.2 5.7.8-4.1 4 1 5.7L12 17l-5.1 2.7 1-5.7-4.1-4 5.7-.8z"/>', // 팔로우
        send: '<path d="M4 12l16-7-6 16-3-7z"/>', // 보내기
        more: '<circle cx="6" cy="12" r="1.2"/><circle cx="12" cy="12" r="1.2"/><circle cx="18" cy="12" r="1.2"/>', // 더 보기
        megaphone: '<path d="M4 10v4h3l8 4V6l-8 4zM18 9a4 4 0 0 1 0 6"/>', // 공지
        crown: '<path d="M4 18h16M5 15L4 7l5 4 3-6 3 6 5-4-1 8z"/>', // 구독
        right: '<path d="M9 6l6 6-6 6"/>', // 오른쪽 화살표
        left: '<path d="M15 6l-6 6 6 6"/>', // 왼쪽 화살표
        close: '<path d="M6 6l12 12M18 6L6 18"/>', // 닫기
        eye: '<path d="M2.5 12s3.5-6.5 9.5-6.5 9.5 6.5 9.5 6.5-3.5 6.5-9.5 6.5S2.5 12 2.5 12z"/><circle cx="12" cy="12" r="2.5"/>', // 방문 수
        save: '<path d="M5 4h11l3 3v13H5zM8 4v5h7V4M8 20v-6h8v6"/>', // 저장
        layers: '<path d="M12 4l8 4-8 4-8-4zM4 12l8 4 8-4M4 16l8 4 8-4"/>', // 조립품
        bolt: '<path d="M13 3L5 13h6l-1 8 8-10h-6z"/>' // 동작 규칙
    }; // 아이콘 그림 모음 끝
    const NAV = // 주요 메뉴
    [
        { id: "studio", href: "studio.html", icon: "studio", label: "내 작업실" }, // 첫 화면
        { id: "explore", href: "explore.html", icon: "compass", label: "둘러보기" }, // 공개 맵 목록
        { id: "feed", href: "feed.html", icon: "bell", label: "소식", count: 3 }, // 소식 피드
        { id: "messages", href: "messages.html", icon: "chat", label: "메시지", count: 2 }, // 대화
        { id: "shop", href: "shop.html", icon: "bag", label: "상점" } // 상점과 마켓
    ]; // 주요 메뉴 끝
    const FOOT = // 아래 메뉴
    [
        { id: "plans", href: "plans.html", icon: "crown", label: "구독" }, // 요금제
        { id: "profile", href: "profile.html", icon: "user", label: "프로필" }, // 내 정보
        { id: "index", href: "index.html", icon: "grid", label: "시안 목록" } // 시안 첫 페이지
    ]; // 아래 메뉴 끝

    function readMode() // 저장된 화면 모드 읽기
    {
        try // 저장소 접근 시도
        {
            const saved = localStorage.getItem(STORAGE_KEY); // 저장 값
            if (saved === "light" || saved === "dark") // 올바른 값 확인
            {
                return saved; // 저장 값 사용
            }
        }
        catch (error) // 저장소 사용 불가
        {
            // 기기 설정으로 넘어감
        }
        return matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light"; // 기기 설정 사용
    }

    function applyMode(mode) // 화면 모드 적용
    {
        document.documentElement.dataset.colorMode = mode; // 문서 뿌리에 기록
        document.querySelectorAll("[data-av-mode]").forEach((button) => // 전환 버튼 순회
        {
            const next = mode === "dark" ? "밝은" : "어두운"; // 바꿀 모드 이름
            button.setAttribute("aria-label", `${next} 화면으로 바꾸기`); // 버튼 설명
            button.innerHTML = `<span class="av-icon">${iconMarkup(mode === "dark" ? "sun" : "moon")}</span>`; // 버튼 아이콘
        });
    }

    function iconMarkup(name) // 아이콘 그림 만들기
    {
        return `<svg viewBox="0 0 24 24" aria-hidden="true">${ICONS[name] || ICONS.cube}</svg>`; // SVG 문자열
    }

    function fillIcons(root) // 아이콘 자리 채우기
    {
        root.querySelectorAll(".av-icon[data-i]").forEach((holder) => // 아이콘 자리 순회
        {
            holder.innerHTML = iconMarkup(holder.dataset.i); // 그림 넣기
            holder.setAttribute("aria-hidden", "true"); // 낭독 제외
        });
    }

    function navLink(item, current, className, labelClass) // 메뉴 항목 만들기
    {
        const active = item.id === current ? ' aria-current="page"' : ""; // 현재 화면 표시
        const count = item.count ? `<span class="av-badge av-nav-count">${item.count}<span class="av-sr">개 새 소식</span></span>` : ""; // 새 소식 수
        return `<a class="${className}" href="${item.href}"${active} title="${item.label}"><span class="av-icon" data-i="${item.icon}"></span><span class="${labelClass}">${item.label}</span>${className === "av-nav-link" ? count : ""}</a>`; // 항목 문자열
    }

    function buildShell() // 공통 틀 만들기
    {
        const body = document.body; // 문서 본문
        if (body.dataset.avShell !== "app") // 공통 틀 사용 여부
        {
            return; // 전체 화면 페이지는 건너뜀
        }
        const main = document.querySelector("main"); // 화면 본문
        const current = body.dataset.avPage || ""; // 현재 화면 이름
        const title = body.dataset.avTitle || document.title; // 화면 제목
        const app = document.createElement("div"); // 앱 전체 틀
        app.className = "av-app"; // 틀 이름
        app.innerHTML = // 틀 내용
            `<aside class="av-rail" aria-label="주요 메뉴">` + // 옆 메뉴 시작
            `<a class="av-brand" href="studio.html"><span class="av-brand-mark"></span><span class="av-brand-name" translate="no">Atelier | Verse</span></a>` + // 서비스 이름
            `<nav class="av-nav">${NAV.map((item) => navLink(item, current, "av-nav-link", "av-nav-label")).join("")}</nav>` + // 주요 메뉴
            `<div class="av-rail-foot">${FOOT.map((item) => navLink(item, current, "av-nav-link", "av-nav-label")).join("")}</div>` + // 아래 메뉴
            `</aside>` + // 옆 메뉴 끝
            `<div class="av-body">` + // 본문 열 시작
            `<p class="av-draft"><span class="av-stage">시안</span>실제 서비스가 아닌 화면 시안이며, 표시된 이름·숫자·금액은 모두 예시입니다.</p>` + // 시안 안내
            `<header class="av-topbar">` + // 상단 띠 시작
            `<h1 class="av-topbar-title">${title}</h1>` + // 화면 제목
            `<label class="av-search"><span class="av-sr">맵과 제작자 검색</span><span class="av-icon" data-i="search"></span><input class="av-input" type="search" placeholder="맵, 조립품, 제작자 검색"></label>` + // 검색칸
            `<a class="av-button av-button-quiet av-button-icon" href="feed.html" aria-label="알림 3개"><span class="av-icon" data-i="bell"></span></a>` + // 알림 버튼
            `<button class="av-button av-button-quiet av-button-icon" type="button" data-av-mode></button>` + // 화면 모드 버튼
            `<a class="av-avatar" href="profile.html" aria-label="내 프로필" data-av-avatar="1" data-av-color="gold" data-status="online"></a>` + // 내 아바타
            `</header>` + // 상단 띠 끝
            `</div>`; // 본문 열 끝
        body.prepend(app); // 틀을 본문 맨 앞에 추가
        app.querySelector(".av-body").append(main); // 화면 본문 옮기기
        main.classList.add("av-main"); // 본문 영역 이름
        const tabbar = document.createElement("nav"); // 휴대폰 아래 메뉴
        tabbar.className = "av-tabbar"; // 아래 메뉴 이름
        tabbar.setAttribute("aria-label", "주요 메뉴"); // 아래 메뉴 설명
        tabbar.innerHTML = NAV.map((item) => navLink(item, current, "av-tabbar-link", "av-tabbar-label")).join(""); // 아래 메뉴 항목
        body.append(tabbar); // 아래 메뉴 추가
    }

    function bindSets() // 고르기 묶음 연결
    {
        document.addEventListener("click", (event) => // 누르기 감지
        {
            const button = event.target.closest("[data-av-set]"); // 고르기 버튼
            if (!button) // 대상 확인
            {
                return; // 다른 누르기는 건너뜀
            }
            const name = button.dataset.avSet; // 묶음 이름
            const attribute = button.getAttribute("role") === "tab" ? "aria-selected" : "aria-pressed"; // 상태 속성
            document.querySelectorAll(`[data-av-set="${name}"]`).forEach((other) => // 같은 묶음 순회
            {
                other.setAttribute(attribute, String(other === button)); // 고른 것만 켜기
            });
            document.querySelectorAll(`[data-av-when^="${name}="]`).forEach((panel) => // 연결된 내용 순회
            {
                panel.hidden = panel.dataset.avWhen !== `${name}=${button.dataset.avValue}`; // 맞는 내용만 표시
            });
        });
    }

    function bindToggles() // 켜고 끄기 연결
    {
        document.addEventListener("click", (event) => // 누르기 감지
        {
            const toggle = event.target.closest("[data-av-toggle]"); // 켜고 끄는 버튼
            if (toggle) // 대상 확인
            {
                toggle.setAttribute("aria-pressed", String(toggle.getAttribute("aria-pressed") !== "true")); // 상태 뒤집기
            }
            const opener = event.target.closest("[data-av-open]"); // 대화상자 여는 버튼
            if (opener) // 대상 확인
            {
                document.getElementById(opener.dataset.avOpen)?.showModal(); // 대화상자 열기
            }
            const closer = event.target.closest("[data-av-close]"); // 대화상자 닫는 버튼
            if (closer) // 대상 확인
            {
                closer.closest("dialog")?.close(); // 대화상자 닫기
            }
            const toaster = event.target.closest("[data-av-toast]"); // 알림 띠 띄우는 버튼
            if (toaster) // 대상 확인
            {
                showToast(toaster.dataset.avToast); // 알림 띠 표시
            }
        });
    }

    function showToast(message) // 알림 띠 표시
    {
        const toast = document.createElement("p"); // 알림 띠
        toast.className = "av-toast"; // 알림 띠 이름
        toast.setAttribute("role", "status"); // 낭독 알림
        toast.style.cssText = "position:fixed;left:50%;bottom:5.5rem;z-index:20;transform:translateX(-50%)"; // 화면 아래 가운데
        toast.textContent = message; // 알림 문구
        (document.querySelector("dialog[open]") || document.body).append(toast); // 열린 대화상자 위에 표시
        setTimeout(() => toast.remove(), 2200); // 잠시 뒤 제거
    }

    function bindMode() // 화면 모드 버튼 연결
    {
        document.addEventListener("click", (event) => // 누르기 감지
        {
            if (!event.target.closest("[data-av-mode]")) // 모드 버튼 확인
            {
                return; // 다른 누르기는 건너뜀
            }
            const next = document.documentElement.dataset.colorMode === "dark" ? "light" : "dark"; // 바꿀 모드
            try // 저장 시도
            {
                localStorage.setItem(STORAGE_KEY, next); // 고른 모드 저장
            }
            catch (error) // 저장소 사용 불가
            {
                // 이번 화면에서만 적용
            }
            applyMode(next); // 모드 적용
        });
    }

    document.documentElement.dataset.colorMode = readMode(); // 첫 그리기 전 모드 적용
    document.addEventListener("DOMContentLoaded", () => // 문서 준비 뒤 실행
    {
        buildShell(); // 공통 틀 만들기
        fillIcons(document); // 아이콘 채우기
        applyMode(document.documentElement.dataset.colorMode); // 모드 버튼 그리기
        bindSets(); // 고르기 묶음 연결
        bindToggles(); // 켜고 끄기 연결
        bindMode(); // 모드 버튼 연결
        window.AtelierScene?.refresh(); // 새로 생긴 아바타 그리기
    });
})(); // 공통 틀 모듈 끝
