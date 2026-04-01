using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Npgsql;
using ZooStore.Infrastructure;

namespace ZooStore
{
    public partial class FormAddProduct : Form
    {
        private readonly string article;
        private string oldPicture = "";
        private string newPicture = "";

        public FormAddProduct()
            : this(null)
        {
        }

        public FormAddProduct(string article)
        {
            InitializeComponent();
            this.article = article;

            Text = string.IsNullOrWhiteSpace(article) ? "Добавление товара" : "Редактирование товара";
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
                using (var connection = new NpgsqlConnection(AppData.ConnectionString))
                {
                    connection.Open();

                    using (var command = new NpgsqlCommand("select category_pk from public.categories order by category_pk;", connection))
                    using (var reader = command.ExecuteReader())
                        while (reader.Read()) comboBoxCategory.Items.Add(reader.GetString(0));

                    using (var command = new NpgsqlCommand("select manufacturer_pk from public.manufacturers order by manufacturer_pk;", connection))
                    using (var reader = command.ExecuteReader())
                        while (reader.Read()) comboBoxManufacturer.Items.Add(reader.GetString(0));

                    using (var command = new NpgsqlCommand("select provider_pk from public.providers order by provider_pk;", connection))
                    using (var reader = command.ExecuteReader())
                        while (reader.Read()) comboBoxProvider.Items.Add(reader.GetString(0));

                    using (var command = new NpgsqlCommand("select measurement_pk from public.measurement order by measurement_pk;", connection))
                    using (var reader = command.ExecuteReader())
                        while (reader.Read()) comboBoxMeasurement.Items.Add(reader.GetString(0));
                }

                if (comboBoxCategory.Items.Count > 0) comboBoxCategory.SelectedIndex = 0;
                if (comboBoxManufacturer.Items.Count > 0) comboBoxManufacturer.SelectedIndex = 0;
                if (comboBoxProvider.Items.Count > 0) comboBoxProvider.SelectedIndex = 0;
                if (comboBoxMeasurement.Items.Count > 0) comboBoxMeasurement.SelectedIndex = 0;

                if (!string.IsNullOrWhiteSpace(article))
                {
                    using (var connection = new NpgsqlConnection(AppData.ConnectionString))
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
                              where i.article=@article
                              limit 1;",
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
                                    oldPicture = reader.GetString(9);

                                    var path = Path.Combine(Application.StartupPath, oldPicture);
                                    var resource = AppResources.GetBitmapByReference(oldPicture);
                                    if (resource != null)
                                    {
                                        pictureBoxEditProduct.Image = new Bitmap(resource);
                                    }
                                    else if (File.Exists(path))
                                    {
                                        using (var image = Image.FromFile(path))
                                            pictureBoxEditProduct.Image = new Bitmap(image);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить форму товара.\n\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private void buttonAddPicture_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp";
                if (dialog.ShowDialog() != DialogResult.OK) return;

                newPicture = dialog.FileName;
                using (var image = Image.FromFile(newPicture))
                    pictureBoxEditProduct.Image = new Bitmap(image);
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            int cost, quantity, discount;

            if (textBoxName.Text.Trim() == "" ||
                comboBoxCategory.Text == "" ||
                comboBoxManufacturer.Text == "" ||
                comboBoxProvider.Text == "" ||
                comboBoxMeasurement.Text == "" ||
                !int.TryParse(textBoxCost.Text, out cost) ||
                !int.TryParse(textBoxQuantity.Text, out quantity) ||
                !int.TryParse(textBoxDiscount.Text, out discount) ||
                cost < 0 || quantity < 0 || discount < 0)
            {
                MessageBox.Show("Заполните поля корректно.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var picture = oldPicture;
            if (newPicture != "")
            {
                var folder = Path.Combine(Application.StartupPath, "Images", "Products");
                Directory.CreateDirectory(folder);
                var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(newPicture);
                var fullPath = Path.Combine(folder, fileName);
                File.Copy(newPicture, fullPath, true);
                picture = Path.Combine("Images", "Products", fileName);
            }

            using (var connection = new NpgsqlConnection(AppData.ConnectionString))
            {
                connection.Open();

                using (var command = new NpgsqlCommand(
                    string.IsNullOrWhiteSpace(article)
                        ? @"insert into public.items
                            (
                                article,item_name,measurement_fk,cost,provider_fk,
                                manufacturer_fk,category_fk,discount,quantity,description,picture
                            )
                            values
                            (
                                @article,@name,
                                (select id from public.measurement where measurement_pk=@measurement limit 1),
                                @cost,
                                (select id from public.providers where provider_pk=@provider limit 1),
                                (select id from public.manufacturers where manufacturer_pk=@manufacturer limit 1),
                                (select id from public.categories where category_pk=@category limit 1),
                                @discount,@quantity,@description,@picture
                            );"
                        : @"update public.items
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
                    command.Parameters.AddWithValue("@article", string.IsNullOrWhiteSpace(article)
                        ? "ART-" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper()
                        : article);
                    command.Parameters.AddWithValue("@name", textBoxName.Text.Trim());
                    command.Parameters.AddWithValue("@measurement", comboBoxMeasurement.Text);
                    command.Parameters.AddWithValue("@cost", cost);
                    command.Parameters.AddWithValue("@provider", comboBoxProvider.Text);
                    command.Parameters.AddWithValue("@manufacturer", comboBoxManufacturer.Text);
                    command.Parameters.AddWithValue("@category", comboBoxCategory.Text);
                    command.Parameters.AddWithValue("@discount", discount);
                    command.Parameters.AddWithValue("@quantity", quantity);
                    command.Parameters.AddWithValue("@description", textBoxDescription.Text.Trim());
                    command.Parameters.AddWithValue("@picture", picture == "" ? (object)DBNull.Value : picture);
                    command.ExecuteNonQuery();
                }
            }

            if (newPicture != "" && oldPicture != "" && File.Exists(Path.Combine(Application.StartupPath, oldPicture)))
            {
                File.Delete(Path.Combine(Application.StartupPath, oldPicture));
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
