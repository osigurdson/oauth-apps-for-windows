using System;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace OAuthConsoleApp
{
   public class GoogleOAuth
   {
      private const string AuthorizationUri = "https://accounts.google.com/o/oauth2/v2/auth";
      private const string TokenRequestUri = "https://www.googleapis.com/oauth2/v4/token";
      private const string UserInfoUri = "https://www.googleapis.com/oauth2/v3/userinfo";
      private readonly string _clientId;
      private readonly string _clientSecret;

      public GoogleOAuth(string clientId, string clientSecret)
      {
         _clientId = clientId;_clientSecret = clientSecret;
      }

      public async Task<string> GetUserInfoAsync()
      {
         var state = GetRandomBase64(32);
         var codeVerifier = GetRandomBase64(32);
         var codeChallenge = ComputeHash(codeVerifier);

         using (var webServer = RedirectHttpListener.Start(state))
         {
            var url = BuildAuthorizationUrl(webServer.RedirectUri, state, codeChallenge);
            var browser = Process.Start(url);
            var accessCode =  await webServer.GetAccessCodeAsync();
            var tokens = await GetAccessTokenInfoAsync(accessCode, codeVerifier, webServer.RedirectUri);

            Console.WriteLine("--- ID Token ---");
            Console.WriteLine(tokens.IdToken);

            Console.WriteLine("--- Refresh Token ---");
            Console.WriteLine(tokens.RefreshToken);
            return await GetUserInfoAsync(tokens.AccessToken);
         }
      }

      private async Task<TokenInfo> GetAccessTokenInfoAsync(string accessCode, string codeVerifier, string redirectUri)
      {
         var tokenRequestBody =
             string.Format("code={0}&redirect_uri={1}&client_id={2}&code_verifier={3}&client_secret={4}&scope=&grant_type=authorization_code",
             accessCode,
             Uri.EscapeDataString(redirectUri),
             _clientId,
             codeVerifier,
             _clientSecret
             );

         var tokenRequest = (HttpWebRequest)WebRequest.Create(TokenRequestUri);
         tokenRequest.Method = "POST";
         tokenRequest.ContentType = "application/x-www-form-urlencoded";
         tokenRequest.Accept = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
         var bytes = Encoding.ASCII.GetBytes(tokenRequestBody);
         tokenRequest.ContentLength = bytes.Length;
         using (var stream = tokenRequest.GetRequestStream())
         {
            await stream.WriteAsync(bytes, 0, bytes.Length);
         }

         var response = await tokenRequest.GetResponseAsync();
         using (var reader = new StreamReader(response.GetResponseStream()))
         {
            var text = await reader.ReadToEndAsync();
            var tokens = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
            var accessToken = tokens["access_token"];

            return new TokenInfo(tokens["access_token"], tokens["id_token"], tokens["refresh_token"]);
         }
      }

      private async Task<string> GetUserInfoAsync(string accessToken)
      { 
         // sends the request
         HttpWebRequest userinfoRequest = (HttpWebRequest)WebRequest.Create(UserInfoUri);
         userinfoRequest.Method = "GET";
         userinfoRequest.Headers.Add(string.Format("Authorization: Bearer {0}", accessToken));
         userinfoRequest.ContentType = "application/x-www-form-urlencoded";
         userinfoRequest.Accept = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

         // gets the response
         var userinfoResponse = await userinfoRequest.GetResponseAsync();
         using (StreamReader userinfoResponseReader = new StreamReader(userinfoResponse.GetResponseStream()))
         {
            // reads response body
            string userinfoResponseText = await userinfoResponseReader.ReadToEndAsync();
            return userinfoResponseText;
         }
      }


      private string BuildAuthorizationUrl(string redirectUrl, string state, string codeChallenge)
      {
         var url =
            string.Format("{0}?response_type=code&scope=openid%20profile&redirect_uri={1}&client_id={2}&state={3}&code_challenge={4}&code_challenge_method={5}",
            AuthorizationUri,
            Uri.EscapeDataString(redirectUrl),
            _clientId,
            state,
            codeChallenge,
            "S256"
            );

         return url;
      }

      private static string GetRandomBase64(int length)
      {
         var rng = new RNGCryptoServiceProvider();
         var bytes = new byte[length];
         rng.GetBytes(bytes);
         return ToBase64Url(bytes);
      }

      private static string ToBase64Url(byte[] bytes)
      {
         return Convert
             .ToBase64String(bytes)
             .Replace("+", "-")
             .Replace("/", "_")
             .Replace("=", "");
      }

      private string ComputeHash(string s)
      {
         var sha = new SHA256Managed();
         var bytes = Encoding.UTF8.GetBytes(s);
         var hashBytes = sha.ComputeHash(bytes);
         return ToBase64Url(hashBytes);
      }

      public class TokenInfo
      {
         public string AccessToken { get; }
         public string IdToken { get; }
         public string RefreshToken { get; }

         public TokenInfo(string accessToken, string idToken, string refreshToken)
         {
            AccessToken = accessToken;
            IdToken = idToken;
            RefreshToken = refreshToken;
         }
      }
   }
}
