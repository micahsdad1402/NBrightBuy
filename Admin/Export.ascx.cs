// --- Copyright (c) notice NevoWeb ---
//  Copyright (c) 2014 SARL NevoWeb.  www.nevoweb.com. The MIT License (MIT).
// Author: D.C.Lee
// ------------------------------------------------------------------------
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// ------------------------------------------------------------------------
// This copyright notice may NOT be removed, obscured or modified without written consent from the author.
// --- End copyright notice --- 

using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI.WebControls;
using System.Xml;
using DotNetNuke.Common;
using DotNetNuke.Entities.Portals;
using NBrightCore.common;
using NBrightCore.render;
using NBrightDNN;
using Nevoweb.DNN.NBrightBuy.Base;
using Nevoweb.DNN.NBrightBuy.Components;

namespace Nevoweb.DNN.NBrightBuy.Admin
{

    /// -----------------------------------------------------------------------------
    /// <summary>
    /// The ViewNBrightGen class displays the content
    /// </summary>
    /// -----------------------------------------------------------------------------
    public partial class Export : NBrightBuyAdminBase
    {


        override protected void OnInit(EventArgs e)
        {
            base.OnInit(e);

            try
            {


                #region "load templates"

                var t1 = "export.html";

                // Get Display Body
                var dataTempl = ModCtrl.GetTemplateData(ModSettings, t1, Utils.GetCurrentCulture(), DebugMode);
                // insert page header text
                rpData.ItemTemplate = NBrightBuyUtils.GetGenXmlTemplate(dataTempl, ModSettings.Settings(), PortalSettings.HomeDirectory);

            }
            catch (Exception exc)
            {
                //display the error on the template (don;t want to log it here, prefer to deal with errors directly.)
                var l = new Literal();
                l.Text = exc.ToString();
                phData.Controls.Add(l);
            }

        }


        protected override void OnLoad(EventArgs e)
        {
            try
            {
                base.OnLoad(e);
                if (Page.IsPostBack == false)
                {
                    PageLoad();
                }
            }
            catch (Exception exc) //Module failed to load
            {
                //display the error on the template (don;t want to log it here, prefer to deal with errors directly.)
                var l = new Literal();
                l.Text = exc.ToString();
                phData.Controls.Add(l);
            }
        }

        private void PageLoad()
        {

            #region "Data Repeater"
            if (UserId > 0) // only logged in users can see data on this module.
            {
                base.DoDetail(rpData);
            }

            #endregion

        }

                #endregion

        #region  "Events "

        protected void CtrlItemCommand(object source, RepeaterCommandEventArgs e)
        {
            var cArg = e.CommandArgument.ToString();
            var param = new string[3];

            switch (e.CommandName.ToLower())
            {
                case "export":
                    param[0] = "";
                    DoExport();
                    Response.Redirect(Globals.NavigateURL(TabId, "", param), true);
                    break;
                case "exportimages":
                    param[0] = "";
                    DoExportImages();
                    Response.Redirect(Globals.NavigateURL(TabId, "", param), true);
                    break;
                case "exportdocs":
                    param[0] = "";
                    DoExportDocs();
                    Response.Redirect(Globals.NavigateURL(TabId, "", param), true);
                    break;
                case "cancel":
                    param[0] = "";
                    Response.Redirect(Globals.NavigateURL(TabId, "", param), true);
                    break;
            }

        }

        #endregion

        private void DoExport()
        {
            var strXml = "<root>";

            if (GenXmlFunctions.GetField(rpData,"exportproducts") == "True")
            {
                var l = ModCtrl.GetList(PortalId, -1, "PRD");
                foreach (var i in l) { strXml += i.ToXmlItem(); }

                l = ModCtrl.GetList(PortalId, -1, "PRDLANG");
                foreach (var i in l) { strXml += i.ToXmlItem(); }
            
                l = ModCtrl.GetList(PortalId, -1, "PRDXREF");
                foreach (var i in l) { strXml += i.ToXmlItem(); }
            }

            if (GenXmlFunctions.GetField(rpData, "exportcategories") == "True")
            {
                var l = ModCtrl.GetList(PortalId, -1, "CATEGORY");
                foreach (var i in l) { strXml += i.ToXmlItem(); }

                l = ModCtrl.GetList(PortalId, -1, "CATEGORYLANG");
                foreach (var i in l) { strXml += i.ToXmlItem(); }
            }

            if (GenXmlFunctions.GetField(rpData, "exportcategories") == "True" && GenXmlFunctions.GetField(rpData, "exportproducts") == "True")
            {
                var l = ModCtrl.GetList(PortalId, -1, "CATCASCADE");
                foreach (var i in l) { strXml += i.ToXmlItem(); }
                l = ModCtrl.GetList(PortalId, -1, "CATXREF");
                foreach (var i in l) { strXml += i.ToXmlItem(); }
            }

            if (GenXmlFunctions.GetField(rpData, "exportproperties") == "True")
            {
                var l = ModCtrl.GetList(PortalId, -1, "GROUP");
                foreach (var i in l) { strXml += i.ToXmlItem(); }

                l = ModCtrl.GetList(PortalId, -1, "GROUPLANG");
                foreach (var i in l) { strXml += i.ToXmlItem(); }
            }

            if (GenXmlFunctions.GetField(rpData, "exportsettings") == "True")
            {
                var l = ModCtrl.GetList(PortalId, 0, "SETTINGS");
                foreach (var i in l) { strXml += i.ToXmlItem(); }
            }

            if (GenXmlFunctions.GetField(rpData, "exportorders") == "True")
            {
                var l = ModCtrl.GetList(PortalId, -1, "ORDER");
                foreach (var i in l) { strXml += i.ToXmlItem(); }
            }

            strXml += "</root>";

            var doc = new XmlDataDocument();
            doc.LoadXml(strXml);
            doc.Save(StoreSettings.Current.FolderUploadsMapPath + "\\export.xml");

            Utils.ForceDocDownload(StoreSettings.Current.FolderUploadsMapPath + "\\export.xml", PortalSettings.PortalAlias.HTTPAlias + "_export.xml", Response);
        }

        private void DoExportImages()
        {
            var fileMapPathList = new List<string>();

            var l = ModCtrl.GetList(PortalId, -1, "PRD");
            foreach (var i in l)
            {
                var nodlist = i.XMLDoc.SelectNodes("genxml/imgs/*");
                if (nodlist != null)
                {
                    foreach (XmlNode nod in nodlist)
                    {
                        var fname = nod.SelectSingleNode("./hidden/imagepath");
                        if (fname != null && fname.InnerText != "") fileMapPathList.Add(fname.InnerText);
                    }
                }
            }

            l = ModCtrl.GetList(PortalId, -1, "CATEGORY");
            foreach (var i in l)
            {
                var fname = i.GetXmlProperty("genxml/hidden/imagepath");
                if (fname != "") fileMapPathList.Add(fname);
            }

            DnnUtils.Zip(StoreSettings.Current.FolderUploadsMapPath + "\\exportimages.zip", fileMapPathList);

            Utils.ForceDocDownload(StoreSettings.Current.FolderUploadsMapPath + "\\exportimages.zip", PortalSettings.PortalAlias.HTTPAlias + "_exportimages.zip", Response);
        }

        private void DoExportDocs()
        {
            var fileMapPathList = new List<string>();

            var l = ModCtrl.GetList(PortalId, -1, "PRD");
            foreach (var i in l)
            {
                var nodlist = i.XMLDoc.SelectNodes("genxml/docs/*");
                if (nodlist != null)
                {
                    foreach (XmlNode nod in nodlist)
                    {
                        var fname = nod.SelectSingleNode("./hidden/docpath");
                        if (fname != null && fname.InnerText != "") fileMapPathList.Add(fname.InnerText);
                    }
                }
            }

            DnnUtils.Zip(StoreSettings.Current.FolderUploadsMapPath + "\\exportdocs.zip", fileMapPathList);

            Utils.ForceDocDownload(StoreSettings.Current.FolderUploadsMapPath + "\\exportdocs.zip", PortalSettings.PortalAlias.HTTPAlias + "_exportdocs.zip", Response);
        }
    }

}
