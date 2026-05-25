using PolyclinicApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PolyclinicApp.Services;

namespace PolyclinicApp.Views
{
    public partial class ReferralsWindow : Window
    {
        private int? _selectedReferralId;

        private List<ReferralPatientComboItem> _allPatients =
            new List<ReferralPatientComboItem>();

        private List<ReferralDoctorComboItem> _allDoctors =
            new List<ReferralDoctorComboItem>();

        public ReferralsWindow()
        {
            InitializeComponent();

            LoadComboBoxes();
            LoadReferrals();
        }

        private void LoadComboBoxes()
        {
            using (var db = new ПоликлиникаEntities1())
            {
                _allPatients = db.Пациенты
                    .OrderBy(p => p.Фамилия)
                    .Select(p => new ReferralPatientComboItem
                    {
                        Id = p.ID_пациента,
                        Name = p.Фамилия + " " + p.Имя + " " + p.Отчество
                    })
                    .ToList();

                PatientComboBox.ItemsSource = _allPatients;

                _allDoctors =
                    (from d in db.Врачи
                     join s in db.Специальности
                        on d.ID_специальности equals s.ID_специальности
                     orderby d.Фамилия
                     select new ReferralDoctorComboItem
                     {
                         Id = d.ID_врача,
                         Name = d.Фамилия + " " + d.Имя + " " + d.Отчество + " — " + s.Название,
                         ShortName = d.Фамилия + " " + d.Имя + " " + d.Отчество,
                         SpecialityName = s.Название
                     })
                    .ToList();

                if (CurrentUser.IsDoctor && CurrentUser.DoctorId != null)
                {
                    int doctorId = CurrentUser.DoctorId.Value;

                    RefDoctorComboBox.ItemsSource = _allDoctors
                        .Where(d => d.Id == doctorId)
                        .ToList();

                    RefDoctorComboBox.SelectedValue = doctorId;
                    RefDoctorComboBox.IsEnabled = false;
                    RefDoctorSearchTextBox.IsEnabled = false;
                }
            }

            StatusComboBox.ItemsSource = new List<ReferralStatusItem>
            {
                new ReferralStatusItem { Id = 0, Name = "Активно" },
                new ReferralStatusItem { Id = 1, Name = "Использовано" },
                new ReferralStatusItem { Id = 2, Name = "Просрочено" }
            };

            StatusComboBox.SelectedValue = 0;
            IssueDatePicker.SelectedDate = DateTime.Today;
            ExpiryDatePicker.SelectedDate = DateTime.Today.AddDays(30);
        }

        private void LoadReferrals()
        {
            using (var db = new ПоликлиникаEntities1())
            {
                string search = SearchTextBox.Text.Trim();

                var query =
                    from r in db.Направления
                    join p in db.Пациенты
                        on r.ID_пациента equals p.ID_пациента
                    join d1 in db.Врачи
                        on r.ID_врача_направившего equals d1.ID_врача
                    join d2 in db.Врачи
                        on r.ID_врача_к_которому equals d2.ID_врача
                    select new
                    {
                        ReferralId = r.ID_направления,
                        Number = r.Номер,
                        IssueDate = r.Дата_выдачи,
                        ExpiryDate = r.Срок_действия,
                        Purpose = r.Цель,
                        Status = r.Статус,

                        PatientId = p.ID_пациента,
                        PatientName = p.Фамилия + " " + p.Имя + " " + p.Отчество,

                        RefDoctorId = d1.ID_врача,
                        RefDoctorName = d1.Фамилия + " " + d1.Имя + " " + d1.Отчество,

                        TargetDoctorId = d2.ID_врача,
                        TargetDoctorName = d2.Фамилия + " " + d2.Имя + " " + d2.Отчество
                    };
                if (CurrentUser.IsDoctor && CurrentUser.DoctorId != null)
                {
                    int doctorId = CurrentUser.DoctorId.Value;
                    query = query.Where(x => x.RefDoctorId == doctorId);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(x =>
                        x.Number.Contains(search) ||
                        x.PatientName.Contains(search) ||
                        x.RefDoctorName.Contains(search) ||
                        x.TargetDoctorName.Contains(search) ||
                        (x.Purpose != null && x.Purpose.Contains(search)));
                }

                ReferralsListView.ItemsSource = query
                    .OrderByDescending(x => x.IssueDate)
                    .Take(800)
                    .ToList()
                    .Select(x => new ReferralListItem
                    {
                        ReferralId = x.ReferralId,
                        Number = x.Number,
                        IssueDate = x.IssueDate,
                        IssueDateText = x.IssueDate.ToString("dd.MM.yyyy"),
                        ExpiryDate = x.ExpiryDate,
                        ExpiryDateText = x.ExpiryDate.ToString("dd.MM.yyyy"),
                        Purpose = x.Purpose,
                        Status = x.Status,
                        StatusText = GetStatusText(x.Status),

                        PatientId = x.PatientId,
                        PatientName = x.PatientName,

                        RefDoctorId = x.RefDoctorId,
                        RefDoctorName = x.RefDoctorName,

                        TargetDoctorId = x.TargetDoctorId,
                        TargetDoctorName = x.TargetDoctorName
                    })
                    .ToList();
            }
        }

        private void PatientSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = PatientSearchTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(search))
            {
                PatientComboBox.ItemsSource = _allPatients;
                return;
            }

            PatientComboBox.ItemsSource = _allPatients
                .Where(p => p.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            PatientComboBox.IsDropDownOpen = true;
        }

        private void RefDoctorSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterDoctorComboBox(RefDoctorSearchTextBox.Text, RefDoctorComboBox);
        }

        private void TargetDoctorSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterDoctorComboBox(TargetDoctorSearchTextBox.Text, TargetDoctorComboBox);
        }

        private void FilterDoctorComboBox(string text, ComboBox comboBox)
        {
            string search = text.Trim();

            if (string.IsNullOrWhiteSpace(search))
            {
                comboBox.ItemsSource = _allDoctors;
                return;
            }

            comboBox.ItemsSource = _allDoctors
                .Where(d =>
                    d.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    d.SpecialityName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            comboBox.IsDropDownOpen = true;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectedInfo();
        }

        private void UpdateSelectedInfo()
        {
            ReferralPatientComboItem patient = PatientComboBox.SelectedItem as ReferralPatientComboItem;
            ReferralDoctorComboItem refDoctor = RefDoctorComboBox.SelectedItem as ReferralDoctorComboItem;
            ReferralDoctorComboItem targetDoctor = TargetDoctorComboBox.SelectedItem as ReferralDoctorComboItem;

            string patientText = patient == null ? "пациент не выбран" : patient.Name;
            string refDoctorText = refDoctor == null ? "направивший врач не выбран" : refDoctor.ShortName;
            string targetDoctorText = targetDoctor == null ? "врач-получатель не выбран" : targetDoctor.ShortName;

            SelectedInfoTextBlock.Text =
                patientText + " | " + refDoctorText + " → " + targetDoctorText;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    int newId = 1;

                    if (db.Направления.Any())
                    {
                        newId = db.Направления.Max(r => r.ID_направления) + 1;
                    }

                    string number = NumberTextBox.Text.Trim();

                    if (string.IsNullOrWhiteSpace(number))
                    {
                        number = "НАП-" + newId.ToString("000000");
                    }

                    bool numberExists = db.Направления.Any(r => r.Номер == number);

                    if (numberExists)
                    {
                        MessageBox.Show("Направление с таким номером уже существует.");
                        return;
                    }

                    Направления referral = new Направления();

                    referral.ID_направления = newId;
                    referral.Номер = number;
                    referral.Дата_выдачи = IssueDatePicker.SelectedDate.Value;
                    referral.Срок_действия = ExpiryDatePicker.SelectedDate.Value;
                    referral.Цель = PurposeTextBox.Text.Trim();
                    referral.Статус = Convert.ToInt32(StatusComboBox.SelectedValue);
                    referral.ID_пациента = Convert.ToInt32(PatientComboBox.SelectedValue);
                    referral.ID_врача_направившего = Convert.ToInt32(RefDoctorComboBox.SelectedValue);
                    referral.ID_врача_к_которому = Convert.ToInt32(TargetDoctorComboBox.SelectedValue);

                    db.Направления.Add(referral);
                    db.SaveChanges();
                }

                LoadReferrals();
                ClearFields();

                MessageBox.Show("Направление добавлено.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении направления: " + ex.Message);
            }
            if (!AccessRules.CanEditReferrals())
            {
                MessageBox.Show("У вашей роли нет прав на изменение направлений.");
                return;
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedReferralId == null)
            {
                MessageBox.Show("Выберите направление для изменения.");
                return;
            }

            if (!ValidateFields())
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Направления referral = db.Направления
                        .FirstOrDefault(r => r.ID_направления == _selectedReferralId.Value);

                    if (referral == null)
                    {
                        MessageBox.Show("Направление не найдено.");
                        return;
                    }

                    string number = NumberTextBox.Text.Trim();

                    bool numberExists = db.Направления.Any(r =>
                        r.Номер == number &&
                        r.ID_направления != referral.ID_направления);

                    if (numberExists)
                    {
                        MessageBox.Show("Другое направление с таким номером уже существует.");
                        return;
                    }

                    referral.Номер = number;
                    referral.Дата_выдачи = IssueDatePicker.SelectedDate.Value;
                    referral.Срок_действия = ExpiryDatePicker.SelectedDate.Value;
                    referral.Цель = PurposeTextBox.Text.Trim();
                    referral.Статус = Convert.ToInt32(StatusComboBox.SelectedValue);
                    referral.ID_пациента = Convert.ToInt32(PatientComboBox.SelectedValue);
                    referral.ID_врача_направившего = Convert.ToInt32(RefDoctorComboBox.SelectedValue);
                    referral.ID_врача_к_которому = Convert.ToInt32(TargetDoctorComboBox.SelectedValue);

                    db.SaveChanges();
                }

                LoadReferrals();
                ClearFields();

                MessageBox.Show("Направление изменено.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при изменении направления: " + ex.Message);
            }
            if (!AccessRules.CanEditReferrals())
            {
                MessageBox.Show("У вашей роли нет прав на изменение направлений.");
                return;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedReferralId == null)
            {
                MessageBox.Show("Выберите направление для удаления.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Удалить выбранное направление?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Направления referral = db.Направления
                        .FirstOrDefault(r => r.ID_направления == _selectedReferralId.Value);

                    if (referral == null)
                    {
                        MessageBox.Show("Направление не найдено.");
                        return;
                    }

                    bool usedInAppointments = db.Приёмы.Any(a =>
                        a.ID_направления == referral.ID_направления);

                    if (usedInAppointments)
                    {
                        MessageBox.Show("Нельзя удалить направление, так как оно связано с приёмом.");
                        return;
                    }

                    db.Направления.Remove(referral);
                    db.SaveChanges();
                }

                LoadReferrals();
                ClearFields();

                MessageBox.Show("Направление удалено.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении направления: " + ex.Message);
            }
            if (!AccessRules.CanEditReferrals())
            {
                MessageBox.Show("У вашей роли нет прав на изменение направлений.");
                return;
            }
        }

        private void ReferralsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReferralListItem selected = ReferralsListView.SelectedItem as ReferralListItem;

            if (selected == null)
                return;

            _selectedReferralId = selected.ReferralId;

            PatientComboBox.ItemsSource = _allPatients;
            RefDoctorComboBox.ItemsSource = _allDoctors;
            TargetDoctorComboBox.ItemsSource = _allDoctors;

            PatientComboBox.SelectedValue = selected.PatientId;
            RefDoctorComboBox.SelectedValue = selected.RefDoctorId;
            TargetDoctorComboBox.SelectedValue = selected.TargetDoctorId;

            NumberTextBox.Text = selected.Number;
            IssueDatePicker.SelectedDate = selected.IssueDate;
            ExpiryDatePicker.SelectedDate = selected.ExpiryDate;
            PurposeTextBox.Text = selected.Purpose;
            StatusComboBox.SelectedValue = selected.Status;

            SelectedInfoTextBlock.Text =
                selected.PatientName + " | " +
                selected.RefDoctorName + " → " +
                selected.TargetDoctorName;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadReferrals();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadComboBoxes();
            LoadReferrals();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void ClearFields()
        {
            _selectedReferralId = null;

            PatientSearchTextBox.Clear();
            RefDoctorSearchTextBox.Clear();
            TargetDoctorSearchTextBox.Clear();

            PatientComboBox.ItemsSource = _allPatients;
            RefDoctorComboBox.ItemsSource = _allDoctors;
            TargetDoctorComboBox.ItemsSource = _allDoctors;

            PatientComboBox.SelectedIndex = -1;
            RefDoctorComboBox.SelectedIndex = -1;
            TargetDoctorComboBox.SelectedIndex = -1;

            NumberTextBox.Clear();

            IssueDatePicker.SelectedDate = DateTime.Today;
            ExpiryDatePicker.SelectedDate = DateTime.Today.AddDays(30);

            PurposeTextBox.Clear();
            StatusComboBox.SelectedValue = 0;

            ReferralsListView.SelectedItem = null;

            SelectedInfoTextBlock.Text = "Направление не выбрано";
        }

        private bool ValidateFields()
        {
            if (PatientComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите пациента.");
                return false;
            }

            if (RefDoctorComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите врача, который выдал направление.");
                return false;
            }

            if (TargetDoctorComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите врача, к которому направлен пациент.");
                return false;
            }

            int refDoctorId = Convert.ToInt32(RefDoctorComboBox.SelectedValue);
            int targetDoctorId = Convert.ToInt32(TargetDoctorComboBox.SelectedValue);

            if (refDoctorId == targetDoctorId)
            {
                MessageBox.Show("Врач, который выдал направление, и врач-получатель не должны совпадать.");
                return false;
            }

            if (IssueDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату выдачи.");
                return false;
            }

            if (ExpiryDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите срок действия.");
                return false;
            }

            if (ExpiryDatePicker.SelectedDate.Value.Date < IssueDatePicker.SelectedDate.Value.Date)
            {
                MessageBox.Show("Срок действия не может быть раньше даты выдачи.");
                return false;
            }

            if (StatusComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите статус.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(NumberTextBox.Text) && _selectedReferralId != null)
            {
                MessageBox.Show("Введите номер направления.");
                return false;
            }

            return true;
        }

        private static string GetStatusText(int status)
        {
            switch (status)
            {
                case 0:
                    return "Активно";
                case 1:
                    return "Использовано";
                case 2:
                    return "Просрочено";
                default:
                    return "Неизвестно";
            }
        }
    }

    public class ReferralListItem
    {
        public int ReferralId { get; set; }

        public string Number { get; set; }

        public DateTime IssueDate { get; set; }
        public string IssueDateText { get; set; }

        public DateTime ExpiryDate { get; set; }
        public string ExpiryDateText { get; set; }

        public string Purpose { get; set; }

        public int Status { get; set; }
        public string StatusText { get; set; }

        public int PatientId { get; set; }
        public string PatientName { get; set; }

        public int RefDoctorId { get; set; }
        public string RefDoctorName { get; set; }

        public int TargetDoctorId { get; set; }
        public string TargetDoctorName { get; set; }
    }

    public class ReferralPatientComboItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ReferralDoctorComboItem
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string ShortName { get; set; }
        public string SpecialityName { get; set; }
    }

    public class ReferralStatusItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}