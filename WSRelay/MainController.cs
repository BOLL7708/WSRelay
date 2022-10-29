using BOLL7708;
using SuperSocket.WebSocket.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BOLL7708.SuperServer;

namespace WSRelay
{
    internal class MainController
    {
        SuperServer _server = new SuperServer();
        ConcurrentDictionary<string, string> _channels = new();

        public Action<ServerStatus, int> StatusAction = (status, count) => { };

        public MainController() {
            _server.StatusAction += (status, count) =>
            {
                Debug.WriteLine(Enum.GetName(typeof(ServerStatus), status));
                StatusAction.Invoke(status, count);

            };
            _server.StatusMessageAction += (WebSocketSession? session, bool status, string message) => {
                if(!status) _channels.TryRemove(session?.SessionID ?? "", out string? oldSessionID);
            };
            _server.MessageReceivedAction += (WebSocketSession session, string message) =>
            {
                var hasChannel = _channels.TryGetValue(session.SessionID, out string? channel);
                if(!hasChannel && message.StartsWith("CHANNEL:"))
                {
                    var messageArr = message.Split(':');
                    if (messageArr.Length == 2) channel = messageArr[1].Trim();
                    if(channel != null && channel.Length > 0) _channels[session.SessionID] = channel;
                }
                Debug.WriteLine(message);
                if(channel != null && channel.Length > 0)
                {
                    List<string> sessionIDs = new();
                    foreach(string key in _channels.Keys)
                    {
                        if (_channels[key] == channel && key != session.SessionID)
                        {
                            sessionIDs.Add(key);
                        }
                    }
                    _server.SendMessageToGroup(sessionIDs.ToArray(), message);
                } else
                {
                    _server.SendMessage(session, "ERROR: Not connected to any channel");
                }
            };
        }

        public void StartOrRestartServer(int port) {
            _server.Start(port);
        }
    }
}
