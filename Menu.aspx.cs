using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Security;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Net;
using System.Web.Script.Serialization;

/*[DataContract]
class Project
{
    [DataMember]
    public string Input { get; set; }
    [DataMember]
    public string Output { get; set; }
}*/

public partial class Menu : System.Web.UI.Page
{
    private readonly string szAppId = "wx6ed1d160ea162245";
    private readonly string szAppSecret = "7b3a6090b7b59d8a41feb28569a04ee9";
    private static readonly string DefaultUserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
    private static string szAccessToken = "bBAdqOVkhsbWwZnq2IkgBxq0eewOMB6A8X3UzhB9St3oirbBJCGYjN-CawEoi1TRMl9jHXv0GQ7FqucmzV7jsgVYVyClCo8ImZjlUuYi8l3cUFjSlh9Jd0NtyCvLW4f6_Im2jasS2tDbGAJYxLZETA";

    protected void Page_Load(object sender, EventArgs e)
    {
        if (szAccessToken == "")
        {
            szAccessToken = GetAccessToken();
        }

        Response.Write(szAccessToken);

        string szResult = PostHttpString(GetPostDataFromFile(Server.MapPath("App_Data/menujson.txt")));
        //{"errcode":0,"errmsg":"ok"}
        string[] szArray = szResult.Split(',');
        string szErr = szArray[0].Remove(0, 11);

        Response.Write(szResult);
        Response.Write(szErr);

        if (szErr == "0")
        {
            Response.Write("设置成功");
        }
        else if (szErr == "42001")
        {
            szAccessToken = GetAccessToken();
            PostHttpString(GetPostDataFromFile(Server.MapPath("App_Data/menujson.txt")));
        }
    }

    public string GetAccessToken()
    {
        string url = "https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid=" + szAppId + "&secret=" + szAppSecret;
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentNullException("url");
        }
        HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
        request.Method = "GET";
        request.UserAgent = DefaultUserAgent;

        WebResponse wResponse = request.GetResponse();

        Stream sResponseStream = wResponse.GetResponseStream();
        using (System.IO.StreamReader reader = new System.IO.StreamReader(sResponseStream, Encoding.GetEncoding("utf-8")))
        {
            return reader.ReadToEnd();
        }
    }

    public string PostHttpString(string szPostData)
    {
        string szUrl = "https://api.weixin.qq.com/cgi-bin/menu/create?access_token=" + szAccessToken;
        Encoding encoding = Encoding.Default;
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(szUrl);

        request.Method = "post";
        request.Accept = "text/html, application/xhtml+xml, */*";
        request.ContentType = "application/x-www-form-urlencoded";

        byte[] buffer = encoding.GetBytes(szPostData);
        request.ContentLength = buffer.Length;
        request.GetRequestStream().Write(buffer, 0, buffer.Length);

        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        using (StreamReader reader = new StreamReader(response.GetResponseStream(), System.Text.Encoding.GetEncoding("utf-8")))
        {
            return reader.ReadToEnd();
        }
    }

    public string GetPostDataFromFile(string szFilePath)
    {
        StreamReader sr = new StreamReader(szFilePath);

        return sr.ReadToEnd();
    }
}