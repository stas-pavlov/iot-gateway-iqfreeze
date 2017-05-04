using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System.Net.Sockets;
using System.IO;
using System.Text;

namespace IoTIngress
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        AzureStorageClient azureStorageClient = new AzureStorageClient();
        IoTConnector iotConnector = new IoTConnector();

        IPEndPoint instEndpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["IoTIn"].IPEndpoint; 

        public override void Run()
        {
            Trace.TraceInformation("IoTIngress is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("IoTIngress has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("IoTIngress is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("IoTIngress has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
           
                Trace.TraceInformation("Working");
                await Task.Delay(1000);

                //In the runtime, we get the deployment IP and merging it with the TCP/IP port
                //Stat to listen for data from devices
                TcpListener server = null;
                IPAddress localAddr = IPAddress.Parse(instEndpoint.Address.ToString());

                server = new TcpListener(localAddr, instEndpoint.Port);
                server.Start();

                Trace.TraceInformation("TCP Listener Started");

                //starting to listen for the telemetry by 514 bytes packets. Need to be adjusted
                //according to the protocol (probably, size can be different)
                Byte[] bytes = new Byte[512];
                String data = null;

                while (!cancellationToken.IsCancellationRequested)
                {
                    TcpClient client = server.AcceptTcpClient();

                    data = null;
                    NetworkStream stream = client.GetStream();

                    int i;
                    StreamWriter writer = new StreamWriter(client.GetStream(), Encoding.ASCII);
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {

                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
    
                        azureStorageClient.AddRecord(DateTime.Now, IoTConnector.deviceID, data);
                    
                        //Writing confirmation according to the specification
                        writer.Write(String.Format("DATA ACCEPT={0}", BitConverter.ToString(bytes, 10, 3)));
                        stream.Write(bytes, 10, 1);                        

                    }

                stream.Flush();
                client.Close();            

            }
        }        
        
    }
}
