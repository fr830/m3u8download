using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.Threading;
using System.Net;
using System.IO;

namespace m3u8download
{
    public partial class Form1 : Form
    {
        HttpClient client;
        List<Thread> threads;
        List<string> files;
        List<string> list;
        Dictionary<string, int> parsent;
        string baseUrl;
        int index = 0;
        int count;

        private int SpeedOfProgress
        {
            get
            {
                return (int)(Math.Round(this.parsent.Where(x => x.Value == 100).Count() / (decimal)this.files.Count, 2) * 100);
            }
        }

        public Form1()
        {
            InitializeComponent();
            this.client = new HttpClient();
            this.threads = new List<Thread>();
            this.files = new List<string>();
            this.parsent = new Dictionary<string, int>();
            this.list = new List<string>();
            timer1.Tick += Timer1_Tick;
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (count <= int.Parse(this.txt_number.Text) && index < this.files.Count)
            {
                DownLoadFile(this.baseUrl + "/" + this.files[index++]);
            }

            var th = new Thread(new ThreadStart(() =>
            {
                this.Invoke(new Action(() =>
                {
                    foreach (var temp in this.parsent)
                    {

                        if (this.list.Contains(temp.Key))
                            goto Label;

                        var item = this.lv_list.FindItemWithText(temp.Key);
                        if (item != null)
                        {
                            item.SubItems[1].Text = temp.Value == -1 ? "下载出错" : temp.Value + "%";
                            if (temp.Value == 100)
                                list.Add(temp.Key);
                        }

                        Label:;
                    }

                    this.progbar_1.Value = this.SpeedOfProgress;
                }));
            }));
            th.Start();
        }

        private async void btn_download_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(this.txt_number.Text, out int a))
            {
                MessageBox.Show("线程数必须为");
                return;
            }

            string strData = null;
            try
            {
                strData = await this.client.GetStringAsync(this.txt_url.Text);
            }
            catch
            {
                this.txt_msg.Text += "下载文件失败，正在重试\r\n";
            }
            if (string.IsNullOrWhiteSpace(strData))
            {
                this.btn_download.Enabled = true;
            }
            else
            {
                this.baseUrl = this.txt_url.Text.Substring(0, this.txt_url.Text.LastIndexOf("/"));
                ShowInfo(strData);
                this.files = GetFilePath(strData);
                this.InitListView();
                timer1.Start();
            }
        }

        private void InitListView()
        {
            foreach (var path in this.files)
            {
                var item = new ListViewItem();
                item.Text = path;
                item.SubItems.Add("0%");
                this.lv_list.Items.Add(item);
            }
        }

        private void ShowInfo(string strData)
        {
            this.txt_msg.Text += "已找到m3u8文件\r\n";
            var files = GetFilePath(strData);
            this.txt_msg.Text += $"共有{files.Count}个文件\r\n";
            this.txt_msg.Text += "准备下载.....\r\n";
            this.txt_msg.Text += "已下载0%";
        }

        private List<string> GetFilePath(string strData)
        {
            return strData.Split('\n').Where(x => x.EndsWith(".ts")).ToList();
        }

        private async void DownLoadFile(string filepath)
        {
            lock (this)
            {
                count++;
            }
            string fileName = Path.GetFileName(filepath);
            this.parsent.Add(fileName, 0);
            var responseMessage = await client.GetAsync(filepath);
            var bys = new List<byte>();
            try
            {
                using (var stream = await responseMessage.Content.ReadAsStreamAsync())
                {
                    var length = stream.Length;
                    while (bys.Count <= length)
                    {
                        bys.Add((byte)stream.ReadByte());
                        this.parsent[fileName] = (int)(Math.Round((bys.Count / (decimal)length), 2) * 100);
                    }
                }
            }
            catch
            {
                this.parsent[fileName] = -1;
            }
            lock (this)
            {
                count--;
            }
            using (var fileStream = new FileStream($"{this.txt_download_path.Text}\\{fileName}", FileMode.Create, FileAccess.Write))
            {
                fileStream.Write(bys.ToArray(), 0, bys.Count);
            }
        }

        private void btn_dialog_Click(object sender, EventArgs e)
        {
            var result = this.folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                this.txt_download_path.Text = this.folderBrowserDialog1.SelectedPath;
            }
        }
    }
}
