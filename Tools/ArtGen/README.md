# Roll & Ruin — Sprite Pack v0.1

86 sprite pixel art 16x16, tema dungeon fantasi. Dibuat ulang lewat `gen_sprites.py`.

## Import ke Unity
- Texture Type: Sprite (2D and UI)
- Pixels Per Unit: 16
- Filter Mode: Point (no filter)
- Compression: None
- Kamera: pakai Pixel Perfect Camera (URP 2D) biar tetap tajam

## Isi
- `tiles/` lantai (4 varian), dinding bata, rim petak (highlight, bahaya, rival)
- `dice/` putih, merah, biru, emas, ungu; sisi 0–6 (0 = sisi kosong buat Dadu Nekat)
- `pawns/` pion pemain (biru) dan rival (merah)
- `snakes/` hijau, emas, beracun; masing-masing head/body/tail. Body bisa di-tile vertikal; putar buat arah lain
- `ladders/` tangga batu, tangga tali, tangga tali lapuk, rantai + kabin Lift Kurcaci. Bisa di-tile vertikal
- `board_icons/` chest, jebakan, api unggun, toko, event, kutukan, obor (2 frame animasi)
- `enemies/` goblin, skeleton, slime, mimic
- `ui/` koin, HP, Sisik, panel dan tombol 9-slice (border 2px)
- `relics/` 7 relic dari GDD

## Extras (dibuat ulang lewat `gen_extras.py`, yang juga menjalankan `gen_sprites.py`)
- `portraits/` 32x32: Raja Kobra, Naga Tangga, Ouroboros, Varga. Versi `_framed` 40x40 pakai bingkai batu buat UI
- `app_icon/` `app_icon_512.png` (Play Store), `app_icon_192.png`, dan layer adaptive icon Android 432x432 (`adaptive_foreground_432.png` + `adaptive_background_432.png`)
- `animations/<nama>/` frame satuan, plus `<nama>_strip.png` (sprite sheet horizontal, slice by cell size)
- `previews/` GIF tiap animasi (diperbesar) buat dicek cepat

### Daftar animasi
| Animasi | Ukuran frame | Catatan |
| --- | --- | --- |
| dice_roll_{warna} | 16x24 | Tumble 7 frame, pivot bawah. Setelah frame terakhir tampilkan sprite sisi hasil asli |
| pawn_{player,rival}_hop | 16x24 | Squash → lompat → mendarat, pivot bawah |
| pawn_{player,rival}_idle | 16x24 | Napas 2 frame |
| snake_{warna}_tongue | 16x16 | Lidah keluar-masuk |
| enemy_slime_bounce | 16x20 | Squash & stretch |
| chest_open | 16x16 | Goyang → terbuka → kilau |
| trap_spikes_activate | 16x16 | Duri naik 3 tahap |
| campfire_flicker, torch_flicker | 16x16 | Loop |
| coin_spin | 16x16 | Loop |
| enemy_*_hit, boss_*_hit | 16x16 / 32x32 | Normal → putih → normal |
| boss_*_idle, char_varga_blink | 32x32 (Ouroboros 32x34) | Loop idle portrait |

Untuk hop dan dice roll, set pivot sprite ke Bottom Center biar posisi kaki/alas nggak loncat.
