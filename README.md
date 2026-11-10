# XIO Puzzle Studio
### Builder + AI Configuration

**Rakit ESP32 impianmu sendiri — tanpa menulis sebaris kode pun.**

Bayangkan ESP32 sebagai papan puzzle kosong, dan aplikasi ini sebagai kotak kepingan yang sudah jadi. Kamu tinggal memilih kepingan yang kamu mau — sensor suhu, layar OLED, relay, servo, LoRa, GPS, sampai asisten AI — lalu menyusunnya lewat klik dan dropdown, bukan lewat baris kode yang bikin pusing. Setiap kali kamu menyalakan satu modul, menggeser satu pin di diagram silikon, atau menambah aturan "JIKA suhu > 30 MAKA nyalakan relay", kamu sedang merakit perangkat versimu sendiri. Ada kepuasan tersendiri saat melihat estimasi Flash/RAM bergerak dan peta pin-mu rapi tanpa konflik: rasa *"aku yang bikin ini"*, tanpa harus jadi programmer.

Yang kamu dapat:

- **Tanpa coding.** Nyalakan/matikan modul, klik pin pada diagram silikon, pilih aturan IF→THEN dari dropdown — semua lewat antarmuka. Mau lebih dalam? Suntikkan modul C++ kustom kapan saja.
- **Seluruh keluarga ESP32.** 11 profil siap pakai: Classic, WROVER, S2, S3, S3 Octal-PSRAM, C2, C3, C5, C6, H2, sampai P4. Ganti chip, diagram pin dan validasinya ikut menyesuaikan.
- **Kepuasan merakit.** Gudang modul, template rangkaian instan (I2C+OLED, ultrasonik+relay, timbangan HX711, LoRa, CAN/RS485, servo/motor), estimasi memori live, dan pengaman pin yang menolak konflik sebelum rangkaianmu jadi.
- **Konfigurasi AI sekali klik.** Pilih provider/model lalu ekspor header default; API key opsional dan tersamar di UI.
- **Ekosistem pendamping.** Rakit template Mosquitto, InfluxDB, Grafana, Node-RED, dan alat PC untuk menemani ESP-mu.

> **Jujur dulu:** aplikasi ini adalah **editor konfigurasi offline**, bukan firmware siap-flash dan bukan aplikasi chat. Ia menyusun *rancangan* fitur ESP32-mu. Firmware lengkap XIO (WebUI, AI di perangkat, P2P mesh chat, BLE, portal, sniff) hidup di proyek asli dan **tidak dibundel di sini**; semuanya tetap didokumentasikan di bawah supaya jelas. Memilih modul di editor tidak otomatis membuat driver/runtime-nya ada di ESP.

| Ringkasan | Keterangan |
| --- | --- |
| Aplikasi ini | Desktop Windows, C# 5, WPF, .NET Framework 4.8 |
| Target editor | 11 profil keluarga/varian ESP32 (Classic → P4) |
| Penyusunan proyek | 25 entri fitur bawaan, modul C++ kustom, sampai 32 slot pin dan 24 aturan |
| Konfigurasi AI | NVIDIA atau OpenRouter; API key opsional dan tersamar di UI |
| Keluaran | Profil JSON, header C++, template companion PC/server |
| Firmware siap-flash | **Tidak termasuk** |
| P2P chat / WebUI lengkap | Ada di proyek asli, **tidak termasuk** edisi ini |
| Izin publikasi | Hak kepemilikan dan redistribusi masih harus diverifikasi; lihat [NOTICE](NOTICE.md) |

**Mulai cepat:** [galeri tampilan](#galeri-tampilan-aplikasi) · [build desktop](#build-aplikasi-desktop) · [tutorial](#tutorial-penggunaan) · [pilihan ESP](#pilihan-esp-dan-kegunaannya) · [fitur asli XIO](#fitur-proyek-asli-xio-yang-pernah-dibuat) · [P2P chat](#p2p-mesh-chat-espxio) · [contoh profil aman](examples/default.xio.public.json)

---

## Daftar isi

- [Galeri tampilan aplikasi](#galeri-tampilan-aplikasi)
- [Dua wujud XIO: paket ini vs proyek asli](#dua-wujud-xio-paket-ini-vs-proyek-asli)
- [Fitur proyek asli XIO yang pernah dibuat](#fitur-proyek-asli-xio-yang-pernah-dibuat)
- [P2P Mesh Chat ESP/XIO](#p2p-mesh-chat-espxio)
- [Fitur paket Builder + AI ini](#fitur-paket-builder--ai-ini)
- [Pilihan ESP dan kegunaannya](#pilihan-esp-dan-kegunaannya)
- [Panduan lima halaman utama](#panduan-lima-halaman-utama)
- [Studio Pin dan keselamatan rangkaian](#studio-pin-dan-keselamatan-rangkaian)
- [Aturan otomasi dan konfigurasi jaringan](#aturan-otomasi-dan-konfigurasi-jaringan)
- [Konfigurasi AI](#konfigurasi-ai)
- [Modul C++ kustom](#modul-c-kustom)
- [Companion PC dan server](#companion-pc-dan-server)
- [Build aplikasi desktop](#build-aplikasi-desktop)
- [Tutorial penggunaan](#tutorial-penggunaan)
- [Jenis keluaran dan perlindungan rahasia](#jenis-keluaran-dan-perlindungan-rahasia)
- [Backend firmware dan alat USB opsional](#backend-firmware-dan-alat-usb-opsional)
- [Struktur source](#struktur-source)
- [Pengujian dan batas verifikasi](#pengujian-dan-batas-verifikasi)
- [Pertanyaan umum](#pertanyaan-umum)
- [Mengunggah ke GitHub](#mengunggah-ke-github)
- [Lisensi dan atribusi](#lisensi-dan-atribusi)

## Galeri tampilan aplikasi

Screenshot asli aplikasi desktop (mode terang, profil bersih tanpa rahasia). Klik gambar untuk memperbesar.

**1. Susun Puzzle — rakit fitur tanpa coding.** Nyalakan/matikan modul, pindah ke Gudang, atau suntikkan modul C++ kustom; estimasi Flash/RAM bergerak live di panel kiri.
![Susun Puzzle](docs/screenshots/01-susun-puzzle.png)

**2. Studio Pin — klik diagram silikon, bukan hafal nomor pin.** Template rangkaian instan dan matriks slot dengan pengaman pin berwarna (aman/ADC/USB/strap/terkunci).
![Studio Pin](docs/screenshots/02-studio-pin.png)

**3. Otomasi Lokal — aturan IF→THEN lewat dropdown.** Sumber sensor, operator, ambang, dan target; plus jaringan Wi-Fi/MQTT dengan sandi tersamar.
![Otomasi Lokal](docs/screenshots/03-otomasi-lokal.png)

**4. VPS & PC — ekosistem pendamping sekali rakit.** Mosquitto, Telegraf+InfluxDB, Grafana, Node-RED, WebSerial, dan jembatan Python PC.
![VPS & PC](docs/screenshots/04-vps-pc.png)

**5. Ekspor & AI — konfigurasi AI dan keluaran.** Provider/model, API key opsional tersamar, ekspor profil publik/header; tombol build/flash dinonaktifkan pada edisi ini.
![Ekspor & AI](docs/screenshots/05-ekspor-ai.png)

## Dua wujud XIO: paket ini vs proyek asli

| Aspek | Paket Builder + AI ini | Proyek asli XIO |
| --- | --- | --- |
| Bentuk | Source desktop Windows + generator konfigurasi | Firmware ESP32 + WebUI + skrip PC/TUI + builder |
| AI | Hanya ekspor header default provider/model/key | Chat AI berjalan di ESP melalui STA/HTTPS, plus TUI PC |
| P2P chat | Tidak ada | Ada: UDP mesh antar-PC + relay riwayat di ESP |
| WebUI perangkat | Tidak ada | Ada: dashboard, WiFi, AI, crypto, portal, BLE, sniff, files, settings, diag, CLI |
| Sensor/display/otomasi | Editor pin/rule/modul kustom | Implementasi perangkat sesuai firmware dan hardware |
| Serangan/audit RF | Tidak dibundel | Ada di firmware asli untuk penggunaan berizin; tidak dijelaskan sebagai tutorial serangan di sini |
| Rahasia | Contoh keyless; profil pribadi DPAPI | Pernah memakai key bawaan/rahasia lokal; **jangan unggah source asli apa adanya** |
| Cocok untuk GitHub publik | Setelah audit lisensi/isi | Perlu audit besar: lisensi upstream, rahasia, dan fitur ofensif |

Kalau tujuanmu adalah “semua fitur yang pernah kita buat ada semua”, maka yang dimaksud adalah **proyek asli XIO**, bukan ZIP Builder + AI ini. ZIP ini sengaja dibuat terpisah agar tidak membawa firmware, rahasia, packer, dan fitur ofensif ke repository publik.

## Fitur proyek asli XIO yang pernah dibuat

Bagian ini adalah **inventaris fitur proyek asli**, berdasarkan source firmware dan panduan lama. Ini bukan hasil uji ulang hardware hari ini, dan tidak berarti fitur-fitur ini ikut dalam paket Builder + AI.

### WebUI, koneksi, dan sistem

| Fitur | Fungsi | Catatan |
| --- | --- | --- |
| Hotspot ESP + WebUI mobile | ESP menjadi AP dan menyajikan antarmuka web | SSID/password hotspot bisa diubah dan disimpan |
| APSTA / Join WiFi luar | ESP tetap menjadi AP sambil terhubung ke router/hotspot berinternet | Diperlukan untuk AI cloud, ping/ARP scan |
| Connect / profil koneksi | Simpan, hubungkan, putuskan, dan uji profil WiFi | Termasuk validasi parameter dan status koneksi |
| QR WiFi | Membantu menyusun/memindai konfigurasi WiFi | Bukan jaminan semua format QR didukung |
| Dashboard | Status perangkat, radio, heap, koneksi, dan ringkasan data | Password tidak ditampilkan di status rutin |
| Settings | Hotspot, pengaturan perangkat, dan konfigurasi terkait | Kredensial dipanggil terpisah, bukan di polling rutin |
| Diag | Diagnostik heap/reset/memori | Membantu melacak restart dan kekurangan RAM |
| CLI web | Menjalankan perintah CLI dan membaca output | Output dipoll selama tab aktif |
| USB Direct | Akses dari PC melalui serial/protokol teks | Membuka port dapat mereset board |
| Reboot / info perangkat | Kontrol dasar perangkat | Bukan flashing otomatis |
| Pin studio web | Konfigurasi slot pin, aksi pin, dan I2C scan di perangkat | Bergantung firmware dan wiring |
| Scroll mobile/captive portal | Perbaikan scroll document untuk WebView Android | Fitur UI, bukan fitur RF |

### AI di ESP dan TUI PC

| Fitur | Fungsi | Catatan |
| --- | --- | --- |
| Chat AI di WebUI | Prompt dijawab oleh provider cloud melalui ESP | Membutuhkan STA berinternet dan RAM cukup |
| Streaming SSE | Membaca jawaban bertahap agar model lambat tidak mudah timeout | Bukan jaminan semua model cepat/stabil |
| Pilihan provider/model | NVIDIA/OpenRouter dan daftar model siap pakai | Ketersediaan model bergantung provider |
| Simpan/hapus API key | Key disimpan di NVS; status hanya `has/provider/model` | Key tidak dikirim ke browser pada status rutin |
| Upload lampiran teks | File teks/kode sementara untuk prompt AI | Dibatasi ukuran dan dibersihkan setelah dipakai |
| Skill AI | Preset peran/keahlian, termasuk Crypto Lab | Ini prompting, bukan bukti kemampuan formal |
| TUI `ai-esp.py` | Chat AI dari terminal, ganti model, status, dan uji koneksi | Berjalan di PC, bukan di ESP |
| Default AI dari builder | Kolom API default di EXE builder dan ekspor header | Paket ini membawa generatornya, bukan runtime chat |

### P2P Mesh Chat (ringkas)

Lihat bagian khusus [P2P Mesh Chat ESP/XIO](#p2p-mesh-chat-espxio). Ringkasnya: banyak PC bisa saling chat lewat UDP mesh terenkripsi, dengan ESP sebagai hub/relay riwayat dan pemicu jawaban AI grup.

### Wi-Fi discovery, monitoring, dan audit

| Kelompok | Contoh fitur yang pernah ada | Catatan |
| --- | --- | --- |
| Scan Wi-Fi | Scan cepat, scan lengkap AP+STA, station, probe, channel/sinyal | Hasil bergantung radio dan lingkungan |
| Daftar/inventaris | Daftar AP, station, SSID, ekspor CSV | CSV dibuat di browser; penyimpanan file butuh SD |
| Sniff | Raw capture, deauth/beacon sniff, PMKID/EAPOL, packet count/rate | Untuk analisis berizin; PCAP ke SD bila ada SD |
| Lab & Audit / frame flood | Preset deauth/beacon/probe dan variasi frame lain, auto-stop | **Hanya untuk perangkat sendiri/izin tertulis**; bukan jammer RF broadband |
| Portal | Evil Portal, template, karma, start/stop/clear | Fitur uji berizin; dapat mengganggu jaringan bila disalahgunakan |
| Network tools | Ping scan, ARP scan, set channel, MAC clone/acak | Ping/ARP butuh STA; port scan terbatas CLI |
| Tracking | Fox hunt, MAC tracker, remote ID, CSI motion, recon | Bergantung hardware dan data yang tertangkap |

### BLE, penyimpanan, dan utilitas

| Kelompok | Contoh fitur yang pernah ada | Catatan |
| --- | --- | --- |
| BLE scan | BLE umum, AirTag/FindMy, iBeacon, perangkat tertentu | Deteksi bergantung advertising dan lingkungan |
| BLE spam | Beberapa varian advertising/spoof | Hanya untuk uji berizin; bukan jammer Bluetooth murni |
| Files | Browse/upload/download/hapus file | Umumnya membutuhkan kartu SD |
| Backup/restore | Backup SPIFFS atau file tertentu | Backup dapat berisi rahasia |
| Crypto lab | Mesin enkripsi klasik client-side dan integrasi tanya AI | Alat belajar/eksperimen, bukan jaminan keamanan kriptografi modern |
| GPS/wardriving | Pernah ada di katalog/firmware tertentu | **Tidak tersedia pada board tanpa GPS** |
| Layar/LED fisik | Bergantung board | Board tanpa layar tidak punya menu TFT |

### Batas hardware yang sering disalahpahami

- Tanpa GPS: GPS tracker, wardriving berbasis GPS, geofence, dan upload WiGLE tidak dapat dipakai.
- Tanpa SD: penyimpanan PCAP/file/backup tertentu tidak dapat ditulis.
- Tanpa PSRAM: daftar hasil scan dan buffer dibatasi; AI TLS tetap sensitif terhadap heap.
- Tanpa layar: kontrol lewat web/serial, bukan menu TFT.
- Jammer RF broadband bukan fitur yang realistis di ESP32 biasa; yang ada adalah frame flood/advertising tertentu.
- Update firmware lewat web pernah dibatasi oleh layout partisi; flashing tetap lewat USB/COM pada konfigurasi tersebut.

### Catatan keamanan proyek asli

- Source/skrip lama pernah memuat **kunci API default tertanam** dan rahasia lokal. Jangan unggah `webui_secret.h`, skrip P2P lama, profil, atau biner berisi key apa adanya. Kunci yang pernah tertanam harus dianggap bocor dan dicabut/dirotasi.
- WebUI perangkat lama tidak boleh dianggap sebagai layanan publik yang aman tanpa autentikasi tambahan.
- Fitur ofensif hanya sah untuk perangkat sendiri atau engagement berizin. Dokumentasi ini tidak memberikan tutorial serangan.

## P2P Mesh Chat ESP/XIO

**Status:** fitur ini ada di proyek asli melalui `p2p_chat_tui.py` dan endpoint chat firmware. **Tidak termasuk dalam paket Builder + AI ini.**

### Arsitektur

```text
PC A ---- UDP mesh terenkripsi ---- PC B
  \                               /
   \                             /
    +---- HTTP /api/chat -------+
                 |
              ESP32
        hub / relay riwayat
        pemicu AI grup
```

| Komponen | Fungsi | Batas penting |
| --- | --- | --- |
| UDP mesh antar-PC | Broadcast beacon, discovery peer, kirim pesan grup | Default port UDP 8888; peer dianggap offline bila tidak terlihat sekitar 15 detik |
| Node ID + nickname | Identitas pengirim dan nama tampilan | Bukan autentikasi identitas kuat |
| Grup/channel | Misalnya GENERAL/OPS/DEV/SECRET | Grup adalah label pesan, bukan ruang terisolasi secara kriptografis |
| Reply/quote | Balas pesan berdasarkan ID | TUI menyimpan riwayat lokal; ESP hanya relay terbatas |
| `/ai` atau `@ai` | Meminta jawaban AI ke grup | Bisa lewat ESP atau fallback PC, bergantung koneksi/key |
| ESP relay | Menyimpan riwayat singkat dan memicu AI | Source saat ini memakai ring buffer **4 pesan**, bukan 25 seperti panduan lama |
| TUI | Antarmuka terminal fullscreen | Berjalan di PC, bukan firmware |

### Enkripsi: apa yang benar-benar dilindungi

- Jika library `cryptography` tersedia, payload UDP memakai **AES-256-GCM** dengan nonce acak dan kunci turunan PBKDF2-HMAC-SHA256.
- Jika `cryptography` tidak ada, skrip lama dapat jatuh ke fallback **XOR-hash**. Itu **bukan enkripsi aman** dan tidak boleh dipakai untuk komunikasi sensitif.
- Password bersama default harus diganti. Password yang sama harus dipakai semua node, tetapi password lemah tetap mudah ditebak.
- Enkripsi UDP mesh **tidak otomatis mengenkripsi HTTP ke ESP**. Endpoint `/api/chat` di firmware berjalan di jaringan lokal perangkat; jangan menganggapnya terlindungi TLS.
- Dekripsi lama dapat menerima paket tanpa field enkripsi. Artinya kompatibilitas tidak sama dengan kebijakan “wajib terenkripsi”.
- Skrip P2P lama pernah memuat kunci API default tertanam untuk fallback AI. **Jangan publikasikan skrip itu apa adanya**; cabut/rotasi kunci dan pindahkan konfigurasi ke environment/NVS yang aman.

### Kapasitas dan perilaku di ESP

- Ring buffer chat saat ini: **4 pesan**.
- Teks pesan POST dipotong sekitar **150 karakter**; struktur riwayat menyimpan teks hingga 159 karakter.
- Jawaban AI bisa lebih panjang, tetapi ketika masuk relay chat tetap terpotong oleh batas pesan.
- Riwayat hilang saat restart; ini bukan database chat permanen.
- AI grup tetap tunduk pada batas RAM/TLS dan koneksi STA.

### Perintah TUI yang pernah ada

| Perintah | Fungsi |
| --- | --- |
| `/help` | Bantuan |
| `/nick <nama>` | Ganti nickname |
| `/group <nama>` | Pindah/buat grup |
| `/pass <kunci>` | Ganti password enkripsi mesh |
| `/r <id> <pesan>` | Balas/kutip pesan |
| `/ai <pertanyaan>` | Tanya AI ke grup |
| `/peers` | Daftar node online |
| `/clear` | Bersihkan tampilan |
| `/quit` | Keluar |

Shortcut umum: Enter kirim, `r` reply, Tab pindah grup, Esc batal/keluar, PgUp/PgDn scroll, panah atas/bawah riwayat input.

## Fitur paket Builder + AI ini

| Kemampuan | Status | Batas penting |
| --- | --- | --- |
| Pilih chip, fitur, pin, aturan | Tersedia | Katalog statis, bukan deteksi board otomatis |
| Simpan/muat profil pribadi | Tersedia | Hanya key AI memakai DPAPI; profil tidak terenkripsi penuh |
| Ekspor profil publik | Tersedia | Membuang rahasia, teks bebas tertentu, dan kode kustom |
| Header Universal | Generator tersedia | Backend pembacanya tidak disertakan |
| Header default AI | Generator tersedia | Tidak chat, tidak inferensi, tidak uji key |
| Editor C++ kustom | Tersedia | Tidak memasang library atau memvalidasi C++ penuh |
| Companion PC/server | Generator template tersedia | Runtime container/Python/browser belum diuji paket ini |
| Pengukur Flash/RAM | Estimasi katalog | Bukan hasil compiler atau heap perangkat |
| Build/flash firmware | Nonaktif tanpa backend; selalu nonaktif di mode AI | Keberadaan file backend bukan bukti kompatibilitas |
| Alat USB opsional | Kontrol masih ada | Dapat memengaruhi perangkat; tidak diuji dalam paket ini |

### Untuk proyek ESP apa saja?

| Keperluan | Yang dibantu editor | Yang tetap harus disediakan |
| --- | --- | --- |
| Sensor lingkungan | Pin I2C/ADC, entri sensor, aturan, MQTT | Sensor, library, driver, firmware pembaca |
| OLED/LCD | Preset I2C, pemetaan SPI/I2C, modul C++ | Driver display, alamat, resolusi, rendering |
| Relay/kipas/pompa | Peran output/relay dan aturan | Driver daya, suplai, proteksi, firmware eksekutor |
| HX711/timbangan | Preset DOUT/SCK dan tombol | Load cell, kalibrasi, kode tare |
| Servo/motor/encoder | Peran PWM/DIR/encoder | Driver motor, suplai, batas mekanik |
| pH/TDS/DO/ADC | Peran analog dan aturan | Pengondisi sinyal, kalibrasi, kendali aman |
| GPS/SD | UART, geofence, MicroSD | Modul GNSS, parser, driver SD |
| RS485/CAN | Preset bus dan parameter | Transceiver, terminasi, library protokol |
| LoRa/radio pasif | Slot SPI/control | Modul RF, antena, firmware sesuai regulasi |
| Audio I2S | BCLK/WS/DIN | Mikrofon, driver I2S, pemrosesan sinyal |
| AI cloud | Provider/model/key default | Firmware klien, TLS, RAM, timeout, API |
| Dasbor PC/server | MQTT/InfluxDB/Grafana/Node-RED | Runtime, autentikasi, dashboard, uji jaringan |

## Pilihan ESP dan kegunaannya

Ada **11 profil katalog**, bukan 11 board yang sudah lulus uji hardware. Radio di bawah mengikuti deklarasi katalog, bukan hasil pengujian protokol.

| Profil / ID | Radio menurut katalog | Contoh arah penggunaan | Catatan |
| --- | --- | --- | --- |
| ESP32 Classic / `esp32` | Wi-Fi 2,4 GHz, BLE | GPIO, sensor, relay, telemetri, konfigurasi AI | WROOM-32D/32E/32U; GPIO bukan nomor kaki fisik |
| ESP32-WROVER / `esp32wrover` | Wi-Fi 2,4 GHz, BLE | Rancangan dengan PSRAM | GPIO16/17 terkunci untuk PSRAM |
| ESP32-S2 / `esp32s2` | Wi-Fi 2,4 GHz; tanpa BLE | Wi-Fi dan USB native | Jaga pin USB bila dipakai |
| ESP32-S3 / `esp32s3` | Wi-Fi 2,4 GHz, BLE | Display, sensor, audio, USB | Pilih varian memori yang tepat |
| ESP32-S3 OPI / `esp32s3_opi` | Wi-Fi 2,4 GHz, BLE | S3 dengan PSRAM OPI | GPIO33–37 terkunci |
| ESP32-C2 / `esp32c2` | Wi-Fi 2,4 GHz, BLE | Node sederhana | Verifikasi toolchain dan memori |
| ESP32-C3 / `esp32c3` | Wi-Fi 2,4 GHz, BLE | Node IoT ringkas | Wiring tiap board bisa berbeda |
| ESP32-C5 / `esp32c5` | Wi-Fi 2,4/5 GHz, BLE, flag Zigbee | Rencana dual-band | Dukungan core/protokol belum dibuktikan paket ini |
| ESP32-C6 / `esp32c6` | Wi-Fi 6 2,4 GHz, BLE, flag Zigbee | Node IoT C6 | Label Zigbee/Thread bukan bukti stack dibundel |
| ESP32-H2 / `esp32h2` | BLE, flag Zigbee; tanpa Wi-Fi | Rancangan non-Wi-Fi | Tidak bisa diasumsikan cloud via Wi-Fi internal |
| ESP32-P4 / `esp32p4` | Tanpa Wi-Fi/BLE internal dalam katalog | Antarmuka/periferal | Nirkabel butuh komponen terpisah |

Mode **Universal ESP** mencakup katalog ini. Mode **Konfigurasi AI** pada UI saat ini hanya untuk **ESP32 Classic/WROVER**. ESP8266/ESP8285 dan board non-ESP tidak punya profil khusus di edisi ini.

## Panduan lima halaman utama

| Halaman | Fungsi | Hasil |
| --- | --- | --- |
| **Susun Puzzle** | Filter/aktifkan modul, Gudang, modul C++ kustom | Susunan fitur dan source kustom |
| **Studio Pin** | Diagram GPIO, peran, parameter, preset, slot | Peta pin dan validasi dasar |
| **Otomasi Lokal** | Sumber, operator, ambang, target, jaringan | Data aturan/koneksi, bukan eksekusi desktop |
| **VPS & PC** | Companion bawaan/kustom | Template layanan di luar ESP |
| **Ekspor & AI** | Provider/model/key, profil/header, backend/USB opsional | Berkas konfigurasi |

Kontrol umum: pilihan chip/mode, tema terang/gelap, zoom, Simpan, muat profil. Datasheet hanya membuka PDF lokal bila tersedia; paket ini tidak menyertakan koleksi PDF.

### Kelompok fitur bawaan editor

| Kelompok | Entri | Maksud |
| --- | --- | --- |
| Inti | `gzip_webui`, `boot_guard`, `heap_guard`, `subnet_guard`, `rules_engine` | Konfigurasi inti bila backend mendukung |
| Sensor/display | `env_sensors`, `motion_force`, `oled_optical`, `ultrasonic`, `chem_bio`, `tof_bio_9dof`, `audio_i2s` | Pengelompokan kebutuhan periferal |
| Industri/robotik | `industrial`, `robotics`, `ros2_wg` | Bus, aktuator, encoder, integrasi |
| RF pasif | `rf_spectrum`, `lora_subghz`, `pkt_monitor` | Perencanaan pemantauan sah |
| GNSS/SD/modem | `gnss_rtc`, `sd_modem` | Lokasi/waktu, penyimpanan, modem |
| Telemetri/OTA | `telemetry_sec`, `ota_web` | Konfigurasi koneksi/pembaruan |
| AI/tambahan | `ai_cloud`, `crypto_qr`, `ble_stack` | Metadata; tidak semuanya punya makro Universal |

## Studio Pin dan keselamatan rangkaian

Editor mulai dengan **8 slot**, maksimum **32 slot**. Setiap slot memuat GPIO, peran, parameter 1/2, dan label.

| Kelompok peran | Pilihan |
| --- | --- |
| Digital | Output LOW/HIGH, relay active-low, pull-up/pull-down/floating, smart-button |
| Analog | Voltmeter, pH, TDS/EC, arus AC RMS, DO |
| Gerak | PWM, servo, motor PWM/DIR, encoder A/B |
| Bus | I2C SDA/SCL, UART RX/TX, RS485 DE/RE, SPI SCK/MISO/MOSI, SD-CS |
| Radio | nRF CE/CSN, LoRa CS/RST/DIO0 |
| Sensor khusus | Ultrasonik TRIG/ECHO, HX711 DOUT/SCK |
| CAN/I2S | CAN TX/RX, I2S BCLK/WS/DIN |

Validasi dasar menolak pin terkunci, duplikasi GPIO, output pada input-only, pull/button pada pin yang tidak memenuhi pemeriksaan, dan analog bukan ADC1. Validator **belum** menjamin semua konflik strap/USB, pasangan bus, rentang parameter, tegangan, atau arus aman.

Tanggung jawab perancang:

1. GPIO bukan keluaran daya; motor/pompa/beban relay butuh driver dan suplai sesuai.
2. Jangan menganggap input ESP toleran 5 V.
3. Periksa pin boot/strapping, USB, Flash, dan PSRAM pada board sebenarnya.
4. Jangan menduplikasi GPIO untuk beberapa perangkat I2C; gunakan bus bersama yang benar.
5. Setelah menghapus/mengurutkan slot, periksa target aturan karena target memakai indeks slot.
6. Untuk beban berbahaya, tambahkan proteksi hardware dan kondisi gagal-aman.

## Aturan otomasi dan konfigurasi jaringan

Editor mulai dengan **4 aturan**, maksimum **24 aturan**:

```text
JIKA [sumber] [operator] [ambang]
MAKA terapkan [nilai] ke [target]
```

Operator: `>`, `<`, `>=`, `<=`, `==`. Ada 18 sumber: suhu, kelembaban, cahaya, jarak ultrasonik, berat HX711, tegangan DC, pH, TDS, ADC slot pertama, CO2, jarak geofence, tombol satu klik, tombol dua klik, jarak ToF, DO, kebisingan audio, trafik Wi-Fi, RSSI LoRa.

Target bisa slot pin atau label aksi khusus AP/log SD/MQTT-webhook. Nilai aksi harus ditafsirkan sesuai backend; `1` tidak selalu berarti HIGH secara listrik.

| Pengaturan | Untuk apa | Batas |
| --- | --- | --- |
| SSID/sandi AP | Default hotspot perangkat | Tidak membuat hotspot di komputer |
| SSID/sandi STA | Default router perangkat | Tidak menghubungkan Wi-Fi komputer |
| MQTT host/port/topik | Telemetri dan companion | Tidak menguji broker |
| Webhook | Integrasi HTTP | Bukan jaminan klien HTTP/TLS lengkap |
| Endpoint VPS/WG | Field endpoint | **Bukan tunnel WireGuard/VPN** |
| Lat/lon/radius | Parameter geofence | Tidak membaca GPS di desktop |

Header mode AI hanya memuat default AI, bukan pin/rule/Wi-Fi/modul kustom.

## Konfigurasi AI

Bagian AI menyiapkan nilai bawaan untuk firmware kompatibel. Aplikasi desktop tidak mengirim prompt, tidak menjalankan model, dan tidak menghubungi penyedia.

| Provider | ID | Model default source |
| --- | --- | --- |
| NVIDIA | `nvidia` | `nvidia/nemotron-3.5-bolt-e4b` |
| OpenRouter | `openrouter` | `openai/gpt-4o-mini` |

Nama model adalah default konfigurasi, bukan janji model tersedia/gratis/teruji. Model kustom dipertahankan saat ganti provider, jadi periksa manual.

Validasi:

- Model kosong atau maksimal 96 karakter `[A-Za-z0-9_./:-]`.
- Key boleh kosong; jika diisi 20–512 karakter `[A-Za-z0-9_-]`.
- NVIDIA harus diawali `nvapi-`; OpenRouter `sk-or-`.
- Tidak menerima `Bearer`, spasi internal, tanda kutip, atau baris baru sebagai bagian key.
- **Kosongkan** hanya menghapus isian form, bukan mencabut key di provider atau menghapus header lama.

Contoh header tanpa rahasia: [examples/xio_ai_defaults.h](examples/xio_ai_defaults.h).

```cpp
#pragma once

#define XIO_CFG_AI_PROVIDER      "nvidia"
#define XIO_CFG_AI_MODEL         "nvidia/nemotron-3.5-bolt-e4b"
#define WEBUI_AI_DEFAULT_KEY     ""
#define WEBUI_AI_DEFAULT_OR_KEY  ""
```

| Makro | Makna |
| --- | --- |
| `XIO_CFG_AI_PROVIDER` | Provider bawaan |
| `XIO_CFG_AI_MODEL` | Model bawaan |
| `WEBUI_AI_DEFAULT_KEY` | Default key NVIDIA |
| `WEBUI_AI_DEFAULT_OR_KEY` | Default key OpenRouter |

Jika AI nonaktif/dikeluarkan dari rakitan, kedua key kosong dan generator memakai default NVIDIA/model default. PasswordBox hanya menyamarkan UI; header berisi key adalah plaintext dan key dalam firmware dapat diekstrak.

## Modul C++ kustom

| Bagian | Pemakaian |
| --- | --- |
| Judul/kategori | Identitas modul |
| Estimasi Flash | Angka perkiraan, bukan hasil compiler |
| Kode global | Deklarasi/include/fungsi pendukung |
| Kode setup | Inisialisasi dalam hook setup |
| Kode loop | Logika berulang dalam hook loop |
| Interval | Jadwal `millis()`; tidak positif berarti setiap pemanggilan hook |
| Ekspresi JSON | Nilai/fragmen telemetri; format harus diuji |

Mode Universal menghasilkan `xio_custom_user_modules.h` dengan hook `xioCustomUserSetup()`, `xioCustomUserLoop()`, dan `xioBuildCustomUserJson()`. Backend harus meng-include dan memanggil hook itu. Paket ini tidak membuktikan firmware eksternal melakukannya.

Batas: tanpa sandbox, tanpa pemasangan library otomatis, loop blocking tetap berbahaya, JSON memakai heuristik, dan ekspor publik menghapus modul kustom.

## Companion PC dan server

Tombol **Rakit Paket VPS & PC Tool** membuat berkas di `output_companion_stack/`; tidak memasang Docker atau menjalankan layanan otomatis.

| Pilihan | Isi yang dibuat | Batas |
| --- | --- | --- |
| Mosquitto | Compose + `mosquitto/config/mosquitto.conf` | Anonymous di dalam broker |
| Telegraf + InfluxDB | Compose + `telegraf/telegraf.conf` | Token/password wajib; runtime belum diuji |
| Grafana | Compose + datasource InfluxDB | **Dashboard grafik tidak dibuat otomatis** |
| Node-RED | Compose + `nodered/flows.json` | Flow MQTT masuk ke debug, bukan kontrol lengkap |
| WebSerial | `web_serial_console.html` | Konsol teks, **bukan flasher firmware** |
| Bridge Python | `xio_pc_ros2_bridge.py` | Parser `@ROS2` ke CSV, **bukan node ROS2** |
| Helper SDR | `xio_sdr_sweep_helper.py` | Wrapper `rtl_power`, bukan radio ESP |
| Docker kustom | Potongan YAML Compose | Tidak diaudit; bisa menjadi placeholder |

Port host bawaan dibatasi ke **127.0.0.1**: MQTT default 1883, WebSocket 9001, InfluxDB 8086, Grafana 3000, Node-RED 1880. ESP/ponsel lain tidak bisa langsung mengakses lewat LAN komputer. Jangan membuka binding ke luar tanpa autentikasi, TLS, dan firewall.

Environment sesuai service: `XIO_INFLUX_PASSWORD`, `XIO_INFLUX_TOKEN`, `XIO_GRAFANA_PASSWORD`. Jangan menaruh nilainya di GitHub. YAML kustom tidak otomatis mengikuti pembatasan template bawaan.

## Build aplikasi desktop

Prasyarat: Windows desktop interaktif, .NET Framework 4.8 + WPF + compiler lokal sesuai, dan Windows PowerShell 5.1 bila memakai skrip. MSBuild/IDE membutuhkan targeting pack 4.8. Arduino CLI/esptool/backend hanya untuk firmware opsional dan tidak dibundel.

```powershell
powershell -NoProfile -File .\scripts\build.ps1
# Hanya jika exit code 0:
.\bin\XIO_Puzzle_Builder.exe
```

Skrip memakai `csc.exe` lokal, mengutamakan Framework64 lalu Framework hanya bila compiler 64-bit tidak ditemukan. Gagal kompilasi tidak memicu fallback compiler. Tidak ada unduhan, NuGet, perubahan execution policy, peluncuran otomatis, build firmware, atau flashing. Kandidat build hanya menggantikan EXE lama bila sukses.

Jika execution policy menolak skrip, ikuti kebijakan administrator; jangan bypass. Alternatif proyek: [builder_app/XIO.Puzzle.Builder.csproj](builder_app/XIO.Puzzle.Builder.csproj), proyek MSBuild klasik tanpa SDK/NuGet. Daftar tujuh sumber eksplisit harus diperbarui bila file C# ditambah.

## Tutorial penggunaan

### A. Tanpa ESP dan tanpa rahasia

1. Jalankan EXE hasil build sukses.
2. Muat [examples/default.xio.public.json](examples/default.xio.public.json).
3. Contoh memakai ESP32 Classic, mode AI, NVIDIA, key kosong, pin/aturan kosong.
4. Buka **Ekspor & AI** untuk melihat provider/model.
5. Gunakan **Ekspor Profil Publik** untuk contoh berbagi.
6. Untuk melihat makro, tekan **Buat Header C++** dan baca peringatan. Tanpa backend, hasil di `exports/`.

Tidak perlu memilih COM, menghubungkan Wi-Fi ke ESP, atau membuka serial.

### B. Rancangan sensor dan OLED

1. Pilih chip dan mode **Universal ESP**.
2. Pilih kelompok sensor/display di **Susun Puzzle**.
3. Pakai preset **I2C + OLED / ToF** atau isi SDA/SCL manual.
4. Periksa diagram, pin terkunci, tegangan, dan alamat perangkat.
5. Tambahkan aturan contoh bila perlu.
6. Simpan profil pribadi atau ekspor publik.
7. **Buat Header C++** menghasilkan `xio_univ_profile.h` dan `xio_custom_user_modules.h`.

Hasilnya rancangan/header, bukan OLED yang sudah berfungsi.

### C. Default AI

1. Pilih ESP32 Classic/WROVER, lalu **Ekspor & AI → Pilih Konfigurasi AI**.
2. Pilih provider dan periksa model.
3. Biarkan key kosong untuk contoh berbagi.
4. Jika key pribadi disertakan, pahami header/firmware bisa mengungkapkannya.
5. **Buat Header C++** menghasilkan `exports/xio_ai_defaults.h` tanpa backend.
6. Integrasikan pada firmware yang memahami empat makro tersebut secara terpisah.

### D. Companion tanpa menjalankan layanan

1. Buka **VPS & PC** dan pilih companion.
2. Untuk MQTT → Telegraf → InfluxDB → Grafana, pilih semua service terkait.
3. Klik **Rakit Paket VPS & PC Tool**, lalu **Buka Folder Hasil**.
4. Tinjau konfigurasi dan berkas lama.
5. Siapkan rahasia hanya di lingkungan lokal.
6. Uji runtime/keamanan terpisah sebelum koneksi perangkat lain.

## Jenis keluaran dan perlindungan rahasia

| Jenis | Lokasi/nama umum | Layak diunggah? |
| --- | --- | --- |
| Source distribusi | `builder_app/`, `scripts/`, `tests/`, `examples/`, `docs/`, dokumen root | Setelah audit isi/lisensi |
| Profil pribadi | `*.xio.private.json` | **Tidak** |
| Profil publik | `*.xio.public.json` | Tinjau dulu |
| Header Universal | `exports/xio_univ_profile.h` | **Tidak**, bisa memuat kredensial |
| Header C++ kustom | `exports/xio_custom_user_modules.h` | Audit terpisah |
| Header AI | `exports/xio_ai_defaults.h` | **Tidak** bila berisi key; pakai contoh keyless |
| Companion | `output_companion_stack/` | Tidak otomatis; endpoint/YAML bisa sensitif |
| Build/backup | `bin/`, output firmware, backup flash | Jangan bagikan sembarangan |

Jika backend Universal ditemukan dan mode Universal dipilih, header ditulis ke folder sketch backend, bukan `exports/`. Berkas dengan nama yang sama dapat ditimpa ekspor berikutnya.

Profil publik mempertahankan ID chip, mode, provider/model, status fitur/companion dikenal, dan data numerik pin/aturan. Ia membuang key, nama proyek, identitas/sandi Wi-Fi, endpoint/topik, koordinat, label pin, nama aturan, entri tak dikenal, source C++ kustom, dan companion/YAML kustom. Profil publik bukan backup lengkap.

DPAPI hanya melindungi key AI untuk akun Windows penyimpan. Ia tidak mengenkripsi seluruh profil, tidak melindungi header/biner, dan tidak menggantikan rotasi key. Jika rahasia telanjur diunggah, cabut/rotasi di penyedia dan tangani salinan/riwayat repository.

## Backend firmware dan alat USB opsional

Backend berarti source firmware yang mengimplementasikan driver, makro, struktur pin/rule, hook modul, dan runtime. Jalur yang diperiksa editor:

```text
universal_firmware/xio_universal/xio_universal.ino
```

Paket ini tidak menyertakan direktori itu. Menyalin sembarang sketch ke sana tidak menjamin kompatibilitas. Verifikasi core, library, FQBN, Flash/PSRAM, partisi, protokol serial, dan lisensi.

| Kontrol | Maksud | Perhatian |
| --- | --- | --- |
| Segarkan port | Daftar COM | Tidak perlu untuk editor offline |
| Sinkron Pin | Kirim konfigurasi via protokol serial | Dapat mengubah konfigurasi perangkat |
| Deteksi Chip & MAC | Inspeksi esptool | Dapat mereset board |
| Baca Partisi | Baca tabel pada offset asumsi | Tidak membuktikan layout cocok |
| Backup Full Flash | Baca Flash ke berkas | Backup bisa berisi rahasia |
| Flash File .bin | Tulis biner pilihan | **Menimpa perangkat** |
| Serial Monitor / Kirim Perintah | Buka port dan teks | Dapat mereset board |
| Bersihkan terminal | Hapus log tampilan | Tidak menghapus data ESP/file |

Tanpa backend, yang nonaktif adalah build dan build+flash, **bukan seluruh alat USB**. Jangan klik alat USB hanya untuk mencoba UI.

## Struktur source

ZIP distribusi berisi **22 file** (17 source/dokumen + 5 screenshot). `bin/`, ekspor, cache, firmware, dan toolchain tidak ikut.

```text
XIO_Puzzle_Studio_Builder_AI/
├── .gitattributes
├── .gitignore
├── README.md
├── NOTICE.md
├── builder_app/
│   ├── AppleTheme.cs
│   ├── CodeSynthesizer.cs
│   ├── MainWindow.Freedom.cs
│   ├── MainWindow.cs
│   ├── Models.cs
│   ├── Program.cs
│   ├── UsbEspTools.cs
│   └── XIO.Puzzle.Builder.csproj
├── scripts/
│   ├── build.ps1
│   └── test.ps1
├── tests/
│   └── Builder.Tests.cs
├── examples/
│   ├── default.xio.public.json
│   └── xio_ai_defaults.h
└── docs/
    └── screenshots/
        ├── 01-susun-puzzle.png
        ├── 02-studio-pin.png
        ├── 03-otomasi-lokal.png
        ├── 04-vps-pc.png
        └── 05-ekspor-ai.png
```

| Source | Tanggung jawab |
| --- | --- |
| [Program.cs](builder_app/Program.cs) | Entry point dan metadata assembly |
| [AppleTheme.cs](builder_app/AppleTheme.cs) | Resource tema WPF |
| [MainWindow.cs](builder_app/MainWindow.cs) | Jendela, chip, pin/rule, AI, ekspor, USB/backend |
| [MainWindow.Freedom.cs](builder_app/MainWindow.Freedom.cs) | Katalog/Gudang, editor kustom, companion, simpan/muat, ekspor publik |
| [Models.cs](builder_app/Models.cs) | Model profil, katalog, validasi pin, DPAPI, ekspor publik |
| [CodeSynthesizer.cs](builder_app/CodeSynthesizer.cs) | Header, validasi AI, companion, payload konfigurasi |
| [UsbEspTools.cs](builder_app/UsbEspTools.cs) | Utilitas tool/USB opsional |
| [Proyek MSBuild](builder_app/XIO.Puzzle.Builder.csproj) | Target Framework dan daftar source |
| [build.ps1](scripts/build.ps1) | Build desktop |
| [test.ps1](scripts/test.ps1) | Kompilasi/jalankan tes |
| [Builder.Tests.cs](tests/Builder.Tests.cs) | Pemeriksaan konfigurasi/key/ekspor/UI |
| [Contoh profil](examples/default.xio.public.json) / [header](examples/xio_ai_defaults.h) | Contoh tanpa key |
| [docs/screenshots/](docs/screenshots/) | Lima screenshot asli aplikasi untuk galeri README |

## Pengujian dan batas verifikasi

```powershell
powershell -NoProfile -File .\scripts\test.ps1
```

Tes membuka jendela WPF dan memakai kredensial sintetis dalam memori. Tes tidak memanggil API, membangun firmware, menjalankan container, membuka serial, atau flashing.

| Pemeriksaan | Hasil yang pernah diperoleh |
| --- | --- |
| Kompilasi desktop langsung via compiler Framework | Lulus |
| Pemeriksaan C#/WPF | **68 pemeriksaan lulus** |
| Parsing sintaks PowerShell dan XML proyek | Lulus |
| Eksekusi end-to-end `.ps1` | **Belum diuji**; diblokir execution policy, kebijakan tidak diubah |
| IDE/MSBuild | Belum diuji end-to-end |
| Firmware/flash/USB/sensor/display/AI API/container | **Tidak diuji dalam validasi paket ini** |

68 pemeriksaan bukan coverage penuh, bukan audit seluruh source, dan bukan uji semua kombinasi 11 chip. Keberhasilan tes desktop tidak membuktikan rangkaian/firmware/cloud bekerja.

## Pertanyaan umum

**Apakah P2P chat ada di paket ini?**  
Tidak. P2P chat ada di proyek asli melalui `p2p_chat_tui.py` dan endpoint firmware. Paket ini hanya builder/generator konfigurasi.

**Apakah semua fitur lama hilang?**  
Tidak hilang dari proyek asli, tetapi tidak dibundel di ZIP ini. README mendokumentasikannya supaya jelas mana yang tersedia di mana.

**Mengapa build hanya menghasilkan EXE, bukan BIN?**  
Skrip build mengompilasi aplikasi Windows. BIN ESP membutuhkan backend firmware dan toolchain terpisah.

**Mengapa tombol Rakit Firmware nonaktif?**  
Backend Universal tidak disertakan atau sedang mode AI. Editor/ekspor tetap bisa dipakai.

**Apakah semua kartu modul sudah bekerja di ESP?**  
Tidak otomatis. Kartu adalah katalog; driver/runtime tidak dibundel atau diuji di sini.

**Bisakah memakai LCD apa saja?**  
Tidak otomatis. OLED I2C punya preset; LCD/TFT lain butuh library, wiring, dan source cocok.

**Apakah ESP32-S3 bisa untuk AI?**  
Secara umum bergantung firmware/layanan, tetapi mode konfigurasi AI di UI edisi ini dibatasi ke Classic/WROVER.

**Apakah key tersamar aman untuk GitHub?**  
Tidak. Penyamaran UI, DPAPI profil, dan keamanan header/biner adalah hal berbeda. Bagikan profil publik/contoh keyless.

**Mengapa kode kustom hilang di ekspor publik?**  
Disengaja agar source arbitrer tidak membawa rahasia. Audit modul terpisah bila ingin publikasi.

**Mengapa Grafana belum ada grafik?**  
Generator hanya membuat datasource. Dashboard, query, data, dan kredensial harus disiapkan.

**Mengapa ESP tidak bisa akses Mosquitto di PC?**  
Template bawaan loopback. Jangan sekadar membuka port; rancang autentikasi/TLS/firewall dulu.

**Apakah ada WireGuard/ROS2 lengkap?**  
Tidak. Field endpoint bukan VPN; helper Python bukan node ROS2.

**Apakah bisa di Linux/macOS/ponsel?**  
EXE ini WPF/.NET Framework Windows. Tidak ada dukungan resmi platform lain dalam source edisi ini.

## Mengunggah ke GitHub

1. Gunakan **ZIP source bersih terbaru**, bukan folder kerja yang sudah berisi build/ekspor.
2. Ekstrak sampai terlihat lima folder dan empat file root.
3. Di repository: **Add file → Upload files**.
4. Seret **`builder_app`, `scripts`, `tests`, `examples`, `docs` sebagai folder**, serta empat file root. Jangan ratakan isi folder.
5. Periksa daftar sebelum **Commit changes**.
6. Root harus menampilkan lima folder dan empat file root; total **22 file** termasuk isi folder (5 screenshot di `docs/screenshots/`).
7. README di root otomatis menjadi halaman penjelasan beserta galeri screenshot. `main` adalah branch, bukan file.

Jika sebelumnya hanya empat file root terunggah, tambahkan folder yang belum ada (`builder_app`, `scripts`, `tests`, `examples`, `docs`). Untuk memperbarui penjelasan, unggah `README.md` terbaru ke root yang sama.

Checklist:

- [ ] Struktur lengkap, bukan hanya README/ZIP.
- [ ] Tidak ada profil pribadi, key, password, endpoint privat, `.env`, backup, header berisi key, atau biner sensitif.
- [ ] Tidak ada `firmware/`, `universal_firmware/`, packer asal, cache, `bin/`, `output_*/`.
- [ ] Contoh JSON/header benar-benar tanpa kredensial.
- [ ] Klaim fitur sesuai batas edisi.
- [ ] Kepemilikan/lisensi upstream diperiksa sebelum Public.

**GitHub browser upload tidak otomatis mengikuti `.gitignore`.** `.gitignore` bukan pemindai rahasia dan tidak menghapus file yang sudah dilacak/riwayat. Pertahankan **Private** sampai izin distribusi selesai.

## Lisensi dan atribusi

Baca [NOTICE.md](NOTICE.md). Sumber builder diadaptasi dari salinan lokal proyek asal; lisensi upstream dan identitas revisi belum tercatat untuk paket ini.

Tidak ada LICENSE atau hibah MIT/GPL/lisensi lain yang diasumsikan. Nama library/perangkat/provider dalam katalog bukan bukti source mereka dibundel, diimplementasikan, atau boleh didistribusikan.

**Siap diunggah secara teknis bukan izin publikasi.** Pemilik harus memverifikasi kepemilikan source/aset dan kewajiban lisensi upstream sebelum publikasi publik. Backend, library, datasheet, dan tool terpisah memerlukan pemeriksaan izinnya sendiri.
