using System;
using System.Drawing;
using System.Windows.Forms;
using ZooStore.Models;

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

        internal void Bind(OrderListItem order)
        {
            if (order == null) throw new ArgumentNullException("order");

            OrderId = order.Id;
            labelOrderTitle.Text = "Заказ №" + order.Id;
            labelOrderDateValue.Text = order.OrderDate.HasValue
                ? order.OrderDate.Value.ToString("dd.MM.yyyy")
                : "Не указана";
            labelDeliveryDateValue.Text = order.DeliveryDate.HasValue
                ? order.DeliveryDate.Value.ToString("dd.MM.yyyy")
                : "Не указана";
            labelPickupPointValue.Text = TextOrDefault(order.PickupPointAddress);
            labelClientValue.Text = TextOrDefault(order.ClientFullName);
            labelPickupCodeValue.Text = order.PickupCode.ToString();
            labelStatusValue.Text = TextOrDefault(order.StatusName);
            labelItemsValue.Text = TextOrDefault(order.ItemsSummary);
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

        private static string TextOrDefault(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? DefaultText : value.Trim();
        }
    }
}
