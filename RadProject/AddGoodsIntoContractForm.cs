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
    public partial class AddGoodsIntoContractForm : Form
    {

        DataSet ds = new DataSet();
        DataTable dt = new DataTable();

        NpgsqlConnection con;
        int id;

        public AddGoodsIntoContractForm(NpgsqlConnection connection, int contract_id)
        {
            InitializeComponent();
            this.con = connection;
            this.id = contract_id;
            initData();
        }

        private void initData() {
            string sql = "SELECT * FROM Goods;";
            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, this.con);
            ds.Reset();
            da.Fill(ds);
            dt = ds.Tables[0];
            dataGridView1.DataSource = dt;
            dataGridView1.Sort(dataGridView1.Columns["goods_id"], ListSortDirection.Ascending);
        }

        private void AddGoodsIntoContractForm_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            int goods_id = (int)dataGridView1.CurrentRow.Cells["goods_id"].Value;
            NpgsqlCommand com = new NpgsqlCommand("insert into Contract_Goods(contract_id, goods_id, amount) values (:contract_id, :goods_id, :amount)", this.con);
            com.Parameters.AddWithValue("contract_id", id);
            com.Parameters.AddWithValue("goods_id", goods_id);
            com.Parameters.AddWithValue("amount", int.Parse(textBox1.Text));
            com.ExecuteNonQuery();
            Close();
        }
    }
}
