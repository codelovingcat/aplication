import { AnalysisError, type ResumeAnalysisResponse } from "../types/analysis";
import type { ProblemDetails } from "../types/intake";

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? "").replace(/\/$/, "");

function safeError(response: Response, problem: ProblemDetails): AnalysisError {
  const code = problem.errorCode ?? "";

  if (response.status === 404 || code === "resume_not_found") {
    return new AnalysisError("Bu CV kaydı bulunamadı. Yeni bir CV yükleyip tekrar deneyebilirsin.", "not-found", response.status);
  }
  if (response.status === 409 || code === "resume_not_ready") {
    return new AnalysisError("CV henüz analize hazır değil. Biraz bekleyip yeniden deneyebilirsin.", "not-ready", response.status);
  }
  if (response.status === 429 || code === "rate_limited") {
    return new AnalysisError("Çok sık istek gönderildi. Lütfen biraz bekleyip yeniden dene.", "network", response.status);
  }

  // Never expose raw problem details, stack traces, or backend exception messages.
  return new AnalysisError("CV şu anda analiz edilemedi. Lütfen biraz sonra yeniden dene.", "unexpected", response.status);
}

function isStringArray(value: unknown): value is string[] {
  return Array.isArray(value) && value.every(item => typeof item === "string");
}

function parseAnalysis(value: unknown): ResumeAnalysisResponse {
  if (typeof value !== "object" || value === null) {
    throw new AnalysisError("Sunucudan beklenen analiz yanıtı alınamadı. Lütfen yeniden dene.", "unexpected");
  }

  const response = value as Record<string, unknown>;
  if (
    typeof response.resumeId !== "string" ||
    typeof response.status !== "string" ||
    typeof response.score !== "number" ||
    !Number.isFinite(response.score) ||
    response.score < 0 ||
    response.score > 100 ||
    !isStringArray(response.strengths) ||
    !isStringArray(response.issues) ||
    !isStringArray(response.recommendations) ||
    !isStringArray(response.sectionFindings) ||
    !isStringArray(response.keywordFindings)
  ) {
    throw new AnalysisError("Sunucudan beklenen analiz yanıtı alınamadı. Lütfen yeniden dene.", "unexpected");
  }

  return response as ResumeAnalysisResponse;
}

export async function analyzeResume(
  resumeId: string,
  signal: AbortSignal
): Promise<ResumeAnalysisResponse> {
  if (!resumeId.trim()) {
    throw new AnalysisError("CV kimliği eksik. Yeni bir CV yükleyip tekrar deneyebilirsin.", "not-found");
  }

  let response: Response;
  try {
    response = await fetch(
      API_BASE_URL + "/api/cv/" + encodeURIComponent(resumeId) + "/analyze",
      {
        method: "POST",
        headers: { Accept: "application/json" },
        signal
      }
    );
  } catch (cause) {
    if (signal.aborted || (cause instanceof DOMException && cause.name === "AbortError")) {
      throw cause;
    }
    throw new AnalysisError("Sunucuya ulaşılamadı. İnternet bağlantını kontrol edip yeniden dene.", "network");
  }

  if (!response.ok) {
    let problem: ProblemDetails = {};
    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      // Use a safe status-based message if the server did not return ProblemDetails.
    }
    throw safeError(response, problem);
  }

  try {
    return parseAnalysis(await response.json());
  } catch (cause) {
    if (cause instanceof AnalysisError) throw cause;
    throw new AnalysisError("Sunucudan beklenen analiz yanıtı alınamadı. Lütfen yeniden dene.", "unexpected", response.status);
  }
}
