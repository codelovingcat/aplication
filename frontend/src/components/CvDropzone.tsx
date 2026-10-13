import { useRef, useState, type DragEvent, type ChangeEvent } from "react";
import { ACCEPTED_EXTENSIONS, MAX_FILE_BYTES } from "../types/intake";

type Props = {
  file: File | null;
  disabled: boolean;
  error: string | null;
  onFileChange: (file: File | null) => void;
};

function validate(file: File): string | null {
  const extension = file.name.slice(file.name.lastIndexOf(".")).toLowerCase();
  if (!ACCEPTED_EXTENSIONS.includes(extension as ".pdf" | ".docx")) return "Yalnızca PDF veya DOCX dosyaları destekleniyor.";
  if (file.size === 0) return "Bu dosya boş görünüyor.";
  if (file.size > MAX_FILE_BYTES) return "Dosya boyutu 10 MB sınırını aşıyor.";
  return null;
}

function formatBytes(bytes: number): string {
  return bytes < 1024 * 1024 ? `${(bytes / 1024).toFixed(0)} KB` : `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export default function CvDropzone({ file, disabled, error, onFileChange }: Props) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [dragging, setDragging] = useState(false);
  const [localError, setLocalError] = useState<string | null>(null);

  function acceptFile(candidate: File | undefined) {
    if (!candidate) return;
    const issue = validate(candidate);
    setLocalError(issue);
    if (issue) {
      // Do not leave an earlier valid file selected after a replacement fails.
      onFileChange(null);
      return;
    }
    onFileChange(candidate);
  }

  function onInputChange(event: ChangeEvent<HTMLInputElement>) {
    acceptFile(event.target.files?.[0]);
    event.target.value = "";
  }

  function onDrop(event: DragEvent<HTMLDivElement>) {
    event.preventDefault();
    setDragging(false);
    if (!disabled) acceptFile(event.dataTransfer.files[0]);
  }

  return (
    <div
      className={`dropzone ${dragging ? "dropzone--active" : ""} ${file ? "dropzone--selected" : ""}`}
      onDragOver={(event) => { event.preventDefault(); if (!disabled) setDragging(true); }}
      onDragLeave={() => setDragging(false)}
      onDrop={onDrop}
      aria-label="CV dosyası yükleme alanı"
    >
      <input ref={inputRef} type="file" accept=".pdf,.docx,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document" onChange={onInputChange} disabled={disabled} hidden />
      {file ? (
        <div className="selected-file">
          <div className="file-icon" aria-hidden="true">{file.name.toLowerCase().endsWith(".pdf") ? "PDF" : "DOCX"}</div>
          <div className="file-details">
            <strong title={file.name}>{file.name}</strong>
            <span>{formatBytes(file.size)} · Yüklemeye hazır</span>
          </div>
          {!disabled && <button className="text-button" type="button" onClick={() => onFileChange(null)}>Kaldır</button>}
          {!disabled && <button className="text-button" type="button" onClick={() => inputRef.current?.click()}>Değiştir</button>}
        </div>
      ) : (
        <>
          <div className="upload-symbol" aria-hidden="true">↑</div>
          <strong>CV dosyanı buraya bırak</strong>
          <span className="muted">veya bilgisayarından bir dosya seç</span>
          <button className="secondary-button" type="button" disabled={disabled} onClick={() => inputRef.current?.click()}>Dosya seç</button>
          <span className="file-hint">PDF veya DOCX · En fazla 10 MB</span>
        </>
      )}
      {(localError || error) && <p className="field-error" role="alert">{localError || error}</p>}
    </div>
  );
}
