using PolyclinicApp.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PolyclinicApp.Views
{
    public partial class SpecialitiesWindow : Window
    {
        private int? _selectedSpecialityId;

        public SpecialitiesWindow()
        {
            InitializeComponent();
            LoadSpecialities();
        }

        private void LoadSpecialities()
        {
            using (var db = new ПоликлиникаEntities1())
            {
                string search = SearchTextBox.Text.Trim();

                var query = db.Специальности.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(s => s.Название.Contains(search));
                }

                SpecialitiesListView.ItemsSource = query
                    .OrderBy(s => s.Название)
                    .ToList()
                    .Select(s => new SpecialityListItem
                    {
                        SpecialityId = s.ID_специальности,
                        Name = s.Название,
                        DoctorsCount = db.Врачи.Count(d => d.ID_специальности == s.ID_специальности)
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
                    string name = NameTextBox.Text.Trim();

                    bool exists = db.Специальности.Any(s => s.Название == name);

                    if (exists)
                    {
                        MessageBox.Show("Такая специальность уже существует.");
                        return;
                    }

                    int newId = 1;

                    if (db.Специальности.Any())
                    {
                        newId = db.Специальности.Max(s => s.ID_специальности) + 1;
                    }

                    Специальности speciality = new Специальности();

                    speciality.ID_специальности = newId;
                    speciality.Название = name;

                    db.Специальности.Add(speciality);
                    db.SaveChanges();
                }

                LoadSpecialities();
                ClearFields();

                MessageBox.Show("Специальность добавлена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении специальности: " + ex.Message);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSpecialityId == null)
            {
                MessageBox.Show("Выберите специальность для изменения.");
                return;
            }

            if (!ValidateFields())
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Специальности speciality = db.Специальности
                        .FirstOrDefault(s => s.ID_специальности == _selectedSpecialityId.Value);

                    if (speciality == null)
                    {
                        MessageBox.Show("Специальность не найдена.");
                        return;
                    }

                    string name = NameTextBox.Text.Trim();

                    bool exists = db.Специальности.Any(s =>
                        s.ID_специальности != speciality.ID_специальности &&
                        s.Название == name);

                    if (exists)
                    {
                        MessageBox.Show("Другая специальность с таким названием уже существует.");
                        return;
                    }

                    speciality.Название = name;

                    db.SaveChanges();
                }

                LoadSpecialities();
                ClearFields();

                MessageBox.Show("Специальность изменена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при изменении специальности: " + ex.Message);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSpecialityId == null)
            {
                MessageBox.Show("Выберите специальность для удаления.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Удалить выбранную специальность?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Специальности speciality = db.Специальности
                        .FirstOrDefault(s => s.ID_специальности == _selectedSpecialityId.Value);

                    if (speciality == null)
                    {
                        MessageBox.Show("Специальность не найдена.");
                        return;
                    }

                    bool usedByDoctors = db.Врачи.Any(d =>
                        d.ID_специальности == speciality.ID_специальности);

                    if (usedByDoctors)
                    {
                        MessageBox.Show("Нельзя удалить специальность, так как она используется у врачей.");
                        return;
                    }

                    db.Специальности.Remove(speciality);
                    db.SaveChanges();
                }

                LoadSpecialities();
                ClearFields();

                MessageBox.Show("Специальность удалена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении специальности: " + ex.Message);
            }
        }

        private void SpecialitiesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SpecialityListItem selected = SpecialitiesListView.SelectedItem as SpecialityListItem;

            if (selected == null)
                return;

            _selectedSpecialityId = selected.SpecialityId;

            NameTextBox.Text = selected.Name;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadSpecialities();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSpecialities();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void ClearFields()
        {
            _selectedSpecialityId = null;

            NameTextBox.Clear();

            SpecialitiesListView.SelectedItem = null;
        }

        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите название специальности.");
                return false;
            }

            return true;
        }
    }

    public class SpecialityListItem
    {
        public int SpecialityId { get; set; }

        public string Name { get; set; }

        public int DoctorsCount { get; set; }
    }
}