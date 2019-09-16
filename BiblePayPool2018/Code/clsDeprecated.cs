using Microsoft.VisualBasic;
using System;
using System.Data;
using System.Data.SqlClient;
using static USGDFramework.Shared;

namespace BiblePayPool2018

{

    public class Deprecated
    {
        public struct Trade
        {
            public string Hash;
            public int Time;
            public string Action;
            public string Symbol;
            public double Quantity;
            public double Price;
            public double Total;
            public string Address;
            public string EscrowTXID;
            public double VOUT;
        }

        /*
         case "order":
                        Trade t = DepersistTrade(sSolution);
        // if action == CANCEL, cancel the orders
        t.Action = t.Action.ToUpper();
                        if (t.Action == "CANCEL")
                        {
                            string sql4 = "Delete from Trade where quantity='" + t.Quantity.ToString() + "' address='" + t.Address + "' and symbol='" + t.Symbol
                                + "' and networkid='" + sNetworkID + "' and (match is null and matchell is null) and escrowtxid is null and executed is null \r\n ";
         mPD.Exec(sql4);
                        }
                        else
                        {
                            // delete any records for this address with same symbol and action
                            string sql4 = "Delete from Trade where address='" + t.Address + "' and networkid = '" + sNetworkID + "' and symbol='" + t.Symbol + "' and act='"
                                + t.Action + "' and quantity = '" + t.Quantity.ToString() + "' and (match is null and matchsell is null) \r\n";
     mPD.ExecResilient(sql4);
                            sql4 = "Insert into Trade (id,address,hash,time,added,ip,act,symbol,quantity,price,total,networkid) values (newid(),'"
                                + t.Address + "','" + t.Hash + "','" + t.Time.ToString() + "',getdate(),'','"
                                + t.Action + "',"
                                + "'" + t.Symbol + "','" + t.Quantity.ToString() + "','" + t.Price.ToString() + "','" + t.Total.ToString() + "','" + sNetworkID + "')";
                             mPD.ExecResilient(sql4);
                            sql4 = "exec ordermatch '" + sNetworkID + "'";
                             mPD.ExecResilient(sql4);
                        }
                        string sTradeResponse = "<RESPONSE>OK</RESPONSE></EOF></HTML>";
                        Response.Write(sTradeResponse);
                        break;
                        */

            /*
        protected void MoveCompletedTrades(string sNetworkID)
        {
            try
            {
                string sql = "select * from trade where executiontxid is not null and len(ExecutionTxId) > 10";
                DataTable dt2 =  mPD.GetDataTable(sql);
                for (int i = 0; i < dt2.Rows.Count; i++)
                {
                    string sExeTxId = dt2.Rows[i]["ExecutionTxId"].ToString();
                    string sql7 = "exec InsTradeHistory '" + sExeTxId + "'";
                     mPD.Exec(sql7);
                }
            }
            catch (Exception ex)
            {
                 Log(" Issue executing the instradehistory " + ex.Message);
            }
        }
        */


        Trade DepersistTrade(string sSolution)
        {
            Trade t = new Trade();
            t.Hash =  ExtractXML(sSolution, "hash=", ",").ToString();
            t.Time = (int)ToDouble( ExtractXML(sSolution, "time=", ","));
            t.Action =  ExtractXML(sSolution, "action=", ",").ToString();
            t.Symbol =  ExtractXML(sSolution, "symbol=", ",").ToString();
            t.Quantity = ToDouble( ExtractXML(sSolution, "quantity=", ","));
            t.Address =  ExtractXML(sSolution, "address=", ",").ToString();
            t.Price = ToDouble( ExtractXML(sSolution, "price=", ","));
            t.EscrowTXID =  ExtractXML(sSolution, "escrowtxid=", ",").ToString();
            t.VOUT = (int)ToDouble( ExtractXML(sSolution, "vout=", ","));
            t.Total = t.Price * t.Quantity;
            return t;
        }

        /*
        protected void SendMutatedTransactions(string sNetworkId)
        {
            return;
            MoveCompletedTrades(sNetworkId);
            string sql = "exec ordermatch '" + sNetworkId + "'";
             mPD.Exec(sql);
            sql = "Update Trade set executionTxid=null,executed=null where executionTxid='SENDING'  or (ExecutionTxId='' and Executed is not null)";
             mPD.Exec(sql);
            // Send out the mutated transactions to the traders
            sql = "Select * from Trade where networkid='" + sNetworkId + "' and executionTxId is null and escrowapproved is not null and act='BUY' and dateadd(minute,1,getdate()) > added";
            DataTable dt2 =  mPD.GetDataTable(sql);
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
                DataTable dt3 =  mPD.GetDataTable(sql20);
                if (dt3.Rows.Count > 0)
                {
                    double sellamount = ToDouble(dt3.Rows[0]["Quantity"]) / 10;
                    string selladdress = dt3.Rows[0]["address"].ToString();
                    double voutsell = ToDouble(dt3.Rows[0]["vout"]);
                    string sColor2 = "401"; // SELL RECORD
                    string sSellEscrowTxId = dt3.Rows[0]["EscrowTxId"].ToString();
                    //update as sending:
                    sql = "Update Trade set Executed=getdate(),ExecutionTxId='SENDING' where id in ('" + sID + "','" + sSellGuid + "')";
                     mPD.Exec(sql);
                    string sExeTxId = SendEscrowTx(sNetworkId, sBuyTxId, amount, selladdress, (int)voutbuy, sSellEscrowTxId, sellamount, buyaddress, (int)voutsell, sID, sColor1, sColor2);
                    string sExeTxId2 = sExeTxId;
                    sql = "Update Trade set Executed=getdate(),ExecutionTxId = '" + sExeTxId + "' where id in ('" + sSellGuid + "')";
                    sql += "Update Trade set Executed=getdate(),ExecutionTxId = '" + sExeTxId2 + "' where id in ('" + sID + "')";
                     mPD.Exec(sql);
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
                string sCRTHex =  GetGenericInfo3(sNetworkID, "exec", oParams);
                object[] oParSign = new object[1];
                oParSign[0] = sCRTHex;
                string sBroadcastHex =  GetGenericInfo2(sNetworkID, "signrawtransaction", oParSign, "hex");
                if (sBroadcastHex.Length < 10)
                {
                    string sBroadcastErr =  GetGenericInfo3(sNetworkID, "signrawtransaction", oParSign);
                    string sql = "Update Trade set Err='Unable to signrawtransaction " + sCRTHex + "' where id = '" + sTradeGuid + "'";
                     mPD.Exec(sql);
                    return "";
                }
                object[] oBroadcast = new object[1];
                oBroadcast[0] = sBroadcastHex;
                string sBroadcastTxId =  GetGenericInfo3(sNetworkID, "sendrawtransaction", oBroadcast);
                // Update the record as sent
                if (sBroadcastTxId == "")
                {
                    string sql = "Update Trade set Err = 'Unable to Broadcast sendrawtransaction' where id = '" + sTradeGuid + "'";
                     mPD.Exec(sql);
                }
                return sBroadcastTxId;
            }
            catch (Exception ex)
            {
                 Log(" error while sending mutated tx " + ex.Message);
                return "ERR";
            }
        }
        */

    }

}