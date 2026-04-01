using System;
using System.Windows.Forms;
using ZooStore.Services;

namespace ZooStore
{
    public partial class FormAuth : Form
    {
        public FormAuth()
        {
            InitializeComponent();
            ConfigureForm();
        }

        private void ConfigureForm()
        {
            Text = "Вход";
            StartPosition = FormStartPosition.CenterScreen;
            textBoxPassword.UseSystemPasswordChar = true;

            buttonEnter.Click += ButtonEnter_Click;
            buttonEnterAsGuest.Click += ButtonEnterAsGuest_Click;
            AcceptButton = buttonEnter;
        }

        private void ButtonEnter_Click(object sender, EventArgs e)
        {
            try
            {
                var login = textBoxLogin.Text.Trim();
                var password = textBoxPassword.Text;

                if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
                {
                    DialogService.ShowWarning(
                        "Введите логин и пароль, затем повторите попытку.",
                        "Не заполнены данные");
                    return;
                }

                var user = AuthService.Authenticate(login, password);
                if (user == null)
                {
                    textBoxPassword.Clear();
                    textBoxPassword.Focus();
                    DialogService.ShowWarning(
                        "Пользователь не найден или пароль введен неверно.\nПроверьте данные и попробуйте снова.",
                        "Вход не выполнен");
                    return;
                }

                AppSession.SignIn(user);
                OpenProductsForm();
            }
            catch (Exception exception)
            {
                DialogService.ShowException(exception, "выполнить вход");
            }
        }

        private void ButtonEnterAsGuest_Click(object sender, EventArgs e)
        {
            AppSession.SignIn(AppSession.CreateGuest());
            OpenProductsForm();
        }

        private void OpenProductsForm()
        {
            Hide();

            using (var productsForm = new FormProducts())
            {
                productsForm.ShowDialog(this);
            }

            AppSession.SignOut();
            textBoxPassword.Clear();
            Show();
            Activate();
            textBoxLogin.Focus();
        }
    }
}
