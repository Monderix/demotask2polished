using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Resources;
using System.Windows.Forms;
using Npgsql;

namespace ZooStore
{
    public enum UserRole
    {
        Guest = 0,
        Admin = 1,
        Manager = 2,
        Client = 3
    }

    internal static class AppData
    {
        public const string ConnectionString =
            "Host=localhost;Port=5432;Database=demotask2;Username=postgres;Password=5696";
    }
}

namespace ZooStore.Models
{
    internal sealed class AppUser
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public string FullName { get; set; }
        public string Login { get; set; }
        public UserRole Role { get { return (UserRole)RoleId; } }
    }

    internal sealed class ProductListItem
    {
        public string Article { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }
        public string Provider { get; set; }
        public string Measurement { get; set; }
        public int Cost { get; set; }
        public int Discount { get; set; }
        public int Quantity { get; set; }
        public string PicturePath { get; set; }
        public decimal FinalPrice { get { return Cost * (100 - Discount) / 100m; } }
        public bool HasDiscount { get { return Discount > 0; } }
    }

    internal sealed class OrderListItem
    {
        public int Id { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string PickupPointAddress { get; set; }
        public string ClientFullName { get; set; }
        public int PickupCode { get; set; }
        public string StatusName { get; set; }
        public string ItemsSummary { get; set; }
    }
}

namespace ZooStore.Infrastructure
{
    internal static class AppResources
    {
        static readonly ResourceManager Rm = new ResourceManager(
            "ZooStore.Properties.Resources",
            typeof(AppResources).Assembly);

        public static Bitmap PicturePlaceholder { get { return GetBitmap("picture"); } }

        public static Bitmap GetBitmap(string name)
        {
            return Rm.GetObject(name) as Bitmap;
        }

        public static Bitmap GetBitmapByReference(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference)) return null;
            return GetBitmap(Path.GetFileNameWithoutExtension(reference.Trim()));
        }
    }
}

namespace ZooStore.Services
{
    using ZooStore.Infrastructure;
    using ZooStore.Models;

    internal static class AppSession
    {
        public static AppUser CurrentUser { get; private set; }
        public static void SignIn(AppUser user) { CurrentUser = user; }
        public static void SignOut() { CurrentUser = null; }
        public static AppUser CreateGuest()
        {
            return new AppUser
            {
                Id = 0,
                RoleId = (int)UserRole.Guest,
                FullName = "Гость",
                Login = "guest"
            };
        }
    }

    internal static class DialogService
    {
        public static void ShowInfo(string text, string title = "Информация")
        {
            MessageBox.Show(text, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void ShowWarning(string text, string title = "Предупреждение")
        {
            MessageBox.Show(text, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static bool Confirm(string text, string title = "Подтверждение")
        {
            return MessageBox.Show(
                text,
                title,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes;
        }

        public static void ShowException(Exception ex, string action)
        {
            var text = "Не удалось " + action + ".";
            if (ex is NpgsqlException)
                text += "\nПроверьте PostgreSQL, строку подключения и доступность базы.";
            else if (ex is IOException)
                text += "\nПроверьте доступ к файлам приложения.";
            MessageBox.Show(
                text + "\n\nТехническая причина: " + ex.Message,
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    internal static class AuthService
    {
        public static AppUser Authenticate(string login, string password)
        {
            using (var connection = new NpgsqlConnection(AppData.ConnectionString))
            {
                connection.Open();

                using (var command = new NpgsqlCommand(
                    @"select id, role_fk, full_name, login
                      from public.users
                      where login=@login and password=@password
                      limit 1;",
                    connection))
                {
                    command.Parameters.AddWithValue("@login", login);
                    command.Parameters.AddWithValue("@password", password);
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read()) return null;

                        return new AppUser
                        {
                            Id = reader.GetInt32(0),
                            RoleId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                            FullName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            Login = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
                        };
                    }
                }
            }
        }
    }

}
