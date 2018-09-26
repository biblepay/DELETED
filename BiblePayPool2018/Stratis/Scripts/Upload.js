
function addCompletedItem(target, fileObj, response) {
    if (response == "1") {
        var fName = fileObj.name;
        var tDiv = $('#' + target);
        if (fName.length > 25) {
            var sName = fName.substr(0, 10);
            var eName = fName.substr(fName.length - 12, fName.length - 1);
            fName = sName + '...' + eName;
        }
        tDiv.append('<div class="uploadifyQueueItem" style="width:90%; display:table-cell;" >' + fName + '</div>');
    }
}

function clearCompletedList(target) {
    var tDiv = $('#' + target);
    tDiv.empty();
}

function updateMaxCollection(target, subCnt) {

    var t1 = $('#' + target).uploadifySettings('scriptData');
    alert(t1);
}