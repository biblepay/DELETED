using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;


namespace USGDFramework
{
    public static class clsServiceOnly
    {
        public static void WarnAdopters(string sNetworkID, int iThreshhold)
        {
            // This sends out an email warning anyone who has not written for 30 days to write to child.  If after 30 days we must force the child to be abandoned.
            string sql = "select * from uz where letterdeadline < getdate()-" + iThreshhold.ToString() + " and adoptedorphanid is not null and (select count(*) from Letters inner join Orphans on Orphans.orphanid = Letters.orphanid where letters.userid = uz.id and letters.added > getdate()-"
                + iThreshhold.ToString() + " and len(body) > 100 and orphans.id = uz.AdoptedOrphanId)= 0";

            DataTable dt = Shared.mPD.GetDataTable2(sql);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string sUserId = dt.Rows[i]["id"].ToString();
                string sOrphanId = dt.Rows[i]["AdoptedOrphanId"].ToString();
                string s1 = "Select Name from Orphans where id = '" + sOrphanId + "'";
                string sName = Shared.mPD.GetScalarString2(s1, "Name", false);
                string sUserName = dt.Rows[i]["username"].ToString();
                string sEmail = dt.Rows[i]["Email"].ToString();
                string sBody = "Dear " + sUserName
                    + ",<br><br>The BiblePay Adoption System is notifying you that you have not written to "
                    + sName + " in over " + iThreshhold.ToString() + " day(s).  <br>Please consider writing to your child today.<br>"
                    + "<br>After 60 days, BiblePay will be forced to abandon the childs relationship from your account which could be dissapointing for a child.<br><br><br>Thank you for using BiblePay.<br><br>Best Regards,<br>BiblePay Support";

                bool sent = Shared.SendEmail(sEmail, "BiblePay Adoption System", sBody, true, true);
            }
        }
        public static List<string> MemorizeNonces(string sNetworkID)
        {
            string sql = "Update Work set Work.Userid = (Select Miners.Userid from Miners where Miners.id = Work.Minerid) where Work.UserId is null";
            Shared.mPD.Exec2(sql, false, false);
            sql = "Select distinct userid,nonce from Work where endtime is not null and validated=1 and error is null and endtime > getdate()-1/24.01/60";
            DataTable dt = Shared.mPD.GetDataTable2(sql, false);
            List<string> nonces = new List<string>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                double dNonce = Shared.GetDouble(dt.Rows[i]["nonce"]);
                string sUserId = dt.Rows[i]["UserId"].ToString();
                string sTotal = sUserId + dNonce.ToString();
                nonces.Add(sTotal);
            }
            return nonces;
        }
        public static bool ScanBlocksForPoolBlocksSolved(string sNetworkID, int iLookback)
        {
            //Find highest block solved
            List<string> poolAddresses = Shared.MemorizePoolAddresses(sNetworkID);
            int nTipHeight = Shared.GetTipHeight(sNetworkID);
            if (nTipHeight < 1)
            {
                return false;
            }
            if (sNetworkID == "main" & nTipHeight < 1000)
            {
                return false;
            }
            for (int nHeight = (nTipHeight - iLookback); nHeight <= nTipHeight; nHeight++)
            {
                string sql2 = "Select count(*) as Ct from Blocks where networkid='" + sNetworkID + "' and height='" + nHeight.ToString() + "'";
                double oCt = Shared.GetScalarDouble(sql2, "ct", false);
                if (oCt == 0)
                {
                    double cSub = Shared.GetDouble(Shared.GetBlockInfo(nHeight, "subsidy", sNetworkID));
                    //if recipient is pool...
                    string sRecipient = Shared.GetBlockInfo(nHeight, "recipient", sNetworkID);
                    for (int z = 0; z < poolAddresses.Count; z++)
                    {
                        string sPoolRecvAddress = poolAddresses[z];
                        if (sRecipient == sPoolRecvAddress)
                        {
                            string sMinerNameByHashPs = "?";

                            string sql1 = "Select isnull(Username,'') as UserName,isnull(MinerName,'') as MinerName from leaderboard" + sNetworkID + " (nolock) order by HPS desc";
                            DataTable dt10 = Shared.mPD.GetDataTable2(sql1);
                            if (dt10.Rows.Count > 0)
                            {
                                sMinerNameByHashPs = dt10.Rows[0]["Username"].ToString() + "." + dt10.Rows[0]["MinerName"].ToString();
                            }
                            // Populate the Miner_who_found_block field:
                            string sVersion = Shared.GetBlockInfo(nHeight, "blockversion", sNetworkID);
                            string sMinerGuid = Shared.GetBlockInfo(nHeight, "minerguid", sNetworkID);

                            sql1 = "Select isnull(miners.username,'NA') as MinerName,isnull( uz.username ,'NA') as UserName,Funded From miners with(nolock) "
                                + "  inner join Uz(nolock) on Uz.id = miners.userid WHERE miners.id = '" + sMinerGuid + "'";
                            dt10 = Shared.mPD.GetDataTable2(sql1);
                            string sMinerNameWhoFoundBlock = "?";
                            string sFunded = "0";
                            if (dt10.Rows.Count > 0)
                            {
                                string s1= (dt10.Rows[0]["UserName"] ?? "").ToString();
                                string s2 = (dt10.Rows[0]["MinerName"] ?? "").ToString();
                                sFunded = (dt10.Rows[0]["Funded"] ?? "0").ToString();
                                sMinerNameWhoFoundBlock = s1 + "." + s2;
                            }

                            string sql = "insert into blocks (id,funded,height,updated,subsidy,networkid,minernamebyhashps,minerid,blockversion,MinerNameWhoFoundBlock) values (newid(),'" + sFunded + "','"
                                + nHeight.ToString() + "',getdate(),'" + cSub.ToString() + "','" + sNetworkID + "','" + sMinerNameByHashPs
                                + "','" + sMinerGuid + "','" + sVersion + "','" + sMinerNameWhoFoundBlock + "')";
                            try
                            {
                                Shared.mPD.Exec2(sql, false, false);
                            }
                            catch (Exception ex)
                            {
                            }
                            AddBlockDistribution(nHeight, cSub, sNetworkID);
                        }
                    }

                }
            }

            Shared.PayBlockParticipants(sNetworkID);
            return true;
        }

        public static void ProcessOrders()
        {
            string sql = "Select * from Orders where processed is null and lastname is not null and address1 is not null";
            DataTable dt = Shared.mPD.GetDataTable2(sql, false);
            if (dt.Rows.Count < 1) return;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                try
                {
                    WalmartAPI.Form1.Order o = new WalmartAPI.Form1.Order();
                    o.FirstName = dt.Rows[i]["FirstName"].ToString();
                    o.LastName = dt.Rows[i]["LastName"].ToString();
                    o.Phone = dt.Rows[i]["Phone"].ToString();
                    o.Zip = dt.Rows[i]["Zip"].ToString();
                    o.State = dt.Rows[i]["State"].ToString();
                    o.Address1 = dt.Rows[i]["Address1"].ToString();
                    o.Address2 = dt.Rows[i]["Address2"].ToString();
                    o.City = dt.Rows[i]["City"].ToString();
                    o.CVC = "104";
                    o.OrderGuid = dt.Rows[i]["id"].ToString();
                    sql = "Update Orders set processed=getdate() where id='" + o.OrderGuid.ToString() + "'";
                    Shared.mPD.Exec2(sql);
                    BuyItem(o);

                }
                catch (Exception ex)
                {
                    string sErr = ex.Message;
                    sql = "Update orders set longerror='" + sErr + "',status1='ORDER_REFUSED_CLERICAL_ERROR' where id = '" 
                        + dt.Rows[i]["id"].ToString() + "'";
                    Shared.mPD.Exec2(sql);
                }
            }
        }


        public static void BuyItem(object j)
        {
            WalmartAPI.Form1.Order o = (WalmartAPI.Form1.Order)j;
            //had to make this its own thread since walmart api takes 90 secs to actually buy the item.
            WalmartAPI.clsPublic W = new WalmartAPI.clsPublic();
            W.sourceOrder = o;
            string s1 = W.ProcessOrder();
            Shared.Log(" Service::Result of Order " + W.orderID + " ; Err " + W.error1 + ";");

            string sql = "Update Orders set updated=getdate(),Status2='" + W.error1 + "',status1='"
                + Shared.PurifySQL(W.orderID, 50) + "',MouseId = '" + Shared.PurifySQL(W.orderID, 50)
                + "' where ID = '" + Shared.GuidOnly(o.OrderGuid) + "'";
            Shared.mPD.Exec2(sql);

        }


        public static void AddBlockDistribution(long nHeight, double cBlockSubsidy, string sNetworkID)
        {
            //Ensure this block distribution does not yet exist
            string sql = "select count(*) as ct from block_distribution where height='" + nHeight.ToString() + "' and networkid='" + sNetworkID + "'";
            DataTable dt = new DataTable();
            dt = Shared.mPD.GetDataTable2(sql);
            if (dt.Rows.Count > 0)
            {
                if (Convert.ToInt32(dt.Rows[0]["ct"]) > 0)
                {
                    Shared.Log("Block already exists in block distribution block # " + nHeight.ToString());
                    //Throw New Exception("Block already exists in block distribution")
                    return;
                }
            }

            //First pre ascertain the total participants and total hashing power
            sql = "select count(*) as participants,isnull(sum(isnull(HPS" + sNetworkID + ",0)),0) as hashpower from Uz where HPS" + sNetworkID + " > 0";
            DataTable dtHashPower = new DataTable();
            dtHashPower = Shared.mPD.GetDataTable2(sql);
            double dParticipants = 0;
            double dTotalHPS = 0;
            if (dtHashPower.Rows.Count > 0)
            {
                try
                {
                    dParticipants = Convert.ToDouble(dtHashPower.Rows[0]["participants"]);
                    dTotalHPS = Convert.ToDouble(dtHashPower.Rows[0]["hashpower"]);
                }
                catch (Exception ex)
                {
                }
            }
            if (dParticipants == 0)
            {
                Shared.Log("No participants in block #" + nHeight.ToString());
                //   Throw New Exception("No Participants in block # " + Trim(nHeight))
                return;
            }

            // Normal Pool Fees here:
            double dFee = Shared.GetDouble(USGDFramework.clsStaticHelper.GetConfig("fee"));
            double dFeeAnonymous = Shared.GetDouble(USGDFramework.clsStaticHelper.GetConfig("fee_anonymous"));
            // Letter writing Pool Fees here:
            double dUpvotedCount = Shared.GetUpvotedLetterCount();
            double dRequiredCount = Shared.GetRequiredLetterCount();
            double dLWF = Shared.GetDouble(USGDFramework.clsStaticHelper.GetConfig("fee_letterwriting"));
            double dLetterFees = (dUpvotedCount < dRequiredCount) ? dLWF : 0;
            double dTotalLetterFeeCount = 0;
            //Ascertain payment per hash
            dynamic dPPH = cBlockSubsidy / (dTotalHPS + 0.01);
            //Loop through current uzers with HPS > 0 (those that are participating in this block) and log the hps, and the block info
            sql = "Select * from Uz where HPS" + sNetworkID + " > 0 order by username";
            dt = Shared.mPD.GetDataTable2(sql);
            double dTotalLetterWritingFees = 0;
            for (int x = 0; x <= dt.Rows.Count - 1; x++)
            {
                string sUserId = dt.Rows[x]["id"].ToString();
                string sUserName = dt.Rows[x]["username"].ToString();
                double hps = Convert.ToDouble(dt.Rows[x]["HPS" + sNetworkID]);
                double cloak = Shared.GetDouble(dt.Rows[x]["Cloak"].ToString());
                // Find if user has not written any letters
                double dMyLetterFees = 0;
                //Get Sub Stats for the user (MinerName and HPS)
                sql = "select avg(work.BoxHPS) HPS, MinerName, Funded from Work with (nolock)  inner join Miners on Miners.id = work.minerid " 
                    + " inner join Uz on Miners.Userid = Uz.Id And Uz.Id = '" + sUserId + "' " + " where Work.BoxHps > 0 AND Work.Networkid='" 
                    + sNetworkID + "' " + " Group by minername,Funded order by MinerName";
                DataTable dtStats = new DataTable();
                try
                {
                    dtStats = Shared.mPD.GetDataTable2(sql);
                    string sStats = "";
                    for (int y = 0; y <= dtStats.Rows.Count - 1; y++)
                    {
                        double dFunded = Shared.GetDouble(dtStats.Rows[y]["Funded"]);
                        string sFunded = (dFunded == 1) ? "[FABN=1]" : "";
                        string sRow = dtStats.Rows[y]["MinerName"].ToString() + " " + sFunded + ": "
                            + Shared.GetDouble(Math.Round(Convert.ToDouble(dtStats.Rows[y]["HPS"]), 0).ToString())
                            + " (" + Shared.GetDouble(Math.Round(hps, 0).ToString()) + ")";
                        sStats += sRow + "<br>";
                        if (sStats.Length > 3500) sStats = sStats.Substring(0, 3500);
                    }
                    double cMinerSubsidy = dPPH * hps;
                    double fee1 = dFee * cMinerSubsidy;
                    double fee2 = dFeeAnonymous * cMinerSubsidy;
                    double fee3 = dMyLetterFees * cMinerSubsidy;
                    double dTotalFees = (cloak == 1 ? (fee2 + fee1 + fee3) : (fee1 + fee3));
                    dTotalLetterFeeCount += 1;
                    dTotalLetterWritingFees += fee3;

                    cMinerSubsidy = cMinerSubsidy - dTotalFees;
                    if (cMinerSubsidy < 0)
                        cMinerSubsidy = 0;

                    sql = "Insert into Block_distribution (id,height,updated,block_subsidy,userid,username,hps,subsidy,paid,networkid,stats,pph) values (newid(),'"
                        + nHeight.ToString() + "',getdate(),'"
                        + cBlockSubsidy.ToString() + "','" + sUserId + "','"
                        + sUserName + "','"
                        + Math.Round(hps, 2).ToString()
                        + "','" + cMinerSubsidy.ToString()
                        + "',null,'" + sNetworkID + "','" + sStats + "','" + dPPH.ToString() + "')";
                    Shared.mPD.Exec2(sql, false, true);
                }
                catch (Exception ex)
                {
                    Shared.Log("AddBlockDistribution " + ex.Message);
                }
            }
            // Track total fees charged for letters so we can award accurate letter writing bounties:
            if (dTotalLetterWritingFees > 0)
            {
                sql = "Insert into LetterWritingFees (id,height,added,amount,networkid,quantity) values (newid(),'" + nHeight.ToString()
                    + "',getdate(),'" + dTotalLetterWritingFees.ToString() + "','" + sNetworkID + "','" + dTotalLetterFeeCount.ToString() + "')";
                Shared.mPD.Exec2(sql);
            }
        }

        public static bool MassPayment(string sNetworkID)
        {
            // For every Verified recipient owed > 100 , pay the payment percent to them once per day
            string sql = "Select balancemain as oldbalance,WithdrawalAddress,balancemain-isnull((select sum(amount) from TransactionLog where transactiontype = 'mining_credit' "
                + " and transactionlog.userid = uz.id and transactionLog.Added > getdate() - 1),0) owed,id,username "
                + " From Uz where balancemain > 100 and withdrawaladdress <> '' and withdrawaladdress is not null and withdrawaladdressvalidated=1 "
                + " order by balancemain desc ";
            DataTable dtMass = Shared.mPD.GetDataTable2(sql);
            double total = 0;
            Shared.Payment[] Payments = new Shared.Payment[dtMass.Rows.Count];
            int iPosition = 0;
            for (int i = 0; i < dtMass.Rows.Count; i++)
            {
                double Owed = Shared.GetDouble(dtMass.Rows[i]["owed"]) * .50;
                string sAddress = dtMass.Rows[i]["WithdrawalAddress"].ToString();
                bool bValid = Shared.ValidateBiblepayAddress(sAddress, sNetworkID);
                if (bValid)
                {
                    // ensure address is not duplicated either
                    for (int z = 0; z < Payments.Length; z++)
                    {
                        if (Payments[z].Address == sAddress && sAddress.Length > 0)
                        {
                            bValid = false;
                            break;
                        }
                    }
                    if (Owed > 100 && bValid)
                    {
                        total += Owed;
                        Shared.Payment p = new Shared.Payment();
                        p.Address = sAddress;
                        p.Amount = Owed;
                        p.UserId = dtMass.Rows[i]["id"].ToString();
                        p.UserName = dtMass.Rows[i]["username"].ToString();
                        p.OldBalance = Shared.GetDouble(dtMass.Rows[i]["oldbalance"]);
                        p.NewBalance = p.OldBalance - p.Amount;
                        Array.Resize<Shared.Payment>(ref Payments, iPosition + 1);

                        Payments[iPosition] = p;
                        iPosition++;
                    }
                }
            }
            string sTXID = "";

            try
            {
                sTXID = Shared.SendMany(Payments, sNetworkID, "pool", "MassPoolPayments");
            }
            catch (Exception ex)
            {
                Shared.Log("Unable to send tx id " + ex.Message);
            }

            int iHeight = 0;
            try
            {
                iHeight = Shared.GetTipHeight(sNetworkID);
            }
            catch (Exception ex2)
            {

            }
            if (iPosition < 1) return true;

            try
            {
                sql = "insert into MassPayments (id,added,Recipients,Total,TXID) values (newid(),getdate(),'"
                    + (iPosition + 1).ToString() + "','" + total.ToString() + "','" + sTXID + "')";
                Shared.mPD.Exec2(sql);
            }
            catch (Exception ex3)
            {

            }
            if (sTXID.Length > 5)
            {
                for (int i = 0; i <= iPosition; i++)
                {
                    try
                    {
                        if (Payments[i].Amount > 0)
                        {
                            bool bAdj = Shared.AdjustUserBalance(sNetworkID, Payments[i].UserId, -1 * Payments[i].Amount);
                            Shared.InsTxLog(Payments[i].UserName, Payments[i].UserId, sNetworkID,
                                iHeight, sTXID, Payments[i].Amount, Payments[i].OldBalance, Payments[i].NewBalance,
                                Payments[i].Address, "withdrawal", "auto_withdrawal");
                        }
                    }
                    catch (Exception ex4)
                    {
                        Shared.Log("mass payments: " + ex4.Message);
                    }
                }

            }
            return true;

        }

        public static void TitheExtraBalances(string sNetworkID)
        {
            string sql = "Select id,balancemain from uz where id not in (select distinct userid from transactionLog (nolock) where added > getdate()-90 and transactiontype <> 'ORPHAN_TITHE') and balancemain > 1000 ";
            DataTable dt = Shared.mPD.GetDataTable2(sql);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string sLetterId = Guid.NewGuid().ToString();
                string sId = dt.Rows[i]["id"].ToString();
                double dDockAmount = -1 * (Shared.GetDouble(dt.Rows[i]["balancemain"]) * .02);
                Shared.AwardBounty(sLetterId, sId, dDockAmount, "ORPHAN_TITHE", 0, sLetterId, "main", "ORPHAN_TITHE " + sLetterId, false);

            }
        }


    }
}
