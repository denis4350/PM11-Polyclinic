using PolyclinicApp.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PolyclinicApp.Views
{
    public partial class PatientsWindow : Window
    {
        private Пациенты _selectedPatient;

        public PatientsWindow()
        {
            InitializeComponent();
            LoadPatients();
        }

        private void LoadPatients()
        {
            using (var db = new ПоликлиникаEntities1())
            {
                PatientsListView.ItemsSource = db.Пациенты
                    .OrderBy(p => p.Фамилия)
                    .ToList();
            }
        }

        private void SearchPatients()
        {
            string search = SearchTextBox.Text.Trim();

            using (var db = new ПоликлиникаEntities1())
            {
                var query = db.Пациенты.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(p => p.Фамилия.Contains(search));
                }

                PatientsListView.ItemsSource = query
                    .OrderBy(p => p.Фамилия)
                    .ToList();
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidatePatientFields())
                return;

            using (var db = new ПоликлиникаEntities1())
            {
                int newId = 1;

                if (db.Пациенты.Any())
                    newId = db.Пациенты.Max(p => p.ID_пациента) + 1;

                Пациенты patient = new Пациенты();

                patient.ID_пациента = newId;
                patient.Фамилия = LastNameTextBox.Text.Trim();
                patient.Имя = FirstNameTextBox.Text.Trim();
                patient.Отчество = MiddleNameTextBox.Text.Trim();
                patient.Дата_рождения = new DateTime(2000, 1, 1);
                patient.Пол = "М";
                patient.СНИЛС = null;
                patient.Телефон = PhoneTextBox.Text.Trim();

                db.Пациенты.Add(patient);
                db.SaveChanges();
            }

            LoadPatients();
            ClearFields();

            MessageBox.Show("Пациент добавлен.");
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPatient == null)
            {
                MessageBox.Show("Выберите пациента для изменения.");
                return;
            }

            if (!ValidatePatientFields())
                return;

            using (var db = new ПоликлиникаEntities1())
            {
                Пациенты patient = db.Пациенты
                    .FirstOrDefault(p => p.ID_пациента == _selectedPatient.ID_пациента);

                if (patient == null)
                {
                    MessageBox.Show("Пациент не найден.");
                    return;
                }

                patient.Фамилия = LastNameTextBox.Text.Trim();
                patient.Имя = FirstNameTextBox.Text.Trim();
                patient.Отчество = MiddleNameTextBox.Text.Trim();
                patient.Телефон = PhoneTextBox.Text.Trim();

                db.SaveChanges();
            }

            LoadPatients();
            ClearFields();

            MessageBox.Show("Данные пациента изменены.");
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPatient == null)
            {
                MessageBox.Show("Выберите пациента для удаления.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Удалить выбранного пациента?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Пациенты patient = db.Пациенты
                        .FirstOrDefault(p => p.ID_пациента == _selectedPatient.ID_пациента);

                    if (patient == null)
                    {
                        MessageBox.Show("Пациент не найден.");
                        return;
                    }

                    db.Пациенты.Remove(patient);
                    db.SaveChanges();
                }

                LoadPatients();
                ClearFields();

                MessageBox.Show("Пациент удален.");
            }
            catch
            {
                MessageBox.Show("Нельзя удалить пациента, так как с ним связаны приемы, карта или назначения.");
            }
        }

        private void PatientsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedPatient = PatientsListView.SelectedItem as Пациенты;

            if (_selectedPatient == null)
                return;

            LastNameTextBox.Text = _selectedPatient.Фамилия;
            FirstNameTextBox.Text = _selectedPatient.Имя;
            MiddleNameTextBox.Text = _selectedPatient.Отчество;
            PhoneTextBox.Text = _selectedPatient.Телефон;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchPatients();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadPatients();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void ClearFields()
        {
            LastNameTextBox.Clear();
            FirstNameTextBox.Clear();
            MiddleNameTextBox.Clear();
            PhoneTextBox.Clear();

            _selectedPatient = null;
            PatientsListView.SelectedItem = null;
        }

        private bool ValidatePatientFields()
        {
            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                MessageBox.Show("Введите фамилию пациента.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                MessageBox.Show("Введите имя пациента.");
                return false;
            }

            return true;
        }
    }
}