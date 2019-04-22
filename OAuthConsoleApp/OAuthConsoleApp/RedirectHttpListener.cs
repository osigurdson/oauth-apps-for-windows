using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OAuthConsoleApp
{
   public class RedirectHttpListener : IDisposable
   {
      private readonly HttpListener _httpListener;
      private readonly string _expectedState;

      public string RedirectUri { get; }

      private RedirectHttpListener(string redirectUri, HttpListener httpListener, string expectedState)
      {
         RedirectUri = redirectUri;
         _httpListener = httpListener;
         _expectedState = expectedState;
      }

      public static RedirectHttpListener Start(string expectedState)
      {
         var listener = new TcpListener(IPAddress.Loopback, 0);
         listener.Start();
         var port = ((IPEndPoint)listener.LocalEndpoint).Port;
         listener.Stop();
         var redirectUri = string.Format("http://{0}:{1}/", IPAddress.Loopback, port);

         var httpListener = new HttpListener();
         httpListener.Prefixes.Add(redirectUri);
         httpListener.Start();

         var result = new RedirectHttpListener(redirectUri, httpListener, expectedState);
         return result;
      }

      public async Task<string> GetAccessCodeAsync()
      {
         var context = await _httpListener.GetContextAsync();
         var response = context.Response;
         var responseString = string.Format("<html><head><meta http-equiv='refresh' content='10;url=https://google.com'></head><body>Please return to the app.</body></html>");
         var buffer = Encoding.UTF8.GetBytes(responseString);
         response.ContentLength64 = buffer.Length;

         await response
            .OutputStream
            .WriteAsync(buffer, 0, buffer.Length);

         response
             .OutputStream
             .Close();

         var state = context.Request.QueryString.Get("state");

         if(state != _expectedState)
         {
            throw new Exception("State not expected"); //TODO throw better exception
         }
         return context.Request.QueryString.Get("code");
      }

      public void Dispose()
      {
         _httpListener.Stop();
      }
   }
}
