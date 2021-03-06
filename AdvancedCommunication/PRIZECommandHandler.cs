// Eternal Lands Bot
// Copyright (C) 2006  Artem Makhutov
// artem@makhutov.org
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

using System;

namespace cs_elbot.AdvancedCommunication
{
	/// <summary>
	/// description of PRIZECommandHandler.
	/// </summary>
	public class PRIZECommandHandler
	{
		private TCPWrapper TheTCPWrapper;
		private BasicCommunication.MessageParser TheMessageParser;
		private MySqlManager TheMySqlManager;
		//private bool CommandIsDisabled;
		private HelpCommandHandler TheHelpCommandHandler;
		private Inventory TheInventory;
        private TradeHandler TheTradeHandler;
        private Stats TheStats;
        private bool claimingPrize = false;
        private bool acceptedOnce = false;
        private string username = "";
        private ActorHandler TheActorHandler;
        private Logger TheLogger;
        private int itemCount = 0;
        private int itemsInWindow = 0;
		
		public PRIZECommandHandler(TCPWrapper MyTCPWrapper, BasicCommunication.MessageParser MyMessageParser,HelpCommandHandler MyHelpCommandHandler, MySqlManager MyMySqlManager, Inventory MyInventory, TradeHandler MyTradeHandler, Stats MyStats, Logger MyLogger, ActorHandler MyActorHandler)
		{
			this.TheTCPWrapper = MyTCPWrapper;
			this.TheMessageParser = MyMessageParser;
			this.TheHelpCommandHandler = MyHelpCommandHandler;
			this.TheMySqlManager = MyMySqlManager;
			this.TheInventory = MyInventory;
            this.TheTradeHandler = MyTradeHandler;
            this.TheStats = MyStats;
            this.TheActorHandler = MyActorHandler;
            this.TheLogger = MyLogger;
            this.TheTCPWrapper.GotCommand += new TCPWrapper.GotCommandEventHandler(OnGotCommand);

			//this.CommandIsDisabled = MyMySqlManager.CheckIfCommandIsDisabled("#inv",Settings.botid);
			
			//if (CommandIsDisabled == false)
			{
                if (Settings.IsTradeBot == true && TheMySqlManager.IGamble())
                {
                    TheHelpCommandHandler.AddCommand("#prize - show my prize list");
                    TheHelpCommandHandler.AddCommand("#prizes - null");
                }
                TheMessageParser.Got_PM += new BasicCommunication.MessageParser.Got_PM_EventHandler(OnGotPM);
                this.TheInventory.GotNewInventoryList += new Inventory.GotNewInventoryListEventHandler(OnGotNewInventoryList);
                this.TheMessageParser.Got_AbortTrade += new BasicCommunication.MessageParser.Got_AbortTrade_EventHandler(OnGotAbortTrade);
            }
		}
        private void OnGotNewInventoryList(object sender, Inventory.GotNewInventoryListEventArgs e)
        {
            if (claimingPrize)
            {
                TradeHandler.TradeLogItem MyTradeLogItem = new TradeHandler.TradeLogItem();
                MyTradeLogItem.action = "Prizes claimed";
                MyTradeLogItem.KnownItemsSqlID = 0;
                MyTradeLogItem.price = 0;
                MyTradeLogItem.quantity = 1;
                this.TheMySqlManager.LogTrade(MyTradeLogItem, username, Settings.botid, true);
                TheInventory.inventoryRequested = true;
                TheMySqlManager.updatePrizes(username);
                TheTradeHandler.claimingPrize = false;
                TheTradeHandler.Gambling = false;
                TheTradeHandler.SentThanks = false;
                TheTradeHandler.stopTimer();
                username = "";
                acceptedOnce = false;
                claimingPrize = false;
                itemsInWindow = 0;
                itemCount = 0;
                TheTradeHandler.Trading = false;
                TradeHandler.username = "";
            }
        }
        private void OnGotPM(object sender, BasicCommunication.MessageParser.Got_PM_EventArgs e)
        {
            string Message = e.Message.ToLower().Replace("\'", "\\\'").Replace("\"", "\\\"");


            if (Message[0] != '#')
            {
                Message = "#" + Message;
            }

            string[] CommandArray = Message.Split(' ');
            if (CommandArray[0] != "#prize" && CommandArray[0] != "#prizes")
            {
                return;
            }
            if (claimingPrize || TheTradeHandler.claimingPrize)
            {
                TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "You're already claiming your prize!"));
                TheTCPWrapper.Send(CommandCreator.EXIT_TRADE());
                return;
            }
            claimingPrize = false;
            acceptedOnce = false;

            if (!TheMySqlManager.IGamble())
            {
                TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "I don't gamble, sorry!"));
                return;
            }

            if (Settings.IsTradeBot == false)
            {
                TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "Sorry, I am not a trade bot!"));
                return;
            }

            int rank = TheMySqlManager.GetUserRank(e.username, Settings.botid);
            if (rank < TheMySqlManager.GetCommandRank("#prize", Settings.botid))
            {
                TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "You are not authorized to use this command!"));
                return;
            }

            if (TheMySqlManager.CheckIfBannedGuild( e.username, Settings.botid) < 0)
            {
                TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "You are not authorized to use this command!"));
                return;
            }

            if (this.TheTradeHandler.AmITrading() && e.username != TradeHandler.username)
            {
                TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "I am currently trading, please retry shortly."));
                return;
            }
            if (TheTradeHandler.Gambling)
            {
                TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "You cannot calim prizes if you're already gambling, cancel and reissue the command!"));
                return;
            }
            if (CommandArray.Length != 2)
                goto WrongArguments;

            username = e.username;
            string argument = CommandArray[1];

            switch (argument)
            {
                case "high":
                case "low":
                case "medium":
                    TheMySqlManager.displayPrizes(argument, e.username);
                    break;
                case "claim":
                    if (!acceptedOnce)
                    {
                        Int16 TradePartnerUserID = TheActorHandler.GetUserIDFromname(username);
                        //Console.WriteLine("usernames: " + TradeHandler.username + " " + username);
                        if (!TheTradeHandler.Trading || (TradeHandler.username != username))
                        {
                            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "You must be trading with me first!"));
                            TheTCPWrapper.Send(CommandCreator.TRADE_WITH(TradePartnerUserID));
                            claimingPrize = true;
                        }
                        else if (TheTradeHandler.Trading && itemCount == 0)
                        {
                            TheTradeHandler.claimingPrize = true;
                            claimingPrize = true;
                            itemCount = TheMySqlManager.putPrizesInWindow(username);
                        }
                    }
                    break;
                case "list":
                    TheMySqlManager.listPrizesWon(username);
                    //TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "Not yet implemented, sorry..."));
                    break;
                default:
                    goto WrongArguments;
            }
            return;

        WrongArguments:
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[----------------------------------------"));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[Here is the usage of the #prize command:"));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[#prize <level> list prizes you can win  "));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[#prize <claim> claim your prize         "));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[#prize <list> lists prizes you can claim"));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[----------------------------------------"));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[Example: #prize high                    "));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[Example: #prize medium                  "));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[Example: #prize low                     "));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[Example: #prize claim                   "));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[Example: #prize list                    "));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[----------------------------------------"));
            return;
		}

        private void OnGotCommand(object sender, TCPWrapper.GotCommandEventArgs e)
        {
            if (!claimingPrize)
            {
                return;
            }

            if (e.CommandBuffer[0] == 41) //GET_TRADE_PARTNER_name
            {
                GET_TRADE_PARTNER(e.CommandBuffer);
                return;
            }

            if (e.CommandBuffer[0] == 35) // GET_TRADE_OBJECT
            {
                GET_TRADE_OBJECT(e.CommandBuffer);
                //they shouldn't be putting stuff in the window, just accepting...
                //TheTCPWrapper.Send(CommandCreator.EXIT_TRADE());
                return;
            }

            if (e.CommandBuffer[0] == 37) //if they un-accept, for some reason...
            {
                GET_TRADE_REJECT(e.CommandBuffer);
                return;
            }

            if (e.CommandBuffer[0] == 36)
            {
                GET_TRADE_ACCEPT(e.CommandBuffer);
                return;
            }

            if (e.CommandBuffer[0] == 39)
            {
                //it should never get here, if they put something in the window we're going to cancel the prize claim anyways
                return;
            }
        }
        private void GET_TRADE_OBJECT(byte[] buffer)
        {
            // buffer[11]==1 --> New trade object on the trade partner side
            // buffer[11]==0 --> New trade object on my side
            if (buffer[11] == 1)
            {
                TheTCPWrapper.Send(CommandCreator.EXIT_TRADE());
            }
            else
            {
                itemsInWindow++;
                claimingPrize = true;
                TheTradeHandler.claimingPrize = true;
                if (itemsInWindow == itemCount)
                {
                    //acceptedOnce = true;
                    //TheTCPWrapper.Send(CommandCreator.ACCEPT_TRADE());
                }
            }

        }
        private void GET_TRADE_PARTNER(byte[] buffer)
        {
            int i = System.BitConverter.ToInt16(buffer, 1) - 2;
            string name = System.Text.Encoding.ASCII.GetString(buffer, 4, i);
            if (name != username)
            {
                TheTCPWrapper.Send(CommandCreator.EXIT_TRADE());
            }
            else
            {
                TradeHandler.username = name;
                //Console.WriteLine("item count: " + itemCount);
                //put the items in the window now that we have someone trading with us...
                if (itemCount == 0)
                {
                    claimingPrize = true;
                    TheTradeHandler.claimingPrize = true;
                    itemCount = TheMySqlManager.putPrizesInWindow(username);
                }
            }
        }
        private void OnGotAbortTrade(object sender, System.EventArgs e)
        {
            if ((claimingPrize || TheTradeHandler.claimingPrize))
            {
                username = "";
                claimingPrize = false;
                acceptedOnce = false;
                TheTradeHandler.Trading = false;
                TradeHandler.username = "";
                itemCount = 0;
                TheTradeHandler.claimingPrize = false;
            }
        }
        private void GET_TRADE_ACCEPT(byte[] buffer)
        {
            if (!claimingPrize)
            {
                return;
            }
            //Console.WriteLine(buffer[3] + " accepted: " + acceptedOnce + ": accepted twice: " + acceptedTwice + " :accepted entire: " + acceptedEntire);
            if (buffer[3] == 0)
            {
                return;
            }

            if (buffer[3] == 1) //partner clicks
            {
                if (acceptedOnce)
                {
                    TheTCPWrapper.Send(CommandCreator.ACCEPT_TRADE_ENTIRE(false));
                }
                else
                {
                    TheTCPWrapper.Send(CommandCreator.ACCEPT_TRADE());
                    acceptedOnce = true;
                }

                //if (acceptedTwice)
                //{
                //    TheMySqlManager.updatePrizes(username);
                //    TheTradeHandler.claimingPrize = false;
                //    TheTradeHandler.Gambling = false;
                //    TheTradeHandler.SentThanks = false;
                //    TheTradeHandler.stopTimer();
                //    username = "";
                //    acceptedOnce = false;
                //    acceptedTwice = false;
                //}
                return;
            }
        }
        private void GET_TRADE_REJECT(byte[] buffer)
        {
            if (buffer[3] == 0)
            {
                TheLogger.Debug("RX : ME: GET_TRADE_REJECT\n");
                acceptedOnce = false;
            }
            else
            {
                TheLogger.Debug("RX : PARTNER: GET_TRADE_REJECT\n");
                TheTCPWrapper.Send(CommandCreator.REJECT_TRADE());
            }
        }
    }
}
