using PolyclinicApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PolyclinicApp.Services;
using System.Diagnostics;

namespace PolyclinicApp.Views
{
    public partial class ReportsWindow : Window
    {
        private List<ReportPatientComboItem> _allPatients =
            new List<ReportPatientComboItem>();

        public ReportsWindow()
        {
            InitializeComponent();

            SetDefaultDates();
            LoadComboBoxes();

            LoadAppointmentsReport();
            LoadDoctorsWorkloadReport();
            LoadLabReport();
        }

        private void SetDefaultDates()
        {
            DateTime start = new DateTime(DateTime.Today.Year, 1, 1);
            DateTime end = new DateTime(DateTime.Today.Year, 12, 31);

            AppointmentsFromDatePicker.SelectedDate = start;
            AppointmentsToDatePicker.SelectedDate = end;

            DoctorsFromDatePicker.SelectedDate = start;
            DoctorsToDatePicker.SelectedDate = end;

            LabFromDatePicker.SelectedDate = start;
            LabToDatePicker.SelectedDate = end;
        }

        private void LoadComboBoxes()
        {
            AppointmentsStatusComboBox.ItemsSource = new List<ReportStatusItem>
            {
                new ReportStatusItem { Id = -1, Name = "Все статусы" },
                new ReportStatusItem { Id = 0, Name = "Назначен" },
                new ReportStatusItem { Id = 1, Name = "В очереди" },
                new ReportStatusItem { Id = 2, Name = "Принят" },
                new ReportStatusItem { Id = 3, Name = "Завершён" },
                new ReportStatusItem { Id = 4, Name = "Отменён" }
            };

            AppointmentsStatusComboBox.SelectedValue = -1;

            LabStatusComboBox.ItemsSource = new List<ReportStatusItem>
            {
                new ReportStatusItem { Id = -1, Name = "Все статусы" },
                new ReportStatusItem { Id = 0, Name = "Назначено" },
                new ReportStatusItem { Id = 1, Name = "Выполнено" },
                new ReportStatusItem { Id = 2, Name = "Отменено" }
            };

            LabStatusComboBox.SelectedValue = -1;

            using (var db = new ПоликлиникаEntities1())
            {
                _allPatients = db.Пациенты
                    .OrderBy(p => p.Фамилия)
                    .Select(p => new ReportPatientComboItem
                    {
                        Id = p.ID_пациента,
                        Name = p.Фамилия + " " + p.Имя + " " + p.Отчество
                    })
                    .ToList();

                PatientComboBox.ItemsSource = _allPatients;
            }
        }

        /* =====================================================
           1. ПРИЁМЫ ЗА ПЕРИОД
           ===================================================== */

        private void LoadAppointmentsReportButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAppointmentsReport();
        }

        private void LoadAppointmentsReport()
        {
            if (AppointmentsFromDatePicker.SelectedDate == null ||
                AppointmentsToDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите период отчёта.");
                return;
            }

            DateTime from = AppointmentsFromDatePicker.SelectedDate.Value.Date;
            DateTime to = AppointmentsToDatePicker.SelectedDate.Value.Date.AddDays(1);

            if (to <= from)
            {
                MessageBox.Show("Дата окончания должна быть позже даты начала.");
                return;
            }


            int status = -1;

            if (AppointmentsStatusComboBox.SelectedValue != null)
            {
                status = Convert.ToInt32(AppointmentsStatusComboBox.SelectedValue);
            }

            using (var db = new ПоликлиникаEntities1())
            {
                var query =
    from a in db.Приёмы
    join p in db.Пациенты
        on a.ID_пациента equals p.ID_пациента
    join d in db.Врачи
        on a.ID_врача equals d.ID_врача
    join c in db.Кабинеты
        on a.ID_кабинета equals c.ID_кабинета
    select new
    {
        DoctorId = d.ID_врача,
        AppointmentId = a.ID_приёма,
        DateTime = a.Дата_время,
        Status = a.Статус,
        Complaints = a.Жалобы,
        PatientName = p.Фамилия + " " + p.Имя + " " + p.Отчество,
        DoctorName = d.Фамилия + " " + d.Имя + " " + d.Отчество,
        CabinetName = c.Номер + " / " + c.Корпус
    };
                if (CurrentUser.IsDoctor && CurrentUser.DoctorId != null)
                {
                    int doctorId = CurrentUser.DoctorId.Value;
                    query = query.Where(x => x.DoctorId == doctorId);
                }

                query = query.Where(x =>
                    x.DateTime >= from &&
                    x.DateTime < to);

                if (status != -1)
                {
                    query = query.Where(x => x.Status == status);
                }

                List<AppointmentsReportItem> list = query
                    .OrderBy(x => x.DateTime)
                    .ToList()
                    .Select(x => new AppointmentsReportItem
                    {
                        AppointmentId = x.AppointmentId,
                        DateTimeText = x.DateTime.ToString("dd.MM.yyyy HH:mm"),
                        PatientName = x.PatientName,
                        DoctorName = x.DoctorName,
                        CabinetName = x.CabinetName,
                        StatusText = GetAppointmentStatusText(x.Status),
                        Complaints = x.Complaints
                    })
                    .ToList();

                AppointmentsReportListView.ItemsSource = list;
                AppointmentsSummaryTextBlock.Text = "Записей: " + list.Count;
            }
        }

        /* =====================================================
           2. ЗАГРУЖЕННОСТЬ ВРАЧЕЙ
           ===================================================== */

        private void LoadDoctorsWorkloadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDoctorsWorkloadReport();
        }

        private void LoadDoctorsWorkloadReport()
        {
            if (DoctorsFromDatePicker.SelectedDate == null ||
                DoctorsToDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите период отчёта.");
                return;
            }

            DateTime from = DoctorsFromDatePicker.SelectedDate.Value.Date;
            DateTime to = DoctorsToDatePicker.SelectedDate.Value.Date.AddDays(1);

            if (to <= from)
            {
                MessageBox.Show("Дата окончания должна быть позже даты начала.");
                return;
            }

            using (var db = new ПоликлиникаEntities1())
            {
                var doctors =
                    (from d in db.Врачи
                     join s in db.Специальности
                        on d.ID_специальности equals s.ID_специальности
                     orderby d.Фамилия
                     select new
                     {
                         DoctorId = d.ID_врача,
                         DoctorName = d.Фамилия + " " + d.Имя + " " + d.Отчество,
                         SpecialityName = s.Название
                     })
                    .ToList();

                List<DoctorsWorkloadReportItem> list =
                    new List<DoctorsWorkloadReportItem>();

                foreach (var doctor in doctors)
                {
                    int total = db.Приёмы.Count(a =>
                        a.ID_врача == doctor.DoctorId &&
                        a.Дата_время >= from &&
                        a.Дата_время < to);

                    int assigned = db.Приёмы.Count(a =>
                        a.ID_врача == doctor.DoctorId &&
                        a.Дата_время >= from &&
                        a.Дата_время < to &&
                        a.Статус == 0);

                    int completed = db.Приёмы.Count(a =>
                        a.ID_врача == doctor.DoctorId &&
                        a.Дата_время >= from &&
                        a.Дата_время < to &&
                        a.Статус == 3);

                    int canceled = db.Приёмы.Count(a =>
                        a.ID_врача == doctor.DoctorId &&
                        a.Дата_время >= from &&
                        a.Дата_время < to &&
                        a.Статус == 4);

                    int prescriptions = db.Лекарственные_назначения.Count(p =>
                        p.ID_врача == doctor.DoctorId);

                    int labs = db.Лабораторные_исследования.Count(l =>
                        l.ID_врача == doctor.DoctorId &&
                        l.Дата_назначения >= from &&
                        l.Дата_назначения < to);

                    list.Add(new DoctorsWorkloadReportItem
                    {
                        DoctorId = doctor.DoctorId,
                        DoctorName = doctor.DoctorName,
                        SpecialityName = doctor.SpecialityName,
                        TotalAppointments = total,
                        AssignedCount = assigned,
                        CompletedCount = completed,
                        CanceledCount = canceled,
                        PrescriptionsCount = prescriptions,
                        LabResearchCount = labs
                    });
                }

                list = list
                    .OrderByDescending(x => x.TotalAppointments)
                    .ToList();

                DoctorsWorkloadListView.ItemsSource = list;
                DoctorsSummaryTextBlock.Text = "Врачей: " + list.Count +
                                               " | Приёмов: " + list.Sum(x => x.TotalAppointments);
            }
        }
        private void ExportDoctorsWorkloadPdfButton_Click(object sender, RoutedEventArgs e)
        {
            List<DoctorsWorkloadReportItem> items =
                DoctorsWorkloadListView.ItemsSource as List<DoctorsWorkloadReportItem>;

            if (items == null || items.Count == 0)
            {
                MessageBox.Show("Нет данных для формирования PDF.");
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "PDF-файл (*.pdf)|*.pdf";
            dialog.FileName = "Отчет_загруженность_врачей_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".pdf";

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                List<string[]> rows = items.Select(x => new string[]
                {
            x.DoctorId.ToString(),
            x.DoctorName,
            x.SpecialityName,
            x.TotalAppointments.ToString(),
            x.AssignedCount.ToString(),
            x.CompletedCount.ToString(),
            x.CanceledCount.ToString(),
            x.PrescriptionsCount.ToString(),
            x.LabResearchCount.ToString()
                }).ToList();

                PdfReportService.CreateTableReport(
                    dialog.FileName,
                    "Отчёт по загруженности врачей",
                    "Дата формирования: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") +
                    "\nКоличество врачей: " + items.Count,
                    new string[]
                    {
                "ID", "Врач", "Специальность", "Всего",
                "Назначено", "Завершено", "Отменено", "Назначений", "Анализов"
                    },
                    new float[] { 0.7f, 2.4f, 1.8f, 1.0f, 1.0f, 1.0f, 1.0f, 1.1f, 1.0f },
                    rows);

                MessageBox.Show("PDF-отчёт сформирован.");
                Process.Start(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при формировании PDF: " + ex.Message);
            }
        }

        /* =====================================================
           3. ИСТОРИЯ ПАЦИЕНТА
           ===================================================== */

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

        private void LoadPatientHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            LoadPatientHistoryReport();
        }

        private void LoadPatientHistoryReport()
        {
            if (PatientComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите пациента.");
                return;
            }

            int patientId = Convert.ToInt32(PatientComboBox.SelectedValue);

            List<PatientHistoryReportItem> result =
                new List<PatientHistoryReportItem>();

            using (var db = new ПоликлиникаEntities1())
            {
                var appointments =
                    (from a in db.Приёмы
                     join d in db.Врачи
                        on a.ID_врача equals d.ID_врача
                     join c in db.Кабинеты
                        on a.ID_кабинета equals c.ID_кабинета
                     where a.ID_пациента == patientId
                     select new
                     {
                         Date = a.Дата_время,
                         DoctorName = d.Фамилия + " " + d.Имя + " " + d.Отчество,
                         CabinetName = c.Номер,
                         Status = a.Статус,
                         Complaints = a.Жалобы
                     })
                    .ToList();

                foreach (var item in appointments)
                {
                    result.Add(new PatientHistoryReportItem
                    {
                        Date = item.Date,
                        DateText = item.Date.ToString("dd.MM.yyyy HH:mm"),
                        RecordType = "Приём",
                        DoctorName = item.DoctorName,
                        StatusText = GetAppointmentStatusText(item.Status),
                        Description = "Кабинет: " + item.CabinetName + ". Жалобы: " + item.Complaints
                    });
                }

                var medicalRecords =
                    (from m in db.Медицинские_записи
                     join d in db.Врачи
                        on m.ID_врача equals d.ID_врача
                     where m.ID_пациента == patientId
                     select new
                     {
                         Date = m.Дата_время,
                         DoctorName = d.Фамилия + " " + d.Имя + " " + d.Отчество,
                         Content = m.Содержание
                     })
                    .ToList();

                foreach (var item in medicalRecords)
                {
                    result.Add(new PatientHistoryReportItem
                    {
                        Date = item.Date,
                        DateText = item.Date.ToString("dd.MM.yyyy HH:mm"),
                        RecordType = "Мед. запись",
                        DoctorName = item.DoctorName,
                        StatusText = "",
                        Description = item.Content
                    });
                }

                var prescriptions =
                    (from p in db.Лекарственные_назначения
                     join d in db.Врачи
                        on p.ID_врача equals d.ID_врача
                     where p.ID_пациента == patientId
                     select new
                     {
                         DoctorName = d.Фамилия + " " + d.Имя + " " + d.Отчество,
                         Medicine = p.Лекарство,
                         Dosage = p.Дозировка,
                         Period = p.Период_приёма,
                         Status = p.Статус
                     })
                    .ToList();

                foreach (var item in prescriptions)
                {
                    result.Add(new PatientHistoryReportItem
                    {
                        Date = DateTime.MinValue,
                        DateText = "",
                        RecordType = "Назначение",
                        DoctorName = item.DoctorName,
                        StatusText = GetPrescriptionStatusText(item.Status),
                        Description = item.Medicine + ", " + item.Dosage + ", " + item.Period
                    });
                }

                var labs =
                    (from l in db.Лабораторные_исследования
                     join d in db.Врачи
                        on l.ID_врача equals d.ID_врача
                     where l.ID_пациента == patientId
                     select new
                     {
                         Date = l.Дата_назначения,
                         DoctorName = d.Фамилия + " " + d.Имя + " " + d.Отчество,
                         Type = l.Тип_анализа,
                         Result = l.Результат,
                         Status = l.Статус
                     })
                    .ToList();

                foreach (var item in labs)
                {
                    result.Add(new PatientHistoryReportItem
                    {
                        Date = item.Date,
                        DateText = item.Date.ToString("dd.MM.yyyy"),
                        RecordType = "Лаборатория",
                        DoctorName = item.DoctorName,
                        StatusText = GetLabStatusText(item.Status),
                        Description = item.Type + ". Результат: " + item.Result
                    });
                }
            }

            result = result
                .OrderByDescending(x => x.Date)
                .ToList();

            PatientHistoryListView.ItemsSource = result;
            PatientHistorySummaryTextBlock.Text = "Записей: " + result.Count;
        }
        private void ExportPatientHistoryPdfButton_Click(object sender, RoutedEventArgs e)
        {
            List<PatientHistoryReportItem> items =
                PatientHistoryListView.ItemsSource as List<PatientHistoryReportItem>;

            if (items == null || items.Count == 0)
            {
                MessageBox.Show("Нет данных для формирования PDF.");
                return;
            }

            string patientName = "Пациент не выбран";

            ReportPatientComboItem selectedPatient =
                PatientComboBox.SelectedItem as ReportPatientComboItem;

            if (selectedPatient != null)
            {
                patientName = selectedPatient.Name;
            }

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "PDF-файл (*.pdf)|*.pdf";
            dialog.FileName = "История_пациента_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".pdf";

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                List<string[]> rows = items.Select(x => new string[]
                {
            x.DateText,
            x.RecordType,
            x.DoctorName,
            x.StatusText,
            x.Description
                }).ToList();

                PdfReportService.CreateTableReport(
                    dialog.FileName,
                    "История пациента",
                    "Пациент: " + patientName +
                    "\nДата формирования: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") +
                    "\nКоличество записей: " + items.Count,
                    new string[]
                    {
                "Дата", "Тип записи", "Врач", "Статус", "Описание"
                    },
                    new float[] { 1.4f, 1.5f, 2.3f, 1.2f, 5.0f },
                    rows);

                MessageBox.Show("PDF-отчёт сформирован.");
                Process.Start(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при формировании PDF: " + ex.Message);
            }
        }

        /* =====================================================
           4. ЛАБОРАТОРНЫЙ ОТЧЁТ
           ===================================================== */

        private void LoadLabReportButton_Click(object sender, RoutedEventArgs e)
        {
            LoadLabReport();
        }

        private void LoadLabReport()
        {
            if (LabFromDatePicker.SelectedDate == null ||
                LabToDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите период отчёта.");
                return;
            }

            DateTime from = LabFromDatePicker.SelectedDate.Value.Date;
            DateTime to = LabToDatePicker.SelectedDate.Value.Date.AddDays(1);

            if (to <= from)
            {
                MessageBox.Show("Дата окончания должна быть позже даты начала.");
                return;
            }

            int status = -1;

            if (LabStatusComboBox.SelectedValue != null)
            {
                status = Convert.ToInt32(LabStatusComboBox.SelectedValue);
            }

            using (var db = new ПоликлиникаEntities1())
            {
                var query =
    from l in db.Лабораторные_исследования
    join p in db.Пациенты
        on l.ID_пациента equals p.ID_пациента
    join d in db.Врачи
        on l.ID_врача equals d.ID_врача
    select new
    {
        DoctorId = d.ID_врача,
        ResearchId = l.ID_исследования,
        AnalysisType = l.Тип_анализа,
        OrderDate = l.Дата_назначения,
        CompletionDate = l.Дата_выполнения,
        Result = l.Результат,
        Status = l.Статус,
        PatientName = p.Фамилия + " " + p.Имя + " " + p.Отчество,
        DoctorName = d.Фамилия + " " + d.Имя + " " + d.Отчество
    };
                if (CurrentUser.IsDoctor && CurrentUser.DoctorId != null)
                {
                    int doctorId = CurrentUser.DoctorId.Value;
                    query = query.Where(x => x.DoctorId == doctorId);
                }

                query = query.Where(x =>
                    x.OrderDate >= from &&
                    x.OrderDate < to);

                if (status != -1)
                {
                    query = query.Where(x => x.Status == status);
                }

                List<LabReportItem> list = query
                    .OrderByDescending(x => x.OrderDate)
                    .ToList()
                    .Select(x => new LabReportItem
                    {
                        ResearchId = x.ResearchId,
                        AnalysisType = x.AnalysisType,
                        PatientName = x.PatientName,
                        DoctorName = x.DoctorName,
                        OrderDateText = x.OrderDate.ToString("dd.MM.yyyy"),
                        CompletionDateText = x.CompletionDate == null
                            ? ""
                            : x.CompletionDate.Value.ToString("dd.MM.yyyy"),
                        StatusText = GetLabStatusText(x.Status),
                        Result = x.Result
                    })
                    .ToList();

                LabReportListView.ItemsSource = list;
                LabSummaryTextBlock.Text = "Исследований: " + list.Count;
            }
        }
        private void ExportLabReportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            List<LabReportItem> items =
                LabReportListView.ItemsSource as List<LabReportItem>;

            if (items == null || items.Count == 0)
            {
                MessageBox.Show("Нет данных для формирования PDF.");
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "PDF-файл (*.pdf)|*.pdf";
            dialog.FileName = "Отчет_лаборатория_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".pdf";

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                List<string[]> rows = items.Select(x => new string[]
                {
            x.ResearchId.ToString(),
            x.AnalysisType,
            x.PatientName,
            x.DoctorName,
            x.OrderDateText,
            x.CompletionDateText,
            x.StatusText,
            x.Result
                }).ToList();

                PdfReportService.CreateTableReport(
                    dialog.FileName,
                    "Отчёт по лабораторным исследованиям",
                    "Дата формирования: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") +
                    "\nКоличество исследований: " + items.Count,
                    new string[]
                    {
                "ID", "Тип анализа", "Пациент", "Врач",
                "Назначено", "Выполнено", "Статус", "Результат"
                    },
                    new float[] { 0.6f, 2.0f, 2.1f, 2.1f, 1.2f, 1.2f, 1.1f, 3.0f },
                    rows);

                MessageBox.Show("PDF-отчёт сформирован.");
                Process.Start(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при формировании PDF: " + ex.Message);
            }
        }

        /* =====================================================
           СЛУЖЕБНЫЕ МЕТОДЫ
           ===================================================== */

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

        private static string GetLabStatusText(int status)
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
        private void ExportAppointmentsPdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppointmentsReportListView.ItemsSource == null)
            {
                MessageBox.Show("Сначала сформируйте отчёт.");
                return;
            }

            List<AppointmentsReportItem> reportItems =
                AppointmentsReportListView.ItemsSource as List<AppointmentsReportItem>;

            if (reportItems == null || reportItems.Count == 0)
            {
                MessageBox.Show("Нет данных для формирования PDF.");
                return;
            }

            if (AppointmentsFromDatePicker.SelectedDate == null ||
                AppointmentsToDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите период отчёта.");
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "PDF-файл (*.pdf)|*.pdf";
            dialog.FileName = "Отчет_по_приемам_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".pdf";

            bool? result = dialog.ShowDialog();

            if (result != true)
                return;

            try
            {
                string statusText = "Все статусы";

                if (AppointmentsStatusComboBox.SelectedItem is ReportStatusItem selectedStatus)
                {
                    statusText = selectedStatus.Name;
                }

                List<AppointmentPdfItem> pdfItems = reportItems
                    .Select(x => new AppointmentPdfItem
                    {
                        AppointmentId = x.AppointmentId,
                        DateTimeText = x.DateTimeText,
                        PatientName = x.PatientName,
                        DoctorName = x.DoctorName,
                        CabinetName = x.CabinetName,
                        StatusText = x.StatusText,
                        Complaints = x.Complaints
                    })
                    .ToList();

                PdfReportService.CreateAppointmentsReport(
                    dialog.FileName,
                    AppointmentsFromDatePicker.SelectedDate.Value,
                    AppointmentsToDatePicker.SelectedDate.Value,
                    statusText,
                    pdfItems);

                MessageBox.Show("PDF-отчёт сформирован.");

                Process.Start(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при формировании PDF: " + ex.Message);
            }
        }
    }

    public class ReportStatusItem
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class ReportPatientComboItem
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class AppointmentsReportItem
    {
        public int AppointmentId { get; set; }

        public string DateTimeText { get; set; }

        public string PatientName { get; set; }

        public string DoctorName { get; set; }

        public string CabinetName { get; set; }

        public string StatusText { get; set; }

        public string Complaints { get; set; }
    }

    public class DoctorsWorkloadReportItem
    {
        public int DoctorId { get; set; }

        public string DoctorName { get; set; }

        public string SpecialityName { get; set; }

        public int TotalAppointments { get; set; }

        public int AssignedCount { get; set; }

        public int CompletedCount { get; set; }

        public int CanceledCount { get; set; }

        public int PrescriptionsCount { get; set; }

        public int LabResearchCount { get; set; }
    }

    public class PatientHistoryReportItem
    {
        public DateTime Date { get; set; }

        public string DateText { get; set; }

        public string RecordType { get; set; }

        public string DoctorName { get; set; }

        public string StatusText { get; set; }

        public string Description { get; set; }
    }

    public class LabReportItem
    {
        public int ResearchId { get; set; }

        public string AnalysisType { get; set; }

        public string PatientName { get; set; }

        public string DoctorName { get; set; }

        public string OrderDateText { get; set; }

        public string CompletionDateText { get; set; }

        public string StatusText { get; set; }

        public string Result { get; set; }
    }
}