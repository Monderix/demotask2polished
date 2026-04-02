using System;
using System.Windows.Forms;
using Npgsql;

namespace ZooStore
{
    public partial class FormProducts : Form
    {
        private string connectionString =
            "Host=localhost;Port=5432;Database=demotask2;Username=postgres;Password=5696";

        private const string AllProviders = "Все поставщики";

        private readonly UserRole role;

        public FormProducts(UserRole role)
        {
            InitializeComponent();
            this.role = role;

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
            buttonOrders.Click += buttonOrders_Click;
            buttonAddProduct.Click += buttonAddProduct_Click_1;
            textBoxSearch.TextChanged += filters_Changed;
            comboBoxProvider.SelectedIndexChanged += filters_Changed;
            comboBoxSort.SelectedIndexChanged += filters_Changed;
        }

        private void FormProducts_Load(object sender, EventArgs e)
        {
            labelFullName.Text = GetRoleName();

            var canFilter = role == UserRole.Admin || role == UserRole.Manager;
            var canEdit = role == UserRole.Admin;

            labelSearch.Visible = canFilter;
            textBoxSearch.Visible = canFilter;
            labelProvider.Visible = canFilter;
            comboBoxProvider.Visible = canFilter;
            labelSort.Visible = canFilter;
            comboBoxSort.Visible = canFilter;
            buttonOrders.Visible = canFilter;
            buttonAddProduct.Visible = canEdit;
            SetAdminInfoVisible(canEdit);

            try
            {
                comboBoxProvider.Items.Clear();
                comboBoxProvider.Items.Add(AllProviders);

                using (var connection = new NpgsqlConnection(connectionString))
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

            var query =
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
                query +=
                    @" and
                      (
                          lower(coalesce(i.item_name, '')) like @search or
                          lower(coalesce(i.description, '')) like @search or
                          lower(coalesce(m.manufacturer_pk, '')) like @search or
                          lower(coalesce(p.provider_pk, '')) like @search
                      )";
            }

            if (comboBoxProvider.Visible &&
                provider != "" &&
                provider != AllProviders)
            {
                query += " and p.provider_pk = @provider";
            }

            if (comboBoxSort.Visible && comboBoxSort.SelectedIndex == 1)
            {
                query += " order by coalesce(i.quantity, 0) asc, i.item_name";
            }
            else if (comboBoxSort.Visible && comboBoxSort.SelectedIndex == 2)
            {
                query += " order by coalesce(i.quantity, 0) desc, i.item_name";
            }
            else
            {
                query += " order by i.item_name nulls last, i.article";
            }

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new NpgsqlCommand(query, connection))
                {
                    if (textBoxSearch.Visible && text != "")
                    {
                        command.Parameters.AddWithValue(
                            "@search",
                            "%" + text.ToLower() + "%");
                    }

                    if (comboBoxProvider.Visible &&
                        provider != "" &&
                        provider != AllProviders)
                    {
                        command.Parameters.AddWithValue("@provider", provider);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var card = new UserControlProduct();
                            card.Bind(
                                reader.GetString(0),
                                reader.GetString(1),
                                reader.GetString(2),
                                reader.GetString(3),
                                reader.GetString(4),
                                reader.GetString(5),
                                reader.GetString(6),
                                reader.GetInt32(7),
                                reader.GetInt32(8),
                                reader.GetInt32(9),
                                reader.GetString(10));

                            if (role == UserRole.Admin)
                            {
                                card.EditRequested += article =>
                                {
                                    using (var form = new FormAddProduct(article))
                                    {
                                        if (form.ShowDialog(this) == DialogResult.OK)
                                        {
                                            LoadProducts();
                                        }
                                    }
                                };

                                card.DeleteRequested += article =>
                                {
                                    DeleteProduct(article);
                                };
                            }

                            flowLayoutPanelProducts.Controls.Add(card);
                        }
                    }
                }
            }
        }

        private void DeleteProduct(string article)
        {
            if (MessageBox.Show(
                "Удалить выбранный товар?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new NpgsqlCommand(
                    "delete from public.items where article=@article;",
                    connection))
                {
                    command.Parameters.AddWithValue("@article", article);
                    command.ExecuteNonQuery();
                }
            }

            LoadProducts();
        }

        private void buttonAddProduct_Click_1(object sender, EventArgs e)
        {
            if (role != UserRole.Admin)
            {
                return;
            }

            using (var form = new FormAddProduct())
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    LoadProducts();
                }
            }
        }

        private void buttonOrders_Click(object sender, EventArgs e)
        {
            if (role != UserRole.Admin && role != UserRole.Manager)
            {
                return;
            }

            using (var form = new FormOrders(role))
            {
                form.ShowDialog(this);
            }
        }

        private void buttonBack_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void SetAdminInfoVisible(bool visible)
        {
            SetControlVisible("labelinfo1", visible);
            SetControlVisible("labelinfo2", visible);
            SetControlVisible("labelInfo1", visible);
            SetControlVisible("labelInfo2", visible);
        }

        private void SetControlVisible(string name, bool visible)
        {
            var controls = Controls.Find(name, true);
            if (controls.Length > 0)
            {
                controls[0].Visible = visible;
            }
        }

        private string GetRoleName()
        {
            if (role == UserRole.Admin) return "Администратор";
            if (role == UserRole.Manager) return "Менеджер";
            if (role == UserRole.Client) return "Клиент";
            return "Гость";
        }

        private void labelFullName_Click(object sender, EventArgs e)
        {
        }
    }
}
