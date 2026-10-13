export type ResumeAnalysisResponse = {
  resumeId: string;
  status: string;
  score: number;
  strengths: string[];
  issues: string[];
  recommendations: string[];
  sectionFindings: string[];
  keywordFindings: string[];
};

export type AnalysisFailureKind =
  | "not-found"
  | "not-ready"
  | "network"
  | "unexpected";

export class AnalysisError extends Error {
  constructor(
    message: string,
    public readonly kind: AnalysisFailureKind,
    public readonly status?: number
  ) {
    super(message);
    this.name = "AnalysisError";
  }
}
