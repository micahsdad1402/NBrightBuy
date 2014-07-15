﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using System.Xml;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Security.Membership;
using DotNetNuke.Services.FileSystem;
using NBrightCore.common;
using NBrightCore.render;
using NBrightDNN;
using NEvoWeb.Modules.NB_Store;

namespace Nevoweb.DNN.NBrightBuy.Components
{
    public class ClientData
    {
        private NBrightInfo _clientInfo;
        public int PortalId;
        private UserInfo _userInfo;

        public ClientData(int portalId, int userid)
        {
            PortalId = portalId;
            PopulateClientData(userid);
        }


        #region "base methods"


        /// <summary>
        /// Get Client Cart
        /// </summary>
        /// <returns></returns>
        public NBrightInfo GetInfo()
        {
            return _clientInfo;
        }

        public void AddClientRole()
        {
            if (_userInfo != null)
            {
                if (!_userInfo.IsInRole("Client"))
                {
                    var rc = new DotNetNuke.Security.Roles.RoleController();
                    var ri = rc.GetRoleByName(PortalId, "Client");
                    if (ri != null) rc.AddUserRole(PortalId, _userInfo.UserID, ri.RoleID, Null.NullDate);
                }
            }
        }

        public void ResetPassword()
        {
            if (_userInfo != null)
            {
                UserController.ResetPassword(_userInfo, "");
                _userInfo.Membership.UpdatePassword = true;
                UserController.UpdateUser(PortalSettings.Current.PortalId, _userInfo);
                DotNetNuke.Services.Mail.Mail.SendMail(_userInfo, DotNetNuke.Services.Mail.MessageType.PasswordReminder, (PortalSettings)HttpContext.Current.Items["PortalSettings"]);
            }
        }

        public void UpdateEmail(String email)
        {
            if (_userInfo != null && Utils.IsEmail(email))
            {
                _userInfo.Email = email;
                UserController.UpdateUser(PortalSettings.Current.PortalId, _userInfo);
            }
        }

        public void UnlockUser()
        {
            if (_userInfo != null) UserController.UnLockUser(_userInfo);
        }

        public void AuthoriseClient()
        {
            if (_userInfo != null)
            {
                _userInfo.Membership.Approved = true;
                UserController.UpdateUser(PortalSettings.Current.PortalId, _userInfo);
            }
        }

        public void OutputDebugFile(String fileName)
        {
            _clientInfo.XMLDoc.Save(PortalSettings.Current.HomeDirectoryMapPath + fileName);
        }

    #endregion

        #region "private methods/functions"

        private void PopulateClientData(int userId)
        {
            _clientInfo = new NBrightInfo(true);
            _clientInfo.ItemID = userId;
            _clientInfo.PortalId = PortalId;

            _userInfo = UserController.GetUserById(PortalId, userId);

            _clientInfo.ModifiedDate = _userInfo.Membership.CreatedDate;

            foreach (var propertyInfo in _userInfo.GetType().GetProperties())
            {
                if (propertyInfo.CanRead)
                {
                    var pv = propertyInfo.GetValue(_userInfo, null);
                    _clientInfo.SetXmlProperty("genxml/textbox/" + propertyInfo.Name.ToLower(), pv.ToString());
                }
            }

            foreach (DotNetNuke.Entities.Profile.ProfilePropertyDefinition p in _userInfo.Profile.ProfileProperties)
            {
                _clientInfo.SetXmlProperty("genxml/textbox/" + p.PropertyName.ToLower(), p.PropertyValue);
            }

            _clientInfo.AddSingleNode("membership","","genxml");
            foreach (var propertyInfo in _userInfo.Membership.GetType().GetProperties())
            {
                if (propertyInfo.CanRead)
                {
                    var pv = propertyInfo.GetValue(_userInfo.Membership, null);
                    if (pv != null) _clientInfo.SetXmlProperty("genxml/membership/" + propertyInfo.Name.ToLower(), pv.ToString());
                }
            }

        }


        #endregion


    }
}
