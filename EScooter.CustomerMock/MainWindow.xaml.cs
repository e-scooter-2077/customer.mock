using Azure.Messaging.ServiceBus;
using EasyDesk.CleanArchitecture.Application.Events.ExternalEvents;
using EasyDesk.CleanArchitecture.Infrastructure.Events.ServiceBus;
using EasyDesk.CleanArchitecture.Infrastructure.Json;
using EasyDesk.CleanArchitecture.Infrastructure.Time;
using EasyDesk.Tools;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace EScooter.CustomerMock
{
    /// <summary>
    /// An event emitted when a scooter is registered to the system.
    /// </summary>
    public record CustomerCreated(Guid Id)
        : IExternalEvent;

    /// <summary>
    /// An event emitted when a scooter is deleted from the system.
    /// </summary>
    public record CustomerDeleted(Guid Id)
        : IExternalEvent;

    /// <summary>
    /// The main window of the application.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IExternalEventPublisher _publisher;

        /// <summary>
        /// Creates a new instance of the main window.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables("ESCOOTER_")
                .AddUserSecrets(GetType().Assembly)
                .Build();

            var connectionString = config.GetValue<string>("AzureServiceBusSettings:ConnectionString");
            var settings = new AzureServiceBusSettings
            {
                BasePath = "dev",
                ConnectionString = connectionString,
                TopicName = "service-events"
            };
            var client = new ServiceBusClient(connectionString);
            var eventBusPublisher = new AzureServiceBusPublisher(client, settings);
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            };
            var serializer = new NewtonsoftJsonSerializer(serializerSettings);

            _publisher = new ExternalEventPublisher(eventBusPublisher, new MachineDateTime(), serializer);
        }

        private async Task UseCustomerId(AsyncAction<Guid> action)
        {
            if (!Guid.TryParse(_txtCustomerId.Text, out var id))
            {
                LogError("Invalid Customer ID");
                return;
            }
            await action(id);
        }

        private void LogError(string message) => Log("ERROR", message);

        private void LogInfo(string message) => Log("INFO", message);

        private void Log(string prefix, string message)
        {
            var formattedMessage = $"[{DateTime.Now}] {prefix} - {message}";
            _txtLog.Text += "\n" + formattedMessage;
        }

        private async Task Publish(IExternalEvent ev)
        {
            try
            {
                await _publisher.Publish(ev);
                LogInfo($"Published event {ev}");
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async void CreateClicked(object sender, RoutedEventArgs e)
        {
            await UseCustomerId(id => Publish(new CustomerCreated(id)));
        }

        private async void CreateRandomClicked(object sender, RoutedEventArgs e)
        {
            await Publish(new CustomerCreated(Guid.NewGuid()));
        }

        private async void DeleteClicked(object sender, RoutedEventArgs e)
        {
            await UseCustomerId(id => Publish(new CustomerDeleted(id)));
        }
    }
}
