using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using lawsoncs.htg.sdtd.AdminServer.objects;
using lawsoncs.htg.sdtd.ServerCommandBase;
using lawsoncs.htg.sdtd.data;
using lawsoncs.htg.sdtd.data.objects;
using lawsoncs.MEFLibrary.MEF;

namespace lawsoncs.htg.sdtd.AdminServer
{
    internal class WorkerClass
    {
        private readonly CancellationTokenSource _tokenSource;

        private static string commandFolder = AppDomain.CurrentDomain.BaseDirectory + @"\Commands";

        private CancellationToken _token;
        private Task _workerTask;

        private readonly ConcurrentQueue<string> _incomingMessageConcurrentQueue;

        private Task _telnetTask; 

        private readonly StateObject _state;

        private MEFLoader _mfLoader;
        private ICollection<CommandBase> foundCmds;

        public WorkerClass()
        { 
            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;
            _incomingMessageConcurrentQueue = new ConcurrentQueue<string>(); 
            _state = new StateObject();
        }

        public void Start()
        {
            try
            {
                _mfLoader = new MEFLoader();
                foundCmds = _mfLoader.LoadByType<CommandBase>(commandFolder);

                if (foundCmds == null || foundCmds.Count == 0) throw new NullReferenceException(string.Format("MEF was unable to load any command classes from {0}", commandFolder));

                _workerTask = Task.Factory.StartNew(Execute, _tokenSource.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Stop()
        {
            _tokenSource.Cancel();

            try
            {
                if (_workerTask != null)
                    _workerTask.Wait();
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    log4net.LogManager.GetLogger("log").Fatal("Worker AggragateExecption", e);
                }
            }
            catch (Exception e)
            {
                log4net.LogManager.GetLogger("log").Fatal("Worker execption", e);
            }
        }

        private void Execute()
        {
            _telnetTask = Task.Factory.StartNew(StartTelnetClient, _tokenSource.Token); 

            Player tempP; //keep this copy so we don't re-create it 

            int? lastGUID = null;

            while (!_token.IsCancellationRequested)
            {
                if (_incomingMessageConcurrentQueue.Count == 0) // if there are no messages to process, wait for 1 sec then try to hit it again
                {
                    Task.Delay(1000).Wait();
                }
                
                string msg="";
                try
                {
                    if (!_incomingMessageConcurrentQueue.TryDequeue(out msg)) continue;


                    if (String.IsNullOrWhiteSpace(msg)) continue;

                    //msg is valid, need to run it through command processors
                           
                }
                catch (Exception e)
                {
                    if (log4net.LogManager.GetLogger("log").IsInfoEnabled)
                        log4net.LogManager.GetLogger("log").Info(string.Format("error working with msg {0}", msg), e);
                }
            }

            _telnetTask.Wait();
        }

        private void StartTelnetClient()
        {
            Socket client = null;
            while (!_token.IsCancellationRequested)
            {
                // Connect to a remote device.
                try
                {
                    // Establish the remote endpoint for the socket.
                    IPAddress ipAddress = IPAddress.Parse(SettingsSingleton.Instance.ServerIP);

                    var remoteEP = new IPEndPoint(ipAddress, SettingsSingleton.Instance.ServerTelnetPort);

                    // Create a TCP/IP socket.
                    client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    // Connect to the remote endpoint.
                    client.Connect(remoteEP);

                    // Send test data to the remote device.
                    Send(client, SettingsSingleton.Instance.ServerAdminPassword + "\n"); 

                    while (!_token.IsCancellationRequested)
                    {
                        try
                        {
                            // Receive the response from the remote device.
                            Receive(client);

                            int concurrentCmdsRun = 0; //this will allow for us to break out after we run a set number of commands

                            //check for any cmds that need to be executed
                            while (ServerStatusSingleton.Instance.PendingCommandsToRun.Count > 0)
                            {
                                if (concurrentCmdsRun > SettingsSingleton.Instance.MaxCommandsToProcessConcurrently) break;

                                string cmd;

                                if (!ServerStatusSingleton.Instance.PendingCommandsToRun.TryDequeue(out cmd)) break;
                                if (cmd == null) continue;

                                Send(client, cmd + "\n");

                                concurrentCmdsRun++;
                            }
                        }
                        catch (Exception ex)//the rx or ex loop failed, don't worry carry on but log the error
                        {
                            if (log4net.LogManager.GetLogger("log").IsErrorEnabled)
                                log4net.LogManager.GetLogger("log").Error("Exception in rx/ex loop", ex);
                        }
                    }

                    //// Write the response to the console.
                    //Console.WriteLine("Response received : {0}", response);

                    // Release the socket.
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());

                    if (log4net.LogManager.GetLogger("log").IsFatalEnabled)
                        log4net.LogManager.GetLogger("log").Fatal("Exception in telnetClient", e);
                }
                finally
                {
                    try
                    {
                        if (client != null)
                        {
                            client.Shutdown(SocketShutdown.Both);
                            client.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        if (log4net.LogManager.GetLogger("log").IsWarnEnabled)
                            log4net.LogManager.GetLogger("log").Warn("problem with continuing the telnet", e);
                    }
                }
            }
        }
        
        private void Receive(Socket client)
        {
            try
            {
                // Begin receiving the data from the remote device.
                int bytesRead = client.Receive(_state.Buffer, 0, StateObject.BufferSize, 0);

                if (bytesRead > 0)
                {
                    //The last read may not have been complete, so add this read onto what ever is stored in state.Sb                    
                    string s =_state.Sb + (Encoding.ASCII.GetString(_state.Buffer, 0, bytesRead));
                    
                    var split = s.Split(new[] {"\r\n"}, StringSplitOptions.None);

                    int splitLength = split.Length;

                    //this means it is not a complete read because mulitlines ends with \r\n, sinle lines will have no \r\n at all, 
                    //so save it off to state.Sb so we can finish reading it next time around
                    if (!s.EndsWith("\r\n")) 
                    {
                        _state.Sb = new StringBuilder();
                        _state.Sb.Append(split[split.Length - 1]); // this holds the left over piece that was cut off from the last read
                        splitLength--;
                    }

                    for (int i = 0; i < splitLength; i++)
                    {
                        _incomingMessageConcurrentQueue.Enqueue(split[i]);
                    } 
                }
                else
                {
                    // All the data has arrived; put it in response.
                    if (_state.Sb.Length > 1)
                    {
                        _incomingMessageConcurrentQueue.Enqueue(_state.Sb.ToString());
                    } 
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            client.Send(byteData, 0, byteData.Length, 0);
        }
    }

    // State object for receiving data from remote device.
    public class StateObject
    { 
        // Size of receive buffer.
        public const int BufferSize = 512;
        // Receive buffer.
        public readonly byte[] Buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder Sb = new StringBuilder();
    }
}
