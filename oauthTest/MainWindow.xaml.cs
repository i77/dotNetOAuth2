using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.IO;

namespace oauthTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        OAuth2Client oa2client = new OAuth2Client();
        AccessToken savedToken = null;

        public MainWindow()
        {
            InitializeComponent();
            oa2client.Authorized += new EventHandler(oa2client_Authorized);
        }

        private void Authorize_Click(object sender, RoutedEventArgs e)
        {
            oa2client.AuthorizationEndpoint = "https://auth.photosi.com/oauth2/authorize/";
            oa2client.TokenEndpoint = "https://auth.photosi.com/oauth2/token/";

            oa2client.ClientId = "c1044bb10aac85c35ba65cce87578d";
            oa2client.ClientSecret = "6741fc500559c29cc9b8f56dde1ac5";

            oa2client.AuthorizeWithWebBrowser(authBrowser, "http://localhost/");
        }

        void oa2client_Authorized(object sender, EventArgs e)
        {
            authBrowser.Visibility = System.Windows.Visibility.Hidden;

            oa2client.ObtainAccesToken();
            savedToken = oa2client.Token;

            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create("https://auth.photosi.com/api/user_profile");
            oa2client.SignHTTPRequest(ref httpRequest);
                                
            WebResponse wr = httpRequest.GetResponse();

            StreamReader stream = new StreamReader(wr.GetResponseStream());
            string text = stream.ReadToEnd();
            MessageBox.Show(text);
        }

        void CallAPI_Click(object sender, RoutedEventArgs e)
        {
            OAuth2Client client = new OAuth2Client();
            oa2client.AuthorizationEndpoint = "https://auth.photosi.com/oauth2/authorize/";
            oa2client.TokenEndpoint = "https://auth.photosi.com/oauth2/token/";
            oa2client.ClientId = "c1044bb10aac85c35ba65cce87578d";
            oa2client.ClientSecret = "6741fc500559c29cc9b8f56dde1ac5";

            oa2client.Token = savedToken;

            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create("https://auth.photosi.com/api/user_profile");
            oa2client.SignHTTPRequest(ref httpRequest);

            //token could be refreshed
            savedToken = oa2client.Token;

            WebResponse wr = httpRequest.GetResponse();

            StreamReader stream = new StreamReader(wr.GetResponseStream());
            string text = stream.ReadToEnd();
            MessageBox.Show(text);
        }
    }
}
