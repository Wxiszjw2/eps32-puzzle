using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace XioPuzzleStudio
{
    public class PartitionEntryInfo
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public string SubTypeName { get; set; }
        public string OffsetHex { get; set; }
        public string SizeText { get; set; }

        public PartitionEntryInfo()
        {
            Name = ""; TypeName = ""; SubTypeName = ""; OffsetHex = ""; SizeText = "";
        }
    }

    public class UsbChipProbeResult
    {
        public bool Ok { get; set; }
        public string RawChipLine { get; set; }
        public string MatchedProfileId { get; set; }
        public string Features { get; set; }
        public string Mac { get; set; }
        public string FlashSize { get; set; }
        public string FullLog { get; set; }

        public UsbChipProbeResult()
        {
            RawChipLine = "-"; MatchedProfileId = ""; Features = "-"; Mac = "-"; FlashSize = "-"; FullLog = "";
        }
    }

    public static class UsbEspTools
    {
        public static string FindArduinoCli()
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] candidates = new string[]
            {
                Path.Combine(localApp, @"Programs\Arduino IDE\resources\app\lib\backend\resources\arduino-cli.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Arduino IDE\resources\app\lib\backend\resources\arduino-cli.exe")
            };
            foreach (string c in candidates)
            {
                if (File.Exists(c)) return c;
            }
            return "arduino-cli.exe";
        }

        public static string FindEsptool()
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string pkgTools = Path.Combine(localApp, @"Arduino15\packages\esp32\tools\esptool_py");
            if (Directory.Exists(pkgTools))
            {
                string[] exes = Directory.GetFiles(pkgTools, "esptool.exe", SearchOption.AllDirectories);
                if (exes.Length > 0)
                {
                    Array.Sort(exes);
                    return exes[exes.Length - 1];
                }
            }
            return "esptool.exe";
        }

        public static string FindBootApp0()
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string hwDir = Path.Combine(localApp, @"Arduino15\packages\esp32\hardware\esp32");
            if (Directory.Exists(hwDir))
            {
                string[] bins = Directory.GetFiles(hwDir, "boot_app0.bin", SearchOption.AllDirectories);
                if (bins.Length > 0)
                {
                    Array.Sort(bins);
                    return bins[bins.Length - 1];
                }
            }
            return "";
        }

        public static int RunExternalProcess(string exe, string args, string workDir, Action<string> onLine)
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = string.IsNullOrEmpty(workDir) ? Environment.CurrentDirectory : workDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using (var proc = new Process { StartInfo = psi })
            {
                proc.OutputDataReceived += delegate(object s, DataReceivedEventArgs e) {
                    if (e.Data != null && onLine != null) onLine(e.Data);
                };
                proc.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e) {
                    if (e.Data != null && onLine != null) onLine(e.Data);
                };
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();
                return proc.ExitCode;
            }
        }

        // ESPConnect-inspired live USB Chip & Flash Inspector
        public static UsbChipProbeResult ProbeChipViaUsb(string esptoolPath, string comPort, Action<string> onLine)
        {
            var res = new UsbChipProbeResult();
            var sb = new StringBuilder();
            string args = String.Format("--port {0} --before default-reset --after hard-reset flash-id", comPort);
            int code = RunExternalProcess(esptoolPath, args, null, delegate(string line) {
                sb.AppendLine(line);
                if (onLine != null) onLine(line);

                if (line.IndexOf("Chip is ", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    int idx = line.IndexOf("Chip is ", StringComparison.OrdinalIgnoreCase);
                    res.RawChipLine = line.Substring(idx + 8).Trim();
                }
                else if (line.IndexOf("Features:", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    int idx = line.IndexOf("Features:", StringComparison.OrdinalIgnoreCase);
                    res.Features = line.Substring(idx + 9).Trim();
                }
                else if (line.IndexOf("MAC:", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    int idx = line.IndexOf("MAC:", StringComparison.OrdinalIgnoreCase);
                    res.Mac = line.Substring(idx + 4).Trim();
                }
                else if (line.IndexOf("Detected flash size:", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    int idx = line.IndexOf("Detected flash size:", StringComparison.OrdinalIgnoreCase);
                    res.FlashSize = line.Substring(idx + 20).Trim();
                }
            });

            res.FullLog = sb.ToString();
            res.Ok = (code == 0 && res.RawChipLine != "-");

            string upper = (res.RawChipLine + " " + res.Features).ToUpperInvariant();
            if (upper.Contains("ESP32-S3"))
                res.MatchedProfileId = upper.Contains("OCTAL") ? "esp32s3_opi" : "esp32s3";
            else if (upper.Contains("ESP32-S2"))
                res.MatchedProfileId = "esp32s2";
            else if (upper.Contains("ESP32-C5"))
                res.MatchedProfileId = "esp32c5";
            else if (upper.Contains("ESP32-C6"))
                res.MatchedProfileId = "esp32c6";
            else if (upper.Contains("ESP32-C3") || upper.Contains("ESP8685"))
                res.MatchedProfileId = "esp32c3";
            else if (upper.Contains("ESP32-C2") || upper.Contains("ESP8684"))
                res.MatchedProfileId = "esp32c2";
            else if (upper.Contains("ESP32-H2"))
                res.MatchedProfileId = "esp32h2";
            else if (upper.Contains("ESP32-P4"))
                res.MatchedProfileId = "esp32p4";
            else if (upper.Contains("ESP32"))
                res.MatchedProfileId = upper.Contains("PSRAM") ? "esp32wrover" : "esp32";

            return res;
        }

        // Native binary ESP32 Partition Table parser (0xAA 0x50 magic, 32-byte records)
        public static List<PartitionEntryInfo> ParsePartitionBytes(byte[] raw)
        {
            var list = new List<PartitionEntryInfo>();
            if (raw == null || raw.Length < 32) return list;

            for (int off = 0; off + 32 <= raw.Length; off += 32)
            {
                byte m0 = raw[off];
                byte m1 = raw[off + 1];
                if (m0 == 0xEB && m1 == 0xEB) break; // MD5 footer entry -> end of table
                if (m0 != 0xAA || m1 != 0x50) break;

                byte pType = raw[off + 2];
                byte subType = raw[off + 3];
                uint partOffset = BitConverter.ToUInt32(raw, off + 4);
                uint partSize = BitConverter.ToUInt32(raw, off + 8);

                int nameLen = 0;
                while (nameLen < 16 && raw[off + 12 + nameLen] != 0) nameLen++;
                string name = Encoding.ASCII.GetString(raw, off + 12, nameLen);

                string tName = (pType == 0x00) ? "app" : (pType == 0x01 ? "data" : String.Format("0x{0:X2}", pType));
                string stName = String.Format("0x{0:X2}", subType);
                if (pType == 0x00)
                {
                    if (subType == 0x00) stName = "factory";
                    else if (subType >= 0x10 && subType <= 0x1F) stName = "ota_" + (subType - 0x10);
                    else if (subType == 0x20) stName = "test";
                }
                else if (pType == 0x01)
                {
                    if (subType == 0x00) stName = "otadata";
                    else if (subType == 0x01) stName = "phy";
                    else if (subType == 0x02) stName = "nvs";
                    else if (subType == 0x03) stName = "coredump";
                    else if (subType == 0x04) stName = "nvs_keys";
                    else if (subType == 0x81) stName = "fatfs";
                    else if (subType == 0x82) stName = "spiffs/littlefs";
                }

                string szText = (partSize >= 1048576U)
                    ? String.Format("{0:F2} MB ({1} KB)", partSize / 1048576.0, partSize / 1024U)
                    : String.Format("{0} KB", partSize / 1024U);

                list.Add(new PartitionEntryInfo
                {
                    Name = name,
                    TypeName = tName,
                    SubTypeName = stName,
                    OffsetHex = String.Format("0x{0:X6}", partOffset),
                    SizeText = szText
                });
            }
            return list;
        }

        public static List<PartitionEntryInfo> ReadPartitionTableViaUsb(string esptoolPath, string comPort, string tempDir, Action<string> onLine)
        {
            Directory.CreateDirectory(tempDir);
            string tmpBin = Path.Combine(tempDir, "usb_part_table_0x8000.bin");
            if (File.Exists(tmpBin)) File.Delete(tmpBin);

            string args = String.Format("--port {0} --baud 460800 read-flash 0x8000 0xC00 \"{1}\"", comPort, tmpBin);
            int code = RunExternalProcess(esptoolPath, args, null, onLine);
            if (code != 0 || !File.Exists(tmpBin)) return new List<PartitionEntryInfo>();
            return ParsePartitionBytes(File.ReadAllBytes(tmpBin));
        }

        public static string SendUsbXioRequest(string comPort, int baud, string method, string path, string body, out bool ok)
        {
            ok = false;
            try
            {
                using (var sp = new SerialPort(comPort, baud, Parity.None, 8, StopBits.One))
                {
                    sp.ReadTimeout = 2500;
                    sp.WriteTimeout = 2000;
                    sp.DtrEnable = false;
                    sp.RtsEnable = false;
                    sp.Open();
                    sp.DiscardInBuffer();

                    byte[] bodyBytes = string.IsNullOrEmpty(body) ? new byte[0] : Encoding.UTF8.GetBytes(body);
                    string reqId = "u" + (Environment.TickCount & 0xFFFF).ToString("x4");
                    string hdr = (bodyBytes.Length > 0)
                        ? String.Format("XIO REQ {0} {1} {2} {3}\n", reqId, method, path, bodyBytes.Length)
                        : String.Format("XIO REQ {0} {1} {2}\n", reqId, method, path);

                    byte[] hdrBytes = Encoding.ASCII.GetBytes(hdr);
                    sp.Write(hdrBytes, 0, hdrBytes.Length);
                    if (bodyBytes.Length > 0)
                    {
                        sp.Write(bodyBytes, 0, bodyBytes.Length);
                    }

                    DateTime deadline = DateTime.UtcNow.AddMilliseconds(3200);
                    string marker = "XIO RES " + reqId + " ";
                    var sb = new StringBuilder();
                    while (DateTime.UtcNow < deadline)
                    {
                        try
                        {
                            string line = sp.ReadLine();
                            if (line != null && line.Contains(marker))
                            {
                                string[] tok = line.Trim().Split(' ');
                                int len = 0;
                                if (tok.Length >= 5) int.TryParse(tok[4], out len);
                                if (len > 0 && len < 65536)
                                {
                                    char[] buf = new char[len];
                                    int read = 0;
                                    while (read < len && DateTime.UtcNow < deadline)
                                    {
                                        int r = sp.Read(buf, read, len - read);
                                        if (r > 0) read += r;
                                    }
                                    ok = true;
                                    return new string(buf, 0, read);
                                }
                                ok = true;
                                return line;
                            }
                            sb.AppendLine(line);
                        }
                        catch (TimeoutException) { break; }
                    }
                    return "Tidak ada balasan XIO RES dari perangkat. Pastikan firmware XIO sudah terpasang & port COM tidak dipakai aplikasi lain.";
                }
            }
            catch (Exception ex)
            {
                return "Kesalahan Serial USB: " + ex.Message;
            }
        }
    }
}
