//check if user is on gogole drive tab when tab is created and when updated
chrome.tabs.onCreated.addListener(function callback)
callback = function (Tab tab) {
    var fileId = '1ZdR3L3qP4Bkq8noWLJHSr_iBau0DNT4Kli4SxNc2YEo';
    //download google file to this location
    var dest = fs.createWriteStream('/tmp/resume.pdf');
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
};

chrome.tabs.onUpdated.addListener(function callback2)
callback2 = function (integer tabId, object changeInfo, Tab tab) {... };
