using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace XioPuzzleStudio
{
    public enum PinCapFlags
    {
        None        = 0x000,
        In          = 0x001,
        Out         = 0x002,
        Pwm         = 0x004,
        Adc1        = 0x008,
        Adc2        = 0x010,
        Strap       = 0x020,
        NativeUsb   = 0x040,
        LockedFlash = 0x080,
        LockedPsram = 0x100,
        LockedSys   = 0x200
    }

    public class HeaderPinVisual
    {
        public string Label { get; set; }
        public int Gpio { get; set; } // -1 for 3V3, GND, EN, 5V

        public HeaderPinVisual() { Label = ""; Gpio = -1; }
        public HeaderPinVisual(string label, int gpio) { Label = label; Gpio = gpio; }
    }

    public class EspChipProfile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Arch { get; set; }
        public string Fqbn { get; set; }
        public string EsptoolChip { get; set; }
        public string BootloaderAddr { get; set; }
        public string DatasheetPdf { get; set; }
        public int MaxGpio { get; set; }
        public bool HasWifi { get; set; }
        public bool HasWifi5G { get; set; }
        public bool HasBle { get; set; }
        public bool HasZigbee { get; set; }
        public bool HasTwaiCan { get; set; }
        public bool SupportsMarauderMode { get; set; }

        // Default safe pins per chip for 1-click presets
        public int DefSda { get; set; }
        public int DefScl { get; set; }
        public int DefUartRx { get; set; }
        public int DefUartTx { get; set; }
        public int DefSpiSck { get; set; }
        public int DefSpiMiso { get; set; }
        public int DefSpiMosi { get; set; }
        public int DefSpiCs { get; set; }
        public int DefAdc1 { get; set; }
        public int DefRelay { get; set; }
        public int DefBtn { get; set; }
        public int DefTrig { get; set; }
        public int DefEcho { get; set; }

        public List<HeaderPinVisual> LeftHeader { get; set; }
        public List<HeaderPinVisual> RightHeader { get; set; }
        public Func<int, PinCapFlags> CapResolver { get; set; }

        public EspChipProfile()
        {
            LeftHeader = new List<HeaderPinVisual>();
            RightHeader = new List<HeaderPinVisual>();
        }
    }

    public class PuzzleFeatureItem
    {
        public string Key { get; set; }
        public string UnivMacro { get; set; }
        public string MarauderMacro { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string SourceOrigin { get; set; }
        public string Description { get; set; }
        public int SavedFlashKb { get; set; }
        public int SavedRamKb { get; set; }
        public bool Enabled { get; set; }
        public bool LockedCore { get; set; }
        public bool Recommended { get; set; }
        public bool InActiveDeck { get; set; }
        public bool RequiresWifi { get; set; }
        public bool RequiresBle { get; set; }

        // User-Created Custom C++ Module Support (100% User Freedom!)
        public bool IsCustomUserModule { get; set; }
        public string CustomGlobalCode { get; set; }
        public string CustomSetupCode { get; set; }
        public string CustomLoopCode { get; set; }
        public int LoopIntervalMs { get; set; }
        public string CustomJsonExpr { get; set; }

        public PuzzleFeatureItem()
        {
            Key = ""; UnivMacro = ""; MarauderMacro = "";
            Category = "Modul Kustom User"; Title = ""; SourceOrigin = ""; Description = "";
            InActiveDeck = true;
            Enabled = true;
            LoopIntervalMs = 1000;
            CustomGlobalCode = "";
            CustomSetupCode = "";
            CustomLoopCode = "";
            CustomJsonExpr = "";
        }
    }

    public class CompanionPieceItem
    {
        public string Key { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string SourceOrigin { get; set; }
        public string Description { get; set; }
        public string PortOrFile { get; set; }
        public bool Enabled { get; set; }
        public bool InActiveDeck { get; set; }
        public bool IsCustom { get; set; }
        public string CustomServiceYaml { get; set; }

        public CompanionPieceItem()
        {
            Key = ""; Category = "Server VPS Docker"; Title = ""; SourceOrigin = ""; Description = "";
            PortOrFile = ""; Enabled = true; InActiveDeck = true; CustomServiceYaml = "";
        }
    }

    public class PinRoleOption
    {
        public int Code { get; set; }
        public int MarauderCode { get; set; }
        public string Name { get; set; }
        public string Hint { get; set; }

        public PinRoleOption(int code, int mCode, string name, string hint)
        {
            Code = code;
            MarauderCode = mCode;
            Name = name;
            Hint = hint;
        }

        public override string ToString() { return Name; }
    }

    public class PinSlotConfig
    {
        public int SlotIndex { get; set; }
        public int Gpio { get; set; }
        public int Role { get; set; }
        public int Param1 { get; set; }
        public int Param2 { get; set; }
        public string Label { get; set; }

        public PinSlotConfig()
        {
            Gpio = -1;
            Role = 0;
            Param1 = 0;
            Param2 = 0;
            Label = "";
        }
    }

    public class AutomationRuleConfig
    {
        public int Index { get; set; }
        public bool Enabled { get; set; }
        public string Name { get; set; }
        public int Source { get; set; }
        public int Op { get; set; }
        public double Threshold { get; set; }
        public int TargetSlot { get; set; }
        public int ActionVal { get; set; }

        public AutomationRuleConfig()
        {
            Enabled = false;
            Name = "";
            Source = 0;
            Op = 0;
            Threshold = 30.0;
            TargetSlot = 0;
            ActionVal = 1;
        }
    }

    public class XioProjectFile
    {
        public string ProjectName { get; set; }
        public string SelectedChipId { get; set; }
        public int BuildEngineMode { get; set; } // 0 = Universal ESP, 1 = Marauder Integrated
        public string ApSsid { get; set; }
        public string ApPass { get; set; }
        public string StaSsid { get; set; }
        public string StaPass { get; set; }
        public string AiProvider { get; set; }
        public string AiModel { get; set; }
        internal string AiApiKey { get; set; }
        public string ProtectedAiApiKey
        {
            get
            {
                if (string.IsNullOrEmpty(AiApiKey)) return "";
                return Convert.ToBase64String(ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(AiApiKey), null, DataProtectionScope.CurrentUser));
            }
            set
            {
                if (string.IsNullOrEmpty(value)) { AiApiKey = ""; return; }
                if (value.Length > 4096) throw new InvalidOperationException("Data kunci API profil terlalu panjang.");
                try
                {
                    AiApiKey = Encoding.UTF8.GetString(ProtectedData.Unprotect(
                        Convert.FromBase64String(value), null, DataProtectionScope.CurrentUser));
                }
                catch (CryptographicException)
                {
                    throw new InvalidOperationException("Kunci API profil hanya dapat dibuka oleh akun Windows yang menyimpannya. Masukkan ulang kunci di akun ini.");
                }
                catch (FormatException)
                {
                    throw new InvalidOperationException("Data kunci API profil tidak valid.");
                }
            }
        }
        public string MqttHost { get; set; }
        public int MqttPort { get; set; }
        public string MqttTopic { get; set; }
        public string WebhookUrl { get; set; }
        public string WireGuardEndpoint { get; set; }
        public double GeofenceLat { get; set; }
        public double GeofenceLon { get; set; }
        public double GeofenceRadiusM { get; set; }
        public Dictionary<string, bool> FeatureStates { get; set; }
        public List<string> RemovedDeckKeys { get; set; }
        public List<PuzzleFeatureItem> CustomUserModules { get; set; }
        public List<PinSlotConfig> Slots { get; set; }
        public List<AutomationRuleConfig> Rules { get; set; }
        public Dictionary<string, bool> CompanionStates { get; set; }
        public List<string> RemovedCompanionKeys { get; set; }
        public List<CompanionPieceItem> CustomCompanions { get; set; }

        public XioProjectFile()
        {
            ProjectName = "Proyek_Universal_XIO";
            SelectedChipId = "esp32";
            BuildEngineMode = 0;
            ApSsid = "XIO UNIVERSAL STUDIO";
            ApPass = "xio123456";
            StaSsid = "";
            StaPass = "";
            AiProvider = "nvidia";
            AiModel = "nvidia/nemotron-3.5-bolt-e4b";
            AiApiKey = "";
            MqttHost = "";
            MqttPort = 1883;
            MqttTopic = "xio/telemetry";
            WebhookUrl = "";
            WireGuardEndpoint = "";
            GeofenceLat = 0.0;
            GeofenceLon = 0.0;
            GeofenceRadiusM = 100.0;
            FeatureStates = new Dictionary<string, bool>();
            RemovedDeckKeys = new List<string>();
            CustomUserModules = new List<PuzzleFeatureItem>();
            Slots = new List<PinSlotConfig>();
            Rules = new List<AutomationRuleConfig>();
            CompanionStates = new Dictionary<string, bool>();
            RemovedCompanionKeys = new List<string>();
            CustomCompanions = new List<CompanionPieceItem>();
        }
    }

    public static class StudioCatalog
    {
        public static readonly string DatasheetDir = Path.Combine(
            Path.GetDirectoryName(typeof(StudioCatalog).Assembly.Location), "datasheets");

        public static List<PinRoleOption> GetAllRoles()
        {
            return new List<PinRoleOption>
            {
                new PinRoleOption(0,  0,  "00 — Kosong / Tidak Dipakai", "Pin bebas"),
                new PinRoleOption(1,  1,  "01 — Output Digital (Default LOW / OFF)", "LED, MOSFET, Buzzer aktif-HIGH"),
                new PinRoleOption(2,  2,  "02 — Output Digital (Default HIGH / ON)", "Enable modul aktif-HIGH"),
                new PinRoleOption(3,  3,  "03 — Relay Active-LOW (Anti-Cetek Saat Boot)", "Modul Relay optocoupler umum"),
                new PinRoleOption(4,  4,  "04 — Input Digital + Pull-Up Internal", "Saklar, Reed Switch, Limit Switch ke GND"),
                new PinRoleOption(5,  5,  "05 — Input Digital + Pull-Down Internal", "Sensor PIR / modul output HIGH"),
                new PinRoleOption(6,  6,  "06 — Input Digital Floating (Aman Pin Input-Only)", "Wajib untuk GPIO 34-39 ESP32 / GPIO 46 S2"),
                new PinRoleOption(7,  4,  "07 — Smart Button (1x Klik / 2x Klik / Tahan 3d Toggle AP)", "Tombol fisik pintar ke GND"),
                new PinRoleOption(8,  7,  "08 — PWM Dimmer / Buzzer (P1=Duty 0-255, P2=Hz)", "Kontrol kecerahan LED / frekuensi"),
                new PinRoleOption(9,  8,  "09 — ADC1 Voltmeter / Baterai (P1=Skala x100)", "Aman saat Wi-Fi aktif (mis P1=200 untuk divider 1:2)"),
                new PinRoleOption(10, 8,  "10 — ADC1 Sensor Kimia pH Air (0..14)", "Kalibrasi otomatis modul pH BNC 2.50V=pH7"),
                new PinRoleOption(11, 8,  "11 — ADC1 Sensor TDS / EC Kualitas Air (ppm)", "Kompensasi suhu otomatis jika sensor suhu aktif"),
                new PinRoleOption(12, 8,  "12 — ADC1 Arus Listrik AC RMS (EmonLib Style)", "Sensor CT Clamp SCT-013 tanpa pustaka luar"),
                new PinRoleOption(13, 9,  "13 — Bus I2C SDA (BME/SHT/AHT/INA/BH1750/MPU/OLED)", "Otomatis pindai & baca sensor I2C"),
                new PinRoleOption(14, 10, "14 — Bus I2C SCL", "Pasangan clock untuk Bus I2C SDA"),
                new PinRoleOption(15, 11, "15 — Bus UART Aux RX (GPS NMEA / Modem SIM AT / RS485)", "P1 = Baud/100 (mis: 96 untuk 9600, 1152 untuk 115200)"),
                new PinRoleOption(16, 12, "16 — Bus UART Aux TX", "Pasangan TX untuk GPS / Modem AT / RS485"),
                new PinRoleOption(17, 1,  "17 — RS485 Modbus RTU Pin DE/RE", "Otomatis HIGH saat kirim frame CRC16, LOW saat terima"),
                new PinRoleOption(18, 13, "18 — Bus SPI SCK (MicroSD / nRF24L01+)", "Clock SPI kustom"),
                new PinRoleOption(19, 14, "19 — Bus SPI MISO", "Data MISO SPI kustom"),
                new PinRoleOption(20, 15, "20 — Bus SPI MOSI", "Data MOSI SPI kustom"),
                new PinRoleOption(21, 16, "21 — MicroSD Card CS (SDSPI CSV Logger)", "Otomatis mount kartu SD & rekam CSV telemetri"),
                new PinRoleOption(22, 1,  "22 — Spektrum Pasif nRF24L01+ CE", "Mode terima murni RPD 2.4GHz (100% pasif tanpa jamming)"),
                new PinRoleOption(23, 16, "23 — Spektrum Pasif nRF24L01+ CSN", "Chip Select SPI untuk nRF24L01+"),
                new PinRoleOption(24, 1,  "24 — Ultrasonik HC-SR04 / JSN-SR04T TRIG", "Pin pemicu pulsa 10us"),
                new PinRoleOption(25, 6,  "25 — Ultrasonik HC-SR04 / JSN-SR04T ECHO", "Pin baca pantulan jarak cm"),
                new PinRoleOption(26, 6,  "26 — Timbangan Load Cell HX711 DOUT", "Pembaca ADC 24-bit presisi"),
                new PinRoleOption(27, 1,  "27 — Timbangan Load Cell HX711 SCK", "Clock HX711"),
                new PinRoleOption(28, 7,  "28 — Servo Motor 0-180 Derajat (P1=Sudut Awal)", "LEDC 50Hz 14-bit presisi 500-2500us"),
                new PinRoleOption(29, 7,  "29 — Motor DC H-Bridge PWM Speed (0-255)", "Kecepatan motor L298N / TB6612 / DRV8833"),
                new PinRoleOption(30, 1,  "30 — Motor DC H-Bridge DIR Arah (0/1)", "Pin arah putaran motor"),
                new PinRoleOption(31, 4,  "31 — Rotary Quadrature Encoder Pin A", "Pembaca putaran encoder gray-code"),
                new PinRoleOption(32, 4,  "32 — Rotary Quadrature Encoder Pin B", "Pasangan fase B encoder"),
                new PinRoleOption(33, 1,  "33 — Bus Industri CAN / TWAI TX (P1=kbps, mis 500)", "Transceiver SN65HVD230 / MCP2551 native TWAI"),
                new PinRoleOption(34, 6,  "34 — Bus Industri CAN / TWAI RX", "Pasangan RX bus CAN/TWAI"),
                new PinRoleOption(35, 16, "35 — Radio Sub-GHz LoRa SX1276/78 CS (P1=MHz: 433/868/915)", "Chip Select SPI modul LoRa Ra-02 / RFM95"),
                new PinRoleOption(36, 1,  "36 — Radio Sub-GHz LoRa RST", "Pin Reset modul LoRa"),
                new PinRoleOption(37, 6,  "37 — Radio Sub-GHz LoRa DIO0 / IRQ", "Pin interupsi terima paket LoRa"),
                new PinRoleOption(38, 8,  "38 — ADC1 Sensor Oksigen Terlarut / DO Air (mg/L)", "Kalibrasi suhu otomatis (P1=mV saturasi, default 1600)"),
                new PinRoleOption(39, 1,  "39 — Audio Digital I2S BCLK (Mikrofon MEMS INMP441)", "Bit Clock mikrofon digital I2S"),
                new PinRoleOption(40, 1,  "40 — Audio Digital I2S WS / LRCLK", "Word Select mikrofon digital I2S"),
                new PinRoleOption(41, 6,  "41 — Audio Digital I2S DIN / SD", "Data masuk audio 24-bit untuk pengukur dB SPL")
            };
        }

        public static List<CompanionPieceItem> CreateDefaultCompanions()
        {
            return new List<CompanionPieceItem>
            {
                new CompanionPieceItem {
                    Key = "mosquitto", Category = "Server VPS Docker",
                    Title = "Broker MQTT Eclipse Mosquitto 2.0",
                    SourceOrigin = "esp32_security_vps_browser (MQTT Stack)",
                    PortOrFile = "TCP 1883 / WS 9001", Enabled = true,
                    Description = "Server broker pesan ringan yang langsung disinkronkan dengan Topik & Port MQTT dari rakitan ESP Anda."
                },
                new CompanionPieceItem {
                    Key = "telegraf_influx", Category = "Server VPS Docker",
                    Title = "Kolektor Telegraf + Database Time-Series InfluxDB v2",
                    SourceOrigin = "iot-dashboard-mqtt-telegraf-influxdb-grafana.zip",
                    PortOrFile = "HTTP 8086 + telegraf.conf", Enabled = true,
                    Description = "Menangkap seluruh JSON sensor ESP dari MQTT secara otomatis dan menyimpannya ke database historis."
                },
                new CompanionPieceItem {
                    Key = "grafana", Category = "Server VPS Docker",
                    Title = "Panel Visualisasi Grafana IoT Realtime",
                    SourceOrigin = "iot-dashboard-mqtt-telegraf-influxdb-grafana.zip",
                    PortOrFile = "HTTP 3000", Enabled = true,
                    Description = "Dashboard grafik suhu, daya, jarak, kualitas air, dan spektrum siap buka di browser VPS."
                },
                new CompanionPieceItem {
                    Key = "nodered", Category = "Server VPS Docker",
                    Title = "Otomasi Visual Node-RED + Alur Flow ESP Otomatis",
                    SourceOrigin = "Node-RED-IoT-ESP32-MQTT-Docker.zip",
                    PortOrFile = "HTTP 1880 + flows.json", Enabled = true,
                    Description = "Server flow Node-RED lengkap dengan template node penangkap topik MQTT & pengirim perintah balik ke ESP."
                },
                new CompanionPieceItem {
                    Key = "webserial_flasher", Category = "Alat PC & Browser",
                    Title = "Aplikasi Web Flasher & Konsol Browser (WebSerial)",
                    SourceOrigin = "WebSerial_ESPTool.zip & ESPConnect.zip",
                    PortOrFile = "web_serial_console.html", Enabled = true,
                    Description = "File HTML mandiri berbasis WebSerial Chrome/Edge untuk memantau log USB, kirim perintah XIO/@ROS2, dan uji perangkat."
                },
                new CompanionPieceItem {
                    Key = "ros2_py_bridge", Category = "Alat PC & Robotik",
                    Title = "Jembatan Python PC (@ROS2 Serial <-> CSV / MQTT / ROS2)",
                    SourceOrigin = "XIO_HARDWARE_SOURCE (ros2_control / industrial_core)",
                    PortOrFile = "xio_pc_ros2_bridge.py", Enabled = true,
                    Description = "Skrip Python PC untuk membaca stream deterministik @ROS2 dari USB ESP, merekam CSV lokal, dan menggerakkan servo/motor."
                },
                new CompanionPieceItem {
                    Key = "sdr_companion", Category = "Alat PC & Spektrum SDR",
                    Title = "Pendamping Spektrum PC RTL-SDR / SoapyPower",
                    SourceOrigin = "esp32_rf_spectrum_defensive (rtl-sdr / soapy_power / qspectrumanalyzer)",
                    PortOrFile = "xio_sdr_sweep_helper.py", Enabled = false,
                    Description = "Skrip pendamping PC untuk memindai pita lebar Sub-GHz (433/868/915 MHz) memakai USB dongle RTL-SDR berdampingan dengan ESP."
                }
            };
        }

        public static List<PuzzleFeatureItem> CreateDefaultFeatures()
        {
            return new List<PuzzleFeatureItem>
            {
                // 1. Core & Reliability (100% Unlocked — User Can Turn Off or Remove Anything!)
                new PuzzleFeatureItem {
                    Key = "gzip_webui", UnivMacro = "XIO_U_GZIP_WEBUI", MarauderMacro = "XIO_FEAT_GZIP_WEBUI",
                    Category = "Inti & Optimasi", Title = "Kompresi GZIP WebUI Super Ringan",
                    SourceOrigin = "ESPHome / WLED WebServer", Enabled = true, Recommended = true, SavedFlashKb = 380, SavedRamKb = 8,
                    Description = "Antarmuka WebUI terkompresi >70% lebih hemat Flash. Matikan/kurangi jika menginginkan firmware Headless USB/Sensor murni!"
                },
                new PuzzleFeatureItem {
                    Key = "boot_guard", UnivMacro = "XIO_U_BOOT_GUARD", MarauderMacro = "XIO_FEAT_PIN_STUDIO",
                    Category = "Inti & Optimasi", Title = "Studio Pin Runtime + Safe Mode 3x Anti-Bootloop",
                    SourceOrigin = "WLED PinManager + Tasmota SafeBoot", Enabled = true, Recommended = true, SavedFlashKb = 18, SavedRamKb = 3,
                    Description = "Ubah fungsi GPIO lewat browser/USB tanpa compile ulang & otomatis lepas pin jika terjadi >=3 crash beruntun."
                },
                new PuzzleFeatureItem {
                    Key = "heap_guard", UnivMacro = "XIO_U_HEAP_GUARD", MarauderMacro = "XIO_FEAT_HEAP_GUARD",
                    Category = "Inti & Optimasi", Title = "Penjaga Blok SRAM Kontinu (Anti-OOM)",
                    SourceOrigin = "WLED Contiguous Heap Guard", Enabled = true, SavedFlashKb = 4, SavedRamKb = 1,
                    Description = "Memantau blok memori bebas terbesar (MALLOC_CAP_8BIT) dan membersihkan antrean sebelum ESP hang."
                },
                new PuzzleFeatureItem {
                    Key = "subnet_guard", UnivMacro = "XIO_U_SUBNET_GUARD", MarauderMacro = "XIO_FEAT_SUBNET_GUARD",
                    Category = "Inti & Optimasi", Title = "Penggeser Subnet Hotspot Otomatis (AP+STA)",
                    SourceOrigin = "Official Espressif ESP-IoT-Bridge", Enabled = true, RequiresWifi = true, SavedFlashKb = 5, SavedRamKb = 1,
                    Description = "Mencegah putusnya akses WebUI saat router rumah sama-sama memakai IP 192.168.4.x (otomatis geser ke 192.168.14.1)."
                },
                new PuzzleFeatureItem {
                    Key = "rules_engine", UnivMacro = "XIO_U_RULES_ENGINE", MarauderMacro = "",
                    Category = "Inti & Optimasi", Title = "Mesin Otomasi Lokal IF -> THEN + Smart Button",
                    SourceOrigin = "Tasmota Rules + ESPHome Automation", Enabled = true, SavedFlashKb = 14, SavedRamKb = 2,
                    Description = "Jalankan logika otomatis lokal tanpa internet (misal: Suhu > 32C nyalakan kipas, Tahan tombol 3 detik matikan/nyalakan AP)."
                },

                // 2. Sensors & Hardware P0-P1 (XIO_HARDWARE_SOURCE_PHASE_P0_P1)
                new PuzzleFeatureItem {
                    Key = "env_sensors", UnivMacro = "XIO_U_ENV_SENSORS", MarauderMacro = "XIO_FEAT_LIVE_SENSORS",
                    Category = "Sensor & Lingkungan", Title = "Driver Register I2C Lingkungan (BME280/SHT3x/AHT20/BH1750/SCD4x)",
                    SourceOrigin = "XIO_HARDWARE_SOURCE_PHASE_P0_P1 (Environmental)", Enabled = true, SavedFlashKb = 22, SavedRamKb = 2,
                    Description = "Baca suhu, kelembaban, tekanan udara, cahaya Lux, dan CO2 langsung dari register I2C tanpa library berat."
                },
                new PuzzleFeatureItem {
                    Key = "motion_force", UnivMacro = "XIO_U_MOTION_FORCE", MarauderMacro = "",
                    Category = "Sensor & Lingkungan", Title = "IMU Akselerometer/Giro (MPU6050/ADXL345) & Load Cell HX711",
                    SourceOrigin = "XIO_HARDWARE_SOURCE_PHASE_P0_P1 (Motion & Force)", Enabled = true, SavedFlashKb = 16, SavedRamKb = 2,
                    Description = "Deteksi kemiringan/getaran 6-axis serta timbangan digital 24-bit lengkap dengan fitur Tare nol."
                },
                new PuzzleFeatureItem {
                    Key = "oled_optical", UnivMacro = "XIO_U_OLED_OPTICAL", MarauderMacro = "XIO_FEAT_I2C_DOCTOR",
                    Category = "Sensor & Lingkungan", Title = "Layar Status OLED SSD1306 128x64 + Dokter Bus I2C",
                    SourceOrigin = "XIO_HARDWARE_SOURCE_PHASE_P0_P1 (Optical & Display)", Enabled = true, SavedFlashKb = 15, SavedRamKb = 2,
                    Description = "Tampilkan nama chip, suhu, tegangan, jarak, dan RAM langsung di layar OLED I2C 0x3C + pemindai chip I2C."
                },
                new PuzzleFeatureItem {
                    Key = "ultrasonic", UnivMacro = "XIO_U_ULTRASONIC", MarauderMacro = "",
                    Category = "Sensor & Lingkungan", Title = "Pengukur Jarak Ultrasonik HC-SR04 / JSN-SR04T",
                    SourceOrigin = "XIO_HARDWARE_SOURCE_PHASE_P0_P1 (Range)", Enabled = true, SavedFlashKb = 6, SavedRamKb = 1,
                    Description = "Pengukuran jarak permukaan air / objek (cm) non-blocking dengan batas aman timeout."
                },
                new PuzzleFeatureItem {
                    Key = "chem_bio", UnivMacro = "XIO_U_CHEM_BIO", MarauderMacro = "",
                    Category = "Sensor & Lingkungan", Title = "Sensor Kimia Air pH Meter & TDS/EC Terkompensasi Suhu",
                    SourceOrigin = "XIO_HARDWARE_SOURCE_PHASE_P0_P1 (Chemical & Bio)", Enabled = true, SavedFlashKb = 10, SavedRamKb = 1,
                    Description = "Kurva kalibrasi ADC1 untuk pemantauan kualitas air hidroponik, kolam, dan laboratorium."
                },
                new PuzzleFeatureItem {
                    Key = "tof_bio_9dof", UnivMacro = "XIO_U_TOF_BIO_9DOF", MarauderMacro = "",
                    Category = "Sensor & Lingkungan", Title = "Laser ToF VL53L0X, Bio-Medis MAX30102 SpO2, Oksigen DO & IMU ICM-20948",
                    SourceOrigin = "XIO_HARDWARE_SOURCE_PHASE_P0_P1 (Optical, Bio & 9-DOF)", Enabled = true, SavedFlashKb = 19, SavedRamKb = 3,
                    Description = "Pengukur jarak milimeter laser I2C (0x29), detak/SpO2 optik (0x57), oksigen terlarut air (mg/L), dan IMU 9-axis (0x69)."
                },
                new PuzzleFeatureItem {
                    Key = "audio_i2s", UnivMacro = "XIO_U_AUDIO_I2S", MarauderMacro = "",
                    Category = "Sensor & Lingkungan", Title = "Pengukur Desibel Mikrofon Digital I2S (INMP441 / ICS-43434)",
                    SourceOrigin = "ESP32_EVERYTHING_PHASE1 (ESP-ADF / Korvo Audio)", Enabled = true, SavedFlashKb = 24, SavedRamKb = 4,
                    Description = "Sampling audio digital 24-bit via bus I2S untuk menghitung tingkat kebisingan suara sekitar (dB SPL) secara real-time."
                },

                // 3. Industrial & Robotics
                new PuzzleFeatureItem {
                    Key = "industrial", UnivMacro = "XIO_U_INDUSTRIAL", MarauderMacro = "XIO_FEAT_UART_BRIDGE",
                    Category = "Industri & Robotik", Title = "Daya INA219 DC, Arus AC RMS, Modbus RTU RS485 & Bus CAN/TWAI",
                    SourceOrigin = "XIO_HARDWARE_SOURCE_PHASE_P0_P1 (Electrical & Industrial)", Enabled = true, SavedFlashKb = 28, SavedRamKb = 4,
                    Description = "Monitor daya baterai/surya, CT-clamp AC RMS (EmonLib), Modbus Master CRC16 + kontrol pin DE, serta native CAN Bus."
                },
                new PuzzleFeatureItem {
                    Key = "robotics", UnivMacro = "XIO_U_ROBOTICS", MarauderMacro = "",
                    Category = "Industri & Robotik", Title = "Robotik: Servo 0-180°, Motor H-Bridge PWM+DIR & Rotary Encoder",
                    SourceOrigin = "XIO_HARDWARE_SOURCE_PHASE_P0_P1 (Robotics)", Enabled = true, SavedFlashKb = 12, SavedRamKb = 2,
                    Description = "Kontrol aktuator servo presisi 50Hz 14-bit, kecepatan/arah motor DC, dan pembacaan pulsa encoder kuadratur."
                },
                new PuzzleFeatureItem {
                    Key = "ros2_wg", UnivMacro = "XIO_U_ROS2_WG", MarauderMacro = "",
                    Category = "Industri & Robotik", Title = "Jembatan Robotik Serial @ROS2 & Tunnel Heartbeat VPN/MicroLink",
                    SourceOrigin = "ros2_control + esp32_security_vps_browser (MicroLink)", Enabled = true, SavedFlashKb = 14, SavedRamKb = 2,
                    Description = "Protokol stream deterministik @ROS2/@ACT ke PC Linux/Python serta paket UDP keepalive otomatis ke endpoint VPS/WireGuard."
                },

                // 4. Passive RF Spectrum & Sub-GHz LoRa (esp32_rf_spectrum_defensive)
                new PuzzleFeatureItem {
                    Key = "rf_spectrum", UnivMacro = "XIO_U_RF_SPECTRUM", MarauderMacro = "",
                    Category = "Spektrum RF Pasif", Title = "Penganalisis Spektrum Pasif Wi-Fi Ch 1-14 & SPI nRF24L01+ RPD",
                    SourceOrigin = "esp32_rf_spectrum_defensive (RF24 & Wi-Fi Scanner)", Enabled = true, RequiresWifi = true, SavedFlashKb = 18, SavedRamKb = 3,
                    Description = "Visualisasi kepadatan kanal 2.4GHz & pemindaian energi 126-kanal nRF24L01+ murni mode terima (100% defensif tanpa gangguan)."
                },
                new PuzzleFeatureItem {
                    Key = "lora_subghz", UnivMacro = "XIO_U_LORA_SUBGHZ", MarauderMacro = "",
                    Category = "Spektrum RF Pasif", Title = "Radio Sub-GHz LoRa SX1276 / SX1278 (433 / 868 / 915 MHz)",
                    SourceOrigin = "esp32_rf_spectrum_defensive (RadioLib & LoraSA)", Enabled = true, SavedFlashKb = 16, SavedRamKb = 2,
                    Description = "Driver register SPI modul LoRa Ra-02 / RFM95 untuk pantau RSSI Sub-GHz dan penghitung paket masuk jarak jauh."
                },
                new PuzzleFeatureItem {
                    Key = "pkt_monitor", UnivMacro = "XIO_U_PKT_MONITOR", MarauderMacro = "",
                    Category = "Spektrum RF Pasif", Title = "Monitor Trafik Paket Wi-Fi Pasif (PPS) & Detektor Anomali Deauth",
                    SourceOrigin = "esp32_security_vps_browser (PacketMonitor32)", Enabled = true, RequiresWifi = true, SavedFlashKb = 12, SavedRamKb = 2,
                    Description = "Menghitung kepadatan paket per detik (Management/Data) secara pasif & memberi peringatan jika terdeteksi serangan Deauth."
                },

                // 5. GNSS, Storage & Cellular Modem (esp_wifi_sd_sim_sources_2)
                new PuzzleFeatureItem {
                    Key = "gnss_rtc", UnivMacro = "XIO_U_GNSS_RTC", MarauderMacro = "XIO_FEAT_GPS",
                    Category = "GNSS, SD & Modem", Title = "GNSS / GPS NMEA Parser + Alarm Geofence WGS84 + RTC DS3231",
                    SourceOrigin = "XIO_HARDWARE_SOURCE_PHASE_P0_P1 + Marauder GPS", Enabled = true, SavedFlashKb = 24, SavedRamKb = 4,
                    Description = "Dekode koordinat satelit $GPGGA/$GNRMC, hitung radius Haversine Geofence dalam meter, dan jam akurat DS3231."
                },
                new PuzzleFeatureItem {
                    Key = "sd_modem", UnivMacro = "XIO_U_SD_MODEM", MarauderMacro = "",
                    Category = "GNSS, SD & Modem", Title = "Perekam Data CSV MicroSD (SDSPI) & Konsol Modem Seluler SIM AT",
                    SourceOrigin = "esp_wifi_sd_sim_sources_2 (esp-modem & sdspi)", Enabled = true, SavedFlashKb = 35, SavedRamKb = 6,
                    Description = "Rekam otomatis riwayat sensor ke /xio_telemetry.csv di MicroSD dan uji sinyal/SIM modem 4G/LTE via perintah AT."
                },

                // 6. Cloud, OTA, Security & Marauder Extras (esp32_security_vps_browser)
                new PuzzleFeatureItem {
                    Key = "telemetry_sec", UnivMacro = "XIO_U_TELEMETRY_SEC", MarauderMacro = "",
                    Category = "Cloud, AI & Keamanan", Title = "Klien MQTT v3.1.1 Ringan, HTTP Webhook & Audit eFuse Keamanan",
                    SourceOrigin = "esp32_security_vps_browser", Enabled = true, RequiresWifi = true, SavedFlashKb = 26, SavedRamKb = 4,
                    Description = "Kirim JSON telemetri ke broker MQTT TCP / Webhook VPS tanpa library berat + periksa status Flash Encryption & Secure Boot v2."
                },
                new PuzzleFeatureItem {
                    Key = "ota_web", UnivMacro = "XIO_U_OTA_WEB", MarauderMacro = "",
                    Category = "Cloud, AI & Keamanan", Title = "Pembaruan Firmware Nirkabel Lewat Browser (Web OTA /api/ota)",
                    SourceOrigin = "esp32_security_vps_browser (espidf_ota)", Enabled = true, RequiresWifi = true, SavedFlashKb = 22, SavedRamKb = 3,
                    Description = "Unggah file firmware .bin rakitan berikutnya langsung lewat Wi-Fi tanpa perlu mencolokkan kabel USB lagi."
                },
                new PuzzleFeatureItem {
                    Key = "ai_cloud", UnivMacro = "", MarauderMacro = "XIO_FEAT_AI",
                    Category = "Cloud, AI & Keamanan", Title = "[Mode Terpadu] Mesin Cloud AI Multidisiplin (HTTPS TLS)",
                    SourceOrigin = "XIO Marauder Integrated Engine", Enabled = true, RequiresWifi = true, SavedFlashKb = 95, SavedRamKb = 18,
                    Description = "Asisten analisis AI lewat Wi-Fi STA pada mode firmware Terpadu. Matikan jika ingin menghemat ~95 KB Flash."
                },
                new PuzzleFeatureItem {
                    Key = "crypto_qr", UnivMacro = "", MarauderMacro = "XIO_FEAT_CRYPTO",
                    Category = "Cloud, AI & Keamanan", Title = "[Mode Terpadu] Mesin Kriptografi Klasik & Generator QR Wi-Fi",
                    SourceOrigin = "XIO Marauder Integrated Engine", Enabled = true, SavedFlashKb = 48, SavedRamKb = 6,
                    Description = "Enkripsi/dekripsi klasik dan pembuat kode QR koneksi cepat di WebUI Terpadu."
                },
                new PuzzleFeatureItem {
                    Key = "ble_stack", UnivMacro = "", MarauderMacro = "XIO_FEAT_BLE_WEB",
                    Category = "Spektrum RF Pasif", Title = "[Mode Terpadu] Tumpukan Bluetooth LE (NimBLE) & Detektor Tracker",
                    SourceOrigin = "XIO Marauder Integrated Engine", Enabled = true, RequiresBle = true, SavedFlashKb = 540, SavedRamKb = 68,
                    Description = "Pemindaian perangkat BLE & tracker sekitar. Mematikan modul ini menghemat ~540 KB Flash & ~68 KB SRAM!"
                }
            };
        }

        private static List<HeaderPinVisual> MakeHeader(params string[] items)
        {
            var list = new List<HeaderPinVisual>();
            foreach (string it in items)
            {
                int colon = it.IndexOf(':');
                if (colon > 0)
                {
                    int g = -1;
                    int.TryParse(it.Substring(colon + 1), out g);
                    list.Add(new HeaderPinVisual(it.Substring(0, colon), g));
                }
                else
                {
                    list.Add(new HeaderPinVisual(it, -1));
                }
            }
            return list;
        }

        public static List<EspChipProfile> CreateAllChips()
        {
            var chips = new List<EspChipProfile>();

            // 1. ESP32 Classic (WROOM-32D / 32E / 32U)
            chips.Add(new EspChipProfile {
                Id = "esp32",
                Name = "ESP32 Classic (WROOM-32D / 32E / 32U)",
                Arch = "Xtensa Dual-Core LX6 @ 240MHz",
                Fqbn = "esp32:esp32:esp32",
                EsptoolChip = "esp32",
                BootloaderAddr = "0x1000",
                DatasheetPdf = "esp32-wroom-32e_esp32-wroom-32ue_datasheet_en.pdf",
                MaxGpio = 39, HasWifi = true, HasBle = true, HasTwaiCan = true, SupportsMarauderMode = true,
                DefSda = 21, DefScl = 22, DefUartRx = 16, DefUartTx = 17,
                DefSpiSck = 18, DefSpiMiso = 19, DefSpiMosi = 23, DefSpiCs = 5,
                DefAdc1 = 34, DefRelay = 26, DefBtn = 27, DefTrig = 25, DefEcho = 35,
                LeftHeader = MakeHeader("3V3:-1","EN:-1","VP (36):36","VN (39):39","IO34:34","IO35:35","IO32:32","IO33:33","IO25:25","IO26:26","IO27:27","IO14:14","IO12:12","GND:-1","IO13:13","SD2 (9):9","SD3 (10):10","CMD (11):11","5V:-1"),
                RightHeader = MakeHeader("GND:-1","IO23:23","IO22:22","TX0 (1):1","RX0 (3):3","IO21:21","GND:-1","IO19:19","IO18:18","IO5:5","IO17:17","IO16:16","IO4:4","IO0:0","IO2:2","IO15:15","SD1 (8):8","SD0 (7):7","CLK (6):6"),
                CapResolver = delegate(int g) {
                    if (g < 0 || g > 39) return PinCapFlags.LockedSys;
                    if (g == 20 || g == 24 || (g >= 28 && g <= 31) || g == 37 || g == 38) return PinCapFlags.LockedSys;
                    if (g >= 6 && g <= 11) return PinCapFlags.LockedFlash;
                    if (g == 1 || g == 3) return PinCapFlags.LockedSys;
                    if (g == 34 || g == 35 || g == 36 || g == 39) return PinCapFlags.In | PinCapFlags.Adc1;
                    PinCapFlags c = PinCapFlags.In | PinCapFlags.Out | PinCapFlags.Pwm;
                    if (g == 32 || g == 33) c |= PinCapFlags.Adc1;
                    if (g == 0 || g == 2 || g == 4 || (g >= 12 && g <= 15) || g == 25 || g == 26 || g == 27) c |= PinCapFlags.Adc2;
                    if (g == 0 || g == 2 || g == 12 || g == 15) c |= PinCapFlags.Strap;
                    return c;
                }
            });

            // 2. ESP32-WROVER (PSRAM 4MB/8MB - locks GPIO 16 & 17!)
            chips.Add(new EspChipProfile {
                Id = "esp32wrover",
                Name = "ESP32-WROVER-E / B (PSRAM 4MB/8MB - IO16/17 Terkunci)",
                Arch = "Xtensa Dual-Core LX6 + QSPI PSRAM",
                Fqbn = "esp32:esp32:esp32wrover",
                EsptoolChip = "esp32",
                BootloaderAddr = "0x1000",
                DatasheetPdf = "esp32-wrover-e_esp32-wrover-ie_datasheet_en.pdf",
                MaxGpio = 39, HasWifi = true, HasBle = true, HasTwaiCan = true, SupportsMarauderMode = true,
                DefSda = 21, DefScl = 22, DefUartRx = 32, DefUartTx = 33,
                DefSpiSck = 18, DefSpiMiso = 19, DefSpiMosi = 23, DefSpiCs = 5,
                DefAdc1 = 34, DefRelay = 26, DefBtn = 27, DefTrig = 25, DefEcho = 35,
                LeftHeader = MakeHeader("3V3:-1","EN:-1","VP (36):36","VN (39):39","IO34:34","IO35:35","IO32:32","IO33:33","IO25:25","IO26:26","IO27:27","IO14:14","IO12:12","GND:-1","IO13:13","5V:-1"),
                RightHeader = MakeHeader("GND:-1","IO23:23","IO22:22","TX0 (1):1","RX0 (3):3","IO21:21","IO19:19","IO18:18","IO5:5","PSRAM (17):17","PSRAM (16):16","IO4:4","IO0:0","IO2:2","IO15:15","GND:-1"),
                CapResolver = delegate(int g) {
                    if (g < 0 || g > 39) return PinCapFlags.LockedSys;
                    if (g == 16 || g == 17) return PinCapFlags.LockedPsram;
                    if (g == 20 || g == 24 || (g >= 28 && g <= 31) || g == 37 || g == 38) return PinCapFlags.LockedSys;
                    if (g >= 6 && g <= 11) return PinCapFlags.LockedFlash;
                    if (g == 1 || g == 3) return PinCapFlags.LockedSys;
                    if (g == 34 || g == 35 || g == 36 || g == 39) return PinCapFlags.In | PinCapFlags.Adc1;
                    PinCapFlags c = PinCapFlags.In | PinCapFlags.Out | PinCapFlags.Pwm;
                    if (g == 32 || g == 33) c |= PinCapFlags.Adc1;
                    if (g == 0 || g == 2 || g == 4 || (g >= 12 && g <= 15) || g == 25 || g == 26 || g == 27) c |= PinCapFlags.Adc2;
                    if (g == 0 || g == 2 || g == 12 || g == 15) c |= PinCapFlags.Strap;
                    return c;
                }
            });

            // 3. ESP32-S2 (Single-Core LX7, Native USB, No BLE)
            chips.Add(new EspChipProfile {
                Id = "esp32s2",
                Name = "ESP32-S2 (WROOM / MINI-1 / Native USB OTG)",
                Arch = "Xtensa Single-Core LX7 @ 240MHz (Tanpa BLE)",
                Fqbn = "esp32:esp32:esp32s2",
                EsptoolChip = "esp32s2",
                BootloaderAddr = "0x1000",
                DatasheetPdf = "esp32-s2_datasheet_en.pdf",
                MaxGpio = 46, HasWifi = true, HasBle = false, HasTwaiCan = true, SupportsMarauderMode = false,
                DefSda = 8, DefScl = 9, DefUartRx = 17, DefUartTx = 18,
                DefSpiSck = 36, DefSpiMiso = 37, DefSpiMosi = 35, DefSpiCs = 34,
                DefAdc1 = 3, DefRelay = 10, DefBtn = 0, DefTrig = 11, DefEcho = 12,
                LeftHeader = MakeHeader("3V3:-1","IO0:0","IO1:1","IO2:2","IO3:3","IO4:4","IO5:5","IO6:6","IO7:7","IO8:8","IO9:9","IO10:10","IO11:11","IO12:12","IO13:13","IO14:14","IO15:15","IO16:16","IO17:17","5V:-1"),
                RightHeader = MakeHeader("GND:-1","TX0 (43):43","RX0 (44):44","IO42:42","IO41:41","IO40:40","IO39:39","IO38:38","IO37:37","IO36:36","IO35:35","IO34:34","IO33:33","IO21:21","USB+ (20):20","USB- (19):19","IO18:18","IN46:46","EN:-1","GND:-1"),
                CapResolver = delegate(int g) {
                    if (g < 0 || g > 46 || (g >= 22 && g <= 25)) return PinCapFlags.LockedSys;
                    if (g >= 26 && g <= 32) return PinCapFlags.LockedFlash;
                    if (g == 43 || g == 44) return PinCapFlags.LockedSys;
                    if (g == 46) return PinCapFlags.In | PinCapFlags.Strap;
                    PinCapFlags c = PinCapFlags.In | PinCapFlags.Out | PinCapFlags.Pwm;
                    if (g >= 1 && g <= 10) c |= PinCapFlags.Adc1;
                    if (g >= 11 && g <= 20) c |= PinCapFlags.Adc2;
                    if (g == 0 || g == 45) c |= PinCapFlags.Strap;
                    if (g == 19 || g == 20) c |= PinCapFlags.NativeUsb;
                    return c;
                }
            });

            // 4. ESP32-S3 Quad (WROOM-1 / MINI-1 / DevKitC-1)
            chips.Add(new EspChipProfile {
                Id = "esp32s3",
                Name = "ESP32-S3 (WROOM-1 / MINI-1 / DevKitC-1 Quad)",
                Arch = "Xtensa Dual-Core LX7 @ 240MHz + AI Vector + BLE 5",
                Fqbn = "esp32:esp32:esp32s3",
                EsptoolChip = "esp32s3",
                BootloaderAddr = "0x0",
                DatasheetPdf = "esp32-s3-wroom-1_wroom-1u_datasheet_en.pdf",
                MaxGpio = 48, HasWifi = true, HasBle = true, HasTwaiCan = true, SupportsMarauderMode = false,
                DefSda = 8, DefScl = 9, DefUartRx = 18, DefUartTx = 17,
                DefSpiSck = 12, DefSpiMiso = 13, DefSpiMosi = 11, DefSpiCs = 10,
                DefAdc1 = 4, DefRelay = 15, DefBtn = 0, DefTrig = 16, DefEcho = 7,
                LeftHeader = MakeHeader("3V3:-1","EN:-1","IO4:4","IO5:5","IO6:6","IO7:7","IO15:15","IO16:16","IO17:17","IO18:18","IO8:8","IO3:3","IO46:46","IO9:9","IO10:10","IO11:11","IO12:12","IO13:13","IO14:14","5V:-1","GND:-1"),
                RightHeader = MakeHeader("GND:-1","TX0 (43):43","RX0 (44):44","IO1:1","IO2:2","IO42:42","IO41:41","IO40:40","IO39:39","IO38:38","IO37:37","IO36:36","IO35:35","IO0:0","IO45:45","IO48:48","IO47:47","IO21:21","USB+ (20):20","USB- (19):19","GND:-1"),
                CapResolver = delegate(int g) {
                    if (g < 0 || g > 48 || (g >= 22 && g <= 25)) return PinCapFlags.LockedSys;
                    if (g >= 26 && g <= 32) return PinCapFlags.LockedFlash;
                    if (g == 43 || g == 44) return PinCapFlags.LockedSys;
                    PinCapFlags c = PinCapFlags.In | PinCapFlags.Out | PinCapFlags.Pwm;
                    if (g >= 1 && g <= 10) c |= PinCapFlags.Adc1;
                    if (g >= 11 && g <= 20) c |= PinCapFlags.Adc2;
                    if (g == 0 || g == 3 || g == 45 || g == 46) c |= PinCapFlags.Strap;
                    if (g == 19 || g == 20) c |= PinCapFlags.NativeUsb;
                    return c;
                }
            });

            // 5. ESP32-S3 Octal PSRAM (N16R8 / WROOM-2 - locks GPIO 33-37!)
            chips.Add(new EspChipProfile {
                Id = "esp32s3_opi",
                Name = "ESP32-S3 Octal PSRAM (N16R8 / WROOM-2 - IO33..37 Terkunci)",
                Arch = "Xtensa Dual-Core LX7 + Octal 8MB PSRAM",
                Fqbn = "esp32:esp32:esp32s3:PSRAM=opi",
                EsptoolChip = "esp32s3",
                BootloaderAddr = "0x0",
                DatasheetPdf = "esp32-s3-wroom-2_datasheet_en.pdf",
                MaxGpio = 48, HasWifi = true, HasBle = true, HasTwaiCan = true, SupportsMarauderMode = false,
                DefSda = 8, DefScl = 9, DefUartRx = 18, DefUartTx = 17,
                DefSpiSck = 12, DefSpiMiso = 13, DefSpiMosi = 11, DefSpiCs = 10,
                DefAdc1 = 4, DefRelay = 15, DefBtn = 0, DefTrig = 16, DefEcho = 7,
                LeftHeader = MakeHeader("3V3:-1","EN:-1","IO4:4","IO5:5","IO6:6","IO7:7","IO15:15","IO16:16","IO17:17","IO18:18","IO8:8","IO9:9","IO10:10","IO11:11","IO12:12","IO13:13","IO14:14","5V:-1"),
                RightHeader = MakeHeader("GND:-1","TX0 (43):43","RX0 (44):44","IO1:1","IO2:2","IO42:42","IO41:41","IO40:40","IO39:39","IO38:38","OPI (37):37","OPI (36):36","OPI (35):35","IO48:48","IO21:21","USB+ (20):20","USB- (19):19","GND:-1"),
                CapResolver = delegate(int g) {
                    if (g < 0 || g > 48 || (g >= 22 && g <= 25)) return PinCapFlags.LockedSys;
                    if (g >= 26 && g <= 32) return PinCapFlags.LockedFlash;
                    if (g >= 33 && g <= 37) return PinCapFlags.LockedPsram;
                    if (g == 43 || g == 44) return PinCapFlags.LockedSys;
                    PinCapFlags c = PinCapFlags.In | PinCapFlags.Out | PinCapFlags.Pwm;
                    if (g >= 1 && g <= 10) c |= PinCapFlags.Adc1;
                    if (g >= 11 && g <= 20) c |= PinCapFlags.Adc2;
                    if (g == 0 || g == 3 || g == 45 || g == 46) c |= PinCapFlags.Strap;
                    if (g == 19 || g == 20) c |= PinCapFlags.NativeUsb;
                    return c;
                }
            });

            // 6. ESP32-C2 / ESP8684
            chips.Add(new EspChipProfile {
                Id = "esp32c2",
                Name = "ESP32-C2 / ESP8684 (MINI-1 / WROOM-01C RISC-V)",
                Arch = "RISC-V 32-bit @ 120MHz + Wi-Fi 4 + BLE 5",
                Fqbn = "esp32:esp32:esp32c2",
                EsptoolChip = "esp32c2",
                BootloaderAddr = "0x0",
                DatasheetPdf = "esp8684_datasheet_en.pdf",
                MaxGpio = 20, HasWifi = true, HasBle = true, HasTwaiCan = false, SupportsMarauderMode = false,
                DefSda = 6, DefScl = 7, DefUartRx = 4, DefUartTx = 5,
                DefSpiSck = 6, DefSpiMiso = 2, DefSpiMosi = 7, DefSpiCs = 10,
                DefAdc1 = 0, DefRelay = 8, DefBtn = 9, DefTrig = 1, DefEcho = 3,
                LeftHeader = MakeHeader("3V3:-1","EN:-1","IO0 (ADC):0","IO1 (ADC):1","IO2 (ADC):2","IO3 (ADC):3","IO4 (ADC):4","GND:-1"),
                RightHeader = MakeHeader("5V:-1","GND:-1","TX0 (20):20","RX0 (19):19","IO18:18","IO10:10","IO9 (BOOT):9","IO8:8","IO7:7","IO6:6","IO5:5"),
                CapResolver = delegate(int g) {
                    if (g < 0 || g > 20) return PinCapFlags.LockedSys;
                    if (g >= 11 && g <= 17) return PinCapFlags.LockedFlash;
                    if (g == 19 || g == 20) return PinCapFlags.LockedSys;
                    PinCapFlags c = PinCapFlags.In | PinCapFlags.Out | PinCapFlags.Pwm;
                    if (g >= 0 && g <= 4) c |= PinCapFlags.Adc1;
                    if (g == 8 || g == 9) c |= PinCapFlags.Strap;
                    return c;
                }
            });

            // 7. ESP32-C3 / ESP8685 (SuperMini / MINI-1)
            chips.Add(new EspChipProfile {
                Id = "esp32c3",
                Name = "ESP32-C3 / ESP8685 (SuperMini / MINI-1 / WROOM-02)",
                Arch = "RISC-V 32-bit @ 160MHz + Native USB + BLE 5",
                Fqbn = "esp32:esp32:esp32c3",
                EsptoolChip = "esp32c3",
                BootloaderAddr = "0x0",
                DatasheetPdf = "esp32-c3_datasheet_en.pdf",
                MaxGpio = 21, HasWifi = true, HasBle = true, HasTwaiCan = true, SupportsMarauderMode = false,
                DefSda = 8, DefScl = 9, DefUartRx = 6, DefUartTx = 7,
                DefSpiSck = 4, DefSpiMiso = 5, DefSpiMosi = 6, DefSpiCs = 10,
                DefAdc1 = 0, DefRelay = 3, DefBtn = 9, DefTrig = 1, DefEcho = 2,
                LeftHeader = MakeHeader("5V:-1","GND:-1","3V3:-1","IO0 (ADC1):0","IO1 (ADC1):1","IO2 (ADC1):2","IO3 (ADC1):3","IO4 (ADC1):4"),
                RightHeader = MakeHeader("TX0 (21):21","RX0 (20):20","USB+ (19):19","USB- (18):18","IO10:10","IO9 (BOOT):9","IO8 (LED):8","IO7:7","IO6:6","IO5:5"),
                CapResolver = delegate(int g) {
                    if (g < 0 || g > 21) return PinCapFlags.LockedSys;
                    if (g >= 12 && g <= 17) return PinCapFlags.LockedFlash;
                    if (g == 20 || g == 21) return PinCapFlags.LockedSys;
                    PinCapFlags c = PinCapFlags.In | PinCapFlags.Out | PinCapFlags.Pwm;
                    if (g >= 0 && g <= 4) c |= PinCapFlags.Adc1;
                    if (g == 5) c |= PinCapFlags.Adc2;
                    if (g == 2 || g == 8 || g == 9) c |= PinCapFlags.Strap;
                    if (g == 18 || g == 19) c |= PinCapFlags.NativeUsb;
                    return c;
                }
            });

            // 8. ESP32-C5 (Dual-Band 2.4GHz + 5GHz Wi-Fi 6)
            chips.Add(new EspChipProfile {
                Id = "esp32c5",
                Name = "ESP32-C5 (Dual-Band 2.4 + 5 GHz Wi-Fi 6 / BLE 5)",
                Arch = "RISC-V 32-bit @ 240MHz + Dual-Band 2.4/5GHz",
                Fqbn = "esp32:esp32:esp32c5",
                EsptoolChip = "esp32c5",
                BootloaderAddr = "0x2000",
                DatasheetPdf = "esp32-c5_datasheet_en.pdf",
                MaxGpio = 28, HasWifi = true, HasWifi5G = true, HasBle = true, HasZigbee = true, HasTwaiCan = true, SupportsMarauderMode = false,
                DefSda = 8, DefScl = 9, DefUartRx = 4, DefUartTx = 5,
                DefSpiSck = 6, DefSpiMiso = 7, DefSpiMosi = 10, DefSpiCs = 23,
                DefAdc1 = 1, DefRelay = 24, DefBtn = 28, DefTrig = 2, DefEcho = 3,
                LeftHeader = MakeHeader("3V3:-1","EN:-1","IO0:0","IO1 (ADC):1","IO2 (ADC):2","IO3 (ADC):3","IO4 (ADC):4","IO5 (ADC):5","IO6 (ADC):6","IO7:7","GND:-1"),
                RightHeader = MakeHeader("5V:-1","GND:-1","TX0 (11):11","RX0 (12):12","USB- (13):13","USB+ (14):14","IO8:8","IO9:9","IO10:10","IO23:23","IO24:24","IO25:25","IO26:26","IO27:27","IO28:28"),
                CapResolver = delegate(int g) {
                    if (g < 0 || g > 28) return PinCapFlags.LockedSys;
                    if (g >= 15 && g <= 22) return PinCapFlags.LockedFlash;
                    if (g == 11 || g == 12) return PinCapFlags.LockedSys;
                    PinCapFlags c = PinCapFlags.In | PinCapFlags.Out | PinCapFlags.Pwm;
                    if (g >= 1 && g <= 6) c |= PinCapFlags.Adc1;
                    if (g == 2 || g == 7 || g == 27 || g == 28) c |= PinCapFlags.Strap;
                    if (g == 13 || g == 14) c |= PinCapFlags.NativeUsb;
                    return c;
                }
            });

            // 9. ESP32-C6 (Wi-Fi 6 + BLE 5 + Zigbee/Thread)
            chips.Add(new EspChipProfile {
                Id = "esp32c6",
                Name = "ESP32-C6 (WROOM-1 / MINI-1 Wi-Fi 6 + Zigbee/Thread)",
                Arch = "RISC-V 32-bit @ 160MHz + 802.11ax + 802.15.4",
                Fqbn = "esp32:esp32:esp32c6",
                EsptoolChip = "esp32c6",
                BootloaderAddr = "0x0",
                DatasheetPdf = "esp32-c6_datasheet_en.pdf",
                MaxGpio = 23, HasWifi = true, HasBle = true, HasZigbee = true, HasTwaiCan = true, SupportsMarauderMode = false,
                DefSda = 6, DefScl = 7, DefUartRx = 4, DefUartTx = 5,
                DefSpiSck = 19, DefSpiMiso = 20, DefSpiMosi = 18, DefSpiCs = 21,
                DefAdc1 = 0, DefRelay = 10, DefBtn = 9, DefTrig = 1, DefEcho = 2,
                LeftHeader = MakeHeader("3V3:-1","EN:-1","IO0 (ADC):0","IO1 (ADC):1","IO2 (ADC):2","IO3 (ADC):3","IO4 (ADC):4","IO5 (ADC):5","IO6 (ADC):6","IO7:7","IO8:8","IO9:9","GND:-1"),
                RightHeader = MakeHeader("5V:-1","GND:-1","TX0 (16):16","RX0 (17):17","USB- (12):12","USB+ (13):13","IO10:10","IO11:11","IO14:14","IO15:15","IO18:18","IO19:19","IO20:20","IO21:21","IO22:22","IO23:23"),
                CapResolver = delegate(int g) {
                    if (g < 0 || g > 23) return PinCapFlags.LockedFlash;
                    if (g == 16 || g == 17) return PinCapFlags.LockedSys;
                    PinCapFlags c = PinCapFlags.In | PinCapFlags.Out | PinCapFlags.Pwm;
                    if (g >= 0 && g <= 6) c |= PinCapFlags.Adc1;
                    if (g == 4 || g == 5 || g == 8 || g == 9 || g == 15) c |= PinCapFlags.Strap;
                    if (g == 12 || g == 13) c |= PinCapFlags.NativeUsb;
                    return c;
                }
            });

            // 10. ESP32-H2 (BLE 5 + Zigbee/Thread, No Wi-Fi)
            chips.Add(new EspChipProfile {
                Id = "esp32h2",
                Name = "ESP32-H2 (BLE 5 + Zigbee/Thread 802.15.4 - Non-WiFi)",
                Arch = "RISC-V 32-bit @ 96MHz (Khusus BLE/Zigbee/USB)",
                Fqbn = "esp32:esp32:esp32h2",
                EsptoolChip = "esp32h2",
                BootloaderAddr = "0x0",
                DatasheetPdf = "esp32-h2_datasheet_en.pdf",
                MaxGpio = 27, HasWifi = false, HasBle = true, HasZigbee = true, HasTwaiCan = true, SupportsMarauderMode = false,
                DefSda = 12, DefScl = 22, DefUartRx = 10, DefUartTx = 11,
                DefSpiSck = 4, DefSpiMiso = 0, DefSpiMosi = 5, DefSpiCs = 1,
                DefAdc1 = 2, DefRelay = 8, DefBtn = 9, DefTrig = 3, DefEcho = 13,
                LeftHeader = MakeHeader("3V3:-1","EN:-1","IO0:0","IO1 (ADC):1","IO2 (ADC):2","IO3 (ADC):3","IO4 (ADC):4","IO5 (ADC):5","IO8:8","IO9:9","GND:-1"),
                RightHeader = MakeHeader("5V:-1","GND:-1","TX0 (24):24","RX0 (23):23","USB- (26):26","USB+ (27):27","IO10:10","IO11:11","IO12:12","IO13:13","IO14:14","IO22:22","IO25:25"),
                CapResolver = delegate(int g) {
                    if (g < 0 || g > 27) return PinCapFlags.LockedSys;
                    if (g >= 15 && g <= 21) return PinCapFlags.LockedFlash;
                    if (g == 23 || g == 24) return PinCapFlags.LockedSys;
                    PinCapFlags c = PinCapFlags.In | PinCapFlags.Out | PinCapFlags.Pwm;
                    if (g >= 1 && g <= 5) c |= PinCapFlags.Adc1;
                    if (g == 8 || g == 9 || g == 25) c |= PinCapFlags.Strap;
                    if (g == 26 || g == 27) c |= PinCapFlags.NativeUsb;
                    return c;
                }
            });

            // 11. ESP32-P4 (Dual-Core 400MHz RISC-V)
            chips.Add(new EspChipProfile {
                Id = "esp32p4",
                Name = "ESP32-P4 (Dual-Core 400MHz RISC-V High-Performance)",
                Arch = "Dual RISC-V @ 400MHz + 55 GPIO + USB HS",
                Fqbn = "esp32:esp32:esp32p4",
                EsptoolChip = "esp32p4",
                BootloaderAddr = "0x2000",
                DatasheetPdf = "esp32-p4_datasheet_en.pdf",
                MaxGpio = 54, HasWifi = false, HasBle = false, HasTwaiCan = true, SupportsMarauderMode = false,
                DefSda = 7, DefScl = 8, DefUartRx = 20, DefUartTx = 21,
                DefSpiSck = 12, DefSpiMiso = 13, DefSpiMosi = 11, DefSpiCs = 10,
                DefAdc1 = 16, DefRelay = 26, DefBtn = 35, DefTrig = 27, DefEcho = 28,
                LeftHeader = MakeHeader("3V3:-1","EN:-1","IO0:0","IO1:1","IO2:2","IO3:3","IO4:4","IO5:5","IO6:6","IO7:7","IO8:8","IO9:9","IO10:10","IO11:11","IO12:12","IO13:13","IO16 (ADC):16","IO17 (ADC):17","GND:-1"),
                RightHeader = MakeHeader("5V:-1","GND:-1","TX0 (37):37","RX0 (38):38","USB- (24):24","USB+ (25):25","IO20:20","IO21:21","IO26:26","IO27:27","IO28:28","IO29:29","IO30:30","IO31:31","IO32:32","IO33:33","IO45:45","IO46:46","GND:-1"),
                CapResolver = delegate(int g) {
                    if (g < 0 || g > 54) return PinCapFlags.LockedSys;
                    if (g == 37 || g == 38) return PinCapFlags.LockedSys;
                    PinCapFlags c = PinCapFlags.In | PinCapFlags.Out | PinCapFlags.Pwm;
                    if (g >= 16 && g <= 23) c |= PinCapFlags.Adc1;
                    if (g == 34 || g == 35 || g == 36) c |= PinCapFlags.Strap;
                    if (g == 24 || g == 25) c |= PinCapFlags.NativeUsb;
                    return c;
                }
            });

            return chips;
        }

        public static string ValidateSlot(EspChipProfile chip, PinSlotConfig slot, HashSet<int> usedGpios)
        {
            if (slot.Role <= 0 || slot.Gpio < 0) return null;
            PinCapFlags caps = chip.CapResolver(slot.Gpio);
            if ((caps & PinCapFlags.LockedFlash) != 0)
                return String.Format("GPIO {0} pada {1} dikunci oleh Internal SPI Flash!", slot.Gpio, chip.Name);
            if ((caps & PinCapFlags.LockedPsram) != 0)
                return String.Format("GPIO {0} pada {1} dikunci oleh chip PSRAM!", slot.Gpio, chip.Name);
            if ((caps & PinCapFlags.LockedSys) != 0)
                return String.Format("GPIO {0} adalah pin UART0 / tidak tersedia di {1}!", slot.Gpio, chip.Name);

            if (usedGpios != null && usedGpios.Contains(slot.Gpio))
                return String.Format("Konflik PinManager! GPIO {0} dipakai oleh lebih dari satu slot!", slot.Gpio);

            bool needsOut = (slot.Role == 1 || slot.Role == 2 || slot.Role == 3 || slot.Role == 8 ||
                             slot.Role == 13 || slot.Role == 14 || slot.Role == 16 || slot.Role == 17 ||
                             slot.Role == 18 || slot.Role == 20 || slot.Role == 21 || slot.Role == 22 ||
                             slot.Role == 23 || slot.Role == 24 || slot.Role == 27 || slot.Role == 28 ||
                             slot.Role == 29 || slot.Role == 30 || slot.Role == 33 || slot.Role == 35 ||
                             slot.Role == 36 || slot.Role == 39 || slot.Role == 40);
            if (needsOut && (caps & PinCapFlags.Out) == 0)
                return String.Format("GPIO {0} pada {1} hanya Input-Only (tidak bisa jadi Output/Bus)!", slot.Gpio, chip.Name);

            if ((slot.Role == 4 || slot.Role == 5 || slot.Role == 7) && (caps & PinCapFlags.Out) == 0)
                return String.Format("GPIO {0} tidak punya resistor Pull-Up/Down internal; gunakan Input Floating!", slot.Gpio);

            bool needsAdc1 = ((slot.Role >= 9 && slot.Role <= 12) || slot.Role == 38);
            if (needsAdc1 && (caps & PinCapFlags.Adc1) == 0)
            {
                if ((caps & PinCapFlags.Adc2) != 0)
                    return String.Format("GPIO {0} berada di ADC2 yang bentrok dengan radio Wi-Fi! Pilih pin bertanda [ADC1].", slot.Gpio);
                return String.Format("GPIO {0} bukan pin analog ADC1 pada {1}!", slot.Gpio, chip.Name);
            }

            if (usedGpios != null) usedGpios.Add(slot.Gpio);
            return null;
        }

        public static string GeneratePublicProjectJson(XioProjectFile source)
        {
            if (source == null) throw new ArgumentNullException("source");

            string selectedChipId = null;
            foreach (EspChipProfile chip in CreateAllChips())
            {
                if (string.Equals(chip.Id, source.SelectedChipId, StringComparison.Ordinal))
                {
                    selectedChipId = chip.Id;
                    break;
                }
            }
            if (selectedChipId == null)
                throw new ArgumentException("ID chip profil publik tidak valid.", "source");
            if (source.BuildEngineMode != 0 && source.BuildEngineMode != 1)
                throw new ArgumentException("Mode build profil publik tidak valid.", "source");

            // Validate only shareable AI settings; never read or protect the source key.
            var aiMeta = new XioProjectFile {
                AiProvider = source.AiProvider,
                AiModel = source.AiModel ?? "",
                AiApiKey = ""
            };
            if (CodeSynthesizer.ValidateAiDefaults(aiMeta) != null)
                throw new ArgumentException("Pengaturan AI profil publik tidak valid.", "source");

            var featureStates = new Dictionary<string, bool>(StringComparer.Ordinal);
            var removedDeckKeys = new List<string>();
            foreach (PuzzleFeatureItem feature in CreateDefaultFeatures())
            {
                bool enabled;
                if (source.FeatureStates != null && source.FeatureStates.TryGetValue(feature.Key, out enabled))
                    featureStates.Add(feature.Key, enabled);
                if (source.RemovedDeckKeys != null && source.RemovedDeckKeys.Contains(feature.Key))
                    removedDeckKeys.Add(feature.Key);
            }

            var companionStates = new Dictionary<string, bool>(StringComparer.Ordinal);
            var removedCompanionKeys = new List<string>();
            foreach (CompanionPieceItem companion in CreateDefaultCompanions())
            {
                bool enabled;
                if (source.CompanionStates != null && source.CompanionStates.TryGetValue(companion.Key, out enabled))
                    companionStates.Add(companion.Key, enabled);
                if (source.RemovedCompanionKeys != null && source.RemovedCompanionKeys.Contains(companion.Key))
                    removedCompanionKeys.Add(companion.Key);
            }

            // Allowlist nested fields too, so future model properties cannot leak into public JSON.
            var slots = new List<Dictionary<string, object>>();
            if (source.Slots != null)
            {
                foreach (PinSlotConfig slot in source.Slots)
                {
                    if (slot == null)
                        throw new ArgumentException("Slot profil publik tidak valid.", "source");
                    slots.Add(new Dictionary<string, object> {
                        { "SlotIndex", slot.SlotIndex },
                        { "Gpio", slot.Gpio },
                        { "Role", slot.Role },
                        { "Param1", slot.Param1 },
                        { "Param2", slot.Param2 },
                        { "Label", "" }
                    });
                }
            }

            var rules = new List<Dictionary<string, object>>();
            if (source.Rules != null)
            {
                foreach (AutomationRuleConfig rule in source.Rules)
                {
                    if (rule == null || double.IsNaN(rule.Threshold) || double.IsInfinity(rule.Threshold))
                        throw new ArgumentException("Aturan profil publik tidak valid.", "source");
                    rules.Add(new Dictionary<string, object> {
                        { "Index", rule.Index },
                        { "Enabled", rule.Enabled },
                        { "Name", "" },
                        { "Source", rule.Source },
                        { "Op", rule.Op },
                        { "Threshold", rule.Threshold },
                        { "TargetSlot", rule.TargetSlot },
                        { "ActionVal", rule.ActionVal }
                    });
                }
            }

            var publicProject = new Dictionary<string, object> {
                { "SelectedChipId", selectedChipId },
                { "BuildEngineMode", source.BuildEngineMode },
                { "AiProvider", aiMeta.AiProvider },
                { "AiModel", aiMeta.AiModel },
                { "FeatureStates", featureStates },
                { "RemovedDeckKeys", removedDeckKeys },
                { "Slots", slots },
                { "Rules", rules },
                { "CompanionStates", companionStates },
                { "RemovedCompanionKeys", removedCompanionKeys }
            };
            var ser = new JavaScriptSerializer();
            ser.MaxJsonLength = 4 * 1024 * 1024;
            return ser.Serialize(publicProject);
        }

        public static void SaveProjectJson(string path, XioProjectFile proj)
        {
            var ser = new JavaScriptSerializer();
            ser.MaxJsonLength = 4 * 1024 * 1024;
            string json = ser.Serialize(proj);
            File.WriteAllText(path, json, System.Text.Encoding.UTF8);
        }

        public static XioProjectFile LoadProjectJson(string path)
        {
            var ser = new JavaScriptSerializer();
            ser.MaxJsonLength = 4 * 1024 * 1024;
            string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
            return ser.Deserialize<XioProjectFile>(json);
        }
    }
}
