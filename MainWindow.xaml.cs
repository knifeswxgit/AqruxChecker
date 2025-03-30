using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Newtonsoft.Json;
using RestSharp;
using Microsoft.Win32;
using System.Net.NetworkInformation;
using System.Diagnostics.Eventing.Reader;

namespace RustCheatChecker
{
    public partial class MainWindow : Window
    {
        private bool isScanning = false;
        private CancellationTokenSource cts;
        private readonly List<string> suspiciousPatterns = new List<string>
        {
            "superi", "ExLoader", "anyware", "invis", "EasyAnticheat.dll", "ak-47", "ak47",
            "macro", "cheat", "hack", ".amc", ".mgn", ".lua", "Monolith",
            "Blume", "loader", ".blm", ".bat", "pasta.cc", "pastative", "0xcheat",
            "HitScan", "Loader.exe", "Vlone", "Amphetamine", "Skyone", "Skyline", "Sky.one",
            "BestHack", "Novazbesting", "CRC32", "Checchi", "foda.win", "foda",
            "UnityCrashHandler64.zip", "rust.assets", "galaxy.bundle",
            "google_driver", ".macro", "Alkad", "chams", "Keyran",
            "Rust", "bypassEAC", "E_AC", "RustSiris",
            "Rmacros", "Kraken", "LootEsp", "AimBot", "Обход", "Чит", "Раст",
            "блюм"
        };
        private readonly HashSet<string> excludedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "system32", "windows", @"C:\Mono\", "VALORANT", "BepInEx", @"C:\Program Files\Adobe",
            @"C:\Program Files\ShareX", @"C:\Program Files\WindowsPowerShell",
            @"C:\Program Files\YandexMusicPatcher\YandexMusic", @"C:\Program Files (x86)\dotnet",
            @"C:\Program Files (x86)\Common Files\Microsoft Shared", @"C:\Program Files (x86)\Steam\steamapps\common",
            @"C:\Program Files\Microsoft Visual Studio\2022\Community", @"C:\Program Files\NVIDIA Corporation\",
            @"C:\ProgramData\NVIDIA Corporation", @"C:\ProgramData\Microsoft",
            @"C:\ProgramData\FL Cloud Plugins", @"C:\ProgramData\Adobe\CameraRaw",
            @"C:\Program Files (x86)\Windows Kits", @"C:\Program Files (x86)\Waves",
            @"C:\Program Files (x86)\Steam\appcache", @"C:\Program Files (x86)\Reference Assemblies",
            @"C:\Users\All Users\Adobe", @"C:\Program Files\Java",
            @"C:\Program Files (x86)\Steam\userdata", @"C:\Program Files\Image-Line",
            @"Rust\Bundles\items", @"C:\Users\All Users\FL Cloud Plugins",
            @"C:\Users\All Users\NVIDIA Corporation",
            @"C:\Program Files\obs-studio",
            @"C:\Program Files\MiniTool Partition Wizard 12",
            @"C:\Program Files\iTunes",
            @"C:\Program Files\Common Files\Adobe",
            @"C:\Program Files\dotnet",
            @"C:\Program Files\Cheat Engine",
            @"C:\Users\knifeswx\AppData\Local\Roblox",
            @"C:\Users\knifeswx\AppData\Local\Google",
            @"C:\Program Files (x86)\TunnelBear",
            @"C:\Users\knifeswx\AppData\Local\CapCut",
            @"C:\Program Files (x86)\Steam\friends",
            @"C:\Users\knifeswx\AppData\Roaming\ModrinthApp",
            @"C:\Program Files (x86)\Roblox",
            @"C:\Users\knifeswx\Documents\Image-Line",
            @"C:\System64",
            @"C:\Users\All Users\Antares",
            @"C:\Users\All Users\Microsoft",
            @"C:\Program Files\IntelSWTools",
            @"C:\Program Files\Reference Assemblies",
            @"C:\Program Files (x86)\NVIDIA Corporation",
            @"C:\Program Files (x86)\Pikacu Test CA Truster",
            @"C:\Users\knifeswx\AppData\Roaming\@flmafia",
            @"C:\Users\knifeswx\AppData\Roaming\.tlauncher\legacy",
            @"C:\Users\knifeswx\AppData\LocalLow\Microsoft\",
            @"C:\Users\knifeswx\AppData\Local\Temp\Roblox",
            @"C:\Users\knifeswx\source\repos",
            @"C:\Users\All Users\Waves Audio",
            @"C:\Users\Public\Documents\Soundtoys",
            @"C:\Users\knifeswx\Documents\AG\tdata",
            @"C:\Users\knifeswx\AppData\Roaming\Figma\DesktopProfile",
            @"C:\Users\knifeswx\AppData\Roaming\FL Cloud Plugins",
            @"C:\Users\knifeswx\AppData\Roaming\FACEIT\Code Cache",
            @"C:\Users\knifeswx\AppData\Roaming\EasyAntiCheat\12",
            @"C:\Users\knifeswx\AppData\Local\Steam\htmlcache",
            @"C:\Users\Все пользователи\FL Cloud Plugins",
            @"C:\Users\knifeswx\AppData\Roaming\BetterDiscord\plugins",
            @"C:\Users\knifeswx\.vscode\extensions",
            @"C:\ProgramData\Antares",
            @"C:\Games\Rust\RustClient_Data\StreamingAssets",
            @"C:\Games\VELOCUTY\tev\Bin\ace-editor",
            @"C:\Games\Rust\cfg\ai",
            @"C:\Users\knifeswx\AppData\Local\CEF\User Data",
            @"C:\ProgramData\Antares",
            @"C:\ProgramData\Antares",
            @"C:\Program Files (x86)\Radmin VPN"
        };
        private readonly HashSet<string> excludedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".wav", ".mp3"
        };
        private readonly HashSet<string> highlightKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".amc", ".blm", "blume", "alkad", "sirius", "rustsirius", "keyran", "vlone", "bypasseac"
        };
        private readonly List<string> quickScanFolders = new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Games"),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            @"C:\Windows\Prefetch"
        };
        private readonly List<string> randomTips = new List<string>
        {
            "Не забывайте чистить кэш! 😜",
            "Проверяйте систему регулярно! 🔍",
            "Читы — это нечестно! 😡",
            "Сканирование в процессе... 🕒",
            "Держите систему в чистоте! 🧹",
            "Не доверяйте подозрительным файлам! 🚨"
        };
        private string checkDate;
        private volatile bool allowAddingFiles = false;
        private string allFoundFiles = "";
        private List<string> foundFilesList = new List<string>();
        private List<string> suspiciousFiles = new List<string>();
        private bool isClosing = false;
        private bool isDarkTheme = true;
        private int totalFilesScanned = 0;
        private int totalFilesToScan = 0;
        private Stopwatch scanStopwatch;
        private Random random = new Random();
        private string steamId = string.Empty;
        private readonly string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AqrCheck", "config.json");
        private readonly string discordWebhookUrl = "https://discord.com/api/webhooks/1353193196372623471/wCnQ9TPe8PNEGXt2wIXv0L-dyvooqeU-iyRoHGQ5isf_eEvgXWZ6C8AT5Qz1_X4xZqzC";
        private bool wasScanCancelled = false;
        private string generatedCode = null; // Хранит сгенерированный код




        public MainWindow()
        {
            try
            {
                Console.WriteLine("MainWindow constructor started");
                InitializeComponent();
                Console.WriteLine("InitializeComponent completed");
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                LoadCheckDate();
                LoadConfig();
                Console.WriteLine("LoadCheckDate completed");
                UpdateCheckDateLabel();
                Console.WriteLine("UpdateCheckDateLabel completed");
                Closing += MainWindow_Closing;

                Console.WriteLine("MainWindow constructor called");

                Loaded += async (s, e) =>
                {
                    Console.WriteLine("MainWindow Loaded event triggered");
                    await ShowWelcomeOverlay();
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in MainWindow constructor: {ex.Message}\nStackTrace: {ex.StackTrace}");
                MessageBox.Show($"Ошибка при инициализации приложения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private async Task ShowWelcomeOverlay()
        {
            WelcomeOverlay.Visibility = Visibility.Visible;

            var opacityAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            WelcomeOverlay.BeginAnimation(OpacityProperty, opacityAnimation);

            await Dispatcher.InvokeAsync(() =>
            {
                MainContent.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 10 };
            });
        }

        private async Task AnimatePanelAppearance(Panel panel)
        {
            panel.Opacity = 0;
            panel.RenderTransform = new TranslateTransform { X = -20 };
            panel.Visibility = Visibility.Visible;

            var opacityAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            var translateAnimation = new DoubleAnimation
            {
                From = -20,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            panel.BeginAnimation(OpacityProperty, opacityAnimation);
            panel.RenderTransform.BeginAnimation(TranslateTransform.XProperty, translateAnimation);
            await Task.Delay(500);
        }

        private void CloseWelcomeOverlay_Click(object sender, RoutedEventArgs e)
        {
            if (StartButton.IsEnabled) // Убеждаемся, что кнопка активна (код и Steam ID верны)
            {
                var opacityAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };
                opacityAnimation.Completed += async (s, args) =>
                {
                    WelcomeOverlay.Visibility = Visibility.Collapsed;
                    Dispatcher.Invoke(() => MainContent.Effect = null);
                    await AnimatePanelAppearance(SidePanel);
                    await AnimatePanelAppearance(ScanPanel);
                    generatedCode = null; // Делаем код одноразовым, сбрасывая его после успешного ввода
                    ScanButton.Visibility = Visibility.Visible; // Показываем кнопку сканирования
                };
                WelcomeOverlay.BeginAnimation(OpacityProperty, opacityAnimation);
            }
        }

        private void CodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string enteredCode = CodeTextBox.Text.Trim();
            CheckStartButtonEnabled(enteredCode, SteamIdTextBox.Text.Trim());
        }

        private void SteamIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string enteredCode = CodeTextBox.Text.Trim();
            string enteredSteamId = SteamIdTextBox.Text.Trim();
            steamId = enteredSteamId;
            CheckStartButtonEnabled(enteredCode, enteredSteamId);

            try
            {
                var data = new { SteamId = steamId };
                File.WriteAllText(configPath, JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in SteamIdTextBox_TextChanged: {ex.Message}\nStackTrace: {ex.StackTrace}");
                MessageBox.Show($"Ошибка при сохранении Steam ID: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckStartButtonEnabled(string enteredCode, string enteredSteamId)
        {
            // Проверяем, что код введен и совпадает с сгенерированным, а Steam ID валиден
            bool isCodeValid = !string.IsNullOrWhiteSpace(enteredCode) && enteredCode == generatedCode;
            bool isSteamIdValid = !string.IsNullOrWhiteSpace(enteredSteamId) && enteredSteamId.Length == 17 && enteredSteamId.StartsWith("7656");

            StartButton.IsEnabled = isCodeValid && isSteamIdValid;

            if (!string.IsNullOrEmpty(generatedCode) && !isCodeValid && enteredCode.Length == 6)
            {
                Dispatcher.Invoke(() => WelcomeText.Text = "Неверный код! Проверьте введенные данные.");
            }
            else if (isCodeValid && isSteamIdValid)
            {
                Dispatcher.Invoke(() => WelcomeText.Text = "Код и Steam ID верны! Нажмите 'Начать'.");
            }
            else
            {
                Dispatcher.Invoke(() => WelcomeText.Text = "Добро пожаловать в AQRUX CHECKER! 🚀");
            }
        }

        private void LoadCheckDate()
        {
            Console.WriteLine("LoadCheckDate called");
            try
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string aqrCheckPath = Path.Combine(documentsPath, "AqrCheck");
                string jsonPath = Path.Combine(aqrCheckPath, "check_info.json");

                if (!Directory.Exists(aqrCheckPath))
                {
                    Directory.CreateDirectory(aqrCheckPath);
                }

                if (File.Exists(jsonPath))
                {
                    var jsonContent = File.ReadAllText(jsonPath);
                    var data = JsonConvert.DeserializeObject<dynamic>(jsonContent);
                    checkDate = data?.StartDate?.ToString() ?? "Не проводилась";
                }
                else
                {
                    checkDate = "Не проводилась";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in LoadCheckDate: {ex.Message}\nStackTrace: {ex.StackTrace}");
                checkDate = "Ошибка загрузки даты";
                MessageBox.Show($"Ошибка при загрузке даты проверки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var jsonContent = File.ReadAllText(configPath);
                    var data = JsonConvert.DeserializeObject<dynamic>(jsonContent);
                    steamId = data?.SteamId?.ToString() ?? string.Empty;
                    SteamIdTextBox.Text = steamId;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in LoadConfig: {ex.Message}\nStackTrace: {ex.StackTrace}");
                MessageBox.Show($"Ошибка при загрузке конфигурации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Console.WriteLine("MainWindow_Closing called. StackTrace:\n" + new StackTrace().ToString());

            if (isClosing)
            {
                e.Cancel = true;
                return;
            }

            isClosing = true;
            e.Cancel = true;

            try
            {
                string soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", "goodbye.wav");
                Console.WriteLine($"Trying to play sound from: {soundPath}");
                if (File.Exists(soundPath))
                {
                    Console.WriteLine("Sound file found, playing...");
                    using (var player = new System.Media.SoundPlayer(soundPath))
                    {
                        player.Play();
                    }
                }
                else
                {
                    Console.WriteLine("Sound file not found!");
                    MessageBox.Show($"Звуковой файл 'goodbye.wav' не найден по пути: {soundPath}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                await Task.Delay(1000);

                try
                {
                    string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string aqrCheckPath = Path.Combine(documentsPath, "AqrCheck");
                    string jsonPath = Path.Combine(aqrCheckPath, "check_info.json");

                    checkDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    var data = new { StartDate = checkDate };
                    File.WriteAllText(jsonPath, JsonConvert.SerializeObject(data, Formatting.Indented));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception while saving check date: {ex.Message}\nStackTrace: {ex.StackTrace}");
                    MessageBox.Show($"Ошибка при сохранении даты проверки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                Console.WriteLine("Shutting down application");
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in MainWindow_Closing: {ex.Message}\nStackTrace: {ex.StackTrace}");
                MessageBox.Show($"Ошибка при закрытии приложения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private void UpdateCheckDateLabel()
        {
            Console.WriteLine("UpdateCheckDateLabel called");
            CheckDateText.Text = $"Дата последней проверки: {checkDate}";
        }

        private async void CheckFilesButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("CheckFilesButton_Click called");
            if (!isScanning)
            {
                await HidePanel(ToolsPanel);
                await HidePanel(SteamAccountsPanel);
                await HidePanel(IpHistoryPanel);
                await ShowPanel(ScanPanel);
                SearchPanel.Visibility = Visibility.Collapsed;
                ScanButton.Visibility = Visibility.Visible;
                StatusText.Text = "Готов к сканированию...";
            }
        }

        private async void ProgramsButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("ProgramsButton_Click called");
            if (isScanning)
            {
                cts?.Cancel();
            }

            await HidePanel(ScanPanel);
            await HidePanel(SteamAccountsPanel);
            await HidePanel(IpHistoryPanel);
            await ShowPanel(ToolsPanel);
            SearchPanel.Visibility = Visibility.Collapsed;
            StatusText.Text = "Выберите программу для запуска";
        }

        private async void SteamAccountsButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("SteamAccountsButton_Click called");
            if (isScanning)
            {
                cts?.Cancel();
            }

            await HidePanel(ScanPanel);
            await HidePanel(ToolsPanel);
            await HidePanel(IpHistoryPanel);
            await ShowPanel(SteamAccountsPanel);
            SearchPanel.Visibility = Visibility.Collapsed;
            StatusText.Text = "Поиск аккаунтов Steam...";

            await GetSteamAccounts();
        }

        private async void RegeditCheckButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("RegeditCheckButton_Click called");
            if (isScanning)
            {
                cts?.Cancel();
            }

            await HidePanel(ScanPanel);
            await HidePanel(ToolsPanel);
            await HidePanel(SteamAccountsPanel);
            await HidePanel(IpHistoryPanel);
            SearchPanel.Visibility = Visibility.Collapsed;
            StatusText.Text = "Проверка реестра...";

            CheckRegistry();
        }

        private async void OpenFoldersButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("OpenFoldersButton_Click called");
            if (isScanning)
            {
                cts?.Cancel();
            }

            await HidePanel(ScanPanel);
            await HidePanel(ToolsPanel);
            await HidePanel(SteamAccountsPanel);
            await HidePanel(IpHistoryPanel);
            SearchPanel.Visibility = Visibility.Collapsed;
            StatusText.Text = "Открытие папок...";

            OpenSuspiciousFolders();
        }

        private async void IpHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("IpHistoryButton_Click called");
            if (isScanning)
            {
                cts?.Cancel();
            }

            await HidePanel(ScanPanel);
            await HidePanel(ToolsPanel);
            await HidePanel(SteamAccountsPanel);
            Console.WriteLine("Hiding other panels completed");

            await ShowPanel(IpHistoryPanel); // Показываем панель истории IP
            Console.WriteLine("ShowPanel for IpHistoryPanel completed");

            SearchPanel.Visibility = Visibility.Collapsed;
            StatusText.Text = "Получение истории IP...";
            Console.WriteLine("SearchPanel hidden, StatusText updated");

            // Добавляем тестовые данные для проверки отображения
            await UpdateIpHistoryText(new List<string> { "Тестовая строка 1", "Тестовая строка 2" });

            await GetIpHistory();
        }

        private async Task ShowPanel(Panel panel)
        {
            Console.WriteLine($"ShowPanel called for {panel.Name}");
            if (panel.Visibility == Visibility.Visible)
            {
                Console.WriteLine($"{panel.Name} is already visible, skipping...");
                return;
            }

            panel.Opacity = 0;
            panel.RenderTransform = new TranslateTransform { X = 20 };
            panel.Visibility = Visibility.Visible;
            Console.WriteLine($"{panel.Name} set to Visible");

            var opacityAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            var translateAnimation = new DoubleAnimation
            {
                From = 20,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            panel.BeginAnimation(OpacityProperty, opacityAnimation);
            panel.RenderTransform.BeginAnimation(TranslateTransform.XProperty, translateAnimation);
            Console.WriteLine($"{panel.Name} animations started");

            await Task.Delay(300);
            Console.WriteLine($"{panel.Name} animations completed, Opacity: {panel.Opacity}");
        }

        private async Task HidePanel(Panel panel)
        {
            Console.WriteLine($"HidePanel called for {panel.Name}");
            if (panel.Visibility == Visibility.Collapsed)
            {
                Console.WriteLine($"{panel.Name} is already collapsed, skipping...");
                return;
            }

            var opacityAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            var translateAnimation = new DoubleAnimation
            {
                From = 0,
                To = 20,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            panel.BeginAnimation(OpacityProperty, opacityAnimation);
            panel.RenderTransform.BeginAnimation(TranslateTransform.XProperty, translateAnimation);
            Console.WriteLine($"{panel.Name} animations started");

            await Task.Delay(300);
            panel.Visibility = Visibility.Collapsed;
            Console.WriteLine($"{panel.Name} set to Collapsed");
        }

        private void LaunchProgram_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("LaunchProgram_Click called");
            if (sender is Button button && button.Tag is string programName)
            {
                try
                {
                    string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs", programName);
                    Process.Start(new ProcessStartInfo { FileName = exePath, UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in LaunchProgram_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
                    MessageBox.Show($"Ошибка при запуске {programName}: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("ScanButton_Click started");
            if (isScanning)
            {
                Console.WriteLine("ScanButton_Click: Already scanning, cancelling...");
                wasScanCancelled = true;
                cts?.Cancel();
                return;
            }

            bool isQuickScan = Dispatcher.Invoke(() => QuickScanCheckBox.IsChecked == true);

            isScanning = true;
            allowAddingFiles = true;
            wasScanCancelled = false;
            cts = new CancellationTokenSource();
            scanStopwatch = Stopwatch.StartNew();
            StatusText.Text = "Сканирование началось...";
            FoundFilesText.Document.Blocks.Clear();
            allFoundFiles = "";
            foundFilesList.Clear();
            suspiciousFiles.Clear();
            ScanButton.Content = "Остановить";
            SearchPanel.Visibility = Visibility.Collapsed;
            ScanProgressBar.Visibility = Visibility.Visible;
            ScanProgressBar.Value = 0;
            totalFilesScanned = 0;
            totalFilesToScan = 0;

            PlaySound("start_scan.wav");

            Task tipsTask = Task.Run(() => ShowRandomTips(cts.Token));

            try
            {
                Console.WriteLine("ScanButton_Click: Starting scan...");
                await Task.Run(() => ScanSystem(cts.Token, isQuickScan), cts.Token);
                if (foundFilesList.Count == 0 && !wasScanCancelled)
                {
                    Console.WriteLine("ScanButton_Click: No suspicious files found");
                    foundFilesList.Add("Подозрительных файлов не найдено");
                    allFoundFiles = "Подозрительных файлов не найдено";
                    await UpdateFoundFilesTextWithTypingEffect(new List<string> { "Подозрительных файлов не найдено" });
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("ScanButton_Click: Scan cancelled by user");
                StatusText.Text = "Сканирование остановлено пользователем";
                if (foundFilesList.Count == 0)
                {
                    foundFilesList.Add("Сканирование было отменено пользователем.");
                    allFoundFiles = "Сканирование было отменено пользователем.";
                    await UpdateFoundFilesTextWithTypingEffect(new List<string> { "Сканирование было отменено пользователем." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ScanButton_Click: Exception occurred: {ex.Message}\nStackTrace: {ex.StackTrace}");
                StatusText.Text = "Произошла ошибка при сканировании";
                MessageBox.Show($"Ошибка при сканировании: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Console.WriteLine("ScanButton_Click: Scan finished");
                scanStopwatch.Stop();
                isScanning = false;
                allowAddingFiles = false;

                try
                {
                    await tipsTask;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("tipsTask was cancelled.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in tipsTask: {ex.Message}");
                }

                cts?.Dispose();
                cts = null;

                ScanButton.Content = "Сканировать систему";
                StatusText.Text = wasScanCancelled
                    ? "Сканирование остановлено пользователем"
                    : $"Сканирование завершено за {FormatTimeSpan(scanStopwatch.Elapsed)}";
                SearchPanel.Visibility = Visibility.Visible;
                ScanProgressBar.Visibility = Visibility.Collapsed;
                PlaySound("scan_complete.wav");

                SaveScanResults();
                await SendToDiscordWebhook();
            }
        }
        private async void GenerateCodeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Генерируем случайный 6-значный код
                generatedCode = random.Next(100000, 999999).ToString();
                Console.WriteLine($"Generated code: {generatedCode}");

                // Отправляем код в Discord через Webhook
                var options = new RestClientOptions(discordWebhookUrl);
                var client = new RestClient(options);
                var request = new RestRequest("", Method.Post);

                string messageContent = $"Сгенерирован одноразовый код: **{generatedCode}**";
                request.AddParameter("content", messageContent);

                var response = await client.ExecuteAsync(request);
                if (response.IsSuccessful)
                {
                    Console.WriteLine("Code successfully sent to Discord webhook.");
                    MessageBox.Show("Код успешно сгенерирован и отправлен в Discord. Введите его в поле ниже.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    Console.WriteLine($"Failed to send code to Discord: {response.StatusCode} - {response.ErrorMessage}");
                    MessageBox.Show($"Ошибка при отправке кода в Discord: {response.ErrorMessage}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    generatedCode = null; // Сбрасываем код, если отправка не удалась
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GenerateCodeButton_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
                MessageBox.Show($"Ошибка при генерации кода: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                generatedCode = null;
            }
        }

        private void PlaySound(string soundFile)
        {
            try
            {
                string soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", soundFile);
                if (File.Exists(soundPath))
                {
                    using (var player = new System.Media.SoundPlayer(soundPath))
                    {
                        player.Play();
                    }
                }
                else
                {
                    Console.WriteLine($"Sound file {soundFile} not found at {soundPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing sound {soundFile}: {ex.Message}");
            }
        }

        private void SaveScanResults()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string resultsPath = Path.Combine(desktopPath, "scan_results.txt");
                File.WriteAllText(resultsPath, allFoundFiles);
                Console.WriteLine($"Scan results saved to {resultsPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving scan results: {ex.Message}");
                MessageBox.Show($"Ошибка при сохранении результатов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SendToDiscordWebhook()
        {
            try
            {
                string resultsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "scan_results.txt");
                if (!File.Exists(resultsPath))
                {
                    Console.WriteLine("Scan results file not found, creating an empty one...");
                    File.WriteAllText(resultsPath, "No results available.");
                }

                var options = new RestClientOptions(discordWebhookUrl);
                var client = new RestClient(options);
                var request = new RestRequest("", Method.Post);

                string steamProfileUrl = string.IsNullOrEmpty(steamId) ? "Не указан Steam ID" : $"https://steamcommunity.com/profiles/{steamId}";
                string steamIdMessage = string.IsNullOrEmpty(steamId) ? "Steam ID: Не указан" : $"Steam ID: [{steamId}]({steamProfileUrl})";

                string suspiciousFilesMessage = suspiciousFiles.Count > 0
                    ? "Подозрительные файлы (выделены красным):\n" + string.Join("\n", suspiciousFiles)
                    : wasScanCancelled
                        ? "Сканирование было отменено пользователем."
                        : "Подозрительных файлов не найдено.";

                string messageContent = $"{steamIdMessage}\n\nРезультаты сканирования:\n{suspiciousFilesMessage}";
                request.AddParameter("content", messageContent);

                request.AddFile("file", File.ReadAllBytes(resultsPath), "scan_results.txt", "text/plain");

                var response = await client.ExecuteAsync(request);
                if (response.IsSuccessful)
                {
                    Console.WriteLine("Successfully sent scan results to Discord webhook.");
                }
                else
                {
                    Console.WriteLine($"Failed to send to Discord webhook: {response.StatusCode} - {response.ErrorMessage}\nContent: {response.Content}");
                    MessageBox.Show($"Ошибка при отправке в Discord: {response.StatusCode} - {response.ErrorMessage}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in SendToDiscordWebhook: {ex.Message}\nStackTrace: {ex.StackTrace}");
                MessageBox.Show($"Ошибка при отправке в Discord: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ShowRandomTips(CancellationToken token)
        {
            while (!token.IsCancellationRequested && isScanning)
            {
                string tip = randomTips[random.Next(randomTips.Count)];
                Dispatcher.Invoke(() => StatusText.Text = tip);
                try
                {
                    await Task.Delay(5000, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            return $"{timeSpan.Minutes} мин {timeSpan.Seconds} сек";
        }

        private void ScanSystem(CancellationToken token, bool isQuickScan)
        {
            Console.WriteLine("ScanSystem called");
            if (isQuickScan)
            {
                totalFilesToScan = 0;
                foreach (var folder in quickScanFolders)
                {
                    if (Directory.Exists(folder))
                    {
                        try
                        {
                            totalFilesToScan += CountFiles(new DirectoryInfo(folder), token);
                        }
                        catch { }
                    }
                }

                Parallel.ForEach(quickScanFolders, new ParallelOptions
                {
                    CancellationToken = token,
                    MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2)
                }, folder =>
                {
                    if (Directory.Exists(folder))
                    {
                        try
                        {
                            ScanDirectory(new DirectoryInfo(folder), token);
                        }
                        catch (UnauthorizedAccessException) { }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Exception in ScanSystem (quick scan): {ex.Message}\nStackTrace: {ex.StackTrace}");
                        }
                    }
                });
            }
            else
            {
                var drives = DriveInfo.GetDrives();
                totalFilesToScan = 0;

                foreach (var drive in drives)
                {
                    if (drive.IsReady)
                    {
                        try
                        {
                            totalFilesToScan += CountFiles(drive.RootDirectory, token);
                        }
                        catch { }
                    }
                }

                Parallel.ForEach(drives, new ParallelOptions
                {
                    CancellationToken = token,
                    MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2)
                }, drive =>
                {
                    if (drive.IsReady)
                    {
                        try
                        {
                            ScanDirectory(drive.RootDirectory, token);
                        }
                        catch (UnauthorizedAccessException) { }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Exception in ScanSystem: {ex.Message}\nStackTrace: {ex.StackTrace}");
                        }
                    }
                });
            }
        }

        private int CountFiles(DirectoryInfo dir, CancellationToken token)
        {
            int count = 0;
            try
            {
                token.ThrowIfCancellationRequested();
                string dirPath = dir.FullName.ToLower();
                if (excludedFolders.Any(folder => dirPath.Contains(folder.ToLower())))
                {
                    return 0;
                }

                count += dir.GetFiles("*", SearchOption.TopDirectoryOnly).Length;
                foreach (var subDir in dir.GetDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    count += CountFiles(subDir, token);
                }
            }
            catch { }
            return count;
        }

        private void ScanDirectory(DirectoryInfo dir, CancellationToken token)
        {
            if (!allowAddingFiles) return;

            token.ThrowIfCancellationRequested();

            string dirPath = dir.FullName.ToLower();
            Console.WriteLine($"Scanning directory: {dirPath}");
            if (excludedFolders.Any(folder => dirPath.Contains(folder.ToLower())))
            {
                Console.WriteLine($"Directory {dirPath} is excluded, skipping...");
                return;
            }

            try
            {
                FileInfo[] files;
                try
                {
                    files = dir.GetFiles("*", SearchOption.TopDirectoryOnly);
                    Console.WriteLine($"Found {files.Length} files in {dirPath}");
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine($"Access denied to {dirPath}, skipping...");
                    return;
                }

                foreach (var file in files)
                {
                    if (!allowAddingFiles) return;
                    token.ThrowIfCancellationRequested();

                    if (excludedExtensions.Contains(file.Extension.ToLower()))
                    {
                        Console.WriteLine($"File {file.FullName} has excluded extension {file.Extension}, skipping...");
                        continue;
                    }

                    Interlocked.Increment(ref totalFilesScanned);
                    if (totalFilesToScan > 0)
                    {
                        double progress = (double)totalFilesScanned / totalFilesToScan * 100;
                        Dispatcher.Invoke(() => ScanProgressBar.Value = Math.Min(progress, 100));
                    }

                    string matchedPattern = null;
                    foreach (var pattern in suspiciousPatterns)
                    {
                        string cleanPattern = pattern.Replace("*", "");
                        if (file.Name.IndexOf(cleanPattern, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            matchedPattern = cleanPattern;
                            break;
                        }
                    }

                    if (matchedPattern != null)
                    {
                        string fileEntry = $"{file.FullName} (matched: {matchedPattern})";
                        Console.WriteLine($"Found suspicious file: {fileEntry}");
                        if (allowAddingFiles)
                        {
                            foundFilesList.Add(fileEntry);
                            allFoundFiles += fileEntry + "\n";
                            bool isSuspicious = highlightKeywords.Any(keyword => fileEntry.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
                            if (isSuspicious)
                            {
                                suspiciousFiles.Add(fileEntry);
                                PlaySound("alert.wav");
                            }
                            Dispatcher.Invoke(async () => await UpdateFoundFilesTextWithTypingEffect(new List<string> { fileEntry }));
                        }
                    }
                }

                DirectoryInfo[] subDirs;
                try
                {
                    subDirs = dir.GetDirectories("*", SearchOption.TopDirectoryOnly);
                    Console.WriteLine($"Found {subDirs.Length} subdirectories in {dirPath}");
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine($"Access denied to subdirectories in {dirPath}, skipping...");
                    return;
                }

                foreach (var subDir in subDirs)
                {
                    ScanDirectory(subDir, token);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"Access denied to {dirPath}, skipping...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in ScanDirectory: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private async Task UpdateFoundFilesTextWithTypingEffect(List<string> filesToAdd)
        {
            Console.WriteLine("UpdateFoundFilesTextWithTypingEffect called");
            var document = FoundFilesText.Document;
            var paragraphs = new List<Paragraph>();

            foreach (var fileEntry in filesToAdd)
            {
                bool isSuspicious = highlightKeywords.Any(keyword => fileEntry.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
                var paragraph = new Paragraph();

                if (fileEntry.StartsWith("Подозрительных файлов не найдено") || fileEntry.StartsWith("Сканирование было отменено пользователем."))
                {
                    var run = new Run(fileEntry)
                    {
                        Foreground = isSuspicious ? new SolidColorBrush(Colors.Red) : (SolidColorBrush)Resources["TextForeground"]
                    };
                    paragraph.Inlines.Add(run);
                }
                else
                {
                    int matchIndex = fileEntry.IndexOf("(matched:");
                    string filePath = matchIndex > 0 ? fileEntry.Substring(0, matchIndex).Trim() : fileEntry;
                    string matchInfo = matchIndex > 0 ? fileEntry.Substring(matchIndex) : string.Empty;

                    var hyperlink = new Hyperlink
                    {
                        Foreground = isSuspicious ? new SolidColorBrush(Colors.Red) : (SolidColorBrush)Resources["TextForeground"],
                        TextDecorations = null
                    };
                    hyperlink.Inlines.Add(filePath);
                    hyperlink.RequestNavigate += (sender, e) =>
                    {
                        try
                        {
                            if (File.Exists(filePath))
                            {
                                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                            }
                            else if (Directory.Exists(Path.GetDirectoryName(filePath)))
                            {
                                Process.Start("explorer.exe", Path.GetDirectoryName(filePath));
                            }
                            else
                            {
                                MessageBox.Show("Файл или папка не найдены.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при открытии файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    };
                    paragraph.Inlines.Add(hyperlink);

                    if (!string.IsNullOrEmpty(matchInfo))
                    {
                        var run = new Run(matchInfo)
                        {
                            Foreground = isSuspicious ? new SolidColorBrush(Colors.Red) : (SolidColorBrush)Resources["TextForeground"]
                        };
                        paragraph.Inlines.Add(run);
                    }
                }

                paragraph.Inlines.Add(new LineBreak());
                paragraphs.Add(paragraph);
            }

            await Dispatcher.InvokeAsync(() =>
            {
                document.Blocks.AddRange(paragraphs);
                FoundFilesText.ScrollToEnd();
            });

            await Task.Delay(10 * filesToAdd.Count);
        }

        private async Task GetSteamAccounts()
        {
            try
            {
                string steamPath = GetSteamPathFromRegistry();
                if (string.IsNullOrEmpty(steamPath))
                {
                    Dispatcher.Invoke(() => StatusText.Text = "Steam не найден на устройстве.");
                    await UpdateSteamAccountsText(new List<string> { "Steam не найден на устройстве." });
                    return;
                }

                string userDataPath = Path.Combine(steamPath, "userdata");
                if (!Directory.Exists(userDataPath))
                {
                    Dispatcher.Invoke(() => StatusText.Text = "Папка userdata не найдена.");
                    await UpdateSteamAccountsText(new List<string> { "Папка userdata не найдена." });
                    return;
                }

                var accountFolders = Directory.GetDirectories(userDataPath);
                if (accountFolders.Length == 0)
                {
                    Dispatcher.Invoke(() => StatusText.Text = "Аккаунты Steam не найдены.");
                    await UpdateSteamAccountsText(new List<string> { "Аккаунты Steam не найдены." });
                    return;
                }

                List<string> accountList = new List<string>();
                foreach (var folder in accountFolders)
                {
                    string steamId32 = Path.GetFileName(folder);
                    if (int.TryParse(steamId32, out int id32) && id32 != 0)
                    {
                        string steamId64 = ConvertSteamId32ToSteamId64(id32);
                        string nickname = await GetSteamNickname(steamId64);
                        string steamProfileUrl = string.IsNullOrEmpty(steamId) ? "Не указан Steam ID" : $"https://steamcommunity.com/profiles/{steamId64}";
                        string accountEntry = $"Ник: {nickname ?? "Неизвестно"} - Steam ID: [{steamId64}]({steamProfileUrl})";
                        accountList.Add(accountEntry);
                    }
                }

                if (accountList.Count == 0)
                {
                    Dispatcher.Invoke(() => StatusText.Text = "Аккаунты Steam не найдены.");
                    await UpdateSteamAccountsText(new List<string> { "Аккаунты Steam не найдены." });
                }
                else
                {
                    Dispatcher.Invoke(() => StatusText.Text = $"Найдено аккаунтов: {accountList.Count}");
                    await UpdateSteamAccountsText(accountList);
                    await SendSteamAccountsToDiscord(accountList);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetSteamAccounts: {ex.Message}\nStackTrace: {ex.StackTrace}");
                Dispatcher.Invoke(() => StatusText.Text = "Ошибка при поиске аккаунтов.");
                await UpdateSteamAccountsText(new List<string> { $"Ошибка: {ex.Message}" });
            }
        }

        private string GetSteamPathFromRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    if (key != null)
                    {
                        return key.GetValue("SteamPath")?.ToString();
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetSteamPathFromRegistry: {ex.Message}");
                return null;
            }
        }

        private string ConvertSteamId32ToSteamId64(int steamId32)
        {
            long universe = 1;
            long accountType = 1;
            long steamId64 = (universe << 56) | (accountType << 52) | (steamId32 & 0xFFFFFFFF);
            return steamId64.ToString();
        }

        private async Task<string> GetSteamNickname(string steamId64)
        {
            const string apiKey = "E1DB2A5CB22CB2C38632711856280083"; // Ваш ключ веб-API Steam
            string url = $"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={apiKey}&steamids={steamId64}";

            try
            {
                var client = new RestClient(url);
                var request = new RestRequest();
                var response = await client.ExecuteAsync(request);

                Console.WriteLine($"Steam API Response for {steamId64}: {response.Content}");

                if (response.IsSuccessful)
                {
                    var json = JsonConvert.DeserializeObject<dynamic>(response.Content);
                    var players = json?.response?.players;
                    if (players != null && ((Newtonsoft.Json.Linq.JArray)players).Count > 0)
                    {
                        return players[0]?.personaname?.ToString();
                    }
                    return null;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetSteamNickname: {ex.Message}");
                return null;
            }
        }

        private async Task UpdateSteamAccountsText(List<string> accounts)
        {
            var document = SteamAccountsText.Document;
            Dispatcher.Invoke(() => document.Blocks.Clear());

            var paragraphs = new List<Paragraph>();
            foreach (var account in accounts)
            {
                var paragraph = new Paragraph();
                var run = new Run(account) { Foreground = (SolidColorBrush)Resources["TextForeground"] };
                paragraph.Inlines.Add(run);
                paragraph.Inlines.Add(new LineBreak());
                paragraphs.Add(paragraph);
            }

            await Dispatcher.InvokeAsync(() =>
            {
                document.Blocks.AddRange(paragraphs);
                SteamAccountsText.ScrollToEnd();
            });
        }

        private async Task SendSteamAccountsToDiscord(List<string> accounts)
        {
            try
            {
                var options = new RestClientOptions(discordWebhookUrl);
                var client = new RestClient(options);
                var request = new RestRequest("", Method.Post);

                string steamProfileUrl = string.IsNullOrEmpty(steamId) ? "Не указан Steam ID" : $"https://steamcommunity.com/profiles/{steamId}";
                string steamIdMessage = string.IsNullOrEmpty(steamId) ? "Steam ID: Не указан" : $"Steam ID: [{steamId}]({steamProfileUrl})";
                string accountsMessage = accounts.Count > 0
                    ? "Остальные аккаунты:\n" + string.Join("\n", accounts)
                    : "Дополнительные аккаунты не найдены.";

                string messageContent = $"{steamIdMessage}\n\n{accountsMessage}";
                request.AddParameter("content", messageContent);

                var response = await client.ExecuteAsync(request);
                if (!response.IsSuccessful)
                {
                    Console.WriteLine($"Failed to send to Discord: {response.StatusCode} - {response.ErrorMessage}");
                    MessageBox.Show($"Ошибка при отправке в Discord: {response.ErrorMessage}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in SendSteamAccountsToDiscord: {ex.Message}");
                MessageBox.Show($"Ошибка при отправке в Discord: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckRegistry()
        {
            try
            {
                string regOutput = "";

                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppSwitched"))
                {
                    if (key != null)
                    {
                        regOutput += "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FeatureUsage\\AppSwitched:\n";
                        foreach (var valueName in key.GetValueNames())
                        {
                            regOutput += $"  {valueName} = {key.GetValue(valueName)}\n";
                        }
                    }
                    else
                    {
                        regOutput += "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FeatureUsage\\AppSwitched: Не найдено\n";
                    }
                }

                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\ControlSet001\Services\bam\State\UserSettings\S-1-5-21-414228067-3517414977-1426574-1001"))
                {
                    if (key != null)
                    {
                        regOutput += "\nHKEY_LOCAL_MACHINE\\SYSTEM\\ControlSet001\\Services\\bam\\State\\UserSettings\\S-1-5-21-414228067-3517414977-1426574-1001:\n";
                        foreach (var valueName in key.GetValueNames())
                        {
                            var value = key.GetValue(valueName);
                            if (value is byte[] bytes)
                            {
                                regOutput += $"  {valueName} = {BitConverter.ToString(bytes).Replace("-", "")}\n";
                            }
                            else
                            {
                                regOutput += $"  {valueName} = {value}\n";
                            }
                        }
                    }
                    else
                    {
                        regOutput += "\nHKEY_LOCAL_MACHINE\\SYSTEM\\ControlSet001\\Services\\bam\\State\\UserSettings\\S-1-5-21-414228067-3517414977-1426574-1001: Не найдено\n";
                    }
                }

                string tempFile = Path.GetTempFileName();
                File.WriteAllText(tempFile, regOutput);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k type \"{tempFile}\" & del \"{tempFile}\"",
                    UseShellExecute = true
                };
                Process.Start(psi);

                Dispatcher.Invoke(() => StatusText.Text = "Проверка реестра завершена.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in CheckRegistry: {ex.Message}");
                Dispatcher.Invoke(() => StatusText.Text = "Ошибка при проверке реестра.");
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenSuspiciousFolders()
        {
            try
            {
                List<string> foldersToOpen = new List<string>
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"C:\PerfLogs",
                    Environment.GetFolderPath(Environment.SpecialFolder.Recent),
                    @"C:\Windows\Prefetch",
                    Path.GetTempPath(),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp")
                };

                foreach (var folder in foldersToOpen)
                {
                    if (Directory.Exists(folder))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"\"{folder}\"",
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Folder not found: {folder}");
                    }
                }

                Dispatcher.Invoke(() => StatusText.Text = "Папки успешно открыты.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in OpenSuspiciousFolders: {ex.Message}");
                Dispatcher.Invoke(() => StatusText.Text = "Ошибка при открытии папок.");
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task GetIpHistory()
        {
            try
            {
                List<string> ipHistoryOutput = new List<string> { "История IP-адресов:" };

                // Получение текущего IP и времени его последнего использования
                var activeInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                    .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .Select(a => new { Address = a.Address.ToString(), LastUsed = GetLastIpUsageTime(a.Address.ToString()) })
                    .ToList();

                ipHistoryOutput.Add("\nТекущий IP:");
                if (activeInterfaces.Any())
                {
                    foreach (var ipInfo in activeInterfaces)
                    {
                        string lastUsed = ipInfo.LastUsed != DateTime.MinValue
                            ? ipInfo.LastUsed.ToString("yyyy-MM-dd HH:mm:ss")
                            : "Не удалось определить время последнего использования";
                        ipHistoryOutput.Add($"  {ipInfo.Address} (Последнее использование: {lastUsed})");
                    }
                }
                else
                {
                    ipHistoryOutput.Add("  Не удалось определить текущий IP");
                }

                // Проверка текущего IP через Dns.GetHostEntry
                ipHistoryOutput.Add($"Текущий IP (проверка через Dns): {System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString() ?? "Не найден"}");

                // Получение истории IP из журнала событий
                ipHistoryOutput.Add("\nИстория IP из системных логов (DHCP):");
                string query = "*[System[(EventID=4201)]]"; // Пробуем другой EventID (Tcpip, подключение к сети)
                EventLogQuery eventQuery = new EventLogQuery("System", PathType.LogName, query);
                EventLogReader logReader = new EventLogReader(eventQuery);
                Dictionary<string, DateTime> ipHistory = new Dictionary<string, DateTime>();

                int eventCount = 0;
                for (EventRecord entry = logReader.ReadEvent(); entry != null; entry = logReader.ReadEvent())
                {
                    eventCount++;
                    string description = entry.FormatDescription();
                    if (!string.IsNullOrEmpty(description) && description.Contains("IP"))
                    {
                        string ip = ExtractIpFromEvent(description);
                        if (!string.IsNullOrEmpty(ip))
                        {
                            if (!ipHistory.ContainsKey(ip) || entry.TimeCreated > ipHistory[ip])
                            {
                                ipHistory[ip] = entry.TimeCreated.Value;
                            }
                        }
                    }
                }

                if (eventCount == 0)
                {
                    ipHistoryOutput.Add("  Не найдено событий в системных логах (EventID 4201)");
                }
                else if (ipHistory.Any())
                {
                    ipHistoryOutput.Add($"  Найдено событий: {eventCount}");
                    foreach (var ipEntry in ipHistory.OrderByDescending(x => x.Value))
                    {
                        ipHistoryOutput.Add($"  {ipEntry.Key} (Последнее использование: {ipEntry.Value:yyyy-MM-dd HH:mm:ss})");
                    }
                }
                else
                {
                    ipHistoryOutput.Add($"  Найдено событий: {eventCount}, но IP-адреса не извлечены");
                }

                // Отображение результата в приложении
                await UpdateIpHistoryText(ipHistoryOutput);

                Dispatcher.Invoke(() => StatusText.Text = "История IP успешно получена.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetIpHistory: {ex.Message}\nStackTrace: {ex.StackTrace}");
                Dispatcher.Invoke(() => StatusText.Text = "Ошибка при получении истории IP.");
                await UpdateIpHistoryText(new List<string> { $"Ошибка: {ex.Message}" });
            }
        }

        private DateTime GetLastIpUsageTime(string ipAddress)
        {
            try
            {
                string query = "*[System[(EventID=4201)]]";
                EventLogQuery eventQuery = new EventLogQuery("System", PathType.LogName, query);
                EventLogReader logReader = new EventLogReader(eventQuery);
                DateTime lastUsed = DateTime.MinValue;
                int eventCount = 0;

                for (EventRecord entry = logReader.ReadEvent(); entry != null; entry = logReader.ReadEvent())
                {
                    eventCount++;
                    string description = entry.FormatDescription();
                    if (!string.IsNullOrEmpty(description) && description.Contains(ipAddress))
                    {
                        if (entry.TimeCreated.HasValue && entry.TimeCreated.Value > lastUsed)
                        {
                            lastUsed = entry.TimeCreated.Value;
                            Console.WriteLine($"Found last usage for IP {ipAddress}: {lastUsed}");
                        }
                    }
                }

                if (eventCount == 0)
                {
                    Console.WriteLine($"No events found for IP {ipAddress} (EventID 4201)");
                }
                else if (lastUsed == DateTime.MinValue)
                {
                    Console.WriteLine($"Found {eventCount} events for IP {ipAddress}, but no matching IP in descriptions");
                }

                return lastUsed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetLastIpUsageTime: {ex.Message}");
                return DateTime.MinValue;
            }
        }

        private async Task UpdateIpHistoryText(List<string> ipEntries)
        {
            var document = IpHistoryText.Document;
            await Dispatcher.InvokeAsync(() =>
            {
                document.Blocks.Clear();
                Console.WriteLine($"Updating IpHistoryText with {ipEntries.Count} entries");
                foreach (var entry in ipEntries)
                {
                    var paragraph = new Paragraph();
                    var run = new Run(entry) { Foreground = (SolidColorBrush)Resources["TextForeground"] };
                    paragraph.Inlines.Add(run);
                    paragraph.Inlines.Add(new LineBreak());
                    document.Blocks.Add(paragraph);
                    Console.WriteLine($"Added entry to IpHistoryText: {entry}");
                }
                IpHistoryText.ScrollToEnd();
            });
        }

        private string ExtractIpFromEvent(string description)
        {
            try
            {
                Console.WriteLine($"Event description: {description}");
                var match = System.Text.RegularExpressions.Regex.Match(description, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
                if (match.Success)
                {
                    string ip = match.Value;
                    Console.WriteLine($"Extracted IP: {ip}");
                    return ip;
                }
                else
                {
                    Console.WriteLine("No IP address found in event description");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in ExtractIpFromEvent: {ex.Message}");
                return null;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text.ToLower();
            var filteredFiles = foundFilesList.Where(f => f.ToLower().Contains(searchText)).ToList();

            if (string.IsNullOrEmpty(searchText) || searchText == "поиск по найденным файлам...")
            {
                Dispatcher.Invoke(() => FoundFilesText.Document.Blocks.Clear());
                UpdateFoundFilesTextWithTypingEffect(foundFilesList);
            }
            else
            {
                Dispatcher.Invoke(() => FoundFilesText.Document.Blocks.Clear());
                UpdateFoundFilesTextWithTypingEffect(filteredFiles);
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            isDarkTheme = !isDarkTheme;
            UpdateTheme();
        }

        private void UpdateTheme()
        {
            if (isDarkTheme)
            {
                Resources["WindowBackground"] = Resources["DarkBackground"];
                Resources["PanelBackground"] = Resources["DarkPanelBackground"];
                Resources["ButtonBackground"] = Resources["DarkButtonBackground"];
                Resources["ButtonHoverBackground"] = Resources["DarkButtonHoverBackground"];
                Resources["ButtonPressedBackground"] = Resources["DarkButtonPressedBackground"];
                Resources["TextForeground"] = Resources["DarkTextForeground"];
                Resources["BorderBrush"] = Resources["DarkBorderBrush"];
            }
            else
            {
                Resources["WindowBackground"] = Resources["LightBackground"];
                Resources["PanelBackground"] = Resources["LightPanelBackground"];
                Resources["ButtonBackground"] = Resources["LightButtonBackground"];
                Resources["ButtonHoverBackground"] = Resources["LightButtonHoverBackground"];
                Resources["ButtonPressedBackground"] = Resources["LightButtonPressedBackground"];
                Resources["TextForeground"] = Resources["LightTextForeground"];
                Resources["BorderBrush"] = Resources["LightBorderBrush"];
            }
        }
    }
}
