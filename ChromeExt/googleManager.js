function googleManager(sendRequestFunction) {

    document.body.style.backgroundColor = "red";
    
                    var requestBody = {
                        "$type": "Dash.UpdateTabBrowserRequest, Dash",
                        "tabId": "777"
                    }
                    sendRequestFunction(requestBody);
              
    
}