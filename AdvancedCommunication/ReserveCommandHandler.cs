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
	/// description of INVCommandHandler.
	/// </summary>
	public class ReserveCommandHandler
	{
		private TCPWrapper TheTCPWrapper;
		private BasicCommunication.MessageParser TheMessageParser;
		private MySqlManager TheMySqlManager;
		//private bool CommandIsDisabled;
		private HelpCommandHandler TheHelpCommandHandler;
		private Inventory TheInventory;
        private TradeHandler TheTradeHandler;
        private Stats TheStats;
        private Storage TheStorage;
		
		public ReserveCommandHandler(TCPWrapper MyTCPWrapper, BasicCommunication.MessageParser MyMessageParser,HelpCommandHandler MyHelpCommandHandler, MySqlManager MyMySqlManager, Inventory MyInventory, TradeHandler MyTradeHandler, Stats MyStats, Storage MyStorage)
		{
			this.TheTCPWrapper = MyTCPWrapper;
			this.TheMessageParser = MyMessageParser;
			this.TheHelpCommandHandler = MyHelpCommandHandler;
			this.TheMySqlManager = MyMySqlManager;
			this.TheInventory = MyInventory;
            this.TheTradeHandler = MyTradeHandler;
            this.TheStats = MyStats;
            this.TheStorage = MyStorage;

			//this.CommandIsDisabled = MyMySqlManager.CheckIfCommandIsDisabled("#inv",Settings.botid);
			
			//if (CommandIsDisabled == false)
			{
                TheHelpCommandHandler.AddCommand("#reserve - manipulate reserved items");
                TheMessageParser.Got_PM += new BasicCommunication.MessageParser.Got_PM_EventHandler(OnGotPM);
            }
		}

        private void OnGotPM(object sender, BasicCommunication.MessageParser.Got_PM_EventArgs e)
        {
            string Message = e.Message.ToLower().Replace("\'", "\\\'").Replace("\"", "\\\"");
            string[] Inv=new string[64];
            int maxlen = 4;
            bool validCommand = false;

            if (Message[0] != '#')
            {
                Message = "#" + Message;
            }

            string[] CommandArray = Message.Split(' ');
            if (CommandArray[0] == "#reserve" )
            {
                bool disabled = TheMySqlManager.CheckIfCommandIsDisabled("#reserve", Settings.botid);

                string str1 = "", str2 = "";

                if (TheInventory.GettingInventoryItems == true)
                {
                    str2 = "I am building my inventory list, please try again in a few seconds";
                    str1 = str1.PadRight(str2.Length, '=');
                    str1 = "[" + str1;
                    str2 = "[" + str2;
                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, str1));
                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, str2));
                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, str1));

                    return;
                }

                if (disabled == true)
                {
                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "This command is disabled"));
                    return;
                }

                int rank = TheMySqlManager.GetUserRank(e.username, Settings.botid);
                if (rank < TheMySqlManager.GetCommandRank("#reserve", Settings.botid))
                {
                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "You are not authorized to use this command!"));
                    return;
                }

                if (this.TheTradeHandler.AmITrading() && e.username != TradeHandler.username)
                {
                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "I am currently trading, please retry shortly."));
                    return;
                }

                if (CommandArray.Length <= 1)
                    goto WrongArguments;

                if (CommandArray[1] == "withdraw")
                {
                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "Still working on this function..."));
                    validCommand = true;
                }

                if (CommandArray[1] == "details")
                {
                    TheMySqlManager.reservedDetails(Settings.botid, e.username);
                    validCommand = true;
                }

                if (CommandArray[1] == "list")
                {
                    validCommand = true;
                    // list all of the inventory items that have reserved amounts

                    //set up the current inventory list
                    System.Collections.ArrayList MyInventoryList = TheInventory.GetInventoryList();

                    //set up a sorted inventory list for output purposes
                    System.Collections.SortedList TheInventoryList = new System.Collections.SortedList();

                    //loop through the inventory finding reserved amounts
                    foreach (Inventory.inventory_item MyInventoryItem in MyInventoryList)
                    {
                        //find the largest name length for output purposes
                        if (maxlen < MyInventoryItem.name.Length && (MyInventoryItem.pos < 36))
                            maxlen = MyInventoryItem.name.Length;
                        //if it's not in the output array, put it there
                        if (TheInventoryList.Contains(MyInventoryItem.SqlID) && MyInventoryItem.pos < 36)
                        {
                            //add the item quantities (these should be the unstackables...)
                            Inventory.inventory_item TempInventoryItem = (Inventory.inventory_item)TheInventoryList[MyInventoryItem.SqlID];
                            TempInventoryItem.quantity += MyInventoryItem.quantity;
                            TheInventoryList[MyInventoryItem.SqlID] = TempInventoryItem;
                        }
                        else
                        {
                            if (MyInventoryItem.pos < 36)
                            {
                                TheInventoryList.Add(MyInventoryItem.SqlID, MyInventoryItem);
                            }
                        }
                    }

                    //loop through the storage finding reserved amounts
                    System.Collections.ArrayList MyStorageList = TheStorage.GetStorageList();
                    foreach (Storage.StorageItem MyStorageItem in MyStorageList)
                    {
                        if (maxlen < MyStorageItem.name.Length)
                        {
                            maxlen = MyStorageItem.name.Length;
                        }
                        if (TheInventoryList.Contains(MyStorageItem.knownItemsID))
                        {
                            //already in the list, do nothing since storage quantity is already the total
                            //and it repeats, kinda like an unstackable item
                        }
                        else
                        {
                            Inventory.inventory_item MyInventoryItem = new Inventory.inventory_item();
                            MyInventoryItem.name = MyStorageItem.name;
                            MyInventoryItem.SqlID = MyStorageItem.knownItemsID;
                            MyInventoryItem.quantity = MyStorageItem.quantity;
                            MyInventoryItem.reservedQuantity = MyStorageItem.reservedQuantity;
                            TheInventoryList.Add(MyInventoryItem.SqlID, MyInventoryItem);
                        }
                    }
                    // pm the reserved items
                    string str = "";
                    string msg = "";
                    str = "[";
                    str = str.PadRight(maxlen + 27, '-');
                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, str));
                    foreach (Inventory.inventory_item MyInventoryItem in TheInventoryList.Values)
                    {
                        uint reservedQuantity = TheMySqlManager.ReservedAmount(MyInventoryItem.SqlID);
                        if (MyInventoryItem.pos < 36 && reservedQuantity > 0)
                        {
                            msg = "[";
                            msg += MyInventoryItem.name + " " + reservedQuantity + " of " + MyInventoryItem.quantity;
                            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, msg));
                        }
                    }
                    str = "[";
                    str = str.PadRight(maxlen + 27, '-');
                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, str));
                }

                if (CommandArray.Length > 2)
                {
                    validCommand = true;
                    try
                    {
                        if (CommandArray[1] == "delete")
                        {
                            Int32 rowIndex = Int32.Parse(CommandArray[2]);
                            if (TheMySqlManager.reservedDelete(Settings.botid, e.username, rowIndex))
                            {
                                TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[Row Deleted."));
                            }
                            else
                            {
                                TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[Index not found."));
                            }
                        }
                        else
                        {
                            Int32 numberToReserve = Int32.Parse(CommandArray[1]);
                            int i;
                            string itemName = "";
                            for (i = 2; i < CommandArray.Length; i++)
                            {
                                itemName += CommandArray[i] + " ";
                            }
                            int itemId = TheMySqlManager.GetItemID(itemName,false);
                            if (itemId > 0)
                            {
                                if (TheMySqlManager.reserveItem(itemId, (uint)numberToReserve, e.username) == true)
                                {
                                    TheTradeHandler.AddTrade(itemId, 0, (uint)numberToReserve, "reserved");
                                    TradeHandler.TradeLogItem myItem = new TradeHandler.TradeLogItem();
                                    myItem.KnownItemsSqlID = itemId;
                                    myItem.quantity = (uint)numberToReserve;
                                    myItem.action = "reserved by";
                                    TheMySqlManager.LogTrade(myItem, e.username, Settings.botid, true);
                                    TheInventory.requestInventory();
                                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[You just reserved " + myItem.quantity + " " + TheMySqlManager.GetKnownItemsname(myItem.KnownItemsSqlID)));
                                }
                                else //probably never get to this one...
                                {
                                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[Item not found!"));
                                }
                            }
                            else
                            {
                                TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[Item not found!"));
                            }
                        }
                    }
                    catch
                    {
                        goto WrongArguments;
                    }
                }
                else
                {
                    if (!validCommand)
                    {
                        goto WrongArguments;
                    }
                }
            }
            return;

        WrongArguments:
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[----------------------------------------------------"));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[Here is the usage of the #reserve command:          "));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[#reserve <list>[withdraw] <amount item>             "));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[----------------------------------------------------"));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[Example: #reserve list                              "));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[Example: #reserve amount item                       "));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[Example: #reserve details                           "));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[Example: #reserve delete <idx>*                     "));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[*<idx> provided by #reserve details command.        "));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[----------------------------------------------------"));
            return;
		}
	}
}
