using PolyclinicApp.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PolyclinicApp.Views
{
    public partial class DoctorsWindow : Window
    {
        private int? _selectedDoctorId;

        public DoctorsWindow()
        {
            InitializeComponent();
            LoadSpecialities();
            LoadDoctors();
        }

        private void LoadSpecialities()
        {
            using (var db = new ПоликлиникаEntities1())
            {
                SpecialityComboBox.ItemsSource = db.Специальности
                    .OrderBy(s => s.Название)
                    .ToList();
            }
        }

        private void LoadDoctors()
        {
            using (var db = new ПоликлиникаEntities1())
            {
                string search = SearchTextBox.Text.Trim();

                var query =
                    from d in db.Врачи
                    join s in db.Специальности
                        on d.ID_специальности equals s.ID_специальности
                    select new DoctorListItem
                    {
                        DoctorId = d.ID_врача,
                        LastName = d.Фамилия,
                        FirstName = d.Имя,
                        MiddleName = d.Отчество,
                        SpecialityId = s.ID_специальности,
                        SpecialityName = s.Название
                    };

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(d => d.LastName.Contains(search));
                }

                DoctorsListView.ItemsSource = query
                    .OrderBy(d => d.LastName)
                    .ToList();
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
                return;

            using (var db = new ПоликлиникаEntities1())
            {
                int newId = 1;

                if (db.Врачи.Any())
                    newId = db.Врачи.Max(d => d.ID_врача) + 1;

                Врачи doctor = new Врачи();

                doctor.ID_врача = newId;
                doctor.Фамилия = LastNameTextBox.Text.Trim();
                doctor.Имя = FirstNameTextBox.Text.Trim();
                doctor.Отчество = MiddleNameTextBox.Text.Trim();
                doctor.ID_специальности = Convert.ToInt32(SpecialityComboBox.SelectedValue);

                db.Врачи.Add(doctor);
                db.SaveChanges();
            }

            LoadDoctors();
            ClearFields();

            MessageBox.Show("Врач добавлен.");
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDoctorId == null)
            {
                MessageBox.Show("Выберите врача для изменения.");
                return;
            }

            if (!ValidateFields())
                return;

            using (var db = new ПоликлиникаEntities1())
            {
                Врачи doctor = db.Врачи.FirstOrDefault(d => d.ID_врача == _selectedDoctorId.Value);

                if (doctor == null)
                {
                    MessageBox.Show("Врач не найден.");
                    return;
                }

                doctor.Фамилия = LastNameTextBox.Text.Trim();
                doctor.Имя = FirstNameTextBox.Text.Trim();
                doctor.Отчество = MiddleNameTextBox.Text.Trim();
                doctor.ID_специальности = Convert.ToInt32(SpecialityComboBox.SelectedValue);

                db.SaveChanges();
            }

            LoadDoctors();
            ClearFields();

            MessageBox.Show("Данные врача изменены.");
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDoctorId == null)
            {
                MessageBox.Show("Выберите врача для удаления.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Удалить выбранного врача?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Врачи doctor = db.Врачи.FirstOrDefault(d => d.ID_врача == _selectedDoctorId.Value);

                    if (doctor == null)
                    {
                        MessageBox.Show("Врач не найден.");
                        return;
                    }

                    db.Врачи.Remove(doctor);
                    db.SaveChanges();
                }

                LoadDoctors();
                ClearFields();

                MessageBox.Show("Врач удален.");
            }
            catch
            {
                MessageBox.Show("Нельзя удалить врача, так как с ним связаны приемы, расписание или пользователи.");
            }
        }

        private void DoctorsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DoctorListItem selected = DoctorsListView.SelectedItem as DoctorListItem;

            if (selected == null)
                return;

            _selectedDoctorId = selected.DoctorId;

            LastNameTextBox.Text = selected.LastName;
            FirstNameTextBox.Text = selected.FirstName;
            MiddleNameTextBox.Text = selected.MiddleName;
            SpecialityComboBox.SelectedValue = selected.SpecialityId;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadDoctors();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDoctors();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void ClearFields()
        {
            _selectedDoctorId = null;

            LastNameTextBox.Clear();
            FirstNameTextBox.Clear();
            MiddleNameTextBox.Clear();
            SpecialityComboBox.SelectedIndex = -1;
            DoctorsListView.SelectedItem = null;
        }

        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                MessageBox.Show("Введите фамилию врача.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                MessageBox.Show("Введите имя врача.");
                return false;
            }

            if (SpecialityComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите специальность.");
                return false;
            }

            return true;
        }
    }

    public class DoctorListItem
    {
        public int DoctorId { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public int SpecialityId { get; set; }
        public string SpecialityName { get; set; }
    }
}