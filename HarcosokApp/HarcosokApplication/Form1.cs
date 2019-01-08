using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace HarcosokApplication
{
	public partial class Form1 : Form
	{

		MySqlConnection conn;


		public Form1()
		{
			InitializeComponent();

			KapcsolatDB_Tablak();
		}

		//Kapcsolat open / Táblák SQL

		private void KapcsolatDB_Tablak()
		{

			const string harcosSQL = "CREATE TABLE IF NOT EXISTS harcosok(id INTEGER AUTO_INCREMENT PRIMARY KEY, " +
				"nev VARCHAR(50) NOT NULL UNIQUE, letrehozas DATE NOT NULL)";

			const string kepessegSQL = "CREATE TABLE IF NOT EXISTS kepessegek(id INTEGER AUTO_INCREMENT PRIMARY KEY , " +
				"nev VARCHAR(80) NOT NULL, leiras TEXT NOT NULL, harcos_id INTEGER NOT NULL, FOREIGN KEY (harcos_id) REFERENCES harcos(id) )";

			//conn

			try
			{
				conn = new MySqlConnection("Server=localhost;Database=cs_harcosok;Uid=root;Pwd=;");
				conn.Open();
				//harcostabla
				var letrehozasHarcos = conn.CreateCommand();
				letrehozasHarcos.CommandText = harcosSQL;
				letrehozasHarcos.ExecuteNonQuery();
				//kepessegtabla
				var letrehozasKepesseg = conn.CreateCommand();
				letrehozasKepesseg.CommandText = kepessegSQL;
				letrehozasKepesseg.ExecuteNonQuery();
				
			}
			catch (MySqlException e)
			{
				MessageBox.Show("Adatbázis hiba: " + e);
				this.Close();
			}

		}

		//Harcos hozzáadás
		private void letrehozasButton_Click(object sender, EventArgs e)
		{

			string nev = harcosNeveTextBox.Text;
			string letrehozas = DateTime.Now.ToString("yyyy'-'MM'-'dd");

			harcosokListBox.Items.Clear();

			//ellenorzes
			var ellenorzes = conn.CreateCommand();
			ellenorzes.CommandText = "SELECT COUNT(*) FROM harcosok WHERE nev = @nev";
			ellenorzes.Parameters.AddWithValue("@nev", nev);
			var darab = (long)ellenorzes.ExecuteScalar();

			if (darab != 0)
			{
				MessageBox.Show("Ez a harcos nev mar szerepel!");
				return;
			}

			if (harcosNeveTextBox.Text.Length > 0)
			{
				var insert = conn.CreateCommand();
				insert.CommandText = "INSERT INTO harcosok (id, nev, letrehozas) VALUES (NULL, @nev,@letrehozas)";
				insert.Parameters.AddWithValue("@nev", nev);
				insert.Parameters.AddWithValue("@letrehozas", letrehozas);
				insert.ExecuteNonQuery();
			}
			else
			{
				MessageBox.Show("Add meg a harcos nevét!");
			}

			var selectCmd = conn.CreateCommand();
			selectCmd.CommandText = "SELECT * FROM harcosok";

			var r = selectCmd.ExecuteReader();

			while (r.Read())
			{

				harcosokListBox.Items.Add(r["nev"].ToString());
				harcosokListBox.Items.Add(r["letrehozas"].ToString());

				hasznaloComboBox.Items.Add(r["nev"].ToString());

			}
			r.Close();
		}

		//Képesség hozzáadás
		private void hozzaadButton_Click(object sender, EventArgs e)
		{
			int hasznaloIndex = hasznaloComboBox.SelectedIndex;
			string nev = kepessegNeveTextBox.Text;
			string leiras = leirasTextBox.Text;
		
			//Ellenorzes
			if (kepessegNeveTextBox.Text.Length > 0 && leirasTextBox.Text.Length > 0)
			{

				MySqlDataReader reader = null;
				string select = "SELECT id FROM harcosok WHERE nev = '" + hasznaloComboBox.GetItemText(hasznaloComboBox.SelectedItem) + "'";
				MySqlCommand command = new MySqlCommand(select, conn);
				reader = command.ExecuteReader();
				var insert = conn.CreateCommand();
				int id = 0;
				while (reader.Read())
				{
					id = (int)reader["id"];
				}

				//hozzaadas
				insert.CommandText = "INSERT INTO kepessegek (id, nev, leiras, harcos_id) VALUES (NULL, '" + kepessegNeveTextBox.Text + "', '" + leirasTextBox.Text + "', '" + id + "');";
				reader.Close();
				insert.ExecuteNonQuery();

				MessageBox.Show("Sikeresen hozzáadtad a képességet!");
			}
			else
			{
				MessageBox.Show("A képesség nevét és leírását is add meg!");
			}
	
		}

		//Képesség megjelenítés

		private void KepessegMegjelenit()
		{
			kepessegekListBox.Items.Clear();
			try
			{
				//kepessegek tomb
				string[] item_nev = harcosokListBox.GetItemText(harcosokListBox.SelectedItem).Split(' ');
				MySqlDataReader reader = null;
				string select = @"SELECT nev FROM kepessegek WHERE harcos_id = 
                                    (SELECT harcos_id FROM kepessegek 
                                    LEFT JOIN harcosok ON harcosok.id = kepessegek.harcos_id 
                                    WHERE harcosok.nev = '" + item_nev[0] + "' " +
									"GROUP BY harcosok.id)";

				MySqlCommand command = new MySqlCommand(select, conn);
				reader = command.ExecuteReader();
				while (reader.Read())
				{
					string nev = (string)reader["nev"];
					kepessegekListBox.Items.Add(nev);
				}
				reader.Close();
			}
			catch (Exception)
			{
				throw;
			}
		}

		//del
		private void torlesButton_Click(object sender, EventArgs e)
		{
			try
			{
				string kepessegNev = kepessegekListBox.GetItemText(kepessegekListBox.SelectedItem);

				if (kepessegNev.Length>0)
				{
					string delete = "DELETE FROM kepessegek WHERE nev = '" + kepessegNev + "'";
					var deleteCommand = conn.CreateCommand();
					deleteCommand.CommandText = delete;
					deleteCommand.ExecuteNonQuery();

					MessageBox.Show("Sikeresen kitörölted a képességet!");

					KepessegMegjelenit();

					kepessegekLeirasaTextBox.Text = "";
				}

			}
			catch (Exception)
			{

				throw;
			}
		}

		//update
		private void modositButton_Click(object sender, EventArgs e)
		{
			try
			{
				string kepessegNev = kepessegekListBox.GetItemText(kepessegekListBox.SelectedItem);
				string kepessegLeiras = kepessegekLeirasaTextBox.Text;

				if (kepessegNev.Length > 0)
				{
					var update = conn.CreateCommand(); 
					update.CommandText = "UPDATE kepessegek SET leiras = @leiras WHERE nev = @nev";
					update.Parameters.AddWithValue("@leiras", kepessegLeiras);
					update.Parameters.AddWithValue("@nev",kepessegNev);
					int sor = update.ExecuteNonQuery();

					MessageBox.Show("Sikeresen módosítottad a képesség leírását!");

				}
			}
			catch (Exception)
			{

				throw;
			}
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			harcosokListBox.Items.Clear();

			using (var conn = new MySqlConnection("Server=localhost;Database=cs_harcosok;UID=root;Pwd="))
			{
				conn.Open();

				var command = conn.CreateCommand();
				command.CommandText = "SELECT * FROM harcosok";

				var r = command.ExecuteReader();


				while (r.Read())
				{

					harcosokListBox.Items.Add(r["nev"].ToString());
					harcosokListBox.Items.Add(r["letrehozas"].ToString());

					hasznaloComboBox.Items.Add(r["nev"].ToString());

				}

				conn.Close();
			}
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			conn.Close();
		}

		private void harcosokListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			KepessegMegjelenit();
		}

		private void kepessegekListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			//LeirasMegjelenit();
		}
	}
}
