import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import App from "./App";

const uploaded = {
  candidateProfileId: "candidate-123",
  resumeId: "resume-456",
  fileName: "cv.pdf",
  extractedText: "Extracted CV text.",
  additionalInformation: null
};

const successfulAnalysis = {
  resumeId: "resume-456",
  status: "Analyzed",
  score: 84,
  strengths: ["A clear experience section was detected."],
  issues: [],
  recommendations: ["Consider adding a professional profile URL."],
  sectionFindings: ["Found: Experience (10/10 points)."],
  keywordFindings: ["Job-specific keyword matching was not performed."]
};

function makeResponse(body: unknown, status = 200): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => body
  } as Response;
}

function renderAnalysisPage() {
  return render(
    <MemoryRouter initialEntries={[{ pathname: "/analysis", state: { result: uploaded } }]}>
      <App />
    </MemoryRouter>
  );
}

describe("ATS findings page", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("shows an accessible loading state while analysis is pending", () => {
    vi.mocked(fetch).mockReturnValue(new Promise<Response>(() => {}));
    renderAnalysisPage();

    expect(screen.getByRole("status").textContent).toContain("CV analiz ediliyor");
  });

  it("renders API findings and explicit empty states from the response", async () => {
    vi.mocked(fetch).mockResolvedValue(makeResponse(successfulAnalysis));
    renderAnalysisPage();

    expect(await screen.findByRole("meter", { name: "ATS okunabilirlik puanı" })).toBeTruthy();
    expect(screen.getByRole("meter").getAttribute("aria-valuenow")).toBe("84");
    expect(screen.getByRole("heading", { name: "Güçlü yönler" })).toBeTruthy();
    expect(screen.getByText("A clear experience section was detected.")).toBeTruthy();
    expect(screen.getByText("Job-specific keyword matching was not performed.")).toBeTruthy();
    expect(screen.getByText("Consider adding a professional profile URL.")).toBeTruthy();
    expect(screen.getByText("Bu kurallar kapsamında belirgin bir sorun bulunmadı. Bu, tüm ATS risklerinin elendiği anlamına gelmez.")).toBeTruthy();
  });

  it("maps API failures to safe copy and supports retry", async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(makeResponse({ errorCode: "resume_not_ready", detail: "Sensitive backend detail" }, 409))
      .mockResolvedValueOnce(makeResponse(successfulAnalysis));
    renderAnalysisPage();

    expect(await screen.findByRole("alert")).toBeTruthy();
    expect(screen.getByRole("alert").textContent).toContain("CV henüz analize hazır değil");
    expect(screen.getByRole("alert").textContent).not.toContain("Sensitive backend detail");

    fireEvent.click(screen.getByRole("button", { name: "Yeniden dene" }));
    await waitFor(() => expect(screen.getByRole("meter").getAttribute("aria-valuenow")).toBe("84"));
  });
});
