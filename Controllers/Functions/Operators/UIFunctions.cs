using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dash.Popups;

namespace Dash
{
    public static class UIFunctions
    {
        public static async Task<TextController> TextInput()
        {
            var popup = new TextInputPopup
            {
                Width = 400,
                Height = 400
            };


            var (title, functionBody) = await popup.OpenAsync();

            if (functionBody != null)
            {
                return new TextController(functionBody);
            }

            return null;
        }

        public static async Task<List<OperatorController>> ManageBehaviors()
        {
            var manageBehaviors = new ManageBehaviorsPopup();

            var newBehaviors = new List<OperatorController>(); 
            if ((newBehaviors = await manageBehaviors.OpenAsync()) != null)
            {
                
            }

            return null;
        }
    } 
}
