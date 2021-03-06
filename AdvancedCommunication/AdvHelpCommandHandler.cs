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
	/// description of AdvHelpCommandHandler.
	/// </summary>
	public class AdvHelpCommandHandler
	{
		private TCPWrapper TheTCPWrapper;
		private BasicCommunication.MessageParser TheMessageParser;
		private PMHandler ThePMHandler;
		private MySqlManager TheMySqlManager;
		//private bool CommandIsDisabled;
		private HelpCommandHandler TheHelpCommandHandler;
		private System.Collections.ArrayList CommandArrayList = new System.Collections.ArrayList();
		
		public AdvHelpCommandHandler(TCPWrapper MyTCPWrapper, BasicCommunication.MessageParser MyMessageParser,PMHandler MyPMHandler, MySqlManager MyMySqlManager,HelpCommandHandler MyHelpCommandHandler)
		{
			this.TheTCPWrapper = MyTCPWrapper;
			this.TheMessageParser = MyMessageParser;
			this.ThePMHandler = MyPMHandler;
			this.TheMySqlManager = MyMySqlManager;
			this.TheHelpCommandHandler = MyHelpCommandHandler;
			//this.CommandIsDisabled = MyMySqlManager.CheckIfCommandIsDisabled("#advhelp",Settings.botid);
			
			//if (CommandIsDisabled == false)
			{
                ThePMHandler.AddCommand("#advhelp");
                ThePMHandler.AddCommand("#ah");
                TheHelpCommandHandler.AddCommand("#advhelp / #ah - displays advanced help");
                TheMessageParser.Got_PM += new BasicCommunication.MessageParser.Got_PM_EventHandler(OnGotPM);
			}
		}
		
		private void OnGotPM(object sender, BasicCommunication.MessageParser.Got_PM_EventArgs e)
		{
            int lineSize = 55;
            string Message = e.Message.ToLower();
			
			if (Message[0]!='#')
			{
				Message = "#" + Message;
			}
			
			string[] CommandArray = Message.Split(' ');

            if (CommandArray[0] == "#advhelp" || CommandArray[0] == "#ah")
			{
                bool disabled = TheMySqlManager.CheckIfCommandIsDisabled("#advhelp", Settings.botid);

                if (disabled == true)
                {
                    TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "This command is disabled"));
                    return;
                }
                
                if (TheMySqlManager.GetUserRank(e.username, Settings.botid) < TheMySqlManager.GetCommandRank("#advhelp", Settings.botid))
			    {
			    	TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username,"You are not authorized to use this command!"));
			    	return;
			    }

                TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[".PadRight(lineSize, '-')));
                TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[List of available advanced commands:".PadRight(lineSize, ' ')));

                // display the advanced commands
                TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[".PadRight(lineSize,'-')));
                foreach (string MyCommand in CommandArrayList)
			{
				if (!MyCommand.ToLower().Contains("null"))
                   		{
					TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[" + MyCommand.PadRight(lineSize-1,' ')));
				}
			}
                TheTCPWrapper.Send(CommandCreator.SEND_PM(e.username, "[".PadRight(lineSize,'=')));
			}
		}
		
		public void AddCommand(string Command)
		{
			string[] CommandArray = Command.Split(' ');
			
			CommandArrayList.Add(Command);
			ThePMHandler.AddCommand(CommandArray[0]);
		}
		
		public void RemoveCommand(string Command)
		{
			string[] CommandArray = Command.Split(' ');
			
			CommandArrayList.Remove(Command);
			ThePMHandler.RemoveCommand(CommandArray[0]);
		}
	}
}
