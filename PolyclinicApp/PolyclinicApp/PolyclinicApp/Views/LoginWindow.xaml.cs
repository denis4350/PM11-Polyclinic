using PolyclinicApp.Models;
using PolyclinicApp.Services;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace PolyclinicApp.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                ErrorTextBlock.Text = "Введите логин и пароль.";
                return;
            }

            string hash = GetMd5Hash(password);

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    var user = db.Пользователи
                        .FirstOrDefault(u => u.Логин == login && u.Хеш_пароля == hash);

                    if (user == null)
                    {
                        ErrorTextBlock.Text = "Неверный логин или пароль.";
                        return;
                    }

                    CurrentUser.UserId = user.ID_пользователя;
                    CurrentUser.Login = user.Логин;

                    // Если таблица старая:
                    CurrentUser.RoleId = user.Роль;

                    // Если ты переименовал поле в ID_роли, используй это:
                    // CurrentUser.RoleId = user.ID_роли;

                    CurrentUser.DoctorId = user.ID_врача;

                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();

                    Close();
                }
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = "Ошибка подключения к базе данных: " + ex.Message;
            }
        }

        private string GetMd5Hash(string input)
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
}