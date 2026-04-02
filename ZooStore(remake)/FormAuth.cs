using System;
using System.Windows.Forms;
using Npgsql;

namespace ZooStore
{
    public partial class FormAuth : Form
    {
        private string connectionString =
            "Host=localhost;Port=5432;Database=demotask2;Username=postgres;Password=5696";

        public FormAuth()
        {
            InitializeComponent();
            Text = "Вход";
            StartPosition = FormStartPosition.CenterScreen;
            textBoxPassword.UseSystemPasswordChar = true;

            buttonEnter.Click += ButtonEnter_Click;
            buttonEnterAsGuest.Click += ButtonEnterAsGuest_Click;
            AcceptButton = buttonEnter;
        }

        private void ButtonEnter_Click(object sender, EventArgs e)
        {
            var login = textBoxLogin.Text.Trim();
            var password = textBoxPassword.Text;

            if (login == "" || password == "")
            {
                MessageBox.Show(
                    "Введите логин и пароль.",
                    "Проверка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var role = UserRole.Guest;

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new NpgsqlCommand(
                    @"select coalesce(role_fk,0)
                      from public.users
                      where login=@login and password=@password
                      limit 1;",
                    connection))
                {
                    command.Parameters.AddWithValue("@login", login);
                    command.Parameters.AddWithValue("@password", password);

                    var result = command.ExecuteScalar();
                    if (result == null)
                    {
                        textBoxPassword.Clear();
                        textBoxPassword.Focus();
                        MessageBox.Show(
                            "Неверный логин или пароль.",
                            "Вход",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    role = (UserRole)Convert.ToInt32(result);
                }
            }

            OpenProductsForm(role);
        }

        private void ButtonEnterAsGuest_Click(object sender, EventArgs e)
        {
            OpenProductsForm(UserRole.Guest);
        }

        private void OpenProductsForm(UserRole role)
        {
            Hide();

            using (var productsForm = new FormProducts(role))
            {
                productsForm.ShowDialog(this);
            }

            textBoxPassword.Clear();
            Show();
            Activate();
            textBoxLogin.Focus();
        }
    }
}
