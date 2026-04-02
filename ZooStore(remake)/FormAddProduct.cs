using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Npgsql;

namespace ZooStore
{
    public partial class FormAddProduct : Form
    {
        private string connectionString =
            "Host=localhost;Port=5432;Database=demotask2;Username=postgres;Password=5696";

        private readonly string article;
        private string picturePath = "";

        public FormAddProduct() : this(null)
        {
        }

        public FormAddProduct(string article)
        {
            InitializeComponent();
            this.article = article;

            Text = string.IsNullOrWhiteSpace(article)
                ? "Добавление товара"
                : "Редактирование товара";
            StartPosition = FormStartPosition.CenterParent;
            pictureBoxEditProduct.SizeMode = PictureBoxSizeMode.Zoom;

            comboBoxCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxManufacturer.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxProvider.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxMeasurement.DropDownStyle = ComboBoxStyle.DropDownList;

            Load += FormAddProduct_Load;
            buttonAddPicture.Click += buttonAddPicture_Click;
            buttonSave.Click += buttonSave_Click;
            buttonBack.Click += buttonBack_Click;
        }

        private void FormAddProduct_Load(object sender, EventArgs e)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    using (var command = new NpgsqlCommand(
                        "select category_pk from public.categories order by category_pk;",
                        connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read()) comboBoxCategory.Items.Add(reader.GetString(0));
                    }

                    using (var command = new NpgsqlCommand(
                        "select manufacturer_pk from public.manufacturers order by manufacturer_pk;",
                        connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read()) comboBoxManufacturer.Items.Add(reader.GetString(0));
                    }

                    using (var command = new NpgsqlCommand(
                        "select provider_pk from public.providers order by provider_pk;",
                        connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read()) comboBoxProvider.Items.Add(reader.GetString(0));
                    }

                    using (var command = new NpgsqlCommand(
                        "select measurement_pk from public.measurement order by measurement_pk;",
                        connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read()) comboBoxMeasurement.Items.Add(reader.GetString(0));
                    }
                }

                if (comboBoxCategory.Items.Count > 0) comboBoxCategory.SelectedIndex = 0;
                if (comboBoxManufacturer.Items.Count > 0) comboBoxManufacturer.SelectedIndex = 0;
                if (comboBoxProvider.Items.Count > 0) comboBoxProvider.SelectedIndex = 0;
                if (comboBoxMeasurement.Items.Count > 0) comboBoxMeasurement.SelectedIndex = 0;

                if (string.IsNullOrWhiteSpace(article))
                {
                    return;
                }

                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    using (var command = new NpgsqlCommand(
                        @"select
                              coalesce(i.item_name,''),
                              coalesce(c.category_pk,''),
                              coalesce(i.description,''),
                              coalesce(m.manufacturer_pk,''),
                              coalesce(p.provider_pk,''),
                              coalesce(i.cost,0),
                              coalesce(me.measurement_pk,''),
                              coalesce(i.quantity,0),
                              coalesce(i.discount,0),
                              coalesce(i.picture,'')
                          from public.items i
                          left join public.categories c on c.id=i.category_fk
                          left join public.manufacturers m on m.id=i.manufacturer_fk
                          left join public.providers p on p.id=i.provider_fk
                          left join public.measurement me on me.id=i.measurement_fk
                          where i.article=@article;",
                        connection))
                    {
                        command.Parameters.AddWithValue("@article", article);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                textBoxName.Text = reader.GetString(0);
                                comboBoxCategory.Text = reader.GetString(1);
                                textBoxDescription.Text = reader.GetString(2);
                                comboBoxManufacturer.Text = reader.GetString(3);
                                comboBoxProvider.Text = reader.GetString(4);
                                textBoxCost.Text = reader.GetInt32(5).ToString();
                                comboBoxMeasurement.Text = reader.GetString(6);
                                textBoxQuantity.Text = reader.GetInt32(7).ToString();
                                textBoxDiscount.Text = reader.GetInt32(8).ToString();
                                picturePath = reader.GetString(9);

                                if (picturePath != "")
                                {
                                    var resourceName =
                                        Path.GetFileNameWithoutExtension(picturePath.Trim());
                                    var resource =
                                        global::ZooStore.Properties.Resources.ResourceManager
                                            .GetObject(resourceName) as Bitmap;

                                    if (resource != null)
                                    {
                                        pictureBoxEditProduct.Image = new Bitmap(resource);
                                    }
                                    else if (File.Exists(picturePath))
                                    {
                                        pictureBoxEditProduct.Image = Image.FromFile(picturePath);
                                    }
                                    else if (File.Exists(Path.Combine(Application.StartupPath, picturePath)))
                                    {
                                        pictureBoxEditProduct.Image =
                                            Image.FromFile(Path.Combine(Application.StartupPath, picturePath));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить форму товара.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Close();
            }
        }

        private void buttonAddPicture_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp";
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                picturePath = dialog.FileName;
                pictureBoxEditProduct.Image = Image.FromFile(picturePath);
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            int cost;
            int quantity;
            int discount;

            if (textBoxName.Text.Trim() == "" ||
                comboBoxCategory.Text == "" ||
                comboBoxManufacturer.Text == "" ||
                comboBoxProvider.Text == "" ||
                comboBoxMeasurement.Text == "" ||
                !int.TryParse(textBoxCost.Text, out cost) ||
                !int.TryParse(textBoxQuantity.Text, out quantity) ||
                !int.TryParse(textBoxDiscount.Text, out discount))
            {
                MessageBox.Show(
                    "Заполните поля корректно.",
                    "Проверка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                if (string.IsNullOrWhiteSpace(article))
                {
                    using (var command = new NpgsqlCommand(
                        @"insert into public.items
                          (
                              article, item_name, measurement_fk, cost, provider_fk,
                              manufacturer_fk, category_fk, discount, quantity, description, picture
                          )
                          values
                          (
                              @article,
                              @name,
                              (select id from public.measurement where measurement_pk=@measurement limit 1),
                              @cost,
                              (select id from public.providers where provider_pk=@provider limit 1),
                              (select id from public.manufacturers where manufacturer_pk=@manufacturer limit 1),
                              (select id from public.categories where category_pk=@category limit 1),
                              @discount,
                              @quantity,
                              @description,
                              @picture
                          );",
                        connection))
                    {
                        command.Parameters.AddWithValue(
                            "@article",
                            "ART-" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper());
                        command.Parameters.AddWithValue("@name", textBoxName.Text.Trim());
                        command.Parameters.AddWithValue("@measurement", comboBoxMeasurement.Text);
                        command.Parameters.AddWithValue("@cost", cost);
                        command.Parameters.AddWithValue("@provider", comboBoxProvider.Text);
                        command.Parameters.AddWithValue("@manufacturer", comboBoxManufacturer.Text);
                        command.Parameters.AddWithValue("@category", comboBoxCategory.Text);
                        command.Parameters.AddWithValue("@discount", discount);
                        command.Parameters.AddWithValue("@quantity", quantity);
                        command.Parameters.AddWithValue("@description", textBoxDescription.Text.Trim());
                        command.Parameters.AddWithValue(
                            "@picture",
                            picturePath == "" ? (object)DBNull.Value : picturePath);
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var command = new NpgsqlCommand(
                        @"update public.items
                          set item_name=@name,
                              measurement_fk=(select id from public.measurement where measurement_pk=@measurement limit 1),
                              cost=@cost,
                              provider_fk=(select id from public.providers where provider_pk=@provider limit 1),
                              manufacturer_fk=(select id from public.manufacturers where manufacturer_pk=@manufacturer limit 1),
                              category_fk=(select id from public.categories where category_pk=@category limit 1),
                              discount=@discount,
                              quantity=@quantity,
                              description=@description,
                              picture=@picture
                          where article=@article;",
                        connection))
                    {
                        command.Parameters.AddWithValue("@article", article);
                        command.Parameters.AddWithValue("@name", textBoxName.Text.Trim());
                        command.Parameters.AddWithValue("@measurement", comboBoxMeasurement.Text);
                        command.Parameters.AddWithValue("@cost", cost);
                        command.Parameters.AddWithValue("@provider", comboBoxProvider.Text);
                        command.Parameters.AddWithValue("@manufacturer", comboBoxManufacturer.Text);
                        command.Parameters.AddWithValue("@category", comboBoxCategory.Text);
                        command.Parameters.AddWithValue("@discount", discount);
                        command.Parameters.AddWithValue("@quantity", quantity);
                        command.Parameters.AddWithValue("@description", textBoxDescription.Text.Trim());
                        command.Parameters.AddWithValue(
                            "@picture",
                            picturePath == "" ? (object)DBNull.Value : picturePath);
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
    }
}
