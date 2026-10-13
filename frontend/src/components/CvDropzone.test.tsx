import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { useState } from "react";
import CvDropzone from "./CvDropzone";
import { MAX_FILE_BYTES } from "../types/intake";

afterEach(() => {
  cleanup();
});

type HarnessProps = {
  initialFile?: File | null;
  onFileChange?: (file: File | null) => void;
};

function ControlledDropzone({ initialFile = null, onFileChange }: HarnessProps) {
  const [file, setFile] = useState<File | null>(initialFile);

  function handleFileChange(nextFile: File | null) {
    setFile(nextFile);
    onFileChange?.(nextFile);
  }

  return (
    <CvDropzone
      file={file}
      disabled={false}
      error={null}
      onFileChange={handleFileChange}
    />
  );
}

function makeFile(name: string, content = "CV contents"): File {
  return new File([content], name, { type: "application/octet-stream" });
}

function selectFile(container: HTMLElement, file: File) {
  const input = container.querySelector<HTMLInputElement>('input[type="file"]');
  if (!input) throw new Error("Expected the CV file input to exist.");
  fireEvent.change(input, { target: { files: [file] } });
}

describe("CV dropzone", () => {
  it("accepts a PDF and shows the selected file", () => {
    const onFileChange = vi.fn();
    const { container } = render(<ControlledDropzone onFileChange={onFileChange} />);
    const file = makeFile("resume.pdf");

    selectFile(container, file);

    expect(onFileChange).toHaveBeenCalledWith(file);
    expect(screen.getByText("resume.pdf")).toBeTruthy();
    expect(screen.getByText(/Yüklemeye hazır/)).toBeTruthy();
  });

  it("accepts a DOCX file case-insensitively", () => {
    const onFileChange = vi.fn();
    const { container } = render(<ControlledDropzone onFileChange={onFileChange} />);
    const file = makeFile("Resume.DOCX");

    selectFile(container, file);

    expect(onFileChange).toHaveBeenCalledWith(file);
    expect(screen.getByText("Resume.DOCX")).toBeTruthy();
  });

  it("clears the old file and displays an error when a replacement has an unsupported extension", () => {
    const previousFile = makeFile("previous.pdf");
    const onFileChange = vi.fn();
    const { container } = render(
      <ControlledDropzone initialFile={previousFile} onFileChange={onFileChange} />
    );

    selectFile(container, makeFile("notes.txt"));

    expect(onFileChange).toHaveBeenCalledWith(null);
    expect(screen.queryByText("previous.pdf")).toBeNull();
    expect(screen.getByRole("alert").textContent).toContain("Yalnızca PDF veya DOCX");
  });

  it.each([
    { file: new File([], "empty.pdf"), message: "boş görünüyor" },
    {
      file: new File([new Uint8Array(MAX_FILE_BYTES + 1)], "large.pdf"),
      message: "10 MB sınırını aşıyor"
    }
  ])("rejects an invalid CV replacement", ({ file, message }) => {
    const previousFile = makeFile("previous.pdf");
    const onFileChange = vi.fn();
    const { container } = render(
      <ControlledDropzone initialFile={previousFile} onFileChange={onFileChange} />
    );

    selectFile(container, file);

    expect(onFileChange).toHaveBeenCalledWith(null);
    expect(screen.queryByText("previous.pdf")).toBeNull();
    expect(screen.getByRole("alert").textContent).toContain(message);
  });

  it("clears the selection when the user removes the file", () => {
    const previousFile = makeFile("resume.pdf");
    const onFileChange = vi.fn();
    render(<ControlledDropzone initialFile={previousFile} onFileChange={onFileChange} />);

    fireEvent.click(screen.getByRole("button", { name: "Kaldır" }));

    expect(onFileChange).toHaveBeenCalledWith(null);
    expect(screen.queryByText("resume.pdf")).toBeNull();
    expect(screen.getByRole("button", { name: "Dosya seç" })).toBeTruthy();
  });

  it("allows a valid file to replace the prior selection and clears the previous error", () => {
    const previousFile = makeFile("previous.pdf");
    const onFileChange = vi.fn();
    const { container } = render(
      <ControlledDropzone initialFile={previousFile} onFileChange={onFileChange} />
    );

    selectFile(container, makeFile("not-a-cv.txt"));
    expect(screen.getByRole("alert")).toBeTruthy();

    const replacement = makeFile("replacement.docx");
    selectFile(container, replacement);

    expect(onFileChange).toHaveBeenLastCalledWith(replacement);
    expect(screen.queryByRole("alert")).toBeNull();
    expect(screen.getByText("replacement.docx")).toBeTruthy();
  });

  it("announces selected CV details in a live status region", () => {
    const { container } = render(<ControlledDropzone />);
    selectFile(container, makeFile("accessible-resume.pdf"));

    expect(screen.getByRole("status").textContent).toContain("accessible-resume.pdf");
    expect(screen.getByRole("status").getAttribute("aria-live")).toBe("polite");
  });
});
