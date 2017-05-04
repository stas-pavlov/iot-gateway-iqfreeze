using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTIngress
{


    class IoTConnector
    {
        static DeviceClient deviceClient;
        static string iotHubUri = "<your-iot-hub-uri>";
        static string deviceKey = "<your-device-key>";
        public static string deviceID = "iQFreeze_Test_Device";

        public IoTConnector()
        {
            deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceID, deviceKey), TransportType.Mqtt); }
    


        public async void SendDeviceToCloudMessagesAsync(string telemetryData)
        {
            var telemetryDataPoint = new
            {
                dateTime = DateTime.Now,
                deviceId = deviceID,
                telemetry = telemetryData
            };
            var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
            var message = new Message(Encoding.ASCII.GetBytes(messageString));

            await deviceClient.SendEventAsync(message);
        }
    }
}
