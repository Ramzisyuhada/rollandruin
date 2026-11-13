# GDD Roguelike Ular Tangga

*Nama kerja: **Roll & Ruin**. Status: draft v0.2 (tema dungeon fantasi, rival AI masuk MVP), buat game portofolio di Play Store.*

*Progres (13 Nov 2026): M1–M4 selesai di sisi kode (212 test lulus); tinggal pasang scene dan playtest. Detail dan keputusan desain baru (HP awal 25, +1 damage pemain di duel, jebakan tersembunyi, 15 relic) ada di PRD §17.*

## 1. Ringkasan

**Pitch:** Ular tangga di dalam menara dungeon, tapi lo yang ngatur nasib. Pemain memanjat Menara Ular lantai demi lantai sambil balapan melawan petualang rival, ngumpulin dadu, relic, dan gold, lalu ngalahin ular raksasa dan boss di tiap lantai.

- **Genre:** Roguelite board game, turn-based
- **Platform:** Android (Play Store), portrait, main satu tangan
- **Engine:** Unity 6 (URP 2D)
- **Durasi satu run:** 10–15 menit
- **Target pemain:** Pemain casual-midcore yang suka Slay the Spire, Luck be a Landlord, atau Dicey Dungeons
- **Pembeda:** Mekanik yang semua orang udah kenal (ular tangga) diubah dari murni luck jadi penuh keputusan
- **Tema:** Dungeon fantasi. Menara terkutuk yang dihuni ular raksasa; tangga berupa tangga batu, tali, dan lift kurcaci antar ruangan.

## 2. Pilar desain

1. **Keberuntungan yang bisa diatur.** Dadu tetap acak, tapi pemain selalu punya pilihan: dadu mana yang dipakai, jalur mana yang diambil, risiko mana yang diterima.
2. **Sesi pendek, keputusan bermakna.** Tiap giliran ada minimal satu pilihan menarik. Nggak ada giliran yang cuma "tap lalu nunggu".
3. **Sinergi bikin nagih.** Kombinasi dadu dan relic menghasilkan build yang beda tiap run.
4. **Kenal dalam 5 detik.** Siapa pun yang lihat screenshot langsung paham ini ular tangga.
5. **Balapan yang terasa hidup.** Rival bikin tiap giliran punya tekanan: maju cepat, atau berhenti buat menyabotase dia.

## 3. Core loop

**Loop giliran:**

1. Pilih satu dadu dari tangan (biasanya 3 dadu).
2. Lempar, lalu pion maju sesuai hasil.
3. Resolusi petak: kosong, event, ular, tangga, toko, musuh.
4. Dadu yang dipakai masuk cooldown 1 giliran.
5. Rival menjalankan gilirannya (dadu dan pilihannya kelihatan), lalu kembali ke giliran pemain.

**Loop run:**

1. Mulai di Lantai 1 dengan karakter dan dadu awal.
2. Tiap lantai = papan 30 petak yang di-generate prosedural.
3. Di ujung lantai ada boss. Menang = pilih 1 dari 3 hadiah, lanjut ke lantai berikutnya.
4. Kalahkan boss lantai 3 = run menang. HP habis, atau rival sampai pintu boss lantai 3 duluan = run kalah.

**Loop meta:**

- Tiap run (menang atau kalah) dapat **Sisik**, currency permanen.
- Sisik dipakai buat unlock dadu baru, relic baru di pool, dan karakter.

## 4. Mekanik utama

### 4.1 Dadu

Tiap dadu adalah objek data dengan 6 sisi. Tiap sisi punya angka dan bisa punya efek tambahan.

| Dadu | Sisi | Catatan |
| --- | --- | --- |
| Dadu Biasa | 1–6 | Dadu awal |
| Dadu Aman | 1,1,2,2,3,3 | Buat menghindari ular di depan |
| Dadu Nekat | 4,5,6,6,0,0 | Besar, tapi bisa diam di tempat |
| Dadu Genap | 2,2,4,4,6,6 | Cocok buat build "angka genap" |
| Dadu Kompas | Pilih 1–3 | Pemain milih angkanya sendiri, cooldown 2 |
| Dadu Kembar | 1–6 | Kalau keluar sama dengan giliran sebelumnya, dapat giliran ekstra |

**Upgrade sisi:** di toko atau event, satu sisi dadu bisa diganti. Contoh: sisi "1" jadi "1 + lompat ke tangga terdekat" atau "3 + dapat 5 gold".

### 4.2 Ular

Ular di sini adalah ular raksasa penjaga menara, dan bukan cuma "turun". Saat mendarat di kepalanya, pemain memilih:

- **Turun** seperti biasa (aman, tapi kehilangan progres), atau
- **Lawan** lewat duel dadu singkat. Menang = ular mati permanen dan dapat loot. Kalah = turun dan kehilangan HP.

**Varian ular:**

- *Ular Emas:* turun, tapi selalu kasih relic langka. Bikin dilema risk vs reward.
- *Ular Berbisa:* turun sekaligus kena efek racun (−1 HP per giliran selama 3 giliran).
- *Ular Lapar:* bergerak 1 petak tiap beberapa giliran.

### 4.3 Tangga

- *Tangga Batu:* naik.
- *Tangga Tali Lapuk:* naik, lalu tangganya hancur.
- *Lift Kurcaci:* naik kalau bayar gold.
- Pemain bisa **membangun tangga** sendiri lewat relic atau kartu, dan **memotong ular**.

### 4.4 Petak spesial

| Petak | Efek |
| --- | --- |
| Toko | Beli dadu, upgrade sisi, relic, potion HP |
| Chest | Pilih 1 dari 3 hadiah |
| Event | Kejadian naratif singkat dengan 2 pilihan |
| Kutukan | Debuff acak, tapi kasih gold besar |
| Api Unggun | Pulihkan HP atau upgrade dadu |
| Musuh | Duel dadu melawan monster dungeon (goblin, skeleton, slime, mimic) |
| Jebakan | Kena damage kalau mendarat di sini. Pemain bisa memicunya dari jauh waktu rival lewat. |

### 4.5 Duel dadu (combat)

Dibuat sederhana biar nggak ngerusak tempo:

- Pemain dan musuh sama-sama lempar dadu. Selisih angka = damage ke yang kalah.
- Pemain boleh pakai dadu mana pun dari tangan, plus efek relic.
- Musuh punya pola dadu yang kelihatan (telegraph), jadi pemain bisa berstrategi.
- Duel selesai dalam 1–3 lemparan.

### 4.6 Relic

Item pasif yang memicu efek lewat event hook. Contoh:

- **Sisik Ular:** tiap turun lewat ular, dapat +5 gold.
- **Tangga Lipat:** sekali per lantai, naik tangga dua kali lipat.
- **Taring Patah:** menang duel melawan ular memulihkan 2 HP.
- **Batu Berat:** semua dadu +1, tapi nggak bisa naik Lift Kurcaci.
- **Peta Kusam:** lihat isi 5 petak di depan.
- **Koin Bermuka Dua:** tiap hasil genap dapat 1 gold, tiap ganjil kehilangan 1 gold.
- **Lonceng Pengkhianat:** tiap kali rival kena ular atau jebakan, pemain dapat 10 gold.

### 4.7 Rival AI (masuk MVP)

Seorang petualang rival (nama kerja: *Varga si Pemburu Harta*) memanjat menara yang sama dan balapan ke pintu boss di tiap lantai.

- **Giliran:** rival jalan setelah pemain, pakai 2–3 dadu sendiri. Dadu dan pilihannya selalu kelihatan, jadi pemain bisa membaca rencananya.
- **Kepribadian:** tiap run rival dapat satu profil acak: *Agresif* (selalu pilih dadu terbesar), *Hati-hati* (menghindari ular), atau *Serakah* (mengejar chest dan gold).
- **Kalau rival sampai pintu boss duluan:** di lantai 1–2, dia mengambil hadiah lantai (pilihan 1 dari 3 hilang) dan pemain kena kutukan acak. Di lantai 3, rival duluan = run kalah.
- **Sabotase:** pemain bisa memicu Jebakan waktu rival lewat, memindahkan ular ke jalurnya, atau mengunci tangga pakai item.
- **Ketemu di petak yang sama:** terjadi duel dadu singkat. Pemenang mencuri gold atau meminjam satu dadu lawan selama 1 giliran.
- **Aturan papan berlaku sama:** ular, tangga, dan jebakan juga kena ke rival, jadi balapannya terasa adil.
- **AI:** pakai logika simulasi yang sama dengan validator papan. Rival menilai tiap dadu di tangannya dengan skor heuristik (jarak maju, risiko ular, nilai petak tujuan), ditambah bobot kepribadian dan sedikit acak biar nggak sempurna.

## 5. Struktur konten

- **3 lantai per run**, masing-masing 30 petak dan 1 boss
- **Lokasi MVP:** Menara Ular dengan tiga zona: Penjara Bawah Tanah (lantai 1), Katakomba (lantai 2), dan Puncak Menara (lantai 3). Satu tileset dungeon, beda palet dan dekorasi.
- **Boss contoh:**
  - *Raja Kobra* (lantai 1): tiap 2 giliran, nambah ular di papan duel
  - *Naga Tangga* (lantai 2): mencuri dadu tertinggi pemain selama 1 giliran
  - *Ouroboros* (lantai 3): kalau pemain kalah satu lemparan, HP boss pulih

## 6. Generator papan prosedural

**Aturan generasi:**

1. Tentukan posisi petak start dan finish.
2. Taruh tangga dan ular sesuai budget kesulitan lantai.
3. Taruh petak spesial dengan bobot (toko minimal 1, api unggun minimal 1 per lantai).
4. **Validasi:** jalankan simulasi (misalnya 500 run dengan dadu awal) buat memastikan papan bisa diselesaikan dan rata-rata jumlah giliran ada di rentang target.
5. Kalau gagal validasi, generate ulang dengan seed berikutnya.

**Seed:** tiap run punya seed, jadi bisa dipakai buat fitur daily challenge.

## 7. Arsitektur teknis (Unity)

- **Data-driven:** `DiceDefinition`, `RelicDefinition`, `TileDefinition`, `EnemyDefinition`, `RivalDefinition` sebagai ScriptableObject.
- **Event bus / hook system:** `OnTurnStart`, `OnRoll`, `OnMove`, `OnLand`, `OnSnake`, `OnLadder`, `OnDuelEnd`, `OnFloorEnd`. Relic cukup subscribe ke hook yang relevan.
- **Turn state machine:** `ChooseDice → Roll → Move → ResolveTile → RivalTurn → EndTurn`.
- **Logika terpisah dari view:** simulasi game jalan tanpa MonoBehaviour, jadi bisa di-unit test dan dipakai validator papan.
- **Rival AI:** `RivalBrain` mensimulasikan tiap pilihan dadu di atas model papan yang sama, memberi skor, lalu memilih dengan bobot kepribadian. Karena logikanya tanpa MonoBehaviour, AI ini bisa di-unit test dan diadu ribuan kali buat balancing.
- **Save/load run:** serialisasi state run ke JSON setiap akhir giliran (penting buat mobile, app bisa ditutup kapan saja).
- **RNG deterministik:** satu `RunRng` (SplitMix64) per run dari seed. Dipilih ketimbang `System.Random` karena state-nya satu `ulong` yang bisa disimpan ke save.
- **Analytics (opsional):** event `run_start`, `run_end`, `floor_reached`, `relic_picked`.

## 8. UI/UX

- Portrait, papan di tengah, tangan dadu di bawah (zona jempol).
- Tap dadu buat pilih, swipe ke atas buat lempar.
- Preview: saat dadu dipilih, petak tujuan yang mungkin di-highlight.
- Tooltip panjang-tekan untuk relic dan petak.
- Indikator rival: posisinya di papan, dadu yang mungkin dia pakai, dan progress bar balapan ke pintu boss.
- Layar: Main Menu, Pilih Karakter, Papan, Duel, Toko, Hadiah, Game Over, Meta Upgrade.

## 9. Art & audio

- **Gaya:** 2D cartoon atau pixel art bertema dungeon: lantai batu, obor, rantai, dan lumut. Papan tetap jelas terbaca sebagai grid ular tangga, dengan ular raksasa dan tangga batu atau kayu.
- **Juice:** animasi dadu menggelinding, kamera ngikutin pion, ular "nelan" pion, efek tangga berkilau, screen shake waktu duel, rival ngejek waktu pemain kena ular.
- **Audio:** SFX dadu yang satisfying (ini paling penting), musik ambient dungeon per zona.
- **Aset:** pakai aset gratis/berbayar berlisensi jelas (misalnya paket dungeon dari Kenney atau itch.io) dulu, ganti belakangan kalau sempat.

## 10. Monetisasi (opsional)

Untuk portofolio, cukup salah satu biar nunjukin skill integrasi:

- Rewarded ad: "lanjut sekali lagi" setelah kalah, atau gandakan Sisik di akhir run.
- IAP non-consumable: "Remove Ads" atau paket karakter.

Nggak pakai pay-to-win.

## 11. Scope MVP

**Masuk MVP:**

- 1 karakter, 1 tileset dungeon (3 zona), 3 lantai + 3 boss
- 10 dadu, 15 relic, 6 jenis petak, 4 musuh biasa
- Generator papan + validator
- Rival AI: 1 rival dengan 3 profil kepribadian, sabotase dasar (jebakan, pindah ular), dan duel di petak yang sama
- Meta progression: unlock dadu dan relic
- Save/load run, tutorial singkat

**Post-MVP:**

- Karakter kedua dan ketiga dengan dadu awal berbeda
- Menara kedua dengan tileset baru (misalnya gua es)
- Rival tambahan dengan dadu dan kemampuan unik
- Daily seed + leaderboard (Unity Gaming Services)

## 12. Roadmap (9 minggu, sambilan)

| Minggu | Target |
| --- | --- |
| 1 | Setup project, turn state machine, papan statis, dadu biasa |
| 2 | Ular, tangga, petak spesial dasar; logika terpisah dari view |
| 3 | Sistem dadu data-driven + hook system + 5 relic pertama |
| 4 | Duel dadu, musuh, boss pertama |
| 5 | Generator papan + validator, 3 lantai |
| 6 | Rival AI: giliran rival, profil kepribadian, sabotase, duel di petak yang sama |
| 7 | Toko, meta progression, save/load |
| 8 | Juice, audio, UI polish, tutorial |
| 9 | Playtest, balancing, build release, halaman Play Store |

## 13. Checklist rilis Play Store

- Akun Google Play Console (biaya sekali bayar)
- Closed testing dengan minimal 12 tester selama 14 hari (wajib untuk akun personal baru)
- Target API level terbaru, build AAB
- Privacy policy (wajib kalau pakai ads/analytics)
- Icon, feature graphic, 4–8 screenshot, video trailer 30 detik
- Rating konten (IARC)

## 14. Materi portofolio

- Repo GitHub publik dengan README yang jelasin arsitektur (hook system, generator, validator)
- Video gameplay 30–60 detik
- Devlog singkat: "Gimana caranya bikin ular tangga yang nggak murni luck"
- Devlog kedua: "Bikin AI rival yang terasa kayak manusia, bukan robot sempurna"
- Angka dari validator (misalnya distribusi jumlah giliran per papan) sebagai bukti desain berbasis data

## 15. Pertanyaan terbuka

- Duel dadu cukup seru, atau perlu sistem kartu ringan?
- Berapa ukuran tangan dadu yang ideal: 3 atau 4?
- Penalti rival di lantai 3 (langsung kalah) terlalu kejam? Alternatifnya: boss jadi lebih kuat.
