using System;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.DataVisualization.Charting;
using System.Data;
using System.Web.SessionState;
using static USGDFramework.Shared;

namespace BiblePayPool2018
{
    public class BiblePayUniv : USGDGui, IRequiresSessionState
    {
        public BiblePayUniv(SystemObject S) : base(S)
        {
            S.CurrentHttpSessionState["CallingClass"] = this;
        }


        public struct structQuestion
        {
            public string Question;
            public List<string> MultipleChoice;
            public string Answer;
            public string Narr;
            public string Title;
            public bool EOF;
        }
        public WebReply TestList_RowClick()
        {
            string sId = Sys.LastWebObject.guid.ToString(); //Test ID
            // Intro screen;
            string sql = "Select * from Test where ID='" +  GuidOnly(sId) + "'";
            string name = Sys._data.GetScalarString2(sql, "Name");
            string q = Sys._data.GetScalarString2(sql, "Questions");
            string[] vQ = q.Split(new string[] { "<QUESTION>" }, StringSplitOptions.None);
            double t = (vQ.Length - 1) * .9;
            string sNarr = "This test takes approximately " + t.ToString() + " minutes to complete.";
            Section TRC = new Section("TRC", 1, Sys, this);
            AddLabel("TRC", "lbl2", "<p>", TRC, "<b>Welcome to Biblepay University's Christian Testing Center.<p> You can take Christian tests and be awarded in BBP! </b> <p><br> ");
            AddLabelWithCaptionWidth("TRC", "1", "<p>", TRC, "Bible Quiz: <font color=yellow>" + name + "</FONT><p><br><p>", 11);
            AddLabel("TRC", "lbl3", "<p>", TRC, "Current Reward: 0 BBP   ");
            AddLabel("TRC", "lbl4", "<p>", TRC, sNarr);
            AddLabel("TRC", "lbl5", "<p>", TRC, "To start this test, press the [TAKE TEST] button.  <p><p><br><p>");
            Edit geBtnTake = new Edit("TRC", Edit.GEType.Button, "btnTakeTest", "Take Test", Sys);
            Sys.SetObjectValue("ActiveTest", "Id", Sys.LastWebObject.guid.ToString());
            Sys.SetObjectValue("ActiveTest", "QuestionNumber", "1");
            geBtnTake.ExtraTD = true;
            TRC.AddControl(geBtnTake);
            return TRC.Render(this, true);
        }


        public structQuestion GetQuestion(string sTestId, int iQuestionID)
        {
            string sql = "Select * from Test where Id='" + sTestId + "'";
            DataTable dt = Sys._data.GetDataTable2(sql);
            structQuestion q = new structQuestion();

            if (dt.Rows.Count > 0)
            {
                string sQuestions = dt.Rows[0]["Questions"].ToString();
                string sAnswers = dt.Rows[0]["Answers"].ToString();
                string[] vQ = sQuestions.Split(new string[] { "<QUESTION>" }, StringSplitOptions.None);
                string[] vA = sAnswers.Split(new string[] { "<ANSWER>" }, StringSplitOptions.None);
                q.MultipleChoice = new List<string>();

                q.Title = dt.Rows[0]["Name"].ToString();
                if (iQuestionID > vQ.Length - 1)
                {
                    q.EOF = true;
                    return q;
                }
                string sQ1 = vQ[iQuestionID];
                sQ1 = sQ1.Replace("</QUESTION>", "");
                string[] vMultipleChoice = sQ1.Split(new string[] { "<ROW>" }, StringSplitOptions.None);
                q.Question = vMultipleChoice[0];
                for (int i = 1; i < vMultipleChoice.Length; i++)
                {
                    string s1 = vMultipleChoice[i];
                    s1 = s1.Replace("\r\n", "");
                    if (s1.Length > 1)
                    {
                        q.MultipleChoice.Add(s1);
                    }

                }
                string sAnswer = vA[iQuestionID];
                q.Narr =  ExtractXML(sAnswer, "<NARR>", "</NARR>").ToString();
                q.Answer =  ExtractXML(sAnswer, "<ANS>", "</ANS>").ToString();

            }
            return q;


        }

        private void AddLabel(string sSectionName, string sControlName, string sCaption, Section s, string sValue)
        {
            Edit lbl1 = new Edit(sSectionName, sControlName, Sys);
            lbl1.Type = Edit.GEType.Label;
            lbl1.CaptionText = sCaption;
            lbl1.TextBoxValue = sValue;
            s.AddControl(lbl1);
        }
        private void AddLabelWithCaptionWidth(string sSectionName, string sControlName, string sCaption, Section s, string sValue, int CaptionWidth)
        {
            Edit lbl1 = new Edit(sSectionName, sControlName, Sys);
            lbl1.Type = Edit.GEType.Label;
            lbl1.CaptionText = sCaption;
            lbl1.TextBoxValue = sValue;
            lbl1.CaptionWidthCSS = " style='width:" + CaptionWidth.ToString() + "px;'";
            s.AddControl(lbl1);
        }

        public WebReply btnTakeTest_Click()
        {
            string sTestId = Sys.GetObjectValue("ActiveTest", "Id");
            double dQID =  GetDouble(Sys.GetObjectValue("ActiveTest", "QuestionNumber"));
            Sys.SetObjectValue("ActiveTest", "QuestionNumber", "1");
            Sys.SetObjectValue("ActiveTest", "Mode", "TEST");
            return DisplayQuestion(sTestId, (int)dQID);

        }

        public WebReply TestList()
        {
            SQLDetective s = Sys.GetSectionSQL("Test List", "Test", string.Empty);
            string sql = "Select id,Name,Added from Test order by added";
            Weblist w = new Weblist(Sys);
            w.bShowRowSelect = false;
            w.bShowRowTrash = false;
            WebReply wr = w.GetWebList(sql, "Test List", "Test List", "", "Test", this, false);
            return wr;
        }

        public WebReply DisplayQuestion(string sTestGuid, int iQuestionNumber)
        {
            structQuestion q = new structQuestion();
            Section Question = new Section("Question", 1, Sys, this);
            q = GetQuestion(sTestGuid, iQuestionNumber);
            string sMode = Sys.GetObjectValue("ActiveTest", "Mode");
            AddLabelWithCaptionWidth("Question", "lblA", "<p>", Question, "Bible Quiz: " + q.Title.ToString(), 11);
            string sModeNarr = (sMode == "TEST") ? "TEST MODE" : "REVIEW MODE";
            AddLabel("Question", "lblE", "<p>", Question, "<b><h3>" + sModeNarr + "</h3> ");
            AddLabel("Question", "lblD", "<p>", Question, "Select one answer for each question.  Then click <NEXT> to Continue. ");
            AddLabel("Question", "lblQ", "<p>", Question, "<hr>");
            AddLabel("Question", "lblQ1", "<p>", Question, "<font color=yellow><b>" + q.Question + "</font>");
            AddLabel("Question", "lblQ2", "<p>", Question, "<hr>");
            // If this is the end of the test, display the results, and give them a chance to review the test.
            if (q.EOF)
            {
                // Grade
                int CorrectAnswers = 0;
                int TotalQuestions = iQuestionNumber - 1;
                for (int i = 1; i < iQuestionNumber - 1; i++)
                {
                    //todo add question count to this
                    string sKey = "ANS_Q" + i.ToString();
                    string sUserAnswer = Sys.GetObjectValue("ActiveTest", sKey);
                    structQuestion u = GetQuestion(sTestGuid, i);
                    bool bCorrect = (u.Answer == sUserAnswer);
                    if (bCorrect) CorrectAnswers++;

                }
                double dPercentage = Math.Round((CorrectAnswers / (TotalQuestions + .01)) * 100, 2);

                double dElapsed = 1;
                // Store the test as a test record

                string sNarr = "Congratulations! <br>You have completed the Test.<br>Correct Answers: " + CorrectAnswers.ToString() + "<br>Total Questions: " + TotalQuestions.ToString() + "<br>Score: " + dPercentage.ToString() + "%<br>Elapsed Time: " + dElapsed.ToString() + " minutes.";

                AddLabel("Question", "lblScore", "<p>", Question, sNarr);
                if (sMode == "TEST")
                {
                    string sql = "Insert into TestTaken (id,userid,testid,name,added,score) values (newid(),'" + Sys.UserGuid.ToString() + "','" + sTestGuid + "','" + q.Title + "',getdate(),'" + dPercentage.ToString() + "')";
                    Sys._data.Exec2(sql);

                }

                Edit geBtnReview = new Edit("Question", Edit.GEType.Button, "btnReview", "Review", Sys);
                geBtnReview.ExtraTD = true;
                Question.AddControl(geBtnReview);
            }

            if (!q.EOF)
            {
                for (int i = 0; i < q.MultipleChoice.Count; i++)
                {
                    string sAnswer = q.MultipleChoice[i];
                    Sys.SetObjectValue("Question", "question", "");

                    if (sAnswer.Length > 1)
                    {

                        Edit r = new Edit("Question", Edit.GEType.Radio, Sys);
                        r.RadioName = "question";
                        r.Name = "r" + i.ToString();
                        r.TextBoxValue = sAnswer;
                        r.CaptionText = sAnswer;
                        Question.AddControl(r);

                    }

                }
                if (sMode == "REVIEW")
                {
                    //Add the answer below
                    string sAnsNarr = "The correct answer is: <b><font color=orange>" + q.Answer + "</font></b>";
                    AddLabel("Question", "lblAnsNarr", "<p>", Question, sAnsNarr);

                }
                string sBtnNarr = (sMode == "TEST") ? "Next Question" : "Review Next Question";
                Edit geBtnNext = new Edit("Question", Edit.GEType.Button, "btnNextQuestion", sBtnNarr, Sys);
                geBtnNext.ExtraTD = true;
                Question.AddControl(geBtnNext);
            }

            return Question.Render(this, true);

        }

        public WebReply btnNextQuestion_Click()
        {
            // Glean the radio button response here
            string sMode = Sys.GetObjectValue("ActiveTest", "Mode");
            int iQN = (int)GetDouble(Sys.GetObjectValue("ActiveTest", "QuestionNumber"));
            if (sMode == "TEST")
            {
                string rb = Sys.GetObjectValue("Question", "question");
                Sys.SetObjectValue("ActiveTest", "ANS_Q" + iQN.ToString(), rb);
            }
            iQN++;
            Sys.SetObjectValue("ActiveTest", "QuestionNumber", iQN.ToString());
            string sTestId = Sys.GetObjectValue("ActiveTest", "Id");
            return DisplayQuestion(sTestId, iQN);
        }

        public WebReply btnReview_Click()
        {
            Sys.SetObjectValue("ActiveTest", "QuestionNumber", "1");
            int iQN = (int)GetDouble(Sys.GetObjectValue("ActiveTest", "QuestionNumber"));

            Sys.SetObjectValue("ActiveTest", "Mode", "REVIEW");
            string sTestId = Sys.GetObjectValue("ActiveTest", "Id");
            return DisplayQuestion(sTestId, iQN);
        }


    }
}