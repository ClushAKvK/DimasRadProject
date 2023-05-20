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

    public partial class MainFrom : Form
    {

        DataSet ds = new DataSet();
        DataTable dt = new DataTable();
        NpgsqlConnection con;

        // DataGridView для отображение данных о товарах в конкретном договоре(подробнее в функции: dataGridView1_CellClick)
        // помечу эту переменную как (*)
        DataGridView goods;

        // Словарь с русификацией для кнопок. В коде используются англ. значения для проверок, отображаются же по-русски
        Dictionary<string, string[]> buttons_tables = new Dictionary<string, string[]>()
        {
            ["button1"] = new string[] { "Client", "Клиенты" },
            ["button2"] = new string[] { "Goods", "Товары" },
            ["button3"] = new string[] { "Contract", "Договоры" },
            ["button4"] = new string[] { "Contract_Goods", "О договорах" }
        };

        // текущая таблица, можно поставить какую нибудь по-дефолту(см. названия кнопок)
        string current_table = "";

        public MainFrom()
        {
            InitializeComponent();
            // Открытие соодиения с БД. Тут надо поменять пароль и название БД
            this.con = new NpgsqlConnection(
                    "Server=localhost; Port=5432; Username=postgres; Password=2305; database=RadStore"
                );
            con.Open();

            // програмная инициализация (*)
            /*
                После выбора для отобржения таблицы Contract, под главным dataGridView1(собсна для отображения Contract)
                будет создано отобнажение таблицы с данными о товарах в контракте(см. функцию dataGridView1_CellClick) - это goods (*)
                и собсна для (*) задаем следующие параметры: Позицию, размер, задний фон, и т.д. 
             */
            goods = new DataGridView();
            goods.Location = new Point(125, 350);
            //contracts.Size = new Size(200, 200);
            goods.Size = new Size(833, 250);
            goods.MaximumSize = new Size(833, 250);
            goods.BackgroundColor = System.Drawing.SystemColors.Control;
            goods.Name = "goods";
            goods.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
            goods.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            goods.AutoSize = true;
            goods.BorderStyle = BorderStyle.None;
            goods.AllowUserToAddRows = false;

        }

        // функция для обновления содержания dataGridView1 - т.е. содержания соблиц из БД
        private void update_view(string table)
        {
            // Очищает таблицу (*), чтобы не мешалась
            if (this.Controls.Contains(goods))
                goods.Columns.Clear();

            // Опредляем в какую таблицу сейчас надо отобразить
            if (current_table == "Client" || current_table == "Goods")
            {
                // Просто выводим всю инфу из таблицы
                string sql = "SELECT * FROM " + current_table + ";";
                NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, this.con);
                ds.Reset();
                da.Fill(ds);
                dt = ds.Tables[0];
                dataGridView1.DataSource = dt;

                // Отдельно меняем размер на указанный, потому что в случае таблицу Contract он будет изменен
                // Если он и сейчас изменен из-за Contract, то меняем на исходный
                dataGridView1.Size = new Size(833, 511);

                // Руссификация названия колонок
                if (current_table == "Client")
                {
                    // Задаем названия и инициализируем колонки
                    string[] coColumns = { "Идентификатор", "Имя", "Фамилия" };
                    foreach (DataGridViewColumn col in dataGridView1.Columns)
                    {
                        col.HeaderText = coColumns[col.Index];
                    }
                }
                else {
                    // Задаем названия и инициализируем колонки
                    string[] coColumns = { "Идентификатор", "Название", "Описание", "Ед. измерения", "Цена"};
                    foreach (DataGridViewColumn col in dataGridView1.Columns)
                    {
                        col.HeaderText = coColumns[col.Index];
                    }
                }
            }
            else if (current_table == "Contract") {
                // Для таблицы Contract клиенты должны отображаться не в виде id, а 'по-человечески' - пускай будет отбражться фамилия
                // Это собсна и делает следующий запрос
                string sql = @"SELECT ct.contract_id, cl.last_name as client, ct.pay_type, ct.status, ct.register_date, ct.total_price 
                                FROM Contract ct
                                JOIN Client cl ON ct.client_id = cl.client_id;";
                NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, this.con);
                ds.Reset();
                da.Fill(ds);
                dt = ds.Tables[0];
                dataGridView1.DataSource = dt;

                string[] coColumns = { "Идентификатор", "Клиент", "Тип оплаты", "Статус", "День регистрации", "Итоговая сумма" };
                foreach (DataGridViewColumn col in dataGridView1.Columns) {
                    col.HeaderText = coColumns[col.Index];
                }

                // Как и было объявлено раньше, для Contract размер задается другой, что бы не мешать отображению (*)
                dataGridView1.Size = new Size(833, 250);
            }
            else if (current_table == "Contract_Goods")
            {
                // Для таблицы Contract товары должны отображаться не в виде id, а 'по-человечески' - пускай будет отбражться название товара
                // Это собсна и делает следующий запрос
                string sql = @"SELECT cg.contract_goods_id, cg.contract_id, go.title as goods, cg.amount, cg.price 
                                FROM Contract_Goods cg
                                JOIN Goods go ON go.goods_id = cg.goods_id;";
                NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, this.con);
                ds.Reset();
                da.Fill(ds);
                dt = ds.Tables[0];
                dataGridView1.DataSource = dt;
                dataGridView1.Columns["contract_goods_id"].DisplayIndex = 0;
                dataGridView1.Size = new Size(833, 511);

                string[] coColumns = { "Идентификатор", "Договор", "Название товара", "Количество", "Итоговая сумма" };
                foreach (DataGridViewColumn col in dataGridView1.Columns)
                {
                    col.HeaderText = coColumns[col.Index];
                }
            }

            // сортируем таблицы по id 
            dataGridView1.Sort(dataGridView1.Columns[current_table.ToLower() + "_id"], ListSortDirection.Ascending);
        }


        // Вспомогательная функцию для наполнения (*)
        // Входные данные: id контракта(договора)
        // Выходные данные: объект DataSource
        private DataTable select_all_goods_from_contract(int id) {
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();

            // Сложный запрос, в краце: среди товаров берет те, которые указаны в id-договоре, групирует по товарам, считает количество и сумму
            string sql = @"SELECT go.title, go.description, go.unit, count(go.title), sum(go.price) FROM Goods go
                            JOIN Contract_goods cg ON go.goods_id = cg.goods_id
                            JOIN Contract ct ON ct.contract_id = cg.contract_id and ct.contract_id = " + id + " " +
                            "GROUP BY cg.goods_id, go.title, go.description, go.unit, go.price;";

            //MessageBox.Show(sql);

            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, this.con);
            ds.Reset();
            da.Fill(ds);
            dt = ds.Tables[0];
            return dt;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        // Далее идет серия функций-обработчиков нажатия на копок
        private void button1_Click(object sender, EventArgs e)
        {
            // нажали на button1 - смотрим в словаре buttons_tables - её англ. значение таблицы
            this.current_table = buttons_tables["button1"][0];
            // а отображаем русское
            table_label.Text = buttons_tables["button1"][1];
            update_view(this.current_table);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.current_table = buttons_tables["button2"][0];
            table_label.Text = buttons_tables["button2"][1];
            update_view(this.current_table);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.current_table = buttons_tables["button3"][0];
            table_label.Text = buttons_tables["button3"][1];
            update_view(this.current_table);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.current_table = buttons_tables["button4"][0];
            table_label.Text = buttons_tables["button4"][1];
            update_view(this.current_table);
        }

        // Обработка меню-сверху: кнопка Составить отчет
        private void отчетToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Запускаем новую форму
            Form1 form = new Form1(this.con);
            form.ShowDialog();
        }

        // Аналогично и с другими
        private void добавитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddFrom form = new AddFrom(this.con, this.current_table);
            form.ShowDialog();
            update_view(current_table);
        }

        private void MainFrom_Load(object sender, EventArgs e)
        {

        }

        private void изменитьToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            int id = (int)dataGridView1.CurrentRow.Cells[current_table + "_id"].Value;
            UpdateForm form = new UpdateForm(this.con, this.current_table, id);
            form.ShowDialog();
            update_view(current_table);
        }

        private void удалитьToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // в данном случае берем id таблицы выбраной на dataGridView1 строке
            int id = (int)dataGridView1.CurrentRow.Cells[current_table + "_id"].Value;
            //  и удаляем из БД
            NpgsqlCommand com = new NpgsqlCommand("DELETE FROM " + current_table + " WHERE " + current_table + "_id = " + id + ";", this.con);
            com.Parameters.AddWithValue("id", id);
            com.ExecuteNonQuery();
            update_view(current_table);
        }

        // Функция наполения (*) - отображение данных о товарах в выбранном контракте
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // если текущая таблица не Contract - выходим
            if (current_table != "Contract") return;

            // Очищение (*) на случай, если мы переключились на другую запись Contract
            if (this.Controls.Contains(goods))
                goods.Columns.Clear();
            
            // наполнение (*)
            goods.DataSource = select_all_goods_from_contract((int)dataGridView1.CurrentRow.Cells["Contract_id"].Value);

            // русификация
            string[] coColumns = { "Название товара", "Описание", "Ед. измерения", "Количество", "Сумма" };
            foreach (DataGridViewColumn col in goods.Columns)
            {
                col.HeaderText = coColumns[col.Index];
            }

            // добавляем для отображени на полотне
            this.Controls.Add(goods);
        }
    }
}
