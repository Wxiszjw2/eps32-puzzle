using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using Microsoft.Win32;

namespace XioPuzzleStudio
{
    public partial class MainWindow : Window
    {
        // Studio State
        private readonly string _studioRoot;
        private readonly List<EspChipProfile> _chips;
        private readonly List<PuzzleFeatureItem> _features;
        private readonly List<CompanionPieceItem> _companions;
        private readonly List<PinRoleOption> _roles;
        private readonly List<PinSlotConfig> _slots;
        private readonly List<AutomationRuleConfig> _rules;

        private EspChipProfile _activeChip;
        private bool _isDarkMode = false;
        private int _buildMode = 0; // 0 = Universal ESP, 1 = Konfigurasi AI
        private string _activeCategory = "Semua";
        private string _searchQuery = "";
        private int _selectedDiagramGpio = -1;
        private double _zoomScale = 1.0;
        private int _activePageIdx = 0;

        // 4-Way Pan State
        private bool _isPanning = false;
        private Point _panStartMouse;
        private double _panStartH = 0;
        private double _panStartV = 0;

        // Main Layout Controls
        private ScrollViewer _mainScroll;
        private Border _workspaceScaleHost;
        private Grid _pageContainer;
        private UIElement _pageCatalog;
        private UIElement _pagePins;
        private UIElement _pageRules;
        private UIElement _pageCompanions;
        private UIElement _pageBuild;
        private readonly List<Button> _segButtons = new List<Button>();

        // Toolbar & Sidebar Controls
        private Button _btnThemeToggle;
        private TextBlock _lblZoom;
        private ComboBox _cmbChip;
        private ComboBox _cmbEngine;
        private TextBlock _valArch, _valGpio, _valRadio, _valBus;
        private ProgressBar _barFlash, _barRam;
        private TextBlock _lblFlashPct, _lblRamPct;
        private Border _pinStatusBadge;
        private TextBlock _lblConflictStatus;

        // Page 1: Dynamic Pin Studio Controls (0..32 User-Resizable Slots!)
        private Canvas _chipDiagramCanvas;
        private ScrollViewer _chipDiagramScroll;
        private TextBlock _lblSelectedPinHint;
        private TextBlock _lblPinMatrixTitle;
        private StackPanel _slotRowsPanel;
        private readonly List<ComboBox> _slotGpioCombos = new List<ComboBox>();
        private readonly List<ComboBox> _slotRoleCombos = new List<ComboBox>();
        private readonly List<TextBox> _slotP1Boxes = new List<TextBox>();
        private readonly List<TextBox> _slotP2Boxes = new List<TextBox>();
        private readonly List<TextBox> _slotLabelBoxes = new List<TextBox>();
        private bool _suppressSlotEvents = false;

        // Page 2: Dynamic Rules & Cloud Controls (0..24 User-Resizable Rules!)
        private TextBlock _lblRuleSectionTitle;
        private StackPanel _ruleRowsPanel;
        private readonly List<CheckBox> _ruleEnBoxes = new List<CheckBox>();
        private readonly List<TextBox> _ruleNameBoxes = new List<TextBox>();
        private readonly List<ComboBox> _ruleSrcCombos = new List<ComboBox>();
        private readonly List<ComboBox> _ruleOpCombos = new List<ComboBox>();
        private readonly List<TextBox> _ruleThBoxes = new List<TextBox>();
        private readonly List<ComboBox> _ruleTgtCombos = new List<ComboBox>();
        private readonly List<TextBox> _ruleValBoxes = new List<TextBox>();
        private bool _suppressRuleEvents = false;
        private TextBox _txtApSsid, _txtStaSsid;
        private PasswordBox _txtApPass, _txtStaPass;
        private TextBox _txtMqttHost, _txtMqttPort, _txtMqttTopic, _txtWebhook, _txtWgEndpoint;
        private TextBox _txtGeoLat, _txtGeoLon, _txtGeoRad;

        // Page 4: Build, ESPConnect USB & Console Controls
        private TextBox _txtCliPath, _txtEsptoolPath, _txtOutBinPath;
        private ComboBox _cmbAiProvider;
        private TextBox _txtAiModel;
        private PasswordBox _pwdAiApiKey;
        private StackPanel _aiDefaultsInputs;
        private TextBlock _lblAiDefaultsState;
        private Button _btnAiIntegrated;
        private Button _btnBuildFirmware, _btnBuildAndFlash;
        private TextBlock _lblBackendState;
        private ComboBox _cmbComPort, _cmbBaud;
        private TextBlock _lblProbeChip, _lblProbeFeat, _lblProbeMac, _lblProbeFlash;
        private StackPanel _partTableRows;
        private TextBox _txtConsoleLog;
        private TextBox _txtSerialInput;
        private Button _btnSerialMon;
        private SerialPort _liveSerial;
        private Thread _serialReaderThread;
        private volatile bool _serialRunning = false;
        private volatile bool _busyTask = false;

        private static readonly string[] RuleSourceLabels = new string[] {
            "Nonaktif",
            "Suhu Udara (°C)",
            "Kelembaban (%)",
            "Cahaya (Lux)",
            "Jarak Ultrasonik (cm)",
            "Berat HX711 (g)",
            "Tegangan DC (V)",
            "Derajat pH Air",
            "TDS Air (ppm)",
            "ADC Slot #1 (mV)",
            "Kadar CO2 (ppm)",
            "Jarak Geofence (m)",
            "Tombol 1x Klik",
            "Tombol 2x Klik",
            "Jarak Laser ToF (mm)",
            "Oksigen Air DO (mg/L)",
            "Kebisingan Audio (dB)",
            "Trafik Wi-Fi (PPS)",
            "Sinyal LoRa (RSSI dBm)"
        };

        private static Brush BrushHex(string hex)
        {
            var b = (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
            b.Freeze();
            return b;
        }

        private Brush ResBrush(string key)
        {
            object v = TryFindResource(key);
            return (v as Brush) ?? Brushes.Gray;
        }

        private Style ResStyle(string key)
        {
            return TryFindResource(key) as Style;
        }

        public MainWindow()
        {
            _studioRoot = ResolveStudioRoot();
            _chips = StudioCatalog.CreateAllChips();
            _features = StudioCatalog.CreateDefaultFeatures();
            _companions = StudioCatalog.CreateDefaultCompanions();
            _roles = StudioCatalog.GetAllRoles();

            // Start clean with 8 dynamic pin slots & 4 dynamic rules (user can freely +Tambah or ×Hapus!)
            _slots = new List<PinSlotConfig>();
            for (int i = 0; i < 8; i++) _slots.Add(new PinSlotConfig { SlotIndex = i });

            _rules = new List<AutomationRuleConfig>();
            for (int i = 0; i < 4; i++) _rules.Add(new AutomationRuleConfig { Index = i, Name = "Aturan " + (i + 1) });

            _activeChip = _chips[0];

            Title = "XIO Studio — Builder + AI Configuration";
            Rect wa = SystemParameters.WorkArea;
            Width = Math.Min(1360.0, Math.Max(940.0, wa.Width - 32.0));
            Height = Math.Min(860.0, Math.Max(580.0, wa.Height - 26.0));
            MaxWidth = wa.Width + 12.0;
            MaxHeight = wa.Height + 12.0;
            MinWidth = 820;
            MinHeight = 520;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FontFamily = new FontFamily("Segoe UI");
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;

            WindowStyle = WindowStyle.None;
            var chrome = new WindowChrome
            {
                CaptionHeight = 0,
                CornerRadius = new CornerRadius(12),
                GlassFrameThickness = new Thickness(1),
                ResizeBorderThickness = new Thickness(6)
            };
            WindowChrome.SetWindowChrome(this, chrome);

            ApplyTheme(_isDarkMode);
            BuildAppleRootLayout();
            RebuildDynamicSlotRows();
            RebuildDynamicRuleRows();
            SelectPage(0);
            RefreshAllChipDependentUi();
            RebuildCompanionCards();
            Closed += delegate { StopLiveSerial(); };
        }

        public void SetDarkMode(bool dark)
        {
            _isDarkMode = dark;
            ApplyTheme(_isDarkMode);
            if (_btnThemeToggle != null)
                _btnThemeToggle.Content = _isDarkMode ? "Mode Terang" : "Mode Gelap";
            UpdateSegmentVisuals();
            RebuildCatalogCards();
            RebuildCompanionCards();
            DrawInteractiveChipDiagram();
        }

        public void SelectPageForTest(int idx)
        {
            SelectPage(idx);
        }

        private void ApplyTheme(bool dark)
        {
            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(AppleTheme.BuildThemeDictionary(dark));
            SetResourceReference(BackgroundProperty, "ApWinBg");
            SetResourceReference(ForegroundProperty, "ApTextPri");
        }

        private string ResolveStudioRoot()
        {
            string baseDir = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            var directory = new DirectoryInfo(baseDir);
            while (directory != null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "builder_app"))) return directory.FullName;
                directory = directory.Parent;
            }
            return baseDir;
        }

        private void BuildAppleRootLayout()
        {
            var outerBorder = new Border { BorderThickness = new Thickness(1) };
            outerBorder.SetResourceReference(Border.BorderBrushProperty, "ApHairline");
            outerBorder.SetResourceReference(Border.BackgroundProperty, "ApWinBg");

            var dock = new DockPanel { LastChildFill = true };

            var topToolbar = BuildMacUnifiedToolbar();
            DockPanel.SetDock(topToolbar, Dock.Top);
            dock.Children.Add(topToolbar);

            var bottomBar = BuildMacBottomStatusBar();
            DockPanel.SetDock(bottomBar, Dock.Bottom);
            dock.Children.Add(bottomBar);

            var sidebar = BuildMacSidebar();
            DockPanel.SetDock(sidebar, Dock.Left);
            dock.Children.Add(sidebar);

            _pageContainer = new Grid
            {
                Width = 910,
                MinWidth = 885,
                Margin = new Thickness(20, 16, 20, 22)
            };

            _pageCatalog    = BuildPageCatalog();
            _pagePins       = BuildPagePins();
            _pageRules      = BuildPageRules();
            _pageCompanions = BuildPageCompanions();
            _pageBuild      = BuildPageBuild();

            _pageContainer.Children.Add(_pageCatalog);
            _pageContainer.Children.Add(_pagePins);
            _pageContainer.Children.Add(_pageRules);
            _pageContainer.Children.Add(_pageCompanions);
            _pageContainer.Children.Add(_pageBuild);

            _workspaceScaleHost = new Border
            {
                Child = _pageContainer,
                Background = Brushes.Transparent
            };

            _mainScroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                CanContentScroll = false,
                PanningMode = PanningMode.Both,
                Content = _workspaceScaleHost
            };
            _mainScroll.SetResourceReference(ScrollViewer.BackgroundProperty, "ApWinBg");
            _mainScroll.SizeChanged += delegate { UpdateWorkspaceResponsiveWidth(); };

            AttachFourWayScrollInteractions(_mainScroll);
            dock.Children.Add(_mainScroll);

            outerBorder.Child = dock;
            Content = outerBorder;
        }

        // ====================================================================
        // UNIFIED APPLE macOS TOP TOOLBAR (5-TAB SEGMENTED CONTROL)
        // ====================================================================
        private UIElement BuildMacUnifiedToolbar()
        {
            var bar = new Border
            {
                Height = 52,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(16, 0, 16, 0)
            };
            bar.SetResourceReference(Border.BackgroundProperty, "ApChromeBg");
            bar.SetResourceReference(Border.BorderBrushProperty, "ApHairline");

            bar.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.OriginalSource == bar || e.OriginalSource is Grid || e.OriginalSource is TextBlock)
                {
                    if (e.ClickCount == 2)
                        WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
                    else
                        DragMove();
                }
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(202) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var leftStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            leftStack.Children.Add(MakeTrafficDot("#FF5F57", "Tutup Aplikasi", delegate { Close(); }));
            leftStack.Children.Add(MakeTrafficDot("#FEBC2E", "Kecilkan Jendela", delegate { WindowState = WindowState.Minimized; }));
            leftStack.Children.Add(MakeTrafficDot("#28C840", "Perbesar / Pulihkan", delegate {
                WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
            }));

            var titleStack = new StackPanel { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            var tMain = new TextBlock { Text = "XIO Studio", FontSize = 14, FontWeight = FontWeights.Bold };
            tMain.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
            var tSub = new TextBlock { Text = "Universal Puzzle Freedom", FontSize = 10.5 };
            tSub.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            titleStack.Children.Add(tMain);
            titleStack.Children.Add(tSub);
            leftStack.Children.Add(titleStack);
            Grid.SetColumn(leftStack, 0);
            grid.Children.Add(leftStack);

            // Centered 5-Tab Apple Segmented Control
            var segOuter = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                BorderThickness = new Thickness(1)
            };
            segOuter.SetResourceReference(Border.BackgroundProperty, "ApSegTrack");
            segOuter.SetResourceReference(Border.BorderBrushProperty, "ApHairline");

            var segRow = new StackPanel { Orientation = Orientation.Horizontal };
            string[] tabs = new string[] { "Susun Puzzle", "Studio Pin", "Otomasi Lokal", "VPS & PC", "Ekspor & AI" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int idx = i;
                var b = new Button
                {
                    Content = tabs[i],
                    MinWidth = 98,
                    Padding = new Thickness(11, 5, 11, 5)
                };
                b.SetResourceReference(FrameworkElement.StyleProperty, "ApSegmentButton");
                b.Click += delegate { SelectPage(idx); };
                _segButtons.Add(b);
                segRow.Children.Add(b);
            }
            segOuter.Child = segRow;
            Grid.SetColumn(segOuter, 1);
            grid.Children.Add(segOuter);

            // Right Toolbar Controls
            var rightStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            _btnThemeToggle = MakeStyledButton(_isDarkMode ? "Mode Terang" : "Mode Gelap", "ApButton", new Thickness(11, 5, 11, 5));
            _btnThemeToggle.MinWidth = 96;
            _btnThemeToggle.Margin = new Thickness(0, 0, 7, 0);
            _btnThemeToggle.Click += delegate { SetDarkMode(!_isDarkMode); };
            rightStack.Children.Add(_btnThemeToggle);

            var zOut = MakeStyledButton("−", "ApButton", new Thickness(8, 4, 8, 4));
            zOut.Click += delegate { SetZoom(_zoomScale - 0.10); };
            _lblZoom = new TextBlock { Text = "100%", Width = 38, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontSize = 11.5, FontWeight = FontWeights.SemiBold };
            _lblZoom.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            var zIn = MakeStyledButton("+", "ApButton", new Thickness(8, 4, 8, 4));
            zIn.Margin = new Thickness(0, 0, 7, 0);
            zIn.Click += delegate { SetZoom(_zoomScale + 0.10); };
            rightStack.Children.Add(zOut);
            rightStack.Children.Add(_lblZoom);
            rightStack.Children.Add(zIn);

            var btnOpen = MakeStyledButton("Buka", "ApButton", new Thickness(10, 5, 10, 5));
            btnOpen.Margin = new Thickness(0, 0, 6, 0);
            btnOpen.Click += delegate { LoadProjectDialog(); };
            rightStack.Children.Add(btnOpen);

            var btnSave = MakeStyledButton("Simpan", "ApPrimaryButton", new Thickness(11, 5, 11, 5));
            btnSave.Click += delegate { SaveProjectDialog(); };
            rightStack.Children.Add(btnSave);

            Grid.SetColumn(rightStack, 2);
            grid.Children.Add(rightStack);

            bar.Child = grid;
            return bar;
        }

        private UIElement MakeTrafficDot(string colorHex, string tip, Action onClick)
        {
            var dot = new Border
            {
                Width = 12,
                Height = 12,
                CornerRadius = new CornerRadius(6),
                Background = BrushHex(colorHex),
                BorderBrush = BrushHex("#22000000"),
                BorderThickness = new Thickness(0.6),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                ToolTip = tip
            };
            dot.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                onClick();
            };
            return dot;
        }

        // ====================================================================
        // BOTTOM STATUS & 4-WAY SCROLL HELPER BAR
        // ====================================================================
        private UIElement BuildMacBottomStatusBar()
        {
            var b = new Border
            {
                Height = 32,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(16, 0, 16, 0)
            };
            b.SetResourceReference(Border.BackgroundProperty, "ApChromeBg");
            b.SetResourceReference(Border.BorderBrushProperty, "ApHairline");

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var leftRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _pinStatusBadge = new Border { Width = 8, Height = 8, CornerRadius = new CornerRadius(4), Background = BrushHex("#34C759"), Margin = new Thickness(0, 0, 7, 0), VerticalAlignment = VerticalAlignment.Center };
            leftRow.Children.Add(_pinStatusBadge);
            _lblConflictStatus = new TextBlock { Text = "PinManager Silikon: Siap • 0 konflik pin", FontSize = 11.5, VerticalAlignment = VerticalAlignment.Center };
            _lblConflictStatus.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            leftRow.Children.Add(_lblConflictStatus);
            Grid.SetColumn(leftRow, 0);
            grid.Children.Add(leftRow);

            var rightRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var hint = new TextBlock {
                Text = "Scroll 4-Arah: Roda = Atas/Bawah • Shift+Roda = Kiri/Kanan • Klik-Kanan/Tengah Geser = Pan Bebas",
                FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0)
            };
            hint.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            rightRow.Children.Add(hint);

            rightRow.Children.Add(MakeMiniNavBtn("‹", "Geser Kiri", delegate { _mainScroll.ScrollToHorizontalOffset(_mainScroll.HorizontalOffset - 180); }));
            rightRow.Children.Add(MakeMiniNavBtn("⌃", "Geser Atas", delegate { _mainScroll.ScrollToVerticalOffset(_mainScroll.VerticalOffset - 180); }));
            rightRow.Children.Add(MakeMiniNavBtn("⌄", "Geser Bawah", delegate { _mainScroll.ScrollToVerticalOffset(_mainScroll.VerticalOffset + 180); }));
            rightRow.Children.Add(MakeMiniNavBtn("›", "Geser Kanan", delegate { _mainScroll.ScrollToHorizontalOffset(_mainScroll.HorizontalOffset + 180); }));

            Grid.SetColumn(rightRow, 1);
            grid.Children.Add(rightRow);

            b.Child = grid;
            return b;
        }

        private Button MakeMiniNavBtn(string sym, string tip, Action act)
        {
            var b = MakeStyledButton(sym, "ApButton", new Thickness(7, 1, 7, 1));
            b.FontSize = 11;
            b.Height = 22;
            b.Margin = new Thickness(2, 0, 0, 0);
            b.ToolTip = tip;
            b.Click += delegate { act(); };
            return b;
        }

        // ====================================================================
        // LEFT APPLE macOS VIBRANCY SIDEBAR
        // ====================================================================
        private UIElement BuildMacSidebar()
        {
            var sideBorder = new Border
            {
                Width = 280,
                BorderThickness = new Thickness(0, 0, 1, 0)
            };
            sideBorder.SetResourceReference(Border.BackgroundProperty, "ApSidebarBg");
            sideBorder.SetResourceReference(Border.BorderBrushProperty, "ApHairline");

            var sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var sp = new StackPanel { Margin = new Thickness(14, 14, 14, 20) };

            sp.Children.Add(MakeSectionCaption("TARGET PERANGKAT"));
            var devCard = MakeAppleCard(new Thickness(12), new Thickness(0, 0, 0, 14));
            var devSp = new StackPanel();

            devSp.Children.Add(MakeSmallLabel("Keluarga Silikon ESP (11 Varian)"));
            _cmbChip = MakeAppleCombo(double.NaN);
            foreach (EspChipProfile cp in _chips) _cmbChip.Items.Add(cp.Name);
            _cmbChip.SelectedIndex = 0;
            _cmbChip.Margin = new Thickness(0, 0, 0, 10);
            _cmbChip.SelectionChanged += delegate {
                if (_cmbChip.SelectedIndex >= 0 && _cmbChip.SelectedIndex < _chips.Count)
                {
                    _activeChip = _chips[_cmbChip.SelectedIndex];
                    if (_buildMode == 1 && !_activeChip.SupportsMarauderMode)
                    {
                        _buildMode = 0;
                        _cmbEngine.SelectedIndex = 0;
                    }
                    RefreshAllChipDependentUi();
                }
            };
            devSp.Children.Add(_cmbChip);

            devSp.Children.Add(MakeSmallLabel("Mesin Sintesis Firmware"));
            _cmbEngine = MakeAppleCombo(double.NaN);
            _cmbEngine.Items.Add("Universal ESP (Semua Chip)");
            _cmbEngine.Items.Add("Konfigurasi AI (ESP32)");
            _cmbEngine.SelectedIndex = 0;
            _cmbEngine.Margin = new Thickness(0, 0, 0, 10);
            _cmbEngine.SelectionChanged += delegate {
                if (_cmbEngine.SelectedIndex == 1 && !_activeChip.SupportsMarauderMode)
                {
                    MessageBox.Show("Mode Konfigurasi AI dikhususkan untuk ESP32 / WROVER.\nChip " + _activeChip.Name + " otomatis memakai Mesin Universal ESP.",
                        "Kompatibilitas Silikon", MessageBoxButton.OK, MessageBoxImage.Information);
                    _cmbEngine.SelectedIndex = 0;
                    return;
                }
                _buildMode = _cmbEngine.SelectedIndex;
                RefreshResourceGauges();
                RebuildCatalogCards();
            };
            devSp.Children.Add(_cmbEngine);

            var btnPdf = MakeStyledButton("Buka Datasheet PDF Resmi", "ApButton", new Thickness(10, 6, 10, 6));
            btnPdf.HorizontalAlignment = HorizontalAlignment.Stretch;
            btnPdf.Click += delegate { OpenActiveDatasheetPdf(); };
            devSp.Children.Add(btnPdf);

            devCard.Child = devSp;
            sp.Children.Add(devCard);

            sp.Children.Add(MakeSectionCaption("SPESIFIKASI SILIKON"));
            var specCard = MakeAppleCard(new Thickness(12, 8, 12, 8), new Thickness(0, 0, 0, 14));
            var specSp = new StackPanel();
            _valArch  = AddSidebarSpecRow(specSp, "Arsitektur", "-", true);
            _valGpio  = AddSidebarSpecRow(specSp, "Rentang Pin", "-", true);
            _valRadio = AddSidebarSpecRow(specSp, "Radio Aktif", "-", true);
            _valBus   = AddSidebarSpecRow(specSp, "Pin Default", "-", false);
            specCard.Child = specSp;
            sp.Children.Add(specCard);

            sp.Children.Add(MakeSectionCaption("ESTIMASI MEMORI"));
            var memCard = MakeAppleCard(new Thickness(12), new Thickness(0, 0, 0, 14));
            var memSp = new StackPanel();

            var flHdr = new DockPanel { Margin = new Thickness(0, 0, 0, 5) };
            _lblFlashPct = new TextBlock { FontSize = 11.5, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Right };
            _lblFlashPct.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            DockPanel.SetDock(_lblFlashPct, Dock.Right);
            flHdr.Children.Add(_lblFlashPct);
            var flTitle = new TextBlock { Text = "Memori Flash", FontSize = 12, FontWeight = FontWeights.Medium };
            flTitle.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
            flHdr.Children.Add(flTitle);
            memSp.Children.Add(flHdr);

            _barFlash = new ProgressBar { Style = ResStyle("ApProgressBar"), Minimum = 0, Maximum = 100, Margin = new Thickness(0, 0, 0, 12) };
            _barFlash.SetResourceReference(ProgressBar.ForegroundProperty, "ApBlue");
            memSp.Children.Add(_barFlash);

            var rmHdr = new DockPanel { Margin = new Thickness(0, 0, 0, 5) };
            _lblRamPct = new TextBlock { FontSize = 11.5, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Right };
            _lblRamPct.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            DockPanel.SetDock(_lblRamPct, Dock.Right);
            rmHdr.Children.Add(_lblRamPct);
            var rmTitle = new TextBlock { Text = "Memori SRAM", FontSize = 12, FontWeight = FontWeights.Medium };
            rmTitle.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
            rmHdr.Children.Add(rmTitle);
            memSp.Children.Add(rmHdr);

            _barRam = new ProgressBar { Style = ResStyle("ApProgressBar"), Minimum = 0, Maximum = 100 };
            _barRam.SetResourceReference(ProgressBar.ForegroundProperty, "ApGreen");
            memSp.Children.Add(_barRam);

            memCard.Child = memSp;
            sp.Children.Add(memCard);

            sp.Children.Add(MakeSectionCaption("PAKET PROFIL CEPAT"));
            var profGrid = new UniformGrid { Columns = 2 };
            AddQuickProfileBtn(profGrid, "Semua Fitur", "all", new Thickness(0, 0, 4, 6));
            AddQuickProfileBtn(profGrid, "Headless Murni", "headless", new Thickness(4, 0, 0, 6));
            AddQuickProfileBtn(profGrid, "Industri & ROS2", "industrial", new Thickness(0, 0, 4, 0));
            AddQuickProfileBtn(profGrid, "Spektrum & LoRa", "rf", new Thickness(4, 0, 0, 0));
            sp.Children.Add(profGrid);

            sv.Content = sp;
            sideBorder.Child = sv;
            return sideBorder;
        }

        private void AddQuickProfileBtn(Panel parent, string title, string preset, Thickness margin)
        {
            var b = MakeStyledButton(title, "ApButton", new Thickness(8, 7, 8, 7));
            b.Margin = margin;
            b.FontSize = 11.5;
            b.Click += delegate { ApplyQuickFeaturePreset(preset); };
            parent.Children.Add(b);
        }

        private TextBlock AddSidebarSpecRow(StackPanel parent, string label, string val, bool divider)
        {
            var g = new Grid { Margin = new Thickness(0, 5, 0, 5) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(82) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var k = new TextBlock { Text = label, FontSize = 11.5 };
            k.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            Grid.SetColumn(k, 0);
            g.Children.Add(k);

            var v = new TextBlock { Text = val, FontSize = 11.5, FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Right, TextWrapping = TextWrapping.Wrap };
            v.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
            Grid.SetColumn(v, 1);
            g.Children.Add(v);

            parent.Children.Add(g);
            if (divider) parent.Children.Add(MakeHairline());
            return v;
        }

        // ====================================================================
        // PAGE 1: STUDIO PIN MULTI-CHIP (DYNAMIC ADD / REMOVE SLOTS 0..32!)
        // ====================================================================
        private UIElement BuildPagePins()
        {
            var sp = new StackPanel();

            var h1 = new TextBlock { Text = "Studio Pin & Pengaman Silikon", FontSize = 22, FontWeight = FontWeights.Bold };
            h1.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
            var sub = new TextBlock {
                Text = "Kebebasan penuh: klik pin pada diagram silikon, gunakan template instan, atau tambah/kurangi baris slot pin sesuka hati (0 hingga 32 slot).",
                FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 14)
            };
            sub.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            sp.Children.Add(h1);
            sp.Children.Add(sub);

            // Symmetrical 5x2 Preset Bar
            sp.Children.Add(MakeSectionCaption("TEMPLATE RANGKAIAN CEPAT (OTOMATIS MENYESUAIKAN CHIP AKTIF)"));
            var presetCard = MakeAppleCard(new Thickness(12), new Thickness(0, 0, 0, 16));
            var pGrid = new UniformGrid { Columns = 5 };
            AddSymPresetBtn(pGrid, "I2C + OLED / ToF", "i2c_oled", new Thickness(0, 0, 5, 6));
            AddSymPresetBtn(pGrid, "Ultrasonik + Relay", "sr04_relay", new Thickness(3, 0, 3, 6));
            AddSymPresetBtn(pGrid, "Timbangan HX711", "hx711_btn", new Thickness(3, 0, 3, 6));
            AddSymPresetBtn(pGrid, "RS485 + CAN Bus", "modbus_can", new Thickness(3, 0, 3, 6));
            AddSymPresetBtn(pGrid, "nRF24 + MicroSD", "nrf_sd", new Thickness(5, 0, 0, 6));
            AddSymPresetBtn(pGrid, "Radio LoRa Sub-GHz", "lora_subghz", new Thickness(0, 0, 5, 0));
            AddSymPresetBtn(pGrid, "Mikrofon Audio I2S", "audio_i2s", new Thickness(3, 0, 3, 0));
            AddSymPresetBtn(pGrid, "Robotik Servo/Motor", "robotics", new Thickness(3, 0, 3, 0));
            AddSymPresetBtn(pGrid, "GPS NMEA + DO/pH", "gps_adc", new Thickness(3, 0, 3, 0));

            var btnResetPins = MakeStyledButton("Reset Slot", "ApButton", new Thickness(8, 7, 8, 7));
            btnResetPins.Margin = new Thickness(5, 0, 0, 0);
            btnResetPins.SetResourceReference(Button.ForegroundProperty, "ApRed");
            btnResetPins.Click += delegate { ClearAllPinSlots(); };
            pGrid.Children.Add(btnResetPins);

            presetCard.Child = pGrid;
            sp.Children.Add(presetCard);

            // Side-by-Side Split: Left (346px) = Silicon Diagram, Right (*) = Dynamic Slot Matrix
            var split = new Grid();
            split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(346) });
            split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var diagCard = MakeAppleCard(new Thickness(14), new Thickness(0));
            var diagSp = new StackPanel();
            var dTitle = new TextBlock { Text = "Diagram Modul Fisik", FontSize = 14, FontWeight = FontWeights.SemiBold };
            dTitle.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
            diagSp.Children.Add(dTitle);

            _lblSelectedPinHint = new TextBlock {
                Text = "Klik pin hijau/biru untuk menugaskan ke slot (otomatis menambah baris jika penuh).",
                FontSize = 11.5, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 10)
            };
            _lblSelectedPinHint.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            diagSp.Children.Add(_lblSelectedPinHint);

            var legWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
            legWrap.Children.Add(MakeLegendDot("Aman I/O", "#34C759"));
            legWrap.Children.Add(MakeLegendDot("ADC1", "#007AFF"));
            legWrap.Children.Add(MakeLegendDot("USB", "#AF52DE"));
            legWrap.Children.Add(MakeLegendDot("Strap/In", "#FF9500"));
            legWrap.Children.Add(MakeLegendDot("Terkunci", "#FF3B30"));
            diagSp.Children.Add(legWrap);

            var canvasFrame = new Border
            {
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(2)
            };
            canvasFrame.SetResourceReference(Border.BackgroundProperty, "ApCardAltBg");
            canvasFrame.SetResourceReference(Border.BorderBrushProperty, "ApHairline");

            _chipDiagramCanvas = new Canvas { Width = 338, Height = 530, Background = Brushes.Transparent };
            _chipDiagramScroll = new ScrollViewer
            {
                Height = 500,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = _chipDiagramCanvas
            };
            canvasFrame.Child = _chipDiagramScroll;
            diagSp.Children.Add(canvasFrame);
            diagCard.Child = diagSp;
            Grid.SetColumn(diagCard, 0);
            split.Children.Add(diagCard);

            // Right Card: Dynamic User-Resizable Pin Matrix
            var matCard = MakeAppleCard(new Thickness(16), new Thickness(0));
            var matSp = new StackPanel();

            var matHeader = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            matHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            matHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var mTitleSp = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            _lblPinMatrixTitle = new TextBlock { Text = "Matriks Slot Periferal & Bus (8 Baris)", FontSize = 14, FontWeight = FontWeights.SemiBold };
            _lblPinMatrixTitle.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
            var mSub = new TextBlock { Text = "Tambah hingga 32 slot atau hapus baris yang tidak diperlukan.", FontSize = 11.5, TextWrapping = TextWrapping.Wrap };
            mSub.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            mTitleSp.Children.Add(_lblPinMatrixTitle);
            mTitleSp.Children.Add(mSub);
            Grid.SetColumn(mTitleSp, 0);
            matHeader.Children.Add(mTitleSp);

            var matActs = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var btnAddSlot = MakeStyledButton("+ Tambah Slot", "ApButton", new Thickness(11, 6, 11, 6));
            btnAddSlot.Margin = new Thickness(0, 0, 6, 0);
            btnAddSlot.Click += delegate { AddNewPinSlotRow(); };
            matActs.Children.Add(btnAddSlot);

            var btnSyncNow = MakeStyledButton("Sinkron USB (<1d)", "ApPrimaryButton", new Thickness(12, 6, 12, 6));
            btnSyncNow.Click += delegate { TriggerLiveUsbSync(); };
            matActs.Children.Add(btnSyncNow);
            Grid.SetColumn(matActs, 1);
            matHeader.Children.Add(matActs);
            matSp.Children.Add(matHeader);

            matSp.Children.Add(MakeHairline());

            var tblContainer = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            var hdrRow = CreatePinMatrixGridRow();
            hdrRow.Margin = new Thickness(0, 0, 0, 6);
            AddMatrixHeader(hdrRow, 0, "#");
            AddMatrixHeader(hdrRow, 1, "GPIO Aman");
            AddMatrixHeader(hdrRow, 2, "Fungsi Periferal / Bus (41 Peran)");
            AddMatrixHeader(hdrRow, 3, "P1");
            AddMatrixHeader(hdrRow, 4, "P2");
            AddMatrixHeader(hdrRow, 5, "Label");
            AddMatrixHeader(hdrRow, 6, "");
            tblContainer.Children.Add(hdrRow);

            _slotRowsPanel = new StackPanel();
            tblContainer.Children.Add(_slotRowsPanel);

            var matScroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = tblContainer
            };
            matSp.Children.Add(matScroll);
            matCard.Child = matSp;
            Grid.SetColumn(matCard, 2);
            split.Children.Add(matCard);

            sp.Children.Add(split);
            return sp;
        }

        private Grid CreatePinMatrixGridRow()
        {
            var g = new Grid { MinWidth = 456 };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(98) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 148 });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(74) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
            return g;
        }

        private void RebuildDynamicSlotRows()
        {
            if (_slotRowsPanel == null) return;
            _suppressSlotEvents = true;
            _slotRowsPanel.Children.Clear();
            _slotGpioCombos.Clear();
            _slotRoleCombos.Clear();
            _slotP1Boxes.Clear();
            _slotP2Boxes.Clear();
            _slotLabelBoxes.Clear();

            if (_lblPinMatrixTitle != null)
                _lblPinMatrixTitle.Text = String.Format("Matriks Slot Periferal & Bus ({0} Baris)", _slots.Count);

            if (_slots.Count == 0)
            {
                var emptyTxt = new TextBlock {
                    Text = "0 slot aktif (Mode Tanpa Pin Eksternal). Klik '+ Tambah Slot' atau klik pin pada diagram silikon untuk menambahkan.",
                    FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 12)
                };
                emptyTxt.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
                _slotRowsPanel.Children.Add(emptyTxt);
                _suppressSlotEvents = false;
                return;
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                int slotIdx = i;
                _slots[i].SlotIndex = i;
                PinSlotConfig s = _slots[i];

                var r = CreatePinMatrixGridRow();
                r.Margin = new Thickness(0, 0, 0, 6);

                var num = new TextBlock { Text = (i + 1).ToString("D2"), FontSize = 11.5, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
                num.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
                Grid.SetColumn(num, 0);
                r.Children.Add(num);

                var cGpio = MakeAppleCombo(double.NaN);
                cGpio.Margin = new Thickness(0, 0, 5, 0);
                PopulateGpioComboForChip(cGpio, s.Gpio);
                Grid.SetColumn(cGpio, 1);
                r.Children.Add(cGpio);
                _slotGpioCombos.Add(cGpio);

                var cRole = MakeAppleCombo(double.NaN);
                int rSel = 0;
                for (int ri = 0; ri < _roles.Count; ri++)
                {
                    cRole.Items.Add(_roles[ri]);
                    if (_roles[ri].Code == s.Role) rSel = ri;
                }
                cRole.SelectedIndex = rSel;
                cRole.Margin = new Thickness(0, 0, 5, 0);
                Grid.SetColumn(cRole, 2);
                r.Children.Add(cRole);
                _slotRoleCombos.Add(cRole);

                var tP1 = MakeAppleTextBox(s.Param1.ToString(), double.NaN);
                tP1.Margin = new Thickness(0, 0, 5, 0);
                Grid.SetColumn(tP1, 3);
                r.Children.Add(tP1);
                _slotP1Boxes.Add(tP1);

                var tP2 = MakeAppleTextBox(s.Param2.ToString(), double.NaN);
                tP2.Margin = new Thickness(0, 0, 5, 0);
                Grid.SetColumn(tP2, 4);
                r.Children.Add(tP2);
                _slotP2Boxes.Add(tP2);

                var tLbl = MakeAppleTextBox(s.Label ?? "", double.NaN);
                tLbl.Margin = new Thickness(0, 0, 5, 0);
                Grid.SetColumn(tLbl, 5);
                r.Children.Add(tLbl);
                _slotLabelBoxes.Add(tLbl);

                var btnDel = MakeStyledButton("×", "ApButton", new Thickness(0));
                btnDel.Width = 24;
                btnDel.Height = 26;
                btnDel.ToolTip = "Kurangi / Hapus Slot #" + (i + 1);
                btnDel.SetResourceReference(Button.ForegroundProperty, "ApRed");
                btnDel.Click += delegate { RemovePinSlotRow(slotIdx); };
                Grid.SetColumn(btnDel, 6);
                r.Children.Add(btnDel);

                cGpio.SelectionChanged += delegate { OnSlotUiChanged(slotIdx); };
                cRole.SelectionChanged += delegate { OnSlotUiChanged(slotIdx); };
                tP1.TextChanged += delegate { OnSlotUiChanged(slotIdx); };
                tP2.TextChanged += delegate { OnSlotUiChanged(slotIdx); };
                tLbl.TextChanged += delegate { OnSlotUiChanged(slotIdx); };

                _slotRowsPanel.Children.Add(r);
            }
            _suppressSlotEvents = false;
        }

        private void AddNewPinSlotRow()
        {
            if (_slots.Count >= 32)
            {
                MessageBox.Show("Maksimal 32 slot pin dinamis per chip.", "Batas Slot", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            _slots.Add(new PinSlotConfig { SlotIndex = _slots.Count });
            RebuildDynamicSlotRows();
            RebuildDynamicRuleRows();
            ValidateAndRenderDiagram();
        }

        private void RemovePinSlotRow(int idx)
        {
            if (idx < 0 || idx >= _slots.Count) return;
            _slots.RemoveAt(idx);
            RebuildDynamicSlotRows();
            RebuildDynamicRuleRows();
            ValidateAndRenderDiagram();
        }

        private void AddSymPresetBtn(Panel parent, string label, string key, Thickness margin)
        {
            var b = MakeStyledButton(label, "ApButton", new Thickness(8, 7, 8, 7));
            b.Margin = margin;
            b.FontSize = 11.5;
            b.Click += delegate { ApplyHardwareWiringPreset(key); };
            parent.Children.Add(b);
        }

        // ====================================================================
        // PAGE 2: OTOMASI LOKAL & CLOUD (DYNAMIC ADD / REMOVE RULES 0..24!)
        // ====================================================================
        private UIElement BuildPageRules()
        {
            var sp = new StackPanel();

            var h1 = new TextBlock { Text = "Otomasi Lokal & Konektivitas", FontSize = 22, FontWeight = FontWeights.Bold };
            h1.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
            var sub = new TextBlock {
                Text = "Tambah atau kurangi aturan logika IF -> THEN sesuka Anda (0..24 aturan), serta atur jaringan Wi-Fi, MQTT, Webhook, Tunnel VPN, dan Geofence.",
                FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 14)
            };
            sub.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            sp.Children.Add(h1);
            sp.Children.Add(sub);

            var ruleHdrBar = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
            var btnAddRule = MakeStyledButton("+ Tambah Aturan IF -> THEN", "ApPrimaryButton", new Thickness(12, 5, 12, 5));
            DockPanel.SetDock(btnAddRule, Dock.Right);
            btnAddRule.Click += delegate { AddNewAutomationRuleRow(); };
            ruleHdrBar.Children.Add(btnAddRule);

            _lblRuleSectionTitle = MakeSectionCaption("ATURAN OTOMASI LOKAL (4 ATURAN — BEBAS TAMBAH / HAPUS)");
            _lblRuleSectionTitle.VerticalAlignment = VerticalAlignment.Bottom;
            ruleHdrBar.Children.Add(_lblRuleSectionTitle);
            sp.Children.Add(ruleHdrBar);

            var ruleCard = MakeAppleCard(new Thickness(16), new Thickness(0, 0, 0, 18));
            var ruleOuter = new StackPanel();

            var rHdr = CreateRuleGridRow();
            rHdr.Margin = new Thickness(0, 0, 0, 8);
            AddMatrixHeader(rHdr, 0, "Aktif");
            AddMatrixHeader(rHdr, 1, "Nama Aturan");
            AddMatrixHeader(rHdr, 2, "");
            AddMatrixHeader(rHdr, 3, "Sumber Sensor / Sinyal (18)");
            AddMatrixHeader(rHdr, 4, "Syarat");
            AddMatrixHeader(rHdr, 5, "Ambang");
            AddMatrixHeader(rHdr, 6, "Target Eksekusi");
            AddMatrixHeader(rHdr, 7, "Nilai");
            AddMatrixHeader(rHdr, 8, "");
            ruleOuter.Children.Add(rHdr);
            ruleOuter.Children.Add(MakeHairline());

            _ruleRowsPanel = new StackPanel();
            ruleOuter.Children.Add(_ruleRowsPanel);

            var ruleScroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = ruleOuter
            };
            ruleCard.Child = ruleScroll;
            sp.Children.Add(ruleCard);

            // Bottom 2-Column Apple System Settings Form Cards
            var botGrid = new Grid();
            botGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            botGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            botGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var leftCol = new StackPanel();
            leftCol.Children.Add(MakeSectionCaption("JARINGAN WI-FI HOTSPOT & ROUTER"));
            var wifiCard = MakeAppleCard(new Thickness(16, 8, 16, 8), new Thickness(0, 0, 0, 14));
            var wSp = new StackPanel();
            _txtApSsid  = AddAppleFormRow(wSp, "SSID Hotspot AP", "XIO UNIVERSAL STUDIO", true);
            _txtApPass  = AddApplePasswordRow(wSp, "Sandi Hotspot AP", "xio123456", true);
            _txtStaSsid = AddAppleFormRow(wSp, "SSID Router (STA)", "", true);
            _txtStaPass = AddApplePasswordRow(wSp, "Sandi Router (STA)", "", false);
            wifiCard.Child = wSp;
            leftCol.Children.Add(wifiCard);

            leftCol.Children.Add(MakeSectionCaption("GEOFENCE SATELIT GPS (WGS84)"));
            var geoCard = MakeAppleCard(new Thickness(16, 8, 16, 8), new Thickness(0));
            var gSp = new StackPanel();
            _txtGeoLat = AddAppleFormRow(gSp, "Garis Lintang (Lat)", "0.0", true);
            _txtGeoLon = AddAppleFormRow(gSp, "Garis Bujur (Lon)", "0.0", true);
            _txtGeoRad = AddAppleFormRow(gSp, "Radius Aman (Meter)", "100.0", false);
            geoCard.Child = gSp;
            leftCol.Children.Add(geoCard);
            Grid.SetColumn(leftCol, 0);
            botGrid.Children.Add(leftCol);

            var rightCol = new StackPanel();
            rightCol.Children.Add(MakeSectionCaption("TELEMETRI CLOUD MQTT, WEBHOOK & TUNNEL"));
            var clCard = MakeAppleCard(new Thickness(16, 8, 16, 8), new Thickness(0));
            var cSp = new StackPanel();
            _txtMqttHost   = AddAppleFormRow(cSp, "Broker MQTT TCP", "", true);
            _txtMqttPort   = AddAppleFormRow(cSp, "Port MQTT", "1883", true);
            _txtMqttTopic  = AddAppleFormRow(cSp, "Topik Publish", "xio/telemetry", true);
            _txtWebhook    = AddAppleFormRow(cSp, "URL HTTP Webhook", "", true);
            _txtWgEndpoint = AddAppleFormRow(cSp, "Endpoint VPS/WG", "", false);
            clCard.Child = cSp;
            rightCol.Children.Add(clCard);
            Grid.SetColumn(rightCol, 2);
            botGrid.Children.Add(rightCol);

            sp.Children.Add(botGrid);
            return sp;
        }

        private Grid CreateRuleGridRow()
        {
            var g = new Grid { MinWidth = 815 };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(116) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(178) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(66) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(68) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 166 });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
            return g;
        }

        private void RebuildDynamicRuleRows()
        {
            if (_ruleRowsPanel == null) return;
            _suppressRuleEvents = true;
            _ruleRowsPanel.Children.Clear();
            _ruleEnBoxes.Clear();
            _ruleNameBoxes.Clear();
            _ruleSrcCombos.Clear();
            _ruleOpCombos.Clear();
            _ruleThBoxes.Clear();
            _ruleTgtCombos.Clear();
            _ruleValBoxes.Clear();

            if (_lblRuleSectionTitle != null)
                _lblRuleSectionTitle.Text = String.Format("ATURAN OTOMASI LOKAL ({0} ATURAN — BEBAS TAMBAH / HAPUS)", _rules.Count);

            if (_rules.Count == 0)
            {
                var emptyR = new TextBlock {
                    Text = "Tidak ada aturan otomasi lokal (0 aturan). Klik '+ Tambah Aturan IF -> THEN' di kanan atas kapan saja Anda membutuhkan logika otomatis.",
                    FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 8)
                };
                emptyR.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
                _ruleRowsPanel.Children.Add(emptyR);
                _suppressRuleEvents = false;
                return;
            }

            string[] opNames = new string[] { ">", "<", ">=", "<=", "==" };
            int slotCap = Math.Max(1, _slots.Count);

            for (int i = 0; i < _rules.Count; i++)
            {
                int rIdx = i;
                _rules[i].Index = i;
                AutomationRuleConfig rc = _rules[i];

                var rg = CreateRuleGridRow();
                rg.Margin = new Thickness(0, 8, 0, 4);

                var sw = new CheckBox { Style = ResStyle("ApToggleSwitch"), IsChecked = rc.Enabled, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left };
                Grid.SetColumn(sw, 0);
                rg.Children.Add(sw);
                _ruleEnBoxes.Add(sw);

                var tNm = MakeAppleTextBox(string.IsNullOrEmpty(rc.Name) ? ("Aturan " + (i + 1)) : rc.Name, double.NaN);
                tNm.Margin = new Thickness(0, 0, 6, 0);
                Grid.SetColumn(tNm, 1);
                rg.Children.Add(tNm);
                _ruleNameBoxes.Add(tNm);

                var lblIf = new TextBlock { Text = "JIKA", FontSize = 11, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
                lblIf.SetResourceReference(TextBlock.ForegroundProperty, "ApBlue");
                Grid.SetColumn(lblIf, 2);
                rg.Children.Add(lblIf);

                var cSrc = MakeAppleCombo(double.NaN);
                foreach (string sn in RuleSourceLabels) cSrc.Items.Add(sn);
                cSrc.SelectedIndex = (rc.Source >= 0 && rc.Source < RuleSourceLabels.Length) ? rc.Source : 0;
                cSrc.Margin = new Thickness(0, 0, 6, 0);
                Grid.SetColumn(cSrc, 3);
                rg.Children.Add(cSrc);
                _ruleSrcCombos.Add(cSrc);

                var cOp = MakeAppleCombo(double.NaN);
                foreach (string onm in opNames) cOp.Items.Add(onm);
                cOp.SelectedIndex = (rc.Op >= 0 && rc.Op < opNames.Length) ? rc.Op : 0;
                cOp.Margin = new Thickness(0, 0, 6, 0);
                Grid.SetColumn(cOp, 4);
                rg.Children.Add(cOp);
                _ruleOpCombos.Add(cOp);

                var tTh = MakeAppleTextBox(rc.Threshold.ToString("F1", CultureInfo.InvariantCulture), double.NaN);
                tTh.Margin = new Thickness(0, 0, 6, 0);
                Grid.SetColumn(tTh, 5);
                rg.Children.Add(tTh);
                _ruleThBoxes.Add(tTh);

                var cTgt = MakeAppleCombo(double.NaN);
                for (int s = 0; s < slotCap; s++)
                    cTgt.Items.Add(new ComboBoxItem { Content = String.Format("Maka -> Slot Pin #{0:D2}", s + 1), Tag = s });
                cTgt.Items.Add(new ComboBoxItem { Content = "Maka -> Toggle Hotspot AP", Tag = 100 });
                cTgt.Items.Add(new ComboBoxItem { Content = "Maka -> Log CSV MicroSD", Tag = 101 });
                cTgt.Items.Add(new ComboBoxItem { Content = "Maka -> Push MQTT/Webhook", Tag = 102 });

                int tgtSelIdx = 0;
                for (int ti = 0; ti < cTgt.Items.Count; ti++)
                {
                    var ci = cTgt.Items[ti] as ComboBoxItem;
                    if (ci != null && (int)ci.Tag == rc.TargetSlot) { tgtSelIdx = ti; break; }
                }
                cTgt.SelectedIndex = tgtSelIdx;
                cTgt.Margin = new Thickness(0, 0, 6, 0);
                Grid.SetColumn(cTgt, 6);
                rg.Children.Add(cTgt);
                _ruleTgtCombos.Add(cTgt);

                var tVal = MakeAppleTextBox(rc.ActionVal.ToString(), double.NaN);
                tVal.Margin = new Thickness(0, 0, 5, 0);
                tVal.ToolTip = "0=OFF, 1=ON, 2=TOGGLE, 0..255 PWM, 0..180 Derajat Servo";
                Grid.SetColumn(tVal, 7);
                rg.Children.Add(tVal);
                _ruleValBoxes.Add(tVal);

                var btnDelRule = MakeStyledButton("×", "ApButton", new Thickness(0));
                btnDelRule.Width = 24;
                btnDelRule.Height = 26;
                btnDelRule.ToolTip = "Hapus Aturan #" + (i + 1);
                btnDelRule.SetResourceReference(Button.ForegroundProperty, "ApRed");
                btnDelRule.Click += delegate { RemoveAutomationRuleRow(rIdx); };
                Grid.SetColumn(btnDelRule, 8);
                rg.Children.Add(btnDelRule);

                sw.Checked += delegate { PullRulesFromUi(rIdx); };
                sw.Unchecked += delegate { PullRulesFromUi(rIdx); };
                tNm.TextChanged += delegate { PullRulesFromUi(rIdx); };
                cSrc.SelectionChanged += delegate { PullRulesFromUi(rIdx); };
                cOp.SelectionChanged += delegate { PullRulesFromUi(rIdx); };
                tTh.TextChanged += delegate { PullRulesFromUi(rIdx); };
                cTgt.SelectionChanged += delegate { PullRulesFromUi(rIdx); };
                tVal.TextChanged += delegate { PullRulesFromUi(rIdx); };

                _ruleRowsPanel.Children.Add(rg);
            }
            _suppressRuleEvents = false;
        }

        private void AddNewAutomationRuleRow()
        {
            if (_rules.Count >= 24)
            {
                MessageBox.Show("Maksimal 24 aturan otomasi lokal.", "Batas Aturan", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            _rules.Add(new AutomationRuleConfig { Index = _rules.Count, Name = "Aturan " + (_rules.Count + 1), Enabled = true, Source = 1, Threshold = 32.0 });
            RebuildDynamicRuleRows();
        }

        private void RemoveAutomationRuleRow(int idx)
        {
            if (idx < 0 || idx >= _rules.Count) return;
            _rules.RemoveAt(idx);
            RebuildDynamicRuleRows();
        }

        // ====================================================================
        // PAGE 4: RAKIT FIRMWARE, ESPCONNECT USB INSPECTOR & TERMINAL
        // ====================================================================
        private UIElement BuildAiDefaultsCard()
        {
            var card = MakeAppleCard(new Thickness(16), new Thickness(0, 0, 0, 16));
            var sp = new StackPanel();
            sp.Children.Add(MakeSectionCaption("KONFIGURASI AI — EKSPOR HEADER"));
            _lblAiDefaultsState = new TextBlock { FontSize = 12.5, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) };
            _lblAiDefaultsState.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            sp.Children.Add(_lblAiDefaultsState);

            _btnAiIntegrated = MakeStyledButton("Pilih Konfigurasi AI", "ApPrimaryButton", new Thickness(12, 5, 12, 5));
            _btnAiIntegrated.HorizontalAlignment = HorizontalAlignment.Left;
            _btnAiIntegrated.Margin = new Thickness(0, 0, 0, 8);
            _btnAiIntegrated.Click += delegate {
                foreach (PuzzleFeatureItem f in _features)
                    if (f.Key == "ai_cloud") { f.InActiveDeck = true; f.Enabled = true; }
                _cmbEngine.SelectedIndex = 1;
                RebuildCatalogCards();
                RefreshResourceGauges();
            };
            sp.Children.Add(_btnAiIntegrated);

            _aiDefaultsInputs = new StackPanel();
            _aiDefaultsInputs.Children.Add(MakeSmallLabel("Provider AI"));
            _cmbAiProvider = MakeAppleCombo(double.NaN);
            _cmbAiProvider.Name = "AiDefaultProvider";
            _cmbAiProvider.Items.Add("NVIDIA");
            _cmbAiProvider.Items.Add("OpenRouter");
            _cmbAiProvider.SelectedIndex = 0;
            _aiDefaultsInputs.Children.Add(_cmbAiProvider);
            _txtAiModel = AddAppleFormRow(_aiDefaultsInputs, "Model AI", CodeSynthesizer.DefaultAiModel("nvidia"), true);
            _txtAiModel.Name = "AiDefaultModel";
            _txtAiModel.MaxLength = 96;

            var keyRow = new Grid { Margin = new Thickness(0, 7, 0, 7) };
            keyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(155) });
            keyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            keyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var keyLabel = MakeSmallLabel("API key default");
            keyLabel.VerticalAlignment = VerticalAlignment.Center;
            keyRow.Children.Add(keyLabel);
            _pwdAiApiKey = new PasswordBox { Name = "AiDefaultApiKey", MaxLength = 512, Height = 30, Padding = new Thickness(9, 4, 9, 4), FontSize = 12.5 };
            _pwdAiApiKey.SetResourceReference(PasswordBox.BackgroundProperty, "ApInputBg");
            _pwdAiApiKey.SetResourceReference(PasswordBox.ForegroundProperty, "ApTextPri");
            _pwdAiApiKey.SetResourceReference(PasswordBox.BorderBrushProperty, "ApHairline");
            System.Windows.Automation.AutomationProperties.SetName(_pwdAiApiKey, "API key AI default");
            Grid.SetColumn(_pwdAiApiKey, 1);
            keyRow.Children.Add(_pwdAiApiKey);
            var clear = MakeStyledButton("Kosongkan", "ApButton", new Thickness(10, 5, 10, 5));
            clear.Margin = new Thickness(8, 0, 0, 0);
            clear.Click += delegate { _pwdAiApiKey.Clear(); };
            Grid.SetColumn(clear, 2);
            keyRow.Children.Add(clear);
            _aiDefaultsInputs.Children.Add(keyRow);
            sp.Children.Add(_aiDefaultsInputs);

            var note = new TextBlock {
                Text = "Validasi hanya memeriksa format, bukan koneksi atau ketersediaan model. Profil pribadi mengenkripsi kunci AI untuk akun Windows ini; header pribadi dan firmware yang menanamnya dapat membocorkan kunci. Ekspor Profil Publik tidak menyertakan kredensial.",
                FontSize = 11.5, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0)
            };
            note.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            sp.Children.Add(note);
            _cmbAiProvider.SelectionChanged += delegate {
                string current = _txtAiModel.Text.Trim();
                if (current.Length == 0 || current == CodeSynthesizer.DefaultAiModel("nvidia") || current == CodeSynthesizer.DefaultAiModel("openrouter"))
                    _txtAiModel.Text = CodeSynthesizer.DefaultAiModel(_cmbAiProvider.SelectedIndex == 1 ? "openrouter" : "nvidia");
                RefreshAiDefaultsUi();
            };
            _txtAiModel.TextChanged += delegate { RefreshAiDefaultsUi(); };
            _pwdAiApiKey.PasswordChanged += delegate { RefreshAiDefaultsUi(); };
            card.Child = sp;
            RefreshAiDefaultsUi();
            return card;
        }

        private void RefreshAiDefaultsUi()
        {
            if (_aiDefaultsInputs == null) return;
            bool aiOn = false;
            foreach (PuzzleFeatureItem f in _features)
                if (f.Key == "ai_cloud" && f.Enabled && f.InActiveDeck) aiOn = true;
            bool integrated = _buildMode == 1 && _activeChip.SupportsMarauderMode;
            _aiDefaultsInputs.IsEnabled = integrated;
            _btnAiIntegrated.Visibility = integrated && aiOn ? Visibility.Collapsed : Visibility.Visible;
            _btnAiIntegrated.IsEnabled = _activeChip.SupportsMarauderMode;
            _lblAiDefaultsState.Text = !integrated
                ? "Pilih Konfigurasi AI untuk mengekspor default provider/model. Firmware dan layanan chat tidak disertakan."
                : !aiOn ? "Modul AI dinonaktifkan: ekspor header tidak menyertakan kunci."
                : _pwdAiApiKey.Password.Length == 0 ? "Kunci kosong. Header berisi default provider/model tanpa kredensial."
                : "Kunci tersamar di aplikasi, tetapi ekspor header pribadi akan memuat kunci mentah.";
        }

        private UIElement BuildPageBuild()
        {
            var sp = new StackPanel();

            var h1 = new TextBlock { Text = "Ekspor Profil & Konfigurasi AI", FontSize = 22, FontWeight = FontWeights.Bold };
            h1.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
            var sub = new TextBlock {
                Text = "Siapkan profil publik tanpa rahasia atau ekspor header pribadi. Edisi ini tidak menyertakan source firmware maupun server chat AI.",
                FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 14)
            };
            sub.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            sp.Children.Add(h1);
            sp.Children.Add(sub);

            sp.Children.Add(BuildAiDefaultsCard());
            var publicExport = MakeStyledButton("Ekspor Profil Publik", "ApPrimaryButton", new Thickness(12, 7, 12, 7));
            publicExport.Name = "ExportPublicProfile";
            publicExport.HorizontalAlignment = HorizontalAlignment.Left;
            publicExport.Margin = new Thickness(0, 0, 0, 12);
            publicExport.Click += delegate { ExportPublicProjectDialog(); };
            sp.Children.Add(publicExport);
            _lblBackendState = new TextBlock { FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) };
            _lblBackendState.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            sp.Children.Add(_lblBackendState);
            sp.Children.Add(MakeSectionCaption("BACKEND LOKAL & USB OPSIONAL"));
            var bldCard = MakeAppleCard(new Thickness(16), new Thickness(0, 0, 0, 16));
            var bSp = new StackPanel();

            var actionGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var usbLeft = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var portLbl = new TextBlock { Text = "Port:", FontSize = 12.5, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) };
            portLbl.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            usbLeft.Children.Add(portLbl);

            _cmbComPort = MakeAppleCombo(105);
            _cmbComPort.Margin = new Thickness(0, 0, 6, 0);
            usbLeft.Children.Add(_cmbComPort);

            var btnRef = MakeStyledButton("Segarkan", "ApButton", new Thickness(10, 5, 10, 5));
            btnRef.Margin = new Thickness(0, 0, 10, 0);
            btnRef.Click += delegate { RefreshComPorts(); };
            usbLeft.Children.Add(btnRef);

            _cmbBaud = MakeAppleCombo(96);
            _cmbBaud.Items.Add("115200"); _cmbBaud.Items.Add("460800"); _cmbBaud.Items.Add("921600");
            _cmbBaud.SelectedIndex = 0;
            usbLeft.Children.Add(_cmbBaud);
            Grid.SetColumn(usbLeft, 0);
            actionGrid.Children.Add(usbLeft);

            var buildRight = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            var bHdrOnly = MakeStyledButton("Buat Header C++", "ApButton", new Thickness(10, 6, 10, 6));
            bHdrOnly.Margin = new Thickness(5, 2, 0, 2);
            bHdrOnly.Click += delegate { SynthesizeHeadersOnly(); };
            buildRight.Children.Add(bHdrOnly);

            var bSyncQuick = MakeStyledButton("Sinkron Pin (<1d)", "ApButton", new Thickness(10, 6, 10, 6));
            bSyncQuick.Margin = new Thickness(5, 2, 0, 2);
            bSyncQuick.Click += delegate { TriggerLiveUsbSync(); };
            buildRight.Children.Add(bSyncQuick);

            var bBuildBin = MakeStyledButton("Rakit Firmware (.bin)", "ApPrimaryButton", new Thickness(11, 6, 11, 6));
            bBuildBin.Margin = new Thickness(5, 2, 0, 2);
            bBuildBin.Click += delegate { StartCompileFirmware(false); };
            _btnBuildFirmware = bBuildBin;
            buildRight.Children.Add(bBuildBin);

            var bBuildFlash = MakeStyledButton("Rakit & Flash USB", "ApPrimaryButton", new Thickness(11, 6, 11, 6));
            bBuildFlash.Background = BrushHex("#34C759");
            bBuildFlash.Margin = new Thickness(5, 2, 0, 2);
            bBuildFlash.Click += delegate { StartCompileFirmware(true); };
            _btnBuildAndFlash = bBuildFlash;
            buildRight.Children.Add(bBuildFlash);

            Grid.SetColumn(buildRight, 1);
            actionGrid.Children.Add(buildRight);
            bSp.Children.Add(actionGrid);

            bSp.Children.Add(MakeHairline());

            _txtCliPath     = AddAppleFormRow(bSp, "Path arduino-cli.exe", UsbEspTools.FindArduinoCli(), true);
            _txtEsptoolPath = AddAppleFormRow(bSp, "Path esptool.exe", UsbEspTools.FindEsptool(), true);
            string defOut   = Path.Combine(_studioRoot, @"output_firmware\XIO_UNIV_ESP32_ALL_IN_ONE_0x0.bin");
            _txtOutBinPath  = AddAppleFormRow(bSp, "File Output Firmware", defOut, false);

            bldCard.Child = bSp;
            sp.Children.Add(bldCard);

            var midGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
            midGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            midGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            midGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var inspCard = MakeAppleCard(new Thickness(16), new Thickness(0));
            var iSp = new StackPanel();
            var iTitle = new TextBlock { Text = "Inspektor Perangkat USB (ESPConnect)", FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 10) };
            iTitle.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
            iSp.Children.Add(iTitle);

            var iBtns = new UniformGrid { Columns = 2, Margin = new Thickness(0, 0, 0, 10) };
            var bDet = MakeStyledButton("Deteksi Chip & MAC", "ApButton", new Thickness(10, 6, 10, 6));
            bDet.Margin = new Thickness(0, 0, 4, 6);
            bDet.Click += delegate { RunUsbChipDetect(); };
            iBtns.Children.Add(bDet);

            var bPart = MakeStyledButton("Baca Partisi (0x8000)", "ApButton", new Thickness(10, 6, 10, 6));
            bPart.Margin = new Thickness(4, 0, 0, 6);
            bPart.Click += delegate { RunUsbReadPartitions(); };
            iBtns.Children.Add(bPart);

            var bBak = MakeStyledButton("Backup Full Flash (.bin)", "ApButton", new Thickness(10, 6, 10, 6));
            bBak.Margin = new Thickness(0, 0, 4, 0);
            bBak.Click += delegate { RunUsbBackupFlash(); };
            iBtns.Children.Add(bBak);

            var bFlBin = MakeStyledButton("Flash File .bin Pilihan", "ApButton", new Thickness(10, 6, 10, 6));
            bFlBin.Margin = new Thickness(4, 0, 0, 0);
            bFlBin.Click += delegate { RunFlashExistingBin(); };
            iBtns.Children.Add(bFlBin);
            iSp.Children.Add(iBtns);

            iSp.Children.Add(MakeHairline());
            _lblProbeChip  = AddSidebarSpecRow(iSp, "Chip Silikon", "Belum dipindai", true);
            _lblProbeFeat  = AddSidebarSpecRow(iSp, "Fitur Chip", "-", true);
            _lblProbeMac   = AddSidebarSpecRow(iSp, "Alamat MAC", "-", true);
            _lblProbeFlash = AddSidebarSpecRow(iSp, "Ukuran Flash", "-", false);
            inspCard.Child = iSp;
            Grid.SetColumn(inspCard, 0);
            midGrid.Children.Add(inspCard);

            var ptCard = MakeAppleCard(new Thickness(16), new Thickness(0));
            var ptSp = new StackPanel();
            var ptTitle = new TextBlock { Text = "Peta Partisi Flash (Dekoder 0xAA50)", FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 10) };
            ptTitle.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
            ptSp.Children.Add(ptTitle);

            var ptHdr = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            ptHdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(105) });
            ptHdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            ptHdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
            ptHdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            AddMatrixHeader(ptHdr, 0, "Nama");
            AddMatrixHeader(ptHdr, 1, "Tipe / Sub");
            AddMatrixHeader(ptHdr, 2, "Offset");
            AddMatrixHeader(ptHdr, 3, "Kapasitas");
            ptSp.Children.Add(ptHdr);
            ptSp.Children.Add(MakeHairline());

            _partTableRows = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            var ptEmpty = new TextBlock {
                Text = "Hubungkan perangkat ESP via USB lalu klik 'Baca Partisi (0x8000)' untuk menampilkan struktur ruang NVS, OTA, App, dan SPIFFS.",
                FontSize = 12, TextWrapping = TextWrapping.Wrap
            };
            ptEmpty.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            _partTableRows.Children.Add(ptEmpty);

            var ptSv = new ScrollViewer { Height = 165, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = _partTableRows };
            ptSp.Children.Add(ptSv);
            ptCard.Child = ptSp;
            Grid.SetColumn(ptCard, 2);
            midGrid.Children.Add(ptCard);
            sp.Children.Add(midGrid);

            sp.Children.Add(MakeSectionCaption("KONSOL KOMPILASI & LIVE SERIAL MONITOR"));
            var termCard = new Border
            {
                Background = BrushHex("#141417"),
                BorderBrush = BrushHex("#2E2E34"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14)
            };
            var tSp = new StackPanel();
            var tBar = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };

            var tRight = new StackPanel { Orientation = Orientation.Horizontal };
            _btnSerialMon = MakeStyledButton("Buka Serial Monitor", "ApButton", new Thickness(11, 5, 11, 5));
            _btnSerialMon.Margin = new Thickness(0, 0, 6, 0);
            _btnSerialMon.Click += delegate { ToggleLiveSerialMonitor(); };
            tRight.Children.Add(_btnSerialMon);

            var bClr = MakeStyledButton("Bersihkan", "ApButton", new Thickness(10, 5, 10, 5));
            bClr.Click += delegate { _txtConsoleLog.Clear(); };
            tRight.Children.Add(bClr);
            DockPanel.SetDock(tRight, Dock.Right);
            tBar.Children.Add(tRight);

            tBar.Children.Add(new TextBlock {
                Text = "Terminal — Output Compiler, Companion Stack, Esptool & Serial ESP",
                FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = BrushHex("#A1A1AA"), VerticalAlignment = VerticalAlignment.Center
            });
            tSp.Children.Add(tBar);

            _txtConsoleLog = new TextBox
            {
                Height = 220,
                IsReadOnly = true,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            _txtConsoleLog.SetResourceReference(FrameworkElement.StyleProperty, "ApTerminalBox");
            tSp.Children.Add(_txtConsoleLog);

            var sendGrid = new Grid { Margin = new Thickness(0, 10, 0, 0) };
            sendGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sendGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _txtSerialInput = MakeAppleTextBox("XIO PING", double.NaN);
            _txtSerialInput.Margin = new Thickness(0, 0, 8, 0);
            _txtSerialInput.KeyDown += delegate(object s, KeyEventArgs e) { if (e.Key == Key.Enter) SendSerialLineFromBox(); };
            Grid.SetColumn(_txtSerialInput, 0);
            sendGrid.Children.Add(_txtSerialInput);

            var bSend = MakeStyledButton("Kirim Perintah", "ApPrimaryButton", new Thickness(14, 5, 14, 5));
            bSend.Click += delegate { SendSerialLineFromBox(); };
            Grid.SetColumn(bSend, 1);
            sendGrid.Children.Add(bSend);

            tSp.Children.Add(sendGrid);
            termCard.Child = tSp;
            sp.Children.Add(termCard);

            RefreshBackendAvailability();
            return sp;
        }

        // ====================================================================
        // NAVIGATION, SCROLL, ZOOM & DIAGRAM RENDERING
        // ====================================================================
        private void SelectPage(int idx)
        {
            _activePageIdx = idx;
            _pageCatalog.Visibility    = (idx == 0) ? Visibility.Visible : Visibility.Collapsed;
            _pagePins.Visibility       = (idx == 1) ? Visibility.Visible : Visibility.Collapsed;
            _pageRules.Visibility      = (idx == 2) ? Visibility.Visible : Visibility.Collapsed;
            _pageCompanions.Visibility = (idx == 3) ? Visibility.Visible : Visibility.Collapsed;
            _pageBuild.Visibility      = (idx == 4) ? Visibility.Visible : Visibility.Collapsed;
            UpdateSegmentVisuals();
            if (_mainScroll != null) _mainScroll.ScrollToTop();
        }

        private void UpdateSegmentVisuals()
        {
            for (int i = 0; i < _segButtons.Count; i++)
            {
                bool active = (i == _activePageIdx);
                _segButtons[i].Background = active ? ResBrush("ApSegActive") : Brushes.Transparent;
                _segButtons[i].Foreground = active ? ResBrush("ApTextPri") : ResBrush("ApTextSec");
            }
        }

        private void UpdateWorkspaceResponsiveWidth()
        {
            if (_mainScroll == null || _pageContainer == null) return;
            double vw = _mainScroll.ViewportWidth;
            if (vw > 100)
            {
                double target = (vw / _zoomScale) - 42.0;
                _pageContainer.Width = Math.Max(885.0, target);
            }
        }

        private void SetZoom(double z)
        {
            if (z < 0.75) z = 0.75;
            if (z > 1.50) z = 1.50;
            _zoomScale = z;
            _workspaceScaleHost.LayoutTransform = new ScaleTransform(_zoomScale, _zoomScale);
            UpdateWorkspaceResponsiveWidth();
            if (_lblZoom != null) _lblZoom.Text = String.Format("{0}%", (int)Math.Round(_zoomScale * 100));
        }

        private void AttachFourWayScrollInteractions(ScrollViewer sv)
        {
            sv.PreviewMouseWheel += delegate(object sender, MouseWheelEventArgs e)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                {
                    SetZoom(_zoomScale + (e.Delta > 0 ? 0.08 : -0.08));
                    e.Handled = true;
                }
                else if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                {
                    sv.ScrollToHorizontalOffset(sv.HorizontalOffset - e.Delta * 0.8);
                    e.Handled = true;
                }
                else
                {
                    var src = e.OriginalSource as DependencyObject;
                    while (src != null && src != sv)
                    {
                        if (src is TextBox && ((TextBox)src).AcceptsReturn) return;
                        if (src == _chipDiagramScroll && _chipDiagramScroll != null && _chipDiagramScroll.ComputedVerticalScrollBarVisibility == Visibility.Visible) return;
                        src = VisualTreeHelper.GetParent(src);
                    }
                    sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta * 0.65);
                    e.Handled = true;
                }
            };

            sv.PreviewMouseDown += delegate(object sender, MouseButtonEventArgs e)
            {
                if (e.ChangedButton == MouseButton.Middle || e.ChangedButton == MouseButton.Right)
                {
                    _isPanning = true;
                    _panStartMouse = e.GetPosition(this);
                    _panStartH = sv.HorizontalOffset;
                    _panStartV = sv.VerticalOffset;
                    sv.Cursor = Cursors.ScrollAll;
                    sv.CaptureMouse();
                    e.Handled = true;
                }
            };
            sv.PreviewMouseMove += delegate(object sender, MouseEventArgs e)
            {
                if (_isPanning)
                {
                    Point cur = e.GetPosition(this);
                    sv.ScrollToHorizontalOffset(_panStartH - (cur.X - _panStartMouse.X));
                    sv.ScrollToVerticalOffset(_panStartV - (cur.Y - _panStartMouse.Y));
                    e.Handled = true;
                }
            };
            sv.PreviewMouseUp += delegate(object sender, MouseButtonEventArgs e)
            {
                if (_isPanning && (e.ChangedButton == MouseButton.Middle || e.ChangedButton == MouseButton.Right))
                {
                    _isPanning = false;
                    sv.Cursor = Cursors.Arrow;
                    sv.ReleaseMouseCapture();
                    e.Handled = true;
                }
            };
        }

        private void DrawInteractiveChipDiagram()
        {
            if (_chipDiagramCanvas == null) return;
            _chipDiagramCanvas.Children.Clear();

            var slotByGpio = new Dictionary<int, PinSlotConfig>();
            foreach (PinSlotConfig s in _slots)
            {
                if (s.Gpio >= 0 && s.Role > 0 && !slotByGpio.ContainsKey(s.Gpio))
                    slotByGpio[s.Gpio] = s;
            }

            int rows = Math.Max(_activeChip.LeftHeader.Count, _activeChip.RightHeader.Count);
            double rowH = 25.0;
            double bodyTop = 26.0;
            double bodyH = Math.Max(210.0, rows * rowH + 22.0);
            _chipDiagramCanvas.Width = 314;
            _chipDiagramCanvas.Height = bodyTop + bodyH + 24.0;

            var pcb = new Border
            {
                Width = 96,
                Height = bodyH,
                Background = BrushHex(_isDarkMode ? "#26262B" : "#E5E5EA"),
                BorderBrush = BrushHex(_isDarkMode ? "#484852" : "#C7C7CC"),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(12)
            };
            Canvas.SetLeft(pcb, 109);
            Canvas.SetTop(pcb, bodyTop);
            _chipDiagramCanvas.Children.Add(pcb);

            var die = new Border
            {
                Width = 70,
                Height = 70,
                Background = BrushHex("#1C1C1E"),
                BorderBrush = BrushHex("#3A3A3C"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Child = new TextBlock {
                    Text = _activeChip.Id.ToUpperInvariant() + "\nSILICON",
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 9.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = BrushHex("#F5F5F7")
                }
            };
            Canvas.SetLeft(die, 122);
            Canvas.SetTop(die, bodyTop + 20);
            _chipDiagramCanvas.Children.Add(die);

            var usb = new Border
            {
                Width = 40, Height = 14,
                Background = BrushHex("#636366"),
                CornerRadius = new CornerRadius(3),
                Child = new TextBlock { Text = "USB-C", FontSize = 8, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
            };
            Canvas.SetLeft(usb, 137);
            Canvas.SetTop(usb, bodyTop + bodyH - 7);
            _chipDiagramCanvas.Children.Add(usb);

            for (int i = 0; i < _activeChip.LeftHeader.Count; i++)
                DrawApplePinPill(_activeChip.LeftHeader[i], true, bodyTop + 12 + i * rowH, slotByGpio);
            for (int i = 0; i < _activeChip.RightHeader.Count; i++)
                DrawApplePinPill(_activeChip.RightHeader[i], false, bodyTop + 12 + i * rowH, slotByGpio);
        }

        private void DrawApplePinPill(HeaderPinVisual hp, bool isLeft, double y, Dictionary<int, PinSlotConfig> assigned)
        {
            string bgHex = _isDarkMode ? "#2C2C31" : "#E5E5EA";
            string fgHex = _isDarkMode ? "#98989F" : "#6E6E73";
            string dotHex = "#8E8E93";
            bool clickable = false;

            if (hp.Gpio >= 0)
            {
                PinCapFlags caps = _activeChip.CapResolver(hp.Gpio);
                if ((caps & (PinCapFlags.LockedFlash | PinCapFlags.LockedPsram | PinCapFlags.LockedSys)) != 0)
                {
                    dotHex = "#FF3B30";
                    bgHex = _isDarkMode ? "#3A1D1E" : "#FFEBEA";
                    fgHex = _isDarkMode ? "#FF6961" : "#C92A2A";
                }
                else
                {
                    clickable = true;
                    fgHex = _isDarkMode ? "#F5F5F7" : "#1D1D1F";
                    bgHex = _isDarkMode ? "#26262B" : "#FFFFFF";
                    if ((caps & PinCapFlags.NativeUsb) != 0) dotHex = "#AF52DE";
                    else if ((caps & PinCapFlags.Adc1) != 0) dotHex = "#007AFF";
                    else if ((caps & PinCapFlags.Out) == 0 || (caps & PinCapFlags.Strap) != 0) dotHex = "#FF9500";
                    else dotHex = "#34C759";
                }
            }

            string text = hp.Label;
            if (hp.Gpio >= 0 && assigned.ContainsKey(hp.Gpio))
            {
                PinSlotConfig sc = assigned[hp.Gpio];
                text = !string.IsNullOrEmpty(sc.Label) ? sc.Label : ("GPIO " + hp.Gpio);
                bgHex = "#007AFF";
                fgHex = "#FFFFFF";
                dotHex = "#FFFFFF";
            }

            var rowGrid = new Grid();
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = isLeft ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = isLeft ? GridLength.Auto : new GridLength(1, GridUnitType.Star) });

            var dot = new Border { Width = 6, Height = 6, CornerRadius = new CornerRadius(3), Background = BrushHex(dotHex), VerticalAlignment = VerticalAlignment.Center, Margin = isLeft ? new Thickness(5, 0, 0, 0) : new Thickness(0, 0, 5, 0) };
            var lbl = new TextBlock {
                Text = text, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = BrushHex(fgHex),
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = isLeft ? TextAlignment.Right : TextAlignment.Left,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            Grid.SetColumn(lbl, isLeft ? 0 : 1);
            Grid.SetColumn(dot, isLeft ? 1 : 0);
            rowGrid.Children.Add(lbl);
            rowGrid.Children.Add(dot);

            var pill = new Border
            {
                Width = 100,
                Height = 20,
                Background = BrushHex(bgHex),
                BorderBrush = (hp.Gpio == _selectedDiagramGpio && hp.Gpio >= 0) ? BrushHex("#007AFF") : ResBrush("ApHairline"),
                BorderThickness = new Thickness(hp.Gpio == _selectedDiagramGpio && hp.Gpio >= 0 ? 1.5 : 0.8),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(5, 1, 5, 1),
                Cursor = clickable ? Cursors.Hand : Cursors.Arrow,
                Child = rowGrid
            };

            if (clickable)
            {
                int gNum = hp.Gpio;
                pill.MouseLeftButtonDown += delegate {
                    _selectedDiagramGpio = gNum;
                    AssignClickedDiagramPin(gNum);
                };
            }

            Canvas.SetLeft(pill, isLeft ? 4 : 210);
            Canvas.SetTop(pill, y);
            _chipDiagramCanvas.Children.Add(pill);
        }

        // ====================================================================
        // DATA BINDING, VALIDATION & HARDWARE ACTIONS
        // ====================================================================
        private void RefreshAllChipDependentUi()
        {
            if (_valArch != null)
            {
                _valArch.Text  = _activeChip.Arch.Replace(" @ ", "\n");
                _valGpio.Text  = String.Format("GPIO 0..{0} ({1})", _activeChip.MaxGpio, _activeChip.BootloaderAddr);
                var rads = new List<string>();
                if (_activeChip.HasWifi) rads.Add(_activeChip.HasWifi5G ? "Wi-Fi 6 (2.4/5G)" : "Wi-Fi 2.4G");
                if (_activeChip.HasBle) rads.Add("BLE");
                if (_activeChip.HasZigbee) rads.Add("802.15.4");
                _valRadio.Text = rads.Count > 0 ? string.Join(" • ", rads.ToArray()) : "Non-Radio (MCU)";
                _valBus.Text   = String.Format("I2C {0}/{1} • UART {2}/{3}", _activeChip.DefSda, _activeChip.DefScl, _activeChip.DefUartRx, _activeChip.DefUartTx);
            }

            if (_txtOutBinPath != null)
            {
                string fname = (_buildMode == 0)
                    ? String.Format("XIO_UNIV_{0}_ALL_IN_ONE_0x0.bin", _activeChip.Id.ToUpperInvariant())
                    : "XIO_PUZZLE_ALL_IN_ONE_0x0.bin";
                _txtOutBinPath.Text = Path.Combine(_studioRoot, "output_firmware", fname);
            }

            RebuildDynamicSlotRows();
            ValidateAndRenderDiagram();
            RebuildCatalogCards();
            RefreshResourceGauges();
        }

        private void RefreshResourceGauges()
        {
            RefreshBackendAvailability();
            int maxFlashKb = 3136;
            int maxRamKb = (_activeChip.Id == "esp32c2") ? 272 : 320;
            bool webUiOn = false;
            foreach (PuzzleFeatureItem wf in _features)
            {
                if (wf.Key == "gzip_webui" && wf.InActiveDeck && wf.Enabled && _activeChip.HasWifi)
                    webUiOn = true;
            }

            int usedFlash = (_buildMode == 0) ? (_activeChip.HasWifi ? (webUiOn ? 810 : 450) : 390) : 1380;
            int usedRam   = (_buildMode == 0) ? (_activeChip.HasWifi ? (webUiOn ? 36 : 22) : 15)    : 64;

            foreach (PuzzleFeatureItem f in _features)
            {
                if (!f.InActiveDeck || !f.Enabled) continue;
                if (_buildMode == 0 && !f.IsCustomUserModule && string.IsNullOrEmpty(f.UnivMacro)) continue;
                if (f.RequiresWifi && !_activeChip.HasWifi) continue;
                if (f.RequiresBle && !_activeChip.HasBle) continue;
                if (f.Key != "gzip_webui")
                {
                    usedFlash += f.SavedFlashKb;
                    usedRam += f.SavedRamKb;
                }
            }

            double fPct = Math.Min(100.0, (usedFlash * 100.0) / maxFlashKb);
            double rPct = Math.Min(100.0, (usedRam * 100.0) / maxRamKb);

            if (_barFlash != null)
            {
                _barFlash.Value = fPct;
                _lblFlashPct.Text = String.Format("{0} KB ({1:F0}%)", usedFlash, fPct);
            }
            if (_barRam != null)
            {
                _barRam.Value = rPct;
                _lblRamPct.Text = String.Format("{0} KB ({1:F0}%)", usedRam, rPct);
            }
        }

        private void ApplyQuickFeaturePreset(string mode)
        {
            foreach (PuzzleFeatureItem f in _features)
            {
                if (mode == "all") { f.InActiveDeck = true; f.Enabled = true; }
                else if (mode == "headless")
                {
                    // True Headless USB/Sensor MCU (even WebUI disabled!)
                    f.Enabled = (f.Key == "boot_guard" || f.Key == "heap_guard" || f.Key == "rules_engine" || f.Key == "env_sensors" || f.Key == "ros2_wg" || f.IsCustomUserModule);
                }
                else if (mode == "industrial")
                {
                    f.Enabled = (f.Category == "Inti & Optimasi" || f.Category == "Industri & Robotik" || f.Key == "env_sensors" || f.Key == "tof_bio_9dof" || f.IsCustomUserModule);
                }
                else if (mode == "rf")
                {
                    f.Enabled = (f.Category == "Inti & Optimasi" || f.Category == "Spektrum RF Pasif" || f.Category == "GNSS, SD & Modem" || f.IsCustomUserModule);
                }
            }
            RebuildCatalogCards();
            RefreshResourceGauges();
        }

        private void ApplyHardwareWiringPreset(string key)
        {
            EspChipProfile c = _activeChip;
            if (key == "i2c_oled")
            {
                AssignSlotSmart(c.DefSda, 13, 0, 0, "I2C SDA");
                AssignSlotSmart(c.DefScl, 14, 0, 0, "I2C SCL");
            }
            else if (key == "sr04_relay")
            {
                AssignSlotSmart(c.DefTrig, 24, 0, 0, "SR04 TRIG");
                AssignSlotSmart(c.DefEcho, 25, 0, 0, "SR04 ECHO");
                AssignSlotSmart(c.DefRelay, 3, 0, 0, "Relay Pompa");
            }
            else if (key == "hx711_btn")
            {
                AssignSlotSmart(c.DefEcho, 26, 0, 0, "HX711 DOUT");
                AssignSlotSmart(c.DefTrig, 27, 0, 0, "HX711 SCK");
                AssignSlotSmart(c.DefBtn, 7, 0, 0, "Tombol Tare");
            }
            else if (key == "modbus_can")
            {
                AssignSlotSmart(c.DefUartRx, 15, 96, 0, "RS485 RX");
                AssignSlotSmart(c.DefUartTx, 16, 96, 0, "RS485 TX");
                AssignSlotSmart(c.DefRelay, 17, 0, 0, "RS485 DE");
                if (c.HasTwaiCan)
                {
                    AssignSlotSmart(c.DefSda, 33, 500, 0, "CAN TX");
                    AssignSlotSmart(c.DefScl, 34, 500, 0, "CAN RX");
                }
            }
            else if (key == "nrf_sd")
            {
                AssignSlotSmart(c.DefSpiSck, 18, 0, 0, "SPI SCK");
                AssignSlotSmart(c.DefSpiMiso, 19, 0, 0, "SPI MISO");
                AssignSlotSmart(c.DefSpiMosi, 20, 0, 0, "SPI MOSI");
                AssignSlotSmart(c.DefSpiCs, 21, 0, 0, "SD Card CS");
                AssignSlotSmart(c.DefTrig, 22, 0, 0, "nRF24 CE");
                AssignSlotSmart(c.DefRelay, 23, 0, 0, "nRF24 CSN");
            }
            else if (key == "lora_subghz")
            {
                AssignSlotSmart(c.DefSpiSck, 18, 0, 0, "SPI SCK");
                AssignSlotSmart(c.DefSpiMiso, 19, 0, 0, "SPI MISO");
                AssignSlotSmart(c.DefSpiMosi, 20, 0, 0, "SPI MOSI");
                AssignSlotSmart(c.DefSpiCs, 35, 433, 0, "LoRa CS 433");
                AssignSlotSmart(c.DefTrig, 36, 0, 0, "LoRa RST");
                AssignSlotSmart(c.DefEcho, 37, 0, 0, "LoRa DIO0");
            }
            else if (key == "audio_i2s")
            {
                AssignSlotSmart(c.DefTrig, 39, 0, 0, "I2S BCLK");
                AssignSlotSmart(c.DefRelay, 40, 0, 0, "I2S WS");
                AssignSlotSmart(c.DefEcho, 41, 0, 0, "I2S DIN");
            }
            else if (key == "robotics")
            {
                AssignSlotSmart(c.DefRelay, 28, 90, 50, "Servo 180");
                AssignSlotSmart(c.DefTrig, 29, 180, 15000, "Motor PWM");
                AssignSlotSmart(c.DefBtn, 30, 1, 0, "Motor DIR");
            }
            else if (key == "gps_adc")
            {
                AssignSlotSmart(c.DefUartRx, 15, 96, 0, "GPS RX");
                AssignSlotSmart(c.DefUartTx, 16, 96, 0, "GPS TX");
                AssignSlotSmart(c.DefAdc1, 38, 1600, 0, "DO Air mg/L");
            }
            RebuildDynamicSlotRows();
            ValidateAndRenderDiagram();
        }

        private void AssignSlotSmart(int gpio, int role, int p1, int p2, string label)
        {
            if (gpio < 0) return;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Gpio == gpio)
                {
                    _slots[i].Role = role; _slots[i].Param1 = p1; _slots[i].Param2 = p2; _slots[i].Label = label;
                    return;
                }
            }
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Gpio < 0 || _slots[i].Role == 0)
                {
                    _slots[i].Gpio = gpio; _slots[i].Role = role; _slots[i].Param1 = p1; _slots[i].Param2 = p2; _slots[i].Label = label;
                    return;
                }
            }
            if (_slots.Count < 32)
            {
                _slots.Add(new PinSlotConfig { SlotIndex = _slots.Count, Gpio = gpio, Role = role, Param1 = p1, Param2 = p2, Label = label });
            }
        }

        private void AssignClickedDiagramPin(int gpio)
        {
            PinCapFlags caps = _activeChip.CapResolver(gpio);
            int defaultRole = ((caps & PinCapFlags.Adc1) != 0 && (caps & PinCapFlags.Out) == 0) ? 9 : 1;
            AssignSlotSmart(gpio, defaultRole, defaultRole == 9 ? 100 : 0, 0, "GPIO " + gpio);
            if (_lblSelectedPinHint != null)
                _lblSelectedPinHint.Text = String.Format("GPIO {0} ditambahkan ke matriks slot di sebelah kanan.", gpio);
            RebuildDynamicSlotRows();
            RebuildDynamicRuleRows();
            ValidateAndRenderDiagram();
        }

        private void ClearAllPinSlots()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].Gpio = -1; _slots[i].Role = 0; _slots[i].Param1 = 0; _slots[i].Param2 = 0; _slots[i].Label = "";
            }
            RebuildDynamicSlotRows();
            ValidateAndRenderDiagram();
        }

        private void PopulateGpioComboForChip(ComboBox cmb, int selectedGpio)
        {
            cmb.Items.Clear();
            cmb.Items.Add(new ComboBoxItem { Content = "Kosong", Tag = -1 });
            int selIdx = 0;
            int cur = 1;
            for (int g = 0; g <= _activeChip.MaxGpio; g++)
            {
                PinCapFlags caps = _activeChip.CapResolver(g);
                if ((caps & (PinCapFlags.LockedFlash | PinCapFlags.LockedPsram | PinCapFlags.LockedSys)) != 0)
                    continue;

                string badge = "";
                if ((caps & PinCapFlags.Adc1) != 0) badge += " • ADC1";
                if ((caps & PinCapFlags.Out) == 0) badge += " • In";
                if ((caps & PinCapFlags.NativeUsb) != 0) badge += " • USB";

                cmb.Items.Add(new ComboBoxItem { Content = String.Format("GPIO {0}{1}", g, badge), Tag = g });
                if (g == selectedGpio) selIdx = cur;
                cur++;
            }
            cmb.SelectedIndex = selIdx;
        }

        private void OnSlotUiChanged(int idx)
        {
            if (_suppressSlotEvents || idx < 0 || idx >= _slots.Count || idx >= _slotGpioCombos.Count) return;
            var gItem = _slotGpioCombos[idx].SelectedItem as ComboBoxItem;
            _slots[idx].Gpio = (gItem != null && gItem.Tag is int) ? (int)gItem.Tag : -1;
            var rItem = _slotRoleCombos[idx].SelectedItem as PinRoleOption;
            _slots[idx].Role = (rItem != null) ? rItem.Code : 0;

            int p1 = 0, p2 = 0;
            int.TryParse(_slotP1Boxes[idx].Text.Trim(), out p1);
            int.TryParse(_slotP2Boxes[idx].Text.Trim(), out p2);
            _slots[idx].Param1 = p1;
            _slots[idx].Param2 = p2;
            _slots[idx].Label = _slotLabelBoxes[idx].Text;

            ValidateAndRenderDiagram();
        }

        private void ValidateAndRenderDiagram()
        {
            var used = new HashSet<int>();
            string firstErr = null;
            int activeCount = 0;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Gpio >= 0 && _slots[i].Role > 0) activeCount++;
                string err = StudioCatalog.ValidateSlot(_activeChip, _slots[i], used);
                if (err != null && firstErr == null)
                    firstErr = String.Format("Slot #{0}: {1}", i + 1, err);
            }

            if (_lblConflictStatus != null)
            {
                if (firstErr != null)
                {
                    _pinStatusBadge.Background = BrushHex("#FF3B30");
                    _lblConflictStatus.Text = firstErr;
                    _lblConflictStatus.SetResourceReference(TextBlock.ForegroundProperty, "ApRed");
                }
                else
                {
                    _pinStatusBadge.Background = BrushHex("#34C759");
                    _lblConflictStatus.Text = String.Format("PinManager ({0}): {1}/{2} slot aktif • 0 konflik pin", _activeChip.Id.ToUpperInvariant(), activeCount, _slots.Count);
                    _lblConflictStatus.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
                }
            }

            DrawInteractiveChipDiagram();
        }

        private void PullRulesFromUi(int idx)
        {
            if (_suppressRuleEvents || idx < 0 || idx >= _rules.Count || idx >= _ruleEnBoxes.Count) return;
            _rules[idx].Enabled = (_ruleEnBoxes[idx].IsChecked == true);
            _rules[idx].Name = _ruleNameBoxes[idx].Text.Trim();
            _rules[idx].Source = Math.Max(0, _ruleSrcCombos[idx].SelectedIndex);
            _rules[idx].Op = Math.Max(0, _ruleOpCombos[idx].SelectedIndex);
            double th = 30.0;
            double.TryParse(_ruleThBoxes[idx].Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out th);
            _rules[idx].Threshold = th;

            var tgtItem = _ruleTgtCombos[idx].SelectedItem as ComboBoxItem;
            if (tgtItem != null && tgtItem.Tag is int)
                _rules[idx].TargetSlot = (int)tgtItem.Tag;

            int av = 1;
            int.TryParse(_ruleValBoxes[idx].Text.Trim(), out av);
            _rules[idx].ActionVal = av;
        }

        private void OpenActiveDatasheetPdf()
        {
            string pdf = Path.Combine(StudioCatalog.DatasheetDir, _activeChip.DatasheetPdf);
            if (File.Exists(pdf)) Process.Start(new ProcessStartInfo(pdf) { UseShellExecute = true });
            else if (Directory.Exists(StudioCatalog.DatasheetDir)) Process.Start(new ProcessStartInfo(StudioCatalog.DatasheetDir) { UseShellExecute = true });
            else MessageBox.Show("Datasheet tidak ditemukan:\n" + pdf);
        }

        private void RefreshComPorts()
        {
            if (_cmbComPort == null) return;
            string prev = _cmbComPort.SelectedItem as string;
            _cmbComPort.Items.Clear();
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            foreach (string p in ports) _cmbComPort.Items.Add(p);
            if (!string.IsNullOrEmpty(prev) && _cmbComPort.Items.Contains(prev)) _cmbComPort.SelectedItem = prev;
            else if (_cmbComPort.Items.Count > 0) _cmbComPort.SelectedIndex = _cmbComPort.Items.Count - 1;
        }

        private void AppendLog(string line)
        {
            Dispatcher.BeginInvoke(new Action(delegate {
                if (_txtConsoleLog == null) return;
                if (_txtConsoleLog.Text.Length > 32000)
                    _txtConsoleLog.Text = _txtConsoleLog.Text.Substring(_txtConsoleLog.Text.Length - 16000);
                _txtConsoleLog.AppendText(line + Environment.NewLine);
                _txtConsoleLog.ScrollToEnd();
            }));
        }

        private bool HasUniversalBackend()
        {
            return File.Exists(Path.Combine(_studioRoot, @"universal_firmware\xio_universal\xio_universal.ino"));
        }

        private void RefreshBackendAvailability()
        {
            if (_lblBackendState == null) return;
            bool available = _buildMode == 0 && HasUniversalBackend();
            _btnBuildFirmware.IsEnabled = available;
            _btnBuildAndFlash.IsEnabled = available;
            _lblBackendState.Text = _buildMode == 1
                ? "Mode AI mengekspor konfigurasi saja, bukan firmware chat. Tidak ada panggilan API dari aplikasi ini."
                : available ? "Backend Universal lokal ditemukan; kompatibilitas dan lisensinya harus diperiksa sebelum build."
                : "Source firmware tidak disertakan. Editor, profil publik, dan ekspor header tetap tersedia; build/flash dinonaktifkan.";
        }

        private bool SynthesizeHeadersInternal()
        {
            var used = new HashSet<int>();
            for (int i = 0; i < _slots.Count; i++)
            {
                string err = StudioCatalog.ValidateSlot(_activeChip, _slots[i], used);
                if (err != null)
                {
                    MessageBox.Show(String.Format("Konflik pada Slot #{0}:\n{1}", i + 1, err),
                        "Pengaman Silikon", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            XioProjectFile meta = GatherProjectSnapshot();
            bool aiOn = _features.Exists(delegate(PuzzleFeatureItem f) { return f.Key == "ai_cloud" && f.Enabled && f.InActiveDeck; });
            string validation = _buildMode == 1 && aiOn ? CodeSynthesizer.ValidateAiDefaults(meta) : null;
            if (validation != null)
            {
                MessageBox.Show(validation, "Pengaturan AI", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (MessageBox.Show("Header lokal dapat berisi API key, sandi, endpoint, dan kode kustom tanpa enkripsi. Jangan unggah hasil ini ke GitHub. Lanjutkan ekspor pribadi?",
                "Ekspor Header Pribadi", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return false;
            try
            {
                string destination = HasUniversalBackend() && _buildMode == 0
                    ? Path.Combine(_studioRoot, @"universal_firmware\xio_universal") : Path.Combine(_studioRoot, "exports");
                Directory.CreateDirectory(destination);
                if (_buildMode == 0)
                {
                    File.WriteAllText(Path.Combine(destination, "xio_univ_profile.h"), CodeSynthesizer.GenerateUniversalProfileHeader(_activeChip, _features, _slots, _rules, meta), Encoding.UTF8);
                    File.WriteAllText(Path.Combine(destination, "xio_custom_user_modules.h"), CodeSynthesizer.GenerateCustomUserModulesHeader(_features), Encoding.UTF8);
                }
                else
                    File.WriteAllText(Path.Combine(destination, "xio_ai_defaults.h"), "#pragma once\n\n" + CodeSynthesizer.GenerateAiDefaultsHeader(meta, aiOn), Encoding.UTF8);
                AppendLog("[EKSPOR] Header pribadi tersimpan -> " + destination);
                return true;
            }
            catch (Exception)
            {
                MessageBox.Show("Header tidak dapat ditulis. Periksa pengaturan dan akses folder tujuan.", "Ekspor Gagal", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void SynthesizeHeadersOnly()
        {
            SelectPage(4);
            if (SynthesizeHeadersInternal())
                MessageBox.Show("Header C++ profil, pemetaan pin dinamis, dan modul kustom berhasil disintesis.", "Sintesis Selesai", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void StartCompileFirmware(bool flashAfter)
        {
            if (_buildMode != 0 || !HasUniversalBackend())
            {
                MessageBox.Show("Backend firmware yang kompatibel tidak tersedia. Gunakan ekspor header atau profil publik.", "Edisi Builder + AI", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (_busyTask) { MessageBox.Show("Proses lain sedang berjalan."); return; }
            SelectPage(4);
            StopLiveSerial();
            if (!SynthesizeHeadersInternal()) return;

            string cli = _txtCliPath.Text.Trim();
            string esptool = _txtEsptoolPath.Text.Trim();
            string outBin = _txtOutBinPath.Text.Trim();
            string comPort = _cmbComPort.SelectedItem as string;
            EspChipProfile chip = _activeChip;

            if (flashAfter && string.IsNullOrEmpty(comPort))
            {
                MessageBox.Show("Pilih Port USB terlebih dahulu.", "Port Kosong", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _busyTask = true;
            AppendLog("──────────────────────────────────────────────────────────────────");
            AppendLog(String.Format("[BUILD] Kompilasi dimulai untuk {0} ({1})...", chip.Name, chip.Fqbn));

            ThreadPool.QueueUserWorkItem(delegate {
                try
                {
                    string sketchDir = Path.Combine(_studioRoot, @"universal_firmware\xio_universal");
                    string sketchName = "xio_universal.ino";
                    string buildCache = Path.Combine(_studioRoot, "build_cache", "univ_" + chip.Id);
                    Directory.CreateDirectory(buildCache);
                    Directory.CreateDirectory(Path.GetDirectoryName(outBin));

                    string fqbn = chip.Fqbn;
                    if (chip.Id == "esp32" || chip.Id == "esp32wrover" || chip.Id == "esp32s2" || chip.Id == "esp32s3" || chip.Id == "esp32c3")
                        fqbn += (fqbn.Contains(":") && fqbn.Split(':').Length > 3) ? ",PartitionScheme=min_spiffs" : ":PartitionScheme=min_spiffs";

                    string args = String.Format("compile --fqbn \"{0}\" --build-path \"{1}\" \"{2}\"", fqbn, buildCache, sketchDir);

                    int code = UsbEspTools.RunExternalProcess(cli, args, sketchDir, AppendLog);
                    if (code != 0) { AppendLog("[ERROR] Kompilasi gagal (" + code + ")."); return; }

                    string bootBin = Path.Combine(buildCache, sketchName + ".bootloader.bin");
                    string partBin = Path.Combine(buildCache, sketchName + ".partitions.bin");
                    string appBin  = Path.Combine(buildCache, sketchName + ".bin");
                    string bootApp0 = UsbEspTools.FindBootApp0();

                    if (File.Exists(bootBin) && File.Exists(partBin) && File.Exists(appBin) && File.Exists(esptool))
                    {
                        string mergeArgs = String.Format(
                            "--chip {0} merge-bin -o \"{1}\" --flash-mode dio --flash-size 4MB {2} \"{3}\" 0x8000 \"{4}\" {5}0x10000 \"{6}\"",
                            chip.EsptoolChip, outBin, chip.BootloaderAddr, bootBin, partBin,
                            (!string.IsNullOrEmpty(bootApp0) && File.Exists(bootApp0)) ? ("0xe000 \"" + bootApp0 + "\" ") : "",
                            appBin);

                        int mCode = UsbEspTools.RunExternalProcess(esptool, mergeArgs, buildCache, AppendLog);
                        if (mCode != 0 || !File.Exists(outBin)) { AppendLog("[ERROR] Penggabungan image gagal; flash dibatalkan."); return; }
                        AppendLog(String.Format("[SUKSES] Image 0x0 siap ({0:N0} byte): {1}", new FileInfo(outBin).Length, outBin));
                    }
                    else { AppendLog("[ERROR] Artefak build atau esptool tidak lengkap; flash dibatalkan."); return; }

                    if (flashAfter && File.Exists(outBin))
                    {
                        AppendLog(String.Format("[FLASH] Menulis ke {0} pada offset 0x0...", comPort));
                        string fArgs = String.Format("--chip {0} --port {1} --baud 460800 --before default-reset --after hard-reset write-flash -z 0x0 \"{2}\"",
                            chip.EsptoolChip, comPort, outBin);
                        int fCode = UsbEspTools.RunExternalProcess(esptool, fArgs, buildCache, AppendLog);
                        AppendLog(fCode == 0 ? "[SUKSES] Flash USB selesai!" : ("[ERROR] Flash gagal: " + fCode));
                    }
                }
                catch (Exception ex) { AppendLog("[EXCEPTION] " + ex.Message); }
                finally { _busyTask = false; }
            });
        }

        private void TriggerLiveUsbSync()
        {
            SelectPage(4);
            StopLiveSerial();
            RefreshComPorts();
            string comPort = _cmbComPort.SelectedItem as string;
            if (string.IsNullOrEmpty(comPort))
            {
                MessageBox.Show("Hubungkan ESP ke USB lalu klik 'Segarkan'.", "Port USB Belum Dipilih", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var used = new HashSet<int>();
            for (int i = 0; i < _slots.Count; i++)
            {
                string err = StudioCatalog.ValidateSlot(_activeChip, _slots[i], used);
                if (err != null) { MessageBox.Show(err, "Konflik Pin", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            }

            string pinsBody = CodeSynthesizer.BuildUsbSlotsFormPayload(_slots, _buildMode == 0);
            string rulesBody = CodeSynthesizer.BuildUsbRulesFormPayload(_rules);
            bool univ = (_buildMode == 0);

            AppendLog("[USB SYNC] Mengirim pemetaan pin & aturan ke " + comPort + "...");
            ThreadPool.QueueUserWorkItem(delegate {
                bool ok1;
                string r1 = UsbEspTools.SendUsbXioRequest(comPort, 115200, "POST", "/api/pins/save", pinsBody, out ok1);
                AppendLog("[USB PIN] " + r1);
                if (univ && ok1)
                {
                    bool ok2;
                    string r2 = UsbEspTools.SendUsbXioRequest(comPort, 115200, "POST", "/api/rules/save", rulesBody, out ok2);
                    AppendLog("[USB RULES] " + r2);
                }
            });
        }

        private void RunUsbChipDetect()
        {
            if (_busyTask) return;
            StopLiveSerial();
            string comPort = _cmbComPort.SelectedItem as string;
            string esptool = _txtEsptoolPath.Text.Trim();
            if (string.IsNullOrEmpty(comPort)) { MessageBox.Show("Pilih Port USB terlebih dahulu."); return; }

            _busyTask = true;
            AppendLog("[ESPCONNECT] Memindai chip di " + comPort + "...");
            ThreadPool.QueueUserWorkItem(delegate {
                try
                {
                    UsbChipProbeResult res = UsbEspTools.ProbeChipViaUsb(esptool, comPort, AppendLog);
                    Dispatcher.BeginInvoke(new Action(delegate {
                        _lblProbeChip.Text = res.RawChipLine;
                        _lblProbeFeat.Text = res.Features;
                        _lblProbeMac.Text = res.Mac;
                        _lblProbeFlash.Text = res.FlashSize;
                        if (res.Ok && !string.IsNullOrEmpty(res.MatchedProfileId))
                        {
                            for (int i = 0; i < _chips.Count; i++)
                                if (_chips[i].Id == res.MatchedProfileId) { _cmbChip.SelectedIndex = i; break; }
                        }
                    }));
                }
                finally { _busyTask = false; }
            });
        }

        private void RunUsbReadPartitions()
        {
            if (_busyTask) return;
            StopLiveSerial();
            string comPort = _cmbComPort.SelectedItem as string;
            string esptool = _txtEsptoolPath.Text.Trim();
            if (string.IsNullOrEmpty(comPort)) { MessageBox.Show("Pilih Port USB terlebih dahulu."); return; }

            _busyTask = true;
            AppendLog("[ESPCONNECT] Membaca tabel partisi 0x8000...");
            ThreadPool.QueueUserWorkItem(delegate {
                try
                {
                    string tmpDir = Path.Combine(_studioRoot, "build_cache");
                    List<PartitionEntryInfo> parts = UsbEspTools.ReadPartitionTableViaUsb(esptool, comPort, tmpDir, AppendLog);
                    Dispatcher.BeginInvoke(new Action(delegate {
                        _partTableRows.Children.Clear();
                        if (parts.Count == 0)
                        {
                            _partTableRows.Children.Add(new TextBlock { Text = "Tidak dapat membaca tabel partisi.", Foreground = BrushHex("#FF3B30") });
                            return;
                        }
                        foreach (PartitionEntryInfo p in parts)
                        {
                            var g = new Grid { Margin = new Thickness(0, 4, 0, 4) };
                            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(105) });
                            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
                            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                            AddPartCell(g, 0, p.Name, true);
                            AddPartCell(g, 1, p.TypeName + "/" + p.SubTypeName, false);
                            AddPartCell(g, 2, p.OffsetHex, false);
                            AddPartCell(g, 3, p.SizeText, false);
                            _partTableRows.Children.Add(g);
                            _partTableRows.Children.Add(MakeHairline());
                        }
                    }));
                }
                finally { _busyTask = false; }
            });
        }

        private void AddPartCell(Grid g, int col, string txt, bool bold)
        {
            var tb = new TextBlock { Text = txt, FontSize = 11.5, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal };
            tb.SetResourceReference(TextBlock.ForegroundProperty, bold ? "ApTextPri" : "ApTextSec");
            Grid.SetColumn(tb, col);
            g.Children.Add(tb);
        }

        private void RunUsbBackupFlash()
        {
            if (_busyTask) return;
            StopLiveSerial();
            string comPort = _cmbComPort.SelectedItem as string;
            string esptool = _txtEsptoolPath.Text.Trim();
            if (string.IsNullOrEmpty(comPort)) { MessageBox.Show("Pilih Port USB terlebih dahulu."); return; }

            var dlg = new SaveFileDialog { Filter = "ESP Flash Backup (*.bin)|*.bin", FileName = String.Format("Backup_{0}_{1:yyyyMMdd_HHmm}.bin", _activeChip.Id, DateTime.Now) };
            if (dlg.ShowDialog() != true) return;
            string savePath = dlg.FileName;

            _busyTask = true;
            AppendLog("[BACKUP] Menyalin 4MB Flash ke " + savePath + "...");
            ThreadPool.QueueUserWorkItem(delegate {
                try
                {
                    int code = UsbEspTools.RunExternalProcess(esptool, String.Format("--port {0} --baud 921600 read-flash 0x0 0x400000 \"{1}\"", comPort, savePath), null, AppendLog);
                    AppendLog(code == 0 ? ("[SUKSES] Backup selesai: " + savePath) : "[ERROR] Backup gagal.");
                }
                finally { _busyTask = false; }
            });
        }

        private void RunFlashExistingBin()
        {
            if (_busyTask) return;
            StopLiveSerial();
            string comPort = _cmbComPort.SelectedItem as string;
            string esptool = _txtEsptoolPath.Text.Trim();
            if (string.IsNullOrEmpty(comPort)) { MessageBox.Show("Pilih Port USB terlebih dahulu."); return; }

            var dlg = new OpenFileDialog { Filter = "Firmware Binary (*.bin)|*.bin" };
            if (dlg.ShowDialog() != true) return;
            string binFile = dlg.FileName;
            string chipArg = _activeChip.EsptoolChip;

            _busyTask = true;
            AppendLog("[FLASH] Menulis " + binFile + " ke " + comPort + "...");
            ThreadPool.QueueUserWorkItem(delegate {
                try
                {
                    int code = UsbEspTools.RunExternalProcess(esptool, String.Format("--chip {0} --port {1} --baud 460800 --before default-reset --after hard-reset write-flash -z 0x0 \"{2}\"", chipArg, comPort, binFile), null, AppendLog);
                    AppendLog(code == 0 ? "[SUKSES] Flash selesai!" : "[ERROR] Flash gagal.");
                }
                finally { _busyTask = false; }
            });
        }

        private void ToggleLiveSerialMonitor()
        {
            if (_serialRunning) { StopLiveSerial(); return; }
            string comPort = _cmbComPort.SelectedItem as string;
            if (string.IsNullOrEmpty(comPort)) { MessageBox.Show("Pilih Port USB terlebih dahulu."); return; }
            int baud = 115200;
            int.TryParse(_cmbBaud.SelectedItem as string, out baud);

            try
            {
                _liveSerial = new SerialPort(comPort, baud > 0 ? baud : 115200, Parity.None, 8, StopBits.One);
                _liveSerial.ReadTimeout = 300;
                _liveSerial.Open();
                _serialRunning = true;
                _btnSerialMon.Content = "Tutup Serial Monitor";
                AppendLog(String.Format("[SERIAL] Terhubung ke {0} @ {1} bps", comPort, baud));

                _serialReaderThread = new Thread(delegate() {
                    while (_serialRunning && _liveSerial != null && _liveSerial.IsOpen)
                    {
                        try
                        {
                            string ln = _liveSerial.ReadLine();
                            if (ln != null) AppendLog("[ESP] " + ln.TrimEnd('\r'));
                        }
                        catch (TimeoutException) { }
                        catch { break; }
                    }
                });
                _serialReaderThread.IsBackground = true;
                _serialReaderThread.Start();
            }
            catch (Exception ex) { AppendLog("[SERIAL ERR] " + ex.Message); }
        }

        private void StopLiveSerial()
        {
            _serialRunning = false;
            try
            {
                if (_liveSerial != null)
                {
                    if (_liveSerial.IsOpen) _liveSerial.Close();
                    _liveSerial.Dispose();
                    _liveSerial = null;
                }
            }
            catch { }
            if (_btnSerialMon != null) _btnSerialMon.Content = "Buka Serial Monitor";
        }

        private void SendSerialLineFromBox()
        {
            string cmd = _txtSerialInput.Text.Trim();
            if (string.IsNullOrEmpty(cmd)) return;
            if (_serialRunning && _liveSerial != null && _liveSerial.IsOpen)
            {
                try { _liveSerial.WriteLine(cmd); AppendLog(">> " + cmd); }
                catch (Exception ex) { AppendLog("[SERIAL ERR] " + ex.Message); }
            }
            else AppendLog("[INFO] Aktifkan 'Buka Serial Monitor' terlebih dahulu.");
        }

        // ====================================================================
        // APPLE HIG COMPONENT HELPERS
        // ====================================================================
        private Border MakeAppleCard(Thickness padding, Thickness margin)
        {
            var b = new Border
            {
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                Padding = padding,
                Margin = margin
            };
            b.SetResourceReference(Border.BackgroundProperty, "ApCardBg");
            b.SetResourceReference(Border.BorderBrushProperty, "ApHairline");
            return b;
        }

        private Border MakeHairline()
        {
            var h = new Border { Height = 1 };
            h.SetResourceReference(Border.BackgroundProperty, "ApHairline");
            return h;
        }

        private TextBlock MakeSectionCaption(string text)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(4, 2, 0, 6)
            };
            tb.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            return tb;
        }

        private TextBlock MakeSmallLabel(string text)
        {
            var tb = new TextBlock { Text = text, FontSize = 11.5, Margin = new Thickness(0, 0, 0, 4) };
            tb.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            return tb;
        }

        private Button MakeStyledButton(string text, string styleKey, Thickness pad)
        {
            var b = new Button
            {
                Content = text,
                Padding = pad
            };
            b.SetResourceReference(FrameworkElement.StyleProperty, styleKey);
            return b;
        }

        private ComboBox MakeAppleCombo(double width)
        {
            var c = new ComboBox();
            c.SetResourceReference(FrameworkElement.StyleProperty, "ApComboBox");
            if (!double.IsNaN(width)) c.Width = width;
            return c;
        }

        private TextBox MakeAppleTextBox(string text, double width)
        {
            var tb = new TextBox { Text = text };
            tb.SetResourceReference(FrameworkElement.StyleProperty, "ApTextBox");
            if (!double.IsNaN(width)) tb.Width = width;
            return tb;
        }

        private Border MakeTagPill(string text, string hexColor)
        {
            return new Border
            {
                Background = BrushHex(hexColor),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 1, 6, 1),
                Margin = new Thickness(0, 0, 6, 0),
                Child = new TextBlock { Text = text, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White }
            };
        }

        private UIElement MakeLegendDot(string label, string hex)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 10, 4), VerticalAlignment = VerticalAlignment.Center };
            sp.Children.Add(new Border { Width = 8, Height = 8, CornerRadius = new CornerRadius(4), Background = BrushHex(hex), Margin = new Thickness(0, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center });
            var tb = new TextBlock { Text = label, FontSize = 11 };
            tb.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            sp.Children.Add(tb);
            return sp;
        }

        private void AddMatrixHeader(Grid g, int col, string text)
        {
            var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold };
            tb.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            Grid.SetColumn(tb, col);
            g.Children.Add(tb);
        }

        private PasswordBox AddApplePasswordRow(StackPanel parent, string label, string value, bool addDivider)
        {
            var row = new Grid { Margin = new Thickness(0, 7, 0, 7) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(155) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var caption = MakeSmallLabel(label);
            caption.VerticalAlignment = VerticalAlignment.Center;
            row.Children.Add(caption);
            var input = new PasswordBox { Password = value, Height = 30, Padding = new Thickness(9, 4, 9, 4), FontSize = 12.5 };
            input.SetResourceReference(BackgroundProperty, "ApInputBg");
            input.SetResourceReference(ForegroundProperty, "ApTextPri");
            input.SetResourceReference(BorderBrushProperty, "ApHairline");
            System.Windows.Automation.AutomationProperties.SetName(input, label);
            Grid.SetColumn(input, 1);
            row.Children.Add(input);
            parent.Children.Add(row);
            if (addDivider) parent.Children.Add(MakeHairline());
            return input;
        }

        private TextBox AddAppleFormRow(StackPanel parent, string label, string val, bool addDivider)
        {
            var row = new Grid { Margin = new Thickness(0, 7, 0, 7) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(155) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lbl = new TextBlock { Text = label, FontSize = 12.5, VerticalAlignment = VerticalAlignment.Center };
            lbl.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
            Grid.SetColumn(lbl, 0);
            row.Children.Add(lbl);

            var tb = MakeAppleTextBox(val, double.NaN);
            Grid.SetColumn(tb, 1);
            row.Children.Add(tb);

            parent.Children.Add(row);
            if (addDivider) parent.Children.Add(MakeHairline());
            return tb;
        }
    }
}
