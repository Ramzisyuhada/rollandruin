# Setup Scene Unity — Roll & Ruin

Panduan memasang scene Game setelah M1–M4. Semua logika ada di kode; yang perlu dilakukan di Editor cuma beberapa klik.

## 1. Buka project dan tunggu compile

1. Buka project di Unity 6000.6.5f1.
2. Tunggu sampai compile selesai (spinner di kanan bawah hilang).
3. Cek **Console**: tidak boleh ada error merah. Kalau ada, kirim isinya.

## 2. Import sprite (sekali saja)

Sprite di `Assets/_Project/Art/Sprites` otomatis di-set saat di-import: Sprite, 16 PPU, Point filter, tanpa kompresi, mesh FullRect, dan border 2px untuk `*_9slice`.

Kalau sprite masih terlihat sebagai Texture biasa, jalankan menu **Roll & Ruin → Reimport Sprites**.

## 3. Bangun scene

1. Menu **Roll & Ruin → Build Game Scene**.
2. Kalau ditanya menyimpan scene yang sedang terbuka, pilih sesuai kebutuhan.
3. Scene baru tersimpan di `Assets/_Project/Scenes/Game.unity` dan otomatis jadi scene pertama di **Build Profiles**.

Isi scene yang dibuat:

| Objek | Komponen | Fungsi |
| --- | --- | --- |
| `Systems` | `AnimationQueue`, Global Light 2D | Antrian animasi; cahaya supaya sprite URP 2D tidak gelap |
| `Board` | `BoardView` | Petak, ikon, ular, tangga, highlight (sprite sudah terisi) |
| `PlayerPawn`, `RivalPawn` | `SpriteRenderer`, `PawnView` | Pion pemain (biru) dan Varga (merah) |
| `Main Camera` | `Camera` (ortho), `BoardCameraFitter` | Menyesuaikan papan dengan rasio layar HP |
| `Canvas` | `HudView` + bar atas, pesan, info, zona jempol | HP/gold/lantai, tangan dadu, tombol Lempar/Sabotase/Menu |
| `Canvas/ChoicePanel` | `ChoicePanelView` | Semua keputusan: ular, lift, toko, chest, event, api unggun, hadiah, layar akhir |
| `EventSystem` | `InputSystemUIInputModule` | Input UI lewat Input System |
| `Game` | `GameBootstrap` | Menghubungkan Core dan View, save otomatis |

## 4. Coba main

1. Buka `Game.unity`, set Game view ke rasio portrait (misalnya 1080x1920).
2. Tekan **Play**.
3. Alur dasar:
   - Tap dadu sekali untuk memilih (petak tujuan di-highlight, merah = bahaya), tap lagi atau tekan **LEMPAR**.
   - Kena kepala ular: pilih **Lawan** atau **Turun**.
   - Saat duel: tap dadu mana pun untuk melempar. Dadu musuh berikutnya terlihat di teks info.
   - **Sabotase**: pasang jebakan atau pindahkan kepala ular ke depan Varga.
4. Kalau keluar dari Play mode di tengah run, run tersimpan. Saat Play lagi, muncul tawaran **Lanjutkan run?**.

Untuk menguji seed tertentu, isi field `Seed` di komponen `GameBootstrap` (0 = acak).

## 5. Lokasi save

Save ada di `Application.persistentDataPath` (Windows: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\RougeLike UlarTangga\`):

- `run.json`: run yang sedang berjalan (dihapus saat run selesai)
- `meta.json`: Sisik dan unlock (permanen)

Hapus kedua file itu untuk mulai dari nol.

## 6. Unit test

**Window → General → Test Runner → EditMode → Run All**. Saat ini ada 212 test.

## 7. Menu Editor lain

| Menu | Fungsi |
| --- | --- |
| Roll & Ruin → Validator Report | Generate 100 papan per lantai dan tampilkan statistik validator di Console (bahan devlog) |
| Roll & Ruin → Reimport Sprites | Terapkan ulang setting import sprite |

## Kalau ada masalah

| Gejala | Penyebab & solusi |
| --- | --- |
| Sprite hitam/gelap | Global Light 2D tidak terpasang. Tambahkan **Light 2D → Global Light 2D** di scene |
| Teks tidak muncul | Font bawaan Unity (`LegacyRuntime.ttf`) dipakai; pastikan modul UI aktif |
| Tombol tidak bisa ditekan | Cek `EventSystem` memakai `InputSystemUIInputModule`, bukan `StandaloneInputModule` |
| Papan terlalu kecil/besar | Atur `Top Reserved` / `Bottom Reserved` di `BoardCameraFitter` |
| Save lama bikin error | Hapus `run.json` (save yang rusak juga otomatis dibuang) |
