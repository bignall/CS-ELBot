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
	public class DepositCommandHandler
	{
		private TCPWrapper TheTCPWrapper;
		private BasicCommunication.MessageParser TheMessageParser;
		private MySqlManager TheMySqlManager;
		//private bool CommandIsDisabled;
		private AdvHelpCommandHandler TheAdvHelpCommandHandler;
		private Inventory TheInventory;
        private TradeHandler TheTradeHandler;
        private Stats TheStats;
        private Storage TheStorage;

        public DepositCommandHandler(TCPWrapper MyTCPWrapper, BasicCommunication.MessageParser MyMessageParser, AdvHelpCommandHandler MyAdvHelpCommandHandler, MySqlManager MyMySqlManager, Inventory MyInventory, TradeHandler MyTradeHandler, Stats MyStats, Storage MyStorage)
		{
			this.TheTCPWrapper = MyTCPWrapper;
			this.TheMessageParser = MyMessageParser;
			this.TheAdvHelpCommandHandler = MyAdvHelpCommandHandler;
			this.TheMySqlManager = MyMySqlManager;
			this.TheInventory = MyInventory;
            this.TheTradeHandler = MyTradeHandler;
            this.TheStats = MyStats;
            this.TheStorage = MyStorage;
			{
                TheAdvHelpCommandHandler.AddCommand("#deposit - deposit something(s) into my storage");
                TheMessageParser.Got_PM += new BasicCommunication.MessageParser.Got_PM_EventHandler(OnGotPM);
                this.TheTCPWrapper.GotCommand += new TCPWrapper.GotCommandEventHandler(OnGotCommand);
            }
		}
        private void OnGotCommand(object sender, TCPWrapper.GotCommandEventArgs e)
        {
            //if (TheTradeHandler.makingDeposit)
            //{
            //    if (e.CommandBuffer[0] == 68) // STORAGE_ITEMS (per category)
            //    {
            //        STORAGE_ITEMS(e.CommandBuffer);
            //    }
            //}
            if (TheTradeHandler.depositMade)
            {
                if (e.CommandBuffer[0] == 68) // STORAGE_ITEMS (per category)
                {
                    STORAGE_ITEMS(e.CommandBuffer);
                }
                if (e.CommandBuffer[0] == 21)
                {
                    GET_NEW_INVENTORY_ITEM(e.CommandBuffer);
                }

                if (e.CommandBuffer[0] == 49)
                {
                    SEND_PARTIAL_STAT(e.CommandBuffer);
                }
            }
        }

        private void STORAGE_ITEMS(byte[] data)
        {
            if (TheTradeHandler.depositMade)
            {
                uint item_count = data[3]; // should be in this byte but isn't yet :P
                UInt16 data_length = System.BitConverter.ToUInt16(data, 1);
                data_length += 2;
                uint category_num = data[4];
                UInt16 pos;
                int imageid;
                uint quantity;
                //Storage.StorageItem MyStorageItem = new Storage.StorageItem();
                // so, we're calculating the number of items with the size of the packet
                item_count = (uint)(data_length - 5) / 8;
                if (item_count == 1)
                {
                    for (int i = 0; i < item_count; i++)
                    {
                        imageid = System.BitConverter.ToUInt16(data, i * 8 + 5);
                        quantity = System.BitConverter.ToUInt32(data, i * 8 + 5 + 2);
                        pos = System.BitConverter.ToUInt16(data, i * 8 + 5 + 6);
                        MyStorageItem.imageid = imageid;
                        MyStorageItem.pos = pos;
                        MyStorageItem.quantity = quantity;
                        //MyStorageItem.name = "";
                        MyStorageItem.category_num = (int)category_num;
                        //TheTCPWrapper.Send(CommandCreator.LOOK_AT_STORAGE_ITEM(pos));
                    }
                }
            }

        }

        Storage.StorageItem MyStorageItem = new Storage.StorageItem();
        int totalDeposited = 0;
        private void SEND_PARTIAL_STAT(byte[] data)
        {
            if (TheTradeHandler.depositMade == true)
            {
                TheTradeHandler.SentThanks = false;
                TradeHandler.username = username;
                TheTradeHandler.LogTrade();
                //TheStorage.updateItem(MyStorageItem, totalDeposited * -1);
                TheStorage.updateItem(MyStorageItem, (int)MyStorageItem.quantity * -1, true);
                TheTradeHandler.depositMade = false;
            }
        }

        private void GET_NEW_INVENTORY_ITEM(byte[] data)
        {
            byte pos = data[9];
            byte flags = data[10];
            uint quantity = System.BitConverter.ToUInt32(data, 5);
            int image_id = System.BitConverter.ToInt16(data, 3);
            if (TheTradeHandler.depositMade)
            {
                int quantityDeposited = 1;
                if (TheInventory.isStackable(MyStorageItem.knownItemsID))
                {
                    quantityDeposited = (int)(TheInventory.Quantity(MyStorageItem.knownItemsID) - quantity);
                }
                TheTradeHandler.AddTrade(MyStorageItem.knownItemsID, 0, (uint)(quantityDeposited), "Deposited");
                MyStorageItem.quantity -= (uint)quantityDeposited;
                totalDeposited += quantityDeposited;
                TheTradeHandler.makingDeposit = false;
            }
        }

        private string username = "";
        private void OnGotPM(object sender, BasicCommunication.MessageParser.Got_PM_EventArgs e)
        {
            string Message = e.Message.ToLower().Replace("\'", "\\\'").Replace("\"", "\\\"");
            string[] Inv=new string[64];

            if (Message[0] != '#')
            {
                Message = "#" + Message;
            }

            string[] CommandArray = Message.Split(' ');
            if (CommandArray[0] == "#deposit")
            {
                username = e.username;
                bool disabled = TheMySqlManager.CheckIfCommandIsDisabled("#deposit", Settings.botid);


                if (disabled == true)
                {
                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "This command is disabled"));
                    return;
                }

                int rank = TheMySqlManager.GetUserRank(e.username, Settings.botid);

                if (rank < TheMySqlManager.GetCommandRank("#deposit", Settings.botid))
                {
                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "You are not authorized to use this command!"));
                    return;
                }

                if (CommandArray.Length < 1)
                    goto WrongArguments;

                if (!TradeHandler.storageOpen)
                {
                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "You must open storage first!"));
                    return;
                }

                if (TheInventory.GettingInventoryItems == true || TheInventory.inventoryRequested)
                {
                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "Please wait a moment for me to finish getting my inventory..."));
                    return;
                }

                TheTradeHandler.makingDeposit = true;
                //ok, let's do a deposit
                uint quantity = 0;
                string itemName = "";
                int SQLID = -1;

                int nameToID = -1;
                string str = "";
                try
                {
                    if (CommandArray.Length < 2)
                    {
                        goto WrongArguments;
                    }
                    if (CommandArray.Length < 3)
                    {
                        quantity = 1;
                        try
                        {
                            SQLID = int.Parse(CommandArray[1]);
                            nameToID = SQLID;
                        }
                        catch
                        {
                            itemName = CommandArray[1] + " ";
                            nameToID = TheMySqlManager.GetItemID(CommandArray[1], Settings.botid, true);
                        }
                    }
                    if (CommandArray.Length > 2)
                    {
                        int i;
                        try
                        {
                            quantity = uint.Parse(CommandArray[1]);
                            for (i = 2; i < CommandArray.Length; i++)
                            {
                                str += CommandArray[i] + " ";
                            }
                        }
                        catch
                        {
                            quantity = 1;
                            for (i = 1; i < CommandArray.Length; i++)
                            {
                                str += CommandArray[i] + " ";
                            }
                        }
                        finally
                        {
                            str = str.Trim();
                            nameToID = TheMySqlManager.GetItemID(str, Settings.botid, true);
                            itemName = str + " ";
                        }
                    }
                }
                catch
                {
                    goto WrongArguments;
                }
                finally
                {
                    try
                    {
                        SQLID = int.Parse(str);
                    }
                    catch
                    {
                        SQLID = nameToID;
                    }
                }

                int invCount = 0;
                MyStorageItem = new Storage.StorageItem();
                foreach (Inventory.inventory_item myInventoryItem in TheInventory.GetInventoryList())
                {
                    if (myInventoryItem.SqlID == SQLID)
                    {
                        TheTCPWrapper.Send(CommandCreator.DEPOSIT_ITEM(myInventoryItem.pos, (UInt16)quantity));
                        TheInventory.requestInventory();
                        TheTradeHandler.makingDeposit = false;
                        TheTradeHandler.depositMade = true;
                        MyStorageItem.knownItemsID = myInventoryItem.SqlID;
                        MyStorageItem.imageid = myInventoryItem.imageid;
                        MyStorageItem.category_num = -1;
                        MyStorageItem.name = myInventoryItem.name;
                        MyStorageItem.reservedQuantity = 0;
                        MyStorageItem.name = myInventoryItem.name;
                        break;
                    }
                    invCount++;
                }
                if (!TheTradeHandler.depositMade)
                {
                    TheTradeHandler.makingDeposit = false;
                    string outputString = "I don't seem to have any " + itemName + " in inventory!";
                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, outputString));
                }
            }
            return;

        WrongArguments:
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[--------------------------------------"));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[Here is the usage of the #sto command:"));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[#withdraw amt item                    "));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[--------------------------------------"));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[Example: #withdraw 1 silver ore       "));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[--------------------------------------"));
            return;
		}
	}
}
