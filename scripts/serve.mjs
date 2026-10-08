// 화면 시안(design 폴더)을 브라우저에서 보기 위한 미리보기 서버
import { createServer } from "node:http"; // HTTP 서버
import { createReadStream, existsSync, statSync } from "node:fs"; // 파일 읽기
import { extname, join, normalize } from "node:path"; // 경로 처리
import { fileURLToPath } from "node:url"; // 파일 주소 변환

const ROOT = normalize(join(fileURLToPath(new URL(".", import.meta.url)), "..", "design")); // 시안 폴더
const PORT = Number(process.env.PORT) || 3100; // 서버 포트
const TYPES = // 확장자별 내용 형식
{
    ".html": "text/html; charset=utf-8", // HTML 문서
    ".css": "text/css; charset=utf-8", // 스타일
    ".js": "text/javascript; charset=utf-8", // 스크립트
    ".svg": "image/svg+xml", // SVG 그림
    ".png": "image/png", // PNG 그림
    ".webp": "image/webp", // WebP 그림
    ".json": "application/json; charset=utf-8" // JSON 자료
}; // 내용 형식 끝

createServer((request, response) => // 요청 처리
{
    const pathname = decodeURIComponent(new URL(request.url, "http://localhost").pathname); // 요청 경로
    const file = normalize(join(ROOT, pathname === "/" ? "index.html" : pathname)); // 파일 경로
    if (!file.startsWith(ROOT) || !existsSync(file) || !statSync(file).isFile()) // 시안 폴더 안의 파일인지 확인
    {
        response.writeHead(404, { "content-type": "text/plain; charset=utf-8" }); // 없음 응답
        response.end("찾을 수 없습니다."); // 없음 문구
        return; // 처리 끝
    }
    response.writeHead(200, { "content-type": TYPES[extname(file)] ?? "application/octet-stream", "cache-control": "no-store" }); // 정상 응답
    createReadStream(file).pipe(response); // 파일 전송
}).listen(PORT, "127.0.0.1", () => console.log(`시안 미리보기: http://127.0.0.1:${PORT}/`)); // 서버 시작
