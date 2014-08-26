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
    public partial class Import : NBrightBuyAdminBase
    {

        private Dictionary<int, int> _recordXref;

        override protected void OnInit(EventArgs e)
        {
            base.OnInit(e);

            try
            {


                #region "load templates"

                var t1 = "import.html";

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
                case "import":
                    param[0] = "";
                    var importXML = GenXmlFunctions.GetGenXml(rpData, "", StoreSettings.Current.FolderUploadsMapPath);
                    var nbi = new NBrightInfo(false);
                    nbi.XMLData = importXML;
                    _recordXref = new Dictionary<int, int>();
                    DoImport(nbi);
                    DoImportImages(nbi);
                    DoImportDocs(nbi);
                    Response.Redirect(Globals.NavigateURL(TabId, "", param), true);
                    break;
                case "cancel":
                    param[0] = "";
                    Response.Redirect(Globals.NavigateURL(TabId, "", param), true);
                    break;
            }

        }

        #endregion

        private void DoImport(NBrightInfo nbi)
        {
            var fname = StoreSettings.Current.FolderUploadsMapPath + "\\" + nbi.GetXmlProperty("genxml/hidden/hiddatafile");
            if (System.IO.File.Exists(fname))
            {

                var xmlFile = new XmlDataDocument();
                xmlFile.Load(fname);

                if (GenXmlFunctions.GetField(rpData, "importproducts") == "True")
                {
                    ImportRecord(xmlFile,"PRD");
                    ImportRecord(xmlFile, "PRDLANG");
                    ImportRecord(xmlFile, "PRDXREF");
                }

                if (GenXmlFunctions.GetField(rpData, "importcategories") == "True")
                {
                    ImportRecord(xmlFile, "CATEGORY");
                    ImportRecord(xmlFile, "CATEGORYLANG");
                }

                if (GenXmlFunctions.GetField(rpData, "importcategories") == "True" && GenXmlFunctions.GetField(rpData, "importproducts") == "True")
                {
                    ImportRecord(xmlFile, "CATCASCADE");
                    ImportRecord(xmlFile, "CATXREF");
                }

                if (GenXmlFunctions.GetField(rpData, "importproperties") == "True")
                {
                    ImportRecord(xmlFile, "GROUP");
                    ImportRecord(xmlFile, "GROUPLANG");
                }

                if (GenXmlFunctions.GetField(rpData, "importsettings") == "True")
                {
                    ImportRecord(xmlFile, "SETTINGS");
                }

                if (GenXmlFunctions.GetField(rpData, "importorders") == "True")
                {
                    ImportRecord(xmlFile, "ORDER");
                }
            }
        }

        private void DoImportImages(NBrightInfo nbi)
        {
            var fname = StoreSettings.Current.FolderUploadsMapPath + "\\" + nbi.GetXmlProperty("genxml/hidden/hidimagefile");
            if (System.IO.File.Exists(fname)) DnnUtils.UnZip(fname, StoreSettings.Current.FolderImagesMapPath);
            Utils.DeleteSysFile(fname);
        }

        private void DoImportDocs(NBrightInfo nbi)
        {
            var fname = StoreSettings.Current.FolderUploadsMapPath + "\\" + nbi.GetXmlProperty("genxml/hidden/hiddocsfile");
            if (System.IO.File.Exists(fname)) DnnUtils.UnZip(fname, StoreSettings.Current.FolderDocumentsMapPath);
            Utils.DeleteSysFile(fname);
        }

        private void ImportRecord(XmlDataDocument xmlFile, String typeCode, Boolean updaterecordsbyref = true)
        {
            var nodList = xmlFile.SelectNodes("root/item[./typecode='" + typeCode + "']");
            if (nodList != null)
            {
                foreach (XmlNode nod in nodList)
                {
                    var update = true;
                    var nbi = new NBrightInfo();
                    nbi.FromXmlItem(nod.OuterXml);
                    var olditemid = nbi.ItemID;
                    
                    // check to see if we have a new record or updating a existing one, use the ref field to find out.
                    nbi.ItemID = -1;
                    if (typeCode == "PRD" && updaterecordsbyref)
                    {
                        var itemref = nbi.GetXmlProperty("genxml/textbox/txtproductref");
                        if (itemref != "")
                        {
                            var l = ModCtrl.GetList(PortalId, -1, "PRD", " and NB3.ProductRef = '" + itemref + "' ");
                            if (l.Count > 0) nbi.ItemID = l[0].ItemID;
                        }
                    }
                    if (typeCode == "PRDLANG" && updaterecordsbyref)
                    {
                        var l = ModCtrl.GetList(PortalId, -1, "PRDLANG", " and NB1.parentitemid = '" + _recordXref[nbi.ParentItemId].ToString("") + "' and NB1.Lang = '" + nbi.Lang + "'");
                        if (l.Count > 0) nbi.ItemID = l[0].ItemID;
                    }
                    if (typeCode == "CATEGORY" && updaterecordsbyref)
                    {
                        var itemref = nbi.GetXmlProperty("genxml/textbox/txtcategoryref");
                        if (itemref != "")
                        {
                            var l = ModCtrl.GetList(PortalId, -1, "CATEGORY", " and [XMLData].value('(genxml/textbox/txtcategoryref)[1]','nvarchar(max)') = '" + itemref + "' ");
                            if (l.Count > 0) nbi.ItemID = l[0].ItemID;
                        }
                    }
                    if (typeCode == "CATEGORYLANG" && updaterecordsbyref)
                    {
                        var l = ModCtrl.GetList(PortalId, -1, "CATEGORYLANG", " and NB1.parentitemid = '" + _recordXref[nbi.ParentItemId].ToString("") + "' and NB1.Lang = '" + nbi.Lang + "'");
                        if (l.Count > 0) nbi.ItemID = l[0].ItemID;
                    }
                    if (typeCode == "GROUP" && updaterecordsbyref)
                    {
                        var itemref = nbi.GetXmlProperty("genxml/textbox/groupref");
                        if (itemref != "")
                        {
                            var l = ModCtrl.GetList(PortalId, -1, "GROUP", " and [XMLData].value('(genxml/textbox/groupref)[1]','nvarchar(max)') = '" + itemref + "' ");
                            if (l.Count > 0) nbi.ItemID = l[0].ItemID;
                        }
                    }
                    if (typeCode == "GROUPLANG" && updaterecordsbyref)
                    {
                        var l = ModCtrl.GetList(PortalId, -1, "GROUPLANG", " and NB1.parentitemid = '" + _recordXref[nbi.ParentItemId].ToString("") + "' and NB1.Lang = '" + nbi.Lang + "'");
                        if (l.Count > 0) nbi.ItemID = l[0].ItemID;
                    }
                    if (typeCode == "SETTINGS") // the setting exported are only the portal settings, not module.  So always update and don;t create new.
                    {
                        var l = ModCtrl.GetList(PortalId, 0, "SETTINGS", " and NB1.GUIDKey = 'NBrightBuySettings' ");
                        if (l.Count > 0) nbi.ItemID = l[0].ItemID;
                    }
                    //NOTE: if ORDERS are imported, we expect those to ALWAYS be new records, we don't want to delete any validate orders in this import.

                    nbi.PortalId = PortalId;

                    // NOTE: we expect the records to be done in typecode order so we know parent and xref itemids.

                    // Get new parentitemid  
                    if (_recordXref.ContainsKey(nbi.ParentItemId)) nbi.ParentItemId = _recordXref[nbi.ParentItemId];
                    // Get new xrefitemid  
                    if (_recordXref.ContainsKey(nbi.XrefItemId)) nbi.XrefItemId = _recordXref[nbi.XrefItemId];
                    // if we have a xref record update the guidkey
                    var newitemid = -1;
                    if (nbi.ParentItemId > 0 && nbi.XrefItemId > 0)
                    {
                        nbi.GUIDKey = nbi.XrefItemId.ToString("") + "x" + nbi.ParentItemId.ToString("");
                        //if we already have a record with this xref guid then we don;t need to update
                        var c = ModCtrl.GetListCount(PortalId, -1, "", " and NB1.ParentItemId = '" + nbi.ParentItemId.ToString("") + "' and NB1.XrefItemId = '" + nbi.XrefItemId.ToString("") + "' ");
                        if (c > 0) update = false;
                    }

                    if (update)
                    {
                        newitemid = ModCtrl.Update(nbi);
                        if (typeCode == "PRD")
                        { // if product then validate the data.
                            var prodData = new ProductData(newitemid, Utils.GetCurrentCulture());
                            prodData.Validate();
                            prodData.Save();
                        }
                    }
                    if (newitemid > 0) _recordXref.Add(olditemid, newitemid);
                }                
            }
        }
    }

}
