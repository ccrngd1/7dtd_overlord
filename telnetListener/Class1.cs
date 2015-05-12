using System;

using System.Net.Sockets;

using System.Net;

using System.IO;

using System.Diagnostics;

 

namespace SimpleTelnetServerSample

{
     class AsyncRedirect

     {

         readonly byte[] buf = new byte[4096];

         readonly Stream r, w;

         readonly AsyncCallback AsyncCallback_;

         public AsyncRedirect(Stream Read, Stream Write) { r = Read; w = Write; AsyncCallback_ = this.AsyncCallback; }

         void AsyncCallback(IAsyncResult ar)

         {

             if (!ar.IsCompleted) return;

             int n = 0;

             try { n = r.EndRead(ar); }

             catch (Exception e) {

                  Console.WriteLine("EndRead failed:{0}", e);

             }

             if (n > 0)

             {

                 w.Write(buf, 0, n);

                 w.Flush();

                 BeginRead();

             }

             else

             {

                 Console.WriteLine("read 0 bytes,finished");

                 w.Close();

             }

         }

         public IAsyncResult BeginRead()

         {

             return r.BeginRead(buf, 0, buf.Length, AsyncCallback_, null);

         }

         //static void Main(string[] args)
        private void Main()
         {

             var psi = new ProcessStartInfo("cmd.exe");

             psi.RedirectStandardInput = psi.RedirectStandardOutput = true;

             psi.UseShellExecute = false;

             var tcpListener = new TcpListener(IPAddress.Any, 233);

             tcpListener.Start();

             while (true)

             {

                 var tcpClient = tcpListener.AcceptTcpClient();

                 var clientStream = tcpClient.GetStream();

                 tcpClient.Client.BeginSend(System.Text.Encoding.ASCII.GetBytes("test"),0, "test".Length, SocketFlags.Broadcast, null, null);

                 //var Pro = new AsyncRedirect(p.StandardOutput.BaseStream, clientStream);

                 //var Tcp = new AsyncRedirect(clientStream, p.StandardInput.BaseStream);

                 //Pro.BeginRead();

                 //Tcp.BeginRead();

             }

         }

     };

}

 

 
