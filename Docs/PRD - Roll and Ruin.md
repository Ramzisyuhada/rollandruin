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

*Update terakhir: 13 November 2026. Dicek dari isi repo dan git history (commit `b5ab658`, branch aktif `feature/run-rng-dice-model`).*

**Posisi di jadwal:** awal M1 (minggu 1). Fondasi project dan aset art sudah ada; belum ada kode gameplay yang ter-commit.

### Sudah selesai

| Area | Detail |
| --- | --- |
| Project Unity | Unity 6000.6.5f1, URP 2D (renderer 2D + profil Mobile/PC), Input System, Test Framework, 2D Pixel Perfect, Tilemap, Sprite |
| Repo | Git + `.gitignore` Unity, remote `origin` ke GitHub, branch `main` + `feature/run-rng-dice-model` |
| Struktur folder | `Assets/_Project/` dengan `Scripts/{App,Core,Data,View,Editor}`, sub-folder Core sesuai GDD §7 (Board, Combat, Dice, Events, Relics, Rival, Run, Save, Turn), `Data/`, `Prefabs/`, `Audio/`, `Tests/{EditMode,PlayMode}` |
| Pipeline art | `Tools/ArtGen/gen_sprites.py` + `gen_extras.py`: generator sprite pixel art 16x16 yang bisa dijalankan ulang, plus README import ke Unity |
| Sprite pack v0.1 | Petak (4 lantai, dinding, rim highlight/bahaya/rival), dadu 5 warna × sisi 0–6, pion pemain & rival, ular hijau/emas/beracun (head/body/tail), tangga batu/tali/tali lapuk + Lift Kurcaci, ikon petak (chest, jebakan, api unggun, toko, event, kutukan, obor), 4 musuh, 7 relic, UI (koin, HP, Sisik, panel & tombol 9-slice) |
| Extras art | Portrait 3 boss + Varga (biasa & berbingkai), app icon 512/192 + adaptive icon Android, 28 animasi frame-by-frame dengan preview GIF |
| Player settings | Min SDK API 26 (sesuai §9), versi 0.1.0, orientasi portrait |

### Belum dikerjakan

| Area | Catatan |
| --- | --- |
| Kode Core | Folder `Scripts/` masih kosong (lihat catatan di bawah). Belum ada `RunRng`, `DiceModel`, `BoardModel`, turn state machine, event bus |
| asmdef | Belum ada di disk; target: `RollAndRuin.Core` (`noEngineReferences`), `Data`, `View`, `App`, `Editor`, plus asmdef test |
| Unit test | `Tests/EditMode` dan `PlayMode` kosong |
| Data ScriptableObject | `Data/{Dice,Enemies,Floors,Relics,Rivals,Tiles}` kosong |
| Scene | Baru `SampleScene` bawaan template; belum ada scene Board/Menu |
| Audio | `Audio/{SFX,Music,Mixers}` kosong (F-13) |
| Art yang masih kurang | Relic baru 7 dari 15 (F-08); sprite boss/duel/rival masih portrait saja, belum ada background zona Katakomba & Puncak Menara |

### Catatan & temuan

- **Script hilang dari disk.** File `RollAndRuin.*.csproj` (dibuat Unity jam 15:40–15:43) masih mereferensikan `GameBootstrap.cs`, `RunRng.cs`, `RunState.cs`, `DiceModel.cs`, `BoardModel.cs`, `BoardGenerator.cs`, `BoardValidator.cs`, `FloorConfig.cs`, `TurnStateMachine.cs`, `GameEventBus.cs`, `GameEvents.cs`, `IRelicEffect.cs`, `RivalBrain.cs`, 4 definisi Data, dan 5 asmdef. Tapi file-file itu nggak ada di disk dan nggak pernah masuk commit. Kalau masih dibutuhkan, cek Recycle Bin atau history IDE (Rider/VS Local History); kalau sengaja dihapus, abaikan.
- **Branch `feature/run-rng-dice-model`** belum punya commit sendiri (masih sama dengan `main`).
- **Folder `Docs/`** belum di-commit.
- **Player settings masih default template:** `productName` = "RougeLike UlarTangga", `companyName` = "DefaultCompany", package name Android = `com.UnityTechnologies.com.unity.template.urpblank`. Perlu diganti (misalnya `com.<nama>.rollandruin`) sebelum build pertama, karena package name nggak bisa diubah setelah upload ke Play Console.
- **Package yang mungkin nggak perlu:** `com.unity.ai.navigation`, `com.unity.visualscripting`, `com.unity.collab-proxy`. Bisa dibuang biar build lebih kecil (§9: AAB < 100 MB).
- **Sisa template:** `Assets/TutorialInfo`, `Assets/Readme.asset`, `Assets/Scenes/SampleScene` bisa dihapus setelah scene sendiri ada.

### Langkah berikutnya (sisa M1)

1. Buat asmdef Core (`noEngineReferences`) + asmdef test EditMode.
2. `RunRng` (seed deterministik, state bisa disimpan) dan `DiceModel` + unit test, sesuai nama branch aktif.
3. `BoardModel` papan statis 30 petak + ular/tangga, dan `TurnStateMachine` dasar.
4. Scene Board: render papan pakai sprite pack, pion jalan dengan dadu biasa (kriteria M1).
5. Ganti package name & product name, lalu commit `Docs/`.
