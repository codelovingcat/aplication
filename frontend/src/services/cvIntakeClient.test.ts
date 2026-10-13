import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { uploadCv } from "./cvIntakeClient";
import { UploadError } from "../types/intake";

class MockXMLHttpRequest {
  static instances: MockXMLHttpRequest[] = [];
  static latest: MockXMLHttpRequest | undefined;

  readonly upload = new EventTarget();
  private readonly events = new EventTarget();
  status = 0;
  responseText = "";
  timeout = 0;
  sentBody: Document | XMLHttpRequestBodyInit | null = null;

  readonly open = vi.fn((_method: string, _url: string) => {});
  readonly send = vi.fn((body: Document | XMLHttpRequestBodyInit | null) => {
    this.sentBody = body;
  });
  readonly abort = vi.fn(() => this.dispatch("abort"));

  constructor() {
    MockXMLHttpRequest.instances.push(this);
    MockXMLHttpRequest.latest = this;
  }

  addEventListener(
    type: string,
    listener: EventListenerOrEventListenerObject | null,
    options?: boolean | AddEventListenerOptions
  ): void {
    this.events.addEventListener(type, listener, options);
  }

  dispatch(type: string, event: Event = new Event(type)): void {
    this.events.dispatchEvent(event);
  }

  respond(status: number, body: unknown): void {
    this.status = status;
    this.responseText = typeof body === "string" ? body : JSON.stringify(body);
    this.dispatch("load");
  }
}

function latestRequest(): MockXMLHttpRequest {
  const request = MockXMLHttpRequest.latest;
  if (!request) throw new Error("No XMLHttpRequest has been created.");
  return request;
}

const successResponse = {
  candidateProfileId: "candidate-123",
  resumeId: "resume-456",
  fileName: "cv.pdf",
  extractedText: "Extracted CV text.",
  additionalInformation: "Additional detail"
};

function pdfFile(name = "cv.pdf"): File {
  return new File([new Uint8Array([37, 80, 68, 70, 45, 49])], name, {
    type: "application/pdf"
  });
}

describe("uploadCv", () => {
  beforeEach(() => {
    MockXMLHttpRequest.instances = [];
    MockXMLHttpRequest.latest = undefined;
    vi.stubGlobal("XMLHttpRequest", MockXMLHttpRequest);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("sends multipart form data, reports upload progress, and returns the typed response", async () => {
    const file = pdfFile();
    const onProgress = vi.fn();
    const promise = uploadCv(
      file,
      "Additional detail",
      new AbortController().signal,
      onProgress
    );
    const request = latestRequest();

    expect(request.open).toHaveBeenCalledWith("POST", expect.stringMatching(/\/api\/cv\/upload$/));
    expect(request.send).toHaveBeenCalledOnce();
    expect(request.timeout).toBe(120_000);

    const body = request.sentBody as FormData;
    expect(body.get("File")).toBeInstanceOf(File);
    expect((body.get("File") as File).name).toBe("cv.pdf");
    expect(body.get("AdditionalInformation")).toBe("Additional detail");

    request.upload.dispatchEvent(Object.assign(new Event("progress"), {
      lengthComputable: true,
      loaded: 75,
      total: 100
    }));
    expect(onProgress).toHaveBeenCalledWith(75);

    request.respond(200, successResponse);
    await expect(promise).resolves.toEqual(successResponse);
  });

  it("does not create or send a request when the signal is already aborted", async () => {
    const controller = new AbortController();
    controller.abort();

    await expect(uploadCv(pdfFile(), "", controller.signal, vi.fn()))
      .rejects.toMatchObject({ name: "AbortError" });
    expect(MockXMLHttpRequest.instances).toHaveLength(0);
  });

  it("aborts an in-flight request when its signal is aborted", async () => {
    const controller = new AbortController();
    const promise = uploadCv(pdfFile(), "", controller.signal, vi.fn());
    const request = latestRequest();

    controller.abort();

    expect(request.abort).toHaveBeenCalledOnce();
    await expect(promise).rejects.toMatchObject({ name: "AbortError" });
  });

  it.each([
    { status: 413, body: { errorCode: "file_too_large" }, kind: "too-large" },
    { status: 422, body: { errorCode: "extraction_failed" }, kind: "extraction" },
    { status: 429, body: { errorCode: "rate_limited" }, kind: "rate-limited" }
  ])("maps HTTP $status to a safe typed upload error", async ({ status, body, kind }) => {
    const promise = uploadCv(pdfFile(), "", new AbortController().signal, vi.fn());
    latestRequest().respond(status, body);

    await expect(promise).rejects.toMatchObject({
      name: "UploadError",
      status,
      kind
    });
  });

  it("does not expose server details when a successful response is malformed", async () => {
    const promise = uploadCv(pdfFile(), "", new AbortController().signal, vi.fn());
    latestRequest().respond(200, "not valid JSON");

    await expect(promise).rejects.toMatchObject({
      name: "UploadError",
      kind: "unexpected",
      message: "Sunucudan beklenen yanıt alınamadı. Lütfen yeniden dene."
    });
  });

  it("maps network errors to a user-safe message", async () => {
    const promise = uploadCv(pdfFile(), "", new AbortController().signal, vi.fn());
    latestRequest().dispatch("error");

    await expect(promise).rejects.toMatchObject({
      name: "UploadError",
      kind: "network",
      message: "Sunucuya ulaşılamadı. İnternet bağlantını kontrol edip yeniden dene."
    });
  });

  it("rejects unsupported files before creating a request", () => {
    expect(() => uploadCv(
      pdfFile("notes.txt"),
      "",
      new AbortController().signal,
      vi.fn()
    )).toThrow(UploadError);

    expect(MockXMLHttpRequest.instances).toHaveLength(0);
  });
});
