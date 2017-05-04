using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure;

namespace IoTIngress
{
    class AzureStorageClient
    {
        static private CloudStorageAccount storageAccount;
       
        static private CloudTable cloudTable;
        static private CloudTableClient tableClient;

        public AzureStorageClient()
        {
            storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("AzureStorageConnectionString"));
            
            tableClient = storageAccount.CreateCloudTableClient();
            cloudTable = tableClient.GetTableReference("iQFreezDataNow");
            cloudTable.CreateIfNotExists();
        }

        private class TelemetryEntity : TableEntity
        {
            public TelemetryEntity(DateTime time, string deviceID)
            {
                this.PartitionKey = time.ToFileTimeUtc().ToString();
                this.RowKey = deviceID;
            }

            public TelemetryEntity() { }

            public string Telemetry { get; set; }
            
        }

        public async void AddRecord(DateTime time, string deviceID, string message)
        {
            
            TelemetryEntity telemetryRecord = new TelemetryEntity(time, deviceID);
            telemetryRecord.Telemetry = message;
                        
            TableOperation insertOperation = TableOperation.Insert(telemetryRecord);
                        
            await cloudTable.ExecuteAsync(insertOperation);            
        }

    }
}
