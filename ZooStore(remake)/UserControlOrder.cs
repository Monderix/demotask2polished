using System;
using System.Drawing;
using System.Windows.Forms;

namespace ZooStore
{
    public partial class UserControlOrder : UserControl
    {
        private const string DefaultText = "Не указано";

        public event EventHandler Selected;
        public event EventHandler EditRequested;

        public UserControlOrder()
        {
            InitializeComponent();
            Hook(this);
        }

        internal int OrderId { get; private set; }

        internal bool IsSelected
        {
            get { return panelOrderCard.BackColor == Color.Gainsboro; }
            set { panelOrderCard.BackColor = value ? Color.Gainsboro : Color.White; }
        }

        internal void Bind(
            int id,
            DateTime? orderDate,
            DateTime? deliveryDate,
            string pickupPointAddress,
            string clientFullName,
            int pickupCode,
            string statusName,
            string itemsSummary)
        {
            OrderId = id;
            labelOrderTitle.Text = "Заказ №" + id;
            labelOrderDateValue.Text = orderDate.HasValue
                ? orderDate.Value.ToString("dd.MM.yyyy")
                : "Не указана";
            labelDeliveryDateValue.Text = deliveryDate.HasValue
                ? deliveryDate.Value.ToString("dd.MM.yyyy")
                : "Не указана";
            labelPickupPointValue.Text = TextOrDefault(pickupPointAddress);
            labelClientValue.Text = TextOrDefault(clientFullName);
            labelPickupCodeValue.Text = pickupCode.ToString();
            labelStatusValue.Text = TextOrDefault(statusName);
            labelItemsValue.Text = TextOrDefault(itemsSummary);
        }

        private void Hook(Control control)
        {
            control.Click += delegate
            {
                if (Selected != null)
                {
                    Selected(this, EventArgs.Empty);
                }
            };

            control.DoubleClick += delegate
            {
                if (EditRequested != null)
                {
                    EditRequested(this, EventArgs.Empty);
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
    }
}
