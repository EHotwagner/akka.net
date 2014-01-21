﻿using Pigeon.Actor;
using Pigeon.Remote;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


//[assembly: PreApplicationStartMethod(typeof(XSocketsBootstrap), "Start")]

namespace Pigeon.Remote
{    
    public class RemoteHost 
    {
        private ActorSystem system;
        private int port;
        TcpListener server;
        public static RemoteHost StartHost(ActorSystem system,int port)
        {
            var host = new RemoteHost(system,port);
                           
            return host;
        }

        public RemoteHost(ActorSystem system,int port)
        {
            try
            {
                this.system = system;
                this.port = port;


                server = TcpListener.Create(port);
                server.ExclusiveAddressUse = false;
                server.AllowNatTraversal(true);
                server.Start(100);

                WaitForClient();
            }
            catch (Exception x)
            {
                Console.WriteLine(x);
            }
        }

        private async void WaitForClient()
        {
            
            var client = await server.AcceptTcpClientAsync();
            ProcessSocket(client);
            await Task.Yield();
            WaitForClient();
        }

        private void ProcessSocket(TcpClient client)
        {
            var stream = client.GetStream();
            while (client.Connected)
            {
                var remoteEnvelope = RemoteEnvelope.ParseDelimitedFrom(stream);
                var serializedMessage = remoteEnvelope.Message;
                var json = serializedMessage.Message.ToString(Encoding.Default);
                var message = fastJSON.JSON.Instance.ToObject(json);
                var recipient = remoteEnvelope.Recipient.ToActorRef(this.system);
                var sender =  remoteEnvelope.Sender.ToActorRef(this.system);

                recipient.Tell(message, sender);
            }
        }
    }

    public static class ProtoExtensions
    {
        public static ActorRef ToActorRef(this ActorRefData self,ActorSystem system)
        {
            var path = new ActorPath(self.Path);
            
            var actor = system.ActorSelection(path);
            return actor;
        }
    }

    //public class ActorHub : Hub
    //{
    //    private ActorSystem system;

    //    public ActorHub(ActorSystem system)
    //    {
    //        this.system = system;
    //    }
    //    public void Post(string remoteActorName, string actorName, string data,string typeName)
    //    {
    //        var type = Type.GetType(typeName);
    //        var message = (object)JsonConvert.DeserializeObject(data, type);
    //        var actor = system.Guardian.Cell.Child(actorName);
    //        var connectionId = this.Context.ConnectionId;
    //        var remoteActor = new SignalRResponseActorRef(remoteActorName, this, connectionId,actor);
    //        actor.Tell(message, remoteActor);
    //    }

    //    private class SignalRResponseActorRef : ActorRef
    //    {
    //        private string remoteActorName;
    //        private Hub hub;
    //        private string connectionId;
    //        public SignalRResponseActorRef(string remoteActorName, Hub hub, string connectionId,ActorRef owner)
    //        {
    //            this.remoteActorName = remoteActorName;
    //            this.hub = hub;
    //            this.connectionId = connectionId;
    //        }

    //        protected override void TellInternal(object message, ActorRef sender)
    //        {
    //            var data = JsonConvert.SerializeObject(message);
    //            hub.Clients.Client(connectionId).Reply(remoteActorName, data, message.GetType().AssemblyQualifiedName);
    //        }
    //    }
    //}
}