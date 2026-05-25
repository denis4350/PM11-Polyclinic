using PolyclinicApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PolyclinicApp.Services;

namespace PolyclinicApp.Views
{
    public partial class CabinetsWindow : Window
    {
        private int? _selectedCabinetId;

        public CabinetsWindow()
        {
            InitializeComponent();

            LoadCorpusList();
            ApplyRoleAccess();
            LoadCabinets();
        }
        private void ApplyRoleAccess()
        {
            if (!AccessRules.CanEditCabinets())
            {
                AddButton.IsEnabled = false;
                EditButton.IsEnabled = false;
                DeleteButton.IsEnabled = false;
            }

            if (CurrentUser.IsLaborant)
            {
                CorpusComboBox.Text = "Лаборатория";
                CorpusComboBox.IsEnabled = false;
            }
        }

        private void LoadCorpusList()
        {
            CorpusComboBox.ItemsSource = new List<string>
            {
                "Основной",
                "Детский",
                "Диагностика",
                "Хирургия",
                "Лаборатория",
                "Кардиология",
                "Офтальмология",
                "Неврология",
                "Администрация"
            };
        }

        private void LoadCabinets()
        {
            using (var db = new ПоликлиникаEntities1())
            {
                string search = SearchTextBox.Text.Trim();

                var query = db.Кабинеты.AsQueryable();

                // Лаборант видит только лабораторные кабинеты
                if (CurrentUser.IsLaborant)
                {
                    query = query.Where(c => c.Корпус.Contains("Лаборатория"));
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(c =>
                        c.Номер.Contains(search) ||
                        c.Корпус.Contains(search) ||
                        c.Телефон.Contains(search));
                }

                CabinetsListView.ItemsSource = query
                    .OrderBy(c => c.Номер)
                    .ToList()
                    .Select(c => new CabinetListItem
                    {
                        CabinetId = c.ID_кабинета,
                        Number = c.Номер,
                        Corpus = c.Корпус,
                        Phone = c.Телефон,
                        ScheduleCount = db.Расписание.Count(r => r.ID_кабинета == c.ID_кабинета),
                        AppointmentCount = db.Приёмы.Count(a => a.ID_кабинета == c.ID_кабинета)
                    })
                    .ToList();
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    string number = NumberTextBox.Text.Trim();
                    string corpus = CorpusComboBox.Text.Trim();

                    bool exists = db.Кабинеты.Any(c =>
                        c.Номер == number &&
                        c.Корпус == corpus);

                    if (exists)
                    {
                        MessageBox.Show("Кабинет с таким номером и корпусом уже существует.");
                        return;
                    }

                    int newId = 1;

                    if (db.Кабинеты.Any())
                    {
                        newId = db.Кабинеты.Max(c => c.ID_кабинета) + 1;
                    }

                    Кабинеты cabinet = new Кабинеты();

                    cabinet.ID_кабинета = newId;
                    cabinet.Номер = number;
                    cabinet.Корпус = corpus;
                    cabinet.Телефон = PhoneTextBox.Text.Trim();

                    db.Кабинеты.Add(cabinet);
                    db.SaveChanges();
                }

                LoadCabinets();
                ClearFields();

                MessageBox.Show("Кабинет добавлен.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении кабинета: " + ex.Message);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCabinetId == null)
            {
                MessageBox.Show("Выберите кабинет для изменения.");
                return;
            }

            if (!ValidateFields())
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Кабинеты cabinet = db.Кабинеты
                        .FirstOrDefault(c => c.ID_кабинета == _selectedCabinetId.Value);

                    if (cabinet == null)
                    {
                        MessageBox.Show("Кабинет не найден.");
                        return;
                    }

                    string number = NumberTextBox.Text.Trim();
                    string corpus = CorpusComboBox.Text.Trim();

                    bool exists = db.Кабинеты.Any(c =>
                        c.ID_кабинета != cabinet.ID_кабинета &&
                        c.Номер == number &&
                        c.Корпус == corpus);

                    if (exists)
                    {
                        MessageBox.Show("Другой кабинет с таким номером и корпусом уже существует.");
                        return;
                    }

                    cabinet.Номер = number;
                    cabinet.Корпус = corpus;
                    cabinet.Телефон = PhoneTextBox.Text.Trim();

                    db.SaveChanges();
                }

                LoadCabinets();
                ClearFields();

                MessageBox.Show("Кабинет изменён.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при изменении кабинета: " + ex.Message);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCabinetId == null)
            {
                MessageBox.Show("Выберите кабинет для удаления.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Удалить выбранный кабинет?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Кабинеты cabinet = db.Кабинеты
                        .FirstOrDefault(c => c.ID_кабинета == _selectedCabinetId.Value);

                    if (cabinet == null)
                    {
                        MessageBox.Show("Кабинет не найден.");
                        return;
                    }

                    bool usedInSchedule = db.Расписание.Any(r =>
                        r.ID_кабинета == cabinet.ID_кабинета);

                    if (usedInSchedule)
                    {
                        MessageBox.Show("Нельзя удалить кабинет, так как он используется в расписании.");
                        return;
                    }

                    bool usedInAppointments = db.Приёмы.Any(a =>
                        a.ID_кабинета == cabinet.ID_кабинета);

                    if (usedInAppointments)
                    {
                        MessageBox.Show("Нельзя удалить кабинет, так как с ним связаны приёмы.");
                        return;
                    }

                    db.Кабинеты.Remove(cabinet);
                    db.SaveChanges();
                }

                LoadCabinets();
                ClearFields();

                MessageBox.Show("Кабинет удалён.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении кабинета: " + ex.Message);
            }
        }

        private void CabinetsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CabinetListItem selected = CabinetsListView.SelectedItem as CabinetListItem;

            if (selected == null)
                return;

            _selectedCabinetId = selected.CabinetId;

            NumberTextBox.Text = selected.Number;
            CorpusComboBox.Text = selected.Corpus;
            PhoneTextBox.Text = selected.Phone;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadCabinets();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadCabinets();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void ClearFields()
        {
            _selectedCabinetId = null;

            NumberTextBox.Clear();
            CorpusComboBox.SelectedIndex = -1;
            CorpusComboBox.Text = "";
            PhoneTextBox.Clear();

            CabinetsListView.SelectedItem = null;
        }

        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(NumberTextBox.Text))
            {
                MessageBox.Show("Введите номер кабинета.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(CorpusComboBox.Text))
            {
                MessageBox.Show("Введите или выберите корпус.");
                return false;
            }

            return true;
        }
    }

    public class CabinetListItem
    {
        public int CabinetId { get; set; }

        public string Number { get; set; }

        public string Corpus { get; set; }

        public string Phone { get; set; }

        public int ScheduleCount { get; set; }

        public int AppointmentCount { get; set; }
    }
}