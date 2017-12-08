
function requestHandler(tabManager) {
    //to handle a new tab request
    var handleNewTab = function(request)
    {
        newUrl = request.url;
        tabManager.addNewTab(newUrl);
    }

    //to handle a set url request
    var handleSetUrl = function (request) {
        newUrl = request.url;
        console.log("request sent to update tab");
    }


    //takes in a string
    this.handle = function(message) {
        //console.log("Message: ")
        //console.log(message)
        if (message.includes("{")) {
            var obj = JSON.parse(message);
            var type = obj.$type.split(".").slice(-1)[0].split(",")[0]

            switch(type) {
                case "NewTabBrowserRequest":
                    handleNewTab(obj)
                    break;

                case "SetUrlBrowserRequest":
                    handleSetUrl(obj)
                    break;
            }
        }

    }
}