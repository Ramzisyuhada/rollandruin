# PRD Roll & Ruin

*Product Requirements Document v0.1. Pendamping GDD Roguelike Ular Tangga v0.2. GDD menjelaskan **bagaimana game dimainkan**; PRD ini menjelaskan **apa yang harus dibangun, untuk siapa, dan kapan dianggap berhasil**.*

| | |
| --- | --- |
| Produk | Roll & Ruin (roguelike ular tangga, tema dungeon fantasi) |
| Platform | Android (Google Play), portrait |
| Engine | Unity 6 (6000.6.x), URP 2D |
| Pemilik | Ramzi (solo developer) |
| Status | Pra-produksi → Minggu 1 (setup project + aset art v0.1 selesai; kode gameplay belum ada). Lihat §17 |
| Target rilis | ~9 minggu sambilan + 14 hari closed testing |
| Repo | github.com/Ramzisyuhada/rollandruin |

## 1. Latar belakang & masalah

Game portofolio sering berakhir sebagai prototype yang nggak pernah rilis, atau game besar yang nggak selesai. Rekruter dan lead studio butuh bukti bahwa kandidat bisa **menyelesaikan dan merilis** produk, plus menulis kode yang terstruktur.

Di sisi pemain, ular tangga adalah game yang semua orang kenal tapi murni untung-untungan. Ada ruang untuk versi yang tetap familiar namun penuh keputusan, dimainkan dalam sesi pendek di HP.

## 2. Tujuan produk

**Tujuan utama (portofolio):**

1. Merilis game yang lengkap dan polished di Google Play.
2. Menunjukkan arsitektur kode yang rapi: logika terpisah dari view, data-driven, teruji unit test.
3. Menunjukkan kemampuan sistem: generator prosedural dengan validasi, AI rival, save/load, integrasi ads/analytics.

**Tujuan sekunder (produk):**

- Pemain bisa memahami game dalam satu run pertama tanpa membaca apa pun.
- Pemain mau memulai run kedua setelah run pertama selesai.

## 3. Non-goal

- Bukan untuk menghasilkan pendapatan besar; monetisasi hanya untuk menunjukkan kemampuan integrasi.
- Tidak ada multiplayer online di versi 1.0.
- Tidak ada rilis iOS di versi 1.0.
- Tidak mengejar konten sebanyak mungkin; prioritas pada kualitas dan kelengkapan.

## 4. Target pengguna

**Persona 1 — "Pemain commute" (pemain utama)**

- Usia 18–35, main di HP saat perjalanan atau istirahat.
- Suka game yang bisa selesai dalam 10–15 menit.
- Pernah main roguelike ringan seperti Slay the Spire, Luck be a Landlord, atau Dicey Dungeons.
- Butuh: bisa main satu tangan, bisa ditutup kapan saja tanpa kehilangan progres.

**Persona 2 — "Reviewer portofolio" (audiens kedua)**

- Lead programmer, game designer, atau rekruter studio.
- Melihat halaman Play Store, video, dan repo GitHub selama 5–10 menit.
- Butuh: build yang jalan tanpa crash, README yang menjelaskan arsitektur, kode yang mudah dibaca.

## 5. Metrik keberhasilan

| Metrik | Target | Cara ukur |
| --- | --- | --- |
| Rilis publik di Google Play | Ya, sebelum minggu ke-12 | Status Play Console |
| Crash-free sessions | ≥ 99% | Android Vitals / Unity Cloud Diagnostics |
| Run pertama selesai (menang atau kalah) | ≥ 70% pemain baru | Analytics: `run_start` vs `run_end` |
| Pemain memulai run kedua | ≥ 40% | Analytics: jumlah `run_start` per pemain |
| Rata-rata durasi run | 10–15 menit | Analytics: selisih waktu `run_start`–`run_end` |
| Papan lolos validator | 100% papan yang ditampilkan | Unit test + log generator |
| Cakupan unit test logika Core | ≥ 60% | Unity Code Coverage |
| Rating Play Store | ≥ 4.0 (jika ada ≥ 10 ulasan) | Play Console |

## 6. Lingkup rilis

### 6.1 MVP (v1.0)

| ID | Fitur | Prioritas |
| --- | --- | --- |
| F-01 | Papan 30 petak per lantai, 3 lantai per run | P0 |
| F-02 | Sistem dadu: tangan 3 dadu, cooldown, 10 jenis dadu | P0 |
| F-03 | Ular & tangga (termasuk varian ular emas, beracun, tangga tali lapuk, Lift Kurcaci) | P0 |
| F-04 | Petak spesial: toko, chest, event, kutukan, api unggun, musuh, jebakan | P0 |
| F-05 | Duel dadu melawan musuh dan ular | P0 |
| F-06 | 3 boss (Raja Kobra, Naga Tangga, Ouroboros) | P0 |
| F-07 | Rival AI (Varga) dengan 3 kepribadian, sabotase, duel di petak yang sama | P0 |
| F-08 | 15 relic dengan sistem hook | P0 |
| F-09 | Generator papan prosedural + validator fairness | P0 |
| F-10 | Save/load run otomatis tiap akhir giliran | P0 |
| F-11 | Meta progression: Sisik untuk unlock dadu dan relic | P1 |
| F-12 | Tutorial interaktif di run pertama | P1 |
| F-13 | Audio: SFX dadu, langkah, ular, duel; musik per zona | P1 |
| F-14 | Analytics event dasar | P1 |
| F-15 | Rewarded ad (lanjut setelah kalah / gandakan Sisik) | P2 |
| F-16 | Pengaturan: volume, getar, bahasa (ID/EN) | P2 |

*P0 = wajib ada untuk rilis. P1 = sangat diharapkan; boleh dirilis tanpa ini kalau terpaksa. P2 = bonus.*

### 6.2 Post-MVP

- Karakter kedua dan ketiga dengan dadu awal berbeda
- Menara kedua (tileset baru)
- Rival tambahan dengan kemampuan unik
- Daily seed challenge + leaderboard (Unity Gaming Services)
- Rilis iOS

## 7. User stories

**Run & papan**

- Sebagai pemain, saya ingin melihat seluruh papan lantai saat ini, supaya bisa merencanakan langkah.
- Sebagai pemain, saya ingin melihat petak mana yang mungkin saya capai saat memilih dadu, supaya keputusan saya jelas.
- Sebagai pemain, saya ingin papan berbeda tiap run, supaya tiap run terasa baru.

**Dadu & relic**

- Sebagai pemain, saya ingin memilih dadu dari tangan sebelum melempar, supaya saya punya kontrol atas risiko.
- Sebagai pemain, saya ingin membaca efek relic dengan menekan lama ikonnya, supaya saya paham sinergi build saya.

**Ular & rival**

- Sebagai pemain, saya ingin memilih antara turun atau melawan ular, supaya ular terasa sebagai keputusan, bukan hukuman acak.
- Sebagai pemain, saya ingin melihat posisi dan dadu rival, supaya saya bisa memutuskan kapan menyabotasenya.

**Sesi mobile**

- Sebagai pemain, saya ingin run saya tersimpan otomatis saat aplikasi ditutup, supaya saya bisa lanjut nanti.
- Sebagai pemain, saya ingin bisa main dengan satu jempol, supaya nyaman dimainkan sambil berdiri.

## 8. Kebutuhan fungsional

**Papan & generator**

- FR-01: Generator menghasilkan papan dari seed yang sama secara deterministik.
- FR-02: Setiap papan yang ditampilkan wajib lolos validator: bisa diselesaikan dengan dadu awal, rata-rata giliran 6–14.
- FR-03: Tidak boleh ada dua ujung ular/tangga di petak yang sama; tidak ada jump di petak start atau pintu boss.
- FR-04: Minimal 1 toko dan 1 api unggun per lantai.

**Giliran**

- FR-05: Urutan giliran: pilih dadu → lempar → gerak → resolusi petak → giliran rival → akhir giliran.
- FR-06: Dadu yang dipakai masuk cooldown sesuai definisinya; minimal satu dadu selalu siap.
- FR-07: Lemparan yang melewati pintu boss berhenti di pintu boss.

**Rival**

- FR-08: Rival memilih dadu berdasarkan skor heuristik + bobot kepribadian + noise.
- FR-09: Jika rival mencapai pintu boss lebih dulu di lantai 1–2, hadiah lantai hilang dan pemain mendapat kutukan; di lantai 3, run kalah.
- FR-10: Ular, tangga, dan jebakan berlaku sama untuk rival.

**Progres & data**

- FR-11: State run (posisi, HP, gold, dadu, relic, seed, state RNG) disimpan setiap akhir giliran.
- FR-12: Saat aplikasi dibuka, pemain ditawari melanjutkan run yang tersimpan.
- FR-13: Sisik tersimpan permanen dan tidak hilang saat run kalah.

## 9. Kebutuhan non-fungsional

| Area | Kebutuhan |
| --- | --- |
| Performa | 60 fps di HP mid-range (misalnya kelas Snapdragon 6xx / RAM 4 GB); minimal 30 fps di low-end |
| Waktu buka | < 5 detik dari tap ikon sampai main menu |
| Ukuran | AAB < 100 MB |
| Kompatibilitas | Android 8.0 (API 26) ke atas; target API level terbaru yang diwajibkan Play |
| Orientasi & input | Portrait, semua aksi bisa dengan satu jempol di sepertiga bawah layar |
| Offline | Seluruh game bisa dimainkan tanpa internet (ads/analytics opsional) |
| Ketahanan | Tidak kehilangan progres saat app di-kill di tengah giliran (paling banyak mengulang giliran berjalan) |
| Aksesibilitas | Teks minimal 14 sp; info penting tidak hanya dibedakan lewat warna |
| Kualitas kode | Logika Core tanpa dependensi UnityEngine (asmdef `noEngineReferences`); unit test jalan di EditMode |
| Privasi | Privacy policy publik; tidak mengumpulkan data pribadi selain yang dibutuhkan SDK ads/analytics |

## 10. Analytics

| Event | Parameter | Tujuan |
| --- | --- | --- |
| `run_start` | seed, karakter | Hitung run dan retensi |
| `floor_reached` | lantai, giliran, HP | Lihat titik sulit |
| `rival_first` | lantai | Ukur balancing rival |
| `relic_picked` | relic_id, lantai | Relic mana yang populer |
| `run_end` | hasil, lantai, durasi, penyebab | Win rate dan durasi run |
| `tutorial_step` | step | Lihat di mana pemain baru berhenti |

## 11. Monetisasi

- Gratis, tanpa pay-to-win.
- Rewarded ad opsional (F-15): lanjut sekali setelah kalah, atau gandakan Sisik di akhir run.
- Opsional: IAP non-consumable "Remove Ads".

## 12. Jadwal & milestone

| Milestone | Minggu | Kriteria selesai |
| --- | --- | --- |
| M1 — Prototype papan | 1–2 | Pion jalan di papan dengan dadu; ular & tangga berfungsi |
| M2 — Sistem inti | 3–4 | Dadu data-driven, relic hook, duel, boss pertama |
| M3 — Run lengkap | 5–6 | Generator + validator, 3 lantai, rival AI |
| M4 — Meta & persistensi | 7 | Toko, Sisik, save/load |
| M5 — Polish | 8 | Juice, audio, UI final, tutorial |
| M6 — Rilis | 9 + 2 minggu | Closed testing 12 tester × 14 hari, lalu produksi |

## 13. Risiko & mitigasi

| Risiko | Dampak | Mitigasi |
| --- | --- | --- |
| Scope membengkak | Game nggak rilis | Patuhi daftar P0; semua ide baru masuk Post-MVP |
| Game terasa terlalu acak | Pemain berhenti setelah 1 run | Playtest sejak M1; tambah preview petak tujuan dan dadu deterministik (Kompas) |
| Rival terlalu kuat/lemah | Frustrasi atau membosankan | Simulasi ribuan run AI vs AI untuk balancing; parameter di ScriptableObject |
| Kesulitan dapat 12 tester closed testing | Rilis tertunda | Kumpulkan tester dari teman/komunitas sejak minggu 6 |
| Waktu sambilan terbatas | Jadwal molor | Milestone kecil; commit tiap fitur; buffer 2 minggu |
| Aset visual kurang menarik | Konversi Play Store rendah | Fokus polish pada juice dan animasi; ganti aset kunci (ikon, boss) bila sempat |

## 14. Dependensi

- Unity 6 + URP 2D, Input System, Test Framework
- Akun Google Play Console (biaya pendaftaran sekali bayar)
- Opsional: Unity Analytics, LevelPlay/Unity Ads
- Git + Git LFS, GitHub

## 15. Kriteria rilis (Definition of Done v1.0)

- Semua fitur P0 selesai dan bisa dimainkan dari menu sampai run berakhir.
- Tidak ada bug blocker atau crash yang diketahui.
- Semua unit test Core lulus.
- Save/load teruji: app di-kill di tiap fase giliran, lalu dibuka lagi.
- Halaman Play Store lengkap: ikon, feature graphic, 4–8 screenshot, video 30 detik, deskripsi ID/EN, privacy policy.
- README repo berisi GIF gameplay, penjelasan arsitektur, dan cara menjalankan test.

## 16. Pertanyaan terbuka

- Penalti rival di lantai 3 (langsung kalah) dipertahankan atau diganti boss lebih kuat?
- Bahasa default: Indonesia atau Inggris?
- Pakai Unity Analytics atau Firebase?
- Rewarded ad dimasukkan di v1.0 atau ditunda?

## 17. Status progres

*Update terakhir: 13 November 2026. Dicek dari isi repo, git history, dan hasil test (`main` = `70438b2`).*

**Posisi di jadwal:** M1–M4 selesai di sisi kode. Semua logika Core untuk fitur P0 (kecuali audio dan polish) sudah ada dan teruji. Yang tersisa sebelum M5: memasang scene di Unity Editor dan playtest di HP.

### Sudah selesai

| Milestone | Fitur | Detail |
| --- | --- | --- |
| Setup | Project & art | Unity 6000.6.5f1, URP 2D, Input System, Test Framework; sprite pack v0.1 + 28 animasi + app icon; import sprite otomatis (16 PPU, point filter, 9-slice) |
| M1 | F-01, F-02 | `RunRng` (SplitMix64, state bisa disimpan), 10 dadu termasuk Kompas (pilih angka) dan Kembar (giliran ekstra), efek sisi (gold, lompat ke tangga), tangan 3 dadu + cooldown + FR-06 |
| M1 | Papan & giliran | `BoardModel` (FR-03), `MoveResolver` (FR-07), `TurnStateMachine` (FR-05) berbasis continuation, `GameEventBus` dengan hook event |
| M1 | View | `BoardView` (grid zig-zag, ular/tangga tiled, preview petak tujuan), `PawnView`, `AnimationQueue`, `HudView`, `DiceHandView`, `ChoicePanelView`, `BoardCameraFitter`, menu Editor **Roll & Ruin → Build Game Scene** |
| M2 | F-08 | 15 relic lewat hook (7 dari GDD + 8 tambahan), state per relic ikut save |
| M2 | F-03, F-05 | Pilihan lawan/turun saat kena ular, Ular Emas/Berbisa, Tangga Tali Lapuk, Lift Kurcaci (bayar gold); duel dadu dengan telegraph dadu musuh, 4 monster |
| M2 | F-06 | Raja Kobra (gigitan tiap 2 ronde), Naga Tangga (curi dadu terkuat), Ouroboros (pulih saat pemain kalah lemparan); hadiah 1 dari 3 lalu lantai berikutnya |
| M3 | F-09 | `BoardGenerator` + `BoardValidator` (500 simulasi, 100% selesai, rata-rata 6–14 giliran). 100 seed per lantai: rata-rata 9,1 / 10,0 / 11,0 giliran, semua lolos di percobaan pertama. Menu Editor **Roll & Ruin → Validator Report** |
| M3 | F-07 | `RivalBrain` (skor heuristik + bobot kepribadian + noise), 3 kepribadian, aturan papan sama (FR-10), rival duluan = kutukan + hadiah berkurang / kalah di lantai 3 (FR-09), duel di petak yang sama, sabotase (pasang jebakan, pindah ular) |
| M4 | F-04 | Toko (relic, dadu, asah sisi, ramuan, item sabotase), chest, 6 event naratif, kutukan, api unggun, jebakan tersembunyi (terlihat setelah dilewati / dengan Peta Kusam) |
| M4 | F-10 | Save JSON tiap awal giliran + saat app di-pause, tulis atomik, tawaran "Lanjutkan run?" (FR-11, FR-12) |
| M4 | F-11 | Sisik tiap run (FR-13), unlock 6 dadu + 8 relic lewat Toko Sisik di layar akhir run |
| Kualitas | Test | **212 test EditMode lulus** (dijalankan juga di luar Unity dengan .NET). Semua script View/App/Editor ter-compile terhadap DLL Unity 6000.6.5 |

### Keputusan desain selama implementasi

- **RNG:** SplitMix64 sendiri, bukan `System.Random`, supaya state bisa disimpan (GDD §7 sudah diupdate).
- **Duel:** pemain dapat +1 damage tiap menang lemparan, dan HP awal 25. Tanpa ini, duel boss terlalu simetris (simulasi 300 run: bot sederhana menang 1%). Setelah semua fitur M4 masuk, bot sederhana menang **25%**. Kalah paling sering di Naga Tangga (19%), Ouroboros (14%), dan karena rival (17%).
- **Jebakan tersembunyi:** supaya relic Peta Kusam ("lihat isi 5 petak di depan") punya arti, jebakan baru terlihat setelah dilewati.
- **Rival duluan di lantai 1–2:** hadiah boss jadi 2 pilihan (dari 3) + kutukan acak. Lantai 3 tetap langsung kalah (pertanyaan terbuka §16 belum diputuskan).
- **Save hanya di awal giliran:** kalau app di-kill di tengah giliran, pemain mengulang giliran itu (sesuai §9).
- **Tangan awal:** 3 Dadu Biasa; maksimal 4 dadu di tangan.

### Belum dikerjakan

| Area | Catatan |
| --- | --- |
| Scene Unity | Jalankan **Roll & Ruin → Build Game Scene**, lalu cek di Play mode. Panduan: [SETUP - Scene Unity.md](SETUP%20-%20Scene%20Unity.md) |
| Playtest | Belum dimainkan di Editor/HP; perlu cek tampilan, ukuran teks, dan alur panel |
| F-12 Tutorial | Belum ada (M5) |
| F-13 Audio | `Audio/` masih kosong (M5) |
| F-14 Analytics | Belum ada; event di §10 bisa dipasang dari `GameEventBus` |
| Main menu | Game langsung masuk ke run; menu utama dan layar pilih karakter masuk M5 |
| Data ScriptableObject | Katalog dadu/relic/musuh masih di kode (`DiceFactory`, `RelicCatalog`, `EnemyCatalog`). Kalau mau tuning tanpa compile ulang, pindahkan ke ScriptableObject di `Data/` |
| PlayMode test | Masih kosong |
| Art | Sprite relic baru 7 dari 15; belum ada sprite pintu boss khusus (sementara pakai `wall_brick`) |

### Catatan setup yang masih terbuka

- **Player settings masih default template:** `productName` = "RougeLike UlarTangga", `companyName` = "DefaultCompany", package Android = `com.UnityTechnologies.com.unity.template.urpblank`. Ganti sebelum build pertama (package name tidak bisa diubah setelah upload ke Play Console).
- **Asmdef test** masih Any Platform; lebih rapi kalau Editor saja.
- **Package yang mungkin tidak perlu:** `com.unity.ai.navigation`, `com.unity.visualscripting`, `com.unity.collab-proxy`.
- **Sisa template:** `Assets/TutorialInfo`, `Assets/Readme.asset`, `Assets/Scenes/SampleScene` bisa dihapus setelah scene Game dicek.

### Langkah berikutnya (M5)

1. Bangun scene lewat menu Editor, main satu run penuh di Play mode, catat yang terasa janggal.
2. Ganti package name & product name, build APK pertama, coba di HP.
3. Tutorial singkat, audio (SFX dadu dulu), juice animasi.
4. Main menu + pengaturan, analytics, lalu persiapan closed testing.
