
document.addEventListener("keydown", keyDownTextField, false);
//window.onbeforeunload = function () { return "Key disabled."; };
$(window).on('beforeunload', function ()
{
    var x = formunload();
    return x;
});


function keyDownTextField(e) {
    var keyCode = e.keyCode;
    if (keyCode == 13)
    {
        // Reserved - Handle the Enter Key
    }
    else if (e.ctrlKey && keyCode == 8)
    {
        // handle ctrl backspace
        alert('backspace');
    }
}

function FrameNav(sClass, sMethod, sGuid)
{
    // Argument 2 (The Method) tells c# to handle this request by invoking the named method
    post("1", "formload", "formevent", "post=FrameNav", sClass, sMethod, postcomplete, sGuid, '');
}

function formload()
{
        post("1", "formload", "formevent", "post=formload", "BiblePayPool2018.Home", 'FormLoad', postcomplete,"", '');
}

function formunload()
{

    //This detects the browser back button and the f5 button
    //return confirm('Are you sure you would like to refresh the browser (Not recommended)?');
    //"1", "formload", "formevent", "post=formload", "BiblePayPool2018.Home", 'FormLoad', postcomplete,"");
    //return false;
}

String.prototype.replaceAll = function (search, replacement)
{
        var target = this;
        return target.replace(new RegExp(search, 'g'), replacement);
};

function Replace2(source, find, replacement)
{
        for (var i = 0; i < 99; i++)
        {
            var begin = source;
            source = source.replace(find, replacement);
            if (begin == source) break;
        }
        return source;
}

function postdiv(o, sEventName, sClassName, sMethodName, sGUID, sData2)
{
        var sOut = "";
        var oDiv = $(o).closest("div");
        // For each input in the div, send as a row

        $(o).closest("div").find("input").each(function (index)
        {
            var sUSGDID = "";
            var sUSGDValue = "";
            var sControlValue = $(this)[0].value;
            var sControlType = $(this)[0].type;
            if (sControlType == "checkbox") sControlValue = $(this)[0].checked;
            var sName = $(this)[0].name;
            var sChecked = "";
            if (sControlType == "radio")
            {
                sName = "";
                sControlValue = "";

                if ($(this)[0].checked)
                {
                    sControlValue = $(this)[0].value;
                    sName = $(this)[0].name;

                }
            }

            var s = sName + "[COL]" + sControlValue + "[COL]" + sUSGDID + "[COL]" + sUSGDValue + "[COL]" + sChecked + "[ROW]";
            s = Replace2(s, '&', '[amp]');
            s = Replace2(s, '+', '[plus]');
            s = Replace2(s, '%', '[percent]');
            sOut += s;
        });
        // for each textarea
        $(o).closest("div").find("textarea").each(function (index)
        {
            var sUSGDID = "";
            var sUSGDValue = "";
            var s = $(this)[0].name + "[COL]" + $(this)[0].value + "[COL]" + sUSGDID + "[COL]" + sUSGDValue + "[COL][ROW]";
            sOut += s;
        });

        // For each select in the div, send as a row
        $(o).closest("div").find("select").each(function (index)
        {
            // Get the USGDID and USGDValue for the control
            var sUSGDID = $(this)[0].options[$(this)[0].selectedIndex].getAttribute('usgdid');
            var sUSGDValue = $(this)[0].options[$(this)[0].selectedIndex].getAttribute('usgdvalue');
            var s = $(this)[0].name + "[COL]" + $(this)[0].value + "[COL]" + sUSGDID + "[COL]"+ sUSGDValue + "[COL][ROW]";
            sOut += s;
        });

        if (oDiv.length == 0)
        {
            return;
        }

        // Extract the divname from current section
        sDivName = oDiv[0].id;

        // If this is a dialog, we need to find the parent div (not the dialog div)
        var oHidden = $("#hdialogclass");
        if (oHidden && oHidden[0] && sDivName != "progressbar")
        {
            // Find the parent div
            sDivName = oHidden[0].value;
        }

        // if the div name is empty OR if this is a breadcrumb, change the div to the first div on the page
        if (sDivName == "")
        {
            sDivName = "1"; //this happens when the client hits the breadcrumb, and the current divname is empty
        }
        post(sDivName,"postdiv",sEventName,sOut,sClassName,sMethodName, postcomplete,sGUID,sData2);
    }

    function postcomplete(sData)
    {
        // Extract the strongly typed c# object
        var myDataArray = JSON.parse(sData);
        if (myDataArray.length > 0)
        {
            for (var i=0; i < myDataArray.length; i++)
            {
                var myData = myDataArray[i];
                if (myData.action == "refresh")
                {
                    // Determine Which div we will refresh, and only update that section
                    var body = myData.body;
                    var js = myData.javascript;
                    var doappend = myData.doappend;
                    var breadcrumb = myData.breadcrumb;
                    if (myData.ClearScreen)
                    {
                        // Erase the content of all divs on this page in the ExpandableSections area:
                        $('[id=1]')[0].innerHTML = "";
                    }
                    // BREADCRUMB
                    if (breadcrumb && breadcrumb.length > 0)
                    {
                        $('[id=' + myData.breadcrumbdiv + ']')[0].innerHTML = breadcrumb;
                    }

                    if (myData.ApplicationMessage && myData.ApplicationMessage.length > 0)
                    {
                        $('[id=ApplicationMessage]')[0].innerHTML = myData.ApplicationMessage;
                    }
                    // END OF BREADCRUMB

                    // First, if section does not exist on the page:
                    var oBody = $('[id=\"' + myData.divname + '"]');
                    if (body.length > 0) {
                        if (oBody.length == 0)
                        {
                            $('[id=1]').append(body);
                        }
                        else {
                            if (oBody.length > 0 && false)
                            {
                                if (doappend == "true")
                                {
                                    try {

                                        $(this).dialog('destroy').remove();
                                        //$('[id=hdialogclass]').remove();
                                    }
                                    catch (e) {

                                    }
                                    oBody[length-1].remove();


                                    var hidden1 = "<input type='hidden' id='hdialogclass' name='hdialogclass' value='" + myData.divname + "'>";
                                    var addto = body + hidden1;
                                    oBody.append(addto);
                                }
                                else
                                {
                                    oBody[0].innerHTML = body;
                                }
                            }
                        }
                    }

                    if (js && js.length > 0)
                    {

                        eval(js);

                    }
                }
                else if (myData.action=="ACK")
                {
                    var c = "ack";
                }
             }
        }

    }

    function fileUpload(form, action_url, div_id, sReturnClass, sReturnMethod, sSectionName)
    {
        // This is a function that allows uploading files through an iframe asynchronously, and then the user receives the status in the same
        var iframe = document.createElement("iframe");
        iframe.setAttribute("id", "upload_iframe");
        iframe.setAttribute("name", "upload_iframe");
        iframe.setAttribute("width", "0");
        iframe.setAttribute("height", "0");
        iframe.setAttribute("border", "0");
        iframe.setAttribute("style", "width: 0; height: 0; border: none;");

        // Add to document...
        form.parentNode.appendChild(iframe);
        window.frames['upload_iframe'].name = "upload_iframe";
        iframeId = document.getElementById("upload_iframe");

        // Add event...
        var eventHandler = function ()
        {

            if (iframeId.detachEvent) iframeId.detachEvent("onload", eventHandler);
            else iframeId.removeEventListener("load", eventHandler, false);

            // Message from server...
            if (iframeId.contentDocument) {
                content = iframeId.contentDocument.body.innerHTML;
            } else if (iframeId.contentWindow) {
                content = iframeId.contentWindow.document.body.innerHTML;
            } else if (iframeId.document) {
                content = iframeId.document.body.innerHTML;
            }

            try{
                if (content)
                {
                    document.getElementById(div_id).innerHTML = content;
                }
            }
            catch(e)
            {
                alert('upload failed');
            }
            // Del the iframe...
        }

        if (iframeId.addEventListener) iframeId.addEventListener("load", eventHandler, true);
        if (iframeId.attachEvent) iframeId.attachEvent("onload", eventHandler);

        // Set properties of form
        form.setAttribute("target", "upload_iframe");
        form.setAttribute("action", action_url);
        form.setAttribute("method", "post");
        form.setAttribute("enctype", "multipart/form-data");
        form.setAttribute("encoding", "multipart/form-data");

        // Submit the form
        form.submit();
        var oDiv = document.getElementById(div_id);
        var oSection = document.getElementById(sSectionName);
        oDiv.innerHTML = "Uploading...";
        postdiv(oSection, 'buttonevent',sReturnClass,sReturnMethod,'','');
    }


    function USGDFileUploader(oForm, sURL, sDivName, sReturnClass, sReturnMethod, sSectionName)
    {
        var x = document.getElementById("myFile");
        var txt = "";
        if ('files' in x)
        {
            if (x.files.length == 0)
            {
                txt = "Select one or more files.";
            }
            else
            {
                for (var i = 0; i < x.files.length; i++)
                {
                    txt += "<br><strong>" + (i + 1) + ". file</strong><br>";
                    var file = x.files[i];
                    if ('name' in file) {
                        txt += "name: " + file.name + "<br>";
                    }
                    if ('size' in file) {
                        txt += "size: " + file.size + " bytes <br>";
                    }
                }
            }
        }
        else
        {
            if (x.value == "")
            {
                txt += "Select one or more files.";
            } else
            {
                txt += "The files property is not supported by your browser!";
                txt += "<br>The path of the selected file: " + x.value; // If the browser does not support the files property, it will return the path of the selected file instead.
            }
        }
        if (txt.length > 0)
        {
            document.getElementById("DivUploadControl").innerHTML = txt;
            fileUpload(oForm, sURL, sDivName, sReturnClass, sReturnMethod, sSectionName);
        }
        else
        {
            alert('list of files empty');
        }
    }

    function post(sDivName,sAction,sEventName,sData,sClassName,sMethodName,callbackfunc,sGUID,sData2)
    {
        // Convert to JSON
        var o = { "name": "", "action": "", "body": "", "classname": "", "eventname" : "", "methodname" : "" };
        o.action = sAction;
        o.body = sData;
        o.divname = sDivName;
        o.classname = sClassName;
        o.methodname = sMethodName;
        o.eventname = sEventName;
        o.guid = sGUID;
        o.data2 = sData2;
        o.orderby = '';
        var myJson = "post=" + JSON.stringify(o);
        var sData = "";

        $.post("pool.ashx", myJson, function (sData)
        {
            $(".result").html(sData);
            callbackfunc(sData);
            return sData;
        });

    }

