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

public partial class _Default : System.Web.UI.Page
{
    private readonly string szToken = "hetx_weixin";//与微信公众账号后台的Token设置保持一致，区分大小写。

    protected void Page_Load(object sender, EventArgs e)
    {
        string szTimeStamp = Request["timestamp"];
        string szNonce = Request["nonce"];
        string szSignature = Request["signature"];
        string szEchoStr = Request["echostr"];

        if (Request.HttpMethod == "GET")
        {
            //这部分是提供给微信公众平台验证使用的 
            if (CheckSignature(szTimeStamp, szNonce, szSignature))
            {
                Response.Write(szEchoStr); //返回随机字符串则表示验证通过
            }
            else
            {
                Response.Write("failed:" + szSignature + "," + szTimeStamp + "。" +
                            "如果你在浏览器中看到这句话，说明此地址可以被作为微信公众账号后台的Url，请注意保持Token一致。");
            }
        }
        else
        {
            if (CheckSignature(szTimeStamp, szNonce, szSignature))
            {
                using (XmlReader xr = XmlReader.Create(Request.InputStream))
                {
                    XDocument RequestDocument = XDocument.Load(xr);
                    RequestDocument.Save(
                        Server.MapPath("~/App_Data/" + DateTime.Now.Ticks + "_Request_" +
                                       GetXmlString(RequestDocument, "FromUserName") + ".txt"));
                    string szMsgType = GetXmlString(RequestDocument, "MsgType");
                    string szToUserName = GetXmlString(RequestDocument, "ToUserName");
                    string szFromUserName = GetXmlString(RequestDocument, "FromUserName");
                    string szCreateTime = GetXmlString(RequestDocument, "CreateTime");
                    string szContent = "";
                    switch (szMsgType)
                    {
                        case "text":
                            string szCmd = GetXmlString(RequestDocument, "Content");
                            XElement ResponseEle  = GetXElementFromFile(szCmd);
                            \

                            if (ResponseEle != null)
                            {
                                Response.Output.Write(CreateResponesMsgXML(szFromUserName, szToUserName, szCreateTime, ResponseEle));
                                /*XDocument AutoDocument = XDocument.Load(szResponseString);
                                AutoDocument.Save(Server.MapPath("~/App_Data/" + DateTime.Now.Ticks + "_Request_text_" +
                                       GetXmlString(RequestDocument, "FromUserName") + ".txt"));
                                Response.Output.Write(CreateResponesMsgXML(szFromUserName, szToUserName, szCreateTime, AutoDocument));*/
                            }
                            else
                            {
                                Response.Output.Write(CreateResponesMsgXMLForText(szFromUserName, szToUserName, szCreateTime, "此消息并未设置自动回复"));
                            }
                            break;
                        case "event":
                            //判断Event类型
                            string szKey = GetXmlString(RequestDocument, "Event");
                            switch (szKey)
                            {
                                case "subscribe":
                                    szContent        = GetSubscribeContentFromFile();
                                    Response.Output.Write(CreateResponesMsgXMLForText(szFromUserName, szToUserName, szCreateTime, szContent));

                                    break;
                                case "CLICK":
                                    szContent = GetXmlString(RequestDocument, "EventKey");
                                    XElement ResponseEle2 = GetXElementFromFile((szContent));
                                    if (ResponseEle2 != null)
                                    {
                                        Response.Output.Write(CreateResponesMsgXML(szFromUserName, szToUserName, szCreateTime, ResponseEle2));
                                    }
                                    break;
                            }
                            break;
                    }
                }
            }
        }

        Response.End();
    }

    public string CreateResponesMsgXMLForText(string szToUserName, string szFromUserName, string szCreateTime, string szContent)
    {
        var doc = new XDocument();
        doc.Add(new XElement("xml"));
        var root = doc.Root;

        root.Add(new XElement("ToUserName", new XCData(szToUserName)));
        root.Add(new XElement("FromUserName", new XCData(szFromUserName)));
        root.Add(new XElement("CreateTime", szCreateTime));
        root.Add(new XElement("MsgType", new XCData("text")));
        root.Add(new XElement("Content", new XCData(szContent)));

        /*doc.Save(Server.MapPath("~/App_Data/" + DateTime.Now.Ticks + "_Respones_" +
                                       szToUserName + ".txt"));*/
        return doc.ToString();
    }

    public string CreateResponesMsgXMLForNews(string szToUserName, string szFromUserName, string szCreateTime, XElement EleContent)
    {
        var doc = new XDocument();
        doc.Add(new XElement("xml"));
        var root = doc.Root;

        root.Add(new XElement("ToUserName", new XCData(szToUserName)));
        root.Add(new XElement("FromUserName", new XCData(szFromUserName)));
        root.Add(new XElement("CreateTime", szCreateTime));
        root.Add(new XElement("MsgType", new XCData("news")));

        root.Add(EleContent.Elements());

        /*doc.Save(Server.MapPath("~/App_Data/" + DateTime.Now.Ticks + "_Respones_for_news_" +
                                       szToUserName + ".txt"));*/
        return doc.ToString();
    }

    public string CreateResponesMsgXML(string szToUserName, string szFromUserName, string szCreateTime, XElement doc)
    {
        string szRetXML = "";
        string szType = doc.Element("MsgType").Value;

        switch(szType)
        {
            case "text":
                string szTextContent = doc.Element("Content").Value;
                szRetXML = CreateResponesMsgXMLForText(szToUserName, szFromUserName, szCreateTime, szTextContent);
                break;
            case "news":
                XElement EleContent = doc.Element("Content");
                szRetXML = CreateResponesMsgXMLForNews(szToUserName, szFromUserName, szCreateTime, EleContent);
                break;
        }

        return szRetXML;
    }

    public string GetXmlString(XDocument doc, string szElementName)
    {
        if (doc.Root.Element(szElementName) != null)
        {
            return doc.Root.Element(szElementName).Value;
        }

        return "";
    }

    public bool CheckSignature(string szTimeStamp, string szNonce, string szSignature)
    {
        //这部分是提供给微信公众平台验证使用的 
        string[] pp = { szToken, szTimeStamp, szNonce };
        Array.Sort(pp);
        string catPP = "";
        for (int i = 0; i < 3; i++)
        {
            catPP += pp[i];
        }

        string p_p = FormsAuthentication.HashPasswordForStoringInConfigFile(catPP, "SHA1");
        if (p_p.ToLower() == szSignature.ToLower())
        {
            return true;
        }

        return false;        
    }

    public string GetSubscribeContentFromFile()
    {
        XDocument SubDocument = XDocument.Load(Server.MapPath("App_Data/subscribe.xml"));
        string szContent = GetXmlString(SubDocument, "Content");

        return szContent;
    }

    public XElement GetXElementFromFile(string szCmd)
    {
        XDocument AutoDocument = XDocument.Load(Server.MapPath("App_Data/auto.xml"));
        XElement AutoElement = AutoDocument.Root.Element(szCmd);
        if (AutoElement != null)
        {
            /*AutoElement.Save(Server.MapPath("~/App_Data/" + DateTime.Now.Ticks + "_xelement_.txt"));*/
        }

        return AutoElement;
    }
}