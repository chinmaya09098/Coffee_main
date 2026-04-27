using System;
using System.Drawing;
using System.Windows.Forms;

namespace Coffee
{
    public class MainForm : Form
    {
        private CoffeeModel currentCup;

        private TextBox txtBeans;
        private CheckBox chkMilk;
        private NumericUpDown nudSugar;
        private Button btnCreateCup;
        private Button btnAddSugar;
        private Label lblCurrentCup;
        private Label lblStatus;
        private Button btnConfirmOrder;
        private Button btnNewOrder;
        private ListBox lstOrders;

        public MainForm()
        {
            BuildUI();
        }
        
        private void BuildUI()
        {
            this.Text = "Coffee Order System";
            this.Size = new Size(460, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 235, 220);

            
            var lblTitle = new Label
            {
                Text = "Coffee Order System",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 40, 10),
                Location = new Point(0, 15),
                Size = new Size(444, 35),
                TextAlign = ContentAlignment.MiddleCenter
            };

            
            var lblBeans = new Label
            {
                Text = "Bean Type:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(60, 30, 10),
                Location = new Point(25, 65),
                Size = new Size(100, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };

            txtBeans = new TextBox
            {
                Font = new Font("Segoe UI", 10),
                Location = new Point(135, 65),
                Size = new Size(280, 25)
            };

            
            var lblMilk = new Label
            {
                Text = "Milk:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(60, 30, 10),
                Location = new Point(25, 105),
                Size = new Size(100, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };

            chkMilk = new CheckBox
            {
                Text = "Add Milk",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(60, 30, 10),
                Location = new Point(135, 105),
                Size = new Size(150, 25)
            };

            
            var lblSugar = new Label
            {
                Text = "Sugar (0-5):",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(60, 30, 10),
                Location = new Point(25, 145),
                Size = new Size(100, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };

            nudSugar = new NumericUpDown
            {
                Font = new Font("Segoe UI", 10),
                Location = new Point(135, 145),
                Size = new Size(70, 25),
                Minimum = 0,
                Maximum = 5
            };

            btnCreateCup = MakeButton("Create Cup", new Point(220, 143), new Size(100, 28),
                Color.FromArgb(100, 60, 20));
            btnCreateCup.Click += BtnCreateCup_Click;

            btnAddSugar = MakeButton("Add Sugar", new Point(330, 143), new Size(90, 28),
                Color.FromArgb(160, 110, 60));
            btnAddSugar.Click += BtnAddSugar_Click;
            btnAddSugar.Enabled = false;

            
            var divider = new Panel
            {
                Location = new Point(25, 185),
                Size = new Size(395, 1),
                BackColor = Color.FromArgb(180, 140, 100)
            };

            
            var lblCupTitle = new Label
            {
                Text = "Current Cup:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 30, 10),
                Location = new Point(25, 195),
                Size = new Size(395, 18)
            };

            lblCurrentCup = new Label
            {
                Text = "No cup created yet.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(80, 50, 20),
                Location = new Point(25, 215),
                Size = new Size(395, 38),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            };

            lblStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.Red,
                Location = new Point(25, 257),
                Size = new Size(395, 18)
            };

            
            btnConfirmOrder = MakeButton("Confirm Order", new Point(25, 280), new Size(140, 35),
                Color.FromArgb(80, 40, 10));
            btnConfirmOrder.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnConfirmOrder.Click += BtnConfirmOrder_Click;
            btnConfirmOrder.Enabled = false;

            btnNewOrder = MakeButton("New Order", new Point(180, 280), new Size(120, 35),
                Color.FromArgb(150, 100, 50));
            btnNewOrder.Click += BtnNewOrder_Click;

            var btnClearAll = MakeButton("Clear All", new Point(315, 280), new Size(105, 35),
                Color.FromArgb(180, 80, 60));
            btnClearAll.Click += (s, e) =>
            {
                lstOrders.Items.Clear();
                ResetForm();
            };

            
            var lblOrders = new Label
            {
                Text = "Orders Placed:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 30, 10),
                Location = new Point(25, 328),
                Size = new Size(395, 18)
            };

            lstOrders = new ListBox
            {
                Font = new Font("Segoe UI", 9),
                Location = new Point(25, 348),
                Size = new Size(395, 130),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 30, 10)
            };

            
            var btnDone = MakeButton("Done", new Point(320, 490), new Size(100, 32),
                Color.FromArgb(80, 80, 80));
            btnDone.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] {
                lblTitle,
                lblBeans, txtBeans,
                lblMilk, chkMilk,
                lblSugar, nudSugar, btnCreateCup, btnAddSugar,
                divider,
                lblCupTitle, lblCurrentCup, lblStatus,
                btnConfirmOrder, btnNewOrder, btnClearAll,
                lblOrders, lstOrders,
                btnDone
            });
        }

        private Button MakeButton(string text, Point location, Size size, Color backColor)
        {
            var btn = new Button
            {
                Text = text,
                Location = location,
                Size = size,
                Font = new Font("Segoe UI", 9),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void BtnCreateCup_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBeans.Text))
            {
                ShowStatus("Please enter a bean type.");
                return;
            }

            try
            {
                currentCup = new CoffeeModel(txtBeans.Text.Trim(), (int)nudSugar.Value, chkMilk.Checked);
                UpdateCupDisplay();
                btnAddSugar.Enabled = true;
                btnConfirmOrder.Enabled = true;
                ShowStatus("");
            }
            catch (ArgumentException ex)
            {
                ShowStatus(ex.Message);
            }
        }

        private void BtnAddSugar_Click(object sender, EventArgs e)
        {
            if (currentCup == null) return;

            int before = currentCup.Sugar;
            currentCup.AddSugar((int)nudSugar.Value);

            if (currentCup.Sugar == before && nudSugar.Value > 0)
                ShowStatus("Maximum sugar is 5 spoons.");
            else
                ShowStatus("");

            UpdateCupDisplay();
        }

        private void BtnConfirmOrder_Click(object sender, EventArgs e)
        {
            if (currentCup == null) return;
            
            lstOrders.Items.Add(string.Format("Order {0}: {1}", lstOrders.Items.Count + 1, currentCup.Details()));
            ResetForm();
            ShowStatus("Order placed!");
        }

        private void BtnNewOrder_Click(object sender, EventArgs e)
        {
            ResetForm();
        }

        private void UpdateCupDisplay()
        {
            lblCurrentCup.Text = currentCup != null ? "  " + currentCup.Details() : "No cup created yet.";
        }

        private void ResetForm()
        {
            currentCup = null;
            txtBeans.Clear();
            chkMilk.Checked = false;
            nudSugar.Value = 0;
            btnAddSugar.Enabled = false;
            btnConfirmOrder.Enabled = false;
            lblCurrentCup.Text = "No cup created yet.";
        }

        private void ShowStatus(string message)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = message.StartsWith("Order placed") ? Color.Green : Color.Red;
        }
    }
}