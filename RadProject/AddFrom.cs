using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;

namespace RadProject
{
    // Класс для добавления новой записи в БД
    public partial class AddFrom : Form
    {
        // англ. названия полей
        Dictionary<string, string[]> en_columns = new Dictionary<string, string[]>() {
            ["Client"] = new string[] { "first_name", "last_name" },
            ["Goods"] = new string[] { "title", "description", "unit", "price" },
            ["Contract"] = new string[] { "client_id", "pay_type", "status", "register_date", "total_price" },
            ["Contract_Goods"] = new string[] { "contract_id", "goods_id", "amount", "price" }
        };

        // рус. названия полей
        Dictionary<string, string[]> ru_columns = new Dictionary<string, string[]>()
        {
            ["Client"] = new string[] { "Имя", "Фамилия" },
            ["Goods"] = new string[] { "Название", "Кр. описание", "Ед. измерения", "Цена" },
            ["Contract"] = new string[] { "Клиент", "Тип оплаты", "Статус", "Дата регистрации" },
            ["Contract_Goods"] = new string[] { "Контракт", "Товар", "Количество" }
        };

        // словарь для соответсвия рус и англ версии типа оплаты
        // БД принимает только такие значения (английские), т.к. стоит ограничение CHECK
        Dictionary<string, string> pay_type = new Dictionary<string, string>() {
            ["Наличные"] = "cash",
            ["Перевод"] = "transfer"
        };

        // тоже самое со статусом закаща
        Dictionary<string, string> status = new Dictionary<string, string>() {
            ["Положен к отгрузке"] = "ready for shipment",
            ["Отгружен"] = "shipped"
        };


        // словарь для соответсвия id-клиентов с id-строк выпадающего списка(см. функцию draw_contract_view)
        Dictionary<int, int> client_ids = new Dictionary<int, int>();

        // Т.к. для разных таблиц, необходимы разные типы данных(строки, цифры, выпадающие списки и т д),
        // то приходится вручную задавать эти компоненты - создавать программно
        
        // компоненты для ввода текста
        TextBox[] textBoxes;

        // вып. списки и выбор даты - для Contract
        ComboBox client_cb;
        ComboBox pay_type_cb;
        ComboBox status_cb;
        DateTimePicker date_dtp;

        // Компоненты для добавления в таб. Contract_goods
        DataGridView goods;
        DataGridView contracts;
        TextBox amount_tb;
        Dictionary<string, int> clients_idx;

        NpgsqlConnection con;
        string table;

        // На вход формы только соединение с БД и название таблицы
        public AddFrom(NpgsqlConnection con, string table)
        {
            InitializeComponent();
            this.con = con;
            this.table = table;

            // используем разные функция для добавления новой записи в разные таблицы
            if (table == "Client" || table == "Goods")
            {
                draw_default_view();
            }
            else if (table == "Contract")
            {
                draw_contract_view();
            }
            else if (table == "Contract_Goods") {
                draw_contract_goods_view();
            }
        }

        // фукция для наполнения формы Client и Goods
        private void draw_default_view() {
            
            // выбираем рус. названия полей
            string [] cols = this.ru_columns[table];

            // инициализуруем названия(lable) полей
            for (int i = 1; i <= cols.Length; i++) {
                var col_lable = new Label();
                col_lable.Name = "lable" + i;
                col_lable.Location = new System.Drawing.Point(20, 40 * i);
                col_lable.Font = new System.Drawing.Font("Microsoft Sans Serif", 9, System.Drawing.FontStyle.Regular);
                col_lable.Text = cols[i - 1];
                this.Controls.Add(col_lable);
            }

            // выбираем англ. названия полей
            cols = this.en_columns[table];
            // инициализируем поля для ввода(textBox) справа от названий
            textBoxes = new TextBox[cols.Length];
            for (int i = 1; i <= cols.Length; i++) {
                var col_textBox = new TextBox();
                col_textBox.Name = "textBox" + i;
                col_textBox.Location = new Point(120, 40 * i - 2);
                col_textBox.Size = new Size(100, 10);
                this.Controls.Add(col_textBox);
                textBoxes[i - 1] = col_textBox;
            }

            // ставим на место кнопку для подтверждения
            add_button.Location = new Point(80, 40 * cols.Length + 40);
            
            // задаем размер окна
            this.Width = 250;
            this.Height = add_button.Location.Y + 75;
        }


        // фукция для наполнения формы Contract
        private void draw_contract_view() {
            // берем ФИО клиентов, наполяем client_ids (см. функцию select_all_from_client)
            List<string> clients = select_all_from_client();

            // Рус. названия полей
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

            // Инициализируем выпадающий список для отображения клиентов
            client_cb = new ComboBox();
            client_cb.Name = "clients";
            client_cb.Location = new Point(120, 40);
            client_cb.Width = 130;
            client_cb.Height = 10;
            client_cb.Text = "Клиент";

            // 
            clients_idx = new Dictionary<string, int>();
            int idx = 1;
            foreach (string client in clients) {
                client_cb.Items.Add(client);
                clients_idx.Add(client, idx);
                idx++;
            }
            
            this.Controls.Add(client_cb);


            // Инициализируем выпадающий список для отображения тип оплаты
            pay_type_cb = new ComboBox();
            pay_type_cb.Name = "pay_type";
            pay_type_cb.Location = new Point(120, 80);
            pay_type_cb.Width = 130;
            pay_type_cb.Height = 10;
            pay_type_cb.Text = "Тип оплаты";
            pay_type_cb.Items.Add("Наличные");
            pay_type_cb.Items.Add("Перевод");
            this.Controls.Add(pay_type_cb);


            // Инициализируем выпадающий список для отображения статуса контракта
            status_cb = new ComboBox();
            status_cb.Name = "status";
            status_cb.Location = new Point(120, 120);
            status_cb.Width = 130;
            status_cb.Height = 10;
            status_cb.Text = "Статус";
            status_cb.Items.Add("Положен к отгрузке");
            status_cb.Items.Add("Отгружен");
            this.Controls.Add(status_cb);

            // Инициализируем 'тыкер' даты регистрации договора
            date_dtp = new DateTimePicker();
            date_dtp.Format = DateTimePickerFormat.Short;
            date_dtp.Name = "reg_date";
            date_dtp.Location = new Point(120, 160);
            date_dtp.Width = 130;
            date_dtp.Height = 10;
            this.Controls.Add(date_dtp);

            // задаем положение кнопки подтверждения
            add_button.Location = new Point(100, 200);

            // и размер окна
            this.Width = 300;
            this.Height = add_button.Location.Y + 75;
        }

        // вспомогательная функция для выбора всех клиентов из таблицы Client и наполнения словаря client_ids
        // входные данные : нет
        // выходные данные : Лист{Имя + Фамилия} - клиентов, т.е. содержит чисто ФИО клиентов
        private List<string> select_all_from_client() {
            string sql = "SELECT * FROM Client;";
            NpgsqlCommand com = new NpgsqlCommand(sql, this.con);
            NpgsqlDataReader reader = com.ExecuteReader();
            List<string> clients = new List<string>();

            // т.к. id в combo_box начинаются с 0, будем привязывать последовательно эти id-Combo_box к id-клиентов
            int client_id = 0;
            while (reader.Read()) {
                // это происходит здесь. Т.е. наполняем словарь client_ids
                client_ids.Add(client_id, int.Parse(reader["client_id"].ToString()));
                client_id++;
                // парсим запись и соединяем в строку с ФИО
                string client = reader["first_name"] + " " + reader["last_name"];
                clients.Add(client);
            }
            reader.Close();
            return clients;
        }

        // фукция для наполнения формы Contract_goods - таблицы Сущности-связи договор - товар
        // Создаются два объекта DataGridView: contracts и goods - для отображения таблиц договоров и товаров соответственно
        // Работает это так: выбирается запись('тыкается') запись на contract и goods, указывается кол-во 'тыкнутого' товара.
        // После чего в 'тыкнутый' договор добавляется N-ое, задаваемое, кол-во 'тыкнутого' товара, и сам этот товар
        private void draw_contract_goods_view() {

            // Инициализация объекта DataGridView - contracts, для отображения контрактов
            contracts = new DataGridView();
            contracts.Location = new Point(40, 60);
            contracts.MinimumSize = new Size(50, 50);
            contracts.MaximumSize = new Size(500, 250);
            contracts.BackgroundColor = System.Drawing.SystemColors.Control;
            contracts.Name = "contracts";
            contracts.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
            contracts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            contracts.AutoSize = true;
            contracts.BorderStyle = BorderStyle.None;
            contracts.AllowUserToAddRows = false;
            // нполняем данными из Contract
            contracts.DataSource = select_all_from("Contract");
            //contracts.Sort(contracts.Columns["contract_id"], ListSortDirection.Ascending);
            this.Controls.Add(contracts);

            // Подпись таблицы Contract
            var col_lable = new Label();
            col_lable.Name = "lable1";
            col_lable.Location = new System.Drawing.Point(270, 20);
            col_lable.Font = new System.Drawing.Font("Microsoft Sans Serif", 12, System.Drawing.FontStyle.Regular);
            col_lable.Text = "Договоры";
            this.Controls.Add(col_lable);


            // аналогично для goods
            goods = new DataGridView();
            goods.Location = new Point(600, 60);
            goods.MinimumSize = new Size(50, 50);
            goods.MaximumSize = new Size(500, 250);
            goods.BackgroundColor = System.Drawing.SystemColors.Control;
            goods.Name = "goods";
            goods.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
            goods.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            goods.AutoSize = true;
            goods.BorderStyle = BorderStyle.None;
            goods.AllowUserToAddRows = false;
            // нполняем данными из Goods
            goods.DataSource = select_all_from("Goods");
            this.Controls.Add(goods);


            // Подпись таблицы Goods
            var col_lable2 = new Label();
            col_lable2.Name = "lable2";
            col_lable2.Location = new System.Drawing.Point(850, 20);
            col_lable2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12, System.Drawing.FontStyle.Regular);
            col_lable2.Text = "Товары";
            this.Controls.Add(col_lable2);


            // Инициализируем названия элемента ввода(или просто lable - подпись)
            var amount = new Label();
            amount.Name = "amount";
            amount.Location = new System.Drawing.Point(40, 325);
            amount.Font = new System.Drawing.Font("Microsoft Sans Serif", 9, System.Drawing.FontStyle.Regular);
            amount.Text = "Количество";
            this.Controls.Add(amount);

            // иничиализируем собсна элемент ввода кол-ва 'тыкнутого' товара
            amount_tb = new TextBox();
            amount_tb.Name = "amount_tb";
            amount_tb.Location = new Point(140, 325);
            amount_tb.Size = new Size(50, 10);
            this.Controls.Add(amount_tb);

            // положение кнопки
            add_button.Location = new Point(40, 360);

            // размер окна
            this.Width = 1150;
            this.Height = add_button.Location.Y + 75;
        }

        // вспомогательная функция выбора товаров из таблицы
        private DataTable select_all_from(string table_name) {
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();

            string sql;
            if (table_name == "Contract")
            {
                // вместо id клиентов выводим Фамилии
                sql = @"SELECT ct.contract_id, cl.last_name as client, ct.pay_type, ct.status, ct.register_date, ct.total_price 
                                FROM Contract ct
                                JOIN Client cl ON ct.client_id = cl.client_id;";
            }
            else {
                sql = "SELECT * FROM " + table_name + ";";
            } 
                
            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, this.con);
            ds.Reset();
            da.Fill(ds);
            dt = ds.Tables[0];
            return dt;

        }

        private void AddFrom_Load(object sender, EventArgs e)
        {

        }

        // функция обработки кнопки 'Добавить'
        // По факту собираем полученные данные в зависимости от текущей таблицы, составляем запрос, вставляем полученные данные в него и деплоим в БД
        private void add_button_Click(object sender, EventArgs e)
        {
            if (table == "Client")
            {
                NpgsqlCommand com = new NpgsqlCommand("insert into client(first_name, last_name) values (:first_name, :last_name)", this.con);
                com.Parameters.AddWithValue("first_name", textBoxes[0].Text);
                com.Parameters.AddWithValue("last_name", textBoxes[1].Text);
                com.ExecuteNonQuery();
                Close();
            }
            else if (table == "Goods")
            {
                NpgsqlCommand com = new NpgsqlCommand("insert into goods(title, description, unit, price) values (:title, :description, :unit, :price)", this.con);
                com.Parameters.AddWithValue("title", textBoxes[0].Text);
                com.Parameters.AddWithValue("description", textBoxes[1].Text);
                com.Parameters.AddWithValue("unit", textBoxes[2].Text);
                com.Parameters.AddWithValue("price", int.Parse(textBoxes[3].Text));
                com.ExecuteNonQuery();
                Close();
            }
            else if (table == "Contract")
            {
                NpgsqlCommand com = new NpgsqlCommand("insert into Contract(client_id, pay_type, status, register_date) values (:client_id, :pay_type, :status, :register_date)", this.con);
                //MessageBox.Show(client_cb.SelectedIndex.ToString());
                com.Parameters.AddWithValue("client_id", client_ids[client_cb.SelectedIndex]);
                com.Parameters.AddWithValue("pay_type", pay_type[pay_type_cb.SelectedItem.ToString().Trim()]);
                com.Parameters.AddWithValue("status", status[status_cb.SelectedItem.ToString()]);
                NpgsqlParameter date1 = new NpgsqlParameter("register_date", NpgsqlTypes.NpgsqlDbType.Date);
                date1.Value = date_dtp.Value.Date;
                com.Parameters.Add(date1);
                com.ExecuteNonQuery();
                Close();
            }
            else if (table == "Contract_Goods") {
                int contract_id = (int)contracts.CurrentRow.Cells["contract_id"].Value;
                int goods_id = (int)goods.CurrentRow.Cells["goods_id"].Value;
                NpgsqlCommand com = new NpgsqlCommand("insert into Contract_Goods(contract_id, goods_id, amount) values (:contract_id, :goods_id, :amount)", this.con);
                com.Parameters.AddWithValue("contract_id", contract_id);
                com.Parameters.AddWithValue("goods_id", goods_id);
                com.Parameters.AddWithValue("amount", int.Parse(amount_tb.Text));
                com.ExecuteNonQuery();
                Close();
            }
        }
    }
}
