using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Threading;
using XioPuzzleStudio;

internal static class BuilderTests
{
    private static int checks;
    private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly PropertyInfo KeyProperty = typeof(XioProjectFile).GetProperty("AiApiKey", PrivateInstance);

    private static void Check(bool valid, string label)
    {
        if (!valid) throw new InvalidOperationException(label);
        checks++;
        Console.WriteLine("PASS: " + label);
    }

    private static void Reject(Action action, string label)
    {
        bool rejected = false;
        try { action(); } catch (Exception) { rejected = true; }
        Check(rejected, label);
    }

    private static T Field<T>(MainWindow window, string name)
    {
        return (T)typeof(MainWindow).GetField(name, PrivateInstance).GetValue(window);
    }

    private static void Pump(MainWindow window)
    {
        window.Dispatcher.Invoke(new Action(delegate { }), DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
    }

    private static void TestConfiguration()
    {
        var project = new XioProjectFile();
        string key = "nvapi-" + new string('a', 32);
        KeyProperty.SetValue(project, key, null);
        Check(CodeSynthesizer.ValidateAiDefaults(project) == null, "NVIDIA key format");
        string header = CodeSynthesizer.GenerateAiDefaultsHeader(project, true);
        Check(header.Contains(key), "Explicit private header includes supplied key");
        Check(!CodeSynthesizer.GenerateAiDefaultsHeader(project, false).Contains(key), "Disabled AI excludes key");
        project.AiProvider = "openrouter";
        project.AiModel = "";
        string orKey = "sk-or-" + new string('b', 32);
        KeyProperty.SetValue(project, orKey, null);
        header = CodeSynthesizer.GenerateAiDefaultsHeader(project, true);
        Check(header.Contains(orKey) && header.Contains("openai/gpt-4o-mini"), "OpenRouter key and default model");
        KeyProperty.SetValue(project, "", null);
        header = CodeSynthesizer.GenerateAiDefaultsHeader(project, true);
        Check(Regex.Matches(header, "WEBUI_AI_DEFAULT(?:_OR)?_KEY\\s+\"\"").Count == 2, "Both empty key macros emitted");
        project.AiProvider = "nvidia";
        foreach (string bad in new[] { "short", orKey, key + " ", key + "\n", key + "\"", "Bearer " + key, "nvapi-" + new string('a', 510) })
        {
            KeyProperty.SetValue(project, bad, null);
            string error = CodeSynthesizer.ValidateAiDefaults(project);
            Check(error != null && !error.Contains(bad), "Invalid key rejected without disclosure");
        }
        KeyProperty.SetValue(project, key, null);
        foreach (string bad in new[] { new string('m', 97), "bad model", "bad\nmodel", "bad\"model" })
        {
            project.AiModel = bad;
            Check(CodeSynthesizer.ValidateAiDefaults(project) != null, "Invalid model rejected");
        }
        project.AiModel = "custom/text-model";
        project.AiProvider = "other";
        Check(CodeSynthesizer.ValidateAiDefaults(project) != null, "Unsupported provider rejected");
        project.AiProvider = "nvidia";
        var serializer = new JavaScriptSerializer();
        string json = serializer.Serialize(project);
        Check(!json.Contains(key) && !json.Contains("\"AiApiKey\":"), "Private profile does not serialize plaintext AI key");
        var restored = serializer.Deserialize<XioProjectFile>(json);
        Check((string)KeyProperty.GetValue(restored, null) == key, "DPAPI roundtrip on current Windows account");
        var injected = serializer.Deserialize<XioProjectFile>(serializer.Serialize(new Dictionary<string, object> { { "AiApiKey", key } }));
        Check(string.IsNullOrEmpty((string)KeyProperty.GetValue(injected, null)), "JSON cannot inject internal plaintext key");
        foreach (string bad in new[] { "not-base64!", Convert.ToBase64String(new byte[] { 1, 2, 3 }), new string('A', 4097) })
            Reject(delegate { project.ProtectedAiApiKey = bad; }, "Corrupt protected key rejected");
        var old = serializer.Deserialize<XioProjectFile>("{}");
        Check(old.AiProvider == "nvidia" && string.IsNullOrEmpty((string)KeyProperty.GetValue(old, null)), "Legacy profile receives keyless defaults");
        Check(typeof(CodeSynthesizer).GetMethod("GenerateMarauderProfileHeader") == null, "Private integrated firmware generator excluded");
        Check(typeof(CodeSynthesizer).GetMethod("RepackMarauderWebUiGz") == null, "Private firmware repacker excluded");
    }

    private static void TestPublicExport()
    {
        string sentinel = "private-value-" + new string('s', 32);
        var project = new XioProjectFile {
            ProjectName = sentinel, ApSsid = sentinel, ApPass = sentinel, StaSsid = sentinel, StaPass = sentinel,
            MqttHost = sentinel, MqttTopic = sentinel, WebhookUrl = sentinel, WireGuardEndpoint = sentinel,
            GeofenceLat = 12.345, GeofenceLon = 67.89, AiModel = "custom/text-model"
        };
        KeyProperty.SetValue(project, "nvapi-" + new string('k', 32), null);
        project.FeatureStates["ai_cloud"] = true;
        project.FeatureStates[sentinel] = true;
        project.RemovedDeckKeys.Add(sentinel);
        project.CompanionStates[sentinel] = true;
        project.RemovedCompanionKeys.Add(sentinel);
        project.Slots.Add(new PinSlotConfig { SlotIndex = 0, Gpio = 2, Role = 1, Param1 = 1, Label = sentinel });
        project.Rules.Add(new AutomationRuleConfig { Index = 0, Enabled = true, Name = sentinel, Source = 1, Threshold = 30, TargetSlot = 0 });
        project.CustomUserModules.Add(new PuzzleFeatureItem { Key = "custom", IsCustomUserModule = true, CustomGlobalCode = sentinel });
        project.CustomCompanions.Add(new CompanionPieceItem { Key = "custom", IsCustom = true, CustomServiceYaml = sentinel });
        string json = StudioCatalog.GeneratePublicProjectJson(project);
        Check(!json.Contains(sentinel), "Public export strips every synthetic secret and free-text field");
        foreach (string field in new[] { "AiApiKey", "ProtectedAiApiKey", "ApSsid", "ApPass", "StaSsid", "StaPass", "MqttHost", "WebhookUrl", "GeofenceLat", "CustomUserModules", "CustomCompanions" })
            Check(!json.Contains("\"" + field + "\""), "Public schema excludes " + field);
        var shared = new JavaScriptSerializer().Deserialize<XioProjectFile>(json);
        Check(shared.Slots.Count == 1 && shared.Slots[0].Gpio == 2 && shared.Slots[0].Label == "", "Public slot numeric configuration imports");
        Check(shared.Rules.Count == 1 && shared.Rules[0].Threshold == 30 && shared.Rules[0].Name == "", "Public rule numeric configuration imports");
        Check(shared.FeatureStates["ai_cloud"], "Public export retains known feature state");
        Check(project.ApPass == sentinel && project.Slots[0].Label == sentinel && project.CustomUserModules.Count == 1, "Public export does not mutate private project");
        Check(json == StudioCatalog.GeneratePublicProjectJson(project), "Public JSON is repeatable for unchanged input");
        project.SelectedChipId = "unknown";
        Reject(delegate { StudioCatalog.GeneratePublicProjectJson(project); }, "Unknown public chip rejected");
        project.SelectedChipId = "esp32";
        project.AiModel = "bad model";
        Reject(delegate { StudioCatalog.GeneratePublicProjectJson(project); }, "Invalid public AI model rejected");
        var chips = StudioCatalog.CreateAllChips();
        var features = StudioCatalog.CreateDefaultFeatures();
        var meta = new XioProjectFile();
        string first = CodeSynthesizer.GenerateUniversalProfileHeader(chips[0], features, meta.Slots, meta.Rules, meta);
        Check(first == CodeSynthesizer.GenerateUniversalProfileHeader(chips[0], features, meta.Slots, meta.Rules, meta), "Universal header has no clock-dependent output");
        var wrover = chips.Find(delegate(EspChipProfile chip) { return chip.Id == "esp32wrover"; });
        Check(StudioCatalog.ValidateSlot(wrover, new PinSlotConfig { Gpio = 16, Role = 1 }, null) != null, "Existing PSRAM pin guard retained");
    }

    private static void TestUi()
    {
        var app = new Application();
        var window = new MainWindow();
        try
        {
            window.Show();
            window.SelectPageForTest(4);
            Pump(window);
            Check(window.IsVisible, "Actual WPF window rendered");
            var password = Field<PasswordBox>(window, "_pwdAiApiKey");
            var provider = Field<ComboBox>(window, "_cmbAiProvider");
            var model = Field<TextBox>(window, "_txtAiModel");
            var engine = Field<ComboBox>(window, "_cmbEngine");
            var enable = Field<Button>(window, "_btnAiIntegrated");
            Check(!Field<Button>(window, "_btnBuildFirmware").IsEnabled && !Field<Button>(window, "_btnBuildAndFlash").IsEnabled, "Missing backend disables build and flash");
            Check(Field<ComboBox>(window, "_cmbComPort").Items.Count == 0, "Startup does not enumerate serial ports");
            Check(!password.IsEnabled && password.Password.Length == 0, "Startup has no inherited AI key");
            enable.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(password.IsEnabled && engine.SelectedIndex == 1, "AI configuration mode enables input");
            Check(!Field<Button>(window, "_btnBuildFirmware").IsEnabled, "AI mode never enables firmware compilation");
            provider.SelectedIndex = 1;
            Check(model.Text == "openai/gpt-4o-mini", "Provider changes default model");
            model.Text = "custom/text-model";
            provider.SelectedIndex = 0;
            Check(model.Text == "custom/text-model", "Custom model preserved");
            string key = "nvapi-" + new string('a', 32);
            password.Password = key;
            Check(UIElementAutomationPeer.CreatePeerForElement(password).IsPassword(), "AI key is a protected password control");
            Check(!Field<TextBlock>(window, "_lblAiDefaultsState").Text.Contains(key), "AI status never displays key");
            Check(Field<PasswordBox>(window, "_txtApPass") != null && Field<PasswordBox>(window, "_txtStaPass") != null, "Network password controls are masked");
            var snapshot = (XioProjectFile)typeof(MainWindow).GetMethod("GatherProjectSnapshot", PrivateInstance).Invoke(window, null);
            Check((string)KeyProperty.GetValue(snapshot, null) == key, "Masked input reaches private snapshot");
            Check(!StudioCatalog.GeneratePublicProjectJson(snapshot).Contains(key), "UI snapshot exports without AI secret");
            foreach (UIElement item in ((Grid)password.Parent).Children)
            {
                Button clear = item as Button;
                if (clear != null) clear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            Check(password.Password.Length == 0, "Clear key action works");
            bool publicButtonFound = false;
            foreach (UIElement item in ((StackPanel)Field<UIElement>(window, "_pageBuild")).Children)
            {
                Button button = item as Button;
                if (button != null && button.Name == "ExportPublicProfile") publicButtonFound = true;
            }
            Check(publicButtonFound, "Public export action is visible");
            for (int page = 0; page < 5; page++) { window.SelectPageForTest(page); Pump(window); }
            Check(Field<List<PinSlotConfig>>(window, "_slots").Count == 8 && Field<List<AutomationRuleConfig>>(window, "_rules").Count == 4, "Five pages preserve initial pin and rule counts");
            foreach (bool dark in new[] { true, false })
            {
                window.SetDarkMode(dark);
                Pump(window);
                Check(password.ActualWidth > 100 && password.ActualHeight >= 29, "AI form renders in theme");
            }
            window.Width = 820;
            window.Height = 520;
            Pump(window);
            Check(password.ActualWidth > 100, "Minimum window keeps usable form width");
            Check(Field<object>(window, "_liveSerial") == null, "Serial remains unopened");
            string root = Field<string>(window, "_studioRoot");
            Check(Directory.Exists(Path.Combine(root, "builder_app")) && !Directory.Exists(Path.Combine(root, "firmware")), "Root resolves within standalone edition");
        }
        finally { window.Close(); app.Shutdown(); }
    }

    [STAThread]
    private static int Main()
    {
        try
        {
            TestConfiguration();
            TestPublicExport();
            TestUi();
            Console.WriteLine("PASSED: " + checks + " checks; no API, firmware, or device operation.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAILED: " + ex.Message);
            return 1;
        }
    }
}
