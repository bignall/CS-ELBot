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
	public class STOCommandHandler
	{
		private TCPWrapper TheTCPWrapper;
		private BasicCommunication.MessageParser TheMessageParser;
		private MySqlManager TheMySqlManager;
		//private bool CommandIsDisabled;
		private AdvHelpCommandHandler TheAdvHelpCommandHandler;
		private Storage TheStorage;
        private TradeHandler TheTradeHandler;
        private Stats TheStats;
		
		public STOCommandHandler(TCPWrapper MyTCPWrapper, BasicCommunication.MessageParser MyMessageParser, AdvHelpCommandHandler MyAdvHelpCommandHandler, MySqlManager MyMySqlManager, Storage MyStorage, TradeHandler MyTradeHandler, Stats MyStats)
		{
			this.TheTCPWrapper = MyTCPWrapper;
			this.TheMessageParser = MyMessageParser;
			this.TheAdvHelpCommandHandler = MyAdvHelpCommandHandler;
			this.TheMySqlManager = MyMySqlManager;
			this.TheStorage = MyStorage;
            this.TheTradeHandler = MyTradeHandler;
            this.TheStats = MyStats;

			//this.CommandIsDisabled = MyMySqlManager.CheckIfCommandIsDisabled("#inv",Settings.botid);
			
			//if (CommandIsDisabled == false)
			{
                TheAdvHelpCommandHandler.AddCommand("#sto - show what's in my storage");
                //TheAdvHelpCommandHandler.AddCommand("#sto - null");
                //TheAdvHelpCommandHandler.AddCommand("#storage - null");
                TheMessageParser.Got_PM += new BasicCommunication.MessageParser.Got_PM_EventHandler(OnGotPM);
			}
		}

        private void OnGotPM(object sender, BasicCommunication.MessageParser.Got_PM_EventArgs e)
        {
            string Message = e.Message.ToLower().Replace("\'", "\\\'").Replace("\"", "\\\"");
            string[] Inv=new string[64];
            string searchName = "";
            string inputCategory = "";

            if (Message[0] != '#')
            {
                Message = "#" + Message;
            }

            string[] CommandArray = Message.Split(' ');
            bool issto = false;
            if (CommandArray[0] == "#sto")
                issto = true;
            if (issto)
            {
                bool disabled = TheMySqlManager.CheckIfCommandIsDisabled("#sto", Settings.botid);


                if (disabled == true)
                {
                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "This command is disabled"));
                    return;
                }

                int rank = TheMySqlManager.GetUserRank(e.username, Settings.botid);
                if (rank < TheMySqlManager.GetCommandRank("#sto", Settings.botid))
                {
                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "You are not authorized to use this command!"));
                    return;
                }

//                if (TheTradeHandler.Trading == false || TheTradeHandler.username.ToLower() == "")
////                if (TheTradeHandler.Trading == false || TheTradeHandler.username.ToLower() != e.username.ToLower())
//                {
//                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "Someone must be trading with me first!"));
//                    return;
//                }
                if (!TradeHandler.storageOpen)
                {
                    if (TradeHandler.openingStorage)
                    {
                        TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "Please wait a moment for storage to finish opening."));
                    }
                    else
                    {
                        TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "You must open storage first!"));
                    }
                    return;
                }

                if (CommandArray.Length < 1)
                    goto WrongArguments;
                else if (CommandArray.Length > 1)
                    //inputCategory = (string)CommandArray.GetValue(1);
                {
                    for (int i = 1; i < CommandArray.Length; i++)
                    {
                        if (inputCategory.Length > 0)
                        {
                            inputCategory += (" " + (string)CommandArray[i]);
                        }
                        else
                        {
                            inputCategory += (string)CommandArray[i];
                        }
                    }
                    //Console.WriteLine("inputCategory:{0}", inputCategory);
                }
                else
                {
                    if (TheStorage.TheStorageCategories.Count > 0 && TradeHandler.storageOpen)
                    {
                        TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "Please enter one of the following categories (or an item name)!"));

                        foreach (Storage.StorageCategory MyStorageCategory in TheStorage.TheStorageCategories)
                        {
                            if (MyStorageCategory.num != -1)
                            {
                                TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, MyStorageCategory.name));
                            }
                        }
                        TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "All"));
                    }
                    return;
                }

                string searchcategory = "";
                bool categoryFound = false;

                searchName = inputCategory;
                foreach (Storage.StorageCategory MyStorageCategory in TheStorage.TheStorageCategories)
                {
                    searchcategory = MyStorageCategory.name.ToLower();
                    if (searchcategory.Contains(inputCategory) || (inputCategory.Contains("all")))
                    {
                        searchName = "";
                        categoryFound = true;
                    }
                }

                if (categoryFound == false)
                {
                    inputCategory = "all";
                }
                //pm the boarder
                string str = "";
                string str2 = "";
                int lineLength = 55;
                str = "[";
                str = str.PadRight(lineLength, '-');
                str2 = "[";
                str2 = str2.PadRight(lineLength, '=');
                //string header = "[ItemID:    Qty:  Name:";
                //header = header.PadRight(lineLength, ' ');
                string catname = "";
                bool itemsFound = false;
                TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, str2));
                bool Member = (TheMySqlManager.CheckIfTradeMember(e.username, Settings.botid) == true);

                //loop through the storage sending pms
                foreach (Storage.StorageCategory MyStorageCategory in TheStorage.TheStorageCategories)
                {

                    catname = "[Item ID:  Count: Category: " + MyStorageCategory.name.Substring(0, MyStorageCategory.name.Length);
                    catname = catname.PadRight(lineLength, ' ');
                    searchcategory = MyStorageCategory.name.ToLower();
                    if (searchcategory.Contains(inputCategory) || (inputCategory.Contains("all")))
                    {
                        if (searchName == "")
                        {
                            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, catname));
                            // send header
                            //TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, header));

                        }
                        bool categoryPrinted = false;
                        foreach (Storage.StorageItem MyStorageItem in TheStorage.GetStorageList())
                        {
                            string outputLine = "";
                            Int32 tempAmount = 0;
                            string temp;
                            string spaces = "   ";
                            temp = MyStorageItem.knownItemsID.ToString();
                            temp = temp.PadLeft(6, ' ');
                            outputLine = "[" + temp + spaces;
                            tempAmount = (Int32)(MyStorageItem.quantity - MyStorageItem.reservedQuantity);
                            if (tempAmount < 0)
                            {
                                tempAmount = 0;
                            }
//                            temp = (MyStorageItem.quantity - MyStorageItem.reservedQuantity).ToString();
                            temp = tempAmount.ToString();
                            temp = temp.PadLeft(5, ' ');
                            outputLine += temp + spaces + MyStorageItem.name.Substring(0, MyStorageItem.name.Length);
                            outputLine = outputLine.PadRight(lineLength, ' ');
                            if (searchName != "")
                            {
                                if (MyStorageItem.name.ToLower().Contains(searchName) && MyStorageItem.category_num == MyStorageCategory.num)
                                {
                                    if (categoryPrinted == false)
                                    {
                                        TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, catname));
                                    }
                                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, outputLine));
                                    itemsFound = true;
                                    categoryPrinted = true;
                                }
                            }
                            else if (MyStorageItem.category_num == MyStorageCategory.num)
                            {
                                TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, outputLine));
                                itemsFound = true;
                            }
                        }
                        if (itemsFound == false && searchName == "")
                        {
                            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[None found"));
                            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, str));
                        }
                        else if (itemsFound == true)
                            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, str));
                        itemsFound = false;
                    }
                }

                return;
            }
            return;

        WrongArguments:
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[--------------------------------------"));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[Here is the usage of the #sto command:"));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[#sto <item>                           "));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[--------------------------------------"));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[Example: #sto                         "));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[Example: #sto silver                  "));
            TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[--------------------------------------"));
            return;
		}
	}
}
