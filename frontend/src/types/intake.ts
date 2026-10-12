export type UploadSuccess = {
  candidateProfileId: string;
  resumeId: string;
  fileName: string;
  extractedText: string;
  additionalInformation: string | null;
};

export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errorCode?: string;
  errors?: string[];
};

export type UploadFailureKind =
  | "validation"
  | "unsupported"
  | "too-large"
  | "extraction"
  | "rate-limited"
  | "network"
  | "unexpected";

export class UploadError extends Error {
  constructor(
    message: string,
    public readonly kind: UploadFailureKind,
    public readonly status?: number
  ) {
    super(message);
    this.name = "UploadError";
  }
}

export const MAX_FILE_BYTES = 10 * 1024 * 1024;
export const MAX_ADDITIONAL_INFORMATION = 2000;
export const ACCEPTED_EXTENSIONS = [".pdf", ".docx"];