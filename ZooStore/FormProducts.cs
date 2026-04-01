using System;
using System.Windows.Forms;
using Npgsql;
using ZooStore.Models;
using ZooStore.Services;

namespace ZooStore
{
    public partial class FormProducts : Form
    {
        private const string AllProviders = "Все поставщики";

        private string selectedArticle = "";

        public FormProducts()
        {
            InitializeComponent();

            Text = "Товары";
            StartPosition = FormStartPosition.CenterScreen;
            buttonBack.Text = "Выход";

            comboBoxProvider.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxSort.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxSort.Items.Add("Без сортировки");
            comboBoxSort.Items.Add("Количество по возрастанию");
            comboBoxSort.Items.Add("Количество по убыванию");
            comboBoxSort.SelectedIndex = 0;

            Load += FormProducts_Load;
            buttonBack.Click += buttonBack_Click;
            buttonDeleteProduct.Click += buttonDeleteProduct_Click;
            buttonOrders.Click += buttonOrders_Click;
            buttonAddProduct.Click += buttonAddProduct_Click_1;
            textBoxSearch.TextChanged += filters_Changed;
            comboBoxProvider.SelectedIndexChanged += filters_Changed;
            comboBoxSort.SelectedIndexChanged += filters_Changed;
        }

        private void FormProducts_Load(object sender, EventArgs e)
        {
            var user = AppSession.CurrentUser;
            if (user == null)
            {
                Close();
                return;
            }

            labelFullName.Text = string.IsNullOrWhiteSpace(user.FullName)
                ? user.Login
                : user.FullName;

            var canFilter = user.Role == UserRole.Admin || user.Role == UserRole.Manager;
            var canEdit = user.Role == UserRole.Admin;
            var canOpenOrders = user.Role == UserRole.Admin || user.Role == UserRole.Manager;

            labelSearch.Visible = canFilter;
            textBoxSearch.Visible = canFilter;
            labelProvider.Visible = canFilter;
            comboBoxProvider.Visible = canFilter;
            labelSort.Visible = canFilter;
            comboBoxSort.Visible = canFilter;
            buttonOrders.Visible = canOpenOrders;
            buttonAddProduct.Visible = canEdit;
            buttonDeleteProduct.Visible = canEdit;

            try
            {
                comboBoxProvider.Items.Clear();
                comboBoxProvider.Items.Add(AllProviders);

                using (var connection = new NpgsqlConnection(AppData.ConnectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(
                        @"select distinct coalesce(provider_pk,'')
                          from public.providers
                          where coalesce(provider_pk,'') <> ''
                          order by coalesce(provider_pk,'');",
                        connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            comboBoxProvider.Items.Add(reader.GetString(0));
                        }
                    }
                }

                comboBoxProvider.SelectedIndex = 0;
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить список товаров.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void filters_Changed(object sender, EventArgs e)
        {
            LoadProducts();
        }

        private void LoadProducts()
        {
            flowLayoutPanelProducts.Controls.Clear();
            selectedArticle = "";

            try
            {
                var sql =
                    @"select
                          i.article,
                          coalesce(i.item_name, ''),
                          coalesce(c.category_pk, ''),
                          coalesce(i.description, ''),
                          coalesce(m.manufacturer_pk, ''),
                          coalesce(p.provider_pk, ''),
                          coalesce(me.measurement_pk, ''),
                          coalesce(i.cost, 0),
                          coalesce(i.discount, 0),
                          coalesce(i.quantity, 0),
                          coalesce(i.picture, '')
                      from public.items i
                      left join public.categories c on c.id = i.category_fk
                      left join public.manufacturers m on m.id = i.manufacturer_fk
                      left join public.providers p on p.id = i.provider_fk
                      left join public.measurement me on me.id = i.measurement_fk
                      where 1=1";

                var text = textBoxSearch.Text.Trim();
                var provider = comboBoxProvider.Text;

                if (textBoxSearch.Visible && text != "")
                {
                    sql +=
                        @" and
                          (
                              i.item_name ilike @search or
                              i.description ilike @search or
                              m.manufacturer_pk ilike @search or
                              p.provider_pk ilike @search
                          )";
                }

                if (comboBoxProvider.Visible && provider != "" && provider != AllProviders)
                {
                    sql += " and p.provider_pk = @provider";
                }

                if (comboBoxSort.Visible && comboBoxSort.SelectedIndex == 1)
                {
                    sql += " order by coalesce(i.quantity, 0), i.item_name";
                }
                else if (comboBoxSort.Visible && comboBoxSort.SelectedIndex == 2)
                {
                    sql += " order by coalesce(i.quantity, 0) desc, i.item_name";
                }
                else
                {
                    sql += " order by i.item_name nulls last, i.article";
                }

                using (var connection = new NpgsqlConnection(AppData.ConnectionString))
                {
                    connection.Open();

                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        if (textBoxSearch.Visible && text != "")
                        {
                            command.Parameters.AddWithValue("@search", "%" + text + "%");
                        }

                        if (comboBoxProvider.Visible && provider != "" && provider != AllProviders)
                        {
                            command.Parameters.AddWithValue("@provider", provider);
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var card = new UserControlProduct();
                                card.Bind(new ProductListItem
                                {
                                    Article = reader.GetString(0),
                                    Name = reader.GetString(1),
                                    Category = reader.GetString(2),
                                    Description = reader.GetString(3),
                                    Manufacturer = reader.GetString(4),
                                    Provider = reader.GetString(5),
                                    Measurement = reader.GetString(6),
                                    Cost = reader.GetInt32(7),
                                    Discount = reader.GetInt32(8),
                                    Quantity = reader.GetInt32(9),
                                    PicturePath = reader.GetString(10)
                                });

                                card.Selected += card_Selected;
                                if (AppSession.CurrentUser != null && AppSession.CurrentUser.Role == UserRole.Admin)
                                {
                                    card.EditRequested += card_EditRequested;
                                }

                                flowLayoutPanelProducts.Controls.Add(card);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить товары.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void card_Selected(object sender, EventArgs e)
        {
            var card = sender as UserControlProduct;
            if (card == null) return;

            foreach (Control control in flowLayoutPanelProducts.Controls)
            {
                var productCard = control as UserControlProduct;
                if (productCard != null)
                {
                    productCard.IsSelected = false;
                }
            }

            selectedArticle = card.ProductArticle;
            card.IsSelected = true;
        }

        private void card_EditRequested(object sender, EventArgs e)
        {
            var card = sender as UserControlProduct;
            if (card == null) return;

            using (var form = new FormAddProduct(card.ProductArticle))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    LoadProducts();
                }
            }
        }

        private void buttonAddProduct_Click_1(object sender, EventArgs e)
        {
            if (AppSession.CurrentUser == null || AppSession.CurrentUser.Role != UserRole.Admin) return;

            using (var form = new FormAddProduct())
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    LoadProducts();
                }
            }
        }

        private void buttonDeleteProduct_Click(object sender, EventArgs e)
        {
            if (AppSession.CurrentUser == null || AppSession.CurrentUser.Role != UserRole.Admin) return;

            if (selectedArticle == "")
            {
                MessageBox.Show("Сначала выберите товар.", "Удаление", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var connection = new NpgsqlConnection(AppData.ConnectionString))
                {
                    connection.Open();

                    using (var command = new NpgsqlCommand(
                        "select count(*) from public.orders_items where article_fk=@article;",
                        connection))
                    {
                        command.Parameters.AddWithValue("@article", selectedArticle);
                        if (Convert.ToInt32(command.ExecuteScalar()) > 0)
                        {
                            MessageBox.Show(
                                "Товар нельзя удалить, потому что он есть в заказах.",
                                "Удаление запрещено",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            return;
                        }
                    }

                    if (MessageBox.Show(
                        "Удалить выбранный товар?",
                        "Подтверждение",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) != DialogResult.Yes)
                    {
                        return;
                    }

                    using (var command = new NpgsqlCommand(
                        "delete from public.items where article=@article;",
                        connection))
                    {
                        command.Parameters.AddWithValue("@article", selectedArticle);
                        command.ExecuteNonQuery();
                    }
                }

                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось удалить товар.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void buttonOrders_Click(object sender, EventArgs e)
        {
            if (AppSession.CurrentUser == null ||
                (AppSession.CurrentUser.Role != UserRole.Admin &&
                 AppSession.CurrentUser.Role != UserRole.Manager)) return;

            using (var form = new FormOrders())
            {
                form.ShowDialog(this);
            }
        }

        private void buttonBack_Click(object sender, EventArgs e)
        {
            AppSession.SignOut();
            Close();
        }

        private void labelFullName_Click(object sender, EventArgs e)
        {
        }
    }
}
