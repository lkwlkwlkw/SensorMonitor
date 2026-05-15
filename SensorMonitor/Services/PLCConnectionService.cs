using Microsoft.Extensions.Options;
using System.Diagnostics;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;

namespace SensorMonitor.Services
{
    public class PLCConnectionService
    {
        private readonly AppSettings _settings;
        private ApplicationDescription clientDescription;
        private ClientSessionChannel channel;
        public event Action OnDataReceived;
        public event Action<string> ConnectionStatusChanged;

        private float[] _Temperature = new float[12];
        public float[] Temperature { get => _Temperature; }

        private float[] _Pressure = new float[2];
        public float[] Pressure { get => _Pressure; }

        private float _Weight;
        public float Weight { get => _Weight; }
        public bool IsConnected { get => channel != null && channel.State == CommunicationState.Opened; }



        public PLCConnectionService(IOptionsMonitor<AppSettings> options)
        {
            _settings = options.CurrentValue;

        }

        private async Task Connect()
        {
            clientDescription = new ApplicationDescription
            {
                ApplicationName = "Workstation.UaClient.FeatureTests",
                ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:Workstation.UaClient.FeatureTests",
                ApplicationType = ApplicationType.Client
            };

            channel = new ClientSessionChannel(
                clientDescription,
                null,
                new AnonymousIdentity(),
                _settings.ConnectionAddress,
                SecurityPolicyUris.None);

            try
            {
                await channel.OpenAsync();
                ConnectionStatusChanged?.Invoke($"Połączono z PLC: {channel.RemoteEndpoint.EndpointUrl}");
                Debug.WriteLine($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
                Debug.WriteLine($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
                Debug.WriteLine($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
                Debug.WriteLine($"UserIdentityToken: '{channel.UserIdentity}'.");

                StartReadingData();
            }
            catch (Exception ex)
            {
                await channel.AbortAsync();
                Debug.WriteLine(ex.Message);
                ConnectionStatusChanged?.Invoke($"Brak połączenia z PLC: {ex.Message}");
                await Connect();
            }
        }

        private async Task Read()
        {
            var readRequest = new ReadRequest
            {
                NodesToRead = new[] {
                    new ReadValueId {
                        NodeId = NodeId.Parse(_settings.NodeIds.T1),
                        AttributeId = AttributeIds.Value
                    },
                    new ReadValueId {
                        NodeId = NodeId.Parse(_settings.NodeIds.T2),
                        AttributeId = AttributeIds.Value
                    },
                    new ReadValueId {
                        NodeId = NodeId.Parse(_settings.NodeIds.T3),
                        AttributeId = AttributeIds.Value
                    },
                    new ReadValueId {
                        NodeId = NodeId.Parse(_settings.NodeIds.T4),
                        AttributeId = AttributeIds.Value
                    },
                    new ReadValueId {
                        NodeId = NodeId.Parse(_settings.NodeIds.T5),
                        AttributeId = AttributeIds.Value
                    },
                    new ReadValueId {
                        NodeId = NodeId.Parse(_settings.NodeIds.T6),
                        AttributeId = AttributeIds.Value
                    },
                    new ReadValueId {
                        NodeId = NodeId.Parse(_settings.NodeIds.T7),
                        AttributeId = AttributeIds.Value
                    },

                    new ReadValueId {
                        NodeId = NodeId.Parse(_settings.NodeIds.T8),
                        AttributeId = AttributeIds.Value
                    },

                    new ReadValueId {
                        NodeId = NodeId.Parse(_settings.NodeIds.T9),
                        AttributeId = AttributeIds.Value
                    },

                    new ReadValueId {
                        NodeId = NodeId.Parse(_settings.NodeIds.T10),
                        AttributeId = AttributeIds.Value
                    },

                    new ReadValueId {
                        NodeId = NodeId.Parse(_settings.NodeIds.T11),
                        AttributeId = AttributeIds.Value
                    },

                    new ReadValueId {
                        NodeId = NodeId.Parse(_settings.NodeIds.T12),
                        AttributeId = AttributeIds.Value
                    },




                     new ReadValueId {
                        NodeId = NodeId.Parse(_settings.NodeIds.P1),
                        AttributeId = AttributeIds.Value
                    },

                      new ReadValueId {
                        NodeId = NodeId.Parse(_settings.NodeIds.P2),
                        AttributeId = AttributeIds.Value
                    },

                       new ReadValueId {
                        NodeId = NodeId.Parse(_settings.NodeIds.W1),
                        AttributeId = AttributeIds.Value
                    },

                }
            };

            while (true)
            {
                try
                {
                    var readResult = await channel!.ReadAsync(readRequest);

                    for (int i = 0; i < 12; i++)
                    {
                        _Temperature[i] = (float)(readResult.Results[i].Value ?? 0.0);
                    }

                    _Pressure[0] = (float)(readResult.Results[12].Value ?? 0.0);
                    _Pressure[1] = (float)(readResult.Results[13].Value ?? 0.0);
                    _Weight = (float)(readResult.Results[14].Value ?? 0.0);

                    OnDataReceived?.Invoke();



                    await Task.Delay(_settings.PLCPollingInterval * 1000);// Opóźnienie między kolejnymi odczytami danych z PLC
                }
                catch (Exception ex)
                {
                    ConnectionStatusChanged?.Invoke($"Problem z połączeniem: {ex.Message}");
                    await channel!.AbortAsync(); //???????????????????
                    await channel!.CloseAsync();
                    await Connect();
                    break;
                }
            }
        }

        public async void ConnectPLC()
        {
            await Connect();
        }

        public async void StartReadingData()
        {
            await Read();
        }

        public void DisconnectPLC()
        {
            if (channel is not null)
            {
                channel.AbortAsync();
            }
        }


    }
}


