﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;
using System.IO;
using Newtonsoft.Json;

namespace SteamBotLite
{
    class Program
    {
        
        static void Main(string[] args)
        {
            //Create userHandlers//
            List<UserHandler> UserHandlers = new List<UserHandler>();

            ConsoleUserHandler consolehandler = new ConsoleUserHandler();
            VBot VbotHandler = new VBot();

            // Create Interfaces//
            List<ApplicationInterface> Bots = new List<ApplicationInterface>();

            SteamAccountVBot SteamPlatformInterface = new SteamAccountVBot();
            DiscordAccountVFun DiscordPlatformInterfaceFun = new DiscordAccountVFun();
            DiscordAccountVBot DiscordPlatformInterfaceRelay = new DiscordAccountVBot();

            Bots.Add(SteamPlatformInterface);
            Bots.Add(DiscordPlatformInterfaceRelay);
            Bots.Add(DiscordPlatformInterfaceFun);

            //Link userhandlers and classes that are two way//
            AssignConnection(VbotHandler, DiscordPlatformInterfaceRelay);
            AssignConnection(VbotHandler, DiscordPlatformInterfaceFun);
            AssignConnection(VbotHandler, SteamPlatformInterface);
            AssignConnection(consolehandler, DiscordPlatformInterfaceRelay);
            AssignConnection(consolehandler, SteamPlatformInterface);
            
            //Start looping and iterating//
            bool Running = true;
            while (Running)
            {
                foreach (ApplicationInterface bot in Bots)
                {
                    bot.tick();
                }
                System.Threading.Thread.Sleep(100);
            }
        }

        public static void AssignConnection (UserHandler userhandler , ApplicationInterface applicationinterface)
        {
            userhandler.AssignAppInterface(applicationinterface);
            applicationinterface.AssignUserHandler(userhandler);
        }
    }
}
    