using BlueApps.MaterialFlow.Common.Connection.Broker;
using BlueApps.MaterialFlow.Common.Connection.Client;
using BlueApps.MaterialFlow.Common.Connection.PacketHelper;
using BlueApps.MaterialFlow.Common.Machines.BaseMachines;
using BlueApps.MaterialFlow.Common.Models.Types;
using BlueApps.MaterialFlow.Common.Values.Types;
using MF152004.Workerservice.Common;
using MF152004.Workerservice.Connection.Packets.PacketHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MF152004.Workerservice.ComTest
{
    internal class Program
    {
        private static IConfigurationRoot _config;
        private static ILogger _logger;
        private static MqttClient _client;
        private static MqttBroker _broker;

        static async Task Main(string[] args)
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            using var factory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            _logger = factory.CreateLogger<Program>();

            CreateTopics();

            while(true)
            {
                Console.WriteLine("Start the program by clicking <A> on your keyboard:");
                Console.WriteLine("For EXIT = <Ctrl + C> or close the window");
                Console.WriteLine();

                var key = Console.ReadKey().Key;

                if (key == ConsoleKey.A)
                {
                    await StartPLCCommunication();
                }
            }
        }

        private static void CreateTopics()
        {
            CommonData.Topics = new Dictionary<TopicType, string>
            {
                { TopicType.PLC_Workerservice, _config["PLC_To_Workerservice"] ?? "MaterialFlow/plc/workerservice" },
                { TopicType.WebService_Workerservice, _config["Webservice_To_Workerservice"] ?? "MaterialFlow/webservice/workerservice" },
                { TopicType.WebService_Workerservice_Config, _config["Webservice_To_Workerservice_Config"] ?? "MaterialFlow/webservice/workerservice/config" },
                { TopicType.Workerservice_Webservice, _config["Workerservice_To_Webservice"] ?? "MaterialFlow/workerservice/webservice" },
                { TopicType.Workerservice_PLC, _config["Workerservice_To_PLC"] ?? "MaterialFlow/workerservice/plc" },
            };
        }

        private static async Task StartPLCCommunication()
        {
            await StartBrokerAndClient();

            _client.OnReceivingMessage += OnReceivingMessage;
            PLC152004_PacketHelper helper = new PLC152004_PacketHelper();

            _logger.LogInformation($"Topic to subscribe: {_config["PLC_To_Workerservice"]}");

            var directions = Enum.GetValues(typeof(Direction));
            var rnd = new Random();

            while (true)
            {
                await Task.Delay(new Random().Next(4000, 10000));
                helper.Create_FlowSortPosition(new FlowSort()
                {
                    BasePosition = $"{rnd.Next(1, 12)}.{new Random().Next(1, 6)}.{new Random().Next(1, 100)}",
                    DriveDirection = (Direction)directions.GetValue(new Random().Next(directions.Length)),
                    Name = "TestDiverter",
                }, rnd.Next(1, 999));

                _logger.LogInformation($"A message will be send now ({TimeOnly.FromDateTime(DateTime.Now).ToLongTimeString()})");

                _client.SendData(helper.GetPacketData());
            }
        }

        private static void OnReceivingMessage(object? sender, BlueApps.MaterialFlow.Common.Connection.Packets.Events.MessagePacketEventArgs e)
        {
            var time = TimeOnly.FromDateTime(DateTime.Now).ToLongTimeString();

            PLC152004_PacketHelper packetHelper = new PLC152004_PacketHelper();
            packetHelper.SetPacketData(e.Message);

            _logger.LogInformation($"New message has been received at {time}. MSG: " + string.Join(";", packetHelper.Areas));
        }

        private static async Task StartBrokerAndClient()
        {
            using var factory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            _broker = new MqttBroker(factory.CreateLogger<MqttBroker>(), _config);
            await _broker.RunBrokerAsync();

            _client = new MqttClient(factory.CreateLogger<MqttClient>(), _config);
            _client.AddTopics(CommonData.Topics
                .Where(x => x.Key == TopicType.PLC_Workerservice)
                .Select(x => x.Value).ToArray());
            await _client.Connect();
        }
    }
}