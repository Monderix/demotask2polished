using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Npgsql;
using ZooStore.Models;
using ZooStore.Services;

namespace ZooStore
{
    public partial class FormOrders : Form
    {
        private UserControlOrder selectedControl;
        private int? selectedOrderId;

        public FormOrders()
        {
            InitializeComponent();

            Text = "Заказы";
            StartPosition = FormStartPosition.CenterParent;

            Load += FormOrders_Load;
            buttonBack.Click += buttonBack_Click;
            buttonAddOrder.Click += buttonAddOrder_Click;
            buttonEditOrder.Click += buttonEditOrder_Click;
            buttonDeleteOrder.Click += buttonDeleteOrder_Click;
        }

        private void FormOrders_Load(object sender, EventArgs e)
        {
            if (AppSession.CurrentUser == null ||
                (AppSession.CurrentUser.Role != UserRole.Admin &&
                 AppSession.CurrentUser.Role != UserRole.Manager))
            {
                MessageBox.Show(
                    "Просмотр заказов доступен только администратору и менеджеру.",
                    "Недостаточно прав",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                Close();
                return;
            }

            var admin = AppSession.CurrentUser.Role == UserRole.Admin;
            buttonAddOrder.Visible = admin;
            buttonEditOrder.Visible = admin;
            buttonDeleteOrder.Visible = admin;

            LoadOrders();
        }

        private void LoadOrders()
        {
            var orders = new List<OrderListItem>();

            using (var connection = new NpgsqlConnection(AppData.ConnectionString))
            {
                connection.Open();

                using (var command = new NpgsqlCommand(
                    @"select
                          o.id,
                          o.order_date,
                          o.delivery_date,
                          coalesce(pp.pickup_point_address, ''),
                          coalesce(u.full_name, ''),
                          coalesce(o.pickup_code, 0),
                          coalesce(s.status_name, ''),
                          coalesce(string_agg(i.item_name || ' x' || oi.quantity, ', ' order by i.item_name), '')
                      from public.orders o
                      left join public.pickup_points pp on pp.id = o.pickup_point
                      left join public.users u on u.id = o.client_full_name
                      left join public.status s on s.id = o.status_fk
                      left join public.orders_items oi on oi.order_id = o.id
                      left join public.items i on i.article = oi.article_fk
                      group by
                          o.id, o.order_date, o.delivery_date, pp.pickup_point_address,
                          u.full_name, o.pickup_code, s.status_name
                      order by o.id desc;",
                    connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        orders.Add(new OrderListItem
                        {
                            Id = reader.GetInt32(0),
                            OrderDate = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1),
                            DeliveryDate = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                            PickupPointAddress = reader.GetString(3),
                            ClientFullName = reader.GetString(4),
                            PickupCode = reader.GetInt32(5),
                            StatusName = reader.GetString(6),
                            ItemsSummary = reader.GetString(7)
                        });
                    }
                }
            }

            flowLayoutPanelProducts.Controls.Clear();
            selectedOrderId = null;
            selectedControl = null;

            foreach (var order in orders)
            {
                var card = new UserControlOrder();
                card.Width = Math.Max(730, flowLayoutPanelProducts.ClientSize.Width - 20);
                card.Bind(order);
                card.Selected += card_Selected;

                if (AppSession.CurrentUser != null && AppSession.CurrentUser.Role == UserRole.Admin)
                {
                    card.EditRequested += card_EditRequested;
                }

                flowLayoutPanelProducts.Controls.Add(card);
            }
        }

        private void card_Selected(object sender, EventArgs e)
        {
            var card = sender as UserControlOrder;
            if (card == null) return;

            if (selectedControl != null && !selectedControl.IsDisposed)
            {
                selectedControl.IsSelected = false;
            }

            selectedControl = card;
            selectedOrderId = card.OrderId;
            selectedControl.IsSelected = true;
        }

        private void card_EditRequested(object sender, EventArgs e)
        {
            buttonEditOrder_Click(sender, e);
        }

        private void buttonAddOrder_Click(object sender, EventArgs e)
        {
            OpenOrderForm(null);
        }

        private void buttonEditOrder_Click(object sender, EventArgs e)
        {
            if (AppSession.CurrentUser == null || AppSession.CurrentUser.Role != UserRole.Admin)
            {
                return;
            }

            if (!selectedOrderId.HasValue)
            {
                MessageBox.Show("Сначала выберите заказ.", "Редактирование", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            OpenOrderForm(selectedOrderId);
        }

        private void OpenOrderForm(int? id)
        {
            if (AppSession.CurrentUser == null || AppSession.CurrentUser.Role != UserRole.Admin)
            {
                return;
            }

            using (var form = new FormEditOrder(id))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    LoadOrders();
                }
            }
        }

        private void buttonDeleteOrder_Click(object sender, EventArgs e)
        {
            if (AppSession.CurrentUser == null || AppSession.CurrentUser.Role != UserRole.Admin)
            {
                return;
            }

            if (!selectedOrderId.HasValue)
            {
                MessageBox.Show("Сначала выберите заказ.", "Удаление", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(
                "Удалить выбранный заказ?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            using (var connection = new NpgsqlConnection(AppData.ConnectionString))
            {
                connection.Open();

                using (var command = new NpgsqlCommand(
                    "delete from public.orders_items where order_id=@id;",
                    connection))
                {
                    command.Parameters.AddWithValue("@id", selectedOrderId.Value);
                    command.ExecuteNonQuery();
                }

                using (var command = new NpgsqlCommand(
                    "delete from public.orders where id=@id;",
                    connection))
                {
                    command.Parameters.AddWithValue("@id", selectedOrderId.Value);
                    command.ExecuteNonQuery();
                }
            }

            LoadOrders();
        }

        private void buttonBack_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
