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

            chrome.identity.getAuthToken({ 'interactive': true }, function (token) {
                
                console.log(token);
                if (token) {
                    this.accessToken = token;
                   // opt_callback && opt_callback();

                    var xhr = new XMLHttpRequest();

                   // xhr.mimeType = "application/vnd.google-apps.document";

                    //the second param is specific to doc
                    xhr.open("GET", "https://www.googleapis.com/drive/v3/files/" + fileId + "/export?alt=media&mimeType=text%plain", true);
                    xhr.setRequestHeader("Authorization", "Bearer " + token);

   
                    xhr.overrideMimeType('text/plain; charset=x-user-defined');
                   // xhr.responseType = 'blob';
                    xhr.onload = function () {
                        console.log(xhr);
                        var reader = new FileReader();
                        reader.onload = function () {

                            console.log(reader.result); // dataURI

                        }
                        reader.readAsDataURL(this.response);
                       // console.log(xhr.responseText);
                    }
                    xhr.onreadystatechange = function () {
                        console.log(xhr);
                        if (xhr.readyState == 4) {
                            save(xhr.response);
                        }
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

function base64ArrayBuffer(arrayBuffer) {
    var base64 = ''
    var encodings = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/'

    var bytes = new Uint8Array(arrayBuffer)
    var byteLength = bytes.byteLength
    var byteRemainder = byteLength % 3
    var mainLength = byteLength - byteRemainder

    var a, b, c, d
    var chunk

    // Main loop deals with bytes in chunks of 3
    for (var i = 0; i < mainLength; i = i + 3) {
        // Combine the three bytes into a single integer
        chunk = (bytes[i] << 16) | (bytes[i + 1] << 8) | bytes[i + 2]

        // Use bitmasks to extract 6-bit segments from the triplet
        a = (chunk & 16515072) >> 18 // 16515072 = (2^6 - 1) << 18
        b = (chunk & 258048) >> 12 // 258048   = (2^6 - 1) << 12
        c = (chunk & 4032) >> 6 // 4032     = (2^6 - 1) << 6
        d = chunk & 63               // 63       = 2^6 - 1

        // Convert the raw binary segments to the appropriate ASCII encoding
        base64 += encodings[a] + encodings[b] + encodings[c] + encodings[d]
    }

    // Deal with the remaining bytes and padding
    if (byteRemainder == 1) {
        chunk = bytes[mainLength]

        a = (chunk & 252) >> 2 // 252 = (2^6 - 1) << 2

        // Set the 4 least significant bits to zero
        b = (chunk & 3) << 4 // 3   = 2^2 - 1

        base64 += encodings[a] + encodings[b] + '=='
    } else if (byteRemainder == 2) {
        chunk = (bytes[mainLength] << 8) | bytes[mainLength + 1]

        a = (chunk & 64512) >> 10 // 64512 = (2^6 - 1) << 10
        b = (chunk & 1008) >> 4 // 1008  = (2^6 - 1) << 4

        // Set the 2 least significant bits to zero
        c = (chunk & 15) << 2 // 15    = 2^4 - 1

        base64 += encodings[a] + encodings[b] + encodings[c] + '='
    }

    return base64
}