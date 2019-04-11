using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
//htt p://example.com/
class Program
{
    static void Main(string[] args)
    {

        TcpListener listener = new TcpListener(IPAddress.Any, 8001);
        listener.Start();

        while (true)
        {
            Socket Client = listener.AcceptSocket();
            ThreadPool.QueueUserWorkItem(ProxySocket, Client);
        }
    }

    
    private static Regex RegexHost = new Regex(@"(Host:\s)(\S+)");
    private static Regex RegexHTTPAnswer = new Regex(@"(HTTP/1.1\s)(\S+\s)(\S+)");

    static void ProxySocket(object request)
    {
        try
        {
            string requestString = string.Empty;
            int bytesReceived;
            int bytesSended;
            byte[] buffer;
            byte[] byteOriginalRequest;

            Socket socketClient = (Socket)request;

            buffer = new byte[60000];

            bytesReceived = socketClient.Receive(buffer, 0, buffer.Length, SocketFlags.None);
            while (socketClient.Available > 0)
            {
                bytesReceived = socketClient.Receive(buffer, 0, buffer.Length, SocketFlags.None);
            }

            byteOriginalRequest = buffer;
            requestString = Encoding.ASCII.GetString(byteOriginalRequest);

            string Host = "";

            Match MatchHost = RegexHost.Match(requestString);
            if (MatchHost.Success)
            {
                //Console.WriteLine(RequestString);
                Host = MatchHost.Groups[2].Value;
            }
            

            IPAddress[] ipAddress = Dns.GetHostAddresses(Host);
            IPEndPoint endPoint = new IPEndPoint(ipAddress[0], 80);

            Socket SocketProxy = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            {
                SocketProxy.Connect(endPoint);

                bytesSended = SocketProxy.Send(byteOriginalRequest, byteOriginalRequest.Length, SocketFlags.None);
                try
                {
                    
                        bytesReceived = SocketProxy.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                byte[] FinalResponse = buffer;
                string stringFinalResponse = Encoding.ASCII.GetString(FinalResponse);

                string Answer = "";
                Match MatchHTTPAnswer = RegexHTTPAnswer.Match(stringFinalResponse);
                Answer = MatchHTTPAnswer.Groups[2].Value + MatchHTTPAnswer.Groups[3].Value;
                if (MatchHTTPAnswer.Success)
                {
                    Console.WriteLine(" URL {0} Answer code {1}", Host, Answer);
                    //Console.WriteLine(stringFinalResponse);
                }
                bytesSended = socketClient.Send(FinalResponse, FinalResponse.Length, SocketFlags.None);
                SocketProxy.Shutdown(SocketShutdown.Send);
                SocketProxy.Close();
            }
            socketClient.Shutdown(SocketShutdown.Send);
            socketClient.Close();
        }
        catch (Exception ex)
        {
        }
    }
    
}