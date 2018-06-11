document.addEventListener('DOMContentLoaded', function () {
    
   // var openDashButton = document.getElementById('openDash');
   // openDashButton.addEventListener('click', function () {
        


        chrome.tabs.getSelected(null, function (tab) {
            //tab.url can now be used to get url
            var url = tab.url;
            if (url.includes("docs.google.com")) {
                //google doc is open
                
                document.getElementById("addDoc").style.visibility = "visible";
            }
        });
   // }, false);

    var addDocButton = document.getElementById('addDoc');
    addDocButton.addEventListener('click', function () {
        chrome.tabs.getSelected(null, function (tab) {
        document.body.style.backgroundColor = "red";
            var urlSections = tab.url.split("/");
            var fileId = urlSections[urlSections.length - 2];

            const fs = cep_node.require('fs');

        var dest = fs.createWriteStream('Desktop/resume.pdf');
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
                .pipe(dest);
        });
    }, false);

}, false)