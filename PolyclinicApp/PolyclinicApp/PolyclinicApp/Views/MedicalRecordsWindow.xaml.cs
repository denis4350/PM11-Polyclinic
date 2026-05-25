using PolyclinicApp.Models;
using PolyclinicApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PolyclinicApp.Views
{
    public partial class MedicalRecordsWindow : Window
    {
        private int? _selectedRecordId;

        private List<MedicalAppointmentComboItem> _allAppointments = new List<MedicalAppointmentComboItem>();

        public MedicalRecordsWindow()
        {
            InitializeComponent();

            RecordDatePicker.SelectedDate = DateTime.Today;

            LoadAppointments();
            LoadMedicalRecords();
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
                        PatientId = p.ID_пациента,
                        PatientName = p.Фамилия + " " + p.Имя + " " + p.Отчество,
                        DoctorId = d.ID_врача,
                        DoctorName = d.Фамилия + " " + d.Имя + " " + d.Отчество,
                        Status = a.Статус
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
                    .Select(x => new MedicalAppointmentComboItem
                    {
                        Id = x.AppointmentId,
                        PatientId = x.PatientId,
                        DoctorId = x.DoctorId,
                        PatientName = x.PatientName,
                        DoctorName = x.DoctorName,
                        Name = "№" + x.AppointmentId + " / " +
                               x.AppointmentDateTime.ToString("dd.MM.yyyy HH:mm") + " / " +
                               GetAppointmentStatusText(x.Status) + " / " +
                               x.PatientName + " / " + x.DoctorName
                    })
                    .ToList();

                AppointmentComboBox.ItemsSource = _allAppointments;
            }
        }

        private void LoadMedicalRecords()
        {
            using (var db = new ПоликлиникаEntities1())
            {
                string search = SearchTextBox.Text.Trim();

                var query =
                    from m in db.Медицинские_записи
                    join p in db.Пациенты
                        on m.ID_пациента equals p.ID_пациента
                    join d in db.Врачи
                        on m.ID_врача equals d.ID_врача
                    select new
                    {
                        RecordId = m.ID_записи,
                        RecordDate = m.Дата_время,
                        Content = m.Содержание,
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
                        x.PatientName.Contains(search) ||
                        x.DoctorName.Contains(search) ||
                        x.Content.Contains(search));
                }

                MedicalRecordsListView.ItemsSource = query
                    .OrderByDescending(x => x.RecordDate)
                    .Take(800)
                    .ToList()
                    .Select(x => new MedicalRecordListItem
                    {
                        RecordId = x.RecordId,
                        RecordDate = x.RecordDate,
                        DateText = x.RecordDate.ToString("dd.MM.yyyy HH:mm"),
                        Content = x.Content,
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
            MedicalAppointmentComboItem selected = AppointmentComboBox.SelectedItem as MedicalAppointmentComboItem;

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

            MedicalAppointmentComboItem appointment = AppointmentComboBox.SelectedItem as MedicalAppointmentComboItem;

            if (appointment == null)
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    int newId = 1;

                    if (db.Медицинские_записи.Any())
                    {
                        newId = db.Медицинские_записи.Max(m => m.ID_записи) + 1;
                    }

                    Медицинские_записи record = new Медицинские_записи();

                    record.ID_записи = newId;
                    record.Дата_время = RecordDatePicker.SelectedDate.Value;
                    record.Содержание = ContentTextBox.Text.Trim();
                    record.ID_пациента = appointment.PatientId;
                    record.ID_врача = appointment.DoctorId;

                    db.Медицинские_записи.Add(record);
                    db.SaveChanges();
                }

                LoadMedicalRecords();
                ClearFields();

                MessageBox.Show("Медицинская запись добавлена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении медицинской записи: " + ex.Message);
            }
            if (!AccessRules.CanEditMedicalRecords())
            {
                MessageBox.Show("У вашей роли нет прав на изменение медицинских записей.");
                return;
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRecordId == null)
            {
                MessageBox.Show("Выберите медицинскую запись для изменения.");
                return;
            }

            if (!ValidateFields())
                return;

            MedicalAppointmentComboItem appointment = AppointmentComboBox.SelectedItem as MedicalAppointmentComboItem;

            if (appointment == null)
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Медицинские_записи record = db.Медицинские_записи
                        .FirstOrDefault(m => m.ID_записи == _selectedRecordId.Value);

                    if (record == null)
                    {
                        MessageBox.Show("Медицинская запись не найдена.");
                        return;
                    }

                    record.Дата_время = RecordDatePicker.SelectedDate.Value;
                    record.Содержание = ContentTextBox.Text.Trim();
                    record.ID_пациента = appointment.PatientId;
                    record.ID_врача = appointment.DoctorId;

                    db.SaveChanges();
                }

                LoadMedicalRecords();
                ClearFields();

                MessageBox.Show("Медицинская запись изменена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при изменении медицинской записи: " + ex.Message);
            }
            if (!AccessRules.CanEditMedicalRecords())
            {
                MessageBox.Show("У вашей роли нет прав на изменение медицинских записей.");
                return;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRecordId == null)
            {
                MessageBox.Show("Выберите медицинскую запись для удаления.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Удалить выбранную медицинскую запись?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Медицинские_записи record = db.Медицинские_записи
                        .FirstOrDefault(m => m.ID_записи == _selectedRecordId.Value);

                    if (record == null)
                    {
                        MessageBox.Show("Медицинская запись не найдена.");
                        return;
                    }

                    db.Медицинские_записи.Remove(record);
                    db.SaveChanges();
                }

                LoadMedicalRecords();
                ClearFields();

                MessageBox.Show("Медицинская запись удалена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении медицинской записи: " + ex.Message);
            }
            if (!AccessRules.CanEditMedicalRecords())
            {
                MessageBox.Show("У вашей роли нет прав на изменение медицинских записей.");
                return;
            }
        }

        private void MedicalRecordsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MedicalRecordListItem selected = MedicalRecordsListView.SelectedItem as MedicalRecordListItem;

            if (selected == null)
                return;

            _selectedRecordId = selected.RecordId;

            RecordDatePicker.SelectedDate = selected.RecordDate;
            ContentTextBox.Text = selected.Content;

            AppointmentComboBox.ItemsSource = _allAppointments;

            MedicalAppointmentComboItem appointment = _allAppointments
                .FirstOrDefault(a =>
                    a.PatientId == selected.PatientId &&
                    a.DoctorId == selected.DoctorId);

            if (appointment != null)
            {
                AppointmentComboBox.SelectedValue = appointment.Id;
            }
            else
            {
                AppointmentComboBox.SelectedIndex = -1;
            }

            SelectedInfoTextBlock.Text =
                "Пациент: " + selected.PatientName +
                " | Врач: " + selected.DoctorName;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadMedicalRecords();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAppointments();
            LoadMedicalRecords();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void ClearFields()
        {
            _selectedRecordId = null;

            AppointmentSearchTextBox.Clear();

            AppointmentComboBox.ItemsSource = _allAppointments;
            AppointmentComboBox.SelectedIndex = -1;

            RecordDatePicker.SelectedDate = DateTime.Today;

            ContentTextBox.Clear();

            MedicalRecordsListView.SelectedItem = null;

            SelectedInfoTextBlock.Text = "Пациент и врач не выбраны";
        }

        private bool ValidateFields()
        {
            if (AppointmentComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите приём.");
                return false;
            }

            if (RecordDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату медицинской записи.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(ContentTextBox.Text))
            {
                MessageBox.Show("Введите содержание медицинской записи.");
                return false;
            }

            return true;
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

    public class MedicalRecordListItem
    {
        public int RecordId { get; set; }

        public DateTime RecordDate { get; set; }
        public string DateText { get; set; }

        public string Content { get; set; }

        public int PatientId { get; set; }
        public string PatientName { get; set; }

        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
    }

    public class MedicalAppointmentComboItem
    {
        public int Id { get; set; }

        public int PatientId { get; set; }
        public string PatientName { get; set; }

        public int DoctorId { get; set; }
        public string DoctorName { get; set; }

        public string Name { get; set; }
    }
}