import {
  MAX_ADDITIONAL_INFORMATION,
  MAX_FILE_BYTES,
  UploadError,
  type ProblemDetails,
  type UploadSuccess
} from "../types/intake";

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? "").replace(/\/$/, "");

function validateFile(file: File): void {
  const extension = file.name.slice(file.name.lastIndexOf(".")).toLowerCase();

  if (![".pdf", ".docx"].includes(extension)) {
    throw new UploadError("Yalnızca PDF veya DOCX dosyası yükleyebilirsin.", "unsupported");
  }
  if (file.size === 0) {
    throw new UploadError("Seçtiğin dosya boş. Lütfen başka bir dosya seç.", "validation");
  }
  if (file.size > MAX_FILE_BYTES) {
    throw new UploadError("Dosya boyutu 10 MB sınırını aşamaz.", "too-large", 413);
  }
}

async function readProblem(response: Response): Promise<ProblemDetails> {
  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return {};
  }
}

function mapProblem(response: Response, problem: ProblemDetails): UploadError {
  const code = problem.errorCode ?? "";
  if (response.status === 413 || code === "file_too_large") {
    return new UploadError("Dosya boyutu 10 MB sınırını aşıyor. Daha küçük bir dosya seç.", "too-large", response.status);
  }
  if (code === "unsupported_file_type") {
    return new UploadError("Dosyanın içeriği desteklenen PDF veya DOCX biçimiyle eşleşmiyor.", "unsupported", response.status);
  }
  if (code === "extraction_failed" || response.status === 422) {
    return new UploadError("CV okunamadı. Dosyanın bozulmadığından emin olup yeniden dene.", "extraction", response.status);
  }
  if (code === "rate_limited" || response.status === 429) {
    return new UploadError("Çok sık deneme yapıldı. Lütfen biraz bekleyip yeniden dene.", "rate-limited", response.status);
  }
  if (response.status === 400 || code === "validation_error") {
    const firstError = problem.errors?.[0];
    return new UploadError(firstError || "Dosya bilgileri doğrulanamadı. Dosyanı kontrol edip yeniden dene.", "validation", response.status);
  }
  // Never render raw response bodies or server exception details in the UI.
  return new UploadError("CV şu anda işlenemedi. Lütfen biraz sonra yeniden dene.", "unexpected", response.status);
}

export function uploadCv(
  file: File,
  additionalInformation: string,
  signal: AbortSignal,
  onProgress: (percent: number) => void
): Promise<UploadSuccess> {
  validateFile(file);
  if (additionalInformation.length > MAX_ADDITIONAL_INFORMATION) {
    throw new UploadError("Ek bilgi en fazla 2.000 karakter olabilir.", "validation");
  }

  return new Promise((resolve, reject) => {
    const request = new XMLHttpRequest();
    const body = new FormData();
    body.append("File", file);
    body.append("AdditionalInformation", additionalInformation);

    const abort = () => request.abort();
    signal.addEventListener("abort", abort, { once: true });

    request.upload.addEventListener("progress", (event) => {
      if (event.lengthComputable) onProgress(Math.round((event.loaded / event.total) * 100));
    });
    request.addEventListener("load", () => {
      signal.removeEventListener("abort", abort);
      if (request.status >= 200 && request.status < 300) {
        try {
          resolve(JSON.parse(request.responseText) as UploadSuccess);
        } catch {
          reject(new UploadError("Sunucudan beklenen yanıt alınamadı. Lütfen yeniden dene.", "unexpected", request.status));
        }
        return;
      }
      let problem: ProblemDetails = {};
      try { problem = JSON.parse(request.responseText) as ProblemDetails; } catch { /* Use safe fallback. */ }
      reject(mapProblem(new Response(null, { status: request.status }), problem));
    });
    request.addEventListener("error", () => {
      signal.removeEventListener("abort", abort);
      reject(new UploadError("Sunucuya ulaşılamadı. İnternet bağlantını kontrol edip yeniden dene.", "network"));
    });
    request.addEventListener("timeout", () => {
      signal.removeEventListener("abort", abort);
      reject(new UploadError("Yükleme zaman aşımına uğradı. Lütfen yeniden dene.", "network"));
    });
    request.addEventListener("abort", () => {
      signal.removeEventListener("abort", abort);
      reject(new DOMException("Yükleme iptal edildi.", "AbortError"));
    });

    request.timeout = 120_000;
    request.open("POST", API_BASE_URL + "/api/cv/upload");
    request.send(body);
  });
}