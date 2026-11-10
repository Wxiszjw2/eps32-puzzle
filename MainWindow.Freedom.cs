using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace XioPuzzleStudio
{
    public partial class MainWindow : Window
    {
        // Page 0: Puzzle Freedom & Custom C++ Module Controls
        private StackPanel _featureSectionsPanel;
        private WrapPanel _catFilterPanel;
        private TextBox _txtSearch;
        private Button _btnToggleGudang;
        private Border _gudangDrawerCard;
        private StackPanel _gudangRowsPanel;
        private Border _customCppEditorCard;
        private PuzzleFeatureItem _editingCustomModule = null;
        private TextBox _txtCustTitle, _txtCustFlashKb, _txtCustLoopMs, _txtCustJsonExpr;
        private ComboBox _cmbCustCategory;
        private TextBox _txtCustGlobal, _txtCustSetup, _txtCustLoop;

        // Page 3: Companion VPS Docker & PC Tools Puzzle Controls
        private StackPanel _companionSectionsPanel;
        private Button _btnToggleCompGudang;
        private Border _compGudangCard;
        private StackPanel _compGudangRowsPanel;
        private Border _customDockerEditorCard;
        private CompanionPieceItem _editingCustomComp = null;
        private TextBox _txtCompTitle, _txtCompPort, _txtCompDesc, _txtCompYaml;
        private TextBlock _lblCompOutSummary;

        // ====================================================================
        // TAB 0: SUSUN PUZZLE MODUL ESP (TAMBAH / KURANGI / MODUL C++ KUSTOM)
        // ====================================================================
        private UIElement BuildPageCatalog()
        {
            var sp = new StackPanel();

            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) });

            var titleStack = new StackPanel();
            var h1 = new TextBlock { Text = "Susun Puzzle Modul ESP", FontSize = 22, FontWeight = FontWeights.Bold };
            h1.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
            var sub = new TextBlock {
                Text = "Kebebasan 100%: nyalakan, matikan, kurangi modul dari rakitan ke Gudang, kembalikan kapan saja, atau suntikkan modul C++ buatan Anda sendiri.",
                FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 14, 0)
            };
            sub.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            titleStack.Children.Add(h1);
            titleStack.Children.Add(sub);
            Grid.SetColumn(titleStack, 0);
            headerGrid.Children.Add(titleStack);

            var searchStack = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom };
            searchStack.Children.Add(MakeSmallLabel("Cari Modul / Sensor / Protokol"));
            _txtSearch = MakeAppleTextBox("", double.NaN);
            _txtSearch.TextChanged += delegate {
                _searchQuery = _txtSearch.Text.Trim();
                RebuildCatalogCards();
            };
            searchStack.Children.Add(_txtSearch);
            Grid.SetColumn(searchStack, 1);
            headerGrid.Children.Add(searchStack);
            sp.Children.Add(headerGrid);

            // Freedom Action Bar Card
            var actCard = MakeAppleCard(new Thickness(12, 10, 12, 10), new Thickness(0, 0, 0, 12));
            var actDock = new DockPanel();

            var rightBtns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnClearDeck = MakeStyledButton("Kosongkan Rakitan", "ApButton", new Thickness(11, 6, 11, 6));
            btnClearDeck.Margin = new Thickness(0, 0, 6, 0);
            btnClearDeck.Click += delegate {
                foreach (PuzzleFeatureItem f in _features)
                {
                    if (!f.IsCustomUserModule) f.InActiveDeck = false;
                    else f.Enabled = false;
                }
                _gudangDrawerCard.Visibility = Visibility.Visible;
                RebuildCatalogCards();
                RefreshResourceGauges();
            };
            rightBtns.Children.Add(btnClearDeck);

            var btnRestoreAll = MakeStyledButton("Pulihkan Semua", "ApButton", new Thickness(11, 6, 11, 6));
            btnRestoreAll.Click += delegate {
                foreach (PuzzleFeatureItem f in _features) { f.InActiveDeck = true; f.Enabled = true; }
                RebuildCatalogCards();
                RefreshResourceGauges();
            };
            rightBtns.Children.Add(btnRestoreAll);
            DockPanel.SetDock(rightBtns, Dock.Right);
            actDock.Children.Add(rightBtns);

            var leftBtns = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
            _btnToggleGudang = MakeStyledButton("+ Gudang Modul (0 tersedia)", "ApButton", new Thickness(12, 6, 12, 6));
            _btnToggleGudang.Margin = new Thickness(0, 0, 8, 0);
            _btnToggleGudang.Click += delegate {
                _gudangDrawerCard.Visibility = (_gudangDrawerCard.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
                RebuildGudangDrawer();
            };
            leftBtns.Children.Add(_btnToggleGudang);

            var btnNewCustom = MakeStyledButton("+ Buat Modul C++ Kustom", "ApPrimaryButton", new Thickness(13, 6, 13, 6));
            btnNewCustom.Click += delegate { OpenCustomCppModuleEditor(null); };
            leftBtns.Children.Add(btnNewCustom);
            actDock.Children.Add(leftBtns);

            actCard.Child = actDock;
            sp.Children.Add(actCard);

            // Collapsible Drawer 1: Gudang Modul Tersedia (Removed Built-In Modules)
            _gudangDrawerCard = MakeAppleCard(new Thickness(14), new Thickness(0, 0, 0, 14));
            _gudangDrawerCard.Visibility = Visibility.Collapsed;
            var gSp = new StackPanel();
            var gHdr = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            var btnCloseGudang = MakeStyledButton("Tutup Gudang", "ApButton", new Thickness(10, 4, 10, 4));
            DockPanel.SetDock(btnCloseGudang, Dock.Right);
            btnCloseGudang.Click += delegate { _gudangDrawerCard.Visibility = Visibility.Collapsed; };
            gHdr.Children.Add(btnCloseGudang);

            var gTitle = new TextBlock { Text = "Gudang Katalog Modul (Klik '+ Tambah' untuk memasukkan kembali ke rakitan)", FontSize = 13.5, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
            gTitle.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
            gHdr.Children.Add(gTitle);
            gSp.Children.Add(gHdr);
            gSp.Children.Add(MakeHairline());

            _gudangRowsPanel = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
            gSp.Children.Add(_gudangRowsPanel);
            _gudangDrawerCard.Child = gSp;
            sp.Children.Add(_gudangDrawerCard);

            // Collapsible Drawer 2: Inline Custom C++ Module Creator / Editor
            _customCppEditorCard = BuildCustomCppEditorCard();
            _customCppEditorCard.Visibility = Visibility.Collapsed;
            sp.Children.Add(_customCppEditorCard);

            // Category Filter Pills
            _catFilterPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 14) };
            string[] cats = new string[] {
                "Semua", "Modul Kustom User", "Inti & Optimasi", "Sensor & Lingkungan",
                "Industri & Robotik", "Spektrum RF Pasif", "GNSS, SD & Modem", "Cloud, AI & Keamanan"
            };
            foreach (string c in cats)
            {
                string catName = c;
                var b = MakeStyledButton(catName, catName == _activeCategory ? "ApPrimaryButton" : "ApButton", new Thickness(11, 5, 11, 5));
                b.Margin = new Thickness(0, 0, 6, 6);
                b.Tag = catName;
                b.Click += delegate {
                    _activeCategory = catName;
                    foreach (UIElement el in _catFilterPanel.Children)
                    {
                        var btn = el as Button;
                        if (btn != null)
                            btn.Style = ResStyle((string)btn.Tag == _activeCategory ? "ApPrimaryButton" : "ApButton");
                    }
                    RebuildCatalogCards();
                };
                _catFilterPanel.Children.Add(b);
            }
            sp.Children.Add(_catFilterPanel);

            _featureSectionsPanel = new StackPanel();
            sp.Children.Add(_featureSectionsPanel);

            return sp;
        }

        private Border BuildCustomCppEditorCard()
        {
            var card = MakeAppleCard(new Thickness(16), new Thickness(0, 0, 0, 16));
            var sp = new StackPanel();

            var hdr = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
            var btnCancel = MakeStyledButton("Tutup", "ApButton", new Thickness(10, 4, 10, 4));
            DockPanel.SetDock(btnCancel, Dock.Right);
            btnCancel.Click += delegate { _customCppEditorCard.Visibility = Visibility.Collapsed; };
            hdr.Children.Add(btnCancel);

            var title = new TextBlock { Text = "Studio Pembuat Modul C++ Kustom (Otomatis Masuk ke Firmware & JSON Telemetri)", FontSize = 14, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center };
            title.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
            hdr.Children.Add(title);
            sp.Children.Add(hdr);
            sp.Children.Add(MakeHairline());

            var topRow = new Grid { Margin = new Thickness(0, 10, 0, 8) };
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(175) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });

            var c0 = new StackPanel();
            c0.Children.Add(MakeSmallLabel("Nama Modul Puzzle Anda"));
            _txtCustTitle = MakeAppleTextBox("Sensor / Logika Kustom #1", double.NaN);
            c0.Children.Add(_txtCustTitle);
            Grid.SetColumn(c0, 0);
            topRow.Children.Add(c0);

            var c1 = new StackPanel();
            c1.Children.Add(MakeSmallLabel("Kategori Rakitan"));
            _cmbCustCategory = MakeAppleCombo(double.NaN);
            _cmbCustCategory.Items.Add("Modul Kustom User");
            _cmbCustCategory.Items.Add("Sensor & Lingkungan");
            _cmbCustCategory.Items.Add("Industri & Robotik");
            _cmbCustCategory.Items.Add("Spektrum RF Pasif");
            _cmbCustCategory.Items.Add("Cloud, AI & Keamanan");
            _cmbCustCategory.SelectedIndex = 0;
            c1.Children.Add(_cmbCustCategory);
            Grid.SetColumn(c1, 2);
            topRow.Children.Add(c1);

            var c2 = new StackPanel();
            c2.Children.Add(MakeSmallLabel("Estimasi KB"));
            _txtCustFlashKb = MakeAppleTextBox("4", double.NaN);
            c2.Children.Add(_txtCustFlashKb);
            Grid.SetColumn(c2, 4);
            topRow.Children.Add(c2);

            var c3 = new StackPanel();
            c3.Children.Add(MakeSmallLabel("Interval Loop (ms)"));
            _txtCustLoopMs = MakeAppleTextBox("1000", double.NaN);
            c3.Children.Add(_txtCustLoopMs);
            Grid.SetColumn(c3, 6);
            topRow.Children.Add(c3);
            sp.Children.Add(topRow);

            var codeGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            codeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            codeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            codeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            codeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            codeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var gBox = new StackPanel();
            gBox.Children.Add(MakeSmallLabel("Deklarasi Global / #include"));
            _txtCustGlobal = MakeCodeEditorBox("static float g_nilai_user = 25.0f;", 68);
            gBox.Children.Add(_txtCustGlobal);
            Grid.SetColumn(gBox, 0);
            codeGrid.Children.Add(gBox);

            var sBox = new StackPanel();
            sBox.Children.Add(MakeSmallLabel("Kode setup() (Jalan 1x Saat Boot)"));
            _txtCustSetup = MakeCodeEditorBox("// Contoh: pinMode(4, INPUT);", 68);
            sBox.Children.Add(_txtCustSetup);
            Grid.SetColumn(sBox, 2);
            codeGrid.Children.Add(sBox);

            var lBox = new StackPanel();
            lBox.Children.Add(MakeSmallLabel("Kode loop() (Jalan Tiap Interval ms)"));
            _txtCustLoop = MakeCodeEditorBox("g_nilai_user += 0.25f;", 68);
            lBox.Children.Add(_txtCustLoop);
            Grid.SetColumn(lBox, 4);
            codeGrid.Children.Add(lBox);
            sp.Children.Add(codeGrid);

            var botBar = new Grid();
            botBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            botBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            botBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var jSp = new StackPanel();
            jSp.Children.Add(MakeSmallLabel("Ekspresi Nilai JSON Telemetri (Otomatis masuk ke /api/status, MQTT & Serial)"));
            _txtCustJsonExpr = MakeAppleTextBox("g_nilai_user", double.NaN);
            jSp.Children.Add(_txtCustJsonExpr);
            Grid.SetColumn(jSp, 0);
            botBar.Children.Add(jSp);

            var btnSaveCust = MakeStyledButton("Simpan Modul ke Rakitan Puzzle", "ApPrimaryButton", new Thickness(16, 8, 16, 8));
            btnSaveCust.VerticalAlignment = VerticalAlignment.Bottom;
            btnSaveCust.Click += delegate { SaveCustomCppModuleFromEditor(); };
            Grid.SetColumn(btnSaveCust, 2);
            botBar.Children.Add(btnSaveCust);
            sp.Children.Add(botBar);

            card.Child = sp;
            return card;
        }

        private TextBox MakeCodeEditorBox(string initialText, double height)
        {
            var tb = MakeAppleTextBox(initialText, double.NaN);
            tb.Height = height;
            tb.AcceptsReturn = true;
            tb.TextWrapping = TextWrapping.Wrap;
            tb.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            tb.VerticalContentAlignment = VerticalAlignment.Top;
            tb.FontFamily = new System.Windows.Media.FontFamily("Consolas");
            tb.FontSize = 11.5;
            return tb;
        }

        private void OpenCustomCppModuleEditor(PuzzleFeatureItem existing)
        {
            _editingCustomModule = existing;
            if (existing != null)
            {
                _txtCustTitle.Text = existing.Title;
                _txtCustFlashKb.Text = existing.SavedFlashKb.ToString();
                _txtCustLoopMs.Text = existing.LoopIntervalMs.ToString();
                _txtCustGlobal.Text = existing.CustomGlobalCode ?? "";
                _txtCustSetup.Text = existing.CustomSetupCode ?? "";
                _txtCustLoop.Text = existing.CustomLoopCode ?? "";
                _txtCustJsonExpr.Text = existing.CustomJsonExpr ?? "";
                for (int i = 0; i < _cmbCustCategory.Items.Count; i++)
                    if ((string)_cmbCustCategory.Items[i] == existing.Category) { _cmbCustCategory.SelectedIndex = i; break; }
            }
            else
            {
                int n = 1;
                foreach (PuzzleFeatureItem f in _features) if (f.IsCustomUserModule) n++;
                _txtCustTitle.Text = "Modul C++ Kustom #" + n;
                _txtCustFlashKb.Text = "4";
                _txtCustLoopMs.Text = "1000";
                _txtCustGlobal.Text = String.Format("static float g_kustom_{0} = 10.0f;", n);
                _txtCustSetup.Text = "// Inisialisasi pin / library Anda di sini";
                _txtCustLoop.Text = String.Format("g_kustom_{0} += 0.5f;", n);
                _txtCustJsonExpr.Text = String.Format("g_kustom_{0}", n);
                _cmbCustCategory.SelectedIndex = 0;
            }
            _customCppEditorCard.Visibility = Visibility.Visible;
        }

        private void SaveCustomCppModuleFromEditor()
        {
            string title = _txtCustTitle.Text.Trim();
            if (string.IsNullOrEmpty(title)) title = "Modul Kustom User";
            int kb = 4, loopMs = 1000;
            int.TryParse(_txtCustFlashKb.Text.Trim(), out kb);
            int.TryParse(_txtCustLoopMs.Text.Trim(), out loopMs);

            PuzzleFeatureItem target = _editingCustomModule;
            if (target == null)
            {
                target = new PuzzleFeatureItem
                {
                    Key = "cust_" + DateTime.Now.Ticks.ToString("x"),
                    IsCustomUserModule = true,
                    UnivMacro = "XIO_USER_CUSTOM",
                    MarauderMacro = "",
                    SourceOrigin = "Kode C++ Buatan User (xio_custom_user_modules.h)",
                    SavedRamKb = 1,
                    Enabled = true,
                    InActiveDeck = true
                };
                _features.Insert(0, target);
            }

            target.Title = title;
            target.Category = (_cmbCustCategory.SelectedItem as string) ?? "Modul Kustom User";
            target.SavedFlashKb = Math.Max(1, kb);
            target.LoopIntervalMs = Math.Max(0, loopMs);
            target.CustomGlobalCode = _txtCustGlobal.Text;
            target.CustomSetupCode = _txtCustSetup.Text;
            target.CustomLoopCode = _txtCustLoop.Text;
            target.CustomJsonExpr = _txtCustJsonExpr.Text.Trim();
            target.Description = String.Format("Modul C++ kustom (interval loop: {0} ms{1}).",
                target.LoopIntervalMs,
                !string.IsNullOrEmpty(target.CustomJsonExpr) ? ", JSON: " + target.CustomJsonExpr : "");

            _editingCustomModule = null;
            _customCppEditorCard.Visibility = Visibility.Collapsed;
            RebuildCatalogCards();
            RefreshResourceGauges();
        }

        private void RebuildGudangDrawer()
        {
            int removedCount = 0;
            foreach (PuzzleFeatureItem f in _features)
            {
                if (!f.InActiveDeck && !f.IsCustomUserModule)
                {
                    if (_buildMode == 0 && string.IsNullOrEmpty(f.UnivMacro)) continue;
                    removedCount++;
                }
            }
            if (_btnToggleGudang != null)
                _btnToggleGudang.Content = String.Format("+ Gudang Modul ({0} tersedia)", removedCount);

            if (_gudangRowsPanel == null) return;
            _gudangRowsPanel.Children.Clear();

            if (removedCount == 0)
            {
                var okTxt = new TextBlock {
                    Text = "Seluruh modul katalog saat ini sudah berada di dalam rakitan aktif Anda. Gunakan tombol 'Kurangi' pada baris modul mana saja jika ingin memindahkannya ke Gudang ini.",
                    FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 4)
                };
                okTxt.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
                _gudangRowsPanel.Children.Add(okTxt);
                return;
            }

            foreach (PuzzleFeatureItem item in _features)
            {
                if (item.InActiveDeck || item.IsCustomUserModule) continue;
                if (_buildMode == 0 && string.IsNullOrEmpty(item.UnivMacro)) continue;

                PuzzleFeatureItem f = item;
                var row = new DockPanel { Margin = new Thickness(0, 5, 0, 5) };
                var bAdd = MakeStyledButton("+ Tambah ke Rakitan", "ApPrimaryButton", new Thickness(11, 4, 11, 4));
                DockPanel.SetDock(bAdd, Dock.Right);
                bAdd.Click += delegate {
                    f.InActiveDeck = true;
                    f.Enabled = true;
                    RebuildCatalogCards();
                    RefreshResourceGauges();
                };
                row.Children.Add(bAdd);

                var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
                var t = new TextBlock { Text = f.Title + "  (" + f.Category + ")", FontSize = 12.5, FontWeight = FontWeights.SemiBold };
                t.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
                info.Children.Add(t);
                row.Children.Add(info);

                _gudangRowsPanel.Children.Add(row);
            }
        }

        private void RebuildCatalogCards()
        {
            RefreshAiDefaultsUi();
            if (_featureSectionsPanel == null) return;
            _featureSectionsPanel.Children.Clear();
            RebuildGudangDrawer();

            string[] orderedCats = new string[] {
                "Modul Kustom User", "Inti & Optimasi", "Sensor & Lingkungan", "Industri & Robotik",
                "Spektrum RF Pasif", "GNSS, SD & Modem", "Cloud, AI & Keamanan"
            };

            int totalRendered = 0;
            foreach (string cat in orderedCats)
            {
                if (_activeCategory != "Semua" && _activeCategory != cat) continue;

                var matching = new List<PuzzleFeatureItem>();
                foreach (PuzzleFeatureItem item in _features)
                {
                    if (!item.InActiveDeck) continue;
                    if (item.Category != cat) continue;
                    if (_buildMode == 0 && !item.IsCustomUserModule && string.IsNullOrEmpty(item.UnivMacro)) continue;
                    if (!string.IsNullOrEmpty(_searchQuery))
                    {
                        string hay = (item.Title + " " + item.Description + " " + item.SourceOrigin).ToLowerInvariant();
                        if (!hay.Contains(_searchQuery.ToLowerInvariant())) continue;
                    }
                    matching.Add(item);
                }
                if (matching.Count == 0) continue;
                totalRendered += matching.Count;

                _featureSectionsPanel.Children.Add(MakeSectionCaption(cat.ToUpperInvariant()));
                var groupCard = MakeAppleCard(new Thickness(0), new Thickness(0, 0, 0, 16));
                var groupRows = new StackPanel();

                for (int i = 0; i < matching.Count; i++)
                {
                    PuzzleFeatureItem f = matching[i];
                    bool supported = true;
                    string warn = "";
                    if (f.RequiresWifi && !_activeChip.HasWifi) { supported = false; warn = "Chip tanpa Wi-Fi"; }
                    else if (f.RequiresBle && !_activeChip.HasBle) { supported = false; warn = "Chip tanpa BLE"; }

                    var rowGrid = new Grid { Margin = new Thickness(16, 11, 16, 11) };
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(136) });
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54) });

                    var txtSp = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
                    var titleRow = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
                    var tTitle = new TextBlock { Text = f.Title, FontSize = 13.5, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 8, 0) };
                    tTitle.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
                    titleRow.Children.Add(tTitle);

                    if (f.IsCustomUserModule)
                        titleRow.Children.Add(MakeTagPill("Kustom C++", "#AF52DE"));
                    else if (f.Recommended)
                        titleRow.Children.Add(MakeTagPill("Disarankan", "#007AFF"));
                    if (!string.IsNullOrEmpty(warn))
                        titleRow.Children.Add(MakeTagPill(warn, "#FF9500"));

                    txtSp.Children.Add(titleRow);

                    var tDesc = new TextBlock { Text = f.Description, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 3) };
                    tDesc.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
                    txtSp.Children.Add(tDesc);

                    var tSrc = new TextBlock { Text = "Sumber: " + f.SourceOrigin, FontSize = 11, Opacity = 0.80 };
                    tSrc.SetResourceReference(TextBlock.ForegroundProperty, "ApBlue");
                    txtSp.Children.Add(tSrc);

                    Grid.SetColumn(txtSp, 0);
                    rowGrid.Children.Add(txtSp);

                    var memBadge = new Border
                    {
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(8, 4, 8, 4),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 8, 0)
                    };
                    memBadge.SetResourceReference(Border.BackgroundProperty, "ApCardAltBg");
                    var memTxt = new TextBlock {
                        Text = String.Format("{0} KB • {1} KB RAM", f.SavedFlashKb, f.SavedRamKb),
                        FontSize = 11, FontWeight = FontWeights.Medium
                    };
                    memTxt.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
                    memBadge.Child = memTxt;
                    Grid.SetColumn(memBadge, 1);
                    rowGrid.Children.Add(memBadge);

                    // Action Buttons: Edit (if custom) + Kurangi / Hapus
                    var actRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                    if (f.IsCustomUserModule)
                    {
                        var bEdit = MakeStyledButton("Edit", "ApButton", new Thickness(9, 4, 9, 4));
                        bEdit.FontSize = 11;
                        bEdit.Margin = new Thickness(0, 0, 5, 0);
                        bEdit.Click += delegate { OpenCustomCppModuleEditor(f); };
                        actRow.Children.Add(bEdit);
                    }

                    var bRem = MakeStyledButton(f.IsCustomUserModule ? "Hapus" : "Kurangi", "ApButton", new Thickness(9, 4, 9, 4));
                    bRem.FontSize = 11;
                    bRem.ToolTip = f.IsCustomUserModule ? "Hapus modul kustom ini" : "Keluarkan dari rakitan aktif ke Gudang Modul";
                    bRem.Click += delegate {
                        if (f.IsCustomUserModule) _features.Remove(f);
                        else f.InActiveDeck = false;
                        RebuildCatalogCards();
                        RefreshResourceGauges();
                    };
                    actRow.Children.Add(bRem);
                    Grid.SetColumn(actRow, 2);
                    rowGrid.Children.Add(actRow);

                    // Unlocked Apple Toggle Switch on EVERY module!
                    var sw = new CheckBox
                    {
                        Style = ResStyle("ApToggleSwitch"),
                        IsChecked = f.Enabled && supported,
                        IsEnabled = supported,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    sw.Checked += delegate { f.Enabled = true; RefreshAiDefaultsUi(); RefreshResourceGauges(); };
                    sw.Unchecked += delegate { f.Enabled = false; RefreshAiDefaultsUi(); RefreshResourceGauges(); };
                    Grid.SetColumn(sw, 3);
                    rowGrid.Children.Add(sw);

                    groupRows.Children.Add(rowGrid);
                    if (i < matching.Count - 1)
                    {
                        var div = MakeHairline();
                        div.Margin = new Thickness(16, 0, 16, 0);
                        groupRows.Children.Add(div);
                    }
                }

                groupCard.Child = groupRows;
                _featureSectionsPanel.Children.Add(groupCard);
            }

            if (totalRendered == 0)
            {
                var emptyCard = MakeAppleCard(new Thickness(20), new Thickness(0));
                var eTxt = new TextBlock {
                    Text = "Rakitan puzzle sedang kosong untuk filter ini. Klik '+ Gudang Modul' untuk menambahkan modul bawaan atau klik '+ Buat Modul C++ Kustom' untuk menulis modul Anda sendiri.",
                    FontSize = 13, TextWrapping = TextWrapping.Wrap
                };
                eTxt.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
                emptyCard.Child = eTxt;
                _featureSectionsPanel.Children.Add(emptyCard);
            }
        }

        // ====================================================================
        // TAB 3: PUZZLE PENDAMPING SERVER VPS DOCKER & ALAT PC
        // ====================================================================
        private UIElement BuildPageCompanions()
        {
            var sp = new StackPanel();

            var h1 = new TextBlock { Text = "Puzzle Server VPS Docker & Alat PC Pendamping", FontSize = 22, FontWeight = FontWeights.Bold };
            h1.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
            var sub = new TextBlock {
                Text = "Rakit ekosistem pendamping di luar chip ESP (Mosquitto MQTT, InfluxDB v2, Grafana, Node-RED, WebSerial Browser, Jembatan Python @ROS2, hingga RTL-SDR). Bebas dikurangi atau ditambah kontainer Docker kustom!",
                FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 14)
            };
            sub.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
            sp.Children.Add(h1);
            sp.Children.Add(sub);

            var topBar = MakeAppleCard(new Thickness(12, 10, 12, 10), new Thickness(0, 0, 0, 14));
            var dock = new DockPanel();

            var rightGen = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnOpenDir = MakeStyledButton("Buka Folder Hasil", "ApButton", new Thickness(11, 6, 11, 6));
            btnOpenDir.Margin = new Thickness(0, 0, 8, 0);
            btnOpenDir.Click += delegate {
                string dir = Path.Combine(_studioRoot, "output_companion_stack");
                Directory.CreateDirectory(dir);
                Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
            };
            rightGen.Children.Add(btnOpenDir);

            var btnGenStack = MakeStyledButton("Rakit Paket VPS & PC Tool", "ApPrimaryButton", new Thickness(14, 6, 14, 6));
            btnGenStack.Click += delegate { SynthesizeCompanionStackNow(true); };
            rightGen.Children.Add(btnGenStack);
            DockPanel.SetDock(rightGen, Dock.Right);
            dock.Children.Add(rightGen);

            var leftCtrl = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
            _btnToggleCompGudang = MakeStyledButton("+ Gudang Pendamping (0)", "ApButton", new Thickness(11, 6, 11, 6));
            _btnToggleCompGudang.Margin = new Thickness(0, 0, 8, 0);
            _btnToggleCompGudang.Click += delegate {
                _compGudangCard.Visibility = (_compGudangCard.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
                RebuildCompGudangDrawer();
            };
            leftCtrl.Children.Add(_btnToggleCompGudang);

            var btnAddDocker = MakeStyledButton("+ Kontainer Docker Kustom", "ApButton", new Thickness(12, 6, 12, 6));
            btnAddDocker.Click += delegate { OpenCustomDockerEditor(null); };
            leftCtrl.Children.Add(btnAddDocker);
            dock.Children.Add(leftCtrl);

            topBar.Child = dock;
            sp.Children.Add(topBar);

            _lblCompOutSummary = new TextBlock {
                Text = "Lokasi hasil rakitan: " + Path.Combine(_studioRoot, "output_companion_stack"),
                FontSize = 11.5, Margin = new Thickness(4, 0, 0, 12)
            };
            _lblCompOutSummary.SetResourceReference(TextBlock.ForegroundProperty, "ApBlue");
            sp.Children.Add(_lblCompOutSummary);

            // Gudang Pendamping Drawer
            _compGudangCard = MakeAppleCard(new Thickness(14), new Thickness(0, 0, 0, 14));
            _compGudangCard.Visibility = Visibility.Collapsed;
            var cgSp = new StackPanel();
            var cgHdr = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            var bCloseCg = MakeStyledButton("Tutup", "ApButton", new Thickness(10, 4, 10, 4));
            DockPanel.SetDock(bCloseCg, Dock.Right);
            bCloseCg.Click += delegate { _compGudangCard.Visibility = Visibility.Collapsed; };
            cgHdr.Children.Add(bCloseCg);
            var cgTitle = new TextBlock { Text = "Gudang Modul VPS & PC (Klik '+ Tambah' untuk mengembalikan ke rakitan)", FontSize = 13.5, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
            cgTitle.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
            cgHdr.Children.Add(cgTitle);
            cgSp.Children.Add(cgHdr);
            cgSp.Children.Add(MakeHairline());
            _compGudangRowsPanel = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
            cgSp.Children.Add(_compGudangRowsPanel);
            _compGudangCard.Child = cgSp;
            sp.Children.Add(_compGudangCard);

            // Custom Docker Service YAML Editor
            _customDockerEditorCard = BuildCustomDockerEditorCard();
            _customDockerEditorCard.Visibility = Visibility.Collapsed;
            sp.Children.Add(_customDockerEditorCard);

            _companionSectionsPanel = new StackPanel();
            sp.Children.Add(_companionSectionsPanel);

            return sp;
        }

        private Border BuildCustomDockerEditorCard()
        {
            var card = MakeAppleCard(new Thickness(16), new Thickness(0, 0, 0, 16));
            var sp = new StackPanel();

            var hdr = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
            var bClose = MakeStyledButton("Tutup", "ApButton", new Thickness(10, 4, 10, 4));
            DockPanel.SetDock(bClose, Dock.Right);
            bClose.Click += delegate { _customDockerEditorCard.Visibility = Visibility.Collapsed; };
            hdr.Children.Add(bClose);
            var t = new TextBlock { Text = "Tambah Layanan / Kontainer Docker Kustom ke docker-compose.yml", FontSize = 14, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center };
            t.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
            hdr.Children.Add(t);
            sp.Children.Add(hdr);
            sp.Children.Add(MakeHairline());

            var row1 = new Grid { Margin = new Thickness(0, 10, 0, 8) };
            row1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            row1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });

            var s0 = new StackPanel();
            s0.Children.Add(MakeSmallLabel("Nama Layanan Kustom"));
            _txtCompTitle = MakeAppleTextBox("Redis Cache / Webhook Worker", double.NaN);
            s0.Children.Add(_txtCompTitle);
            Grid.SetColumn(s0, 0);
            row1.Children.Add(s0);

            var s1 = new StackPanel();
            s1.Children.Add(MakeSmallLabel("Label Port / Info"));
            _txtCompPort = MakeAppleTextBox("TCP 6379", double.NaN);
            s1.Children.Add(_txtCompPort);
            Grid.SetColumn(s1, 2);
            row1.Children.Add(s1);
            sp.Children.Add(row1);

            sp.Children.Add(MakeSmallLabel("Deskripsi Singkat"));
            _txtCompDesc = MakeAppleTextBox("Kontainer tambahan kustom di dalam jaringan Docker IoT yang sama.", double.NaN);
            _txtCompDesc.Margin = new Thickness(0, 0, 0, 8);
            sp.Children.Add(_txtCompDesc);

            sp.Children.Add(MakeSmallLabel("Definisi YAML Docker Compose (otomatis digabung di bawah blok services:)"));
            _txtCompYaml = MakeCodeEditorBox("image: redis:7-alpine\nrestart: unless-stopped\nports:\n  - \"6379:6379\"", 84);
            _txtCompYaml.Margin = new Thickness(0, 0, 0, 10);
            sp.Children.Add(_txtCompYaml);

            var bSave = MakeStyledButton("Simpan Layanan ke Rakitan VPS", "ApPrimaryButton", new Thickness(16, 7, 16, 7));
            bSave.HorizontalAlignment = HorizontalAlignment.Right;
            bSave.Click += delegate { SaveCustomDockerFromEditor(); };
            sp.Children.Add(bSave);

            card.Child = sp;
            return card;
        }

        private void OpenCustomDockerEditor(CompanionPieceItem existing)
        {
            _editingCustomComp = existing;
            if (existing != null)
            {
                _txtCompTitle.Text = existing.Title;
                _txtCompPort.Text = existing.PortOrFile;
                _txtCompDesc.Text = existing.Description;
                _txtCompYaml.Text = existing.CustomServiceYaml;
            }
            else
            {
                _txtCompTitle.Text = "Kontainer Docker Kustom";
                _txtCompPort.Text = "TCP 6379";
                _txtCompDesc.Text = "Kontainer tambahan kustom yang terhubung ke jaringan Docker XIO.";
                _txtCompYaml.Text = "image: redis:7-alpine\nrestart: unless-stopped\nports:\n  - \"6379:6379\"";
            }
            _customDockerEditorCard.Visibility = Visibility.Visible;
        }

        private void SaveCustomDockerFromEditor()
        {
            CompanionPieceItem target = _editingCustomComp;
            if (target == null)
            {
                target = new CompanionPieceItem
                {
                    Key = "cdock_" + DateTime.Now.Ticks.ToString("x"),
                    Category = "Layanan Kustom User",
                    SourceOrigin = "Kontainer Docker Buatan User",
                    IsCustom = true,
                    Enabled = true,
                    InActiveDeck = true
                };
                _companions.Insert(0, target);
            }
            target.Title = string.IsNullOrEmpty(_txtCompTitle.Text.Trim()) ? "Layanan Docker Kustom" : _txtCompTitle.Text.Trim();
            target.PortOrFile = _txtCompPort.Text.Trim();
            target.Description = _txtCompDesc.Text.Trim();
            target.CustomServiceYaml = _txtCompYaml.Text;
            _editingCustomComp = null;
            _customDockerEditorCard.Visibility = Visibility.Collapsed;
            RebuildCompanionCards();
        }

        private void RebuildCompGudangDrawer()
        {
            int rem = 0;
            foreach (CompanionPieceItem c in _companions) if (!c.InActiveDeck && !c.IsCustom) rem++;
            if (_btnToggleCompGudang != null)
                _btnToggleCompGudang.Content = String.Format("+ Gudang Pendamping ({0} tersedia)", rem);

            if (_compGudangRowsPanel == null) return;
            _compGudangRowsPanel.Children.Clear();

            if (rem == 0)
            {
                var t = new TextBlock { Text = "Semua modul pendamping VPS & PC berada di rakitan aktif.", FontSize = 12 };
                t.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
                _compGudangRowsPanel.Children.Add(t);
                return;
            }

            foreach (CompanionPieceItem item in _companions)
            {
                if (item.InActiveDeck || item.IsCustom) continue;
                CompanionPieceItem ci = item;
                var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4) };
                var bAdd = MakeStyledButton("+ Tambah", "ApPrimaryButton", new Thickness(11, 4, 11, 4));
                DockPanel.SetDock(bAdd, Dock.Right);
                bAdd.Click += delegate { ci.InActiveDeck = true; ci.Enabled = true; RebuildCompanionCards(); };
                row.Children.Add(bAdd);

                var lbl = new TextBlock { Text = ci.Title + " (" + ci.PortOrFile + ")", FontSize = 12.5, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
                lbl.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
                row.Children.Add(lbl);
                _compGudangRowsPanel.Children.Add(row);
            }
        }

        private void RebuildCompanionCards()
        {
            if (_companionSectionsPanel == null) return;
            _companionSectionsPanel.Children.Clear();
            RebuildCompGudangDrawer();

            string[] cats = new string[] {
                "Layanan Kustom User", "Server VPS Docker", "Alat PC & Browser",
                "Alat PC & Robotik", "Alat PC & Spektrum SDR"
            };

            foreach (string cat in cats)
            {
                var list = new List<CompanionPieceItem>();
                foreach (CompanionPieceItem c in _companions)
                    if (c.InActiveDeck && c.Category == cat) list.Add(c);
                if (list.Count == 0) continue;

                _companionSectionsPanel.Children.Add(MakeSectionCaption(cat.ToUpperInvariant()));
                var card = MakeAppleCard(new Thickness(0), new Thickness(0, 0, 0, 16));
                var rows = new StackPanel();

                for (int i = 0; i < list.Count; i++)
                {
                    CompanionPieceItem ci = list[i];
                    var rg = new Grid { Margin = new Thickness(16, 12, 16, 12) };
                    rg.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    rg.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    rg.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    rg.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54) });

                    var left = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
                    var tRow = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
                    var tTitle = new TextBlock { Text = ci.Title, FontSize = 13.5, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 8, 0) };
                    tTitle.SetResourceReference(TextBlock.ForegroundProperty, "ApTextPri");
                    tRow.Children.Add(tTitle);
                    if (ci.IsCustom) tRow.Children.Add(MakeTagPill("Docker Kustom", "#AF52DE"));
                    left.Children.Add(tRow);

                    var tDesc = new TextBlock { Text = ci.Description, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 3) };
                    tDesc.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
                    left.Children.Add(tDesc);

                    var tSrc = new TextBlock { Text = "Sumber: " + ci.SourceOrigin, FontSize = 11, Opacity = 0.80 };
                    tSrc.SetResourceReference(TextBlock.ForegroundProperty, "ApBlue");
                    left.Children.Add(tSrc);
                    Grid.SetColumn(left, 0);
                    rg.Children.Add(left);

                    var badge = new Border { CornerRadius = new CornerRadius(6), Padding = new Thickness(9, 4, 9, 4), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
                    badge.SetResourceReference(Border.BackgroundProperty, "ApCardAltBg");
                    var bTxt = new TextBlock { Text = ci.PortOrFile, FontSize = 11, FontWeight = FontWeights.Medium };
                    bTxt.SetResourceReference(TextBlock.ForegroundProperty, "ApTextSec");
                    badge.Child = bTxt;
                    Grid.SetColumn(badge, 1);
                    rg.Children.Add(badge);

                    var acts = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                    if (ci.IsCustom)
                    {
                        var bEd = MakeStyledButton("Edit", "ApButton", new Thickness(9, 4, 9, 4));
                        bEd.FontSize = 11;
                        bEd.Margin = new Thickness(0, 0, 5, 0);
                        bEd.Click += delegate { OpenCustomDockerEditor(ci); };
                        acts.Children.Add(bEd);
                    }
                    var bRem = MakeStyledButton(ci.IsCustom ? "Hapus" : "Kurangi", "ApButton", new Thickness(9, 4, 9, 4));
                    bRem.FontSize = 11;
                    bRem.Click += delegate {
                        if (ci.IsCustom) _companions.Remove(ci);
                        else ci.InActiveDeck = false;
                        RebuildCompanionCards();
                    };
                    acts.Children.Add(bRem);
                    Grid.SetColumn(acts, 2);
                    rg.Children.Add(acts);

                    var sw = new CheckBox { Style = ResStyle("ApToggleSwitch"), IsChecked = ci.Enabled, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
                    sw.Checked += delegate { ci.Enabled = true; };
                    sw.Unchecked += delegate { ci.Enabled = false; };
                    Grid.SetColumn(sw, 3);
                    rg.Children.Add(sw);

                    rows.Children.Add(rg);
                    if (i < list.Count - 1)
                    {
                        var div = MakeHairline();
                        div.Margin = new Thickness(16, 0, 16, 0);
                        rows.Children.Add(div);
                    }
                }
                card.Child = rows;
                _companionSectionsPanel.Children.Add(card);
            }
        }

        public List<string> SynthesizeCompanionStackNow(bool showDialog)
        {
            string outDir = Path.Combine(_studioRoot, "output_companion_stack");
            XioProjectFile meta = GatherProjectSnapshot();
            List<string> files = CodeSynthesizer.GenerateCompanionStackFiles(outDir, _companions, meta);
            if (_lblCompOutSummary != null)
                _lblCompOutSummary.Text = String.Format("Berhasil merakit {0} file ke: {1}", files.Count, outDir);
            AppendLog(String.Format("[VPS & PC STACK] {0} berkas pendamping dirakit di -> {1}", files.Count, outDir));
            if (showDialog)
            {
                MessageBox.Show(String.Format("Paket Server VPS Docker & Alat PC berhasil dirakit ({0} file)!\n\nFolder: {1}", files.Count, outDir),
                    "Rakitan Pendamping Selesai", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            return files;
        }

        // ====================================================================
        // FULL PROJECT SNAPSHOT SAVE / LOAD (WITH USER FREEDOM EXTENSIONS!)
        // ====================================================================
        private XioProjectFile GatherProjectSnapshot()
        {
            var p = new XioProjectFile
            {
                SelectedChipId = _activeChip.Id,
                BuildEngineMode = _buildMode,
                ApSsid = _txtApSsid != null ? _txtApSsid.Text.Trim() : "XIO UNIVERSAL STUDIO",
                ApPass = _txtApPass != null ? _txtApPass.Password : "xio123456",
                StaSsid = _txtStaSsid != null ? _txtStaSsid.Text.Trim() : "",
                StaPass = _txtStaPass != null ? _txtStaPass.Password : "",
                MqttHost = _txtMqttHost != null ? _txtMqttHost.Text.Trim() : "",
                MqttTopic = _txtMqttTopic != null ? _txtMqttTopic.Text.Trim() : "xio/telemetry",
                WebhookUrl = _txtWebhook != null ? _txtWebhook.Text.Trim() : "",
                WireGuardEndpoint = _txtWgEndpoint != null ? _txtWgEndpoint.Text.Trim() : "",
                AiProvider = _cmbAiProvider != null && _cmbAiProvider.SelectedIndex == 1 ? "openrouter" : "nvidia",
                AiModel = _txtAiModel != null ? _txtAiModel.Text.Trim() : "",
                AiApiKey = _pwdAiApiKey != null ? _pwdAiApiKey.Password.Trim() : ""
            };
            int mp = 1883;
            if (_txtMqttPort != null) int.TryParse(_txtMqttPort.Text.Trim(), out mp);
            p.MqttPort = mp > 0 ? mp : 1883;

            double glat = 0, glon = 0, grad = 100;
            if (_txtGeoLat != null) double.TryParse(_txtGeoLat.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out glat);
            if (_txtGeoLon != null) double.TryParse(_txtGeoLon.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out glon);
            if (_txtGeoRad != null) double.TryParse(_txtGeoRad.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out grad);
            p.GeofenceLat = glat; p.GeofenceLon = glon; p.GeofenceRadiusM = grad;

            foreach (PuzzleFeatureItem f in _features)
            {
                if (f.IsCustomUserModule)
                    p.CustomUserModules.Add(f);
                else
                {
                    p.FeatureStates[f.Key] = f.Enabled;
                    if (!f.InActiveDeck) p.RemovedDeckKeys.Add(f.Key);
                }
            }

            foreach (CompanionPieceItem c in _companions)
            {
                if (c.IsCustom)
                    p.CustomCompanions.Add(c);
                else
                {
                    p.CompanionStates[c.Key] = c.Enabled;
                    if (!c.InActiveDeck) p.RemovedCompanionKeys.Add(c.Key);
                }
            }

            p.Slots.AddRange(_slots);
            p.Rules.AddRange(_rules);
            return p;
        }

        private void SaveProjectDialog()
        {
            var dlg = new SaveFileDialog
            {
                Filter = "Profil Pribadi XIO (*.xio.private.json)|*.xio.private.json",
                FileName = String.Format("Profil_{0}.xio.private.json", _activeChip.Id)
            };
            if (dlg.ShowDialog() == true)
            {
                if (MessageBox.Show("Profil pribadi dapat berisi kredensial jaringan dan kode kustom. Hanya API key AI yang dienkripsi untuk akun Windows ini. Jangan unggah profil pribadi; gunakan Ekspor Profil Publik untuk berbagi.",
                    "Simpan Profil Pribadi", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
                try
                {
                    StudioCatalog.SaveProjectJson(dlg.FileName, GatherProjectSnapshot());
                    AppendLog("[PROYEK] Profil pribadi tersimpan -> " + dlg.FileName);
                }
                catch (Exception)
                {
                    MessageBox.Show("Profil tidak dapat disimpan. Periksa lokasi tujuan dan akses enkripsi Windows.", "Gagal Menyimpan", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportPublicProjectDialog()
        {
            var dlg = new SaveFileDialog { Filter = "Profil Publik XIO (*.xio.public.json)|*.xio.public.json", FileName = "XIO_Public.xio.public.json" };
            if (dlg.ShowDialog() != true) return;
            try
            {
                File.WriteAllText(dlg.FileName, StudioCatalog.GeneratePublicProjectJson(GatherProjectSnapshot()), System.Text.Encoding.UTF8);
                MessageBox.Show("Profil publik diekspor tanpa kredensial, identitas jaringan, lokasi, label bebas, atau kode kustom. Periksa kembali isinya sebelum dibagikan.", "Ekspor Publik", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception)
            {
                MessageBox.Show("Ekspor gagal. Periksa pengaturan provider/model dan lokasi tujuan.", "Ekspor Publik", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadProjectDialog()
        {
            var dlg = new OpenFileDialog { Filter = "XIO Studio Project (*.xio.json;*.json)|*.xio.json;*.json" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    XioProjectFile p = StudioCatalog.LoadProjectJson(dlg.FileName);
                    for (int i = 0; i < _chips.Count; i++)
                        if (_chips[i].Id == p.SelectedChipId) { _cmbChip.SelectedIndex = i; break; }

                    _cmbEngine.SelectedIndex = (p.BuildEngineMode == 1 && _activeChip.SupportsMarauderMode) ? 1 : 0;

                    _features.RemoveAll(delegate(PuzzleFeatureItem x) { return x.IsCustomUserModule; });
                    foreach (PuzzleFeatureItem f in _features)
                    {
                        f.InActiveDeck = !(p.RemovedDeckKeys != null && p.RemovedDeckKeys.Contains(f.Key));
                        if (p.FeatureStates != null && p.FeatureStates.ContainsKey(f.Key))
                            f.Enabled = p.FeatureStates[f.Key];
                    }
                    if (p.CustomUserModules != null)
                    {
                        for (int i = p.CustomUserModules.Count - 1; i >= 0; i--)
                        {
                            p.CustomUserModules[i].IsCustomUserModule = true;
                            _features.Insert(0, p.CustomUserModules[i]);
                        }
                    }

                    _companions.RemoveAll(delegate(CompanionPieceItem x) { return x.IsCustom; });
                    foreach (CompanionPieceItem c in _companions)
                    {
                        c.InActiveDeck = !(p.RemovedCompanionKeys != null && p.RemovedCompanionKeys.Contains(c.Key));
                        if (p.CompanionStates != null && p.CompanionStates.ContainsKey(c.Key))
                            c.Enabled = p.CompanionStates[c.Key];
                    }
                    if (p.CustomCompanions != null)
                    {
                        for (int i = p.CustomCompanions.Count - 1; i >= 0; i--)
                        {
                            p.CustomCompanions[i].IsCustom = true;
                            _companions.Insert(0, p.CustomCompanions[i]);
                        }
                    }

                    _slots.Clear();
                    if (p.Slots != null) _slots.AddRange(p.Slots);

                    _rules.Clear();
                    if (p.Rules != null) _rules.AddRange(p.Rules);

                    _txtApSsid.Text = p.ApSsid ?? "XIO UNIVERSAL STUDIO";
                    _txtApPass.Password = p.ApPass ?? "xio123456";
                    _txtStaSsid.Text = p.StaSsid ?? "";
                    _txtStaPass.Password = p.StaPass ?? "";
                    _txtMqttHost.Text = p.MqttHost ?? "";
                    _txtMqttPort.Text = p.MqttPort.ToString();
                    _txtMqttTopic.Text = p.MqttTopic ?? "xio/telemetry";
                    _txtWebhook.Text = p.WebhookUrl ?? "";
                    _txtWgEndpoint.Text = p.WireGuardEndpoint ?? "";
                    _cmbAiProvider.SelectedIndex = p.AiProvider == "openrouter" ? 1 : 0;
                    _txtAiModel.Text = string.IsNullOrEmpty(p.AiModel) ? CodeSynthesizer.DefaultAiModel(p.AiProvider) : p.AiModel;
                    _pwdAiApiKey.Password = p.AiApiKey ?? "";
                    _txtGeoLat.Text = p.GeofenceLat.ToString(CultureInfo.InvariantCulture);
                    _txtGeoLon.Text = p.GeofenceLon.ToString(CultureInfo.InvariantCulture);
                    _txtGeoRad.Text = p.GeofenceRadiusM.ToString(CultureInfo.InvariantCulture);

                    RebuildDynamicRuleRows();
                    RebuildCompanionCards();
                    RefreshAllChipDependentUi();
                    AppendLog("[PROYEK] Dimuat <- " + dlg.FileName);
                }
                catch (Exception)
                {
                    MessageBox.Show("Profil tidak dapat dibuka. Periksa format JSON; kunci terenkripsi hanya dapat dibuka oleh akun Windows asal.", "Kesalahan", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
