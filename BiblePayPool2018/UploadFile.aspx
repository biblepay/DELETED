<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="UploadFile.aspx.cs" Inherits="BiblePayPool2018.UploadFile" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server" method="post" enctype="multipart/form-data" runat="server">
         <INPUT type=file id=File1 name=File1 runat="server" >
           <br>
       
         <asp:Button ID="Button1" runat="server" OnClick="Button1_Click" Text="Upload Files" />
    </form>
</body>
</html>
    