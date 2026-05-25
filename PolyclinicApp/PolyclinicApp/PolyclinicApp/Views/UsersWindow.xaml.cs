using PolyclinicApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PolyclinicApp.Views
{
    public partial class UsersWindow : Window
    {
        private int? _selectedUserId;

        private List<UserDoctorComboItem> _allDoctors =
            new List<UserDoctorComboItem>();

        private List<UserRoleItem> _roles =
            new List<UserRoleItem>();

        public UsersWindow()
        {
            InitializeComponent();

            LoadComboBoxes();
            LoadUsers();
        }

        private void LoadComboBoxes()
        {
            _roles = new List<UserRoleItem>
            {
                new UserRoleItem { Id = 1, Name = "Администратор" },
                new UserRoleItem { Id = 2, Name = "Регистратор" },
                new UserRoleItem { Id = 3, Name = "Врач" },
                new UserRoleItem { Id = 4, Name = "Лаборант" }
            };

            RoleComboBox.ItemsSource = _roles;

            using (var db = new ПоликлиникаEntities1())
            {
                _allDoctors =
                    (from d in db.Врачи
                     join s in db.Специальности
                        on d.ID_специальности equals s.ID_специальности
                     orderby d.Фамилия
                     select new UserDoctorComboItem
                     {
                         Id = d.ID_врача,
                         Name = d.Фамилия + " " + d.Имя + " " + d.Отчество + " — " + s.Название,
                         ShortName = d.Фамилия + " " + d.Имя + " " + d.Отчество,
                         SpecialityName = s.Название
                     })
                    .ToList();

                DoctorComboBox.ItemsSource = _allDoctors;
            }

            RoleComboBox.SelectedValue = 2;
            DoctorComboBox.IsEnabled = false;
            DoctorSearchTextBox.IsEnabled = false;
        }

        private void LoadUsers()
        {
            using (var db = new ПоликлиникаEntities1())
            {
                string search = SearchTextBox.Text.Trim();

                var users = db.Пользователи
                    .ToList()
                    .Select(u => new
                    {
                        UserId = u.ID_пользователя,
                        Login = u.Логин,
                        RoleId = u.Роль,
                        DoctorId = u.ID_врача
                    })
                    .ToList();

                var doctors =
                    (from d in db.Врачи
                     join s in db.Специальности
                        on d.ID_специальности equals s.ID_специальности
                     select new
                     {
                         DoctorId = d.ID_врача,
                         DoctorName = d.Фамилия + " " + d.Имя + " " + d.Отчество + " — " + s.Название
                     })
                    .ToList();

                var list =
                    (from u in users
                     join d in doctors
                        on u.DoctorId equals d.DoctorId into doctorJoin
                     from doctor in doctorJoin.DefaultIfEmpty()
                     select new UserListItem
                     {
                         UserId = u.UserId,
                         Login = u.Login,
                         RoleId = u.RoleId,
                         RoleName = GetRoleName(u.RoleId),
                         DoctorId = u.DoctorId,
                         DoctorIdText = u.DoctorId == null ? "" : u.DoctorId.Value.ToString(),
                         DoctorName = doctor == null ? "" : doctor.DoctorName
                     })
                    .ToList();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    list = list
                        .Where(x =>
                            x.Login.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            x.RoleName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            x.DoctorName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToList();
                }

                UsersListView.ItemsSource = list
                    .OrderBy(x => x.UserId)
                    .ToList();
            }
        }

        private void RoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoleComboBox.SelectedValue == null)
                return;

            int roleId = Convert.ToInt32(RoleComboBox.SelectedValue);

            bool isDoctorRole = roleId == 3;

            DoctorComboBox.IsEnabled = isDoctorRole;
            DoctorSearchTextBox.IsEnabled = isDoctorRole;

            if (!isDoctorRole)
            {
                DoctorComboBox.SelectedIndex = -1;
                DoctorSearchTextBox.Clear();
            }

            if (isDoctorRole)
            {
                HintTextBlock.Text = "Для роли «Врач» обязательно выберите связанного врача.";
            }
            else
            {
                HintTextBlock.Text = "Для этой роли привязка к врачу не требуется.";
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

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields(isEdit: false))
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    string login = LoginTextBox.Text.Trim();

                    bool loginExists = db.Пользователи.Any(u => u.Логин == login);

                    if (loginExists)
                    {
                        MessageBox.Show("Пользователь с таким логином уже существует.");
                        return;
                    }

                    int newId = 1;

                    if (db.Пользователи.Any())
                    {
                        newId = db.Пользователи.Max(u => u.ID_пользователя) + 1;
                    }

                    int roleId = Convert.ToInt32(RoleComboBox.SelectedValue);

                    Пользователи user = new Пользователи();

                    user.ID_пользователя = newId;
                    user.Логин = login;
                    user.Хеш_пароля = GetMd5Hash(PasswordBox.Password.Trim());
                    user.Роль = roleId;

                    if (roleId == 3)
                    {
                        user.ID_врача = Convert.ToInt32(DoctorComboBox.SelectedValue);
                    }
                    else
                    {
                        user.ID_врача = null;
                    }

                    db.Пользователи.Add(user);
                    db.SaveChanges();
                }

                LoadUsers();
                ClearFields();

                MessageBox.Show("Пользователь добавлен.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении пользователя: " + ex.Message);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUserId == null)
            {
                MessageBox.Show("Выберите пользователя для изменения.");
                return;
            }

            if (!ValidateFields(isEdit: true))
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Пользователи user = db.Пользователи
                        .FirstOrDefault(u => u.ID_пользователя == _selectedUserId.Value);

                    if (user == null)
                    {
                        MessageBox.Show("Пользователь не найден.");
                        return;
                    }

                    string login = LoginTextBox.Text.Trim();

                    bool loginExists = db.Пользователи.Any(u =>
                        u.ID_пользователя != user.ID_пользователя &&
                        u.Логин == login);

                    if (loginExists)
                    {
                        MessageBox.Show("Другой пользователь с таким логином уже существует.");
                        return;
                    }

                    int roleId = Convert.ToInt32(RoleComboBox.SelectedValue);

                    user.Логин = login;
                    user.Роль = roleId;

                    if (!string.IsNullOrWhiteSpace(PasswordBox.Password))
                    {
                        user.Хеш_пароля = GetMd5Hash(PasswordBox.Password.Trim());
                    }

                    if (roleId == 3)
                    {
                        user.ID_врача = Convert.ToInt32(DoctorComboBox.SelectedValue);
                    }
                    else
                    {
                        user.ID_врача = null;
                    }

                    db.SaveChanges();
                }

                LoadUsers();
                ClearFields();

                MessageBox.Show("Пользователь изменён.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при изменении пользователя: " + ex.Message);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUserId == null)
            {
                MessageBox.Show("Выберите пользователя для удаления.");
                return;
            }

            if (_selectedUserId == 1)
            {
                MessageBox.Show("Основного администратора удалять нельзя.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Удалить выбранного пользователя?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Пользователи user = db.Пользователи
                        .FirstOrDefault(u => u.ID_пользователя == _selectedUserId.Value);

                    if (user == null)
                    {
                        MessageBox.Show("Пользователь не найден.");
                        return;
                    }

                    bool usedInAudit = db.Журнал_аудита.Any(a =>
                        a.ID_пользователя == user.ID_пользователя);

                    if (usedInAudit)
                    {
                        MessageBox.Show("Нельзя удалить пользователя, так как он есть в журнале аудита.");
                        return;
                    }

                    db.Пользователи.Remove(user);
                    db.SaveChanges();
                }

                LoadUsers();
                ClearFields();

                MessageBox.Show("Пользователь удалён.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении пользователя: " + ex.Message);
            }
        }

        private void UsersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UserListItem selected = UsersListView.SelectedItem as UserListItem;

            if (selected == null)
                return;

            _selectedUserId = selected.UserId;

            LoginTextBox.Text = selected.Login;
            PasswordBox.Clear();

            RoleComboBox.SelectedValue = selected.RoleId;

            DoctorComboBox.ItemsSource = _allDoctors;

            if (selected.DoctorId != null)
            {
                DoctorComboBox.SelectedValue = selected.DoctorId.Value;
            }
            else
            {
                DoctorComboBox.SelectedIndex = -1;
            }

            HintTextBlock.Text = "Пароль оставьте пустым, если не нужно его менять.";
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadUsers();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadComboBoxes();
            LoadUsers();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void ClearFields()
        {
            _selectedUserId = null;

            LoginTextBox.Clear();
            PasswordBox.Clear();

            RoleComboBox.SelectedValue = 2;

            DoctorSearchTextBox.Clear();
            DoctorComboBox.ItemsSource = _allDoctors;
            DoctorComboBox.SelectedIndex = -1;

            UsersListView.SelectedItem = null;

            HintTextBlock.Text = "Для роли «Врач» выберите связанного врача.";
        }

        private bool ValidateFields(bool isEdit)
        {
            if (string.IsNullOrWhiteSpace(LoginTextBox.Text))
            {
                MessageBox.Show("Введите логин.");
                return false;
            }

            if (!isEdit && string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Введите пароль.");
                return false;
            }

            if (RoleComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите роль.");
                return false;
            }

            int roleId = Convert.ToInt32(RoleComboBox.SelectedValue);

            if (roleId == 3 && DoctorComboBox.SelectedValue == null)
            {
                MessageBox.Show("Для пользователя с ролью «Врач» выберите врача.");
                return false;
            }

            if (LoginTextBox.Text.Trim().Length < 3)
            {
                MessageBox.Show("Логин должен быть не короче 3 символов.");
                return false;
            }

            if (!isEdit && PasswordBox.Password.Trim().Length < 4)
            {
                MessageBox.Show("Пароль должен быть не короче 4 символов.");
                return false;
            }

            return true;
        }

        private static string GetRoleName(int roleId)
        {
            switch (roleId)
            {
                case 1:
                    return "Администратор";
                case 2:
                    return "Регистратор";
                case 3:
                    return "Врач";
                case 4:
                    return "Лаборант";
                default:
                    return "Неизвестно";
            }
        }

        private static string GetMd5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(bytes);

                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < hashBytes.Length; i++)
                {
                    builder.Append(hashBytes[i].ToString("X2"));
                }

                return builder.ToString();
            }
        }
    }

    public class UserListItem
    {
        public int UserId { get; set; }

        public string Login { get; set; }

        public int RoleId { get; set; }

        public string RoleName { get; set; }

        public int? DoctorId { get; set; }

        public string DoctorIdText { get; set; }

        public string DoctorName { get; set; }
    }

    public class UserRoleItem
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class UserDoctorComboItem
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string ShortName { get; set; }

        public string SpecialityName { get; set; }
    }
}