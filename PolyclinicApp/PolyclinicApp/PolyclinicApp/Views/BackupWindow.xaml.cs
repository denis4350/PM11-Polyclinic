using Microsoft.Win32;
using PolyclinicApp.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace PolyclinicApp.Views
{
    public partial class BackupWindow : Window
    {
        public BackupWindow()
        {
            InitializeComponent();

            string defaultFolder = @"C:\SQLBackups";

            FolderPathTextBox.Text = defaultFolder;

            BackupPathTextBox.Text = Path.Combine(
                defaultFolder,
                "Поликлиника_backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".bak");

            AddLog("Окно резервного копирования открыто.");
            AddLog("Рекомендуемая папка: " + defaultFolder);
        }

        private void ChooseBackupPathButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "SQL Server Backup (*.bak)|*.bak";
            dialog.FileName = "Поликлиника_backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".bak";

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                BackupPathTextBox.Text = dialog.FileName;
                AddLog("Выбран путь для резервной копии: " + dialog.FileName);
            }
        }

        private void DefaultFolderButton_Click(object sender, RoutedEventArgs e)
        {
            FolderPathTextBox.Text = @"C:\SQLBackups";

            BackupPathTextBox.Text = Path.Combine(
                FolderPathTextBox.Text,
                "Поликлиника_backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".bak");

            AddLog("Установлена папка по умолчанию: " + FolderPathTextBox.Text);
        }

        private void CreateBackupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = BackupPathTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(path))
                {
                    MessageBox.Show("Укажите путь для резервной копии.");
                    return;
                }

                AddLog("Создание резервной копии...");
                AddLog("Файл: " + path);

                DatabaseBackupService.CreateBackup(path);

                AddLog("Резервная копия успешно создана.");
                MessageBox.Show("Резервная копия создана.");

                OpenFolder(path);
            }
            catch (Exception ex)
            {
                AddLog("Ошибка: " + ex.Message);
                MessageBox.Show("Ошибка при создании резервной копии: " + ex.Message);
            }
        }

        private void CreateSixBackupsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string folder = FolderPathTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(folder))
                {
                    MessageBox.Show("Укажите папку для резервных копий.");
                    return;
                }

                AddLog("Создание 6 резервных копий...");
                AddLog("Папка: " + folder);

                DatabaseBackupService.CreateSixBackups(folder);

                AddLog("6 резервных копий успешно созданы.");
                MessageBox.Show("Создано 6 резервных копий.");

                if (Directory.Exists(folder))
                {
                    Process.Start(folder);
                }
            }
            catch (Exception ex)
            {
                AddLog("Ошибка: " + ex.Message);
                MessageBox.Show("Ошибка при создании 6 копий: " + ex.Message);
            }
        }

        private void ChooseRestorePathButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "SQL Server Backup (*.bak)|*.bak";

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                RestorePathTextBox.Text = dialog.FileName;
                AddLog("Выбран файл для восстановления: " + dialog.FileName);
            }
        }

        private void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
        {
            string path = RestorePathTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show("Выберите файл резервной копии.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Восстановление заменит текущее состояние базы данных. Продолжить?",
                "Подтверждение восстановления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                AddLog("Восстановление базы данных...");
                AddLog("Файл: " + path);

                DatabaseBackupService.RestoreBackup(path);

                AddLog("База данных успешно восстановлена.");
                MessageBox.Show("База данных восстановлена. Лучше перезапустить приложение.");
            }
            catch (Exception ex)
            {
                AddLog("Ошибка: " + ex.Message);
                MessageBox.Show("Ошибка при восстановлении базы: " + ex.Message);
            }
        }

        private void AddLog(string text)
        {
            LogTextBox.AppendText(
                "[" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "] " +
                text +
                Environment.NewLine);

            LogTextBox.ScrollToEnd();
        }

        private void OpenFolder(string filePath)
        {
            string folder = Path.GetDirectoryName(filePath);

            if (Directory.Exists(folder))
            {
                Process.Start(folder);
            }
        }
    }
}