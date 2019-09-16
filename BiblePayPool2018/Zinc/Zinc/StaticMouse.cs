using Newtonsoft.Json;
using System;
using System.Reflection;
using System.Text;

namespace Zinc
{

    public class BiblePayMouse
    {
        private string token = "";


        public BiblePayMouse(string Token)
        {
            token = Token;
        }
        
        public struct payment_method
        {
            public string name_on_card;
            public string number;
            public string security_code;
            public int expiration_month;
            public int expiration_year;
            public bool use_gift;
        }
        
        public struct shipping_address
        {
            public string first_name;
            public string last_name;
            public string address_line1;
            public string address_line2;
            public string zip_code;
            public string city;
            public string state;
            public string country;
            public string phone_number;
        }
        
        public struct shipping
        {
            public string order_by;
            public int max_days;
            public double max_price;
        }
        
        public struct billing_address
        {
            public string first_name;
            public string last_name;
            public string address_line1;
            public string address_line2;
            public string zip_code;
            public string city;
            public string state;
            public string country;
            public string phone_number;
        }
        public struct webhooks
        {
            public string order_placed;
            public string order_failed;
            public string tracking_obtained;
        }
        public struct client_notes1
        {
            public string our_internal_order_id;
        }

        public struct retailer_credentials
        {
            public string email;
            public string password;
        }

        public struct Variants
        {
            public string dimension;
            public string value;
        }

        public struct Product
        {
            public string product_id;
            public int quantity;
        }

        public bool AbortOrder(string sOrderID)
        {
            string sURL = "https://api.zinc.io/v1/orders/" + sOrderID + "/abort";
            string sOut = GetMouseReply(sURL);
            dynamic o = Newtonsoft.Json.JsonConvert.DeserializeObject(sOut);
            bool aborted = o["_abort"];
            return aborted;
        }

        private string GetMouseReply(string sURL)
        {
            MyMouseClient wc = new MyMouseClient();
            wc.Token = token;
            wc.AddToken(wc);
            string sOut = "";
            try
            {
                sOut = wc.DownloadString(sURL);
            }
            catch (Exception ex)
            {
                sOut = ex.Message;
            }
            return sOut;
       
        }
        public string CancelOrder(string sOrderID)
        {

            string sURL = "https://api.zinc.io/v1/orders/" + sOrderID + "/cancel";
            string sOut = GetMouseReply(sURL);
            return sOut;
        }

        public string GetJsonValues(dynamic o, string sName)
        {
            string sOut = "";
            try
            {
                dynamic oSet = o[sName];
                foreach (dynamic o1 in oSet)
                {
                    sOut += o1.ToString() + ",";
                }
                if (sOut.Length > 1) sOut = sOut.Substring(0, sOut.Length - 1);
                return sOut;

            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public string GetStr(object o)
        {
            if (o == null) return "";
            return o.ToString();
        }

        public string GetJsonValue(dynamic o, string sName)
        {
            string sOut = "";
            try
            {
                sOut = o[sName].ToString();


                return sOut;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public struct MouseOutput
        {
            public string code;
            public string message;
            public string tracking;
            public dynamic mouse_object;
            public double price;
            public string product_details;
            public string URL;
            public string title;
            public string delivery_line_1;
            public string delivery_line_2;
            public string last_line;
            public bool dpv_match_code;
            public bool dpv_active;
            public double BBPPrice;
            public double BTCPrice;
            public double BBPSatoshi;
            public double ROI;
            public double CPMPer10k;
            public double RPMPer10k;

        }

        public MouseOutput GetOrderStatus(string sOrderID)
        {
            string sURL = "https://api.zinc.io/v1/orders/" + sOrderID + "";
            string sOut = GetMouseReply(sURL);
            dynamic o = Newtonsoft.Json.JsonConvert.DeserializeObject(sOut);
            MouseOutput mo = new MouseOutput();
            mo.message = "";
            mo.mouse_object = o;
            mo.code = GetJsonValue(o, "code");
            if (o["status_updates"] != null)
            {
                if (o["status_updates"].ToString().Length > 10)
                {
                    for (int i = 0; i <= 9; i++)
                    {
                        string sSU = "";
                        try
                        {
                            sSU = (o["status_updates"][i]["message"] ?? String.Empty).ToString();
                        }
                        catch(Exception ex)
                        {

                        }
                        mo.message += sSU + ";";
                    }
                }
            }
            if (o["message"] != null)
            {
                mo.message += o["message"].ToString() + ";";
            }
            try
            {
                string carrier = (o["tracking"][0]["carrier"]).ToString();
                string tracking = (o["tracking"][0]["tracking_number"]).ToString();
                mo.tracking = carrier + " " + tracking;
            }
            catch (Exception ex1)
            {

            }
            return mo;
        }

        public MouseOutput GetProductDetails(string sProductID, string sRetailer)
        {
            string sURL = "https://api.zinc.io/v1/products/" + sProductID + "?retailer=" + sRetailer;
            string sOut = GetMouseReply(sURL);
            // Glean URL and description 
            dynamic o = Newtonsoft.Json.JsonConvert.DeserializeObject(sOut);
            MouseOutput mo = new MouseOutput();
            mo.mouse_object = o;
            string sPrice = GetJsonValue(o, "price");
            mo.price = Convert.ToDouble("0" + sPrice);
            mo.product_details = GetJsonValue(o, "product_details");
            mo.title = GetJsonValue(o, "title");
            mo.URL = GetJsonValues(o, "images");
            return mo;
        }

        private string Serialize(dynamic o, Type structType, string sOuterName, bool AddTrailingComma)
        {
            FieldInfo[] fields = structType.GetFields();
            string sOut = "";
            if (sOuterName.Length > 0) sOut = "   \"" + sOuterName + "\"" + ": {\r\n";
            foreach (FieldInfo field in fields)
            {
                string sFieldName = field.Name;
                bool bMasked = false;
                var oField = o.GetType().GetField(sFieldName);
                object oValue = oField.GetValue(o);
                if (oValue == null) oValue = "";
                string sRow = ToJson(sFieldName, oValue);
                string sIndent = "   ";
                sOut += sIndent + sRow + ",\r\n";
                if (sFieldName == "quantity" && false)
                {
                    sOut += "     \"variants\": [\r\n]";
                }

            }
            sOut = sOut.Substring(0, sOut.Length - 3);
            if (sOuterName.Length > 0) sOut += "\r\n}";
            if (AddTrailingComma) sOut += ",\r\n";
            return sOut;
        }


        private string Order = "";
        private string QS(string value)
        {
            string sRow = "\"" + value + "\"" + ":";
            return sRow;
        }

        private string ToJson(string sField, object oValue)
        {
            string sType = oValue.GetType().ToString();
            string sValue = oValue.ToString();

            bool bString = (sType == "System.String") ? true : false;
            if (sType == "System.Boolean") sValue = sValue.ToLower();

            string sQ = "\"";
            string sRow = "\"" + sField + "\"" + ": " + (bString ? sQ : "") + sValue + (bString ? sQ : "");
            return sRow;
        }

        private void Add(string sKey, string sValue)
        {
            string sRow = "  \"" + sKey + "\"" + ": " + "\"" + sValue + "\"" + ",\r\n";
            Order += sRow;
        }

        private void Add(string sKey, double dValue)
        {
            string sRow = "  \"" + sKey + "\"" + ": " + dValue.ToString() + ",\r\n";
            Order += sRow;
        }

        private void Add(string sKey, bool bValue)
        {
            string sRow = "  \"" + sKey + "\"" + ": " + bValue.ToString().ToLower() + ",\r\n";
            Order += sRow;
        }

        public MouseOutput ValidateAddress(string sAddressLine1, string sAddressLine2, string City, string sState, string zip)
        {
            string sStreet = sAddressLine1 + " " + sAddressLine2;
            string sURL = "https://us-street.api.smartystreets.com/street-address?auth-id=70ca2ac1-c22f-2655-58b0-97e443b142dc&auth-token=UtdU4T1VBkuexBF9MJuy&"
            + "street=" + sStreet + "&city=" + City + "&state=" + sState + "&zipcode=" + zip;
            sURL = sURL.Replace(" ", "%20");
            MyMouseClient mc = new MyMouseClient();
            string sOut = mc.DownloadString(sURL);
            dynamic oDynamic = JsonConvert.DeserializeObject(sOut);
            MouseOutput o = new MouseOutput();
            o.mouse_object = oDynamic;

            if (oDynamic.ToString().Length < 10)
            {
                return o; //Bad address
            }

            o.delivery_line_1 = GetJsonValue(oDynamic[0], "delivery_line_1");
            o.last_line = GetJsonValue(oDynamic[0], "last_line");
            o.dpv_match_code = (GetStr(oDynamic[0]["analysis"]["dpv_match_code"]) == "Y") ? true : false;
            o.dpv_active = (GetStr(oDynamic[0]["analysis"]["active"]) == "Y") ? true : false;
            return o;

        }

        public string CreateOrder(string sOrderGuid, string retailer, Product[] products, double max_price,
               bool is_gift, string gift_message,
               shipping_address shippi_addr, shipping shipping1, payment_method pm1, billing_address ba1,
               retailer_credentials rc1, webhooks wh1, client_notes1 cn1)
        {
            Order = "{\r\n";
            Add("idempotency_key", sOrderGuid);
            Add("retailer", retailer);
            Order += QS("products") + " [\r\n";
            for (int i = 0; i <= products.Length - 1; i++)
            {
                Order += "{\r\n";
                Product p = products[i];
                Type structType = typeof(Product);
                string sRow = Serialize(products[i], structType, "", false);
                Order += sRow;
                Order += "\r\n}";
            }
            Order += "\r\n],\r\n";
            Add("max_price", max_price);
            Type tShipAddr = typeof(shipping_address);
            string s1 = Serialize(shippi_addr, tShipAddr, "shipping_address", true);
            Order += s1;
            Add("is_gift", is_gift);
            Add("gift_message", gift_message);
            //shipping
            Type tShp = typeof(shipping);
            string s2 = Serialize(shipping1, tShp, "shipping", true);
            Order += s2;
            //payment_method
            Type tPM = typeof(payment_method);
            s2 = Serialize(pm1, tPM, "payment_method", true);
            Order += s2;

            //billing_address
            Type tBA = typeof(billing_address);
            s2 = Serialize(ba1, tBA, "billing_address", true);
            Order += s2;
            //retailer_credentials
            Type tRC = typeof(retailer_credentials);
            s2 = Serialize(rc1, tRC, "retailer_credentials", true);
            Order += s2;

            // webhooks
            Type tWH = typeof(webhooks);
            s2 = Serialize(wh1, tWH, "webhooks", true);
            Order += s2;

            // client_notes
            Type tCN = typeof(client_notes1);
            s2 = Serialize(cn1, tCN, "client_notes", false);
            Order += s2;
            Order += "\r\n}\r\n";
            string sURL = "https://api.zinc.io/v1/orders";
            MyMouseClient wc = new MyMouseClient();
            wc.Token = token;
            wc.AddToken(wc);
            byte[] bytUp = Encoding.UTF8.GetBytes("" + Order + "");
            byte[] bDown = wc.UploadData(sURL, bytUp);
            string sData = System.Text.Encoding.Default.GetString(bDown);
            dynamic oResult = Newtonsoft.Json.JsonConvert.DeserializeObject(sData);
            string req_id = "";
            if (oResult != null)
            {
                req_id = (oResult["request_id"] ?? string.Empty).ToString();
            }
            return req_id;
        }
    }


    public class StaticMouse
    {
    }

    
    public class MyMouseClient : System.Net.WebClient
    {
        public string Token = "";

        public void AddToken(MyMouseClient wc)
        {
            string sUser = Token;
            string sPass = "";
            string clientString = sUser + ":" + sPass;
            byte[] clientEncode = Encoding.UTF8.GetBytes(clientString);
            var credentials = "Basic " + System.Convert.ToBase64String(clientEncode);
            string sHeader = "Authorization:" + credentials;
            wc.Headers.Add(sHeader);
        }

        protected override System.Net.WebRequest GetWebRequest(Uri uri)
        {
            System.Net.WebRequest w = base.GetWebRequest(uri);
            w.Timeout = 37000;
            AddToken(this);
            return w;
        }
    }

}
