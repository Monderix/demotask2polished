using System;
using Npgsql;
using System.Windows.Forms;

namespace ZooStore
{
    public partial class FormEditOrder : Form
    {
        private string connectionString = "Host=localhost;Port=5432;Database=demotask2;Username=postgres;Password=5696";
        private readonly int? orderId;

        public FormEditOrder()
            : this(null)
        {
        }

        public FormEditOrder(int? orderId)
        {
            InitializeComponent();
            this.orderId = orderId;

            Text = orderId.HasValue ? "Редактирование заказа" : "Добавление заказа";
            StartPosition = FormStartPosition.CenterParent;
            textBoxArticle.Multiline = true;
            textBoxArticle.ScrollBars = ScrollBars.Vertical;
            labelArticleOrder.Text = "Позиции заказа (артикул:количество):";

            comboBoxStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxPickupPoint.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxClient.DropDownStyle = ComboBoxStyle.DropDownList;

            buttonSave.Click += buttonSave_Click;
            buttonBack.Click += buttonBack_Click;
            Load += FormEditOrder_Load;
        }

        private void FormEditOrder_Load(object sender, EventArgs e)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    LoadCombo(connection, comboBoxStatus, "select id, coalesce(status_name,'') from public.status order by id;");
                    LoadCombo(connection, comboBoxPickupPoint, "select id, coalesce(pickup_point_address,'') from public.pickup_points order by pickup_point_address;");
                    LoadCombo(connection, comboBoxClient, "select id, coalesce(full_name,'') from public.users where role_fk=3 order by full_name;");
                }

                dateTimePickerOrder.Value = DateTime.Now;
                dateTimePickerDelivery.Value = DateTime.Now.AddDays(1);
                textBoxPickupCode.Text = new Random().Next(100000, 999999).ToString();

                if (comboBoxStatus.Items.Count > 0)
                {
                    comboBoxStatus.SelectedIndex = 0;
                }

                if (comboBoxPickupPoint.Items.Count > 0)
                {
                    comboBoxPickupPoint.SelectedIndex = 0;
                }

                if (comboBoxClient.Items.Count > 0)
                {
                    comboBoxClient.SelectedIndex = 0;
                }

                if (orderId.HasValue)
                {
                    LoadOrder();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить форму заказа.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Close();
            }
        }

        private void LoadOrder()
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new NpgsqlCommand(
                    @"select
                          o.id,
                          coalesce(o.order_date, now()),
                          coalesce(o.delivery_date, now()),
                          coalesce(o.pickup_point, 0),
                          coalesce(o.client_full_name, 0),
                          coalesce(o.pickup_code, 0),
                          coalesce(o.status_fk, 0)
                      from public.orders o
                      where o.id=@id
                      limit 1;",
                    connection))
                {
                    command.Parameters.AddWithValue("@id", orderId.Value);

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            throw new Exception("Заказ не найден.");
                        }

                        Text = "Редактирование заказа №" + reader.GetInt32(0);
                        dateTimePickerOrder.Value = reader.GetDateTime(1);
                        dateTimePickerDelivery.Value = reader.GetDateTime(2);
                        SelectComboValue(comboBoxPickupPoint, reader.GetInt32(3));
                        SelectComboValue(comboBoxClient, reader.GetInt32(4));
                        textBoxPickupCode.Text = reader.GetInt32(5).ToString();
                        SelectComboValue(comboBoxStatus, reader.GetInt32(6));
                    }
                }

                using (var command = new NpgsqlCommand(
                    @"select coalesce(article_fk, ''), coalesce(quantity, 1)
                      from public.orders_items
                      where order_id=@id
                      order by id;",
                    connection))
                {
                    command.Parameters.AddWithValue("@id", orderId.Value);

                    using (var reader = command.ExecuteReader())
                    {
                        var lines = "";

                        while (reader.Read())
                        {
                            var line = reader.GetString(0) + ":" + reader.GetInt32(1);
                            lines = string.IsNullOrWhiteSpace(lines)
                                ? line
                                : lines + Environment.NewLine + line;
                        }

                        textBoxArticle.Text = lines;
                    }
                }
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            int pickupCode;

            if (comboBoxStatus.SelectedItem == null ||
                comboBoxPickupPoint.SelectedItem == null ||
                comboBoxClient.SelectedItem == null ||
                string.IsNullOrWhiteSpace(textBoxArticle.Text) ||
                !int.TryParse(textBoxPickupCode.Text.Trim(), out pickupCode))
            {
                MessageBox.Show(
                    "Заполните все поля и введите корректный код получения.",
                    "Проверьте данные",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var lines = textBoxArticle.Text
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0)
            {
                MessageBox.Show(
                    "Добавьте хотя бы одну позицию заказа в формате артикул:количество.",
                    "Проверьте данные",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                int id = orderId ?? 0;
                var statusId = ((ComboItem)comboBoxStatus.SelectedItem).Id;
                var pickupPointId = ((ComboItem)comboBoxPickupPoint.SelectedItem).Id;
                var clientId = ((ComboItem)comboBoxClient.SelectedItem).Id;

                if (orderId.HasValue)
                {
                    using (var command = new NpgsqlCommand(
                        @"update public.orders
                          set order_date=@orderDate,
                              delivery_date=@deliveryDate,
                              pickup_point=@pickupPoint,
                              client_full_name=@clientId,
                              pickup_code=@pickupCode,
                              status_fk=@statusId
                          where id=@id;",
                        connection))
                    {
                        command.Parameters.AddWithValue("@orderDate", dateTimePickerOrder.Value);
                        command.Parameters.AddWithValue("@deliveryDate", dateTimePickerDelivery.Value);
                        command.Parameters.AddWithValue("@pickupPoint", pickupPointId);
                        command.Parameters.AddWithValue("@clientId", clientId);
                        command.Parameters.AddWithValue("@pickupCode", pickupCode);
                        command.Parameters.AddWithValue("@statusId", statusId);
                        command.Parameters.AddWithValue("@id", id);
                        command.ExecuteNonQuery();
                    }

                    using (var command = new NpgsqlCommand(
                        "delete from public.orders_items where order_id=@id;",
                        connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var command = new NpgsqlCommand(
                        @"insert into public.orders
                          (order_date, delivery_date, pickup_point, client_full_name, pickup_code, status_fk)
                          values
                          (@orderDate, @deliveryDate, @pickupPoint, @clientId, @pickupCode, @statusId)
                          returning id;",
                        connection))
                    {
                        command.Parameters.AddWithValue("@orderDate", dateTimePickerOrder.Value);
                        command.Parameters.AddWithValue("@deliveryDate", dateTimePickerDelivery.Value);
                        command.Parameters.AddWithValue("@pickupPoint", pickupPointId);
                        command.Parameters.AddWithValue("@clientId", clientId);
                        command.Parameters.AddWithValue("@pickupCode", pickupCode);
                        command.Parameters.AddWithValue("@statusId", statusId);
                        id = Convert.ToInt32(command.ExecuteScalar());
                    }
                }

                foreach (var rawLine in lines)
                {
                    var parts = rawLine.Split(':');
                    int quantity;

                    if (parts.Length != 2 ||
                        string.IsNullOrWhiteSpace(parts[0]) ||
                        !int.TryParse(parts[1].Trim(), out quantity) ||
                        quantity <= 0)
                    {
                        MessageBox.Show(
                            "Все позиции должны быть в формате артикул:количество.\nНапример: A123:2",
                            "Проверьте данные",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    using (var command = new NpgsqlCommand(
                        @"insert into public.orders_items(order_id, article_fk, quantity)
                          values(@orderId, @article, @quantity);",
                        connection))
                    {
                        command.Parameters.AddWithValue("@orderId", id);
                        command.Parameters.AddWithValue("@article", parts[0].Trim());
                        command.Parameters.AddWithValue("@quantity", quantity);
                        command.ExecuteNonQuery();
                    }
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonBack_Click(object sender, EventArgs e)
        {
            Close();
        }

        private static void LoadCombo(NpgsqlConnection connection, ComboBox comboBox, string sql)
        {
            comboBox.Items.Clear();

            using (var command = new NpgsqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    comboBox.Items.Add(new ComboItem
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    });
                }
            }
        }

        private static void SelectComboValue(ComboBox comboBox, int id)
        {
            for (var i = 0; i < comboBox.Items.Count; i++)
            {
                var item = comboBox.Items[i] as ComboItem;
                if (item != null && item.Id == id)
                {
                    comboBox.SelectedIndex = i;
                    return;
                }
            }
        }

        private class ComboItem
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}
