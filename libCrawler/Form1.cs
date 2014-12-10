using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Helper;
using System.Web;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

namespace libCrawler
{
    //<dl>.*?href="(?<href>[^"]+).*?">(?<title>[^<]+).*?<ddclass="author">.*?(?<author>.*?</dd+).*?<ddclass="journal">.*?<a.*?>(?<journal>[^<]+).*?<a.*?>(?<publishtime>[^<]+).*?<ddclass="abstract".*?</strong>.*?(?<abstract>[^<]+)>*?</dd>
    //<div.*?class="search_gs">.*?</span>.*?(?<count>[^篇]+)
    //@"WriterSearch\(\'.*?(?<author>[^']+)"
    //<span.*?class="org">.*?<a.*?>.*?(?<org>[^<]+)
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        static string key = "";
        static int page = 1;
        static bool isbegin = false;
        private void button1_Click(object sender, EventArgs e)
        {
            if (isbegin)
            {
                MessageBox.Show("已经开始了");
                return;
            }
            isbegin = true;
            System.Threading.Thread th = new System.Threading.Thread(handelhtml);
            th.Start();
        }

        //object sender, System.Timers.ElapsedEventArgs e
        public void handelhtml()
        {
            DataTable dt = getCounty();
            if (dt == null||dt.Rows.Count<=0)
            {
                MessageBox.Show("木有关键字");
                isbegin = false;
                return;
            }
            foreach (DataRow v in dt.Rows)
            {
                handelJob(v["Name"].ToString());
                Helper.MySqlHelper.ExecuteNonQuery("UPDATE `county` SET  `Status`='0' WHERE (`Id`=@Id);", new MySqlParameter("@Id", v["Id"]));
            }
            isbegin = false;
            MessageBox.Show("执行完毕");
        }

        #region getCounty
        /// <summary>
        /// 获得县列表
        /// </summary>
        /// <returns></returns>
        public DataTable getCounty()
        {
            return Helper.MySqlHelper.ExecuteDataTable("SELECT county.`Name`,county.`Id` FROM county where county.`Status`=1");
        }
        #endregion
        #region handelJob 处理单个关键字返回的结果 ，并插入数据库
        /// <summary>
        /// 处理单个关键字返回的结果 ，并插入数据库
        /// </summary>
        /// <param name="w"></param>
        public void handelJob(string w)
        {
            string regcount = "<div.*?class=\"search_gs\">.*?</span>.*?(?<count>[^篇]+)";//搜索结果（数据）
            string reglist = "<dl>.*?href=\"(?<href>[^\"]+).*?\">(?<title>[^<]+).*?<ddclass=\"author\">.*?(?<author>.*?</dd+).*?<ddclass=\"journal\">.*?<a.*?>(?<journal>[^<]+).*?<a.*?>(?<publishtime>[^<]+).*?<ddclass=\"abstract\".*?</strong>.*?(?<abstract>[^<]+)>*?</dd>";//搜索结果（内容）
            for (int i = 1; i <= page; i++)
            {
                string str = getHtmlRes(buildUrl(w, i)).Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("<fontstyle='color:red'>", "").Replace("</font>", "");
                Regex re = new Regex(@"\w");
                MatchCollection mc = null;
                if (page <= 1)
                {
                    re = new Regex(regcount, RegexOptions.Singleline);
                    mc = re.Matches(str);
                    foreach (Match v in mc)
                    {
                        if (!v.Success)
                        {
                            continue;
                        }
                        page = Convert.ToInt32(v.Groups["count"].Value);
                        page = page <= 0 ? 1 : (int)Math.Ceiling(page / 20.0);
                        break;
                    }
                }
                re = new Regex(reglist, RegexOptions.Singleline);
                mc = re.Matches(str);
                foreach (Match v in mc)
                {
                    weipu wp = new weipu();
                    if (!v.Success)
                    {
                        continue;
                    }
                    wp.Title = v.Groups["title"].Value;
                    wp.Abstract = v.Groups["abstract"].Value;
                    wp.Author = v.Groups["author"].Value;
                    wp.CountyName = w;
                    wp.Href = "http://exam.cqvip.com" + v.Groups["href"].Value;
                    wp.PublishName = v.Groups["journal"].Value;
                    wp.Truetimekey = v.Groups["publishtime"].Value;
                    wp.Year_IntValue = wp.Truetimekey.Substring(0, 4);
                    re = new Regex(@"WriterSearch\(\'.*?(?<author>[^']+)", RegexOptions.Singleline);
                    MatchCollection alist = re.Matches(wp.Author);
                    wp.Author = "";
                    foreach (Match author in alist)
                    {
                        if (!v.Success)
                        {
                            break;
                        }
                        wp.Author += author.Groups["author"].Value + ";  ";
                    }
                    string orgstr = getHtmlRes(wp.Href);
                    re = new Regex("<span.*?class=\"org\">.*?<a.*?>.*?(?<org>[^<]+)", RegexOptions.Singleline);
                    MatchCollection orgmc = re.Matches(orgstr);
                    wp.OrgName = wp.PublishName;
                    foreach (Match m in orgmc)
                    {
                        if (!v.Success)
                        {
                            break;
                        }
                        wp.OrgName = m.Groups["org"].Value;
                        break;
                    }
                    insertweipu(wp);
                }
            }
            page = 1;
        }

        #endregion
        #region getHtmlRes 根据URL 返回html内容
        /// <summary>
        /// 根据URL 返回html内容
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string getHtmlRes(string url)
        {
            HttpHelper httphelper = new HttpHelper();
            HttpItem item = new HttpItem();
            item.URL = url;
            HttpResult httprst = httphelper.GetHtml(item);
            return httprst.Html;
        }
        #endregion

        public static bool insertweipu(weipu w)
        {
            //INSERT INTO `gzifelib`.`document` (`Id`, `Title`, `Author`, `PublishName`, `CountyName`, `Year_IntValue`, `OrgName`, `City`, `Href`, `Abstract`) VALUES ('1', '4324', '4324', '34', '4324', '324', '24324', '324', '432', '65467');

            object ex = Helper.MySqlHelper.ExecuteScalar("SELECT `document`.`Id` FROM `document` WHERE `Title`=@Title and `Href`=@Href", new MySqlParameter("@Title", w.Title), new MySqlParameter("@Href", w.Href));
            if (ex != null)
            {
                return false;
            }
            int rs = Helper.MySqlHelper.ExecuteNonQuery("INSERT INTO `document` ( `Title`, `Author`, `PublishName`, `CountyName`, `Year_IntValue`,   `Href`, `Abstract`,`Truetimekey`,`OrgName`) VALUES ( @Title, @Author, @PublishName,@CountyName,@Year_IntValue,  @Href,  @Abstract,@Truetimekey,@OrgName);",
                new MySqlParameter("@Title", w.Title),
                new MySqlParameter("@Author", w.Author),
                new MySqlParameter("@PublishName", w.PublishName),
                new MySqlParameter("@CountyName", w.CountyName),
                new MySqlParameter("@Year_IntValue", w.Year_IntValue),
                new MySqlParameter("@Href", w.Href),
                new MySqlParameter("@Abstract", w.Abstract),
                new MySqlParameter("@Truetimekey", w.Truetimekey),
                new MySqlParameter("@OrgName", w.OrgName));
            return rs > 0;
        }
        public static string buildUrl(string w, int p)
        {
            key = "M=";
            key += w;
            key = UrlEncode(key);
            string url = "http://exam.cqvip.com/zk/search.aspx?E=" + key + "&M=&P=" + p + "&CP=0&CC=0&LC=0&H=&Entry=M&S=1&SJ=0&ZJ=&GC=&Type=&strChkRecord=&ChkRecordCount=0";
            return url;
        }
        public static string UrlEncode(string str)
        {
            StringBuilder sb = new StringBuilder();
            byte[] byStr = System.Text.Encoding.UTF8.GetBytes(str); //默认是System.Text.Encoding.Default.GetBytes(str)
            for (int i = 0; i < byStr.Length; i++)
            {
                sb.Append(@"%" + Convert.ToString(byStr[i], 16));
            }
            return (sb.ToString());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (isbegin)
            {
                MessageBox.Show("程序正在执行");
                return;
            }
            GC.Collect();
            Application.Exit();
        }
    }
    public class weipu
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string PublishName { get; set; }
        public string CountyName { get; set; }
        public string Year_IntValue { get; set; }
        public string OrgName { get; set; }
        public string City { get; set; }
        public string Href { get; set; }
        public string Abstract { get; set; }
        public string Truetimekey { get; set; }
    }
}
