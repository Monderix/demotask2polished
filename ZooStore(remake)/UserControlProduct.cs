using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ZooStore
{
    public partial class UserControlProduct : UserControl
    {
        private static readonly Color DiscountColor =
            ColorTranslator.FromHtml("#67D31D");

        private static readonly Color EmptyColor =
            ColorTranslator.FromHtml("#ADD8E6");

        private const string DefaultText = "Не указано";

        public event Action<string> EditRequested;
        public event Action<string> DeleteRequested;

        public UserControlProduct()
        {
            InitializeComponent();
            Margin = new Padding(8);
            pictureBoxProduct.SizeMode = PictureBoxSizeMode.Zoom;
            panelDiscount.BackColor = Color.WhiteSmoke;
            labelDiscount.TextAlign = ContentAlignment.MiddleCenter;
            Hook(this);
            Disposed += delegate
            {
                if (pictureBoxProduct.Image != null)
                {
                    pictureBoxProduct.Image.Dispose();
                    pictureBoxProduct.Image = null;
                }
            };
        }

        internal string ProductArticle { get; private set; }

        internal void Bind(
            string article,
            string name,
            string category,
            string description,
            string manufacturer,
            string provider,
            string measurement,
            int cost,
            int discount,
            int quantity,
            string picturePath)
        {
            var hasDiscount = discount > 0;
            var finalPrice = hasDiscount
                ? Math.Round(cost - cost * discount / 100.0, 2)
                : cost;

            ProductArticle = article;
            labelCategoryNameItem.Text =
                TextOrDefault(category) + " | " + TextOrDefault(name);
            labelDescriptVal.Text = TextOrDefault(description);
            labelManufacturerVal.Text = TextOrDefault(manufacturer);
            labelProviderVal.Text = TextOrDefault(provider);
            labelMeasVal.Text = TextOrDefault(measurement);
            labelQuanVal.Text = quantity.ToString();
            labelDiscount.Text = discount + "%";

            label3.Visible = false;
            labelDescriptonValue.Visible = false;
            labelManufacturerValue.Visible = false;
            labelProviderValue.Visible = false;
            labelMeasurementValue.Visible = false;
            labelQuantityValue.Visible = false;
            labelCostValue.Visible = false;

            labelCost.Text = hasDiscount ? "Старая цена:" : "Цена:";
            labelPriceVal.ForeColor = hasDiscount ? Color.Firebrick : Color.Black;
            labelPriceVal.Font = new Font(
                labelPriceVal.Font,
                hasDiscount ? FontStyle.Strikeout : FontStyle.Regular);
            labelPriceVal.Text = cost + " руб.";

            labelFinalPrice.Visible = hasDiscount;
            if (hasDiscount)
            {
                labelFinalPrice.Text =
                    "Новая цена: " + finalPrice.ToString("0.##") + " руб.";
            }

            panelInformationProduct.BackColor = Color.White;
            panelDiscount.BackColor = discount > 15
                ? DiscountColor
                : Color.WhiteSmoke;
            labelQuanVal.BackColor = quantity == 0
                ? EmptyColor
                : Color.Transparent;

            if (pictureBoxProduct.Image != null)
            {
                pictureBoxProduct.Image.Dispose();
                pictureBoxProduct.Image = null;
            }

            if (!string.IsNullOrWhiteSpace(picturePath))
            {
                var resourceName = Path.GetFileNameWithoutExtension(picturePath.Trim());
                var resource =
                    global::ZooStore.Properties.Resources.ResourceManager.GetObject(resourceName)
                    as Bitmap;

                if (resource != null)
                {
                    pictureBoxProduct.Image = new Bitmap(resource);
                }
                else if (File.Exists(picturePath))
                {
                    pictureBoxProduct.Image = Image.FromFile(picturePath);
                }
                else if (File.Exists(Path.Combine(Application.StartupPath, picturePath)))
                {
                    pictureBoxProduct.Image =
                        Image.FromFile(Path.Combine(Application.StartupPath, picturePath));
                }
            }
        }

        private void Hook(Control control)
        {
            control.DoubleClick += delegate
            {
                if (!string.IsNullOrWhiteSpace(ProductArticle) && EditRequested != null)
                {
                    EditRequested(ProductArticle);
                }
            };

            control.MouseUp += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Right &&
                    !string.IsNullOrWhiteSpace(ProductArticle) &&
                    DeleteRequested != null)
                {
                    DeleteRequested(ProductArticle);
                }
            };

            foreach (Control child in control.Controls)
            {
                Hook(child);
            }
        }

        private static string TextOrDefault(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? DefaultText : value.Trim();
        }

        private void labelQuanVal_Click(object sender, EventArgs e) { }

        private void labelFinalPrice_Click(object sender, EventArgs e) { }
    }
}
