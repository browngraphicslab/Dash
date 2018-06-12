document.addEventListener('DOMContentLoaded', function () {

        chrome.tabs.getSelected(null, function (tab) {
            //tab.url can now be used to get url
            let url = tab.url;
            if (url.includes("docs.google.com")) {
                //google doc is open
                document.getElementById("addDoc").style.visibility = "visible";
            }
        });

    const addDocButton = document.getElementById('addDoc');
    addDocButton.addEventListener('click', function () {
        chrome.tabs.getSelected(null, function (tab) {

            let urlSections = tab.url.split("/");
            let fileId = urlSections[urlSections.length - 2];

            //allows user to type in info to save their authentication token as token
            chrome.identity.getAuthToken({ 'interactive': true }, function (token) { 
                if (token) {
                    this.accessToken = token;

                    var xhr = new XMLHttpRequest();

                    //send fileId and mimeType as a parameter to url to get file info
                    xhr.open("GET", "https://www.googleapis.com/drive/v3/files/" + fileId + "/export?mimeType=application%2Fpdf", true);
                    xhr.setRequestHeader("Authorization", "Bearer " + token);
                    xhr.responseType = "arraybuffer"

                    xhr.onload = function () {
                        console.log(xhr);
                        var pdf = xhr.response;

                        var array = new Uint8Array(pdf);

                        var text = array,
                            blob = new Blob([text], { type: 'octet/stream' }),
                            anchor = document.createElement('a');

                        anchor.download = "hello.pdf";
                        anchor.href = (window.webkitURL || window.URL).createObjectURL(blob);
                        //anchor.dataset.downloadurl = ['application/pdf', anchor.download, anchor.href].join(':');
                        anchor.click();

                        
                    }
                    xhr.send('alt=media');
                }
                
            });
        });
    }, false);

}, false)
