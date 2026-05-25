using PolyclinicApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PolyclinicApp.Services;


namespace PolyclinicApp.Views
{
    public partial class ScheduleWindow : Window
    {
        private int? _selectedScheduleId;

        private List<ScheduleDoctorComboItem> _allDoctors =
            new List<ScheduleDoctorComboItem>();

        private List<ScheduleCabinetComboItem> _allCabinets =
            new List<ScheduleCabinetComboItem>();

        private List<ScheduleDayItem> _days =
            new List<ScheduleDayItem>();

        private List<ScheduleTimeItem> _times =
            new List<ScheduleTimeItem>();

        public ScheduleWindow()
        {
            InitializeComponent();

            LoadComboBoxes();
            LoadSchedule();
        }

        private void LoadComboBoxes()
        {
            using (var db = new ПоликлиникаEntities1())
            {
                var doctorsQuery =
                    from d in db.Врачи
                    join s in db.Специальности
                        on d.ID_специальности equals s.ID_специальности
                    select new
                    {
                        DoctorId = d.ID_врача,
                        DoctorName = d.Фамилия + " " + d.Имя + " " + d.Отчество,
                        SpecialityName = s.Название
                    };

                // Врач видит только себя
                if (CurrentUser.IsDoctor && CurrentUser.DoctorId != null)
                {
                    int doctorId = CurrentUser.DoctorId.Value;
                    doctorsQuery = doctorsQuery.Where(d => d.DoctorId == doctorId);
                }


                _allDoctors = doctorsQuery
                    .OrderBy(d => d.DoctorName)
                    .ToList()
                    .Select(d => new ScheduleDoctorComboItem
                    {
                        Id = d.DoctorId,
                        Name = d.DoctorName + " — " + d.SpecialityName,
                        ShortName = d.DoctorName,
                        SpecialityName = d.SpecialityName
                    })
                    .ToList();

                DoctorComboBox.ItemsSource = _allDoctors;

                var cabinetsQuery = db.Кабинеты.AsQueryable();

                // Лаборант видит только лабораторные кабинеты
                if (CurrentUser.IsLaborant)
                {
                    cabinetsQuery = cabinetsQuery.Where(c => c.Корпус.Contains("Лаборатория"));
                }

                _allCabinets = cabinetsQuery
                    .OrderBy(c => c.Номер)
                    .ToList()
                    .Select(c => new ScheduleCabinetComboItem
                    {
                        Id = c.ID_кабинета,
                        Number = c.Номер,
                        Corpus = c.Корпус,
                        Name = c.Номер + " / " + c.Корпус
                    })
                    .ToList();

                CabinetComboBox.ItemsSource = _allCabinets;
            }

            _days = new List<ScheduleDayItem>
    {
        new ScheduleDayItem { Id = 1, Name = "Понедельник" },
        new ScheduleDayItem { Id = 2, Name = "Вторник" },
        new ScheduleDayItem { Id = 3, Name = "Среда" },
        new ScheduleDayItem { Id = 4, Name = "Четверг" },
        new ScheduleDayItem { Id = 5, Name = "Пятница" },
        new ScheduleDayItem { Id = 6, Name = "Суббота" },
        new ScheduleDayItem { Id = 7, Name = "Воскресенье" }
    };

            DayComboBox.ItemsSource = _days;

            _times = CreateTimeItems();

            StartTimeComboBox.ItemsSource = _times;
            EndTimeComboBox.ItemsSource = _times;

            ActiveCheckBox.IsChecked = true;

            // Если вошёл врач, сразу выбираем его в списке
            if (CurrentUser.IsDoctor && CurrentUser.DoctorId != null)
            {
                DoctorComboBox.SelectedValue = CurrentUser.DoctorId.Value;
                DoctorComboBox.IsEnabled = false;
                DoctorSearchTextBox.IsEnabled = false;
            }
        }

        private List<ScheduleTimeItem> CreateTimeItems()
        {
            List<ScheduleTimeItem> result = new List<ScheduleTimeItem>();

            TimeSpan start = new TimeSpan(8, 0, 0);
            TimeSpan end = new TimeSpan(20, 0, 0);

            TimeSpan current = start;

            while (current <= end)
            {
                result.Add(new ScheduleTimeItem
                {
                    Value = current,
                    Text = current.ToString(@"hh\:mm")
                });

                current = current.Add(TimeSpan.FromMinutes(30));
            }

            return result;
        }

        private void LoadSchedule()
        {
            using (var db = new ПоликлиникаEntities1())
            {
                string search = SearchTextBox.Text.Trim();

                var query =
                    from r in db.Расписание
                    join d in db.Врачи
                        on r.ID_врача equals d.ID_врача
                    join s in db.Специальности
                        on d.ID_специальности equals s.ID_специальности
                    join c in db.Кабинеты
                        on r.ID_кабинета equals c.ID_кабинета
                    select new
                    {
                        ScheduleId = r.ID_расписания,
                        DayId = r.День_недели,
                        StartTime = r.Время_начала,
                        EndTime = r.Время_окончания,
                        Active = r.Активно,

                        DoctorId = d.ID_врача,
                        DoctorName = d.Фамилия + " " + d.Имя + " " + d.Отчество,
                        SpecialityName = s.Название,

                        CabinetId = c.ID_кабинета,
                        CabinetNumber = c.Номер,
                        Corpus = c.Корпус
                    };

                // Врач видит только своё расписание
                if (CurrentUser.IsDoctor && CurrentUser.DoctorId != null)
                {
                    int doctorId = CurrentUser.DoctorId.Value;
                    query = query.Where(x => x.DoctorId == doctorId);
                }

                // Лаборант видит только расписание лабораторных кабинетов
                if (CurrentUser.IsLaborant)
                {
                    query = query.Where(x => x.Corpus.Contains("Лаборатория"));
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(x =>
                        x.DoctorName.Contains(search) ||
                        x.SpecialityName.Contains(search) ||
                        x.CabinetNumber.Contains(search) ||
                        x.Corpus.Contains(search));
                }


                ScheduleListView.ItemsSource = query
                    .OrderBy(x => x.DayId)
                    .ThenBy(x => x.StartTime)
                    .ToList()
                    .Select(x => new ScheduleListItem
                    {
                        ScheduleId = x.ScheduleId,
                        DayId = x.DayId,
                        DayName = GetDayName(x.DayId),

                        StartTime = x.StartTime,
                        StartTimeText = x.StartTime.ToString(@"hh\:mm"),

                        EndTime = x.EndTime,
                        EndTimeText = x.EndTime.ToString(@"hh\:mm"),

                        Active = x.Active,
                        ActiveText = x.Active ? "Да" : "Нет",

                        DoctorId = x.DoctorId,
                        DoctorName = x.DoctorName,
                        SpecialityName = x.SpecialityName,

                        CabinetId = x.CabinetId,
                        CabinetName = x.CabinetNumber,
                        Corpus = x.Corpus
                    })
                    .ToList();
            }
        }

        private void DoctorSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = DoctorSearchTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(search))
            {
                DoctorComboBox.ItemsSource = _allDoctors;
                return;
            }

            DoctorComboBox.ItemsSource = _allDoctors
                .Where(d =>
                    d.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    d.SpecialityName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            DoctorComboBox.IsDropDownOpen = true;
        }

        private void ComboBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateSelectedInfo();
        }

        private void UpdateSelectedInfo()
        {
            ScheduleDoctorComboItem doctor = DoctorComboBox.SelectedItem as ScheduleDoctorComboItem;
            ScheduleCabinetComboItem cabinet = CabinetComboBox.SelectedItem as ScheduleCabinetComboItem;
            ScheduleDayItem day = DayComboBox.SelectedItem as ScheduleDayItem;
            ScheduleTimeItem start = StartTimeComboBox.SelectedItem as ScheduleTimeItem;
            ScheduleTimeItem end = EndTimeComboBox.SelectedItem as ScheduleTimeItem;

            string doctorText = doctor == null ? "врач не выбран" : doctor.ShortName;
            string cabinetText = cabinet == null ? "кабинет не выбран" : cabinet.Name;
            string dayText = day == null ? "день не выбран" : day.Name;
            string timeText = start == null || end == null
                ? "время не выбрано"
                : start.Text + "–" + end.Text;

            string activeText = ActiveCheckBox.IsChecked == true ? "активно" : "неактивно";

            SelectedInfoTextBlock.Text =
                doctorText + " | " +
                cabinetText + " | " +
                dayText + " | " +
                timeText + " | " +
                activeText;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            TimeSpan startTime;
            TimeSpan endTime;

            if (!ValidateFields(out startTime, out endTime))
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    int doctorId = Convert.ToInt32(DoctorComboBox.SelectedValue);
                    int cabinetId = Convert.ToInt32(CabinetComboBox.SelectedValue);
                    int dayId = Convert.ToInt32(DayComboBox.SelectedValue);

                    bool hasConflict = HasScheduleConflict(
                        db,
                        null,
                        doctorId,
                        cabinetId,
                        dayId,
                        startTime,
                        endTime);

                    if (hasConflict)
                    {
                        MessageBox.Show("Найдено пересечение по времени у выбранного врача или кабинета.");
                        return;
                    }

                    int newId = 1;

                    if (db.Расписание.Any())
                    {
                        newId = db.Расписание.Max(r => r.ID_расписания) + 1;
                    }

                    Расписание schedule = new Расписание();

                    schedule.ID_расписания = newId;
                    schedule.День_недели = dayId;
                    schedule.Время_начала = startTime;
                    schedule.Время_окончания = endTime;
                    schedule.Активно = ActiveCheckBox.IsChecked == true;
                    schedule.ID_врача = doctorId;
                    schedule.ID_кабинета = cabinetId;

                    db.Расписание.Add(schedule);
                    db.SaveChanges();
                }

                LoadSchedule();
                ClearFields();

                MessageBox.Show("Расписание добавлено.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении расписания: " + ex.Message);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedScheduleId == null)
            {
                MessageBox.Show("Выберите запись расписания для изменения.");
                return;
            }

            TimeSpan startTime;
            TimeSpan endTime;

            if (!ValidateFields(out startTime, out endTime))
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Расписание schedule = db.Расписание
                        .FirstOrDefault(r => r.ID_расписания == _selectedScheduleId.Value);

                    if (schedule == null)
                    {
                        MessageBox.Show("Запись расписания не найдена.");
                        return;
                    }

                    int doctorId = Convert.ToInt32(DoctorComboBox.SelectedValue);
                    int cabinetId = Convert.ToInt32(CabinetComboBox.SelectedValue);
                    int dayId = Convert.ToInt32(DayComboBox.SelectedValue);

                    bool hasConflict = HasScheduleConflict(
                        db,
                        schedule.ID_расписания,
                        doctorId,
                        cabinetId,
                        dayId,
                        startTime,
                        endTime);

                    if (hasConflict)
                    {
                        MessageBox.Show("Найдено пересечение по времени у выбранного врача или кабинета.");
                        return;
                    }

                    schedule.День_недели = dayId;
                    schedule.Время_начала = startTime;
                    schedule.Время_окончания = endTime;
                    schedule.Активно = ActiveCheckBox.IsChecked == true;
                    schedule.ID_врача = doctorId;
                    schedule.ID_кабинета = cabinetId;

                    db.SaveChanges();
                }

                LoadSchedule();
                ClearFields();

                MessageBox.Show("Расписание изменено.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при изменении расписания: " + ex.Message);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedScheduleId == null)
            {
                MessageBox.Show("Выберите запись расписания для удаления.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Удалить выбранную запись расписания?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Расписание schedule = db.Расписание
                        .FirstOrDefault(r => r.ID_расписания == _selectedScheduleId.Value);

                    if (schedule == null)
                    {
                        MessageBox.Show("Запись расписания не найдена.");
                        return;
                    }

                    db.Расписание.Remove(schedule);
                    db.SaveChanges();
                }

                LoadSchedule();
                ClearFields();

                MessageBox.Show("Запись расписания удалена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении расписания: " + ex.Message);
            }
        }

        private bool HasScheduleConflict(
            ПоликлиникаEntities1 db,
            int? currentScheduleId,
            int doctorId,
            int cabinetId,
            int dayId,
            TimeSpan startTime,
            TimeSpan endTime)
        {
            return db.Расписание.Any(r =>
                r.Активно == true &&
                r.День_недели == dayId &&
                (r.ID_врача == doctorId || r.ID_кабинета == cabinetId) &&
                (currentScheduleId == null || r.ID_расписания != currentScheduleId.Value) &&
                startTime < r.Время_окончания &&
                endTime > r.Время_начала);
        }

        private void ScheduleListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ScheduleListItem selected = ScheduleListView.SelectedItem as ScheduleListItem;

            if (selected == null)
                return;

            _selectedScheduleId = selected.ScheduleId;

            DoctorComboBox.ItemsSource = _allDoctors;
            CabinetComboBox.ItemsSource = _allCabinets;

            DoctorComboBox.SelectedValue = selected.DoctorId;
            CabinetComboBox.SelectedValue = selected.CabinetId;
            DayComboBox.SelectedValue = selected.DayId;

            StartTimeComboBox.SelectedValue = selected.StartTime;
            EndTimeComboBox.SelectedValue = selected.EndTime;

            ActiveCheckBox.IsChecked = selected.Active;

            SelectedInfoTextBlock.Text =
                selected.DoctorName + " | каб. " +
                selected.CabinetName + " | " +
                selected.DayName + " | " +
                selected.StartTimeText + "–" + selected.EndTimeText;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadSchedule();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadComboBoxes();
            LoadSchedule();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void ClearFields()
        {
            _selectedScheduleId = null;

            DoctorSearchTextBox.Clear();

            DoctorComboBox.ItemsSource = _allDoctors;
            CabinetComboBox.ItemsSource = _allCabinets;

            DoctorComboBox.SelectedIndex = -1;
            CabinetComboBox.SelectedIndex = -1;
            DayComboBox.SelectedIndex = -1;
            StartTimeComboBox.SelectedIndex = -1;
            EndTimeComboBox.SelectedIndex = -1;

            ActiveCheckBox.IsChecked = true;

            ScheduleListView.SelectedItem = null;

            SelectedInfoTextBlock.Text = "Расписание не выбрано";
        }

        private bool ValidateFields(out TimeSpan startTime, out TimeSpan endTime)
        {
            startTime = TimeSpan.Zero;
            endTime = TimeSpan.Zero;

            if (DoctorComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите врача.");
                return false;
            }

            if (CabinetComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите кабинет.");
                return false;
            }

            if (DayComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите день недели.");
                return false;
            }

            if (StartTimeComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите время начала.");
                return false;
            }

            if (EndTimeComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите время окончания.");
                return false;
            }

            if (!AccessRules.CanEditSchedule())
            {
                MessageBox.Show("У вашей роли нет прав на изменение расписания.");
                return false;
            }

            if (CurrentUser.IsDoctor && CurrentUser.DoctorId != null)
            {
                int selectedDoctorId = Convert.ToInt32(DoctorComboBox.SelectedValue);

                if (selectedDoctorId != CurrentUser.DoctorId.Value)
                {
                    MessageBox.Show("Врач может редактировать только своё расписание.");
                    return false;
                }
            }

            if (CurrentUser.IsLaborant)
            {
                int selectedCabinetId = Convert.ToInt32(CabinetComboBox.SelectedValue);

                ScheduleCabinetComboItem cabinet = _allCabinets
                    .FirstOrDefault(c => c.Id == selectedCabinetId);

                if (cabinet == null ||
                    string.IsNullOrWhiteSpace(cabinet.Corpus) ||
                    !cabinet.Corpus.Contains("Лаборатория"))
                {
                    MessageBox.Show("Лаборант может работать только с лабораторными кабинетами.");
                    return false;
                }
            }

            startTime = (TimeSpan)StartTimeComboBox.SelectedValue;
            endTime = (TimeSpan)EndTimeComboBox.SelectedValue;

            if (endTime <= startTime)
            {
                MessageBox.Show("Время окончания должно быть позже времени начала.");
                return false;
            }

            return true;
        }

        private static string GetDayName(int day)
        {
            switch (day)
            {
                case 1:
                    return "Понедельник";
                case 2:
                    return "Вторник";
                case 3:
                    return "Среда";
                case 4:
                    return "Четверг";
                case 5:
                    return "Пятница";
                case 6:
                    return "Суббота";
                case 7:
                    return "Воскресенье";
                default:
                    return "Неизвестно";
            }
        }
    }

    public class ScheduleListItem
    {
        public int ScheduleId { get; set; }

        public int DayId { get; set; }
        public string DayName { get; set; }

        public TimeSpan StartTime { get; set; }
        public string StartTimeText { get; set; }

        public TimeSpan EndTime { get; set; }
        public string EndTimeText { get; set; }

        public bool Active { get; set; }
        public string ActiveText { get; set; }

        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public string SpecialityName { get; set; }

        public int CabinetId { get; set; }
        public string CabinetName { get; set; }
        public string Corpus { get; set; }
    }

    public class ScheduleDoctorComboItem
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string ShortName { get; set; }
        public string SpecialityName { get; set; }
    }

    public class ScheduleCabinetComboItem
    {
        public int Id { get; set; }

        public string Number { get; set; }
        public string Corpus { get; set; }
        public string Name { get; set; }
    }

    public class ScheduleDayItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ScheduleTimeItem
    {
        public TimeSpan Value { get; set; }
        public string Text { get; set; }
    }
}