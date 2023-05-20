using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RadProject
{
    // подробное объяснение кода см в AddFrom - здесь те же самые элементы, за исключением того, в поля всталяются данные выбранной записи
    public partial class UpdateForm : Form
    {

        Dictionary<string, string[]> ru_columns = new Dictionary<string, string[]>()
        {
            ["Client"] = new string[] { "Имя", "Фамилия" },
            ["Goods"] = new string[] { "Название", "Кр. описание", "Ед. измерения", "Цена" },
            ["Contract"] = new string[] { "Клиент", "Тип оплаты", "Статус", "Дата регистрации" },
            ["Contract_Goods"] = new string[] { "Контракт", "Товар", "Количество" }
        };

        Dictionary<string, string[]> en_columns = new Dictionary<string, string[]>()
        {
            ["Client"] = new string[] { "first_name", "last_name" },
            ["Goods"] = new string[] { "title", "description", "unit", "price" },
            ["Contract"] = new string[] { "client_id", "pay_type", "status", "register_date" },
            ["Contract_Goods"] = new string[] { "contract_id", "goods_id", "amount" }
        };

        Dictionary<string, string> pay_type = new Dictionary<string, string>()
        {
            ["Наличные"] = "cash",
            ["Перевод"] = "transfer"
        };

        Dictionary<string, string> status = new Dictionary<string, string>()
        {
            ["Положен к отгрузке"] = "ready for shipment",
            ["Отгружен"] = "shipped"
        };

        Dictionary<int, int> client_ids = new Dictionary<int, int>();
        int temp_select_client = 0;
        bool flag = true;

        TextBox[] textBoxes;
        string[] data;

        ComboBox client_cb;
        ComboBox pay_type_cb;
        ComboBox status_cb;
        DateTimePicker date_dtp;

        TextBox amount_tb;

        NpgsqlConnection con;
        string table;
        int id;

        // ОТЛИЧИЕ: на вход ещё идет id - записи, которую хотим поменять
        public UpdateForm(NpgsqlConnection con, string table, int id)
        {
            InitializeComponent();
            this.con = con;
            this.table = table;
            this.id = id;

            if (table == "Client" || table == "Goods")
            {
                draw_default_view();
            }
            else if (table == "Contract")
            {
                draw_contract_view();
            }
            else if (table == "Contract_Goods")
            {
                draw_contract_goods_view();
            }
        }

        private void draw_default_view() {
            //ОТЛИЧИЕ: выгрузка данных из конкретной записи с указанным id
            data = new string[en_columns[table].Length];
            string sql = "SELECT * FROM " + table + " WHERE " + table + "_id = " + id.ToString();
            NpgsqlCommand com = new NpgsqlCommand(sql, this.con);

            NpgsqlDataReader reader = com.ExecuteReader();
            while (reader.Read())
            {
                int tb_idx = 0;
                foreach (string col in en_columns[table])
                {
                    data[tb_idx] = reader[col].ToString();
                    tb_idx++;
                }
            }
            reader.Close();

            string[] cols = this.ru_columns[table];

            for (int i = 1; i <= cols.Length; i++)
            {
                var col_lable = new Label();
                col_lable.Name = "lable" + i;
                col_lable.Location = new System.Drawing.Point(20, 40 * i);
                col_lable.Font = new System.Drawing.Font("Microsoft Sans Serif", 9, System.Drawing.FontStyle.Regular);
                col_lable.Text = cols[i - 1];
                this.Controls.Add(col_lable);
            }

            textBoxes = new TextBox[cols.Length];
            for (int i = 1; i <= cols.Length; i++)
            {
                var col_textBox = new TextBox();
                col_textBox.Name = "textBox" + i;
                col_textBox.Location = new Point(120, 40 * i - 2);
                col_textBox.Size = new Size(170, 10);
                // заполняем по-дефолту значением поля из data
                col_textBox.Text = data[i - 1];
                this.Controls.Add(col_textBox);
                textBoxes[i - 1] = col_textBox;
            }

            update_button.Location = new Point(130, 40 * cols.Length + 40);

            this.Width = 350;
            this.Height = update_button.Location.Y + 75;
        }

        private void draw_contract_view() {
            // ОТЛИЧИЕ: аналогично draw_default_view
            data = new string[en_columns[table].Length];
            string sql = "SELECT * FROM " + table + " WHERE " + table + "_id = " + id.ToString();
            NpgsqlCommand com = new NpgsqlCommand(sql, this.con);

            NpgsqlDataReader reader = com.ExecuteReader();
            while (reader.Read())
            {
                int tb_idx = 0;
                foreach (string col in en_columns[table])
                {
                    data[tb_idx] = reader[col].ToString();
                    tb_idx++;
                }
            }
            reader.Close();


            List<string> clients = select_all_from_client(int.Parse(data[0]));

            string[] cols = this.ru_columns[table];

            for (int i = 1; i <= cols.Length; i++)
            {
                var col_lable = new Label();
                col_lable.Name = "lable" + i;
                col_lable.Location = new System.Drawing.Point(20, 40 * i);
                col_lable.Font = new System.Drawing.Font("Microsoft Sans Serif", 9, System.Drawing.FontStyle.Regular);
                col_lable.Text = cols[i - 1];
                this.Controls.Add(col_lable);
            }

            client_cb = new ComboBox();
            client_cb.Name = "clients";
            client_cb.Location = new Point(120, 40);
            client_cb.Width = 130;
            client_cb.Height = 10;
            client_cb.Text = "Клиент";


            //clients_idx = new Dictionary<string, int>();
            int idx = 1;
            foreach (string client in clients)
            {
                client_cb.Items.Add(client);
                //clients_idx.Add(client, idx);
                idx++;
            }
            client_cb.SelectedIndex = temp_select_client;
            this.Controls.Add(client_cb);


            pay_type_cb = new ComboBox();
            pay_type_cb.Name = "pay_type";
            pay_type_cb.Location = new Point(120, 80);
            pay_type_cb.Width = 130;
            pay_type_cb.Height = 10;
            pay_type_cb.Text = "Тип оплаты";
            pay_type_cb.Items.Add("Наличные");
            pay_type_cb.Items.Add("Перевод");
            // ориентируясь на data ставим нужное значение типа оплаты
            if (data[1] == "cash")
                pay_type_cb.SelectedIndex = 0;
            else
                pay_type_cb.SelectedIndex = 1;
            this.Controls.Add(pay_type_cb);


            status_cb = new ComboBox();
            status_cb.Name = "status";
            status_cb.Location = new Point(120, 120);
            status_cb.Width = 130;
            status_cb.Height = 10;
            status_cb.Text = "Статус";
            status_cb.Items.Add("Положен к отгрузке");
            status_cb.Items.Add("Отгружен");
            // ориентируясь на data ставим нужное значение статуса
            if (data[2] == "ready for shipment")
                status_cb.SelectedIndex = 0;
            else
                status_cb.SelectedIndex = 1;
            this.Controls.Add(status_cb);


            date_dtp = new DateTimePicker();
            date_dtp.Format = DateTimePickerFormat.Short;
            date_dtp.Name = "reg_date";
            date_dtp.Location = new Point(120, 160);
            date_dtp.Width = 130;
            date_dtp.Height = 10;
            // ставим указаную дату из data
            date_dtp.Text = data[3];
            this.Controls.Add(date_dtp);


            update_button.Location = new Point(100, 200);

            this.Width = 300;
            this.Height = update_button.Location.Y + 75;
        }

        private List<string> select_all_from_client(int temp_id)
        {
            string sql = "SELECT * FROM Client;";
            NpgsqlCommand com = new NpgsqlCommand(sql, this.con);
            NpgsqlDataReader reader = com.ExecuteReader();
            List<string> clients = new List<string>();

            int client_id = 0;
            while (reader.Read())
            {
                if (int.Parse(reader["client_id"].ToString()) == temp_id)
                    flag = false;
                if (flag)
                    temp_select_client++;
                client_ids.Add(client_id, int.Parse(reader["client_id"].ToString()));
                client_id++;
                string client = reader["first_name"] + " " + reader["last_name"];
                clients.Add(client);
            }
            reader.Close();
            return clients;
        }

        private void draw_contract_goods_view() {
            data = new string[en_columns[table].Length];
            string sql = "SELECT * FROM " + table + " WHERE " + table + "_id = " + id.ToString();
            //MessageBox.Show(sql);
            NpgsqlCommand com = new NpgsqlCommand(sql, this.con);

            NpgsqlDataReader reader = com.ExecuteReader();
            while (reader.Read())
            {
                int tb_idx = 0;
                foreach (string col in en_columns[table])
                {
                    data[tb_idx] = reader[col].ToString();
                    tb_idx++;
                }
            }
            reader.Close();


            var amount = new Label();
            amount.Name = "amount";
            amount.Location = new System.Drawing.Point(20, 40);
            amount.Font = new System.Drawing.Font("Microsoft Sans Serif", 9, System.Drawing.FontStyle.Regular);
            amount.Text = "Количество";
            this.Controls.Add(amount);

            amount_tb = new TextBox();
            amount_tb.Name = "amount_tb";
            amount_tb.Location = new Point(120, 40);
            amount_tb.Size = new Size(70, 10);
            amount_tb.Text = data[2];
            this.Controls.Add(amount_tb);

            update_button.Location = new Point(80, 80);

            this.Width = 250;
            this.Height = update_button.Location.Y + 75;
        }

        private void UpdateForm_Load(object sender, EventArgs e)
        {

        }

        // Обработка кнопки 'Изменить'. По факту, в зависимости от выбранной таблицы, меняем все значения на собранные
        private void update_button_Click(object sender, EventArgs e)
        {
            if (table == "Client")
            {
                NpgsqlCommand com = new NpgsqlCommand(@"UPDATE Client SET (first_name, last_name) = (:first_name, :last_name) WHERE client_id = :id", this.con);
                com.Parameters.AddWithValue("first_name", textBoxes[0].Text);
                com.Parameters.AddWithValue("last_name", textBoxes[1].Text);
                com.Parameters.AddWithValue("id", this.id);
                com.ExecuteNonQuery();
                Close();
            }
            else if (table == "Goods")
            {
                NpgsqlCommand com = new NpgsqlCommand(@"UPDATE Goods SET (title, description, unit, price) = (:title, :description, :unit, :price) WHERE goods_id = :id", this.con);
                com.Parameters.AddWithValue("title", textBoxes[0].Text);
                com.Parameters.AddWithValue("description", textBoxes[1].Text);
                com.Parameters.AddWithValue("unit", textBoxes[2].Text);
                com.Parameters.AddWithValue("price", int.Parse(textBoxes[3].Text));
                com.Parameters.AddWithValue("id", this.id);
                com.ExecuteNonQuery();
                Close();
            }
            else if (table == "Contract")
            {
                NpgsqlCommand com = new NpgsqlCommand(@"UPDATE Contract SET (client_id, pay_type, status, register_date) = (:client_id, :pay_type, :status, :register_date) WHERE contract_id = :id", this.con);
                com.Parameters.AddWithValue("client_id", client_ids[client_cb.SelectedIndex]);
                com.Parameters.AddWithValue("pay_type", pay_type[pay_type_cb.SelectedItem.ToString().Trim()]);
                com.Parameters.AddWithValue("status", status[status_cb.SelectedItem.ToString()]);
                com.Parameters.AddWithValue("id", this.id);
                NpgsqlParameter date1 = new NpgsqlParameter("register_date", NpgsqlTypes.NpgsqlDbType.Date);
                date1.Value = date_dtp.Value.Date;
                com.Parameters.Add(date1);
                com.ExecuteNonQuery();
                Close();
            }
            else if (table == "Contract_Goods")
            {
                NpgsqlCommand com = new NpgsqlCommand(@"UPDATE Contract_Goods SET amount = :amount WHERE contract_goods_id = :id", this.con);
                com.Parameters.AddWithValue("amount", int.Parse(amount_tb.Text));
                com.Parameters.AddWithValue("id", this.id);
                com.ExecuteNonQuery();
                Close();
            }
        }
    }
}
