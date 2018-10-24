using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Dash
{
    class DashVoiceCommand
    {
        public string voiceCommand;
        public string commandMode;
        public string textSpoken;
        public string destination;

        /// <summary>
        /// Set up the voice command struct with the provided details about the voice command.
        /// Oriented around the "showTripToDestination" VCD command (See AdventureWorksCommands.xml)
        /// </summary>
        /// <param name="voiceCommand">The voice command (the Command element in the VCD xml) </param>
        /// <param name="commandMode">The command mode (whether it was voice or text activation)</param>
        /// <param name="textSpoken">The raw voice command text.</param>
        /// <param name="destination">The destination parameter.</param>
        public DashVoiceCommand(string voiceCommand, string commandMode, string textSpoken, string destination)
        {
            this.voiceCommand = voiceCommand;
            this.commandMode = commandMode;
            this.textSpoken = textSpoken;
            this.destination = destination;
        }
    }

}
