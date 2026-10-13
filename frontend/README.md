# ATS CV Frontend

React + TypeScript + Vite user interface for the CV intake flow.

## Requirements

- Node.js 20 or later
- The ASP.NET API from \`src/AtsCv.Api\`

## Run locally

\`\`\`bash
cd frontend
npm install
cp .env.example .env
npm run dev
\`\`\`

The Vite development server proxies \`/api\` requests to \`VITE_DEV_API_TARGET\` (default \`http://localhost:5000\`). Set \`VITE_API_BASE_URL\` only when the API must be called at a separate origin. Configure the ASP.NET API's CORS policy if you use a separate origin.

## Build

\`\`\`bash
npm run build
\`\`\`

## Upload lifecycle

- Client-side PDF/DOCX and 10 MiB size validation
- Upload progress, processing status and cancellation
- Safe mapping of the API's ProblemDetails error codes
- Retry after recoverable errors without refreshing the page
- Success route after the API confirms extraction and creates draft IDs

After upload, the frontend requests `POST /api/cv/{resumeId}/analyze` and renders the persisted analysis response. The findings page includes the heuristic score, strengths, issues, recommendations, section findings, and keyword findings, plus loading, safe error, retry, and empty states. The score is not a hiring probability.

## Test and build

Run `npm ci`, `npm run build`, and `npm test` from this directory.
