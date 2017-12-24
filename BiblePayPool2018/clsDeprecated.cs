using Microsoft.VisualBasic;
using System;
using System.Data;
using System.Data.SqlClient;
using static BiblePayPool2018.Ext;

namespace BiblePayPool2018

{

    public class Deprecated
    {

        protected void MoveCompletedTrades(string sNetworkID)
        {
            try
            {
                string sql = "select * from trade where executiontxid is not null and len(ExecutionTxId) > 10";
                DataTable dt2 = clsStaticHelper.mPD.GetDataTable(sql);
                for (int i = 0; i < dt2.Rows.Count; i++)
                {
                    string sExeTxId = dt2.Rows[i]["ExecutionTxId"].ToString();
                    string sql7 = "exec InsTradeHistory '" + sExeTxId + "'";
                    clsStaticHelper.mPD.Exec(sql7);
                }
            }
            catch (Exception ex)
            {
                clsStaticHelper.Log(" Issue executing the instradehistory " + ex.Message);
            }
        }


        protected void SendMutatedTransactions(string sNetworkId)
        {
            return;
            MoveCompletedTrades(sNetworkId);
            string sql = "exec ordermatch '" + sNetworkId + "'";
            clsStaticHelper.mPD.Exec(sql);
            sql = "Update Trade set executionTxid=null,executed=null where executionTxid='SENDING'  or (ExecutionTxId='' and Executed is not null)";
            clsStaticHelper.mPD.Exec(sql);
            // Send out the mutated transactions to the traders
            sql = "Select * from Trade where networkid='" + sNetworkId + "' and executionTxId is null and escrowapproved is not null and act='BUY' and dateadd(minute,1,getdate()) > added";
            DataTable dt2 = clsStaticHelper.mPD.GetDataTable(sql);
            for (int i = 0; i < dt2.Rows.Count; i++)
            {
                double amount = ToDouble(dt2.Rows[i]["Total"]);
                string buyaddress = dt2.Rows[i]["Address"].ToString();
                string sID = dt2.Rows[i]["id"].ToString();
                string sSellGuid = dt2.Rows[i]["Match"].ToString(); //provided by buy record
                string sColor1 = ""; // BUY RECORD
                string sBuyTxId = dt2.Rows[i]["EscrowTxId"].ToString();
                double voutbuy = ToDouble(dt2.Rows[i]["vout"]);
                string sql20 = "Select * from  Trade where id = '" + sSellGuid + "'";
                DataTable dt3 = clsStaticHelper.mPD.GetDataTable(sql20);
                if (dt3.Rows.Count > 0)
                {
                    double sellamount = ToDouble(dt3.Rows[0]["Quantity"]) / 10;
                    string selladdress = dt3.Rows[0]["address"].ToString();
                    double voutsell = ToDouble(dt3.Rows[0]["vout"]);
                    string sColor2 = "401"; // SELL RECORD
                    string sSellEscrowTxId = dt3.Rows[0]["EscrowTxId"].ToString();
                    //update as sending:
                    sql = "Update Trade set Executed=getdate(),ExecutionTxId='SENDING' where id in ('" + sID + "','" + sSellGuid + "')";
                    clsStaticHelper.mPD.Exec(sql);
                    string sExeTxId = SendEscrowTx(sNetworkId, sBuyTxId, amount, selladdress, (int)voutbuy, sSellEscrowTxId, sellamount, buyaddress, (int)voutsell, sID, sColor1, sColor2);
                    string sExeTxId2 = sExeTxId;
                    sql = "Update Trade set Executed=getdate(),ExecutionTxId = '" + sExeTxId + "' where id in ('" + sSellGuid + "')";
                    sql += "Update Trade set Executed=getdate(),ExecutionTxId = '" + sExeTxId2 + "' where id in ('" + sID + "')";
                    clsStaticHelper.mPD.Exec(sql);
                }
            }
        }


        protected string SendEscrowTx(string sNetworkID, string sDependsOnTxId, double Amount, string Address, int VOUT1,
        string sDependsOnTxId2, double Amount2, string Address2, int VOUT2, string sTradeGuid, string sColor1, string sColor2)
        {
            if (sNetworkID != "test") return "";

            try
            {
                // Construct the complex tx depending on 2 and going to 2

                string sDependsOn2 = "<TXID>" + sDependsOnTxId2 + "</TXID><VOUT>" + VOUT2.ToString() + "</VOUT>";
                string XML = "<INPUTS><TXID>" + sDependsOnTxId + "</TXID><VOUT>" + VOUT1.ToString() + "</VOUT><ROW>";
                if (sDependsOnTxId2.Length > 0) XML += sDependsOn2;
                string sRecip2 = "<RECIPIENT>" + Address2 + "</RECIPIENT><COLOR>" + sColor2 + "</COLOR><AMOUNT>" + Amount2.ToString() + "</AMOUNT><ROW>";

                XML += "</INPUTS><RECIPIENTS><RECIPIENT>"
                    + Address + "</RECIPIENT><COLOR>" + sColor1 + "</COLOR><AMOUNT>" + Amount.ToString() + "</AMOUNT><ROW>";
                if (Address2.Length > 0) XML += sRecip2;

                XML += "</RECIPIENTS>";
                object[] oParams = new object[2];
                oParams[0] = "createescrowtransaction";
                oParams[1] = XML;
                string sCRTHex = clsStaticHelper.GetGenericInfo3(sNetworkID, "exec", oParams);
                object[] oParSign = new object[1];
                oParSign[0] = sCRTHex;
                string sBroadcastHex = clsStaticHelper.GetGenericInfo2(sNetworkID, "signrawtransaction", oParSign, "hex");
                if (sBroadcastHex.Length < 10)
                {
                    string sBroadcastErr = clsStaticHelper.GetGenericInfo3(sNetworkID, "signrawtransaction", oParSign);
                    string sql = "Update Trade set Err='Unable to signrawtransaction " + sCRTHex + "' where id = '" + sTradeGuid + "'";
                    clsStaticHelper.mPD.Exec(sql);
                    return "";
                }
                object[] oBroadcast = new object[1];
                oBroadcast[0] = sBroadcastHex;
                string sBroadcastTxId = clsStaticHelper.GetGenericInfo3(sNetworkID, "sendrawtransaction", oBroadcast);
                // Update the record as sent
                if (sBroadcastTxId == "")
                {
                    string sql = "Update Trade set Err = 'Unable to Broadcast sendrawtransaction' where id = '" + sTradeGuid + "'";
                    clsStaticHelper.mPD.Exec(sql);
                }
                return sBroadcastTxId;
            }
            catch (Exception ex)
            {
                clsStaticHelper.Log(" error while sending mutated tx " + ex.Message);
                return "ERR";
            }
        }
        

    }

}