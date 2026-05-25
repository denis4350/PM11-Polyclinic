using PolyclinicApp.Services;
using PolyclinicApp.Views;
using System.Windows;

namespace PolyclinicApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            UserTextBlock.Text = "Пользователь: " + CurrentUser.Login +
                          " | Роль: " + CurrentUser.RoleName;

            ApplyAccessRights();
        }

        private void ApplyAccessRights()
        {
            HideAllButtons();

            // Администратор:
            // пользователи, журнал аудита, резервные копии, отчёты
            if (CurrentUser.IsAdmin)
            {
                UsersButton.Visibility = Visibility.Visible;
                AuditButton.Visibility = Visibility.Visible;
                BackupButton.Visibility = Visibility.Visible;
                ReportsButton.Visibility = Visibility.Visible;
                ExitButton.Visibility = Visibility.Visible;

                UserTextBlock.Text = "Пользователь: " + CurrentUser.Login + " | Роль: Администратор";
                return;
            }

            // Главный врач:
            // все медицинские разделы, кроме пользователей и резервного копирования
            if (CurrentUser.IsChiefDoctor)
            {
                PatientsButton.Visibility = Visibility.Visible;
                DoctorsButton.Visibility = Visibility.Visible;
                AppointmentsButton.Visibility = Visibility.Visible;
                LaboratoryButton.Visibility = Visibility.Visible;
                MedicalRecordsButton.Visibility = Visibility.Visible;
                PrescriptionsButton.Visibility = Visibility.Visible;
                ReferralsButton.Visibility = Visibility.Visible;
                ScheduleButton.Visibility = Visibility.Visible;
                CabinetsButton.Visibility = Visibility.Visible;
                SpecialitiesButton.Visibility = Visibility.Visible;
                DiagnosesButton.Visibility = Visibility.Visible;
                AuditButton.Visibility = Visibility.Visible;
                ReportsButton.Visibility = Visibility.Visible;
                ExitButton.Visibility = Visibility.Visible;

                UserTextBlock.Text = "Пользователь: " + CurrentUser.Login + " | Роль: Главный врач";
                return;
            }

            // Врач:
            // свои приёмы, своё расписание, мед. записи, назначения, направления
            if (CurrentUser.IsDoctor)
            {
                PatientsButton.Visibility = Visibility.Visible;
                AppointmentsButton.Visibility = Visibility.Visible;
                LaboratoryButton.Visibility = Visibility.Visible;
                MedicalRecordsButton.Visibility = Visibility.Visible;
                PrescriptionsButton.Visibility = Visibility.Visible;
                ReferralsButton.Visibility = Visibility.Visible;
                ScheduleButton.Visibility = Visibility.Visible;
                DiagnosesButton.Visibility = Visibility.Visible;
                ReportsButton.Visibility = Visibility.Visible;
                ExitButton.Visibility = Visibility.Visible;

                UserTextBlock.Text = "Пользователь: " + CurrentUser.Login + " | Роль: Врач";
                return;
            }

            // Лаборант:
            // лаборатория, пациенты просмотр, врачи просмотр, приёмы просмотр, отчёты
            if (CurrentUser.IsLaborant)
            {
                PatientsButton.Visibility = Visibility.Visible;
                DoctorsButton.Visibility = Visibility.Visible;
                AppointmentsButton.Visibility = Visibility.Visible;
                LaboratoryButton.Visibility = Visibility.Visible;
                ScheduleButton.Visibility = Visibility.Visible;
                CabinetsButton.Visibility = Visibility.Visible;
                ReportsButton.Visibility = Visibility.Visible;
                ExitButton.Visibility = Visibility.Visible;

                UserTextBlock.Text = "Пользователь: " + CurrentUser.Login + " | Роль: Лаборант";
                return;
            }

            // Регистратор:
            // пациенты, запись на приём, направления, расписание, кабинеты, отчёты
            if (CurrentUser.IsRegistrar)
            {
                PatientsButton.Visibility = Visibility.Visible;
                DoctorsButton.Visibility = Visibility.Visible;
                AppointmentsButton.Visibility = Visibility.Visible;
                ReferralsButton.Visibility = Visibility.Visible;
                ScheduleButton.Visibility = Visibility.Visible;
                CabinetsButton.Visibility = Visibility.Visible;
                ReportsButton.Visibility = Visibility.Visible;
                ExitButton.Visibility = Visibility.Visible;

                UserTextBlock.Text = "Пользователь: " + CurrentUser.Login + " | Роль: Регистратор";
                return;
            }

            ExitButton.Visibility = Visibility.Visible;
            UserTextBlock.Text = "Пользователь: " + CurrentUser.Login + " | Роль: неизвестна";
        }

        private void HideAllButtons()
        {
            PatientsButton.Visibility = Visibility.Collapsed;
            DoctorsButton.Visibility = Visibility.Collapsed;
            AppointmentsButton.Visibility = Visibility.Collapsed;
            LaboratoryButton.Visibility = Visibility.Collapsed;
            MedicalRecordsButton.Visibility = Visibility.Collapsed;
            PrescriptionsButton.Visibility = Visibility.Collapsed;
            ReferralsButton.Visibility = Visibility.Collapsed;
            ScheduleButton.Visibility = Visibility.Collapsed;
            CabinetsButton.Visibility = Visibility.Collapsed;
            SpecialitiesButton.Visibility = Visibility.Collapsed;
            DiagnosesButton.Visibility = Visibility.Collapsed;
            UsersButton.Visibility = Visibility.Collapsed;
            AuditButton.Visibility = Visibility.Collapsed;
            ReportsButton.Visibility = Visibility.Collapsed;
            BackupButton.Visibility = Visibility.Collapsed;
            ExitButton.Visibility = Visibility.Collapsed;
        }

        private void PatientsButton_Click(object sender, RoutedEventArgs e)
        {
            PatientsWindow window = new PatientsWindow();
            window.ShowDialog();
        }

        private void DoctorsButton_Click(object sender, RoutedEventArgs e)
        {
            DoctorsWindow window = new DoctorsWindow();
            window.ShowDialog();
        }

        private void AppointmentsButton_Click(object sender, RoutedEventArgs e)
        {
            AppointmentsWindow window = new AppointmentsWindow();
            window.ShowDialog();
        }

        private void LaboratoryButton_Click(object sender, RoutedEventArgs e)
        {
            LaboratoryWindow window = new LaboratoryWindow();
            window.ShowDialog();
        }

        private void MedicalRecordsButton_Click(object sender, RoutedEventArgs e)
        {
            MedicalRecordsWindow window = new MedicalRecordsWindow();
            window.ShowDialog();
        }

        private void PrescriptionsButton_Click(object sender, RoutedEventArgs e)
        {
            PrescriptionsWindow window = new PrescriptionsWindow();
            window.ShowDialog();
        }

        private void ReferralsButton_Click(object sender, RoutedEventArgs e)
        {
            ReferralsWindow window = new ReferralsWindow();
            window.ShowDialog();
        }

        private void ScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            ScheduleWindow window = new ScheduleWindow();
            window.ShowDialog();
        }

        private void CabinetsButton_Click(object sender, RoutedEventArgs e)
        {
            CabinetsWindow window = new CabinetsWindow();
            window.ShowDialog();
        }

        private void SpecialitiesButton_Click(object sender, RoutedEventArgs e)
        {
            SpecialitiesWindow window = new SpecialitiesWindow();
            window.ShowDialog();
        }

        private void DiagnosesButton_Click(object sender, RoutedEventArgs e)
        {
            DiagnosesWindow window = new DiagnosesWindow();
            window.ShowDialog();
        }

        private void UsersButton_Click(object sender, RoutedEventArgs e)
        {
            UsersWindow window = new UsersWindow();
            window.ShowDialog();
        }

        private void AuditButton_Click(object sender, RoutedEventArgs e)
        {
            AuditWindow window = new AuditWindow();
            window.ShowDialog();
        }

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {
            ReportsWindow window = new ReportsWindow();
            window.ShowDialog();
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            BackupWindow window = new BackupWindow();
            window.ShowDialog();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();

            Close();
        }
    }
}