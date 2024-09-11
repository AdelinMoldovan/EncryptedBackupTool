using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;
using Path = System.IO.Path;
using System.IO.Compression;
using System.Diagnostics;
using System.Configuration;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Timers.Timer;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using IWshRuntimeLibrary;
using File = System.IO.File;

namespace RecoveryApplication
{
    public partial class MainWindow : Window
    {
        private Timer _archiveTimer;
        private NotifyIcon notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            SetupSystemTray();
            AddToStartup();
            LoadUserSettings();
            StartArchivingIfScheduled();

            GPGKeyPasswordBox.Clear();
            GPGKeyPasswordBoxRecovery.Clear();
        }


        private void LoadUserSettings()
        {
            SourceDirectoryTextBox.Text = ConfigurationManager.AppSettings["SourceDirectory"];
            GPGKeyPasswordBox.Text = ConfigurationManager.AppSettings["GPGKey_Archive"];
            GPGKeyPasswordBoxRecovery.Text = ConfigurationManager.AppSettings["GPGKey_Recovery"];
            DestinationDirectoryTextBox.Text = ConfigurationManager.AppSettings["DestinationDirectory"];

            if (int.TryParse(ConfigurationManager.AppSettings["ArchiveFrequency"], out int frequency))
            {
                ArchivingFrequencyComboBox.SelectedItem = frequency;
            }
        }

        private void SetupSystemTray()
        {
            notifyIcon = new NotifyIcon();
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "your_icon.ico");
            notifyIcon.Icon = new System.Drawing.Icon(iconPath);
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Open", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("Exit", null, (s, e) => CloseApplication());

            notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowWindow();
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void CloseApplication()
        {
            notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            notifyIcon.Dispose();
            base.OnClosing(e);
        }

        private void AddToStartup()
        {
            string shortcutName = "MyArchivingApp.lnk";
            string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcutPath = Path.Combine(startupFolderPath, shortcutName);

            if (!System.IO.File.Exists(shortcutPath))
            {
                WshShell shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
                shortcut.Description = "My Archiving App";
                shortcut.TargetPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                shortcut.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                shortcut.Save();
            }
        }

        private void SaveFrequencySetting()
        {
            if (ArchivingFrequencyComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedFrequencyString = selectedItem.Content.ToString();

                if (int.TryParse(selectedFrequencyString, out int selectedFrequency))
                {
                    UpdateAppConfig("ArchiveFrequency", selectedFrequency.ToString());
                }
                else
                {
                    MessageBox.Show("Unable to parse the selected frequency. Please ensure it is a valid number.", "Parsing Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a valid frequency from the dropdown.", "Invalid Frequency", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveUserSettings()
        {
            // Salvează toate setările
            UpdateAppConfig("SourceDirectory", SourceDirectoryTextBox.Text);
            UpdateAppConfig("GPGKey_Archive", GPGKeyPasswordBox.Text);
            UpdateAppConfig("DestinationDirectory", DestinationDirectoryTextBox.Text);
            SaveFrequencySetting();

            // Golește câmpurile după ce au fost salvate
            GPGKeyPasswordBox.Clear();
            GPGKeyPasswordBoxRecovery.Clear();
        }


        private void UpdateAppConfig(string key, string value)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings[key].Value = value;
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private async void StartArchiving_Click(object sender, RoutedEventArgs e)
        {
            //Validate source directory
            if (string.IsNullOrWhiteSpace(SourceDirectoryTextBox.Text) || !Directory.Exists(SourceDirectoryTextBox.Text))
            {
                MessageBox.Show("The specified source directory does not exist or is invalid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //validate gnu key
            if (string.IsNullOrWhiteSpace(GPGKeyPasswordBox.Text))
            {
                MessageBox.Show("The GNU Privacy Guard Key cannot be empty. Please enter a valid key.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //Validate destination directory
            string destinationDir = DestinationDirectoryTextBox.Text;
            if (!Directory.Exists(destinationDir))
            {
                MessageBox.Show("The specified destination directory does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validate frequency setting
            if (ArchivingFrequencyComboBox.SelectedItem == null ||
                !int.TryParse((ArchivingFrequencyComboBox.SelectedItem as ComboBoxItem)?.Content.ToString(), out int frequencyInMinutes))
            {
                MessageBox.Show("Please select a valid archiving frequency.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveUserSettings();

            //PerformArchiving();
            var loadingScreen = new LoadingScreen();
            loadingScreen.Owner = this; // Set the owner of the loading screen to the main window
            loadingScreen.Show();

            try
            {
                // Perform the archiving
                await Task.Run(() => PerformArchiving());

                // Start the timer for future archiving
                //int frequencyInMinutes = int.Parse(ConfigurationManager.AppSettings["ArchiveFrequency"]);
                _archiveTimer = new Timer(frequencyInMinutes * 86400000);
                _archiveTimer.Elapsed += OnTimerElapsed;
                _archiveTimer.Start();

                // Inform the user that the archiving process started
                MessageBox.Show("Archiving process started. The application will archive the files at the specified interval.", "Archiving Started", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during the archiving process
                MessageBox.Show($"An error occurred during the archiving process: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Close the loading screen
                loadingScreen.Close();
            }
        }



        private void StartArchivingIfScheduled()
        {
            string destinationDir = ConfigurationManager.AppSettings["DestinationDirectory"];
            if (Directory.Exists(destinationDir))
            {
                int frequencyInMinutes = int.Parse(ConfigurationManager.AppSettings["ArchiveFrequency"]);
                _archiveTimer = new Timer(frequencyInMinutes * 60000);
                _archiveTimer.Elapsed += OnTimerElapsed;
                _archiveTimer.Start();
            }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(async () =>
            {
                var loadingScreen = new LoadingScreen();
                loadingScreen.Owner = this;
                loadingScreen.Show();

                try
                {
                    // Perform archiving asynchronously to keep the UI responsive
                    await Task.Run(() => PerformArchiving());
                }
                finally
                {
                    // Ensure the loading screen is closed even if an error occurs
                    loadingScreen.Close();
                }
            });
        }


        private void PerformArchiving()
        {
            string sourceDir = ConfigurationManager.AppSettings["SourceDirectory"];
            string gpgKey = ConfigurationManager.AppSettings["GPGKey_Archive"];
            string destinationDir = ConfigurationManager.AppSettings["DestinationDirectory"];

            if (!Directory.Exists(destinationDir))
            {
                MessageBox.Show("The specified destination directory does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string tempZipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
                ZipFile.CreateFromDirectory(sourceDir, tempZipPath);

                string outputFilePath = Path.Combine(destinationDir, $"{Path.GetFileName(sourceDir)}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.zip.gpg");
                EncryptFileWithGpg(tempZipPath, outputFilePath, gpgKey);

                File.Delete(tempZipPath);


                Dispatcher.Invoke(() => GPGKeyPasswordBox.Clear());

                MessageBox.Show($"Archive has been encrypted and saved to {outputFilePath}", "Encryption Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during the archiving process: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void BrowseSourceFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Select Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                string folderPath = Path.GetDirectoryName(dialog.FileName);
                SourceDirectoryTextBox.Text = folderPath;
            }
        }

        private void BrowseDestinationFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Select Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                string folderPath = Path.GetDirectoryName(dialog.FileName);
                DestinationDirectoryTextBox.Text = folderPath;
            }
        }

        private void BrowseSourceArchive_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Filter = "GPG files (*.gpg)|*.gpg|All files (*.*)|*.*",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                SourceArchiveTextBox.Text = dialog.FileName;
            }
        }

        private void BrowseDestinationFolderRecovery_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Select Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                string folderPath = Path.GetDirectoryName(dialog.FileName);
                DestinationDirectoryTextBoxRecovery.Text = folderPath;
            }
        }

        private void StartRecovery_Click(object sender, RoutedEventArgs e)
        {
            string sourceArchive = SourceArchiveTextBox.Text;
            string gpgKey = GPGKeyPasswordBoxRecovery.Text.Trim();
            string destinationDir = DestinationDirectoryTextBoxRecovery.Text;

            if (!Directory.Exists(destinationDir))
            {
                MessageBox.Show("The specified destination directory does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string tempZipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
                DecryptFileWithGpg(sourceArchive, tempZipPath, gpgKey);

                ZipFile.ExtractToDirectory(tempZipPath, destinationDir);
                MessageBox.Show($"Files have been recovered to {destinationDir}", "Recovery Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                //Dispatcher.Invoke(() => GPGKeyPasswordBoxRecovery.Clear());
                GPGKeyPasswordBoxRecovery.Clear();

                File.Delete(tempZipPath);
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show("The provided cryptographic key is incorrect. Please try again.", "Decryption Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during the recovery process: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EncryptFileWithGpg(string inputFilePath, string outputFilePath, string gpgKey)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "gpg",
                Arguments = $"--batch --yes --passphrase \"{gpgKey}\" --pinentry-mode loopback --symmetric -o \"{outputFilePath}\" \"{inputFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (var process = new Process())
            {
                process.StartInfo = processInfo;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"GPG encryption failed with the following error: {error}");
                }
            }
        }

        private void DecryptFileWithGpg(string inputFilePath, string outputFilePath, string gpgKey)
        {
            inputFilePath = $"\"{inputFilePath}\"";
            outputFilePath = $"\"{outputFilePath}\"";

            var processInfo = new ProcessStartInfo
            {
                FileName = "gpg",
                Arguments = $"--batch --yes --pinentry-mode loopback --passphrase \"{gpgKey}\" --output {outputFilePath} --decrypt {inputFilePath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (var process = new Process())
            {
                process.StartInfo = processInfo;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                string command = $"{processInfo.FileName} {processInfo.Arguments}";
                MessageBox.Show($"Executed Command: {command}", "Debug Info", MessageBoxButton.OK, MessageBoxImage.Information);

                if (!string.IsNullOrWhiteSpace(error) && (error.Contains("error") || error.Contains("failed")))
                {
                    throw new InvalidOperationException($"GPG decryption failed with the following error: {error}");
                }
            }
        }
    }
}
