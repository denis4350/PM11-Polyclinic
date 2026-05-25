using PolyclinicApp.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PolyclinicApp.Views
{
    public partial class DiagnosesWindow : Window
    {
        private string _selectedDiagnosisCode;

        public DiagnosesWindow()
        {
            InitializeComponent();
            LoadDiagnoses();
        }

        private void LoadDiagnoses()
        {
            using (var db = new ПоликлиникаEntities1())
            {
                string search = SearchTextBox.Text.Trim();

                var query = db.Диагнозы_МКБ10.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(d =>
                        d.Код_диагноза.Contains(search) ||
                        d.Название.Contains(search) ||
                        d.Группа.Contains(search));
                }

                DiagnosesListView.ItemsSource = query
                    .OrderBy(d => d.Код_диагноза)
                    .ToList()
                    .Select(d => new DiagnosisListItem
                    {
                        Code = d.Код_диагноза,
                        Name = d.Название,
                        Group = d.Группа,
                        AppointmentCount = db.Приёмы.Count(p => p.Код_диагноза == d.Код_диагноза)
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
                    string code = CodeTextBox.Text.Trim();
                    string name = NameTextBox.Text.Trim();
                    string group = GroupTextBox.Text.Trim();

                    bool exists = db.Диагнозы_МКБ10.Any(d => d.Код_диагноза == code);

                    if (exists)
                    {
                        MessageBox.Show("Диагноз с таким кодом уже существует.");
                        return;
                    }

                    Диагнозы_МКБ10 diagnosis = new Диагнозы_МКБ10();

                    diagnosis.Код_диагноза = code;
                    diagnosis.Название = name;
                    diagnosis.Группа = group;

                    db.Диагнозы_МКБ10.Add(diagnosis);
                    db.SaveChanges();
                }

                LoadDiagnoses();
                ClearFields();

                MessageBox.Show("Диагноз добавлен.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении диагноза: " + ex.Message);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedDiagnosisCode))
            {
                MessageBox.Show("Выберите диагноз для изменения.");
                return;
            }

            if (!ValidateFields())
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Диагнозы_МКБ10 diagnosis = db.Диагнозы_МКБ10
                        .FirstOrDefault(d => d.Код_диагноза == _selectedDiagnosisCode);

                    if (diagnosis == null)
                    {
                        MessageBox.Show("Диагноз не найден.");
                        return;
                    }

                    string newCode = CodeTextBox.Text.Trim();
                    string newName = NameTextBox.Text.Trim();
                    string newGroup = GroupTextBox.Text.Trim();

                    if (newCode != _selectedDiagnosisCode)
                    {
                        bool usedInAppointments = db.Приёмы.Any(p =>
                            p.Код_диагноза == _selectedDiagnosisCode);

                        if (usedInAppointments)
                        {
                            MessageBox.Show("Нельзя изменить код диагноза, так как он используется в приёмах.");
                            return;
                        }

                        bool codeExists = db.Диагнозы_МКБ10.Any(d =>
                            d.Код_диагноза == newCode);

                        if (codeExists)
                        {
                            MessageBox.Show("Диагноз с новым кодом уже существует.");
                            return;
                        }

                        db.Диагнозы_МКБ10.Remove(diagnosis);
                        db.SaveChanges();

                        Диагнозы_МКБ10 newDiagnosis = new Диагнозы_МКБ10();

                        newDiagnosis.Код_диагноза = newCode;
                        newDiagnosis.Название = newName;
                        newDiagnosis.Группа = newGroup;

                        db.Диагнозы_МКБ10.Add(newDiagnosis);
                    }
                    else
                    {
                        diagnosis.Название = newName;
                        diagnosis.Группа = newGroup;
                    }

                    db.SaveChanges();
                }

                LoadDiagnoses();
                ClearFields();

                MessageBox.Show("Диагноз изменён.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при изменении диагноза: " + ex.Message);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedDiagnosisCode))
            {
                MessageBox.Show("Выберите диагноз для удаления.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Удалить выбранный диагноз?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Диагнозы_МКБ10 diagnosis = db.Диагнозы_МКБ10
                        .FirstOrDefault(d => d.Код_диагноза == _selectedDiagnosisCode);

                    if (diagnosis == null)
                    {
                        MessageBox.Show("Диагноз не найден.");
                        return;
                    }

                    bool usedInAppointments = db.Приёмы.Any(p =>
                        p.Код_диагноза == diagnosis.Код_диагноза);

                    if (usedInAppointments)
                    {
                        MessageBox.Show("Нельзя удалить диагноз, так как он используется в приёмах.");
                        return;
                    }

                    db.Диагнозы_МКБ10.Remove(diagnosis);
                    db.SaveChanges();
                }

                LoadDiagnoses();
                ClearFields();

                MessageBox.Show("Диагноз удалён.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении диагноза: " + ex.Message);
            }
        }

        private void DiagnosesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DiagnosisListItem selected = DiagnosesListView.SelectedItem as DiagnosisListItem;

            if (selected == null)
                return;

            _selectedDiagnosisCode = selected.Code;

            CodeTextBox.Text = selected.Code;
            NameTextBox.Text = selected.Name;
            GroupTextBox.Text = selected.Group;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadDiagnoses();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDiagnoses();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void ClearFields()
        {
            _selectedDiagnosisCode = null;

            CodeTextBox.Clear();
            NameTextBox.Clear();
            GroupTextBox.Clear();

            DiagnosesListView.SelectedItem = null;
        }

        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(CodeTextBox.Text))
            {
                MessageBox.Show("Введите код диагноза.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите название диагноза.");
                return false;
            }

            return true;
        }
    }

    public class DiagnosisListItem
    {
        public string Code { get; set; }

        public string Name { get; set; }

        public string Group { get; set; }

        public int AppointmentCount { get; set; }
    }
}