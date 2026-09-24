# ATS CV Application

ATS uyumlu CV oluşturma, analiz etme ve belirli iş ilanlarına göre kişiselleştirme amacıyla geliştirilen bir uygulamadır.

Uygulamanın temel amacı, kullanıcının mevcut CV'sini yalnızca "daha güzel" hale getirmek değil; CV'yi ATS tarafından daha kolay işlenebilecek, açık ve yapılandırılmış bir formata dönüştürmek ve daha sonra belirli bir iş ilanı için kontrollü biçimde uyarlamaktır.

---

## 🎯 Ürünün Temel Amacı

Uygulama iki ana kullanım senaryosuna dayanır.

### 1. Mevcut CV'yi ATS uyumlu hale getirme

Kullanıcı mevcut CV'sini sisteme yükler.

Desteklenen temel dosya türleri:
Ayrıca bu uygulama ön yazı da hazırlamaktadır.

- PDF
- DOCX / Word

Sistem:

1. Dosyayı doğrular.
2. Güvenli biçimde işler.
3. CV içeriğini metne dönüştürür.
4. CV'nin ATS açısından sorunlarını analiz eder.
5. Sorunları ve nedenlerini kullanıcıya açıklar.
6. CV içeriğini ATS uyumlu bir yapıya dönüştürür.
7. Kullanıcının doğrulanmış bilgilerini koruyarak yeni CV'yi oluşturur.
8. Oluşturulan CV'yi kullanıcının indirebileceği bir doküman olarak üretir.

Önemli nokta: Sistem yalnızca biçimlendirmeyi değiştirmekle kalmaz. ATS açısından sorun oluşturabilecek başlıklar, bölüm yapısı, okunabilirlik, anahtar kelime kullanımı ve içerik organizasyonu gibi unsurları da değerlendirebilir.

---

## 💼 2. Belirli bir iş ilanına göre CV + ön yazı oluşturma

İkinci kullanım senaryosunda kullanıcı belirli bir pozisyona başvurmak ister.

Kullanıcı:

- Mevcut CV'sini kullanır.
- İş ilanını sisteme verir.
- İsterse ayrıca ek bilgi girer.
- CV'de özellikle değiştirilmesini veya eklenmesini istediği noktaları belirtir.

Örneğin kullanıcı şöyle bir ek bilgi verebilir:

> "Bu ilanda Azure ve mikroservis deneyimi özellikle önemli. CV'mde Azure kısmı çok kısa. Gerçek deneyimimde yaptığım Azure çalışmalarını daha görünür hale getir."

Veya:

> "Bu pozisyon için projeler bölümünü daha belirgin yapmak istiyorum."

Sistem daha sonra:

1. İş ilanını analiz eder.
2. İlanda aranan yetkinlikleri ve sorumlulukları çıkarır.
3. Kullanıcının aday profilini ve mevcut CV'sini iş ilanıyla karşılaştırır.
4. Eşleşen deneyimleri belirler.
5. Kullanıcının verdiği ek bilgileri dikkate alır.
6. CV'yi yalnızca kullanıcının sağladığı gerçek bilgiler üzerinden ilana göre yeniden düzenler.
7. İlan için kişiselleştirilmiş CV oluşturur.
8. Aynı başvuru için kişiselleştirilmiş bir ön yazı (cover letter) üretir.

---

## ✍️ "Eklemek istediğin başka bir şey var mı?" Alanı

Uygulamada yalnızca CV yüklenen bir alan bulunmayacaktır.

Kullanıcıya ayrıca serbest metin girebileceği bir alan verilecektir.

Bu alan sayesinde kullanıcı:

- CV'de bulunmayan ama kendisinin doğruladığı bilgileri,
- yeni tamamladığı eğitim veya sertifikaları,
- son dönemde yaptığı işleri,
- belirli bir ilana özel vurgulanmasını istediği deneyimleri,
- CV'de kısa tuttuğu fakat başvuru için önemli olduğunu düşündüğü bilgileri

ekleyebilir.

Bu bilgi alanı özellikle kişiselleştirilmiş başvurularda kullanılacaktır.

### Kritik kural

Bu alan AI için serbestçe "yeni bilgi üretme" alanı değildir.

Kullanıcı tarafından sağlanmayan bir:

- iş deneyimi,
- şirket,
- tarih,
- eğitim,
- sertifika,
- teknik beceri,
- proje,
- başarı,
- unvan

AI tarafından uydurulamaz.

AI yalnızca mevcut ve doğrulanmış bilgiler üzerinde:

- yeniden sıralama,
- yeniden yazma,
- özetleme,
- vurgulama,
- daha uygun başlıklandırma,
- ilana göre önceliklendirme

yapabilir.

---

## 🧠 AI Kullanım Prensibi

AI katmanı ürünün merkezi parçalarından biridir ancak uygulamanın sahibi değildir.

AI'ın görevi:

- CV'yi anlamlandırmak
- ATS problemlerini tespit etmek
- İş ilanındaki gereksinimleri çıkarmak
- Aday profili ile iş ilanını karşılaştırmak
- Kullanıcının gerçek deneyimlerini daha doğru şekilde vurgulamak
- CV'yi ve ön yazıyı hedef role göre yeniden düzenlemek

AI'ın görevi olmayan şey:

> Kullanıcının sahip olmadığı deneyimleri varmış gibi göstermek.

Bu nedenle uygulamada source of truth olarak aday profili ve kullanıcı tarafından sağlanan belgeler kabul edilir.

---

## 🔄 Temel Ürün Akışları

### Akış A — ATS CV dönüşümü

~~~text
CV Upload
   ↓
File Validation
   ↓
Secure Temporary Storage
   ↓
PDF / DOCX Text Extraction
   ↓
Candidate Profile
   ↓
ATS Analysis
   ↓
Issue Explanation
   ↓
ATS-Friendly CV Generation
   ↓
Download
~~~

### Akış B — İş ilanına özel başvuru

~~~text
Existing CV / Candidate Profile
          +
Job Description
          +
Optional Additional Information
          ↓
Job Requirement Extraction
          ↓
Candidate ↔ Job Matching
          ↓
Relevant Experience Selection
          ↓
ATS-Friendly Tailored CV
          +
Tailored Cover Letter
          ↓
Download / Application Archive
~~~

---

## 🏗️ Mimari Yaklaşım

Uygulama Clean Architecture yaklaşımıyla katmanlı olarak tasarlanmaktadır.

### Domain

İş kuralları ve temel modeller burada bulunur.

Örnekler:

- CandidateProfile
- Resume
- JobPosting
- gelecekte ResumeAnalysis
- gelecekte TailoredApplication

### Application

Use-case'ler ve abstraction'lar burada bulunur.

Örnekler:

- CV upload
- CV analysis
- job matching
- CV tailoring
- cover letter generation
- document extraction abstractions
- AI provider abstractions

### Infrastructure

Teknik implementasyonlar burada bulunur.

Örnekler:

- PDF parsing
- DOCX parsing
- file storage
- persistence
- AI provider integration
- document generation

### API

HTTP katmanı burada bulunur.

Örnek endpoint grupları:

- CV upload
- CV analysis
- candidate profile
- job posting
- tailored application generation

---

## 🔐 Güvenlik ve Güvenilirlik İlkeleri

CV bir kullanıcı dosyası olduğu için dosya yükleme alanı güvenlik açısından kritik kabul edilir.

Uygulamada hedeflenen temel önlemler:

- Dosya boyutu sınırı
- Sadece desteklenen dosya türlerine izin verilmesi
- Uzantıya ek olarak dosya içeriğinin/sihirli baytların doğrulanması
- Path traversal saldırılarına karşı güvenli dosya adlandırma
- Geçici dosyaların güvenli depolanması
- Sunucu iç detaylarının client'a gönderilmemesi
- Standart ProblemDetails hata modeli
- Rate limiting
- Validation'ın AI/parsing işleminden önce yapılması
- Yapılandırılabilir limitler
- Aday verilerinin gereksiz yere loglanmaması
- AI çıktılarının kullanıcı kaynaklı gerçek verilerle sınırlandırılması

---

## 📁 Proje Yapısı

~~~text
aplication/
├── docs/
│   ├── architecture.md
│   └── cv-intake.md
│
├── src/
│   ├── AtsCv.Domain/
│   ├── AtsCv.Application/
│   ├── AtsCv.Infrastructure/
│   └── AtsCv.Api/
│
├── global.json
├── Directory.Build.props
├── README.md
├── ritm
└── fake.json
~~~

ritm ve fake.json geliştirme sırasında oluşturulmuş mevcut dosyalardır; ATS uygulamasının çekirdek mimarisinden ayrı tutulabilirler.

---

## 🧩 Mevcut Temel Modeller

### CandidateProfile

Adayın uzun ömürlü ve tekrar kullanılabilir bilgilerinin ana kaynağıdır.

Örneğin:

- Ad soyad
- E-posta
- Telefon
- AdditionalInformation
- gelecekte deneyimler
- eğitim
- yetenekler
- sertifikalar
- projeler

### Resume

Belirli bir CV dokümanını temsil eder.

CV'nin:

- orijinal dosya adı
- content type
- extracted text
- processing status
- oluşturulma zamanı

gibi bilgileri tutulabilir.

### JobPosting

Belirli bir iş ilanını temsil eder.

Temel bilgiler:

- Şirket
- Pozisyon
- İş ilanı metni

### Tailored Application

İlerleyen aşamada belirli bir iş ilanı için oluşturulan başvuru snapshot'ı olacaktır.

Bu snapshot:

- hedef şirket
- hedef pozisyon
- iş ilanı
- kullanılan aday verileri
- oluşturulan CV versiyonu
- oluşturulan cover letter
- generation metadata

bilgilerini koruyacaktır.

Bu sayede aday profilini daha sonra değiştirse bile geçmiş başvuruların hangi bilgilerle oluşturulduğu kaybolmaz.

---

## 🖥️ İlk Kullanıcı Arayüzü Yaklaşımı

İlk CV ekranının temel yapısı:

~~~text
┌────────────────────────────────────────────┐
│              CV'nizi yükleyin              │
│                                            │
│       [ PDF / DOCX dosyası seç ]           │
│                                            │
├────────────────────────────────────────────┤
│                                            │
│  Eklemek istediğiniz başka bilgiler var mı?│
│                                            │
│  ┌──────────────────────────────────────┐  │
│  │ Serbest metin alanı                  │  │
│  │                                      │  │
│  └──────────────────────────────────────┘  │
│                                            │
│             [ CV'yi Analiz Et ]            │
└────────────────────────────────────────────┘
~~~

İş ilanı ile başvuru yapılacak ikinci senaryoda bunun yanında veya sonraki adımda:

~~~text
┌──────────────────────┐
│ İş İlanı             │
│                      │
│ [ İlan metnini yapıştır ]
│                      │
├──────────────────────┤
│ CV'de özellikle      │
│ eklensin/değişsin    │
│ dediğin şeyler?      │
│                      │
│ [ Serbest metin ]    │
└──────────────────────┘
~~~

alanları bulunabilir.

---

## 🛣️ Geliştirme Yol Haritası

İlk aşamada amaç sistemi küçük ve doğrulanabilir adımlarla geliştirmektir.

### Faz 1 — CV Intake

- PDF upload
- DOCX upload
- file validation
- secure temporary storage
- text extraction
- CandidateProfile oluşturma

### Faz 2 — ATS Analysis

- ATS kurallarının modellenmesi
- CV analizi
- skor
- sorunların açıklanması
- düzeltme önerileri

### Faz 3 — ATS-Friendly CV Generation

- ATS-friendly CV template
- candidate data mapping
- document generation
- PDF/DOCX output

### Faz 4 — Job Matching

- job description parsing
- requirement extraction
- candidate/job matching
- relevant experience selection

### Faz 5 — Tailored CV + Cover Letter

- ilana özel CV
- kullanıcı ek bilgileri
- kişiselleştirilmiş özet
- keyword alignment
- cover letter generation

### Faz 6 — Application History

- oluşturulmuş başvurular
- CV versiyonları
- cover letter versiyonları
- hangi ilana göre üretildiği
- tekrar kullanılabilir aday profili

---

## ✅ Başarı Kriteri

Uygulamanın nihai hedefi:

> Kullanıcı mevcut CV'sini yükler, sistem CV'yi analiz eder ve ATS uyumlu hale getirir.

Ardından kullanıcı:

> "Bu iş ilanına başvuracağım."

dediğinde aynı aday profilini kullanarak;

- ilana uygun CV,
- gerekli ek bilgilerin kontrollü biçimde dahil edilmesi,
- ATS uyumlu düzenleme,
- kişiselleştirilmiş ön yazı

tek bir başvuru akışında üretilebilir.

Sonuç olarak uygulama yalnızca bir CV formatter değil, adayın doğrulanmış kariyer bilgilerinin kaynağını koruyan ve bu bilgiler üzerinden tekrar tekrar ATS uyumlu başvuru belgeleri üretebilen bir başvuru platformu olacaktır.

---

## 📌 Geliştirme Notu

Bu repository şu anda aktif geliştirme aşamasındadır.

Kod ve issue'lar küçük, bağımsız ve doğrulanabilir parçalara bölünerek ilerletilecektir. Güvenlik, veri doğruluğu, AI grounding ve sürdürülebilir mimari; özellik geliştirme kadar önceliklidir.
