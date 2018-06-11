document.addEventListener('DOMContentLoaded', function () {
    
   // var openDashButton = document.getElementById('openDash');
   // openDashButton.addEventListener('click', function () {
        


        chrome.tabs.getSelected(null, function (tab) {
            //tab.url can now be used to get url
            let url = tab.url;
            if (url.includes("docs.google.com")) {
                //google doc is open
                
                document.getElementById("addDoc").style.visibility = "visible";
            }
        });
   // }, false);

    const addDocButton = document.getElementById('addDoc');
    addDocButton.addEventListener('click', function () {
        chrome.tabs.getSelected(null, function (tab) {

            let urlSections = tab.url.split("/");
            let fileId = urlSections[urlSections.length - 2];

            chrome.identity.getAuthToken({ 'interactive': true }, function (token) {
                
                console.log(token);
                if (token) {
                    document.body.style.backgroundColor = "blue";
                    this.accessToken = token;
                   // opt_callback && opt_callback();

                    var xhr = new XMLHttpRequest();

                    xhr.open("GET", "https://www.googleapis.com/drive/v3/files/" + fileId, true);
                    xhr.setRequestHeader("Authorization", "Bearer " + token);
                    xhr.onload = function () {
                        console.log(xhr);
                    }
                    xhr.send('alt=media');
                }
                
            });
    /*
            try {
                chrome.identity.getAuthToken({ interactive: interactive }, function (token) {
                    if (token) {
                        this.accessToken = token;
                        opt_callback && opt_callback();
                    }
                }.bind(this));
            } catch (e) {
                console.log(e);
            }*/




            //I created bundle.js using browserfy to get require to work
        //    const fs = require('fs');
            
            /*
            gapi.client.init({
                'apiKey': 'AIzaSyAI8fUZnDRr7zGh1vrHhngRRojlhrIQuW8',
                'clientId': '533488754428-s7cp12aob9b9b11ll1l0ojtf82hnimsq.apps.googleusercontent.com',
                'scope': 'https://www.googleapis.com/auth/drive.metadata.readonly',
                'discoveryDocs': ['https://www.googleapis.com/discovery/v1/apis/drive/v3/rest']
            }).then(function () {
                GoogleAuth = gapi.auth2.getAuthInstance();
                
                console.log(gapi.auth2);

                GoogleAuth.signIn();

                // Listen for sign-in state changes.
                GoogleAuth.isSignedIn.listen(updateSigninStatus);
            }); 
             var accessToken = gapi.auth.getToken().access_token;
            console.log(gapi.auth2);
            //I made this acess token up, won't work 
             accessToken = "a762t"
            var xhr = new XMLHttpRequest();
           
            xhr.open("GET", "https://www.googleapis.com/drive/v3/files/" + fileId, true);
            xhr.setRequestHeader("Authorization", "OAuth " + accessToken);
            xhr.onload = function () {
                console.log(xhr);
            }
            xhr.send('alt=media'); */

           /*
            let dest = fs.createWriteStream('Desktop/resume.pdf');
        drive.files.export({
            fileId: fileId,
            mimeType: 'application/pdf'
        })
            .on('end', function () {
                console.log('Done');
            })
            .on('error', function (err) {
                console.log('Error during download', err);
            })
                .pipe(dest); */
        });
    }, false);

}, false)