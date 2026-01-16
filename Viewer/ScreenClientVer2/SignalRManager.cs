using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenClientVer2
{
    // This manager wraps HubConnection and exposes events for UI
    public class SignalRManager
    {
        private HubConnection? hub;
        private string hubUrl;

        public SignalRManager(string hubUrl)
        {
            this.hubUrl = hubUrl;
        }

        public bool IsConnected => hub?.State == HubConnectionState.Connected;

        // events
        public event Action<IEnumerable<string>>? ClientListUpdated;
        public event Action<string, string>? ReceiveScreenshot; // machine, base64

        public async Task ConnectAsync()
        {
            if (hub != null) return;

            hub = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            // handlers from server
            hub.On<IEnumerable<string>>("ClientListUpdated", clients =>
            {
                ClientListUpdated?.Invoke(clients);
            });

            // single receiver used for both per-machine and lab broadcasts
            hub.On<string, string>("ReceiveScreenshot", (machine, base64) =>
            {
                ReceiveScreenshot?.Invoke(machine, base64);
            });

            hub.Closed += async (error) =>
            {
                // log and let auto reconnect handle it; callers can observe IsConnected
                Console.WriteLine("SignalR: connection closed.");
                await Task.Delay(3000);
            };

            hub.Reconnected += async (id) =>
            {
                Console.WriteLine("SignalR: reconnected, requesting client list.");
                try
                {
                    // Refresh clients after reconnect
                    var clients = await hub.InvokeAsync<IEnumerable<string>>("GetConnectedClients");
                    ClientListUpdated?.Invoke(clients);
                }
                catch { }
            };

            await hub.StartAsync();
            // optional: request current client list immediately
            var current = await hub.InvokeAsync<IEnumerable<string>>("GetConnectedClients");
            ClientListUpdated?.Invoke(current);
        }

        public async void SendRemoteInput(string machine, RemoteInputCommand cmd)
        {
            if (hub == null || hub.State != HubConnectionState.Connected)
                return;

            await hub.InvokeAsync("SendRemoteInput", machine, cmd);
        }

        public async Task SubscribeLab(string lab)
        {
            if (hub == null) throw new InvalidOperationException("Not connected");
            await hub.InvokeAsync("SubscribeLab", lab);
        }

        public async Task UnsubscribeLab(string lab)
        {
            if (hub == null) throw new InvalidOperationException("Not connected");
            await hub.InvokeAsync("UnsubscribeLab", lab);
        }

        // If you want per-machine subscribe (keep for compatibility)
        public async Task SubscribeMachine(string machine)
        {
            if (hub == null) throw new InvalidOperationException("Not connected");
            await hub.InvokeAsync("Subscribe", machine);
        }

        public async Task UnsubscribeMachine(string machine)
        {
            if (hub == null) throw new InvalidOperationException("Not connected");
            await hub.InvokeAsync("Unsubscribe", machine);
        }
    }
}
