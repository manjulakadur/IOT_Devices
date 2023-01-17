using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Common.Exceptions;
using Newtonsoft.Json;

namespace IoTApplicationManjula
{
    class Program
    {
        static RegistryManager registryManager;
        private static DeviceClient s_deviceClient;
        static DeviceClient Client = null;
        static string DeviceConnectionString = "HostName=manjula-iot-hub.azure-devices.net;SharedAccessKeyName=ServiceRegistry;SharedAccessKey=sXPSNoFE8BFD4fDxnmme9q10JSq8nMv7nzQTo19+D9U=";

        private static async Task AddDeviceAsync(string deviceId)
        {
            //string deviceId = "myFirstdevice";
            Device device;
            try
            {
                Console.WriteLine("MyDevice :");
                for (int i = 0; i < 10; i++)
                {
                    string newDeviceId = deviceId + i;

                    device = await registryManager.AddDeviceAsync(new Device(newDeviceId));
                    Console.WriteLine("Created device :{0}" , newDeviceId);

                }
                // device = await registryManager.AddDeviceAsync(new Device(deviceId));
            }
            catch (DeviceAlreadyExistsException)
            {
                Console.WriteLine("Already existing device:");
                device = await registryManager.GetDeviceAsync(deviceId);
            }
            //Console.WriteLine("Generated device key: {0}", device.Authentication.SymmetricKey.PrimaryKey);
        }
        private static async Task GetAllDevices()
        {
            try
            {
                Console.WriteLine("Get all the devices");
                //[System.Obsolete('"Use CreateQuery("select * from devices", pageSize);"')];
                var devices = await registryManager.GetDevicesAsync(1000);

                foreach (var device in devices)
                {
                    Console.WriteLine($"Id {device.Id} - Status {device.Status} - Reason {device.StatusReason}");
                }

            }
            catch (Exception)
            {

                throw;
            }
        }
        private async static Task UpdateDeviceAsync()
        {
            try
            {
                Console.WriteLine("Update device - Enter Device Name");
                var deviceName = Console.ReadLine();

                var d = await registryManager.GetDeviceAsync(deviceName);

                if (d != null)
                {
                    d.Status = DeviceStatus.Disabled;
                    d.StatusReason = "Disabled for test";

                    var dd = await registryManager.UpdateDeviceAsync(d);
                }
                else
                {
                    Console.WriteLine("Device not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception {ex.Message}");
            }
        }

        private async static Task RemoveDeviceAsync()
        {
            try
            {
                Console.WriteLine("Remove device- Enter DeviceName to remove");
                var deviceName = Console.ReadLine();

                await registryManager.RemoveDeviceAsync(deviceName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception {ex.Message}");
            }
        }
        public static async Task DeviceOperation()
        {
            registryManager = RegistryManager.CreateFromConnectionString(DeviceConnectionString);
            string inputValue = string.Empty;
            Console.WriteLine("enter 1 to addDevice ,2 to ReadDevices ,3 to UpdateDevice and 4 to DeleteDevice",inputValue);
            inputValue= Console.ReadLine().ToString();

            switch (inputValue)
            {
                case "1":
                    string deviceId = "device";                    
                    await AddDeviceAsync(deviceId);
                    break;
                case "2":
                    await GetAllDevices();
                    break;
                case "3":
                    await UpdateDeviceAsync();
                    break;
                case "4":
                    await RemoveDeviceAsync();
                    break;
                default:
                    break;

            }
        }
        //o	Sending telemetry messages from device to IoT Hub  
        private static async void SendDeviceToCloudMessagesAsync(DeviceClient s_deviceClient)
    {
        try
        {
            double minTemperature = 20;
            double minHumidity = 60;
            Random rand = new Random();

            while (true)
            {
                double currentTemperature = minTemperature + rand.NextDouble() * 15;
                double currentHumidity = minHumidity + rand.NextDouble() * 20;

                // Create JSON message  

                var telemetryDataPoint = new
                {

                    temperature = currentTemperature,
                    humidity = currentHumidity
                };

                string messageString = "";



                messageString = JsonConvert.SerializeObject(telemetryDataPoint);

                var message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(messageString));

                // Add a custom application property to the message.  
                // An IoT hub can filter on these properties without access to the message body.  
                //message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");  

                // Send the telemetry message  
                await s_deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);
                await Task.Delay(1000 * 10);

            }
        }
        catch (Exception ex)
        {

            throw ex;
        }
    }

        public static async Task AddTagsAndQuery()
        {
            Console.WriteLine("Updating desired properties -Enter device name");
            var myDeviceId = Console.ReadLine();
            var twin = await registryManager.GetTwinAsync(myDeviceId);
            var patch =
                @"{
            tags: {
                location: {
                    region: 'US',
                    plant: 'Redmond43'
                }
            }
        }";
            await registryManager.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag);

            var query = registryManager.CreateQuery(
              "SELECT * FROM devices WHERE tags.location.plant = 'Redmond43'", 100);
            var twinsInRedmond43 = await query.GetNextAsTwinAsync();
            Console.WriteLine("Devices in Redmond43: {0}",
              string.Join(", ", twinsInRedmond43.Select(t => t.DeviceId)));

            query = registryManager.CreateQuery("SELECT * FROM devices WHERE tags.location.plant = 'Redmond43' AND properties.reported.connectivity.type = 'cellular'", 100);
            var twinsInRedmond43UsingCellular = await query.GetNextAsTwinAsync();
            Console.WriteLine("Devices in Redmond43 using cellular network: {0}",
              string.Join(", ", twinsInRedmond43UsingCellular.Select(t => t.DeviceId)));
            Console.WriteLine("Desired Properties updated");
        }

        //reported properties
        public static async void InitClient()
        {
            try
            {
                Console.WriteLine("Updating Reported Properties");
                Console.WriteLine("Enter Device name");
                var myDeviceId = Console.ReadLine();
                Client = DeviceClient.CreateFromConnectionString(DeviceConnectionString,myDeviceId,
                  Microsoft.Azure.Devices.Client.TransportType.Mqtt);
                Console.WriteLine("Retrieving twin");
                await Client.GetTwinAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }
        public static async void ReportConnectivity()
        {
            try
            {
                Console.WriteLine("Sending connectivity data as reported property");

                TwinCollection reportedProperties, connectivity;
                reportedProperties = new TwinCollection();
                connectivity = new TwinCollection();
                connectivity["type"] = "cellular";
                reportedProperties["connectivity"] = connectivity;
                await Client.UpdateReportedPropertiesAsync(reportedProperties);
                await Task.Delay(2000);
                Console.WriteLine("Reprted Properties Updated");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        static void Main(string[] args)
        {
            DeviceOperation().Wait();
            //desired properties
            AddTagsAndQuery().Wait();
            //Reported properties
            InitClient();
            ReportConnectivity();

            //tremetry message            
            Console.WriteLine("Enter device name");
            var d = Console.ReadLine();
            s_deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString,d, Microsoft.Azure.Devices.Client.TransportType.Mqtt);
            SendDeviceToCloudMessagesAsync(s_deviceClient);
            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }
    }
}
