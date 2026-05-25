using PolyclinicApp.Models;
using PolyclinicApp.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PolyclinicApp.Services;

namespace PolyclinicApp.Views
{
    public partial class LaboratoryWindow : Window
    {
        private int? _selectedResearchId;

        private List<AppointmentComboItem> _allAppointments = new List<AppointmentComboItem>();

        public LaboratoryWindow()
        {
            InitializeComponent();

            LoadComboBoxes();
            LoadLaboratoryResearches();
        }

        private void LoadComboBoxes()
        {
            LoadAppointments();

            AnalysisTypeComboBox.ItemsSource = new List<string>
            {
                "Общий анализ крови",
                "Биохимический анализ крови",
                "Общий анализ мочи",
                "ЭКГ",
                "УЗИ органов брюшной полости",
                "Флюорография",
                "Анализ крови на глюкозу",
                "Исследование уровня холестерина",
                "Анализ крови на гормоны",
                "Мазок на микрофлору"
            };

            StatusComboBox.ItemsSource = new List<ComboItem>
            {
                new ComboItem { Id = 0, Name = "Назначено" },
                new ComboItem { Id = 1, Name = "Выполнено" },
                new ComboItem { Id = 2, Name = "Отменено" }
            };

            StatusComboBox.SelectedValue = 0;
            OrderDatePicker.SelectedDate = DateTime.Today;
        }

        private void LoadAppointments()
        {
            using (var db = new ПоликлиникаEntities1())
            {
                _allAppointments =
                    (from a in db.Приёмы
                     join p in db.Пациенты
                        on a.ID_пациента equals p.ID_пациента
                     join d in db.Врачи
                        on a.ID_врача equals d.ID_врача
                     orderby a.Дата_время descending
                     select new
                     {
                         AppointmentId = a.ID_приёма,
                         DateTime = a.Дата_время,
                         PatientId = p.ID_пациента,
                         PatientName = p.Фамилия + " " + p.Имя + " " + p.Отчество,
                         DoctorId = d.ID_врача,
                         DoctorName = d.Фамилия + " " + d.Имя + " " + d.Отчество
                     })
                    .Take(800)
                    .ToList()
                    .Select(x => new AppointmentComboItem
                    {
                        Id = x.AppointmentId,
                        PatientId = x.PatientId,
                        DoctorId = x.DoctorId,
                        PatientName = x.PatientName,
                        DoctorName = x.DoctorName,
                        Name = "№" + x.AppointmentId + " / " +
                               x.DateTime.ToString("dd.MM.yyyy HH:mm") + " / " +
                               x.PatientName + " / " + x.DoctorName
                    })
                    .ToList();

                AppointmentComboBox.ItemsSource = _allAppointments;
            }
        }

        private void LoadLaboratoryResearches()
        {
            using (var db = new ПоликлиникаEntities1())
            {
                string search = SearchTextBox.Text.Trim();

                var query =
                    from l in db.Лабораторные_исследования
                    join p in db.Пациенты
                        on l.ID_пациента equals p.ID_пациента
                    join d in db.Врачи
                        on l.ID_врача equals d.ID_врача
                    select new
                    {
                        ResearchId = l.ID_исследования,
                        AnalysisType = l.Тип_анализа,
                        OrderDate = l.Дата_назначения,
                        CompletionDate = l.Дата_выполнения,
                        Result = l.Результат,
                        Status = l.Статус,
                        AppointmentId = l.ID_приёма,
                        PatientId = l.ID_пациента,
                        DoctorId = l.ID_врача,
                        PatientName = p.Фамилия + " " + p.Имя + " " + p.Отчество,
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
                        x.AnalysisType.Contains(search) ||
                        x.PatientName.Contains(search) ||
                        x.DoctorName.Contains(search) ||
                        x.Result.Contains(search));
                }

                LaboratoryListView.ItemsSource = query
                    .OrderByDescending(x => x.OrderDate)
                    .Take(700)
                    .ToList()
                    .Select(x => new LaboratoryListItem
                    {
                        ResearchId = x.ResearchId,
                        AnalysisType = x.AnalysisType,
                        OrderDate = x.OrderDate,
                        OrderDateText = x.OrderDate.ToString("dd.MM.yyyy"),
                        CompletionDate = x.CompletionDate,
                        CompletionDateText = x.CompletionDate == null
                            ? ""
                            : x.CompletionDate.Value.ToString("dd.MM.yyyy"),
                        Result = x.Result,
                        Status = x.Status,
                        StatusText = GetStatusText(x.Status),
                        AppointmentId = x.AppointmentId,
                        PatientId = x.PatientId,
                        DoctorId = x.DoctorId,
                        PatientName = x.PatientName,
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
            AppointmentComboItem selected = AppointmentComboBox.SelectedItem as AppointmentComboItem;

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

            AppointmentComboItem appointment = AppointmentComboBox.SelectedItem as AppointmentComboItem;

            if (appointment == null)
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    int newId = 1;

                    if (db.Лабораторные_исследования.Any())
                    {
                        newId = db.Лабораторные_исследования.Max(l => l.ID_исследования) + 1;
                    }

                    Лабораторные_исследования research = new Лабораторные_исследования();

                    research.ID_исследования = newId;
                    research.Тип_анализа = AnalysisTypeComboBox.Text.Trim();
                    research.Дата_назначения = OrderDatePicker.SelectedDate.Value;
                    research.Дата_выполнения = CompletionDatePicker.SelectedDate;
                    research.Результат = ResultTextBox.Text.Trim();
                    research.Статус = Convert.ToInt32(StatusComboBox.SelectedValue);
                    research.ID_приёма = appointment.Id;
                    research.ID_пациента = appointment.PatientId;
                    research.ID_врача = appointment.DoctorId;

                    db.Лабораторные_исследования.Add(research);
                    db.SaveChanges();
                }

                LoadLaboratoryResearches();
                ClearFields();

                MessageBox.Show("Лабораторное исследование добавлено.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении исследования: " + ex.Message);
            }
            if (!AccessRules.CanEditLaboratory())
            {
                MessageBox.Show("У вашей роли нет прав на изменение лабораторных исследований.");
                return;
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedResearchId == null)
            {
                MessageBox.Show("Выберите исследование для изменения.");
                return;
            }

            if (!ValidateFields())
                return;

            AppointmentComboItem appointment = AppointmentComboBox.SelectedItem as AppointmentComboItem;

            if (appointment == null)
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Лабораторные_исследования research = db.Лабораторные_исследования
                        .FirstOrDefault(l => l.ID_исследования == _selectedResearchId.Value);

                    if (research == null)
                    {
                        MessageBox.Show("Исследование не найдено.");
                        return;
                    }

                    research.Тип_анализа = AnalysisTypeComboBox.Text.Trim();
                    research.Дата_назначения = OrderDatePicker.SelectedDate.Value;
                    research.Дата_выполнения = CompletionDatePicker.SelectedDate;
                    research.Результат = ResultTextBox.Text.Trim();
                    research.Статус = Convert.ToInt32(StatusComboBox.SelectedValue);
                    research.ID_приёма = appointment.Id;
                    research.ID_пациента = appointment.PatientId;
                    research.ID_врача = appointment.DoctorId;

                    db.SaveChanges();
                }

                LoadLaboratoryResearches();
                ClearFields();

                MessageBox.Show("Исследование изменено.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при изменении исследования: " + ex.Message);
            }
            if (!AccessRules.CanEditLaboratory())
            {
                MessageBox.Show("У вашей роли нет прав на изменение лабораторных исследований.");
                return;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedResearchId == null)
            {
                MessageBox.Show("Выберите исследование для удаления.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Удалить выбранное лабораторное исследование?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Лабораторные_исследования research = db.Лабораторные_исследования
                        .FirstOrDefault(l => l.ID_исследования == _selectedResearchId.Value);

                    if (research == null)
                    {
                        MessageBox.Show("Исследование не найдено.");
                        return;
                    }

                    db.Лабораторные_исследования.Remove(research);
                    db.SaveChanges();
                }

                LoadLaboratoryResearches();
                ClearFields();

                MessageBox.Show("Исследование удалено.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении исследования: " + ex.Message);
            }
            if (!AccessRules.CanEditLaboratory())
            {
                MessageBox.Show("У вашей роли нет прав на изменение лабораторных исследований.");
                return;
            }
        }

        private void LaboratoryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LaboratoryListItem selected = LaboratoryListView.SelectedItem as LaboratoryListItem;

            if (selected == null)
                return;

            _selectedResearchId = selected.ResearchId;

            AppointmentComboBox.ItemsSource = _allAppointments;
            AppointmentComboBox.SelectedValue = selected.AppointmentId;

            AnalysisTypeComboBox.Text = selected.AnalysisType;
            OrderDatePicker.SelectedDate = selected.OrderDate;
            CompletionDatePicker.SelectedDate = selected.CompletionDate;
            StatusComboBox.SelectedValue = selected.Status;
            ResultTextBox.Text = selected.Result;

            SelectedInfoTextBlock.Text =
                "Пациент: " + selected.PatientName +
                " | Врач: " + selected.DoctorName;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadLaboratoryResearches();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAppointments();
            LoadLaboratoryResearches();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void ClearFields()
        {
            _selectedResearchId = null;

            AppointmentSearchTextBox.Clear();

            AppointmentComboBox.ItemsSource = _allAppointments;
            AppointmentComboBox.SelectedIndex = -1;

            AnalysisTypeComboBox.SelectedIndex = -1;
            AnalysisTypeComboBox.Text = "";

            OrderDatePicker.SelectedDate = DateTime.Today;
            CompletionDatePicker.SelectedDate = null;

            StatusComboBox.SelectedValue = 0;

            ResultTextBox.Clear();

            LaboratoryListView.SelectedItem = null;

            SelectedInfoTextBlock.Text = "Пациент и врач не выбраны";
        }

        private bool ValidateFields()
        {
            if (AppointmentComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите приём.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(AnalysisTypeComboBox.Text))
            {
                MessageBox.Show("Выберите или введите тип анализа.");
                return false;
            }

            if (OrderDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату назначения.");
                return false;
            }

            if (StatusComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите статус исследования.");
                return false;
            }

            int status = Convert.ToInt32(StatusComboBox.SelectedValue);

            if (status == 1 && CompletionDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Для выполненного исследования укажите дату выполнения.");
                return false;
            }

            if (CompletionDatePicker.SelectedDate != null &&
                CompletionDatePicker.SelectedDate.Value.Date < OrderDatePicker.SelectedDate.Value.Date)
            {
                MessageBox.Show("Дата выполнения не может быть раньше даты назначения.");
                return false;
            }

            return true;
        }

        private static string GetStatusText(int status)
        {
            switch (status)
            {
                case 0:
                    return "Назначено";
                case 1:
                    return "Выполнено";
                case 2:
                    return "Отменено";
                default:
                    return "Неизвестно";
            }
        }
    }

    public class LaboratoryListItem
    {
        public int ResearchId { get; set; }

        public string AnalysisType { get; set; }

        public DateTime OrderDate { get; set; }
        public string OrderDateText { get; set; }

        public DateTime? CompletionDate { get; set; }
        public string CompletionDateText { get; set; }

        public string Result { get; set; }

        public int Status { get; set; }
        public string StatusText { get; set; }

        public int? AppointmentId { get; set; }

        public int PatientId { get; set; }
        public string PatientName { get; set; }

        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
    }

    public class AppointmentComboItem
    {
        public int Id { get; set; }

        public int PatientId { get; set; }
        public string PatientName { get; set; }

        public int DoctorId { get; set; }
        public string DoctorName { get; set; }

        public string Name { get; set; }
    }
}