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

            //I created bundle.js using browserfy to get require to work
            const fs = require('fs');
            document.body.style.backgroundColor = "blue";
            
            // var accessToken = gapi.auth.getToken().access_token;
            console.log(gapi.auth);
            //I made this acess token up, won't work 
            let accessToken = "a762t"
            var xhr = new XMLHttpRequest();
           
            xhr.open("GET", "https://www.googleapis.com/drive/v3/files/" + fileId, true);
            xhr.setRequestHeader("Authorization", "OAuth " + accessToken);
            xhr.onload = function () {
                console.log(xhr);
            }
            xhr.send('alt=media');

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