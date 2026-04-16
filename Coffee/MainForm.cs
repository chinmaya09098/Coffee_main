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

            // --- Title ---
            var lblTitle = new Label
            {
                Text = "Coffee Order System",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 40, 10),
                Location = new Point(0, 15),
                Size = new Size(444, 35),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // --- Bean Type ---
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

            // --- Milk ---
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

            // --- Sugar ---
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
                Color.FromArgb(100, 60, 