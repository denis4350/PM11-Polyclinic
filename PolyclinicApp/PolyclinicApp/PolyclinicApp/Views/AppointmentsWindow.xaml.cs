using PolyclinicApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PolyclinicApp.Services;

namespace PolyclinicApp.Views
{
    public partial class AppointmentsWindow : Window
    {
        private int? _selectedAppointmentId;

        private List<PatientComboItem> _allPatients = new List<PatientComboItem>();
        private List<DoctorComboItem> _allDoctors = new List<DoctorComboItem>();
        private List<CabinetComboItem> _allCabinets = new List<CabinetComboItem>();

        private bool _isLoading = false;

        private const int PlanningDays = 60;
        private const int SlotMinutes = 30;

        public AppointmentsWindow()
        {
            InitializeComponent();

            _isLoading = true;
            LoadComboBoxes();
            ApplyRoleAccess();
            _isLoading = false;

            LoadAppointments();
            ApplyDateBlackouts();
        }
        private void ApplyRoleAccess()
        {
            if (!AccessRules.CanEditAppointments())
            {
                AddButton.IsEnabled = false;
                EditButton.IsEnabled = false;
                DeleteButton.IsEnabled = false;
            }

            // Врач работает только со своими приёмами
            if (CurrentUser.IsDoctor && CurrentUser.DoctorId != null)
            {
                DoctorComboBox.SelectedValue = CurrentUser.DoctorId.Value;
                DoctorComboBox.IsEnabled = false;
                DoctorSearchTextBox.IsEnabled = false;
            }

            // Лаборант только смотрит приёмы
            if (CurrentUser.IsLaborant)
            {
                AddButton.IsEnabled = false;
                EditButton.IsEnabled = false;
                DeleteButton.IsEnabled = false;
            }
        }

        private void LoadComboBoxes()
        {
            using (var db = new ПоликлиникаEntities1())
            {
                _allPatients = db.Пациенты
                    .OrderBy(p => p.Фамилия)
                    .Select(p => new PatientComboItem
                    {
                        Id = p.ID_пациента,
                        Name = p.Фамилия + " " + p.Имя + " " + p.Отчество,
                        BirthDate = p.Дата_рождения
                    })
                    .ToList();

                PatientComboBox.ItemsSource = _allPatients;

                _allDoctors =
                    (from d in db.Врачи
                     join s in db.Специальности
                        on d.ID_специальности equals s.ID_специальности
                     orderby d.Фамилия
                     select new DoctorComboItem
                     {
                         Id = d.ID_врача,
                         Name = d.Фамилия + " " + d.Имя + " " + d.Отчество + " — " + s.Название,
                         SpecialityId = s.ID_специальности,
                         SpecialityName = s.Название
                     })
                    .ToList();

                DoctorComboBox.ItemsSource = _allDoctors;

                _allCabinets = db.Кабинеты
                    .OrderBy(c => c.Номер)
                    .Select(c => new CabinetComboItem
                    {
                        Id = c.ID_кабинета,
                        Number = c.Номер,
                        Corpus = c.Корпус,
                        Name = c.Номер + " / " + c.Корпус
                    })
                    .ToList();

                CabinetComboBox.ItemsSource = _allCabinets;
            }

            StatusComboBox.ItemsSource = new List<ComboItem>
            {
                new ComboItem { Id = 0, Name = "Назначен" },
                new ComboItem { Id = 1, Name = "В очереди" },
                new ComboItem { Id = 2, Name = "Принят" },
                new ComboItem { Id = 3, Name = "Завершён" },
                new ComboItem { Id = 4, Name = "Отменён" }
            };

            StatusComboBox.SelectedValue = 0;
        }

        private void LoadAppointments()
        {
            using (var db = new ПоликлиникаEntities1())
            {
                string search = SearchTextBox.Text.Trim();

                var query =
                    from a in db.Приёмы
                    join p in db.Пациенты
                        on a.ID_пациента equals p.ID_пациента
                    join d in db.Врачи
                        on a.ID_врача equals d.ID_врача
                    join s in db.Специальности
                        on d.ID_специальности equals s.ID_специальности
                    join c in db.Кабинеты
                        on a.ID_кабинета equals c.ID_кабинета
                    select new
                    {
                        AppointmentId = a.ID_приёма,
                        AppointmentDateTime = a.Дата_время,
                        Status = a.Статус,
                        Complaints = a.Жалобы,
                        DoctorNote = a.Примечание_врача,


                        PatientId = p.ID_пациента,
                        PatientName = p.Фамилия + " " + p.Имя + " " + p.Отчество,
                        PatientBirthDate = p.Дата_рождения,

                        DoctorId = d.ID_врача,
                        DoctorName = d.Фамилия + " " + d.Имя + " " + d.Отчество,
                        SpecialityName = s.Название,

                        CabinetId = c.ID_кабинета,
                        CabinetName = c.Номер + " / " + c.Корпус
                    };
                if (CurrentUser.IsDoctor && CurrentUser.DoctorId != null)
                {
                    int doctorId = CurrentUser.DoctorId.Value;
                    query = query.Where(x => x.DoctorId == doctorId);
                }

                if (CurrentUser.IsLaborant)
                {
                    // Лаборант видит приёмы, связанные с лабораторией по смыслу,
                    // но не редактирует их. Пока оставляем общий просмотр.
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(x =>
                        x.PatientName.Contains(search) ||
                        x.DoctorName.Contains(search) ||
                        x.SpecialityName.Contains(search) ||
                        x.CabinetName.Contains(search));
                }

                var list = query
                    .OrderByDescending(x => x.AppointmentDateTime)
                    .Take(700)
                    .ToList()
                    .Select(x => new AppointmentListItem
                    {
                        AppointmentId = x.AppointmentId,
                        AppointmentDateTime = x.AppointmentDateTime,
                        DateTimeText = x.AppointmentDateTime.ToString("dd.MM.yyyy HH:mm"),

                        PatientId = x.PatientId,
                        PatientName = x.PatientName,
                        PatientAge = CalculateAge(x.PatientBirthDate),

                        DoctorId = x.DoctorId,
                        DoctorName = x.DoctorName,
                        SpecialityName = x.SpecialityName,

                        CabinetId = x.CabinetId,
                        CabinetName = x.CabinetName,

                        Status = x.Status,
                        StatusText = GetStatusText(x.Status),

                        Complaints = x.Complaints,
                        DoctorNote = x.DoctorNote
                    })
                    .ToList();

                AppointmentsListView.ItemsSource = list;
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

        private void PatientComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading)
                return;

            UpdateAgeText();
            ApplyDateBlackouts();
        }

        private void DoctorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading)
                return;

            ApplyDateBlackouts();
        }

        private void AppointmentDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading)
                return;

            LoadAvailableTimeSlots();
        }

        private void TimeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TimeSlotItem slot = TimeComboBox.SelectedItem as TimeSlotItem;

            if (slot == null)
                return;

            CabinetComboBox.SelectedValue = slot.CabinetId;
        }

        private void ApplyDateBlackouts()
        {
            if (_isLoading)
                return;

            _isLoading = true;

            AppointmentDatePicker.BlackoutDates.Clear();
            AppointmentDatePicker.SelectedDate = null;

            TimeComboBox.ItemsSource = null;
            TimeComboBox.SelectedIndex = -1;
            CabinetComboBox.SelectedIndex = -1;

            DateTime startDate = DateTime.Today;
            DateTime endDate = DateTime.Today.AddDays(PlanningDays);

            AppointmentDatePicker.DisplayDateStart = startDate;
            AppointmentDatePicker.DisplayDateEnd = endDate;

            if (DoctorComboBox.SelectedValue == null)
            {
                _isLoading = false;
                return;
            }

            int doctorId = Convert.ToInt32(DoctorComboBox.SelectedValue);

            DateTime currentDate = startDate;

            while (currentDate <= endDate)
            {
                bool hasSlots = GetFreeTimeSlotsForDate(doctorId, currentDate).Count > 0;

                if (!hasSlots)
                {
                    AppointmentDatePicker.BlackoutDates.Add(new CalendarDateRange(currentDate));
                }

                currentDate = currentDate.AddDays(1);
            }

            _isLoading = false;
        }

        private void LoadAvailableTimeSlots()
        {
            TimeComboBox.ItemsSource = null;
            TimeComboBox.SelectedIndex = -1;
            CabinetComboBox.SelectedIndex = -1;

            if (DoctorComboBox.SelectedValue == null)
                return;

            if (AppointmentDatePicker.SelectedDate == null)
                return;

            int doctorId = Convert.ToInt32(DoctorComboBox.SelectedValue);
            DateTime date = AppointmentDatePicker.SelectedDate.Value.Date;

            List<TimeSlotItem> slots = GetFreeTimeSlotsForDate(doctorId, date);

            TimeComboBox.ItemsSource = slots;

            if (slots.Count > 0)
            {
                TimeComboBox.SelectedIndex = 0;
            }
        }

        private List<TimeSlotItem> GetFreeTimeSlotsForDate(int doctorId, DateTime date)
        {
            List<TimeSlotItem> result = new List<TimeSlotItem>();

            int dayOfWeek = GetRussianDayOfWeek(date);

            DoctorComboItem selectedDoctor = _allDoctors.FirstOrDefault(d => d.Id == doctorId);
            PatientComboItem selectedPatient = GetSelectedPatient();

            using (var db = new ПоликлиникаEntities1())
            {
                var schedules = db.Расписание
                    .Where(r =>
                        r.ID_врача == doctorId &&
                        r.День_недели == dayOfWeek &&
                        r.Активно == true)
                    .OrderBy(r => r.Время_начала)
                    .ToList();

                foreach (var schedule in schedules)
                {
                    TimeSpan currentTime = schedule.Время_начала;
                    TimeSpan endTime = schedule.Время_окончания;

                    while (currentTime < endTime)
                    {
                        DateTime appointmentDateTime = date.Date.Add(currentTime);
                        int cabinetId = ResolveCabinetId(schedule.ID_кабинета, selectedDoctor, selectedPatient);

                        bool doctorBusy = db.Приёмы.Any(a =>
                            a.ID_врача == doctorId &&
                            a.Дата_время == appointmentDateTime &&
                            a.Статус != 4 &&
                            (_selectedAppointmentId == null || a.ID_приёма != _selectedAppointmentId.Value));

                        bool cabinetBusy = db.Приёмы.Any(a =>
                            a.ID_кабинета == cabinetId &&
                            a.Дата_время == appointmentDateTime &&
                            a.Статус != 4 &&
                            (_selectedAppointmentId == null || a.ID_приёма != _selectedAppointmentId.Value));

                        bool patientBusy = false;

                        if (PatientComboBox.SelectedValue != null)
                        {
                            int patientId = Convert.ToInt32(PatientComboBox.SelectedValue);

                            patientBusy = db.Приёмы.Any(a =>
                                a.ID_пациента == patientId &&
                                a.Дата_время == appointmentDateTime &&
                                a.Статус != 4 &&
                                (_selectedAppointmentId == null || a.ID_приёма != _selectedAppointmentId.Value));
                        }

                        bool canceledOtherExists = false;

                        if (_selectedAppointmentId != null)
                        {
                            canceledOtherExists = db.Приёмы.Any(a =>
                                a.ID_кабинета == cabinetId &&
                                a.Дата_время == appointmentDateTime &&
                                a.Статус == 4 &&
                                a.ID_приёма != _selectedAppointmentId.Value);
                        }

                        if (!doctorBusy && !cabinetBusy && !patientBusy && !canceledOtherExists)
                        {
                            CabinetComboItem cabinet = _allCabinets.FirstOrDefault(c => c.Id == cabinetId);
                            string cabinetText = cabinet != null ? cabinet.Name : cabinetId.ToString();

                            result.Add(new TimeSlotItem
                            {
                                Value = currentTime,
                                CabinetId = cabinetId,
                                Text = currentTime.ToString(@"hh\:mm") + " / каб. " + cabinetText
                            });
                        }

                        currentTime = currentTime.Add(TimeSpan.FromMinutes(SlotMinutes));
                    }
                }
            }

            return result;
        }

        private int ResolveCabinetId(int scheduleCabinetId, DoctorComboItem doctor, PatientComboItem patient)
        {
            if (doctor == null)
                return scheduleCabinetId;

            bool isPediatrician = doctor.SpecialityName
                .IndexOf("Педиатр", StringComparison.OrdinalIgnoreCase) >= 0;

            bool isChild = false;

            if (patient != null)
            {
                isChild = CalculateAge(patient.BirthDate) < 18;
            }

            if (isPediatrician && isChild)
            {
                CabinetComboItem childCabinet = _allCabinets.FirstOrDefault(c =>
                    !string.IsNullOrWhiteSpace(c.Corpus) &&
                    c.Corpus.IndexOf("Детский", StringComparison.OrdinalIgnoreCase) >= 0);

                if (childCabinet != null)
                    return childCabinet.Id;
            }

            if (isPediatrician)
            {
                CabinetComboItem pediatricCabinet = _allCabinets.FirstOrDefault(c =>
                    !string.IsNullOrWhiteSpace(c.Corpus) &&
                    c.Corpus.IndexOf("Детский", StringComparison.OrdinalIgnoreCase) >= 0);

                if (pediatricCabinet != null)
                    return pediatricCabinet.Id;
            }

            return scheduleCabinetId;
        }

        private PatientComboItem GetSelectedPatient()
        {
            if (PatientComboBox.SelectedValue == null)
                return null;

            int patientId = Convert.ToInt32(PatientComboBox.SelectedValue);

            return _allPatients.FirstOrDefault(p => p.Id == patientId);
        }

        private void UpdateAgeText()
        {
            PatientComboItem patient = GetSelectedPatient();

            if (patient == null)
            {
                AgeTextBlock.Text = "Возраст пациента: не выбран";
                return;
            }

            int age = CalculateAge(patient.BirthDate);

            if (age < 18)
            {
                AgeTextBlock.Text = "Возраст пациента: " + age + " лет, детский приём";
            }
            else
            {
                AgeTextBlock.Text = "Возраст пациента: " + age + " лет, взрослый приём";
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            DateTime appointmentDateTime;

            if (!ValidateFields(out appointmentDateTime))
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    int doctorId = Convert.ToInt32(DoctorComboBox.SelectedValue);
                    int patientId = Convert.ToInt32(PatientComboBox.SelectedValue);

                    TimeSlotItem slot = TimeComboBox.SelectedItem as TimeSlotItem;

                    if (slot == null)
                    {
                        MessageBox.Show("Выберите свободное время.");
                        return;
                    }

                    int cabinetId = slot.CabinetId;

                    bool doctorBusy = db.Приёмы.Any(a =>
                        a.ID_врача == doctorId &&
                        a.Дата_время == appointmentDateTime &&
                        a.Статус != 4);

                    if (doctorBusy)
                    {
                        MessageBox.Show("У выбранного врача уже есть приём на это время.");
                        return;
                    }

                    bool patientBusy = db.Приёмы.Any(a =>
                        a.ID_пациента == patientId &&
                        a.Дата_время == appointmentDateTime &&
                        a.Статус != 4);

                    if (patientBusy)
                    {
                        MessageBox.Show("Пациент уже записан на это время.");
                        return;
                    }

                    bool cabinetBusy = db.Приёмы.Any(a =>
                        a.ID_кабинета == cabinetId &&
                        a.Дата_время == appointmentDateTime &&
                        a.Статус != 4);

                    if (cabinetBusy)
                    {
                        MessageBox.Show("Кабинет уже занят на это время.");
                        return;
                    }

                    Приёмы canceledAppointment = db.Приёмы.FirstOrDefault(a =>
                        a.ID_кабинета == cabinetId &&
                        a.Дата_время == appointmentDateTime &&
                        a.Статус == 4);

                    if (canceledAppointment != null)
                    {
                        canceledAppointment.Статус = Convert.ToInt32(StatusComboBox.SelectedValue);
                        canceledAppointment.Жалобы = ComplaintsTextBox.Text.Trim();
                        canceledAppointment.Примечание_врача = DoctorNoteTextBox.Text.Trim();
                        canceledAppointment.ID_пациента = patientId;
                        canceledAppointment.ID_врача = doctorId;
                        canceledAppointment.ID_кабинета = cabinetId;
                        canceledAppointment.Код_диагноза = null;
                        canceledAppointment.ID_направления = null;
                    }
                    else
                    {
                        int newId = 1;

                        if (db.Приёмы.Any())
                        {
                            newId = db.Приёмы.Max(a => a.ID_приёма) + 1;
                        }

                        Приёмы appointment = new Приёмы();

                        appointment.ID_приёма = newId;
                        appointment.Дата_время = appointmentDateTime;
                        appointment.Статус = Convert.ToInt32(StatusComboBox.SelectedValue);
                        appointment.Жалобы = ComplaintsTextBox.Text.Trim();
                        appointment.Примечание_врача = DoctorNoteTextBox.Text.Trim();
                        appointment.ID_пациента = patientId;
                        appointment.ID_врача = doctorId;
                        appointment.ID_кабинета = cabinetId;
                        appointment.Код_диагноза = null;
                        appointment.ID_направления = null;

                        db.Приёмы.Add(appointment);
                    }

                    db.SaveChanges();
                }

                LoadAppointments();
                ClearFields();

                MessageBox.Show("Приём добавлен.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении приёма: " + ex.Message);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAppointmentId == null)
            {
                MessageBox.Show("Выберите приём для изменения.");
                return;
            }

            DateTime appointmentDateTime;

            if (!ValidateFields(out appointmentDateTime))
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Приёмы appointment = db.Приёмы
                        .FirstOrDefault(a => a.ID_приёма == _selectedAppointmentId.Value);

                    if (appointment == null)
                    {
                        MessageBox.Show("Приём не найден.");
                        return;
                    }

                    int doctorId = Convert.ToInt32(DoctorComboBox.SelectedValue);
                    int patientId = Convert.ToInt32(PatientComboBox.SelectedValue);

                    TimeSlotItem slot = TimeComboBox.SelectedItem as TimeSlotItem;

                    if (slot == null)
                    {
                        MessageBox.Show("Выберите свободное время.");
                        return;
                    }

                    int cabinetId = slot.CabinetId;

                    bool doctorBusy = db.Приёмы.Any(a =>
                        a.ID_приёма != appointment.ID_приёма &&
                        a.ID_врача == doctorId &&
                        a.Дата_время == appointmentDateTime &&
                        a.Статус != 4);

                    if (doctorBusy)
                    {
                        MessageBox.Show("У выбранного врача уже есть другой приём на это время.");
                        return;
                    }

                    bool patientBusy = db.Приёмы.Any(a =>
                        a.ID_приёма != appointment.ID_приёма &&
                        a.ID_пациента == patientId &&
                        a.Дата_время == appointmentDateTime &&
                        a.Статус != 4);

                    if (patientBusy)
                    {
                        MessageBox.Show("Пациент уже записан на другой приём в это время.");
                        return;
                    }

                    bool cabinetBusy = db.Приёмы.Any(a =>
                        a.ID_приёма != appointment.ID_приёма &&
                        a.ID_кабинета == cabinetId &&
                        a.Дата_время == appointmentDateTime);

                    if (cabinetBusy)
                    {
                        MessageBox.Show("Кабинет уже занят или занят отменённой записью на это время. Для новой записи используйте кнопку Добавить.");
                        return;
                    }

                    appointment.Дата_время = appointmentDateTime;
                    appointment.Статус = Convert.ToInt32(StatusComboBox.SelectedValue);
                    appointment.Жалобы = ComplaintsTextBox.Text.Trim();
                    appointment.Примечание_врача = DoctorNoteTextBox.Text.Trim();
                    appointment.ID_пациента = patientId;
                    appointment.ID_врача = doctorId;
                    appointment.ID_кабинета = cabinetId;

                    db.SaveChanges();
                }

                LoadAppointments();
                ClearFields();

                MessageBox.Show("Приём изменён.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при изменении приёма: " + ex.Message);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAppointmentId == null)
            {
                MessageBox.Show("Выберите приём для отмены.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Отменить выбранный приём?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Приёмы appointment = db.Приёмы
                        .FirstOrDefault(a => a.ID_приёма == _selectedAppointmentId.Value);

                    if (appointment == null)
                    {
                        MessageBox.Show("Приём не найден.");
                        return;
                    }

                    appointment.Статус = 4;
                    appointment.Примечание_врача = "Приём отменён. Слот доступен для повторной записи.";

                    db.SaveChanges();
                }

                LoadAppointments();
                ClearFields();

                MessageBox.Show("Приём отменён. Дата и время снова доступны для записи.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при отмене приёма: " + ex.Message);
            }
        }

        private void AppointmentsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AppointmentListItem selected = AppointmentsListView.SelectedItem as AppointmentListItem;

            if (selected == null)
                return;

            _isLoading = true;

            _selectedAppointmentId = selected.AppointmentId;

            AppointmentDatePicker.BlackoutDates.Clear();

            PatientComboBox.ItemsSource = _allPatients;
            DoctorComboBox.ItemsSource = _allDoctors;

            PatientComboBox.SelectedValue = selected.PatientId;
            DoctorComboBox.SelectedValue = selected.DoctorId;
            CabinetComboBox.SelectedValue = selected.CabinetId;

            AppointmentDatePicker.SelectedDate = selected.AppointmentDateTime.Date;

            StatusComboBox.SelectedValue = selected.Status;
            ComplaintsTextBox.Text = selected.Complaints;
            DoctorNoteTextBox.Text = selected.DoctorNote;

            _isLoading = false;

            UpdateAgeText();
            LoadAvailableTimeSlots();

            foreach (TimeSlotItem item in TimeComboBox.Items)
            {
                if (item.Value == selected.AppointmentDateTime.TimeOfDay &&
                    item.CabinetId == selected.CabinetId)
                {
                    TimeComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadAppointments();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAppointments();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void ClearFields()
        {
            _isLoading = true;

            _selectedAppointmentId = null;

            PatientSearchTextBox.Clear();
            DoctorSearchTextBox.Clear();

            PatientComboBox.ItemsSource = _allPatients;
            DoctorComboBox.ItemsSource = _allDoctors;

            PatientComboBox.SelectedIndex = -1;
            DoctorComboBox.SelectedIndex = -1;
            CabinetComboBox.SelectedIndex = -1;

            AppointmentDatePicker.BlackoutDates.Clear();
            AppointmentDatePicker.SelectedDate = null;

            TimeComboBox.ItemsSource = null;
            TimeComboBox.SelectedIndex = -1;

            StatusComboBox.SelectedValue = 0;

            ComplaintsTextBox.Clear();
            DoctorNoteTextBox.Clear();

            AppointmentsListView.SelectedItem = null;

            AgeTextBlock.Text = "Возраст пациента: не выбран";

            _isLoading = false;

            ApplyDateBlackouts();
        }

        private bool ValidateFields(out DateTime appointmentDateTime)
        {
            appointmentDateTime = DateTime.MinValue;

            if (PatientComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите пациента.");
                return false;
            }

            if (DoctorComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите врача.");
                return false;
            }

            if (AppointmentDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите доступную дату приёма.");
                return false;
            }

            if (TimeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите свободное время приёма.");
                return false;
            }

            TimeSlotItem slot = TimeComboBox.SelectedItem as TimeSlotItem;

            if (slot == null)
            {
                MessageBox.Show("Некорректно выбрано время приёма.");
                return false;
            }

            appointmentDateTime = AppointmentDatePicker.SelectedDate.Value.Date.Add(slot.Value);

            if (StatusComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите статус приёма.");
                return false;
            }

            CabinetComboBox.SelectedValue = slot.CabinetId;

            return true;
        }

        private int GetRussianDayOfWeek(DateTime date)
        {
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    return 1;
                case DayOfWeek.Tuesday:
                    return 2;
                case DayOfWeek.Wednesday:
                    return 3;
                case DayOfWeek.Thursday:
                    return 4;
                case DayOfWeek.Friday:
                    return 5;
                case DayOfWeek.Saturday:
                    return 6;
                case DayOfWeek.Sunday:
                    return 7;
                default:
                    return 1;
            }
        }

        private static int CalculateAge(DateTime birthDate)
        {
            DateTime today = DateTime.Today;

            int age = today.Year - birthDate.Year;

            if (birthDate.Date > today.AddYears(-age))
            {
                age--;
            }

            return age;
        }

        private static string GetStatusText(int status)
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

    public class AppointmentListItem
    {
        public int AppointmentId { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public string DateTimeText { get; set; }

        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public int PatientAge { get; set; }

        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public string SpecialityName { get; set; }

        public int CabinetId { get; set; }
        public string CabinetName { get; set; }

        public int Status { get; set; }
        public string StatusText { get; set; }

        public string Complaints { get; set; }
        public string DoctorNote { get; set; }
    }

    public class ComboItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class PatientComboItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime BirthDate { get; set; }
    }

    public class DoctorComboItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int SpecialityId { get; set; }
        public string SpecialityName { get; set; }
    }

    public class CabinetComboItem
    {
        public int Id { get; set; }
        public string Number { get; set; }
        public string Corpus { get; set; }
        public string Name { get; set; }
    }

    public class TimeSlotItem
    {
        public TimeSpan Value { get; set; }
        public string Text { get; set; }
        public int CabinetId { get; set; }
    }
}