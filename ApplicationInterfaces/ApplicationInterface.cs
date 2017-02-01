﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SteamBotLite
{
    
    public class ChatroomEntity
    {
        public enum AdminStatus { Unknown, Other, False, True};
        public ApplicationInterface Application;
        public object identifier;
        public AdminStatus Rank;
        public string DisplayName;
        public object ExtraData;

        public ChatroomEntity(object identifier, ApplicationInterface Application, string DisplayName = "", AdminStatus Rank = AdminStatus.Unknown , object ParentIdentifier = null, object ExtraData = null)
        {
            this.identifier = identifier;
            this.Rank = Rank;
            this.Application = Application;
            this.DisplayName = DisplayName;

            if (ParentIdentifier != null) {
                this.ParentIdentifier = ParentIdentifier;
                IsChild = true;
            }
            else {
                this.ParentIdentifier = null;
                IsChild = false;
            }

            this.ExtraData = ExtraData;
            
        }
        
        
        
        public object ParentIdentifier;
        bool IsChild;
    }

    public class Chatroom : ChatroomEntity {
        public ChatroomEntity(object identifier, ApplicationInterface Application, string DisplayName = "", AdminStatus Rank = AdminStatus.Unknown, object ParentIdentifier = null, object ExtraData = null)
        {
        };
    public class User : ChatroomEntity {
    };

    public class MessageEventArgs : EventArgs
    {
        public ChatroomEntity Sender;
        public ChatroomEntity Destination;
        public ChatroomEntity Chatroom;
        public string ReceivedMessage;
        public string ReplyMessage;
        public ApplicationInterface InterfaceHandlerDestination;    
        public MessageEventArgs (ApplicationInterface interfacehandlerdestination)
        {
            InterfaceHandlerDestination = interfacehandlerdestination;
        }
    }

        public abstract class ApplicationInterface
    {

        public List<ChatroomEntity> MainChatroomsCollection;
        public Dictionary<string, object> config;

        public List<string> Whitelist;
        public List<string> Blacklist;
        bool WhitelistOnly;

        public ApplicationInterface()
        {
            this.config = JsonConvert.DeserializeObject<Dictionary<string, object>>(System.IO.File.ReadAllText(Path.Combine("applicationconfigs" , this.GetType().Name.ToString() + ".json")));
        
            Whitelist = JsonConvert.DeserializeObject <List<string>> (config["Whitelist"].ToString());
            Blacklist = JsonConvert.DeserializeObject<List<string>>(config["BlackList"].ToString());
            WhitelistOnly = bool.Parse(config["WhitelistOnly"].ToString());
        }

        public bool CheckEntryValid (string entry)
        {
            if (WhitelistOnly) {
                if (Whitelist.Contains(entry)) {
                    return true;
                }
                else {
                    return false;
                }
            }
            else if (Blacklist.Contains(entry)) {
                return false;
            }

            return true; 
        }


        public abstract void SendChatRoomMessage(object sender, MessageEventArgs messagedata);
        public abstract void SendPrivateMessage(object sender, MessageEventArgs messagedata);
        public abstract void BroadCastMessage(object sender, string message);


        public void AssignUserHandler(UserHandler userhandler)
        {
            userhandler.ChatRoomJoin += EnterChatRoom;
            userhandler.ChatRoomLeave += LeaveChatroom;
            userhandler.SendChatRoomMessageEvent += SendChatRoomMessage;
            userhandler.SendPrivateMessageEvent += SendPrivateMessage;
            userhandler.SetUsernameEvent += SetUsername;
            userhandler.RebootEvent += Reboot;
            userhandler.BroadcastMessageEvent += BroadCastMessage;
            userhandler.MainChatRoomJoin += JoinAllChatrooms;
            userhandler.MainChatRoomLeave += LeaveAllChatrooms;
            userhandler.ChatMemberInfoEvent += ChatMemberInfoEvent;
            userhandler.SetStatusmessage += SetStatusMessage;
        }


        /* I beleive there is no benefit to making this role mandatory
        public abstract void ReceiveChatRoomMessage(ChatroomEntity ChatroomEntity, string Message);
        public abstract void ReceivePrivateMessage(ChatroomEntity ChatroomEntity, string Message);
        */

        public class ChatMemberInfoEventArgs
        {
            ChatroomEntity User;
            bool IsAdmin;
        }

        public event EventHandler<MessageEventArgs> ChatRoomMessageEvent;
        
        //The event-invoking method that derived classes can override.
        protected virtual void ChatRoomMessageProcessEvent(MessageEventArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<MessageEventArgs> handler = ChatRoomMessageEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler<MessageEventArgs> PrivateMessageEvent;

        //The event-invoking method that derived classes can override.
        protected virtual void PrivateMessageProcessEvent(MessageEventArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<MessageEventArgs> handler = PrivateMessageEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        

        public event EventHandler<Tuple<ChatroomEntity, bool>> ChatMemberInfoEvent;
        //The event-invoking method that derived classes can override.
        protected virtual void ChatMemberInfoProcessEvent(ChatroomEntity e , bool isadmin)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<Tuple<ChatroomEntity, bool>> handler = ChatMemberInfoEvent;
            if (handler != null)
            {
                handler(this, new Tuple<ChatroomEntity, bool>(e,isadmin));
            }
        }

        public event EventHandler AnnounceLoginCompletedEvent;

        protected virtual void AnnounceLoginCompleted()
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler handler = AnnounceLoginCompletedEvent;
            if (handler != null)
            {
                AnnounceLoginCompletedEvent(this , null);
            }
        }

        public event EventHandler<string> SetStatusMessageEvent;

        public abstract void SetStatusMessage(object sender, string message);

        public abstract void ReceiveChatMemberInfo(ChatroomEntity ChatroomEntity, bool AdminStatus);

        public abstract void EnterChatRoom (object sender, ChatroomEntity ChatroomEntity);
        public abstract void LeaveChatroom (object sender, ChatroomEntity ChatroomEntity);

        
        public void JoinAllChatrooms(object sender, EventArgs e)
        {
            foreach(ChatroomEntity entry in MainChatroomsCollection)
            {
                EnterChatRoom(sender, entry);
            }
        }

        public void LeaveAllChatrooms(object sender, EventArgs e)
        {
            foreach (ChatroomEntity entry in MainChatroomsCollection)
            {
                LeaveChatroom(sender, entry);
            }
        }

        public abstract void Reboot(object sender, EventArgs e);

        public abstract void SetUsername(object sender, string Username);
        public abstract string GetUsername();

        public abstract string GetOthersUsername(object sender, ChatroomEntity user);

        public abstract void tick();

        public enum TickThreadState { Running , Stopped};

        public TickThreadState TickThread = TickThreadState.Running;

        public void StartTickThreadLoop()
        {
            while (TickThread == TickThreadState.Running)
            {
                tick();
            }
        }

        public string Username
        {
            get
            {
                return GetUsername();
            }

            set
            {
               SetUsername(this, value);
            }
        }
    }
}
