using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ZooStore.Infrastructure;
using ZooStore.Models;

namespace ZooStore
{
    public partial class UserControlProduct : UserControl
    {
        private static readonly Color DiscountColor =
            ColorTranslator.FromHtml("#67D31D");

        private static readonly Color EmptyColor =
            ColorTranslator.FromHtml("#ADD8E6");

        private const string DefaultText = "Не указано";

        public event EventHandler EditRequested;
        public event EventHandler Selected;

        public UserControlProduct()
        {
            InitializeComponent();
            Margin = new Padding(8);
            pictureBoxProduct.SizeMode = PictureBoxSizeMode.Zoom;
            panelDiscount.BackColor = Color.WhiteSmoke;
            labelDiscount.TextAlign = ContentAlignment.MiddleCenter;
            BorderStyle = BorderStyle.None;
            Hook(this);
            Disposed += delegate { ClearImage(); };
        }

        internal string ProductArticle { get; private set; }

        internal bool IsSelected
        {
            get { return BorderStyle == BorderStyle.FixedSingle; }
            set { BorderStyle = value ? BorderStyle.FixedSingle : BorderStyle.None; }
        }

        internal void Bind(ProductListItem p)
        {
            if (p == null) throw new ArgumentNullException("product");

            ProductArticle = p.Article;

            labelCategoryNameItem.Text =
                TextOrDefault(p.Category) + " | " + TextOrDefault(p.Name);

            labelDescriptVal.Text = TextOrDefault(p.Description);
            labelManufacturerVal.Text = TextOrDefault(p.Manufacturer);
            labelProviderVal.Text = TextOrDefault(p.Provider);
            labelMeasVal.Text = TextOrDefault(p.Measurement);
            labelQuanVal.Text = p.Quantity.ToString();
            labelDiscount.Text = p.Discount + "%";

            label3.Visible = false;
            labelDescriptonValue.Visible = false;
            labelManufacturerValue.Visible = false;
            labelProviderValue.Visible = false;
            labelMeasurementValue.Visible = false;
            labelQuantityValue.Visible = false;
            labelCostValue.Visible = false;

            labelCost.Text = p.HasDiscount ? "Старая цена:" : "Цена:";
            labelPriceVal.ForeColor = p.HasDiscount ? Color.Firebrick : Color.Black;
            labelPriceVal.Font = new Font(
                labelPriceVal.Font,
                p.HasDiscount ? FontStyle.Strikeout : FontStyle.Regular);
            labelPriceVal.Text = p.Cost + " руб.";

            labelFinalPrice.Visible = p.HasDiscount;
            if (p.HasDiscount)
            {
                labelFinalPrice.Text =
                    "Новая цена: " + p.FinalPrice.ToString("0.##") + " руб.";
            }

            panelInformationProduct.BackColor = Color.White;
            panelDiscount.BackColor = p.Discount > 15 ? DiscountColor : Color.WhiteSmoke;
            labelQuanVal.BackColor = p.Quantity == 0 ? EmptyColor : Color.Transparent;

            ApplyImage(p.PicturePath);
        }

        private void Hook(Control control)
        {
            control.Click += delegate
            {
                if (Selected != null) Selected(this, EventArgs.Empty);
            };

            control.DoubleClick += delegate
            {
                if (EditRequested != null) EditRequested(this, EventArgs.Empty);
            };

            foreach (Control child in control.Controls)
            {
                Hook(child);
            }
        }

        private void ApplyImage(string path)
        {
            ClearImage();
            pictureBoxProduct.Image = LoadImage(path);
        }

        private void ClearImage()
        {
            if (pictureBoxProduct.Image != null)
            {
                pictureBoxProduct.Image.Dispose();
                pictureBoxProduct.Image = null;
            }
        }

        private static Image LoadImage(string path)
        {
            var img = AppResources.GetBitmapByReference(path);
            if (img != null)
            {
                return new Bitmap(img);
            }

            var full = string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : Path.Combine(Application.StartupPath, path);

            if (!string.IsNullOrWhiteSpace(full) && File.Exists(full))
            {
                using (var source = Image.FromFile(full))
                {
                    return new Bitmap(source);
                }
            }

            return new Bitmap(AppResources.PicturePlaceholder);
        }

        private static string TextOrDefault(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? DefaultText : value.Trim();
        }

        private void labelQuanVal_Click(object sender, EventArgs e) { }

        private void labelFinalPrice_Click(object sender, EventArgs e) { }
    }
}
