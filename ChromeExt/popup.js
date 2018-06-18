function gsuiteManager(sendFunction) {
    document.addEventListener('DOMContentLoaded',
        function() {

            chrome.tabs.getSelected(null,
                function(tab) {
                    //tab.url can now be used to get url
                    let url = tab.url;
                    if (url.includes("docs.google.com")) {
                        //google doc is open
                        document.getElementById("addDoc").style.visibility = "visible";
                    }
                });

            const addDocButton = document.getElementById('addDoc');
            addDocButton.addEventListener('click',
                function() {
                    chrome.tabs.getSelected(null,
                        function(tab) {

                            let urlSections = tab.url.split("/");
                            let fileId = urlSections[urlSections.length - 2];

                            //allows user to type in info to save their authentication token as token
                            chrome.identity.getAuthToken({ 'interactive': true },
                                function(token) {
                                    if (token) {
                                        this.accessToken = token;


                                        //You could try to use a symlink and link your temp-directory as sub-folder into downloads. I am not sure if this works, though
                                        //chrome.downloads.download({
                                        //    url: "C:\\Users\\GFX lab\\Downloads\\byes.pdf",
                                        //    filename: "\\test"
                                        //});

                                        var xhr = new XMLHttpRequest();

                                        //send fileId and mimeType as a parameter to url to get file info
                                        xhr.open("GET",
                                            "https://www.googleapis.com/drive/v3/files/" +
                                            fileId +
                                            "/export?mimeType=application%2Fpdf",
                                            true);
                                        xhr.setRequestHeader("Authorization", "Bearer " + token);
                                        xhr.responseType = "arraybuffer";

                                        xhr.onload = function() {
                                            //console.log(xhr);
                                            var pdf = xhr.response;

                                            var data = btoa(pdf);
                                            var request = {
                                                "$type": "Dash.GSuiteImportRequest, Dash",
                                                "data": data
                                            }
                                            //var array = new Uint8Array(pdf);

                                            //var text = array,
                                            //    blob = new Blob([text], { type: 'octet/stream' }),
                                            //    anchor = document.createElement('a');

                                            //anchor.download = "byes.pdf";
                                            //anchor.href = (window.webkitURL || window.URL).createObjectURL(blob);
                                            ////['application/pdf', anchor.download, anchor.href].join(':');
                                            //anchor.click();


                                        }
                                        xhr.send('alt=media');

                                        /*        
                                        var file = IO.newFile("Downloads", "byes.pdf");
                                        var destination = IO.newFile("Desktop", "");
                                        file.copyTo(destination, "byes.pdf");
                    
                                        var object = new ActiveXObject("Scripting.FileSystemObject");
                                        var file = object.GetFile(var io = require('socket.io')(server);");
                                        file.Move("C:\\Users\\GFX lab\\AppData\\Local\\Packages\\115b743b-4c3a-45e5-a780-6fbd26aec201_hz258y3tkez3a\\LocalState\\test\\");
                    
                                        var sourceFolder = new Folder("C:\\Users\\GFX lab\\Downloads");
                                        var destFolder = new Folder("C:\\Users\\GFX lab\\AppData\\Local\\Packages\\115b743b-4c3a-45e5-a780-6fbd26aec201_hz258y3tkez3a\\LocalState\\test");
                                        var fileList = sourceFolder.getFiles();
                                        for (var i = 0; i < fileList.length; i++) {
                                            if (fileList[i].copy(decodeURI(destFolder) + "/" + fileList[i].displayName)) {
                                                //fileList[i].remove();
                                            }
                                        } */
                                    }


                                });
                        });
                },
                false);

        },
        false);
}
