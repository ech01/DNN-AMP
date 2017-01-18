/*
' Copyright (c) 2017  Risdall Marketing Group
'  All rights reserved.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
' TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
' THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
' CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
' DEALINGS IN THE SOFTWARE.
' 
*/

using System;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Common.Utilities;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Collections.Generic;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.IO;
using System.Text;
using System.Net;
using System.Linq;
using Risdall.Modules.DNN_AMP.Components;

namespace Risdall.Modules.DNN_AMP
{
    public partial class View : PortalModuleBase
    {

        #region Event Handlers

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!Page.IsPostBack)
            {

            }
        }

        protected void cmdSave_Click(object sender, EventArgs e)
        {
            HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.OptionFixNestedTags = true;

            //Get current page URL
            string url = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority + HttpContext.Current.Request.RawUrl;

            //Pretend we are a browser
            HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:31.0) Gecko/20100101 Firefox/31.0";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-us,en;q=0.5");

            //Load page
            WebResponse response = request.GetResponse();
            htmlDoc.Load(response.GetResponseStream(), true);

            if (htmlDoc.DocumentNode != null)
            {
                //Get content pane
                var content = htmlDoc.DocumentNode
                                    .SelectSingleNode("//div[@id='dnn_ContentPane']");
                content.ParentNode.RemoveChild(content, true); //<--remove content div but keep inner

                string parsedHTML = GoogleAmpConverter.Convert(content.OuterHtml);

                var sbNewAMPPage = new StringBuilder();
                //grab template for start of document
                sbNewAMPPage.Append(File.ReadAllText(Server.MapPath(this.ControlPath + "templates/start.html")));
                //token replace for template
                sbNewAMPPage.Replace("[ItemUrl]", url);
                sbNewAMPPage.Replace("[LOGO]", this.PortalSettings.HomeDirectory + this.PortalSettings.LogoFile);

                sbNewAMPPage.Append(parsedHTML);
                sbNewAMPPage.Append("</body></html>");

                //create page name based on current tab
                string ampPageName = DotNetNuke.Entities.Tabs.TabController.CurrentPage.TabPath.Replace("//", "");
                //path to amp page
                string folderPath = this.PortalSettings.HomeDirectory + "AMP";
                lblTest.Text = folderPath;
                System.IO.Directory.CreateDirectory("~" + folderPath);


                string strFileName = HttpContext.Current.Server.MapPath(folderPath + "/" + ampPageName);
                strFileName = strFileName + "_" + DateTime.Now.ToString("yyyyMMdd") + ".html";
                FileStream fs = new FileStream(strFileName, FileMode.Create);
                StreamWriter writer = new StreamWriter(fs, Encoding.UTF8);
                writer.Write(sbNewAMPPage.ToString());
                writer.Close();

            }
           
           

        }

       

        #endregion

    }
}