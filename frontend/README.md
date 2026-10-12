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

The current API endpoint returns extracted CV text. ATS scoring/analysis is not yet connected; the success screen explicitly identifies that follow-up instead of inventing a score. The API currently returns extracted text in its response; review API response handling and data-retention policy before exposing this service to production users.
