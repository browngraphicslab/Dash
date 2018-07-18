var loadingBarWidth = 0;
var loadToPercentage = 0;

var loadBarTo = function () {
    var loadingBar = document.getElementById("loadingBar");

    if (loadingBarWidth >= loadToPercentage) {
        if (loadingBarWidth == 100) {
            setTimeout(function() {
                    loadingBar.style.width = 0;
                    loadingBarWidth = 0;
                    loadToPercentage = 0;
                },
                100);
        }
        return;
    }
    loadingBarWidth++;
    loadingBar.style.width = loadingBarWidth + "%";
    setTimeout(loadBarTo, loadingBarWidth);
};

document.addEventListener('DOMContentLoaded',
    function () {

        chrome.tabs.getSelected(null,
            function (tab) {
                //tab.url can now be used to get url
                let url = tab.url;
                if (url.includes("docs.google.com") || url.includes(".pdf") || url.includes(".PDF")) {
                    //google doc is open
                    document.getElementById("addDoc").style.visibility = "visible";
                } 
            });

        const addDocButton = document.getElementById('addDoc');
        addDocButton.addEventListener('click',
            function () {
                loadToPercentage = 80;
                loadBarTo();
                chrome.tabs.getSelected(null,
                    function (tab) {
                            //allows user to type in info to save their authentication token as token
                            chrome.identity.getAuthToken({ 'interactive': true },
                                function(token) {
                                    if (token) {
                                        console.log(token);
                                        this.accessToken = token;

                                        var xhr = new XMLHttpRequest();

                                        let path = "";
                                        if (tab.url.includes("docs.google.com")) {
                                            let urlSections = tab.url.split("/");
                                            let fileId = urlSections[urlSections.length - 2];
                                            console.log(fileId);
                                            path = "https://www.googleapis.com/drive/v3/files/" +
                                                fileId +
                                                "/export?mimeType=application%2Fpdf";
                                        } else {
                                            path = tab.url;
                                        }
                                        console.log(path);

                                        //send fileId and mimeType as a parameter to url to get file info
                                        xhr.open("GET",
                                            path,
                                            true);
                                        xhr.setRequestHeader("Authorization", "Bearer " + token);

                                        xhr.responseType = "arraybuffer";

                                        xhr.onload = function() {
                                            //only continue if successful
                                            console.log(xhr.status);
                                            if (xhr.status === 200) {
                                                var res = xhr.response;

                                                var pdf = '';
                                                var bytes = new Uint8Array(res);
                                                var len = res.byteLength;
                                                for (var i = 0; i < len; i++) {
                                                    pdf += String.fromCharCode(bytes[i]);
                                                }

                                                var data = btoa(pdf);
                                                var request = {
                                                    "$type": "Dash.GSuiteImportRequest, Dash",
                                                    "data": data
                                                }
                                                chrome.runtime.sendMessage({ type: "sendRequest", data: request });

                                            }

                                            loadToPercentage = 100;
                                            loadBarTo();
                                        };
                                        //var array = new Uint8Array(pdf);

                                        //var text = array,
                                        //    blob = new Blob([text], { type: 'octet/stream' }),
                                        //    anchor = document.createElement('a');

                                        //anchor.download = "byes.pdf";
                                        //anchor.href = (window.webkitURL || window.URL).createObjectURL(blob);
                                        ////['application/pdf', anchor.download, anchor.href].join(':');
                                        //anchor.click();


                                        xhr.send('alt=media');
                                    }
                                });

                    });
            });
    },
    false);