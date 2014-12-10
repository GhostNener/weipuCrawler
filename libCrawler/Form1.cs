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
    #region 正则
    //单条信息
    //<dl>.*?href="(?<href>[^"]+).*?">(?<title>[^<]+).*?<ddclass="author">.*?(?<author>.*?</dd+).*?<ddclass="journal">.*?<a.*?>(?<journal>[^<]+).*?<a.*?>(?<publishtime>[^<]+).*?<ddclass="abstract".*?</strong>.*?(?<abstract>[^<]+)>*?</dd>
    //数量
    //<div.*?class="search_gs">.*?</span>.*?(?<count>[^篇]+)
    //作者
    //@"WriterSearch\(\'.*?(?<author>[^']+)"
    //机构
    //<span.*?class="org">.*?<a.*?>.*?(?<org>[^<]+) 
    #endregion
    public partial class Form1 : Form
    {
        #region 配置
        static string key = "";//关键字
        static int page = 1;//翻页
        static bool isbegin = false;//是否在执行
        static readonly string startYear = "2012";//起始年份
        static readonly string endYear = "2014";//结束年份
        static readonly string cookie = "Cookie: __utma=85074125.440850300.1418113467.1418127327.1418184090.4; __utmz=85074125.1418113467.1.1.utmcsr=(direct)|utmccn=(direct)|utmcmd=(none); VIPSearID=817da61a-7f78-444c-8d93-8f3fd471bce4; Hm_lvt_36a05ff8f5e8a5be3ff03e1d75c8a9d1=1418114009,1418184090,1418185109; bdshare_firstime=1418123298936; __utma=164835757.1613674349.1418186771.1418186771.1418186771.1; __utmz=164835757.1418186771.1.1.utmcsr=(direct)|utmccn=(direct)|utmcmd=(none); Hm_lvt_69fff6aaf37627a0e2ac81d849c2d313=1418177630,1418186391; __utmb=85074125.52.9.1418187928072; ASP.NET_SessionId=6c7d8fbc-bcbb-44ab-8ff5-c9df0bdad2d2; LIBUser=UserID=11354827&ADUserID=11354827&UserName=%e8%b4%b5%e5%b7%9e%e8%b4%a2%e7%bb%8f%e5%a4%a7%e5%ad%a6&allcheck=B2714870D98B0E319ADB06F62F9BD9D5; __utmc=85074125; Hm_lpvt_36a05ff8f5e8a5be3ff03e1d75c8a9d1=1418187928; __utmb=164835757.1.10.1418186771; __utmc=164835757; Hm_lpvt_69fff6aaf37627a0e2ac81d849c2d313=1418186771; __utmt=1";
        #endregion
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (isbegin)
            {
                MessageBox.Show("已经开始了");
                return;
            }
            isbegin = true;
            System.Threading.Thread th = new System.Threading.Thread(handelhtml);
            th.IsBackground = true;
            th.Priority = System.Threading.ThreadPriority.Highest;
            th.Start();
        }

        public void handelhtml()
        {
            DataTable dt = getCounty();
            if (dt == null || dt.Rows.Count <= 0)
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
            string regcount = "<div.*?class=\"search_gs\">.*?</span>.*?(?<count>[^篇]+)";//匹配结果（数量）
            string reglist = "<dl>.*?href=\"(?<href>[^\"]+).*?\">(?<title>[^<]+).*?<ddclass=\"journal\">.*?<a.*?>(?<journal>[^<]+).*?<a.*?>(?<publishtime>[^<]+).*?<ddclass=\"abstract\".*?</strong>.*?(?<abstract>[^<]+)>*?</dd>";//匹配单条详细结果（内容）
            for (int i = 1; i <= page; i++)
            {
                string str = getHtmlRes(buildUrl(w, i)).Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("<fontstyle='color:red'>", "").Replace("</font>", "");
                setPage(regcount, str);
                Regex re = new Regex("<dl>.*?</dl>", RegexOptions.Singleline);//匹配每页列表
                MatchCollection mc = re.Matches(str);
                Regex regitem = new Regex(reglist, RegexOptions.Singleline);//匹配单条
                for (int j = 0; j < mc.Count; j++)
                {
                    string itemstr = mc[j].Value == null ? "" : mc[j].Value;
                    Match v = null;
                    try
                    {
                        v = regitem.Match(itemstr);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    if (v == null || !v.Success)
                    {
                        continue;
                    }
                    weipu wp = new weipu();
                    wp.Title = v.Groups["title"].Value;
                    if (wp.Title == null || wp.Title == "") { continue; }
                    wp.Abstract = v.Groups["abstract"].Value;
                    wp.CountyName = w;
                    wp.Href = "http://exam.cqvip.com" + v.Groups["href"].Value;
                    wp.PublishName = v.Groups["journal"].Value;
                    wp.Truetimekey = v.Groups["publishtime"].Value;
                    wp.Year_IntValue = wp.Truetimekey.Substring(0, 4);
                    Regex reauthor = new Regex(@"WriterSearch\(\'.*?(?<author>[^']+)", RegexOptions.Singleline);
                    MatchCollection alist = reauthor.Matches(mc[j].Value);//匹配作者
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
                    Match m = re.Match(orgstr);
                    wp.OrgName = m.Success ? m.Groups["org"].Value : wp.PublishName;
                    insertweipu(wp);
                }
            }
            page = 1;
        }
        private static void setPage(string regcount, string str)
        {
            if (page <= 1)
            {
                Regex re = new Regex(regcount, RegexOptions.Singleline);
                Match v = re.Match(str);
                if (!v.Success)
                {
                    page = 1;
                    return;
                }
                page = Convert.ToInt32(v.Groups["count"].Value);
                page = page <= 0 ? 1 : (int)Math.Ceiling(page / 20.0);
            }
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
            item.Connectionlimit = 10000;
            item.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            item.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:34.0) Gecko/20100101 Firefox/34.0";
            item.Host = "exam.cqvip.com";
            item.KeepAlive = true;
            item.Cookie = cookie;
            HttpResult httprst = httphelper.GetHtml(item);
            return httprst.Html == null ? "" : httprst.Html;
        }
        #endregion
        public static bool insertweipu(weipu w)
        {
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
            key += "[*]YY=" + startYear + "-" + endYear;
            // key = UrlEncode(key);
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
