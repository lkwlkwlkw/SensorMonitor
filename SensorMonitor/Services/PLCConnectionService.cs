using System.Diagnostics;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;

namespace SensorMonitor.Services
{
    public class PLCConnectionService
    {       
        private ApplicationDescription? clientDescription;
        private ClientSessionChannel? channel;
        public event Action? OnDataReceived;

        
        private Double[] _Temperature  = new Double[12];
        public Double[] Temperature { get => _Temperature; }


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
                "opc.tcp://Precision7530:53530/OPCUA/SimulationServer",
                SecurityPolicyUris.None);

            try
            {
                await channel.OpenAsync();

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
                await Connect();
            }
        }

        private async Task Read()
        {
            var readRequest = new ReadRequest
            {
                NodesToRead = new[] {
                    new ReadValueId {
                        NodeId = NodeId.Parse("ns=3;i=1007"),
                        AttributeId = AttributeIds.Value
                    },
                    new ReadValueId {
                        NodeId = NodeId.Parse("ns=3;i=1008"),
                        AttributeId = AttributeIds.Value
                    },
                    new ReadValueId {
                        NodeId = NodeId.Parse("ns=3;i=1009"),
                        AttributeId = AttributeIds.Value
                    },
                    new ReadValueId {
                        NodeId = NodeId.Parse("ns=3;i=1010"),
                        AttributeId = AttributeIds.Value
                    },
                    new ReadValueId {
                        NodeId = NodeId.Parse("ns=3;i=1011"),
                        AttributeId = AttributeIds.Value
                    },
                    new ReadValueId {
                        NodeId = NodeId.Parse("ns=3;i=1012"),
                        AttributeId = AttributeIds.Value
                    },
                    new ReadValueId {
                        NodeId = NodeId.Parse("ns=3;i=1013"),
                        AttributeId = AttributeIds.Value
                    },

                    new ReadValueId {
                        NodeId = NodeId.Parse("ns=3;i=1014"),
                        AttributeId = AttributeIds.Value
                    },

                    new ReadValueId {
                        NodeId = NodeId.Parse("ns=3;i=1015"),
                        AttributeId = AttributeIds.Value
                    },

                    new ReadValueId {
                        NodeId = NodeId.Parse("ns=3;i=1016"),
                        AttributeId = AttributeIds.Value
                    },

                    new ReadValueId {
                        NodeId = NodeId.Parse("ns=3;i=1017"),
                        AttributeId = AttributeIds.Value
                    },

                    new ReadValueId {
                        NodeId = NodeId.Parse("ns=3;i=1019"),
                        AttributeId = AttributeIds.Value
                    },

                }
            };

            while (true)
            {
                try
                {
                    var readResult = await channel!.ReadAsync(readRequest);
                    

                    for (int i = 0; i < readResult.Results.Length; i++)
                    {
                        _Temperature[i] = (Double)readResult.Results[i].Value;
                    }

                    OnDataReceived?.Invoke();
                      

                    Debug.WriteLine("aaaa" + _Temperature[0].ToString());

                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    await channel!.AbortAsync();
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


