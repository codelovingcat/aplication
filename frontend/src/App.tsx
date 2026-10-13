import { useEffect, useRef, useState, type FormEvent, type ReactNode } from "react";
import { Link, Navigate, Route, Routes, useLocation, useNavigate } from "react-router-dom";
import CvDropzone from "./components/CvDropzone";
import { uploadCv } from "./services/cvIntakeClient";
import { analyzeResume } from "./services/analysisClient";
import { AnalysisError, type ResumeAnalysisResponse } from "./types/analysis";
import { MAX_ADDITIONAL_INFORMATION, type UploadSuccess, UploadError } from "./types/intake";

type Phase = "idle" | "uploading" | "processing" | "success" | "error";

function Brand() {
  return <Link className="brand" to="/" aria-label="ATS CV ana sayfa"><span className="brand-mark">CV</span><span>ats<span className="brand-accent">.</span>cv</span></Link>;
}

function Shell({ children }: { children: ReactNode }) {
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
          <div className="progress-track" role="progressbar" aria-label="Dosya yükleme ilerlemesi" aria-valuemin={0} aria-valuemax={100} aria-valuenow={progress}><span style={{ width: `${progress}%` }} /></div>
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

type FindingGroupProps = {
  heading: string;
  items: string[];
  emptyMessage: string;
};

function FindingGroup({ heading, items, emptyMessage }: FindingGroupProps) {
  const headingId = "heading-" + heading.replace(/\s+/g, "-").toLowerCase();

  return <section className="findings-section" aria-labelledby={headingId}>
    <h2 id={headingId}>{heading}</h2>
    {items.length > 0
      ? <ul className="findings-list">{items.map((item, index) => <li key={heading + "-" + index}>{item}</li>)}</ul>
      : <p className="empty-findings">{emptyMessage}</p>}
  </section>;
}

function AnalysisPage() {
  const location = useLocation();
  const result = (location.state as { result?: UploadSuccess } | null)?.result;
  const [analysis, setAnalysis] = useState<ResumeAnalysisResponse | null>(null);
  const [phase, setPhase] = useState<"loading" | "success" | "error">("loading");
  const [error, setError] = useState<string | null>(null);
  const [retryCount, setRetryCount] = useState(0);

  useEffect(() => {
    if (!result?.resumeId) return;
    const controller = new AbortController();
    setPhase("loading");
    setAnalysis(null);
    setError(null);

    analyzeResume(result.resumeId, controller.signal)
      .then(response => {
        if (controller.signal.aborted) return;
        setAnalysis(response);
        setPhase("success");
      })
      .catch(cause => {
        if (controller.signal.aborted || (cause instanceof DOMException && cause.name === "AbortError")) return;
        setError(cause instanceof AnalysisError
          ? cause.message
          : "CV şu anda analiz edilemedi. Lütfen biraz sonra yeniden dene.");
        setPhase("error");
      });

    return () => controller.abort();
  }, [result?.resumeId, retryCount]);

  if (!result) return <Navigate to="/" replace />;

  return <Shell><main className="main-content result-content">
    <div className="eyebrow"><span className="eyebrow-dot" /> ATS ANALİZİ</div>
    <h1>CV'ni daha yakından<br /><span>tanıyalım.</span></h1>
    <p className="intro">Bu değerlendirme, kaydedilmiş CV metnindeki gözlemlenebilir sinyalleri açıklar. Puan bir işe alınma olasılığı değildir ve işe alım sonucunu tahmin etmez.</p>

    <section className="result-card analysis-card" aria-labelledby="uploaded-file-heading">
      <span className="section-kicker">İNCELENEN DOSYA</span>
      <h2 id="uploaded-file-heading">{result.fileName}</h2>
      <div className="result-meta"><span>✓ Metin çıkarıldı</span><span>✓ CV taslağı kaydedildi</span></div>
      <div className="id-grid">
        <div><small>CV taslak kimliği</small><code>{result.resumeId}</code></div>
        <div><small>Aday profili kimliği</small><code>{result.candidateProfileId}</code></div>
      </div>
    </section>

    {phase === "loading" && <div className="progress-panel analysis-loading" role="status" aria-live="polite">
      <div className="progress-copy"><span className="spinner" aria-hidden="true" /><div><strong>CV analiz ediliyor...</strong><span>Bölümler, iletişim bilgileri, metin kalitesi ve genel anahtar kelime sinyalleri inceleniyor.</span></div></div>
    </div>}

    {phase === "error" && <div className="alert alert--error" role="alert">
      <span aria-hidden="true">!</span><div><strong>Analiz tamamlanamadı</strong><p>{error}</p>
        <button className="secondary-button retry-button" type="button" onClick={() => setRetryCount(count => count + 1)}>Yeniden dene</button>
      </div>
    </div>}

    {phase === "success" && analysis && <>
      <section className="score-panel" aria-labelledby="score-heading">
        <div className="score-copy"><span className="section-kicker">METİN TEMELLİ ATS OKUNABİLİRLİĞİ</span><h2 id="score-heading">Heuristik puanın</h2><p>Gözlemlenebilir metin kurallarından hesaplanır; iş tanımı eşleşmesi veya işe alınma ihtimali değildir.</p></div>
        <div className="analysis-score" role="meter" aria-label="ATS okunabilirlik puanı" aria-valuemin={0} aria-valuemax={100} aria-valuenow={analysis.score}><strong>{analysis.score}</strong><span>/ 100</span></div>
      </section>
      <div className="findings-grid">
        <FindingGroup heading="Güçlü yönler" items={analysis.strengths} emptyMessage="Metinde doğrulanabilir olumlu sinyal bulunamadı." />
        <FindingGroup heading="Dikkat edilmesi gerekenler" items={analysis.issues} emptyMessage="Bu kurallar kapsamında belirgin bir sorun bulunmadı. Bu, tüm ATS risklerinin elendiği anlamına gelmez." />
        <FindingGroup heading="Öneriler" items={analysis.recommendations} emptyMessage="Şu anda ek bir öneri yok." />
        <FindingGroup heading="Bölüm bulguları" items={analysis.sectionFindings} emptyMessage="Bölüm bulgusu bulunamadı." />
        <FindingGroup heading="Anahtar kelime bulguları" items={analysis.keywordFindings} emptyMessage="Anahtar kelime bulgusu bulunamadı." />
      </div>
    </>}

    <Link className="primary-button link-button back-button" to="/">Yeni bir CV yükle <span aria-hidden="true">→</span></Link>
  </main></Shell>;
}

export default function App() {
  return <Routes><Route path="/" element={<UploadPage />} /><Route path="/analysis" element={<AnalysisPage />} /><Route path="*" element={<Navigate to="/" replace />} /></Routes>;
}