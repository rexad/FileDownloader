﻿using System;
using System.Collections.Generic;
using AgodaFileDownloader.Model;
using AgodaFileDownloader.Service.ServiceInterface;

namespace AgodaFileDownloader.Service
{
    public class ResourceDetail
    {
        #region Fields

        private string _url;
        
        private IProtocolDownloader _provider;
        #endregion

        #region Constructor

        public static IList<ResourceDetail> FromListUrl(IList<string> urls,IList<AuthenticatedUrl> urlWithAuthentification)
        {
            List<ResourceDetail> result = new List<ResourceDetail>();

            foreach (string t in urls)
            {
                result.Add(FromURL(t));
            }

            foreach (AuthenticatedUrl t in urlWithAuthentification)
            {
                result.Add(FromURL(t.Url,true,t.Login,t.Password));
            }
            return result;
        }

        private static ResourceDetail FromURL(string url)
        {
            ResourceDetail rl = new ResourceDetail();
            rl.Url = url;
            return rl;
        }

        

        private static ResourceDetail FromURL(string url,bool authenticate,string login,string password)
        {
            ResourceDetail rl = new ResourceDetail();
            rl.Url = url;
            rl.Authenticate = authenticate;
            rl.Login = login;
            rl.Password = password;
            return rl;
        }

        #endregion

        #region Properties

        public string Url
        {
            get { return _url; }
            set
            {
                _url = value;
                BindProtocolProviderType();
            }
        }
        public Type ProtocolType { get; set; }
        public bool Authenticate { get; set; }

        public string Login { get; set; }

        public string Password { get; set; }

        public string Protocol
        {
            get
            {
                if (ProtocolType == null)
                {
                    return null;
                }
                return ProtocolType.AssemblyQualifiedName;
            }
            set
            {
                if (value == null)
                {
                    BindProtocolProviderType();
                }
                else
                {
                    ProtocolType = Type.GetType(value);
                }
            }
        }

        #endregion

        #region Methods

        public void BindProtocolProviderType()
        {

            if (!string.IsNullOrEmpty(this.Url))
            {
                ProtocolType = ProtocolProviderFactory.GetProtocolType(this.Url);
            }
        }

        /*
        public IProtocolDownloader BindProtocolProviderInstance()
        {
            if (ProtocolType == null)
            {
                BindProtocolProviderType();
            }

            if (_provider == null)
            {
                _provider = ProtocolProviderFactory.CreateProvider(ProtocolType);
            }

            return _provider;
        }*/
        #endregion
    }
}
