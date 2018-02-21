using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using ENusbaum.Applications.WWWinamp.Classes;

namespace ENusbaum.Applications.WWWinamp.Forms
{
    public partial class frmAddUser : Form
    {
        public frmAddUser()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Verify Path selected still exists
            if(!Directory.Exists(textBox1.Text))
            {
                MessageBox.Show("Selected path does not exist. Please verify and try again.");
                return;
            }

            //Verify Username is specified
            if(textBox2.Text.Equals(""))
            {
                MessageBox.Show("Username is blank. Please verify and try again.");
                return;
            }

            //Verify Passwords Match
            if(!textBox3.Text.Equals(textBox4.Text) && !String.IsNullOrEmpty(textBox3.Text) && !String.IsNullOrEmpty(textBox4.Text))
            {
                MessageBox.Show("Passwords do not match or are blank. Please verify and try again.");
                return;
            }

            string sHash = string.Empty;

            if (radioButton1.Checked) sHash = Convert_PlaintextAuthToSHAAuth(textBox2.Text + ":" + textBox3.Text);
            if (radioButton2.Checked) sHash = Convert_PlaintextAuthToMD5Auth(textBox2.Text + ":" + textBox3.Text);

            TextWriter oTW = new StreamWriter(textBox1.Text + @"\.htpasswd", File.Exists(textBox1.Text + @"\.htpasswd"));
            oTW.WriteLine(sHash);
            oTW.Close();

            MessageBox.Show("User Added!");
            textBox2.Text = string.Empty;
            textBox3.Text = string.Empty;
            textBox4.Text = string.Empty;
        }

        private string Convert_PlaintextAuthToSHAAuth(string sAuth)
        {
            string sAuthString;
            string[] sPassword = sAuth.Split(new char[] { ':' });  //Extract the password

            sAuthString = sPassword[0] + ":{SHA}" + Convert.ToBase64String(Functions.Create_SHA1Hash(Encoding.ASCII.GetBytes(sPassword[1])));

            return sAuthString;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "Select folder:";
            folderBrowserDialog1.ShowDialog();
            textBox1.Text = folderBrowserDialog1.SelectedPath;
        }

        private string Convert_PlaintextAuthToMD5Auth(string sAuth)
        {
            string sAuthString;
            string[] sPassword = sAuth.Split(new char[] { ':' });  //Extract the password

            sAuthString = sPassword[0] + ":$apr1$" + Encoding.ASCII.GetString(Functions.Create_MD5Hash(Encoding.ASCII.GetBytes(sPassword[1])));

            return sAuthString;
        }

    }
}