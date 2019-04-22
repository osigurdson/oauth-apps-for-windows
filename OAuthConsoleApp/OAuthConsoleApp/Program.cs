// Copyright 2016 Google Inc.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace OAuthConsoleApp
{
   class Program
   {
      static void Main(string[] args)
      {
         AsyncMain().GetAwaiter().GetResult();
      }

      private static async Task AsyncMain()
      {
         Console.WriteLine("+-----------------------+");
         Console.WriteLine("|  Sign in with Google  |");
         Console.WriteLine("+-----------------------+");
         Console.WriteLine("");
         Console.WriteLine("Press any key to sign in...");
         Console.ReadKey();

         var oauth = new GoogleOAuth(
            clientId: "581786658708-elflankerquo1a6vsckabbhn25hclla0.apps.googleusercontent.com",
            clientSecret: "3f6NggMbPtrmIBpgx-MK2xXK"
         );
            

           var userInfo = await oauth.GetUserInfoAsync();

         Console.WriteLine(userInfo);
      }
   }
}
