﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Web;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Net;
using System.IO;
using System.Windows.Navigation;

namespace oauthTest
{
    public class OAuth2Client
    {

        string _code = null;
        string _authEndpoint = null;
        string _tokenEndpoint = null;
        string _redirect = null;
        NavigatingCancelEventHandler _navHandler = null;
         
        AccessToken _accessToken = null;

        /// <summary>
        /// The client is succesfully authorized
        /// </summary>
        public event EventHandler Authorized;

        /// <summary>
        /// oAuth2 client ID
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// oAuth2 client secret
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Access token endpoint
        /// </summary>
        public string TokenEndpoint
        {
            get 
            {
                return _tokenEndpoint;
            }
            set
            {
                _tokenEndpoint = _fixEndpoint(value);
            }
        }

        /// <summary>
        /// Authorization endpoint
        /// </summary>
        public string AuthorizationEndpoint 
        {
            get
            {
                return _authEndpoint;
            }
            set
            {
                _authEndpoint = _fixEndpoint(value);
            }
        }

        /// <summary>
        /// Client is already authorized
        /// </summary>
        public bool IsAuthorized
        {
            get
            {
                return (_code != null);
            }
        }

        /// <summary>
        /// Access token is valid (not expired)?
        /// </summary>
        public bool IsTokenValid
        {
            get
            {
                return (_accessToken != null && (DateTime.Compare(DateTime.Now, _accessToken.TokenDate.AddSeconds(_accessToken.ExpiresIn)) < 0));
            }
        }

        /// <summary>
        /// Access token is refreshable?
        /// </summary>
        public bool IsTokenRefreshable
        {
            get
            {
                return (_accessToken != null && _accessToken.RefreshToken != null);
            }
        }

        /// <summary>
        /// Get/set access token
        /// </summary>
        public AccessToken Token
        {
            get
            {
                return _accessToken;
            }
            set
            {
                _accessToken = value;
            }
        }

        /// <summary>
        /// Authorize client using a <see cref="WebBrowser"/> control
        /// </summary>
        /// <param name="browser">An instantiated <see cref="WebBrowser"/></param>
        /// <param name="redirect">oAuth2 redirect param</param>
        public void AuthorizeWithWebBrowser(WebBrowser browser, string redirect)
        {
            //show browser window
            if (_navHandler != null) browser.Navigating -= _navHandler;
            _navHandler = delegate(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
            {
                if (e.Uri != null)
                {
                    string authCode = HttpUtility.ParseQueryString(e.Uri.Query)["code"];
                    if (authCode != null)
                    {
                        e.Cancel = true;
                        _code = authCode;
                        _redirect = redirect;
                        if (this.Authorized != null) this.Authorized.Invoke(this, null);
                    }
                }
            };
            browser.Navigating += _navHandler;
            browser.Navigate(String.Format("{0}?response_type=code&client_id={1}&redirect_uri={2}", _authEndpoint, this.ClientId, HttpUtility.UrlEncode(redirect)));
        }

        /// <summary>
        /// Obtain first access token after authorization
        /// </summary>
        public void ObtainAccesToken()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(String.Format("{0}?grant_type=authorization_code&client_id={1}&redirect_uri={2}&code={3}", _tokenEndpoint, ClientId, HttpUtility.UrlEncode(_redirect), _code));
            request.Headers["Authorization"] = _getClientHeader();
            DateTime tokenReqestDate = DateTime.Now;
            WebResponse response = request.GetResponse();

            _accessToken = _deserializeToken(response.GetResponseStream());
            _accessToken.TokenDate = tokenReqestDate;
        }

        /// <summary>
        /// Refresh expired access token
        /// </summary>
        public void RefreshAccessToken()
        {
            if (!IsTokenValid && IsTokenRefreshable)
            {
                //refreshable token needs to refresh
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(String.Format("{0}?grant_type=refresh_token&client_id={1}refresh_token={2}", _tokenEndpoint, ClientId, _accessToken.RefreshToken));
                request.Headers["Authorization"] = _getClientHeader();
                DateTime tokenReqestDate = DateTime.Now;
                WebResponse response = request.GetResponse();

                _accessToken = _deserializeToken(response.GetResponseStream());
                _accessToken.TokenDate = tokenReqestDate;
            }
        }

        /// <summary>
        /// Sign a <see cref="HttpWebRequest"/> with obtained access token
        /// </summary>
        /// <param name="request">An instantiated <see cref="HttpWebRequest"/></param>
        public void SignHTTPRequest(ref HttpWebRequest request)
        {
            if (_accessToken == null){
                ObtainAccesToken();
            }
            RefreshAccessToken();
            if (_accessToken.TokenType.ToLower() == "bearer"){
                request.Headers["Authorization"] = String.Format("Bearer {0}", _accessToken.Token);
            }else{
                throw new NotImplementedException("Only bearer token type is currently supported.");
            }
        }

        private string _fixEndpoint(string endpoint){
            if (!endpoint.EndsWith("/")){
                endpoint = String.Concat(endpoint,'/');
            }
            return endpoint;
        }

        private string _getClientHeader()
        {
            return String.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes(String.Format("{0}:{1}", ClientId, ClientSecret))));
        }

        private AccessToken _deserializeToken(Stream tokenStream)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(AccessToken));
            AccessToken token = ser.ReadObject(tokenStream) as AccessToken;
            return token;
        }
    }

    [DataContract]
    public class AccessToken
    {
        [DataMember(Name = "access_token")]
        public string Token { private set; get; }
        [DataMember(Name = "token_type")]
        public string TokenType { private set; get; }
        [DataMember(Name = "expires_in")]
        public int ExpiresIn { private set; get; }
        [DataMember(Name = "refresh_token")]
        public string RefreshToken { private set; get; }
        public DateTime TokenDate { set; get; }
    }

}
