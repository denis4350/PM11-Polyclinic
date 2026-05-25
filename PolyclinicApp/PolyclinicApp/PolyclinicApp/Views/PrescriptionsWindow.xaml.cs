using PolyclinicApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PolyclinicApp.Services;

namespace PolyclinicApp.Views
{
    public partial class PrescriptionsWindow : Window
    {
        private int? _selectedPrescriptionId;

        private List<PrescriptionAppointmentComboItem> _allAppointments =
            new List<PrescriptionAppointmentComboItem>();

        public PrescriptionsWindow()
        {
            InitializeComponent();

            LoadComboBoxes();
            LoadPrescriptions();
        }

        private void LoadComboBoxes()
        {
            LoadAppointments();

            MedicineComboBox.ItemsSource = new List<string>
            {
                "Парацетамол",
                "Ибупрофен",
                "Амоксициллин",
                "Лоратадин",
                "Лизиноприл",
                "Омепразол",
                "Дротаверин",
                "Амброксол",
                "Метформин",
                "Цетиризин",
                "Аскорбиновая кислота",
                "Витамин D"
            };

            DosageComboBox.ItemsSource = new List<string>
            {
                "1 таблетка 1 раз в день",
                "1 таблетка 2 раза в день",
                "1 таблетка 3 раза в день",
                "5 мл 2 раза в день",
                "10 мл 2 раза в день",
                "По назначению врача"
            };

            PeriodComboBox.ItemsSource = new List<string>
            {
                "5 дней",
                "7 дней",
                "10 дней",
                "14 дней",
                "1 месяц",
                "До повторного приёма"
            };

            StatusComboBox.ItemsSource = new List<PrescriptionStatusItem>
            {
                new PrescriptionStatusItem { Id = 0, Name = "Активно" },
                new PrescriptionStatusItem { Id = 1, Name = "Завершено" },
                new PrescriptionStatusItem { Id = 2, Name = "Отменено" }
            };

            StatusComboBox.SelectedValue = 0;
        }

        private void LoadAppointments()
        {
            using (var db = new ПоликлиникаEntities1())
            {
                var query =
                    from a in db.Приёмы
                    join p in db.Пациенты
                        on a.ID_пациента equals p.ID_пациента
                    join d in db.Врачи
                        on a.ID_врача equals d.ID_врача
                    where a.Статус != 4
                    orderby a.Дата_время descending
                    select new
                    {
                        AppointmentId = a.ID_приёма,
                        AppointmentDateTime = a.Дата_время,
                        AppointmentStatus = a.Статус,
                        PatientId = p.ID_пациента,
                        PatientName = p.Фамилия + " " + p.Имя + " " + p.Отчество,
                        DoctorId = d.ID_врача,
                        DoctorName = d.Фамилия + " " + d.Имя + " " + d.Отчество
                    };

                // Если вошёл врач — показываем только его приёмы
                if (CurrentUser.IsDoctor && CurrentUser.DoctorId != null)
                {
                    int doctorId = CurrentUser.DoctorId.Value;
                    query = query.Where(x => x.DoctorId == doctorId);
                }

                _allAppointments = query
                    .Take(1000)
                    .ToList()
                    .Select(x => new PrescriptionAppointmentComboItem
                    {
                        Id = x.AppointmentId,
                        PatientId = x.PatientId,
                        DoctorId = x.DoctorId,
                        PatientName = x.PatientName,
                        DoctorName = x.DoctorName,
                        AppointmentDateTime = x.AppointmentDateTime,
                        Name = "№" + x.AppointmentId + " / " +
                               x.AppointmentDateTime.ToString("dd.MM.yyyy HH:mm") + " / " +
                               GetAppointmentStatusText(x.AppointmentStatus) + " / " +
                               x.PatientName + " / " + x.DoctorName
                    })
                    .ToList();

                AppointmentComboBox.ItemsSource = _allAppointments;
            }
        }

        private void LoadPrescriptions()
        {
            using (var db = new ПоликлиникаEntities1())
            {
                string search = SearchTextBox.Text.Trim();

                var query =
                    from pr in db.Лекарственные_назначения
                    join p in db.Пациенты
                        on pr.ID_пациента equals p.ID_пациента
                    join d in db.Врачи
                        on pr.ID_врача equals d.ID_врача
                    select new
                    {
                        PrescriptionId = pr.ID_назначения,
                        Medicine = pr.Лекарство,
                        Dosage = pr.Дозировка,
                        Period = pr.Период_приёма,
                        Status = pr.Статус,
                        AppointmentId = pr.ID_приёма,
                        PatientId = p.ID_пациента,
                        PatientName = p.Фамилия + " " + p.Имя + " " + p.Отчество,
                        DoctorId = d.ID_врача,
                        DoctorName = d.Фамилия + " " + d.Имя + " " + d.Отчество
                    };
                if (CurrentUser.IsDoctor && CurrentUser.DoctorId != null)
                {
                    int doctorId = CurrentUser.DoctorId.Value;
                    query = query.Where(x => x.DoctorId == doctorId);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(x =>
                        x.Medicine.Contains(search) ||
                        x.Dosage.Contains(search) ||
                        x.PatientName.Contains(search) ||
                        x.DoctorName.Contains(search) ||
                        (x.Period != null && x.Period.Contains(search)));
                }

                PrescriptionsListView.ItemsSource = query
                    .OrderByDescending(x => x.PrescriptionId)
                    .Take(800)
                    .ToList()
                    .Select(x => new PrescriptionListItem
                    {
                        PrescriptionId = x.PrescriptionId,
                        Medicine = x.Medicine,
                        Dosage = x.Dosage,
                        Period = x.Period,
                        Status = x.Status,
                        StatusText = GetPrescriptionStatusText(x.Status),
                        AppointmentId = x.AppointmentId,
                        AppointmentText = x.AppointmentId == null
                            ? ""
                            : "Приём №" + x.AppointmentId.Value,
                        PatientId = x.PatientId,
                        PatientName = x.PatientName,
                        DoctorId = x.DoctorId,
                        DoctorName = x.DoctorName
                    })
                    .ToList();
            }
        }

        private void AppointmentSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = AppointmentSearchTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(search))
            {
                AppointmentComboBox.ItemsSource = _allAppointments;
                return;
            }

            AppointmentComboBox.ItemsSource = _allAppointments
                .Where(a =>
                    a.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    a.PatientName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    a.DoctorName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            AppointmentComboBox.IsDropDownOpen = true;
        }

        private void AppointmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PrescriptionAppointmentComboItem selected =
                AppointmentComboBox.SelectedItem as PrescriptionAppointmentComboItem;

            if (selected == null)
            {
                SelectedInfoTextBlock.Text = "Пациент и врач не выбраны";
                return;
            }

            SelectedInfoTextBlock.Text =
                "Пациент: " + selected.PatientName +
                " | Врач: " + selected.DoctorName;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
                return;

            PrescriptionAppointmentComboItem appointment =
                AppointmentComboBox.SelectedItem as PrescriptionAppointmentComboItem;

            if (appointment == null)
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    int newId = 1;

                    if (db.Лекарственные_назначения.Any())
                    {
                        newId = db.Лекарственные_назначения.Max(x => x.ID_назначения) + 1;
                    }

                    Лекарственные_назначения prescription =
                        new Лекарственные_назначения();

                    prescription.ID_назначения = newId;
                    prescription.Лекарство = MedicineComboBox.Text.Trim();
                    prescription.Дозировка = DosageComboBox.Text.Trim();
                    prescription.Период_приёма = PeriodComboBox.Text.Trim();
                    prescription.Статус = Convert.ToInt32(StatusComboBox.SelectedValue);
                    prescription.ID_приёма = appointment.Id;
                    prescription.ID_пациента = appointment.PatientId;
                    prescription.ID_врача = appointment.DoctorId;

                    db.Лекарственные_назначения.Add(prescription);
                    db.SaveChanges();
                }

                LoadPrescriptions();
                ClearFields();

                MessageBox.Show("Лекарственное назначение добавлено.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении назначения: " + ex.Message);
            }
            if (!AccessRules.CanEditPrescriptions())
            {
                MessageBox.Show("У вашей роли нет прав на изменение лекарственных назначений.");
                return;
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPrescriptionId == null)
            {
                MessageBox.Show("Выберите назначение для изменения.");
                return;
            }

            if (!ValidateFields())
                return;

            PrescriptionAppointmentComboItem appointment =
                AppointmentComboBox.SelectedItem as PrescriptionAppointmentComboItem;

            if (appointment == null)
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Лекарственные_назначения prescription =
                        db.Лекарственные_назначения
                            .FirstOrDefault(x => x.ID_назначения == _selectedPrescriptionId.Value);

                    if (prescription == null)
                    {
                        MessageBox.Show("Назначение не найдено.");
                        return;
                    }

                    prescription.Лекарство = MedicineComboBox.Text.Trim();
                    prescription.Дозировка = DosageComboBox.Text.Trim();
                    prescription.Период_приёма = PeriodComboBox.Text.Trim();
                    prescription.Статус = Convert.ToInt32(StatusComboBox.SelectedValue);
                    prescription.ID_приёма = appointment.Id;
                    prescription.ID_пациента = appointment.PatientId;
                    prescription.ID_врача = appointment.DoctorId;

                    db.SaveChanges();
                }

                LoadPrescriptions();
                ClearFields();

                MessageBox.Show("Лекарственное назначение изменено.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при изменении назначения: " + ex.Message);
            }
            if (!AccessRules.CanEditPrescriptions())
            {
                MessageBox.Show("У вашей роли нет прав на изменение лекарственных назначений.");
                return;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPrescriptionId == null)
            {
                MessageBox.Show("Выберите назначение для удаления.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Удалить выбранное лекарственное назначение?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Лекарственные_назначения prescription =
                        db.Лекарственные_назначения
                            .FirstOrDefault(x => x.ID_назначения == _selectedPrescriptionId.Value);

                    if (prescription == null)
                    {
                        MessageBox.Show("Назначение не найдено.");
                        return;
                    }

                    db.Лекарственные_назначения.Remove(prescription);
                    db.SaveChanges();
                }

                LoadPrescriptions();
                ClearFields();

                MessageBox.Show("Лекарственное назначение удалено.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении назначения: " + ex.Message);
            }
            if (!AccessRules.CanEditPrescriptions())
            {
                MessageBox.Show("У вашей роли нет прав на изменение лекарственных назначений.");
                return;
            }
        }

        private void PrescriptionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PrescriptionListItem selected =
                PrescriptionsListView.SelectedItem as PrescriptionListItem;

            if (selected == null)
                return;

            _selectedPrescriptionId = selected.PrescriptionId;

            AppointmentComboBox.ItemsSource = _allAppointments;

            if (selected.AppointmentId != null)
            {
                AppointmentComboBox.SelectedValue = selected.AppointmentId.Value;
            }
            else
            {
                AppointmentComboBox.SelectedIndex = -1;
            }

            MedicineComboBox.Text = selected.Medicine;
            DosageComboBox.Text = selected.Dosage;
            PeriodComboBox.Text = selected.Period;
            StatusComboBox.SelectedValue = selected.Status;

            SelectedInfoTextBlock.Text =
                "Пациент: " + selected.PatientName +
                " | Врач: " + selected.DoctorName;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadPrescriptions();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAppointments();
            LoadPrescriptions();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void ClearFields()
        {
            _selectedPrescriptionId = null;

            AppointmentSearchTextBox.Clear();

            AppointmentComboBox.ItemsSource = _allAppointments;
            AppointmentComboBox.SelectedIndex = -1;

            MedicineComboBox.SelectedIndex = -1;
            MedicineComboBox.Text = "";

            DosageComboBox.SelectedIndex = -1;
            DosageComboBox.Text = "";

            PeriodComboBox.SelectedIndex = -1;
            PeriodComboBox.Text = "";

            StatusComboBox.SelectedValue = 0;

            PrescriptionsListView.SelectedItem = null;

            SelectedInfoTextBlock.Text = "Пациент и врач не выбраны";
        }

        private bool ValidateFields()
        {
            if (AppointmentComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите приём.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(MedicineComboBox.Text))
            {
                MessageBox.Show("Введите лекарство.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(DosageComboBox.Text))
            {
                MessageBox.Show("Введите дозировку.");
                return false;
            }

            if (StatusComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите статус назначения.");
                return false;
            }

            return true;
        }

        private static string GetPrescriptionStatusText(int status)
        {
            switch (status)
            {
                case 0:
                    return "Активно";
                case 1:
                    return "Завершено";
                case 2:
                    return "Отменено";
                default:
                    return "Неизвестно";
            }
        }

        private static string GetAppointmentStatusText(int status)
        {
            switch (status)
            {
                case 0:
                    return "Назначен";
                case 1:
                    return "В очереди";
                case 2:
                    return "Принят";
                case 3:
                    return "Завершён";
                case 4:
                    return "Отменён";
                default:
                    return "Неизвестно";
            }
        }
    }

    public class PrescriptionListItem
    {
        public int PrescriptionId { get; set; }

        public string Medicine { get; set; }
        public string Dosage { get; set; }
        public string Period { get; set; }

        public int Status { get; set; }
        public string StatusText { get; set; }

        public int? AppointmentId { get; set; }
        public string AppointmentText { get; set; }

        public int PatientId { get; set; }
        public string PatientName { get; set; }

        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
    }

    public class PrescriptionAppointmentComboItem
    {
        public int Id { get; set; }

        public DateTime AppointmentDateTime { get; set; }

        public int PatientId { get; set; }
        public string PatientName { get; set; }

        public int DoctorId { get; set; }
        public string DoctorName { get; set; }

        public string Name { get; set; }
    }

    public class PrescriptionStatusItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}