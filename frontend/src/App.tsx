import { useEffect, useRef, useState, type FormEvent } from "react";
import { Link, Navigate, Route, Routes, useLocation, useNavigate } from "react-router-dom";
import CvDropzone from "./components/CvDropzone";
import { uploadCv } from "./services/cvIntakeClient";
import { MAX_ADDITIONAL_INFORMATION, type UploadSuccess, UploadError } from "./types/intake";

type Phase = "idle" | "uploading" | "processing" | "success" | "error";

function Brand() {
  return <Link className="brand" to="/" aria-label="ATS CV ana sayfa"><span className="brand-mark">CV</span><span>ats<span className="brand-accent">.</span>cv</span></Link>;
}

function Shell({ children }: { children: React.ReactNode }) {
  return <div className="app-shell"><header className="topbar"><Brand /><span className="topbar-note">Kariyerinin bir sonraki adımı</span></header>{children}<footer className="footer"><span>ATS CV · Başvuruna sağlam bir başlangıç</span><span>Dosyaların yalnızca CV işleme isteği için gönderilir.</span></footer></div>;
}

function UploadPage() {
  const [file, setFile] = useState<File | null>(null);
  const [additional, setAdditional] = useState("");
  const [phase, setPhase] = useState<Phase>("idle");
  const [progress, setProgress] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<UploadSuccess | null>(null);
  const abortRef = useRef<AbortController | null>(null);
  const navigate = useNavigate();

  useEffect(() => () => abortRef.current?.abort(), []);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!file || phase === "uploading" || phase === "processing") return;
    setError(null);
    setProgress(0);
    setPhase("uploading");
    const controller = new AbortController();
    abortRef.current = controller;

    try {
      const response = await uploadCv(file, additional, controller.signal, (value) => {
        setProgress(value);
        if (value >= 100) setPhase("processing");
      });
      setResult(response);
      setPhase("success");
      navigate("/analysis", { state: { result: response } });
    } catch (cause) {
      if (cause instanceof DOMException && cause.name === "AbortError") {
        setPhase("idle");
        setError("Yükleme iptal edildi. İstersen yeniden deneyebilirsin.");
      } else {
        const safeError = cause instanceof UploadError ? cause.message : "Beklenmeyen bir hata oluştu. Lütfen yeniden dene.";
        setError(safeError);
        setPhase("error");
      }
    } finally {
      abortRef.current = null;
    }
  }

  const busy = phase === "uploading" || phase === "processing";

  return <Shell>
    <main className="main-content">
      <div className="eyebrow"><span className="eyebrow-dot" /> CV HAZIRLAMA ALANI</div>
      <h1>Deneyimini doğru anlatan<br /><span>bir CV ile başla.</span></h1>
      <p className="intro">Mevcut CV'ni yükle. İçeriğini işlemeye başlayalım; sonraki adımlarda ATS uyumluluğunu ve başvurunu birlikte geliştirelim.</p>

      <div className="stepper" aria-label="Başvuru adımları">
        <div className="step step--active"><span>1</span><div><strong>CV'ni yükle</strong><small>Dosyanı ekle</small></div></div>
        <div className="step-line" />
        <div className="step"><span>2</span><div><strong>İçeriği incele</strong><small>Sonraki adım</small></div></div>
        <div className="step-line" />
        <div className="step"><span>3</span><div><strong>CV'ni geliştir</strong><small>Sonuçlarını gör</small></div></div>
      </div>

      <form className="upload-card" onSubmit={submit}>
        <div className="card-heading"><div><span className="section-kicker">ADIM 01</span><h2>CV dosyan</h2></div><span className="optional-pill">Gerekli</span></div>
        <CvDropzone file={file} disabled={busy} error={null} onFileChange={(next) => { setFile(next); setError(null); setPhase("idle"); setResult(null); }} />

        <div className="field-heading"><label htmlFor="additional">Eklemek istediğin başka bilgiler var mı?</label><span>İsteğe bağlı</span></div>
        <p className="field-description">CV'nde yer almayan becerilerini, yeni sertifikalarını veya hedeflediğin rolü kısaca paylaşabilirsin.</p>
        <textarea id="additional" value={additional} onChange={(event) => setAdditional(event.target.value)} maxLength={MAX_ADDITIONAL_INFORMATION} disabled={busy} placeholder="Örn. Son projemde .NET 8 ve Azure kullandım..." rows={4} />
        <div className="character-count">{additional.length.toLocaleString("tr-TR")} / {MAX_ADDITIONAL_INFORMATION.toLocaleString("tr-TR")}</div>

        {busy && <div className="progress-panel" aria-live="polite">
          <div className="progress-copy"><span className="spinner" aria-hidden="true" /><div><strong>{phase === "uploading" ? "CV yükleniyor..." : "CV işleniyor..."}</strong><span>{phase === "uploading" ? "Dosyan güvenli şekilde gönderiliyor." : "Sunucu dosyanın içeriğini kontrol ediyor ve metni çıkarıyor."}</span></div><b>{progress}%</b></div>
          <div className="progress-track" role="progressbar" aria-label="Dosya yükleme ilerlemesi" aria-valuemin={0} aria-valuemax={100} aria-valuenow={progress}><span style={{ width: \`\${progress}%\` }} /></div>
          <button type="button" className="text-button cancel-button" onClick={() => abortRef.current?.abort()}>Yüklemeyi iptal et</button>
        </div>}

        {error && <div className="alert alert--error" role="alert"><span aria-hidden="true">!</span><div><strong>İşlem tamamlanamadı</strong><p>{error}</p></div></div>}
        {phase === "success" && result && <div className="alert alert--success" role="status"><span aria-hidden="true">✓</span><div><strong>CV alındı</strong><p>Dosyan başarıyla işlendi.</p></div></div>}

        <button className="primary-button" type="submit" disabled={!file || busy || additional.length > MAX_ADDITIONAL_INFORMATION}>
          {busy ? "İşlem devam ediyor..." : phase === "error" ? "Yeniden dene" : "CV'ni yükle ve devam et"} <span aria-hidden="true">→</span>
        </button>
        <p className="privacy-note"><span aria-hidden="true">⌑</span> Dosyanı yüklemeden önce doğru CV'yi seçtiğinden emin ol.</p>
      </form>
      <div className="trust-row"><div><span>✓</span><div><strong>PDF ve Word</strong><small>PDF / DOCX desteği</small></div></div><div><span>↗</span><div><strong>Senin kontrolünde</strong><small>Ek bilgileri sen belirlersin</small></div></div><div><span>◎</span><div><strong>Şeffaf süreç</strong><small>Hata olursa yeniden dene</small></div></div></div>
    </main>
  </Shell>;
}

function AnalysisPage() {
  const location = useLocation();
  const result = (location.state as { result?: UploadSuccess } | null)?.result;
  if (!result) return <Navigate to="/" replace />;

  return <Shell><main className="main-content result-content">
    <div className="success-icon" aria-hidden="true">✓</div>
    <div className="eyebrow"><span className="eyebrow-dot" /> İLK ADIM TAMAMLANDI</div>
    <h1>CV dosyan alındı.<br /><span>Buradan devam edeceğiz.</span></h1>
    <p className="intro">Dosyan başarıyla okundu ve metin çıkarıldı. ATS analizi bu sürümde henüz bağlanmadı; bu nedenle sonuç veya puan uydurmuyoruz.</p>
    <section className="result-card"><span className="section-kicker">YÜKLENEN DOSYA</span><h2>{result.fileName}</h2><div className="result-meta"><span>✓ Metin çıkarıldı</span><span>✓ CV taslağı oluşturuldu</span></div><div className="id-grid"><div><small>CV taslak kimliği</small><code>{result.resumeId}</code></div><div><small>Aday profili kimliği</small><code>{result.candidateProfileId}</code></div></div></section>
    <div className="next-step"><span className="next-number">02</span><div><strong>Sıradaki adım: ATS analizi</strong><p>Bir sonraki geliştirmede başlıklar, beceriler, anahtar kelimeler ve okunabilirlik incelenecek.</p></div></div>
    <Link className="primary-button link-button" to="/">Başka bir CV yükle <span aria-hidden="true">→</span></Link>
  </main></Shell>;
}

export default function App() {
  return <Routes><Route path="/" element={<UploadPage />} /><Route path="/analysis" element={<AnalysisPage />} /><Route path="*" element={<Navigate to="/" replace />} /></Routes>;
}