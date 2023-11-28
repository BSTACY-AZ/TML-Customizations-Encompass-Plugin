using System;
using System.Collections.Generic;
using EllieMae.Encompass.Automation;
using EllieMae.Encompass.ComponentModel;
using EllieMae.Encompass.Collections;
using EllieMae.Encompass.BusinessObjects.Loans.Logging;
using System.Windows.Forms;
using EllieMae.Encompass.BusinessObjects.Loans;
using EllieMae.Encompass.Client;
using EntrustLibrary;

namespace TML_Customizations
{
    [Plugin]
    public class TML_Customizations
    {
        //  Constructor

        public TML_Customizations()
        {
            EncompassApplication.LoanOpened += new EventHandler(Application_LoanOpened);
            EncompassApplication.LoanClosing += new EventHandler(Application_LoanClosing);
        }

        //  Encompass events

        private void Application_LoanOpened(object sender, EventArgs e)
        {
            // Get active loan lock info & current user id
            Loan loan = EncompassApplication.CurrentLoan;
            LoanLock loanLock = loan.GetCurrentLock();
            Session session = loan.Session;
            string currentUser = session.UserID;

            // Use Try-Catch to wrap anything that will invoke a change
            try
            {
                // Make sure loan is NOT newly created and user has lock control over the file 
                // Require this condition before making changes or subscribing to events that do
                if (loan.LoanNumber != "" && loanLock.LockedBy == currentUser)
                {
                    //Subscribe to Events
                    EncompassApplication.CurrentLoan.LogEntryChange += CurrentLoan_LogEntryChange;
                    EncompassApplication.CurrentLoan.FieldChange += new FieldChangeEventHandler(CurrentLoan_FieldChange);

                    CountConditions();
                }
            }
            catch (NullReferenceException)
            {
                // Catches System.NullReferenceException: Object reference not set to an instance of an object.
                // Caused by attempting to invoke changes on a file with a user having limited AccessRights
            }
        }

        private void CurrentLoan_LogEntryChange(object source, LogEntryEventArgs e)
        {
            if (e.LogEntry.EntryType == LogEntryType.UnderwritingCondition)
            {
                CountConditions();
            }
        }

        private void CurrentLoan_FieldChange(object source, FieldChangeEventArgs e)
        {
            if (e.FieldID == "CX.TMI.PLUGIN.ACTION")
            {
                if (e.NewValue == "Request STP")
                {
                    //Run the code/checks that we want to run at time of initial submission
                    RequestSTP();
                }
                else if (e.NewValue == "Sub to UW")
                {
                    //Run the code/checks that we want to run at time of initial submission
                    InitialSubmission();
                }
                else if (e.NewValue == "Resubmission")
                {
                    //Run the code/checks that we want to run at time of resubmission
                    Resubmission();
                }
                else if (e.NewValue == "Refinal")
                {
                    //Run the code/checks that we want to run at time of resubmission for refinal
                    Refinal();
                }
                else if (e.NewValue == "UW Cond Approval")
                {
                    //Run the code/checks that we want to run at time of conditional approval from UW
                    UWCondApproval();
                }
                else if (e.NewValue == "UW Final Approval")
                {
                    //Run the code/checks that we want to run at time of final approval from UW
                    UWFinalApproval();
                }
                else if (e.NewValue == "UW Refinal")
                {
                    UWRefinal();
                }
                else if (e.NewValue == "UW Suspense")
                {
                    //Run the code/checks that we want to run at time of suspense
                    UWSuspense();
                }
                else if (e.NewValue == "UW RFD")
                {
                    //Checks run for Recommended for Decline
                    UWRFD();
                }
                else if (e.NewValue == "Signing Request")
                {
                    //Checks run for signing request
                    SigningReq();
                }
                else if (e.NewValue == "Docs Request")
                {
                    //Checks run for signing request
                    DocsReq();
                }
                else if (e.NewValue == "Docs Out")
                {
                    //Checks run for docs out
                    DocsOut();
                }
                else if (e.NewValue == "Fund Loan")
                {
                    //Checks run for docs out
                    FundLoan();
                }
                else if (e.NewValue == "Initial Disclosures")
                {
                    InitialDisclosures();
                }
                else if (e.NewValue == "Locked LE")
                {
                    LockedLE();
                }
                else if (e.NewValue == "Log UWC Clear")
                {
                    LogUWCClear();
                }
                else if (e.NewValue == "Log UWC Clear")
                {
                    StoreUWValues();
                }
                else if (e.NewValue == "Export Status Hist")
                {
                    ExportHist("CX.LOAN.STATUS", "CX.LOAN.STATUS.HIST");
                }
                else if (e.NewValue == "Export EstVal Hist")
                {
                    ExportHist("1821", "CX.LOAN.ESTVAL.HIST");
                }
                else if (e.NewValue == "Export LP Hist")
                {
                    ExportHist("LoanTeamMember.Name.Loan Processor", "CX.LOAN.LP.HIST");
                }
                else if (e.NewValue == "Move File Proc/Closing")
                {
                    MoveFileProcClosing();
                }
                else if (e.NewValue == "Req Fields Test")
                {
                    ReqFieldsTest();
                }
            }
            else if (e.FieldID == "CX.WF.HS.CHECKLOCKDAYS" && e.NewValue == "X")
            {
                //Run Lock Day validation to identify the number of days left on lock
                //     Once # of days has been established, various other tests will reference that 
                //     value for various tests
                ValidateLockDaysRemaining();
            }
            else if (e.FieldID == "CX.WF.HS.DISCCHECK" && e.NewValue == "X")
            {
                //Run Disclosure Validation to check if 
                //     1) APR has increased by .125 or more since last disclosure
                //     2) Whether or not the rate has been locked since the last disclosure
                //     3) Whether or not the amortization type has changed since the last disclosure
                //     4) Whether or not there is currently a fee variance (LE or CD) which needs to be disclosed
                ValidateDisclosures_APR();
                ValidateDisclosures_LockDate();
                ValidateDisclosures_Product();
                ValidateDisclosures_Variance();
            }
            else if (e.FieldID == "CX.WF.HS.CHECKMAVENT" && e.NewValue == "X")
            {
                //Run Mavent Validation to check if 
                //     1) Mavent has been recently run
                ValidateMaventRun();
            }
            else if (e.FieldID == "CX.WF.HS.CHECKMVTRESULT")
            {
                //Run Mavent Validation to check if 
                //     2) Whether or not the Mavent passed 
                ValidateMaventResult();
            }
            else if (e.FieldID == "CX.WF.HS.MVTRESULT" && e.NewValue == "FAIL")
            {
                //Check Individual Mavent Reports when the mavent run has failed and the Current Status of the loan to determine if a hard stop should be applied/enforced  
                ValidateMaventHardStop();
            }
            else if (e.FieldID == "CX.WF.HS.MITEST" && e.NewValue == "X")
            {
                //Check Individual Mavent Reports when the mavent run has failed and the Current Status of the loan to determine if a hard stop should be applied/enforced   
                ValidateMI();
            }
        }

        private void Application_LoanClosing(object sender, EventArgs e)
        {
            EncompassApplication.CurrentLoan.FieldChange -= CurrentLoan_FieldChange;
            EncompassApplication.CurrentLoan.FieldChange -= Application_LoanOpened;
        }

        //  Custom methods

        private void CountConditions()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            LogUnderwritingConditions uwConditions = ln.Log.UnderwritingConditions;

            int totalPTACount = 0;
            int totalPTDCount = 0;
            int totalPTFCount = 0;
            int totalACCount = 0;
            int totalPTPCount = 0;
            int clearedPTACount = 0;
            int clearedPTDCount = 0;
            int clearedPTFCount = 0;
            int clearedACCount = 0;
            int clearedPTPCount = 0;

            foreach(UnderwritingCondition condition in uwConditions)
            {
                if (condition.PriorTo == "PTA")
                {
                    ++totalPTACount;

                    if (condition.Cleared || condition.Waived)
                        ++clearedPTACount;
                }
                else if (condition.PriorTo == "PTD")
                {
                    ++totalPTDCount;

                    if (condition.Cleared || condition.Waived)
                        ++clearedPTDCount;
                }
                else if (condition.PriorTo == "PTF")
                {
                    ++totalPTFCount;

                    if (condition.Cleared || condition.Waived)
                        ++clearedPTFCount;
                }
                else if (condition.PriorTo == "AC")
                {
                    ++totalACCount;

                    if (condition.Cleared || condition.Waived)
                        ++clearedACCount;
                }
                else if (condition.PriorTo == "PTP")
                {
                    ++totalPTPCount;

                    if (condition.Cleared || condition.Waived)
                        ++clearedPTPCount;
                }
            }

            int openPTACount = totalPTACount - clearedPTACount;
            //MessageBox.Show("There are " + clearedPTDCount.ToString() + " cleared PTDs out of " + totalPTDCount.ToString() + " total PTDs");
            int openPTDCount = totalPTDCount - clearedPTDCount;
            int openACCount = totalACCount - clearedACCount;
            int openPTFCount = totalPTFCount - clearedPTFCount;
            int openPTPCount;

            if (clearedPTPCount == totalPTPCount)
                openPTPCount = 0;
            else
                openPTPCount = totalPTFCount - clearedPTFCount;

            ln.Fields["CX.LOAN.OPENPTAS"].Value = openPTACount;
            ln.Fields["CX.LOAN.OPENPTDS"].Value = openPTDCount;
            ln.Fields["CX.LOAN.OPENPTFS"].Value = openPTFCount;
            ln.Fields["CX.LOAN.OPENACS"].Value = openACCount;
            ln.Fields["CX.LOAN.OPENPTPS"].Value = openPTPCount;
        }

        public void ReqFieldsTest()
        {
            //BEGIN Validate Req Fields Testing
            EntrustLibrary.FieldValidationResults results = EntrustLibrary.HelperMethods.ValidateReqFields(new List<string>() { "1543", "1544", "DU.LP.ID" });
            Macro.Alert(results.ToString());
            //END Validate Req Fields Testing
        }

        public void MoveFileProcClosing()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            List<string> errorMessages = new List<string>();
            //string strCurrentStatus = ln.Fields["CX.LOAN.STATUS"].ToString();
            string strCurrentUWStatus = ln.Fields["CX.UW.STATUS"].ToString();
            string strMsgConvMIFail = "MI is required when loan type is Conventional and LTV is > 80%.";
            string strMsgFHAMIFail = "Loan is missing monthly MI, upfront MI, or both. Both are required on FHA loans.";
            string strMsgOldMavent = "You must run a new Mavent report prior to moving this file to the Closing Department. Note that if the Mavent report does not pass, any failures will need to be addressed prior to moving the file.";
            string strMsgFailedMavent = "The most recent Mavent/Compliance report indicates that one or more compliance checks have failed. You must have a passing Mavent report prior to moving this file to the Closing Department.";
            string strMsgNoFinal = "You cannot move a file from Processing to Closing unless the UW Status is Final Approved. Current UW Status is " + strCurrentUWStatus + ".";
            string strLoanType = ln.Fields["1172"].ToString();
            string strMsgAPRTestFail = "APR has increased by .125 or more since the last disclosure. Redisclosure is required.";
            string strMsgLockTestFail = "No redisclosure has taken place since the rate was locked. Redisclosure is required.";
            string strMsgAmortTestFail = "Amortization type has changed sinces last disclosure. Redisclosure is required.";
            string strMsgMINotNeeded = "This is a Conventional loan with LTV/CLTV < 80% and Mortgage Insurance applied. Please remove the MI to continue.";
            decimal dLTV = ln.Fields["353"].ToDecimal();
            decimal dCLTV = ln.Fields["976"].ToDecimal();

            ValidateMI();
            string strMITestResult = ln.Fields["CX.WF.HS.MITESTRESULT"].ToString();

            if (strMITestResult == "FAIL" && strLoanType == "Conventional")
                errorMessages.Add(strMsgConvMIFail);
            else if (strMITestResult == "FAIL" && strLoanType == "FHA")
                errorMessages.Add(strMsgFHAMIFail);

            ValidateMaventRun();
            ValidateMaventResult();
            ValidateMaventHardStop();
            string strMvtRun = ln.Fields["CX.WF.HS.MVTRUN"].ToString();
            string strMvtHardStop = ln.Fields["CX.WF.HS.MVTHARDSTOP"].ToString();

            if (strMvtRun == "FAIL")
                errorMessages.Add(strMsgOldMavent);
            else if (strMvtHardStop == "Y")
                errorMessages.Add(strMsgFailedMavent);

            ValidateDisclosures_APR();
            string strAPRTestResult = ln.Fields["CX.WF.HS.APRTEST"].ToString();

            if (strAPRTestResult == "FAIL")
                errorMessages.Add(strMsgAPRTestFail);

            ValidateDisclosures_LockDate();
            string strLockTestResult = ln.Fields["CX.WF.HS.LOCKDISCTEST"].ToString();

            if (strLockTestResult == "FAIL")
                errorMessages.Add(strMsgLockTestFail);

            ValidateDisclosures_Product();
            string strAmortTestResult = ln.Fields["CX.WF.HS.AMORTDISCTEST"].ToString();

            if (strAmortTestResult == "FAIL")
                errorMessages.Add(strMsgAmortTestFail);

            if (strCurrentUWStatus != "Final Approved")
                errorMessages.Add(strMsgNoFinal);

            string strHasMI = ln.Fields["CX.WF.HS.HASMI"].ToString();

            if (strHasMI == "Y" && strLoanType == "Conventional" && dLTV < 80 && dCLTV < 80)
                errorMessages.Add(strMsgMINotNeeded);

            string strIntlDocsReqDate = ln.Fields["CX.HIST.STATUS.DOCSREQ"].ToString();
            string strIntlDocsOutDate = ln.Fields["CX.HIST.STATUS.DOCSOUT"].ToString();
            string strIntlDocsBackDate = ln.Fields["CX.HIST.STATUS.DOCSBACK"].ToString();
            string strStatusInClosing = ln.Fields["CX.LOAN.STATUS"].ToString();

            if (strIntlDocsReqDate != "//" && strIntlDocsOutDate == "//" && strIntlDocsBackDate == "//")
                strStatusInClosing = "Docs Requested";
            else if (strIntlDocsReqDate != "//" && strIntlDocsOutDate != "//" && strIntlDocsBackDate == "//")
                strStatusInClosing = "Docs Out";
            else if (strIntlDocsReqDate != "//" && strIntlDocsOutDate != "//" && strIntlDocsBackDate != "//")
                strStatusInClosing = "Docs Back";
            
            if (errorMessages.Count == 0)
            {
                ln.Fields["CX.LOAN.STATUS"].Value = strStatusInClosing;
                ln.Fields["CX.LOAN.DEPT"].Value = "Closing";
            }
            else
            {
                HelperMethods.DisplayValidationResults("Loan Data Validation", "Encompass Hard Stops", errorMessages);
            }
        }

        private void ExportHist(string inputFieldName, string outputFieldName)
        {
            Loan ln = EncompassApplication.CurrentLoan;
            string strLoanNumber = ln.Fields["364"].ToString();
            string resultString = "";
            AuditTrailEntryList entries = ln.AuditTrail.GetHistory(inputFieldName);

            foreach (AuditTrailEntry e in entries)
                resultString += (strLoanNumber + "," + e.Timestamp + "," + e.UserName + "," + e.UserID + "," + e.Field.FormattedValue + Environment.NewLine);

            ln.Fields[outputFieldName].Value = resultString;
        }

        public void RequestSTP()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            //MessageBox.Show("Sub to UW fired!");
            List<string> errorMessages = new List<string>();
            string strMsgConvMIFail = "MI is required when loan type is Conventional and LTV is > 80%.";
            string strMsgFHAMIFail = "Loan is missing monthly MI, upfront MI, or both. Both are required on FHA loans.";
            string strMsgRepriceNeeded = "You must price/re-price this loan prior to reqeusting STP. Changes to pricing will require redisclosure.";
            string strMsgFeeUpdateNeeded = "You must import/re-import title fees loan prior to requesting STP. Changes to fees will require redisclosure.";
            string strLoanType = ln.Fields["1172"].ToString();

            ValidateMI();
            string strMITestResult = ln.Fields["CX.WF.HS.MITESTRESULT"].ToString();

            if (strMITestResult == "FAIL" && strLoanType == "Conventional")
                errorMessages.Add(strMsgConvMIFail);
            else if (strMITestResult == "FAIL" && strLoanType == "FHA")
                errorMessages.Add(strMsgFHAMIFail);

            string strRepriceTestResult = ln.Fields["CX.REPRICED"].ToString();

            //MessageBox.Show(strRepriceTestResult);
            if (strRepriceTestResult != "X")
                errorMessages.Add(strMsgRepriceNeeded);

            string strTitleFeeTestResult = ln.Fields["CX.REIMPORTEDTITLEFEES"].ToString();
            //MessageBox.Show(strTitleFeeTestResult);

            if (strTitleFeeTestResult != "X")
                errorMessages.Add(strMsgFeeUpdateNeeded);

            if (errorMessages.Count == 0)
            {
                ln.Fields["CX.LOAN.STATUS"].Value = "STP Requested";
                ln.Fields["CX.LOAN.DEPT"].Value = "POD";
            }
            else
            {
                HelperMethods.DisplayValidationResults("Loan Data Validation", "Encompass Hard Stops", errorMessages);
            }
        }

        public void InitialDisclosures()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            //MessageBox.Show("Sub to UW fired!");
            List<string> errorMessages = new List<string>();
            string strMsgConvMIFail = "MI is required when loan type is Conventional and LTV is > 80%.";
            string strMsgFHAMIFail = "Loan is missing monthly MI, upfront MI, or both. Both are required on FHA loans.";
            string strMsgRepriceNeeded = "You must price/re-price this loan prior to sending initial disclosures.";
            string strMsgFeeUpdateNeeded = "You must import/re-import title fees loan prior to sending initial disclosures.";
            string strMsgMissingDemographicInfo = "One or more Borrowers/Co - Borrowers are missing Demographic Info. Please complete prior to issuing disclosures.";
            string strLoanType = ln.Fields["1172"].ToString();

            ValidateMI();
            string strMITestResult = ln.Fields["CX.WF.HS.MITESTRESULT"].ToString();

            if (strMITestResult == "FAIL" && strLoanType == "Conventional")
                errorMessages.Add(strMsgConvMIFail);
            else if (strMITestResult == "FAIL" && strLoanType == "FHA")
                errorMessages.Add(strMsgFHAMIFail);

            string strRepriceTestResult = ln.Fields["CX.REPRICED"].ToString();

            //MessageBox.Show(strRepriceTestResult);
            if (strRepriceTestResult != "X")
                errorMessages.Add(strMsgRepriceNeeded);

            string strTitleFeeTestResult = ln.Fields["CX.REIMPORTEDTITLEFEES"].ToString();
            //MessageBox.Show(strTitleFeeTestResult);

            if (strTitleFeeTestResult != "X")
                errorMessages.Add(strMsgFeeUpdateNeeded);

            bool bMissingBorDemo = false;
            bool bMissingCoBorDemo = false;

            foreach (BorrowerPair pair in ln.BorrowerPairs) //<- 6 total pairs
            {
                if (ln.Fields["CX.DEMOG.INFO.COMP.BOR"].ToString() != "X")
                    bMissingBorDemo = true;

                if (ln.Fields["CX.DEMOG.INFO.COMP.CBOR"].ToString() != "X")
                    bMissingCoBorDemo = true;
            }
                
            if (bMissingBorDemo == true || bMissingCoBorDemo == true)
                errorMessages.Add(strMsgMissingDemographicInfo);

            if (errorMessages.Count == 0)
            {
                ln.Fields["CX.KM.EXECSIGNATURE"].Value = "";
                ln.Fields["CX.KM.EXECSIGNATURE"].Value = "_EPASS_SIGNATURE;ENCOMPASSDOCS;EDISCLOSURES2";
            }
            else
            {
                HelperMethods.DisplayValidationResults("Loan Data Validation", "Encompass Hard Stops", errorMessages);
            }
        }

        public void LockedLE()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            //MessageBox.Show("Sub to UW fired!");
            List<string> errorMessages = new List<string>();
            string strMsgConvMIFail = "MI is required when loan type is Conventional and LTV is > 80%.";
            string strMsgFHAMIFail = "Loan is missing monthly MI, upfront MI, or both. Both are required on FHA loans.";
            string strMsgRepriceNeeded = "Data on this loan has changed since the rate was locked. Please ensure that pricing is validated on the Lock Comparison screen before sending the Locked LE";
            string strLoanType = ln.Fields["1172"].ToString();

            ValidateMI();
            string strMITestResult = ln.Fields["CX.WF.HS.MITESTRESULT"].ToString();

            if (strMITestResult == "FAIL" && strLoanType == "Conventional")
                errorMessages.Add(strMsgConvMIFail);
            else if (strMITestResult == "FAIL" && strLoanType == "FHA")
                errorMessages.Add(strMsgFHAMIFail);

            string strLockVarianceResult = ln.Fields["CX.LCKC.VARIANCE"].ToString();
            //MessageBox.Show(strRepriceTestResult);

            if (strLockVarianceResult == "Y")
                errorMessages.Add(strMsgRepriceNeeded);

            //string strTitleFeeTestResult = ln.Fields["CX.REIMPORTEDTITLEFEES"].ToString();

            if (errorMessages.Count == 0)
            {
                ln.Fields["LE1.X1"].Value = DateTime.Now.ToShortDateString();
                ln.Fields["3168"].Value = "Y";
                ln.Fields["3165"].Value = DateTime.Now.ToShortDateString();
                ln.Fields["LE1.X81"].Value = "Y";
                ln.Fields["CX.KM.EXECSIGNATURE"].Value = "";
                ln.Fields["CX.KM.EXECSIGNATURE"].Value = "_EPASS_SIGNATURE;ENCOMPASSDOCS;EDISCLOSURES2";
            }
            else
            {
                HelperMethods.DisplayValidationResults("Loan Data Validation", "Encompass Hard Stops", errorMessages);
            }
        }

        private void ValidateDisclosures_APR()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            //MessageBox.Show("Validate Disclosures APR fired");
            string strIntlDisclosureDate = ln.Fields["3977"].ToDate().ToShortDateString();

            if (strIntlDisclosureDate != "1/1/0001")
            {
                try
                {
                    //Variables for the APR Variance Test
                    decimal dDisclosedAPR = (decimal)ln.Fields["3121"].Value;
                    decimal dCurrentAPR = (decimal)ln.Fields["799"].Value;
                    decimal aprSpread = decimal.Subtract(dCurrentAPR, dDisclosedAPR);

                    //APR Variance Test
                    if (aprSpread >= .125m)
                    {
                        //MessageBox.Show("APR variance detected!!!!");
                        ln.Fields["CX.WF.HS.APRTEST"].Value = "FAIL";
                    }
                    else if (aprSpread < .125m)
                    {
                        //MessageBox.Show("No APR variance detected");
                        ln.Fields["CX.WF.HS.APRTEST"].Value = "PASS";
                    }
                }

                catch (Exception ex)
                {
                    MessageBox.Show("UNEXPECTED ERROR: " + ex.Message);
                }
            }
            else
            {
                ln.Fields["CX.WF.HS.APRTEST"].Value = "PASS";
            }
        }

        private void ValidateDisclosures_LockDate()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            //MessageBox.Show("Validate Disclosures Lock Date fired");
            string compareDate;
            string strLastCDSentDate = ln.Fields["CD1.X47"].ToDate().ToShortDateString();
            string lockDate = ln.Fields["761"].ToDate().ToShortDateString();

            if (strLastCDSentDate == "1/1/0001")
                compareDate = ln.Fields["LE1.X33"].ToDate().ToShortDateString();
            else
                compareDate = strLastCDSentDate;

            try
            {
                //Rate LE Lock Disclosed Test
                if (Convert.ToDateTime(lockDate) > Convert.ToDateTime(compareDate))
                {
                    //MessageBox.Show("Rate locked since last LE");
                    ln.Fields["CX.WF.HS.LOCKDISCTEST"].Value = "FAIL";
                }
                else if (Convert.ToDateTime(lockDate) <= Convert.ToDateTime(compareDate))
                {
                    //MessageBox.Show("LE Sent since lock or loan not locked");
                    ln.Fields["CX.WF.HS.LOCKDISCTEST"].Value = "PASS";
                }
            }
            catch(Exception e)
            {
                MessageBox.Show("UNEXPECTED ERROR: " + e.Message);
            }
        }

        private void ValidateDisclosures_Product()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            //MessageBox.Show("Validate Disclosures Product fired");
            string strIntlDisclosureDate = ln.Fields["3152"].ToDate().ToShortDateString();

            if (strIntlDisclosureDate != "1/1/0001")
            {
                try
                {
                    //Variables for the Product Test
                    string strLastDisclosedProduct = ln.Fields["4017"].Value.ToString();
                    string strCurrentProduct = ln.Fields["LE1.X5"].Value.ToString();

                    //APR Variance Test
                    if (strLastDisclosedProduct != strCurrentProduct)
                    {
                        //MessageBox.Show("Product changed since last disclosure!!!!");
                        ln.Fields["CX.WF.HS.AMORTDISCTEST"].Value = "FAIL";
                    }
                    else if (strLastDisclosedProduct == strCurrentProduct)
                    {
                        //MessageBox.Show("Product matches last disclosure");
                        ln.Fields["CX.WF.HS.AMORTDISCTEST"].Value = "PASS";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("UNEXPECTED ERROR: " + ex.Message);
                }
            }
            else
            {
                ln.Fields["CX.WF.HS.AMORTDISCTEST"].Value = "PASS";
            }
        }

        public void ValidateDisclosures_Variance()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            //MessageBox.Show("Validate Disclosures Variance fired");
            string strLastCDSentDate = ln.Fields["CD1.X47"].ToDate().ToShortDateString();

            if (strLastCDSentDate == "1/1/0001" || strLastCDSentDate == "//")
                //If there is no CD date then we should look for an LE fee variance rather than a CD fee variance
                try
                {
                    //MessageBox.Show("LE Variance Test Fired.");
                    //Variables for the Variance Test
                    decimal dLEVariance = ln.Fields["FV.X345"].ToDecimal();
                
                    //Rate LE Lock Disclosed Test
                    if (dLEVariance != 0)
                    {
                        //MessageBox.Show("There is an LE Fee Variance");
                        ln.Fields["CX.WF.HS.DISCFVTEST"].Value = "FAIL";
                    }
                    else if (dLEVariance == 0)
                    {
                        //MessageBox.Show("There is no LE Fee Variance");
                        ln.Fields["CX.WF.HS.DISCFVTEST"].Value = "PASS";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("UNEXPECTED ERROR: " + ex.Message);
                }
            else if (strLastCDSentDate != "1/1/0001")
            //If a CD has been sent then we should look for a CD fee variance rather than an LE fee variance
            try
            {
                //Variables for the Lock Date Test
                decimal dCDVariance = ln.Fields["FV.X347"].ToDecimal();
                decimal dAppliedCure = ln.Fields["FV.X366"].ToDecimal();

                //Rate LE Lock Disclosed Test
                if (dCDVariance != 0)
                {
                    if (dCDVariance <= dAppliedCure)
                    {
                        //There is a variance but a cure has been applied to cover it
                        ln.Fields["CX.WF.HS.DISCFVTEST"].Value = "PASS";
                    }
                    else
                    {
                        //MessageBox.Show("There is a CD Fee Variance");
                        ln.Fields["CX.WF.HS.DISCFVTEST"].Value = "FAIL";
                    }
                }
                else if (dCDVariance == 0)
                {
                    //MessageBox.Show("There is NO CD Fee Variance");
                    ln.Fields["CX.WF.HS.DISCFVTEST"].Value = "PASS";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("UNEXPECTED ERROR: " + ex.Message);
            }
        }

        public void ValidateMaventRun()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            //MessageBox.Show("Validate Mavent Run fired");
            string strNow = DateTime.UtcNow.ToString();
            bool bNoMavent;
            int iElapsedMinutes = 0;
            string strMaventDateTimeUTC = ln.Fields["CX.MAVENT.DATETIMEUTC"].ToString();

            if (strMaventDateTimeUTC == "")
            {
                bNoMavent = true;
            }
            else
            {
                bNoMavent = false;
                TimeSpan elapsedTime = DateTime.Parse(strNow).Subtract(DateTime.Parse(strMaventDateTimeUTC));
                string strElapsedMinutes = elapsedTime.Minutes.ToString();
                iElapsedMinutes = int.Parse(strElapsedMinutes);
            }

            if (iElapsedMinutes > 7 || bNoMavent == true)
            {
                ln.Fields["CX.WF.HS.MVTRUN"].Value = "FAIL";
            }
            else if (iElapsedMinutes <= 7)
            {
                ln.Fields["CX.WF.HS.MVTRUN"].Value = "PASS";
                ln.Fields["CX.WF.HS.CHECKMVTRESULT"].Value = "X";
            }
        }

        public void ValidateMaventResult()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            //MessageBox.Show("Validate Mavent Result fired");
            string strComplianceReviewResults = ln.Fields["COMPLIANCEREVIEW.X1"].ToString();
            //MessageBox.Show(strComplianceReviewResults);

            if (strComplianceReviewResults == "FAIL")
                ln.Fields["CX.WF.HS.MVTRESULT"].Value = "FAIL";
            else
                ln.Fields["CX.WF.HS.MVTRESULT"].Value = "PASS";
        }

        public void ValidateMaventHardStop()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            //MessageBox.Show("Mavent Hard Stop Test Fired!");
            string strCurrentStatus = ln.Fields["CX.LOAN.STATUS"].Value.ToString();
            //string strCurrentDept = ln.Fields["CX.LOAN.DEPT"].Value.ToString();
            string strATRQM = ln.Fields["COMPLIANCEREVIEW.X18"].Value.ToString();
            string strTILARESPA = ln.Fields["COMPLIANCEREVIEW.X14"].Value.ToString();
            string strHighCost = ln.Fields["COMPLIANCEREVIEW.X7"].Value.ToString();
            string strHigherPriced = ln.Fields["COMPLIANCEREVIEW.X16"].Value.ToString();
            string strStateRules = ln.Fields["COMPLIANCEREVIEW.X12"].Value.ToString();
            string strLicense = ln.Fields["COMPLIANCEREVIEW.X9"].Value.ToString();
            string strNMLS = ln.Fields["COMPLIANCEREVIEW.X17"].Value.ToString();
            string strGSE = ln.Fields["COMPLIANCEREVIEW.X6"].Value.ToString();
            string strEnterpriseRules = ln.Fields["COMPLIANCEREVIEW.X5"].Value.ToString();
            string strHMDA = ln.Fields["COMPLIANCEREVIEW.X8"].Value.ToString();
            string strOFAC = ln.Fields["COMPLIANCEREVIEW.X10"].Value.ToString();
            string strOther = ln.Fields["COMPLIANCEREVIEW.X11"].Value.ToString();
            string strLoanLocked = ln.Fields["2400"].Value.ToString();
            string strHMDAStatus = ln.Fields["1393"].Value.ToString();
            int iFailCount = 0;

            if (strLoanLocked == "Y" && strATRQM == "FAIL")
            {
                ++iFailCount;
            }
            
            if (strTILARESPA == "FAIL")
            {
                ++iFailCount;
            }
            
            if (strLoanLocked == "Y" && strHighCost == "FAIL")
            {
                ++iFailCount;
            }

            if (strLoanLocked == "Y" && strHigherPriced == "FAIL")
            {
                ++iFailCount;
            }

            if (strStateRules == "FAIL")
            {
                ++iFailCount;
            }

            if (strLicense == "FAIL")
            {
                ++iFailCount;
            }

            if (strNMLS == "FAIL")
            {
                ++iFailCount;
            }

            if (strGSE == "FAIL")
            {
                ++iFailCount;
            }

            if (strEnterpriseRules == "FAIL")
            {
                ++iFailCount;
            }

            if (strHMDA == "FAIL" && strHMDAStatus != "" && strHMDAStatus != "Active Loan")
            {
                ++iFailCount;
            }

            if (strOFAC == "FAIL" && (strCurrentStatus == "Final Approved" || strCurrentStatus == "Docs Requested" || strCurrentStatus == "Docs Out" || strCurrentStatus == "Docs Back"))
            {
                ++iFailCount;
            }

            if (strOther == "FAIL")
            {
                ++iFailCount;
            }

            if (iFailCount > 0)
            { 
                //MessageBox.Show("Mavent Hard Stop Count is " + iFailCount.ToString() + " You would be prevented from moving/updating this file!");
                ln.Fields["CX.WF.HS.MVTHARDSTOP"].Value = "Y";
            }
            else
            {
                ln.Fields["CX.WF.HS.MVTHARDSTOP"].Value = "N";
            }
        }

        public void ValidateMI()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            //MessageBox.Show("MI Test Fired!");
            string strLoanType = ln.Fields["1172"].ToString();
            decimal dLTV = ln.Fields["353"].ToDecimal();
            decimal dMIPremium = ln.Fields["337"].ToDecimal();
            decimal dMIMonthly = ln.Fields["232"].ToDecimal();
            string strLenderPaidMI = ln.Fields["3533"].ToString();

            if (strLoanType == "Conventional" && dLTV >= 80.01m && dMIMonthly == 0 && dMIPremium <= 0 && (strLenderPaidMI == "N" || strLenderPaidMI == ""))
            {
                ln.Fields["CX.WF.HS.MIREQUIRED"].Value = "Y";
                ln.Fields["CX.WF.HS.HASMI"].Value = "N";
                ln.Fields["CX.WF.HS.MITESTRESULT"].Value = "FAIL";
            }
            else if (strLoanType == "Conventional" && dLTV >= 80.01m && (dMIMonthly != 0 || dMIPremium != 0 || strLenderPaidMI == "Y")) 
            {
                ln.Fields["CX.WF.HS.MIREQUIRED"].Value = "Y";
                ln.Fields["CX.WF.HS.HASMI"].Value = "Y";
                ln.Fields["CX.WF.HS.MITESTRESULT"].Value = "PASS";
            }
            else if (strLoanType == "Conventional" && dLTV <= 80.0m)
            {
                ln.Fields["CX.WF.HS.MIREQUIRED"].Value = "N";
                ln.Fields["CX.WF.HS.HASMI"].Value = "N";
                ln.Fields["CX.WF.HS.MITESTRESULT"].Value = "PASS";
            }
            else if (strLoanType == "FHA" && (dMIMonthly == 0 || dMIPremium ==0))
            {
                ln.Fields["CX.WF.HS.MIREQUIRED"].Value = "Y";
                ln.Fields["CX.WF.HS.HASMI"].Value = "N";
                ln.Fields["CX.WF.HS.MITESTRESULT"].Value = "FAIL";
            }
            else if (strLoanType == "FHA" && (dMIMonthly != 0 && dMIPremium != 0))
            {
                ln.Fields["CX.WF.HS.MIREQUIRED"].Value = "Y";
                ln.Fields["CX.WF.HS.HASMI"].Value = "Y";
                ln.Fields["CX.WF.HS.MITESTRESULT"].Value = "PASS";
            }
            else if (strLoanType == "VA")
            {
                ln.Fields["CX.WF.HS.MIREQUIRED"].Value = "N";
                ln.Fields["CX.WF.HS.HASMI"].Value = "N";
                ln.Fields["CX.WF.HS.MITESTRESULT"].Value = "PASS";
            }
        }

        public void ValidateLockDaysRemaining()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            //MessageBox.Show("Validate Mavent Result fired");
            string strLockExpirationTest = ln.Fields["762"].ToString();

            if (strLockExpirationTest != "//")
            {
                DateTime dLockExpiration = ln.Fields["762"].ToDate();
                //MessageBox.Show(strComplianceReviewResults);
                double iLockDaysRem = (dLockExpiration - DateTime.Today).TotalDays;
                ln.Fields["CX.WF.HS.LOCKDAYSREM"].Value = iLockDaysRem;
            }
        }

        public void InitialSubmission()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            //MessageBox.Show("Sub to UW fired!");
            List<string> errorMessages = new List<string>();
            string strMsgOldMavent = "You must run a new Mavent report prior to submission/resubmission. Note that if the Mavent report does not pass, any failures will need to be addressed and the report re-ran prior to submission/resubmission.";
            string strMsgFailedMavent = "The most recent Mavent/Compliance report indicates that one or more compliance checks have failed. You must have a passing Mavent report prior to submission/resubmission.";
            string strMsgConvMIFail = "MI is required when loan type is Conventional and LTV is > 80%.";
            string strMsgFHAMIFail = "Loan is missing monthly MI, upfront MI, or both. Both are required on FHA loans.";
            string strMsgAPRTestFail = "APR has increased by .125 or more since the last disclosure. Redisclosure is required.";
            string strMsgLockTestFail = "No redisclosure has taken place since the rate was locked. Redisclosure is required.";
            string strMsgAmortTestFail = "Amortization type has changed sinces last disclosure. Redisclosure is required.";
            string strMsgFVTestFail = "There have been changes to fees since the last disclosures. COC Redisclosure is required.";
            string strMsgMINotNeeded = "This is a Conventional loan with LTV/CLTV < 80% and Mortgage Insurance applied. Please remove the MI to continue.";
            string strLoanType = ln.Fields["1172"].ToString();
            decimal dLTV = ln.Fields["353"].ToDecimal();
            decimal dCLTV = ln.Fields["976"].ToDecimal();
            MilestoneEvent milestone = ln.Log.MilestoneEvents.GetEventForMilestone("Submitted");

            ValidateMaventRun();
            ValidateMaventResult();
            ValidateMaventHardStop();
            string strMvtRun = ln.Fields["CX.WF.HS.MVTRUN"].ToString();
            string strMvtHardStop = ln.Fields["CX.WF.HS.MVTHARDSTOP"].ToString();

            if (strMvtRun == "FAIL")
                errorMessages.Add(strMsgOldMavent);
            else if (strMvtHardStop == "Y")
                errorMessages.Add(strMsgFailedMavent);

            ValidateMI();
            string strMITestResult = ln.Fields["CX.WF.HS.MITESTRESULT"].ToString();

            if (strMITestResult == "FAIL" && strLoanType == "Conventional" )
                errorMessages.Add(strMsgConvMIFail);
            else if (strMITestResult == "FAIL" && strLoanType == "FHA")
                errorMessages.Add(strMsgFHAMIFail);

            string strHasMI = ln.Fields["CX.WF.HS.HASMI"].ToString();

            if (strHasMI == "Y" && strLoanType == "Conventional" && dLTV < 80 && dCLTV < 80)
                errorMessages.Add(strMsgMINotNeeded);

            ValidateDisclosures_APR();
            string strAPRTestResult = ln.Fields["CX.WF.HS.APRTEST"].ToString();
            
            if (strAPRTestResult == "FAIL")
                errorMessages.Add(strMsgAPRTestFail);

            ValidateDisclosures_LockDate();
            string strLockTestResult = ln.Fields["CX.WF.HS.LOCKDISCTEST"].ToString();
            
            if (strLockTestResult == "FAIL")
                errorMessages.Add(strMsgLockTestFail);

            ValidateDisclosures_Product();
            string strAmortTestResult = ln.Fields["CX.WF.HS.AMORTDISCTEST"].ToString();

            if (strAmortTestResult == "FAIL")
                errorMessages.Add(strMsgAmortTestFail);

            ValidateDisclosures_Variance();
            string strFVTestResults = ln.Fields["CX.WF.HS.DISCFVTEST"].ToString();

            if (strFVTestResults == "FAIL")
                errorMessages.Add(strMsgFVTestFail);

            if (errorMessages.Count == 0)
            {
                ln.Fields["CX.UW.LOGSTATUS"].Value = DateTime.Now;
                ln.Fields["CX.LOAN.STATUS"].Value = "Submitted";
                ln.Fields["CX.LOAN.DEPT"].Value = "Underwriting";
                ln.Fields["CX.UW.STATUS"].Value = "To Be Assigned";
                milestone.AdjustDate(DateTime.Now, true, true);
                milestone.Completed = true;
                ln.Fields["CX.TRACKING.UW.SUBMSSIONS"].Value = "1";
            }
            else
            {
                HelperMethods.DisplayValidationResults("Loan Data Validation", "Encompass Hard Stops", errorMessages);
            }
        }

        public void UWCondApproval()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            //MessageBox.Show("Sub to UW fired!");
            List<string> errorMessages = new List<string>();
            string strMsgOldMavent = "You must run a new Mavent report prior to issuing conditional approval. Note that if the Mavent report does not pass, any failures will need to be addressed and the report re-ran prior to conditional approval.";
            //string strMsgFailedMavent = "The most recent Mavent/Compliance report indicates that one or more compliance checks have failed. You must have a passing Mavent report prior to issuing conditional approval.";
            string strMsgConvMIFail = "MI is required when loan type is Conventional and LTV is > 80%.";
            string strMsgFHAMIFail = "Loan is missing monthly MI, upfront MI, or both. Both are required on FHA loans.";
            string strQMFactorsFail = "You must validate that you have evaulated all ATR/QM factors prior to issuing Conditional Approval.";
            //string strMsgAPRTestFail = "APR has increased by .125 or more since the last disclosure. Redisclosure is required.";
            //string strMsgLockTestFail = "No redisclosure has taken place since the rate was locked. Redisclosure is required.";
            //string strMsgAmortTestFail = "Amortization type has changed sinces last disclosure. Redisclosure is required.";
            //string strMsgFVTestFail = "There have been changes to fees since the last disclosures. COC Redisclosure is required.";
            string strNoFEMACheck = "You must run a FEMA search against the property prior to issuing a loan decision.";
            string strCurrentUserName = EncompassApplication.CurrentUser.FullName;
            string strUWNotes = ln.Fields["CX.UW.NOTES.TMP"].ToString();
            string strLoanType = ln.Fields["1172"].ToString();

            ValidateMaventRun();
            //ValidateMaventResult();
            ValidateMaventHardStop();
            string strMvtRun = ln.Fields["CX.WF.HS.MVTRUN"].ToString();
            //string strMvtHardStop = ln.Fields["CX.WF.HS.MVTHARDSTOP"].ToString();

            if (strMvtRun == "FAIL")
                errorMessages.Add(strMsgOldMavent);
            //else if (strMvtHardStop == "Y")
            //{
                //MessageBox.Show(strMsgFailedMavent, "Encompass Hard Stop", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                //iHardStopCount++;
            //}

            ValidateMI();
            string strMITestResult = ln.Fields["CX.WF.HS.MITESTRESULT"].ToString();

            if (strMITestResult == "FAIL" && strLoanType == "Conventional")
                errorMessages.Add(strMsgConvMIFail);
            else if (strMITestResult == "FAIL" && strLoanType == "FHA")
                errorMessages.Add(strMsgFHAMIFail);

            //ValidateDisclosures_APR();
            //string strAPRTestResult = ln.Fields["CX.WF.HS.APRTEST"].ToString();

            //if (strAPRTestResult == "FAIL")
            //MessageBox.Show(strMsgAPRTestFail, "Encompass Hard Stop", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            //ValidateDisclosures_LockDate();
            //string strLockTestResult = ln.Fields["CX.WF.HS.LOCKDISCTEST"].ToString();

            //if (strLockTestResult == "FAIL")
            //MessageBox.Show(strMsgLockTestFail, "Encompass Hard Stop", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            //ValidateDisclosures_Product();
            //string strAmortTestResult = ln.Fields["CX.WF.HS.AMORTDISCTEST"].ToString();

            //if (strAmortTestResult == "FAIL")
            //MessageBox.Show(strMsgAmortTestFail, "Encompass Hard Stop", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            string strQMFactorsEval = ln.Fields["CX.LOAN.QMFACTORS.EVAL"].ToString();

            if (strQMFactorsEval != "Y")
                errorMessages.Add(strQMFactorsFail);
            
            //ValidateDisclosures_Variance();
            
            string strValidationLogCurrent = ln.Fields["CX.UWC.DATAVALIDATION.LOG"].ToString();
            string strEarliestExpirationDate = ln.Fields["CX.EXP.EARLIEST.DATE"].ToString();
            string strCurrentUserFullName = ln.Fields["CX.KM.USER.FULLNAME"].ToString();
            string strAUSResponse = ln.Fields["1544"].ToString();
            string strAUSType = ln.Fields["1543"].ToString();

            string strFEMAHasRun = ln.Fields["CX.FEMA.HASRUN"].ToString();
            if (strFEMAHasRun == "")
            {
                errorMessages.Add(strNoFEMACheck);
            }

            if (errorMessages.Count == 0)
            {
                ln.Fields["CX.UW.LOGSTATUS"].Value = DateTime.Now;
                ln.Fields["CX.LOAN.STATUS"].Value = "Approved";
                ln.Fields["CX.LOAN.DEPT"].Value = "Processing";
                ln.Fields["CX.UW.STATUS"].Value = "Approved";
                ln.Fields["CX.UW.STATUS.U"].Value = strCurrentUserName;
                ln.Fields["CX.UW.STATUS.D"].Value = DateTime.Now;
                ln.Fields["CX.UW.NOTES"].Value = DateTime.Now.ToString("f") + " - " + strCurrentUserName + Environment.NewLine + strUWNotes;
                ln.Fields["CX.UW.LOGSTATUS"].Value = DateTime.Now.ToString("f");
                ln.Fields["CX.KM.PRINTSCENARIO"].Value = "";
                ln.Fields["CX.KM.PRINTSCENARIO"].Value = "UW Conditional Approval";
                ln.Fields["2984"].Value = strCurrentUserName;
                ln.Fields["2301"].Value = DateTime.Now;
                ln.Fields["2302"].Value = strEarliestExpirationDate;
                ln.Fields["984"].Value = strCurrentUserFullName;
                StoreUWValues();

                if (strValidationLogCurrent != "")
                    ln.Fields["CX.UWC.DATAVALIDATION.LOG"].Value = strValidationLogCurrent + Environment.NewLine + Environment.NewLine + DateTime.Now.ToString("MM/dd/yyyy h:mm tt") + "~" + strCurrentUserName + "~UW Decision - Approval";
                else if (strValidationLogCurrent == "")
                    ln.Fields["CX.UWC.DATAVALIDATION.LOG"].Value = DateTime.Now.ToString("MM/dd/yyyy h:mm tt") + "~" + strCurrentUserName + "~UW Decision - Approval";

                if (strAUSResponse == "Approve/Eligible" || strAUSResponse == "Accept")
                    ln.Fields["3878"].Value = "Y";
                
                if (strAUSType == "Manual Underwriting")
                    ln.Fields["3880"].Value = "Y";
            }
            else
            {
                HelperMethods.DisplayValidationResults("Loan Data Validation", "Encompass Hard Stops", errorMessages);
            }
        }

        public void UWFinalApproval()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            //MessageBox.Show("Sub to UW fired!");
            List<string> errorMessages = new List<string>();
            string strMsgOldMavent = "You must run a new Mavent report prior to issuing Final Approval. Note that if the Mavent report does not pass, any failures will need to be addressed and the report re-ran prior to Final Approval.";
            string strMsgFailedMavent = "The most recent Mavent/Compliance report indicates that one or more compliance checks have failed. You must have a passing Mavent report prior to issuing Final Approval.";
            string strMsgConvMIFail = "MI is required when loan type is Conventional and LTV is > 80%.";
            string strMsgFHAMIFail = "Loan is missing monthly MI, upfront MI, or both. Both are required on FHA loans.";
            string strMsgAPRTestFail = "APR has increased by .125 or more since the last disclosure. Redisclosure is required.";
            string strMsgLockTestFail = "No redisclosure has taken place since the rate was locked. Redisclosure is required.";
            string strMsgAmortTestFail = "Amortization type has changed sinces last disclosure. Redisclosure is required.";
            string strMsgFVTestFail = "There have been changes to fees since the last disclosures. COC Redisclosure is required.";
            string strMsgFailingQM = "This loan does not currently meet Qualified Mortgage requirements. Please review a new Mavent/Compliance report for Alerts in the ATR/QM report section. Final Approval cannot be issued until this is addressed.";
            //string strLockDaysTestFail = "Final Approval cannot be issued with less than 7 days remaining on the rate lock.";
            string strSESecondSignFail = "Loan requires a second signature prior to Final Approval.";
            string strMSGAppraisalID = "Appraisal type is marked as FULL, please ensure the Appraisal ID is updated (ULDD.X31) on the Appraisal/Property Info screen before issuing Final Approval.";
            string strMSGOpenPTDs = "All Prior to Docs conditions much either be satisfied, waived, or moved to At Closing, Prior to Funding, or Prior to Purchase before issuing Final Approval.";
            string strApprovalExp = "Final Approval cannot be issued - Loan is currently configured with a signing date that is after approval documentation has expired. Either the documentation should be updated, or loan must sign prior to the earliest documentation expiration date.";
            string strMsgLockVarianceFail = "Data has changed since the interest rate was locked. Pricing needs to be validated by the Lock Desk via the Lock Comparison form and any variance must be addressed prior to Final Approval.";
            string strMsgUWCVarianceFail = "There are variances which much be reviewed and cleared since the last UW review. Please address all variances via the UW Comparison screen before attempting to issue Final Approval.";
            string strQMFactorsFail = "You must validate that you have evaulated all ATR/QM factors prior to issuing Final Approval.";
            string strCurrentUserName = EncompassApplication.CurrentUser.FullName;
            string strUWNotes = ln.Fields["CX.UW.NOTES.TMP"].ToString();
            string strByPassRules = ln.Fields["CX.LOAN.UWDCSN.BYPASS"].ToString();
            string strMsgMINotNeeded = "This is a Conventional loan with LTV/CLTV < 80% and Mortgage Insurance applied. Please remove the MI to continue.";
            string strMsgMissingSSRs = "For loans with an appraisal type of 'Full', one or more SSR scores are required prior to Final Approval.";
            string strMsgFHAAdjValue = "File cannot be moved to Final Approval: Your Adjusted value is less than the appraised value.";
            string strLoanType = ln.Fields["1172"].ToString();
            string strNoFEMACheck = "You must run a FEMA search against the property prior to issuing a loan decision.";
            string strFHAAppraisalLogged = "Field Id: 3076 (Appraisal Logged Date) must contain a valid date in order to final approve a non-Streamline FHA loan.";
            decimal dLTV = ln.Fields["353"].ToDecimal();
            decimal dCLTV = ln.Fields["976"].ToDecimal();
            MilestoneEvent milestone = ln.Log.MilestoneEvents.GetEventForMilestone("Final Approved");

            RequiredFields_Final();
            ValidateMaventRun();
            ValidateMaventResult();
            ValidateMaventHardStop();
            string strMvtRun = ln.Fields["CX.WF.HS.MVTRUN"].ToString();
            string strMvtHardStop = ln.Fields["CX.WF.HS.MVTHARDSTOP"].ToString();

            if (strMvtRun == "FAIL")
                errorMessages.Add(strMsgOldMavent);
            else if (strMvtHardStop == "Y")
                errorMessages.Add(strMsgFailedMavent);

            //Validate that loan is not marked as Standard ATR or Non-QM
            string strFailingQM = ln.Fields["CX.FAILINGQM"].ToString();

            if (strFailingQM == "X")
                errorMessages.Add(strMsgFailingQM);

            ValidateMI();
            string strMITestResult = ln.Fields["CX.WF.HS.MITESTRESULT"].ToString();

            if (strMITestResult == "FAIL" && strLoanType == "Conventional")
                errorMessages.Add(strMsgConvMIFail);
            else if (strMITestResult == "FAIL" && strLoanType == "FHA")
                errorMessages.Add(strMsgFHAMIFail);

            string strHasMI = ln.Fields["CX.WF.HS.HASMI"].ToString();

            if (strHasMI == "Y" && strLoanType == "Conventional" && dLTV < 80 && dCLTV < 80)
                errorMessages.Add(strMsgMINotNeeded);

            ValidateDisclosures_APR();
            string strAPRTestResult = ln.Fields["CX.WF.HS.APRTEST"].ToString();

            if (strAPRTestResult == "FAIL")
                errorMessages.Add(strMsgAPRTestFail);

            //ValidateDisclosures_LockDate();
            string strLockTestResult = ln.Fields["CX.WF.HS.LOCKDISCTEST"].ToString();

            if (strLockTestResult == "FAIL")
                errorMessages.Add(strMsgLockTestFail);

            ValidateDisclosures_Product();
            string strAmortTestResult = ln.Fields["CX.WF.HS.AMORTDISCTEST"].ToString();

            if (strAmortTestResult == "FAIL")
                errorMessages.Add(strMsgAmortTestFail);

            ValidateDisclosures_Variance();
            string strFVTestResults = ln.Fields["CX.WF.HS.DISCFVTEST"].ToString();

            if (strFVTestResults == "FAIL")
                errorMessages.Add(strMsgFVTestFail);

            ValidateLockDaysRemaining();

            //int iLockDays = ln.Fields["CX.WF.HS.LOCKDAYSREM"].ToInt();
            //string strUWDCSNLockDaysBypass = ln.Fields["CX.LOAN.UWDCSN.LCKEXP.BYPASS"].ToString();

            //if (iLockDays <= 6 && strUWDCSNLockDaysBypass != "Y")
            //    MessageBox.Show(strLockDaysTestFail, "Encompass Hard Stop", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            int iOpenPTDCount = ln.Fields["CX.LOAN.OPENPTDS"].ToInt();

            if (iOpenPTDCount > 0)
                errorMessages.Add(strMSGOpenPTDs);

            string strAppraisalId = ln.Fields["ULDD.X31"].ToString();
            string strAppraisalType = ln.Fields["CX.LOAN.APPRAISALTYPE"].ToString();

            if (strAppraisalType == "Full" && strLoanType != "VA" && strLoanType != "FHA" && string.IsNullOrEmpty(strAppraisalId))
                errorMessages.Add(strMSGAppraisalID);

            decimal dFNMASSR = ln.Fields["CX.UW.SSRSCORE.FNMA"].ToDecimal();
            decimal dFHLMCSSR = ln.Fields["CX.UW.SSRSCORE.FHLMC"].ToDecimal();
            string strFNMASSRNA = ln.Fields["CX.UW.SSR.NA.FNMA"].ToString();
            string strFHLMCSSRNA = ln.Fields["CX.UW.SSR.NA.FHLMC"].ToString();
            string strPropType = ln.Fields["CX.PROPERTY.TYPE.GLOBAL"].ToString();

            if (strLoanType == "Conventional" && strPropType != "2 Unit" && strPropType != "3 Unit" && strPropType != "4 Unit" && strPropType != "Manufactured Housing")
            { 
                if (strAppraisalType == "Full" && dFNMASSR == 0 && dFHLMCSSR == 0 && strFNMASSRNA == "" && strFHLMCSSRNA == "")
                    errorMessages.Add(strMsgMissingSSRs);
            }

            DateTime dEstClosingDate = ln.Fields["763"].ToDate();
            DateTime dApprovalExpDate = ln.Fields["CX.EXP.APPROVAL.EXPDATE"].ToDate();

            if (dEstClosingDate > dApprovalExpDate)
                errorMessages.Add(strApprovalExp);

            string strSecondSignNeeded = ln.Fields["CX.UW.2NDSIG"].ToString();
            string strSecondSignUW = ln.Fields["CX.UW.2NDSIG.U"].ToString();

            if (strSecondSignNeeded == "Y" && strSecondSignUW == "")
                errorMessages.Add(strSESecondSignFail);

            string strLockVarianceTestResult = ln.Fields["CX.LCKC.VARIANCE"].ToString();

            if (strLockVarianceTestResult != "N")
                errorMessages.Add(strMsgLockVarianceFail);

            string strUWCVariance = ln.Fields["CX.UWC.VARIANCE"].ToString();

            if (strUWCVariance != "N")
                errorMessages.Add(strMsgUWCVarianceFail);

            string strQMFactorsEval = ln.Fields["CX.LOAN.QMFACTORS.EVAL"].ToString();

            if (strQMFactorsEval != "Y")
                errorMessages.Add(strQMFactorsFail);
            
            DateTime dtPurchaseDate = ln.Fields["1518"].ToDate();
            DateTime dtCaseAssignment = ln.Fields["3042"].ToDate();
            decimal dOriginalCost = ln.Fields["25"].ToDecimal();
            decimal dApprValue = ln.Fields["356"].ToDecimal();
            string strRefiType = ln.Fields["URLA.X166"].ToString();

            if (strLoanType == "FHA" && strRefiType != "StreamlineWithAppraisal" && strRefiType != "StreamlineWithoutAppraisal")
            {
                DateTime dtCaseLessOneYear = dtCaseAssignment.AddMonths(-12);

                if (dtPurchaseDate >= dtCaseLessOneYear && dOriginalCost < dApprValue)
                    errorMessages.Add(strMsgFHAAdjValue);
            }

            string strFEMAHasRun = ln.Fields["CX.FEMA.HASRUN"].ToString();
            if (strFEMAHasRun == "")
            {
                errorMessages.Add(strNoFEMACheck);
            }
            //MessageBox.Show("HardStop Count Is: " + iHardStopCount + " strBypassValue is: " + strByPassRules, "Encompass Debug Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            string strFHAApprslLogDate = ln.Fields["3076"].ToString();
            if (strFHAApprslLogDate == "//" && strLoanType == "FHA" && strRefiType != "StreamlineWithAppraisal" && strRefiType != "StreamlineWithoutAppraisal")
            {
                errorMessages.Add(strFHAAppraisalLogged);
            }
            
            if (errorMessages.Count == 0 || strByPassRules == "Y")
            {
                string strValidationLogCurrent = ln.Fields["CX.UWC.DATAVALIDATION.LOG"].ToString();
                ln.Fields["CX.UW.LOGSTATUS"].Value = DateTime.Now;
                ln.Fields["CX.LOAN.STATUS"].Value = "Final Approved";
                ln.Fields["CX.LOAN.DEPT"].Value = "Processing";
                ln.Fields["CX.UW.STATUS"].Value = "Final Approved";
                ln.Fields["CX.UW.STATUS.U"].Value = strCurrentUserName;
                ln.Fields["CX.UW.STATUS.D"].Value = DateTime.Now;
                ln.Fields["CX.UW.NOTES"].Value = DateTime.Now.ToString("f") + " - " + strCurrentUserName + Environment.NewLine + strUWNotes;
                ln.Fields["CX.UW.LOGSTATUS"].Value = DateTime.Now.ToString("f");
                milestone.AdjustDate(DateTime.Now, true, true);
                milestone.Completed = true;
                
                StoreUWValues();
                
                if (strValidationLogCurrent != "")
                    ln.Fields["CX.UWC.DATAVALIDATION.LOG"].Value = strValidationLogCurrent + System.Environment.NewLine + System.Environment.NewLine + DateTime.Now.ToString("f") + "~" + strCurrentUserName + "~UW Decision - Final Approval";
                else if (strValidationLogCurrent == "")
                    ln.Fields["CX.UWC.DATAVALIDATION.LOG"].Value = DateTime.Now.ToString("f") + "~" + strCurrentUserName + "~UW Decision - Final Approval";
            }
            else
            {
                //ShowValidationWindow();
                HelperMethods.DisplayValidationResults("Loan Data Validation", "Encompass Hard Stops", errorMessages);
            }
        }

        public void RequiredFields_Final()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            string strMissingFieldMsg = "The following fields must be completed prior to issuing Final Approval.";
            List<string> missingFields = new List<string>();
            string strLoanType = ln.Fields["1172"].ToString();
            string strLoanPurpose = ln.Fields["19"].ToString();
            string strRefiType = ln.Fields["URLA.X166"].ToString();
            string dtPurchaseDate = ln.Fields["1518"].ToString();
            string dtCaseAssignmentDate = ln.Fields["3042"].ToString();
            decimal dOriginalCost = ln.Fields["25"].ToDecimal();

            //All Loans 

            //Conventional Purchase

            //Conventional Refi

            //FHA General

            //FHA Purchase

            //FHA Full Doc

            if (strLoanType == "FHA" && strLoanPurpose != "Purchase" && strRefiType != "StreamlineWithAppraisal" && strRefiType != "StreamlineWithoutAppraisal")
            {
                if (dtCaseAssignmentDate == "//")
                    missingFields.Add("Field Id: 3042 - FHA Case Assignment Date");

                if (dtPurchaseDate == "//")
                    missingFields.Add("Field Id: 1518 - Subject Property Purchase Date");

                if (string.IsNullOrEmpty(dOriginalCost.ToString()) || dOriginalCost == 0)
                    missingFields.Add("Field Id: 25 - Original Cost");
            }

            if (missingFields.Count > 0)
            {
                HelperMethods.DisplayValidationResults("Encompass Hard Stop", strMissingFieldMsg, missingFields);
                //ShowValidationWindow();
            }

            //FHA Streamline

            //VA Purchase

            //VA Cash Out

            //VA IRRRL
        }

        public void UWRefinal()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            //MessageBox.Show("UW Refinal fired!");
            List<string> errorMessages = new List<string>();
            string strMsgOldMavent = "You must run a new Mavent report prior to reissuing Final Approval. Note that if the Mavent report does not pass, any failures will need to be addressed and the report re-ran prior to Final Approval.";
            string strMsgFailedMavent = "The most recent Mavent/Compliance report indicates that one or more compliance checks have failed. You must have a passing Mavent report prior to reissuing Final Approval.";
            string strMsgConvMIFail = "MI is required when loan type is Conventional and LTV is > 80%.";
            string strMsgFHAMIFail = "Loan is missing monthly MI, upfront MI, or both. Both are required on FHA loans.";
            string strMsgAPRTestFail = "APR has increased by .125 or more since the last disclosure. Redisclosure is required.";
            string strMsgLockTestFail = "No redisclosure has taken place since the rate was locked. Redisclosure is required.";
            string strMsgAmortTestFail = "Amortization type has changed sinces last disclosure. Redisclosure is required.";
            string strMsgFVTestFail = "There have been changes to fees since the last disclosures. COC Redisclosure is required.";
            string strMsgFailingQM = "This loan does not currently meet Qualified Mortgage requirements. Please review a new Mavent/Compliance report for Alerts in the ATR/QM report section. Final Approval cannot be issued until this is addressed.";
            //string strLockExp = "This loan is currently scheduled to fund after the lock expiration date. The Lock either needs to be extended or the loan needs to be updated to ensure disbursement takes place prior to the lock expiration date.";
            string strSESecondSignFail = "Loan requires a second signature prior to Final Approval.";
            string strMSGAppraisalID = "Appraisal type is marked as FULL, please ensure the Appraisal ID is updated (ULDD.X31) on the Appraisal/Property Info screen before issuing Final Approval.";
            string strMSGOpenPTDs = "All Prior to Docs conditions much either be satisfied, waived, or moved to At Closing, Prior to Funding, or Prior to Purchase before issuing Final Approval.";
            string strApprovalExp = "Final Approval cannot be issued - Loan is currently configured with a signing date that is after approval documentation has expired. Either the documentation should be updated, or loan must sign prior to the earliest documentation expiration date.";
            string strMsgLockVarianceFail = "Data has changed since the interest rate was locked. Pricing needs to be validated by the Lock Desk via the Lock Comparison form and any variance must be addressed prior to Final Approval.";
            string strMsgUWCVarianceFail = "There are variances which much be reviewed and cleared since the last UW review. Please address all variances via the UW Comparison screen before attempting to reissue Final Approval.";
            string strQMFactorsFail = "You must validate that you have evaulated all ATR/QM factors prior to reissuing Final Approval.";
            string strCurrentUserName = EncompassApplication.CurrentUser.FullName;
            string strUWNotes = ln.Fields["CX.UW.NOTES.TMP"].ToString();
            string strByPassRules = ln.Fields["CX.LOAN.UWDCSN.BYPASS"].ToString();
            string strMsgMINotNeeded = "This is a Conventional loan with LTV/CLTV < 80% and Mortgage Insurance applied. Please remove the MI to continue.";
            string strMsgMissingSSRs = "For loans with an appraisal type of 'Full', one or more SSR scores are required prior to Final Approval.";
            string strLoanType = ln.Fields["1172"].ToString();
            string strNoFEMACheck = "You must run a FEMA search against the property prior to issuing a loan decision.";
            decimal dLTV = ln.Fields["353"].ToDecimal();
            decimal dCLTV = ln.Fields["976"].ToDecimal();
            MilestoneEvent milestone = ln.Log.MilestoneEvents.GetEventForMilestone("Final Approved");
            //MessageBox.Show("Hard Stop Count is: " + iHardStopCount.ToString());

            ValidateMaventRun();
            ValidateMaventResult();
            ValidateMaventHardStop();
            string strMvtRun = ln.Fields["CX.WF.HS.MVTRUN"].ToString();
            string strMvtHardStop = ln.Fields["CX.WF.HS.MVTHARDSTOP"].ToString();

            if (strMvtRun == "FAIL")
                errorMessages.Add(strMsgOldMavent);
            else if (strMvtHardStop == "Y")
                errorMessages.Add(strMsgFailedMavent);

            //Validate that loan is not marked as Standard ATR or Non-QM
            string strFailingQM = ln.Fields["CX.FAILINGQM"].ToString();

            if (strFailingQM == "X")
                errorMessages.Add(strMsgFailingQM);

            ValidateMI();
            string strMITestResult = ln.Fields["CX.WF.HS.MITESTRESULT"].ToString();

            if (strMITestResult == "FAIL" && strLoanType == "Conventional")
                errorMessages.Add(strMsgConvMIFail);
            else if (strMITestResult == "FAIL" && strLoanType == "FHA")
                errorMessages.Add(strMsgFHAMIFail);

            string strHasMI = ln.Fields["CX.WF.HS.HASMI"].ToString();

            if (strHasMI == "Y" && strLoanType == "Conventional" && dLTV < 80 && dCLTV < 80)
                errorMessages.Add(strMsgMINotNeeded);

            ValidateDisclosures_APR();
            string strAPRTestResult = ln.Fields["CX.WF.HS.APRTEST"].ToString();

            if (strAPRTestResult == "FAIL")
                errorMessages.Add(strMsgAPRTestFail);

            ValidateDisclosures_LockDate();
            string strLockTestResult = ln.Fields["CX.WF.HS.LOCKDISCTEST"].ToString();

            if (strLockTestResult == "FAIL")
                errorMessages.Add(strMsgLockTestFail);

            ValidateDisclosures_Product();
            string strAmortTestResult = ln.Fields["CX.WF.HS.AMORTDISCTEST"].ToString();

            if (strAmortTestResult == "FAIL")
                errorMessages.Add(strMsgAmortTestFail);

            ValidateDisclosures_Variance();
            string strFVTestResults = ln.Fields["CX.WF.HS.DISCFVTEST"].ToString();

            if (strFVTestResults == "FAIL")
                errorMessages.Add(strMsgFVTestFail);

            //Validate that loan is not scheduled to fund after the lock expiration date
            //DateTime dEstDisbDate = ln.Fields["2553"].ToDate();
            //DateTime dLockExpDate = ln.Fields["762"].ToDate();
                
            //Lock Test Commented Out per updated Lock Policy discussions on 11/10
            //if (dEstDisbDate > dLockExpDate)
            //    MessageBox.Show(strLockExp, "Encompass Hard Stop", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            int iOpenPTDCount = ln.Fields["CX.LOAN.OPENPTDS"].ToInt();

            if (iOpenPTDCount > 0)
                errorMessages.Add(strMSGOpenPTDs);

            string strAppraisalId = ln.Fields["ULDD.X31"].ToString();
            string strAppraisalType = ln.Fields["CX.LOAN.APPRAISALTYPE"].ToString();

            if (strAppraisalType == "Full" && strLoanType != "VA" && strLoanType != "FHA" && string.IsNullOrEmpty(strAppraisalId))
                errorMessages.Add(strMSGAppraisalID);

            decimal dFNMASSR = ln.Fields["CX.UW.SSRSCORE.FNMA"].ToDecimal();
            decimal dFHLMCSSR = ln.Fields["CX.UW.SSRSCORE.FHLMC"].ToDecimal();
            string strFNMASSRNA = ln.Fields["CX.UW.SSR.NA.FNMA"].ToString();
            string strFHLMCSSRNA = ln.Fields["CX.UW.SSR.NA.FHLMC"].ToString();
            string strPropType = ln.Fields["CX.PROPERTY.TYPE.GLOBAL"].ToString();

            if (strLoanType == "Conventional" && strPropType != "2 Unit" && strPropType != "3 Unit" && strPropType != "4 Unit" && strPropType != "Manufactured Housing")
            {
                if (strAppraisalType == "Full" && dFNMASSR == 0 && dFHLMCSSR == 0 && strFNMASSRNA == "" && strFHLMCSSRNA == "")
                    errorMessages.Add(strMsgMissingSSRs);
            }

            DateTime dEstClosingDate = ln.Fields["763"].ToDate();
            DateTime dApprovalExpDate = ln.Fields["CX.EXP.APPROVAL.EXPDATE"].ToDate();

            if (dEstClosingDate > dApprovalExpDate)
                errorMessages.Add(strApprovalExp);

            string strSecondSignNeeded = ln.Fields["CX.UW.2NDSIG"].ToString();
            string strSecondSignUW = ln.Fields["CX.UW.2NDSIG.U"].ToString();

            if (strSecondSignNeeded == "Y" && strSecondSignUW == "")
                errorMessages.Add(strSESecondSignFail);

            string strLockVarianceTestResult = ln.Fields["CX.LCKC.VARIANCE"].ToString();

            if (strLockVarianceTestResult != "N")
                errorMessages.Add(strMsgLockVarianceFail);

            string strUWCVariance = ln.Fields["CX.UWC.VARIANCE"].ToString();

            if (strUWCVariance != "N")
                errorMessages.Add(strMsgUWCVarianceFail);

            string strQMFactorsEval = ln.Fields["CX.LOAN.QMFACTORS.EVAL"].ToString();

            if (strQMFactorsEval != "Y")
                errorMessages.Add(strQMFactorsFail);

            //MessageBox.Show("HardStop Count Is: " + iHardStopCount + " strBypassValue is: " + strByPassRules, "Encompass Debug Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            string strFEMAHasRun = ln.Fields["CX.FEMA.HASRUN"].ToString();
            if (strFEMAHasRun == "")
            {
                errorMessages.Add(strNoFEMACheck);
            }

            if (errorMessages.Count == 0 || strByPassRules == "Y")
            {
                string strValidationLogCurrent = ln.Fields["CX.UWC.DATAVALIDATION.LOG"].ToString();
                ln.Fields["CX.UW.LOGSTATUS"].Value = DateTime.Now;
                ln.Fields["CX.LOAN.STATUS"].Value = "Final Approved";
                ln.Fields["CX.LOAN.DEPT"].Value = "Processing";
                ln.Fields["CX.UW.STATUS"].Value = "Final Approved";
                ln.Fields["CX.UW.STATUS.U"].Value = strCurrentUserName;
                ln.Fields["CX.UW.STATUS.D"].Value = DateTime.Now;
                ln.Fields["CX.UW.NOTES"].Value = DateTime.Now.ToString("f") + " - " + strCurrentUserName + Environment.NewLine + strUWNotes;
                ln.Fields["CX.UW.LOGSTATUS"].Value = DateTime.Now.ToString("f");
                milestone.AdjustDate(DateTime.Now, true, true);
                milestone.Completed = true;

                StoreUWValues();

                if (strValidationLogCurrent != "")
                    ln.Fields["CX.UWC.DATAVALIDATION.LOG"].Value = strValidationLogCurrent + System.Environment.NewLine + System.Environment.NewLine + DateTime.Now.ToString("MM/dd/yyyy h:mm tt") + "~" + strCurrentUserName + "~UW Decision - Re-Final Approval";
                else if (strValidationLogCurrent == "")
                    ln.Fields["CX.UWC.DATAVALIDATION.LOG"].Value = DateTime.Now.ToString("MM/dd/yyyy h:mm tt") + "~" + strCurrentUserName + "~UW Decision - Re-Final Approval";
            }
            else
            {
                //ShowValidationWindow();
                HelperMethods.DisplayValidationResults("Loan Data Validation", "Encompass Hard Stops", errorMessages);
            }
        }

        public void ShowValidationWindow()
        {
            using (Form form = new Form())
            {
                form.Text = "About Us";
                // form.Controls.Add(...);
                form.ShowDialog();
            }
        }

        public void SigningReq()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            List<string> errorMessages = new List<string>();
            string strMsgOldMavent = "You must run a new Mavent report prior to requesting docs. Note that if the Mavent report does not pass, any failures will need to be addressed and the report re-ran prior to requesting docs.";
            string strMsgFailedMavent = "The most recent Mavent/Compliance report indicates that one or more compliance checks have failed. You must have a passing Mavent report prior to requesting docs.";
            string strMsgConvMIFail = "MI is required when loan type is Conventional and LTV is > 80%.";
            string strMsgFHAMIFail = "Loan is missing monthly MI, upfront MI, or both. Both are required on FHA loans.";
            string strMsgAPRTestFail = "APR has increased by .125 or more since the last disclosure. Redisclosure is required.";
            string strMsgLockTestFail = "No redisclosure has taken place since the rate was locked. Redisclosure is required.";
            string strMsgAmortTestFail = "Amortization type has changed sinces last disclosure. Redisclosure is required.";
            string strMsgFVTestFail = "There have been changes to fees since the last disclosures. COC Redisclosure is required.";
            //string strLockDaysTestFail = "Closing docs cannot be requested with less than 7 days remaining on the rate lock.";
            string strApprovalExp = "The currently selected signing date is later than the approval expiration date. Either the approval expiration date or the signing date need to be updated to complete the signing request.";
            //string strCurrentStatus = ln.Fields["CX.LOAN.STATUS"].ToString();
            //string strExpressFlowLn = ln.Fields["CX.LOAN.EXPRESSFLOW"].ToString();
            string strBypassRules = ln.Fields["CX.LOAN.REQSIGNING.BYPASS"].ToString();
            string strMsgLockVarianceFail = "Data has changed since the interest rate was locked. Pricing needs to be validated by the Lock Desk via the Lock Comparison form and any variance must be addressed prior to requesting a closing appointment.";
            string strNoCDSentFail = "No initial CD has been issued on this file. You cannot schedule a closing until an initial CD has been issued and e-signed by all parties to the loan.";
            string strLoanType = ln.Fields["1172"].ToString();

            ValidateMaventRun();
            ValidateMaventResult();
            ValidateMaventHardStop();
            string strMvtRun = ln.Fields["CX.WF.HS.MVTRUN"].ToString();
            string strMvtHardStop = ln.Fields["CX.WF.HS.MVTHARDSTOP"].ToString();

            if (strMvtRun == "FAIL" && strBypassRules != "Y")
                errorMessages.Add(strMsgOldMavent);
            else if (strMvtHardStop == "Y" && strBypassRules != "Y")
                errorMessages.Add(strMsgFailedMavent);

            ValidateMI();
            string strMITestResult = ln.Fields["CX.WF.HS.MITESTRESULT"].ToString();

            if (strMITestResult == "FAIL" && strLoanType == "Conventional")
                errorMessages.Add(strMsgConvMIFail);
            else if (strMITestResult == "FAIL" && strLoanType == "FHA")
                errorMessages.Add(strMsgFHAMIFail);

            ValidateDisclosures_APR();
            string strAPRTestResult = ln.Fields["CX.WF.HS.APRTEST"].ToString();

            if (strAPRTestResult == "FAIL" && strBypassRules != "Y")
                errorMessages.Add(strMsgAPRTestFail);

            ValidateDisclosures_LockDate();
            string strLockTestResult = ln.Fields["CX.WF.HS.LOCKDISCTEST"].ToString();

            if (strLockTestResult == "FAIL" && strBypassRules != "Y")
                errorMessages.Add(strMsgLockTestFail);

            ValidateDisclosures_Product();
            string strAmortTestResult = ln.Fields["CX.WF.HS.AMORTDISCTEST"].ToString();

            if (strAmortTestResult == "FAIL")
                errorMessages.Add(strMsgAmortTestFail);

            ValidateDisclosures_Variance();
            string strFVTestResults = ln.Fields["CX.WF.HS.DISCFVTEST"].ToString();

            if (strFVTestResults == "FAIL" && strBypassRules != "Y")
                errorMessages.Add(strMsgFVTestFail);

            ValidateLockDaysRemaining();

            //int iLockDays = ln.Fields["CX.WF.HS.LOCKDAYSREM"].ToInt();
            //string strSignReqLockDaysBypass = ln.Fields["CX.LOAN.REQSGN.LCKEXP.BYPASS"].ToString();
            //if (iLockDays <= 6 && strSignReqLockDaysBypass != "Y")
            //    MessageBox.Show(strLockDaysTestFail, "Encompass Hard Stop", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            DateTime dEstClosingDate = ln.Fields["763"].ToDate();
            DateTime dApprovalExpDate = ln.Fields["CX.EXP.APPROVAL.EXPDATE"].ToDate();

            if (dEstClosingDate > dApprovalExpDate)
                errorMessages.Add(strApprovalExp);

            string strLockVarianceTestResult = ln.Fields["CX.LCKC.VARIANCE"].ToString();

            if (strLockVarianceTestResult != "N")
                errorMessages.Add(strMsgLockVarianceFail);

            string strICDDate = ln.Fields["3977"].ToString();

            if (strICDDate == "//")
                errorMessages.Add(strNoCDSentFail);

            if (errorMessages.Count == 0)
            {
                ln.Fields["CX.LOAN.REQSIGNING.ACTIVE"].Value = "X";
                //ln.Fields["CX.LOAN.STATUS"].Value = "Docs Requested";
                //ln.Fields["CX.LOAN.DEPT"].Value = "Closing";
            }
            else
            {
                HelperMethods.DisplayValidationResults("Loan Data Validation", "Encompass Hard Stops", errorMessages);
            }
        }

        public void DocsReq()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            List<string> errorMessages = new List<string>();
            string strMsgOldMavent = "You must run a new Mavent report prior to requesting docs. Note that if the Mavent report does not pass, any failures will need to be addressed and the report re-ran prior to requesting docs.";
            string strMsgFailedMavent = "The most recent Mavent/Compliance report indicates that one or more compliance checks have failed. You must have a passing Mavent report prior to requesting docs.";
            string strMsgConvMIFail = "MI is required when loan type is Conventional and LTV is > 80%.";
            string strMsgFHAMIFail = "Loan is missing monthly MI, upfront MI, or both. Both are required on FHA loans.";
            string strMsgAPRTestFail = "APR has increased by .125 or more since the last disclosure. Redisclosure is required.";
            string strMsgLockTestFail = "No redisclosure has taken place since the rate was locked. Redisclosure is required.";
            string strMsgAmortTestFail = "Amortization type has changed sinces last disclosure. Redisclosure is required.";
            string strMsgFVTestFail = "There have been changes to fees since the last disclosures. COC Redisclosure is required.";
            //string strLockDaysTestFail = "Closing docs cannot be requested with less than 7 days remaining on the rate lock.";
            string strApprovalExp = "The currently selected signing date is later than the approval expiration date. Either the approval expiration date or the signing date need to be updated to complete the signing request.";
            string strStatusTestFail = "You cannot request closing docs unless the loan status is Final Approved or greater.";
            string strUWStatusTestFail = "You cannot request closing docs unless the UW Status is Final Approved.";
            string strCurrentStatus = ln.Fields["CX.LOAN.STATUS"].ToString();
            string strCurrentUWStatus = ln.Fields["CX.UW.STATUS"].ToString();
            string strSigningAppt = "You cannot request closing docs without a future, active signing appointment scheduled.";
            string strMsgLockVarianceFail = "Data has changed since the interest rate was locked. Pricing needs to be validated by the Lock Desk via the Lock Comparison form and any variance must be addressed prior to requesting a closing appointment.";
            string strNoCDSentFail = "No initial CD has been issued on this file. You cannot request docs until an initial CD has been issued and e-signed by all parties to the loan.";
            string strLoanType = ln.Fields["1172"].ToString();

            ValidateMaventRun();
            ValidateMaventResult();
            ValidateMaventHardStop();
            string strMvtRun = ln.Fields["CX.WF.HS.MVTRUN"].ToString();
            string strMvtHardStop = ln.Fields["CX.WF.HS.MVTHARDSTOP"].ToString();

            if (strMvtRun == "FAIL")
                errorMessages.Add(strMsgOldMavent);
            else if (strMvtHardStop == "Y")
                errorMessages.Add(strMsgFailedMavent);

            ValidateMI();
            string strMITestResult = ln.Fields["CX.WF.HS.MITESTRESULT"].ToString();

            if (strMITestResult == "FAIL" && strLoanType == "Conventional")
                errorMessages.Add(strMsgConvMIFail);
            else if (strMITestResult == "FAIL" && strLoanType == "FHA")
                errorMessages.Add(strMsgFHAMIFail);

            ValidateDisclosures_APR();
            string strAPRTestResult = ln.Fields["CX.WF.HS.APRTEST"].ToString();

            if (strAPRTestResult == "FAIL")
                errorMessages.Add(strMsgAPRTestFail);

            ValidateDisclosures_LockDate();
            string strLockTestResult = ln.Fields["CX.WF.HS.LOCKDISCTEST"].ToString();

            if (strLockTestResult == "FAIL")
                errorMessages.Add(strMsgLockTestFail);

            ValidateDisclosures_Product();
            string strAmortTestResult = ln.Fields["CX.WF.HS.AMORTDISCTEST"].ToString();

            if (strAmortTestResult == "FAIL")
                errorMessages.Add(strMsgAmortTestFail);

            ValidateDisclosures_Variance();
            string strFVTestResults = ln.Fields["CX.WF.HS.DISCFVTEST"].ToString();

            if (strFVTestResults == "FAIL")
                errorMessages.Add(strMsgFVTestFail);

            ValidateLockDaysRemaining();

            //int iLockDays = ln.Fields["CX.WF.HS.LOCKDAYSREM"].ToInt();                                     
            //string strDocReqLockExpBypass = ln.Fields["CX.LOAN.DOCREQ.LCKEXP.BYPASS"].ToString();
            //Lock Exp Test Commented out as a result of Lock Policy discussions 11/10
            //if (iLockDays <= 6 && strDocReqLockExpBypass != "Y")
            //    MessageBox.Show(strLockDaysTestFail, "Encompass Hard Stop", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            if (strCurrentStatus != "Final Approved")
                errorMessages.Add(strStatusTestFail);

            if (strCurrentUWStatus != "Final Approved")
                errorMessages.Add(strUWStatusTestFail);

            DateTime dEstClosingDate = ln.Fields["763"].ToDate();
            DateTime dApprovalExpDate = ln.Fields["CX.EXP.APPROVAL.EXPDATE"].ToDate();

            if (dEstClosingDate > dApprovalExpDate)
                errorMessages.Add(strApprovalExp);

            string strLockVarianceTestResult = ln.Fields["CX.LCKC.VARIANCE"].ToString();

            if (strLockVarianceTestResult != "N")
                errorMessages.Add(strMsgLockVarianceFail);

            string strICDDate = ln.Fields["3977"].ToString();

            if (strICDDate == "//")
                errorMessages.Add(strNoCDSentFail);

            string strActiveSigningAppt = ln.Fields["CX.LOAN.REQSIGNING.ACTIVE"].ToString();
            string strSigningReqDate = ln.Fields["CX.LOAN.REQSIGNING.DATE"].ToDate().ToShortDateString();

            //No signing date
            if (strSigningReqDate == "//")
                errorMessages.Add(strSigningAppt);

            //Signing date but signing is not active
            if (strSigningReqDate != "//" && strActiveSigningAppt != "X")
                errorMessages.Add(strSigningAppt);

            //Signing date, signing is active, but signing date is in the past
            if (strActiveSigningAppt == "X" && strSigningReqDate != "//" && (Convert.ToDateTime(strSigningReqDate) < Convert.ToDateTime(DateTime.Now.ToShortDateString())))
                errorMessages.Add(strSigningAppt);

            if (errorMessages.Count == 0)
            {
                string strSuccess = "Closing Docs have been requested.";
                ln.Fields["CX.LOAN.STATUS"].Value = "Docs Requested";
                ln.Fields["CX.LOAN.DEPT"].Value = "Closing";
                MessageBox.Show(strSuccess, "Encompass Notification", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                HelperMethods.DisplayValidationResults("Loan Data Validation", "Encompass Hard Stops", errorMessages);
            }
        }

        public void Resubmission()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            //MessageBox.Show("Sub to UW fired!");
            List<string> errorMessages = new List<string>();
            string strMsgOldMavent = "You must run a new Mavent report prior to submission/resubmission. Note that if the Mavent report does not pass, any failures will need to be addressed and the report re-ran prior to submission/resubmission.";
            string strMsgFailedMavent = "The most recent Mavent/Compliance report indicates that one or more compliance checks have failed. You must have a passing Mavent report prior to submission/resubmission.";
            string strMsgConvMIFail = "MI is required when loan type is Conventional and LTV is > 80%.";
            string strMsgFHAMIFail = "Loan is missing monthly MI, upfront MI, or both. Both are required on FHA loans.";
            string strMsgAPRTestFail = "APR has increased by .125 or more since the last disclosure. Redisclosure is required.";
            string strMsgLockTestFail = "No redisclosure has taken place since the rate was locked. Redisclosure is required.";
            string strMsgAmortTestFail = "Amortization type has changed sinces last disclosure. Redisclosure is required.";
            string strMsgFVTestFail = "There have been changes to fees since the last disclosures. COC Redisclosure is required.";
            string strMsgQMTestFail = "This loan is currently failing QM. Please review/address prior to submission for Final Approval.";
            string strMsgLockVarianceFail = "Data has changed since the interest rate was locked. Pricing needs to be validated by the Lock Desk via the Lock Comparison form and any variance must be addressed prior to resubmission";
            string strMsgMINotNeeded = "This is a Conventional loan with LTV/CLTV < 80% and Mortgage Insurance applied. Please remove the MI to continue.";
            string strMsgNoCD = "An Initial CD must have been sent or an Initial CD Request must be in process in order to Resubmit for Final Approval." + Environment.NewLine + Environment.NewLine + "If Resubmitting for a reason other than to obtain Final Approval, the Processing Manager can override and Resubmit if an Initial CD has not yet been sent/requested.";
            string strLoanType = ln.Fields["1172"].ToString();
            decimal dLTV = ln.Fields["353"].ToDecimal();
            decimal dCLTV = ln.Fields["976"].ToDecimal();
            string strCurrUWStatus = ln.Fields["CX.UW.STATUS"].ToString();
            //string strInitFADate = ln.Fields["CX.HIST.STATUS.FINAPPROVED"].ToString();
            int subCount = ln.Fields["CX.TRACKING.UW.SUBMISSIONS"].ToInt();

            ValidateMaventRun();
            ValidateMaventResult();
            ValidateMaventHardStop();
            string strMvtRun = ln.Fields["CX.WF.HS.MVTRUN"].ToString();
            string strMvtHardStop = ln.Fields["CX.WF.HS.MVTHARDSTOP"].ToString();

            if (strMvtRun == "FAIL")
                errorMessages.Add(strMsgOldMavent);
            else if (strMvtHardStop == "Y")
                errorMessages.Add(strMsgFailedMavent);

            ValidateMI();
            string strMITestResult = ln.Fields["CX.WF.HS.MITESTRESULT"].ToString();

            if (strMITestResult == "FAIL" && strLoanType == "Conventional")
                errorMessages.Add(strMsgConvMIFail);
            else if (strMITestResult == "FAIL" && strLoanType == "FHA")
                errorMessages.Add(strMsgFHAMIFail);

            string strHasMI = ln.Fields["CX.WF.HS.HASMI"].ToString();

            if (strHasMI == "Y" && strLoanType == "Conventional" && dLTV < 80 && dCLTV < 80)
                errorMessages.Add(strMsgMINotNeeded);

            ValidateDisclosures_APR();
            string strAPRTestResult = ln.Fields["CX.WF.HS.APRTEST"].ToString();

            if (strAPRTestResult == "FAIL")
                errorMessages.Add(strMsgAPRTestFail);

            ValidateDisclosures_LockDate();
            string strLockTestResult = ln.Fields["CX.WF.HS.LOCKDISCTEST"].ToString();

            if (strLockTestResult == "FAIL")
                errorMessages.Add(strMsgLockTestFail);

            ValidateDisclosures_Product();
            string strAmortTestResult = ln.Fields["CX.WF.HS.AMORTDISCTEST"].ToString();

            if (strAmortTestResult == "FAIL")
                errorMessages.Add(strMsgAmortTestFail);

            ValidateDisclosures_Variance();
            string strFVTestResults = ln.Fields["CX.WF.HS.DISCFVTEST"].ToString();

            if (strFVTestResults == "FAIL")
                errorMessages.Add(strMsgFVTestFail);

            string strQMTestResults = ln.Fields["CX.FAILINGQM"].ToString();

            if (strQMTestResults == "X")
                errorMessages.Add(strMsgQMTestFail);

            string strLockVarianceTestResult = ln.Fields["CX.LCKC.VARIANCE"].ToString();

            if (strLockVarianceTestResult != "N")
                errorMessages.Add(strMsgLockVarianceFail);

            string strICDSentDate = ln.Fields["3977"].ToString();
            string strCDRActive = ln.Fields["CX.CDR.ACTIVEREQUEST"].ToString();
            string strResubCDBypass = ln.Fields["CX.LOAN.RESUBCD.BYPASS"].ToString();

            if (strICDSentDate == "//" && strCDRActive != "X" && strCurrUWStatus != "Suspended" && strResubCDBypass != "Y")
                errorMessages.Add(strMsgNoCD);

            if (errorMessages.Count == 0 & strCurrUWStatus == "Approved")
            {
                ln.Fields["CX.UW.LOGSTATUS"].Value = DateTime.Now;
                ln.Fields["CX.LOAN.STATUS"].Value = "Resubmitted";
                ln.Fields["CX.LOAN.DEPT"].Value = "Underwriting";
                ln.Fields["CX.UW.STATUS"].Value = "Pending Condition Review";
                ln.Fields["CX.TRACKING.UW.SUBMSSIONS"].Value = (++subCount).ToString();
            }
            else if (errorMessages.Count == 0 & (strCurrUWStatus == "Suspended" || strCurrUWStatus == "Recommended for Decline"))
            {
                ln.Fields["CX.UW.LOGSTATUS"].Value = DateTime.Now;
                ln.Fields["CX.LOAN.STATUS"].Value = "Resubmitted";
                ln.Fields["CX.LOAN.DEPT"].Value = "Underwriting";
                ln.Fields["CX.UW.STATUS"].Value = "Pending Suspense Review";
                ln.Fields["CX.TRACKING.UW.SUBMSSIONS"].Value = (++subCount).ToString();
            }
            else if (errorMessages.Count > 0)
            {
                HelperMethods.DisplayValidationResults("Loan Data Validation", "Encompass Hard Stops", errorMessages);
            }
        }

        public void UWSuspense()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            //MessageBox.Show("Sub to UW fired!");
            List<string> errorMessages = new List<string>();
            string strMsgOldMavent = "You must run a new Mavent report prior to issuing a Suspense.";
            string strMsgFailedMavent = "The most recent Mavent/Compliance report indicates that one or more compliance checks have failed. Please ensure that any failing compliance tests which may affect the ability to gain loan approval are referenced within a PTA condition.";
            string strMsgConvMIFail = "MI is required when loan type is Conventional and LTV is > 80%.";
            string strMsgFHAMIFail = "Loan is missing monthly MI, upfront MI, or both. Both are required on FHA loans.";
            string strMsgAPRTestFail = "APR has increased by .125 or more since the last disclosure. Redisclosure is required.";
            string strMsgLockTestFail = "No redisclosure has taken place since the rate was locked. Redisclosure is required.";
            string strMsgAmortTestFail = "Amortization type has changed sinces last disclosure. Redisclosure is required.";
            string strMsgFVTestFail = "There have been changes to fees since the last disclosures. COC Redisclosure is required.";
            string strNoFEMACheck = "You must run a FEMA search against the property prior to issuing a loan decision.";
            string strCurrentUserName = EncompassApplication.CurrentUser.FullName;
            string strUWNotes = ln.Fields["CX.UW.NOTES.TMP"].ToString();
            string strLoanType = ln.Fields["1172"].ToString();

            ValidateMaventRun();
            ValidateMaventResult();
            ValidateMaventHardStop();
            string strMvtRun = ln.Fields["CX.WF.HS.MVTRUN"].ToString();
            string strMvtHardStop = ln.Fields["CX.WF.HS.MVTHARDSTOP"].ToString();

            if (strMvtRun == "FAIL")
                errorMessages.Add(strMsgOldMavent);
            else if (strMvtHardStop == "Y")
                errorMessages.Add(strMsgFailedMavent);

            ValidateMI();
            string strMITestResult = ln.Fields["CX.WF.HS.MITESTRESULT"].ToString();

            if (strMITestResult == "FAIL" && strLoanType == "Conventional")
                errorMessages.Add(strMsgConvMIFail);
            else if (strMITestResult == "FAIL" && strLoanType == "FHA")
                errorMessages.Add(strMsgFHAMIFail);

            ValidateDisclosures_APR();
            string strAPRTestResult = ln.Fields["CX.WF.HS.APRTEST"].ToString();

            if (strAPRTestResult == "FAIL")
                errorMessages.Add(strMsgAPRTestFail);

            ValidateDisclosures_LockDate();
            string strLockTestResult = ln.Fields["CX.WF.HS.LOCKDISCTEST"].ToString();

            if (strLockTestResult == "FAIL")
                errorMessages.Add(strMsgLockTestFail);

            ValidateDisclosures_Product();
            string strAmortTestResult = ln.Fields["CX.WF.HS.AMORTDISCTEST"].ToString();

            if (strAmortTestResult == "FAIL")
                errorMessages.Add(strMsgAmortTestFail);

            ValidateDisclosures_Variance();
            string strFVTestResults = ln.Fields["CX.WF.HS.DISCFVTEST"].ToString();

            if (strFVTestResults == "FAIL")
                errorMessages.Add(strMsgFVTestFail);

            ValidateLockDaysRemaining();

            string strFEMAHasRun = ln.Fields["CX.FEMA.HASRUN"].ToString();
            if (strFEMAHasRun == "")
            {
                errorMessages.Add(strNoFEMACheck);
            }

            if (errorMessages.Count == 0)
            {
                string strValidationLogCurrent = ln.Fields["CX.UWC.DATAVALIDATION.LOG"].ToString();
                string strCurrentUserFullName = ln.Fields["CX.KM.USER.FULLNAME"].ToString();
                ln.Fields["CX.UW.LOGSTATUS"].Value = DateTime.Now;
                ln.Fields["CX.LOAN.STATUS"].Value = "Suspended";
                ln.Fields["CX.LOAN.DEPT"].Value = "Processing";
                ln.Fields["CX.UW.STATUS"].Value = "Suspended";
                ln.Fields["CX.UW.STATUS.U"].Value = strCurrentUserName;
                ln.Fields["CX.UW.STATUS.D"].Value = DateTime.Now;
                ln.Fields["CX.UW.NOTES"].Value = DateTime.Now.ToString("f") + " - " + strCurrentUserName + Environment.NewLine + strUWNotes;
                ln.Fields["CX.UW.LOGSTATUS"].Value = DateTime.Now.ToString("f");
                ln.Fields["2985"].Value = strCurrentUserName;
                ln.Fields["2303"].Value = DateTime.Now;
                ln.Fields["984"].Value = strCurrentUserFullName;
                StoreUWValues();

                if (strValidationLogCurrent != "")
                    ln.Fields["CX.UWC.DATAVALIDATION.LOG"].Value = strValidationLogCurrent + Environment.NewLine + Environment.NewLine + DateTime.Now.ToString("MM/dd/yyyy h:mm tt") + "~" + strCurrentUserName + "~UW Decision - Suspense";
                else if (strValidationLogCurrent == "")
                    ln.Fields["CX.UWC.DATAVALIDATION.LOG"].Value = DateTime.Now.ToString("MM/dd/yyyy h:mm tt") + "~" + strCurrentUserName + "~UW Decision - Suspense";
            }
            else
            {
                HelperMethods.DisplayValidationResults("Loan Data Validation", "Encompass Hard Stops", errorMessages);
            }
        }

        public void UWRFD()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            //MessageBox.Show("Sub to UW fired!");
            List<string> errorMessages = new List<string>();
            string strCurrentUserName = EncompassApplication.CurrentUser.FullName;
            //string strLoanType = ln.Fields["1172"].ToString();
            string strUWNotes = ln.Fields["CX.UW.NOTES.TMP"].ToString();
            string strValidationLogCurrent = ln.Fields["CX.UWC.DATAVALIDATION.LOG"].ToString();
            string strNoFEMACheck = "You must run a FEMA search against the property prior to issuing a loan decision.";

            string strFEMAHasRun = ln.Fields["CX.FEMA.HASRUN"].ToString();
            if (strFEMAHasRun == "")
            {
                errorMessages.Add(strNoFEMACheck);
            }

            if (errorMessages.Count == 0)
            {
                ln.Fields["CX.UW.LOGSTATUS"].Value = DateTime.Now;
                ln.Fields["CX.LOAN.STATUS"].Value = "Recommended for Decline";
                ln.Fields["CX.LOAN.DEPT"].Value = "Processing";
                ln.Fields["CX.UW.STATUS"].Value = "Recommended for Decline";
                ln.Fields["CX.UW.STATUS.U"].Value = strCurrentUserName;
                ln.Fields["CX.UW.STATUS.D"].Value = DateTime.Now;
                ln.Fields["CX.UW.NOTES"].Value = DateTime.Now.ToString("f") + " - " + strCurrentUserName + Environment.NewLine + strUWNotes;
                ln.Fields["CX.UW.LOGSTATUS"].Value = DateTime.Now.ToString("f");
                StoreUWValues();

                if (strValidationLogCurrent != "")
                    ln.Fields["CX.UWC.DATAVALIDATION.LOG"].Value = strValidationLogCurrent + Environment.NewLine + Environment.NewLine + DateTime.Now.ToString("MM/dd/yyyy h:mm tt") + "~" + strCurrentUserName + "~UW Decision - RFD";
                else if (strValidationLogCurrent == "")
                    ln.Fields["CX.UWC.DATAVALIDATION.LOG"].Value = DateTime.Now.ToString("MM/dd/yyyy h:mm tt") + "~" + strCurrentUserName + "~UW Decision - RFD";
            }
            else
            {
                HelperMethods.DisplayValidationResults("Loan Data Validation", "Encompass Hard Stops", errorMessages);
            }
        }

        public void Refinal()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            //MessageBox.Show("Sub to UW fired!");
            List<string> errorMessages = new List<string>();
            string strMsgOldMavent = "You must run a new Mavent report prior to submission/resubmission. Note that if the Mavent report does not pass, any failures will need to be addressed and the report re-ran prior to submission/resubmission.";
            string strMsgFailedMavent = "The most recent Mavent/Compliance report indicates that one or more compliance checks have failed. You must have a passing Mavent report prior to submission/resubmission.";
            string strMsgConvMIFail = "MI is required when loan type is Conventional and LTV is > 80%.";
            string strMsgFHAMIFail = "Loan is missing monthly MI, upfront MI, or both. Both are required on FHA loans.";
            string strMsgAPRTestFail = "APR has increased by .125 or more since the last disclosure. Redisclosure is required.";
            string strMsgLockTestFail = "No redisclosure has taken place since the rate was locked. Redisclosure is required.";
            string strMsgAmortTestFail = "Amortization type has changed sinces last disclosure. Redisclosure is required.";
            string strMsgFVTestFail = "There have been changes to fees since the last disclosures. COC Redisclosure is required.";
            string strMsgMINotNeeded = "This is a Conventional loan with LTV/CLTV < 80% and Mortgage Insurance applied. Please remove the MI to continue.";
            string strLoanType = ln.Fields["1172"].ToString();
            decimal dLTV = ln.Fields["353"].ToDecimal();
            decimal dCLTV = ln.Fields["976"].ToDecimal();
            int subCount = ln.Fields["CX.TRACKING.UW.SUBMISSIONS"].ToInt();

            ValidateMaventRun();
            ValidateMaventResult();
            ValidateMaventHardStop();
            string strMvtRun = ln.Fields["CX.WF.HS.MVTRUN"].ToString();
            string strMvtHardStop = ln.Fields["CX.WF.HS.MVTHARDSTOP"].ToString();

            if (strMvtRun == "FAIL")
                errorMessages.Add(strMsgOldMavent);
            else if (strMvtHardStop == "Y")
                errorMessages.Add(strMsgFailedMavent);

            ValidateMI();
            string strMITestResult = ln.Fields["CX.WF.HS.MITESTRESULT"].ToString();

            if (strMITestResult == "FAIL" && strLoanType == "Conventional")
                errorMessages.Add(strMsgConvMIFail);
            else if (strMITestResult == "FAIL" && strLoanType == "FHA")
                errorMessages.Add(strMsgFHAMIFail);

            string strHasMI = ln.Fields["CX.WF.HS.HASMI"].ToString();

            if (strHasMI == "Y" && strLoanType == "Conventional" && dLTV < 80 && dCLTV < 80)
                errorMessages.Add(strMsgMINotNeeded);

            ValidateDisclosures_APR();
            string strAPRTestResult = ln.Fields["CX.WF.HS.APRTEST"].ToString();

            if (strAPRTestResult == "FAIL")
                errorMessages.Add(strMsgAPRTestFail);

            ValidateDisclosures_LockDate();
            string strLockTestResult = ln.Fields["CX.WF.HS.LOCKDISCTEST"].ToString();

            if (strLockTestResult == "FAIL")
                errorMessages.Add(strMsgLockTestFail);

            ValidateDisclosures_Product();
            string strAmortTestResult = ln.Fields["CX.WF.HS.AMORTDISCTEST"].ToString();

            if (strAmortTestResult == "FAIL")
                errorMessages.Add(strMsgAmortTestFail);

            ValidateDisclosures_Variance();
            string strFVTestResults = ln.Fields["CX.WF.HS.DISCFVTEST"].ToString();

            if (strFVTestResults == "FAIL")
                errorMessages.Add(strMsgFVTestFail);

            if (errorMessages.Count == 0)
            {
                ln.Fields["CX.UW.LOGSTATUS"].Value = DateTime.Now;
                ln.Fields["CX.LOAN.STATUS"].Value = "Resubmitted";
                ln.Fields["CX.LOAN.DEPT"].Value = "Underwriting";
                ln.Fields["CX.UW.STATUS"].Value = "Pending Re-Final Review";
                ln.Fields["CX.TRACKING.UW.SUBMSSIONS"].Value = (++subCount).ToString();
            }
            else
            {
                HelperMethods.DisplayValidationResults("Loan Data Validation", "Encompass Hard Stops", errorMessages);
            }
        }

        public void DocsOut()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            List<string> errorMessages = new List<string>();
            string strMsgOldMavent = "You must run a new Mavent report prior to Docs Out. Note that if the Mavent report does not pass, any failures will need to be addressed and the report re-ran prior to Final Approval.";
            string strMsgFailedMavent = "The most recent Mavent/Compliance report indicates that one or more compliance checks have failed. You must have a passing Mavent report prior to issuing Final Approval.";
            string strMsgConvMIFail = "MI is required when loan type is Conventional and LTV is > 80%.";
            string strMsgFHAMIFail = "Loan is missing monthly MI, upfront MI, or both. Both are required on FHA loans.";
            string strMsgAPRTestFail = "APR has increased by .125 or more since the last disclosure. Redisclosure of the CD is required and a new three day waiting period must elapse prior to signing.";
            string strMsgLockTestFail = "No redisclosure has taken place since the rate was locked. Redisclosure is required.";
            string strMsgAmortTestFail = "Amortization type has changed sinces last disclosure. Redisclosure is required.";
            string strMsgFVTestFail = "There have been changes to fees since the last disclosures. A COC Redisclosure is required.";
            string disbursementDate = ln.Fields["2553"].ToDate().ToShortDateString();
            string lockExpDate = ln.Fields["762"].ToDate().ToShortDateString();
            string strLockDaysTestFail = "The current lock will expire prior to the currently scheduled funding date";
            string strLockProgramMismatch = "The locked loan program and the current loan program do not match.";
            string strStatusFail = "The UW status of the loan must be final approved in order to push loan to Docs Out.";
            string strApprovalExp = "Signing date is after approval expiration date. Either the approval expiration date or the signing date need to be updated. Docs cannot be sent until this is completed.";
            string strMsgLockVarianceFail = "Data has changed since the interest rate was locked. Pricing needs to be validated by the Lock Desk via the Lock Comparison form and any variance must be addressed prior to Docs Out.";
            //string strCurrentUserName = EncompassApplication.CurrentUser.FullName;
            string strLoanType = ln.Fields["1172"].ToString();
            string strCurrentUWStatus = ln.Fields["CX.UW.STATUS"].ToString();

            if (strCurrentUWStatus != "Final Approved")
                errorMessages.Add(strStatusFail);

            if (Convert.ToDateTime(disbursementDate) <= Convert.ToDateTime(lockExpDate))
                errorMessages.Add(strLockDaysTestFail);

            ValidateMaventRun();
            ValidateMaventResult();
            ValidateMaventHardStop();
            string strMvtRun = ln.Fields["CX.WF.HS.MVTRUN"].ToString();
            string strMvtHardStop = ln.Fields["CX.WF.HS.MVTHARDSTOP"].ToString();

            if (strMvtRun == "FAIL")
                errorMessages.Add(strMsgOldMavent);
            else if (strMvtHardStop == "Y")
                errorMessages.Add(strMsgFailedMavent);

            ValidateMI();
            string strMITestResult = ln.Fields["CX.WF.HS.MITESTRESULT"].ToString();

            if (strMITestResult == "FAIL" && strLoanType == "Conventional")
                errorMessages.Add(strMsgConvMIFail);
            else if (strMITestResult == "FAIL" && strLoanType == "FHA")
                errorMessages.Add(strMsgFHAMIFail);

            ValidateDisclosures_APR();
            string strAPRTestResult = ln.Fields["CX.WF.HS.APRTEST"].ToString();

            if (strAPRTestResult == "FAIL")
                errorMessages.Add(strMsgAPRTestFail);

            ValidateDisclosures_LockDate();
            string strLockTestResult = ln.Fields["CX.WF.HS.LOCKDISCTEST"].ToString();

            if (strLockTestResult == "FAIL")
                errorMessages.Add(strMsgLockTestFail);

            ValidateDisclosures_Product();
            string strAmortTestResult = ln.Fields["CX.WF.HS.AMORTDISCTEST"].ToString();

            if (strAmortTestResult == "FAIL")
                errorMessages.Add(strMsgAmortTestFail);

            ValidateDisclosures_Variance();
            string strFVTestResults = ln.Fields["CX.WF.HS.DISCFVTEST"].ToString();

            if (strFVTestResults == "FAIL")
                errorMessages.Add(strMsgFVTestFail);

            ValidateLockDaysRemaining();
            //int iLockDays = ln.Fields["CX.WF.HS.LOCKDAYSREM"].ToInt();
            string strProgramVariance = ln.Fields["CX.LOAN.LOCK.PRGRMVARIANCE"].ToString();

            if (strProgramVariance == "Y")
                errorMessages.Add(strLockProgramMismatch);

            DateTime dEstClosingDate = ln.Fields["763"].ToDate();
            DateTime dApprovalExpDate = ln.Fields["CX.EXP.APPROVAL.EXPDATE"].ToDate();

            if (dEstClosingDate > dApprovalExpDate)
                errorMessages.Add(strApprovalExp);

            string strLockVarianceTestResult = ln.Fields["CX.LCKC.VARIANCE"].ToString();

            if (strLockVarianceTestResult != "N")
                errorMessages.Add(strMsgLockVarianceFail);

            if (errorMessages.Count == 0)
                ln.Fields["CX.LOAN.STATUS"].Value = "Docs Out";
            else
                HelperMethods.DisplayValidationResults("Loan Data Validation", "Encompass Hard Stops", errorMessages);
        }

        public void FundLoan()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            List<string> errorMessages = new List<string>();
            string strMsgOldMavent = "You must run a new Mavent report prior to Funding. Note that if the Mavent report does not pass, any failures will need to be addressed and the report re-ran prior to Final Approval.";
            string strMsgFailedMavent = "The most recent Mavent/Compliance report indicates that one or more compliance checks have failed. You must have a passing Mavent report prior to issuing Final Approval.";
            string strMsgConvMIFail = "MI is required when loan type is Conventional and LTV is > 80%.";
            string strMsgFHAMIFail = "Loan is missing monthly MI, upfront MI, or both. Both are required on FHA loans.";
            string strMsgAPRTestFail = "APR has increased by .125 or more since the last disclosure. Redisclosure of the CD is required and a new three day waiting period must elapse prior to signing.";
            string strMsgFVTestFail = "There have been changes to fees since the last disclosures. A COC Redisclosure is required.";
            string lockExpDate = ln.Fields["762"].ToDate().ToShortDateString();
            string strLockProgramMismatch = "The locked loan program and the current loan program do not match.";
            string strStatusFail = "The UW status of the loan must be final approved in order to complete funding.";
            string strLockExpired = "Loan cannot be funded with an expired rate lock.";
            string strMSGOpenConds = "All Prior To Funding or earlier conditions much be satisfied, waived, or moved to Prior to Purchase before Funding can be completed.";
            string strMsgLockVarianceFail = "Data has changed since the interest rate was locked. Pricing needs to be validated by the Lock Desk via the Lock Comparison form and any variance must be addressed prior to funding.";
            //string strCurrentUserName = EncompassApplication.CurrentUser.FullName;
            string strLoanType = ln.Fields["1172"].ToString();
            string strCurrentUWStatus = ln.Fields["CX.UW.STATUS"].ToString();
            string todaysDate = DateTime.Now.ToShortDateString();
            MilestoneEvent milestone = ln.Log.MilestoneEvents.GetEventForMilestone("Funded");

            if (strCurrentUWStatus != "Final Approved")
                errorMessages.Add(strStatusFail);

            if (Convert.ToDateTime(todaysDate) > Convert.ToDateTime(lockExpDate))
                errorMessages.Add(strLockExpired);

            ValidateMaventRun();
            ValidateMaventResult();
            ValidateMaventHardStop();
            string strMvtRun = ln.Fields["CX.WF.HS.MVTRUN"].ToString();
            string strMvtHardStop = ln.Fields["CX.WF.HS.MVTHARDSTOP"].ToString();

            if (strMvtRun == "FAIL")
                errorMessages.Add(strMsgOldMavent);
            else if (strMvtHardStop == "Y")
                errorMessages.Add(strMsgFailedMavent);

            ValidateMI();
            string strMITestResult = ln.Fields["CX.WF.HS.MITESTRESULT"].ToString();

            if (strMITestResult == "FAIL" && strLoanType == "Conventional")
                errorMessages.Add(strMsgConvMIFail);
            else if (strMITestResult == "FAIL" && strLoanType == "FHA")
                errorMessages.Add(strMsgFHAMIFail);

            ValidateDisclosures_APR();
            string strAPRTestResult = ln.Fields["CX.WF.HS.APRTEST"].ToString();

            if (strAPRTestResult == "FAIL")
                errorMessages.Add(strMsgAPRTestFail);

            ValidateDisclosures_Variance();
            string strFVTestResults = ln.Fields["CX.WF.HS.DISCFVTEST"].ToString();

            if (strFVTestResults == "FAIL")
                errorMessages.Add(strMsgFVTestFail);
 
            string strProgramVariance = ln.Fields["CX.LOAN.LOCK.PRGRMVARIANCE"].ToString();

            if (strProgramVariance == "Y")
                errorMessages.Add(strLockProgramMismatch);

            int iOpenPTACount = ln.Fields["CX.LOAN.OPENPTAS"].ToInt();
            int iOpenPTDCount = ln.Fields["CX.LOAN.OPENPTDS"].ToInt();
            int iOpenPTFCount = ln.Fields["CX.LOAN.OPENPTFS"].ToInt();
            int iOpenACCount = ln.Fields["CX.LOAN.OPENACS"].ToInt();

            if (iOpenPTACount > 0 || iOpenPTDCount > 0 || iOpenPTFCount > 0 || iOpenACCount > 0)
                errorMessages.Add(strMSGOpenConds);

            string strLockVarianceTestResult = ln.Fields["CX.LCKC.VARIANCE"].ToString();

            if (strLockVarianceTestResult != "N")
                errorMessages.Add(strMsgLockVarianceFail);

            if (errorMessages.Count == 0)
            {
                ln.Fields["CX.LOAN.STATUS"].Value = "Funded";
                ln.Fields["CX.LOAN.DEPT"].Value = "Post-Closing";
                milestone.AdjustDate(DateTime.Now, true, true);
                milestone.Completed = true;
            }
            else
            {
                HelperMethods.DisplayValidationResults("Loan Data Validation", "Encompass Hard Stops", errorMessages);
            }
        }

        public void LogUWCClear()
        {
            Loan ln = EncompassApplication.CurrentLoan;
            string strValidationLogCurrent = ln.Fields["CX.UWC.DATAVALIDATION.LOG"].ToString();
            string strCurrentUserName = EncompassApplication.CurrentUser.FullName;

            if (strValidationLogCurrent != "")
                ln.Fields["CX.UWC.DATAVALIDATION.LOG"].Value = strValidationLogCurrent + System.Environment.NewLine + System.Environment.NewLine + DateTime.Now.ToString("MM/dd/yyyy h:mm tt") + "~" + strCurrentUserName + "~UW Comparison - Clear";
            else if (strValidationLogCurrent == "")
                ln.Fields["CX.UWC.DATAVALIDATION.LOG"].Value = DateTime.Now.ToString("MM/dd/yyyy h:mm tt") + "~" + strCurrentUserName + "~UW Comparison - Clear";
        }

        //Stores all values as they exist currently for use with the UW Comparison Form
        public void StoreUWValues()
        {
            Loan ln = EncompassApplication.CurrentLoan;

            for(int i = 1; i <= 2; ++i)
            {
                string baseFieldString = "CX.UWC.BP" + i + ".";

                //Borrower Info    
                string strBP1BDOB = ln.Fields["1402"].ToString();

                ln.Fields[baseFieldString + "BFIRSTNAME"].Value = ln.Fields["4000"].ToString();
                ln.Fields[baseFieldString + "BMIDNAME"].Value = ln.Fields["4001"].ToString();
                ln.Fields[baseFieldString + "BLASTNAME"].Value = ln.Fields["4002"].ToString();
                ln.Fields[baseFieldString + "BSUFFIX"].Value = ln.Fields["4003"].ToString();
                ln.Fields[baseFieldString + "BSSN"].Value = ln.Fields["65"].ToString();
                ln.Fields[baseFieldString + "BMARITAL"].Value = ln.Fields["52"].ToString();

                if (strBP1BDOB != "//")
                    ln.Fields[baseFieldString + "BDOB"].Value = strBP1BDOB;

                //Co - Borrower Info
                string strBP1CBDOB = ln.Fields["1403"].ToString();

                if (strBP1CBDOB != "//")
                    ln.Fields[baseFieldString + "CBDOB"].Value = strBP1CBDOB;

                ln.Fields[baseFieldString + "CBFIRSTNAME"].Value = ln.Fields["4004"].ToString();
                ln.Fields[baseFieldString + "CBMIDNAME"].Value = ln.Fields["4005"].ToString();
                ln.Fields[baseFieldString + "CBLASTNAME"].Value = ln.Fields["4006"].ToString();
                ln.Fields[baseFieldString + "CBSUFFIX"].Value = ln.Fields["4007"].ToString();
                ln.Fields[baseFieldString + "CBSSN"].Value = ln.Fields["97"].ToString();
                ln.Fields[baseFieldString + "CBMARITAL"].Value = ln.Fields["84"].ToString();
            }

            //Subject Property Information
            ln.Fields["CX.UWC.SPI.STREETADDRESS"].Value = ln.Fields["URLA.X73"].ToString();
            ln.Fields["CX.UWC.SPI.UNITTYPE"].Value = ln.Fields["URLA.X74"].ToString();
            ln.Fields["CX.UWC.SPI.UNITNUMBER"].Value = ln.Fields["URLA.X75"].ToString();
            ln.Fields["CX.UWC.SPI.CITY"].Value = ln.Fields["12"].ToString();
            ln.Fields["CX.UWC.SPI.STATE"].Value = ln.Fields["14"].ToString();

            //ZIP CODE STUFF HERE - This code is to account for many different variations of # of digits in the zip code
            //'Add the '-' character to a zip with more than 5 digits since Encompass does this as well.
            //Otherwise the copied zip will never match the zip in field 15 which includes the '-'
            string strCurrentZip = ln.Fields["15"].ToString();
            int iCZLen = strCurrentZip.Length;
            string strUWCZip = "";
            string strPartOne = strCurrentZip.Substring(0, 5);
            string strPartTwo;

            if (iCZLen <= 5)
            {
                strUWCZip = strCurrentZip;
            }
            else if (iCZLen == 6)
            {
                strPartTwo = strCurrentZip.Substring(5, 1);
                strUWCZip = strPartOne + "-" + strPartTwo;
            }
            else if (iCZLen == 7)
            {
                strPartTwo = strCurrentZip.Substring(5, 2);
                strUWCZip = strPartOne + "-" + strPartTwo;
            }
            else if (iCZLen == 8)
            {
                strPartTwo = strCurrentZip.Substring(5, 3);
                strUWCZip = strPartOne + "-" + strPartTwo;
            }
            else if (iCZLen == 9)
            {
                strPartTwo = strCurrentZip.Substring(5, 4);
                strUWCZip = strPartOne + "-" + strPartTwo;
            }
            else if (iCZLen >= 9)
            {
                strPartTwo = strCurrentZip.Substring(5, 4);
                strUWCZip = strPartOne + "-" + strPartTwo;
            }

            ln.Fields["CX.UWC.SPI.ZIP"].Value = strUWCZip;
            // END ZIP CODE STUFF

            ln.Fields["CX.UWC.SPI.COUNTY"].Value = ln.Fields["13"].ToString();
            ln.Fields["CX.UWC.SPI.UNITS"].Value = ln.Fields["16"].ToInt();
            ln.Fields["CX.UWC.SPI.ESTVALUE"].Value = ln.Fields["1821"].ToInt();
            ln.Fields["CX.UWC.SPI.ACTUALVALUE"].Value = ln.Fields["356"].ToInt();
            ln.Fields["CX.UWC.SPI.APPRAISALTYPE"].Value = ln.Fields["CX.LOAN.APPRAISALTYPE"].ToString();

            //Project Type Information
            ln.Fields["CX.UWC.SPI.PT.CONDO"].Value = ln.Fields["URLA.X205"].ToString();
            ln.Fields["CX.UWC.SPI.PT.COOP"].Value = ln.Fields["URLA.X206"].ToString();
            ln.Fields["CX.UWC.SPI.PT.PUD"].Value = ln.Fields["URLA.X207"].ToString();
            ln.Fields["CX.UWC.SPI.PT.NOPROJECT"].Value = ln.Fields["URLA.X208"].ToString();
            ln.Fields["CX.UWC.SPI.PT.ATTACHTYPE"].Value = ln.Fields["ULDD.X177"].ToString();
            ln.Fields["CX.UWC.SPI.PT.PROPERTYTYPE"].Value = ln.Fields["CX.PROPERTY.TYPE.GLOBAL"].ToString();
            ln.Fields["CX.UWC.SPI.PT.PROJDESIGNTYPE"].Value = ln.Fields["ULDD.X140"].ToString();
            ln.Fields["CX.UWC.SPI.PT.CONSTTYPE"].Value = ln.Fields["ULDD.X187"].ToString();

            //Transaction Details - LTV
            ln.Fields["CX.UWC.TD.LTV"].Value = ln.Fields["353"].ToDecimal();
            ln.Fields["CX.UWC.TD.CLTV"].Value = ln.Fields["976"].ToDecimal();
            ln.Fields["CX.UWC.TD.SUBFINANCING"].Value = ln.Fields["140"].ToDecimal();

            //Transaction Details - Main
            ln.Fields["CX.UWC.TD.AUSTYPE"].Value = ln.Fields["1543"].ToString();
            ln.Fields["CX.UWC.TD.AUSRESPONSE"].Value = ln.Fields["1544"].ToString();
            ln.Fields["CX.UWC.TD.AUSSUBNUM"].Value = ln.Fields["CX.UWRA.SUBMISSION.NUM"].ToString();

            string strCaseDate = ln.Fields["3042"].ToString();

            ln.Fields["CX.UWC.TD.CASENUMBER"].Value = ln.Fields["1040"].ToString();
            ln.Fields["CX.UWC.TD.VETERANTYPE"].Value = ln.Fields["VAVOB.X72"].ToString();
            ln.Fields["CX.UWC.TD.VETERANSTATUS"].Value = ln.Fields["955"].ToString();

            if (strCaseDate != "//")
                ln.Fields["CX.UWC.TD.CASEDATE"].Value = strCaseDate;

            ln.Fields["CX.UWC.TD.FFSTATUS"].Value = ln.Fields["990"].ToString();
            ln.Fields["CX.UWC.TD.MIPFFGUARANTEE"].Value = ln.Fields["969"].ToString();
            ln.Fields["CX.UWC.TD.MILOCK"].Value = ln.Fields["1765"].ToString();
            ln.Fields["CX.UWC.TD.PMICASHPYMT"].Value = ln.Fields["3033"].ToDecimal();

            ln.Fields["CX.UWC.TD.LOANAMOUNT"].Value = ln.Fields["1109"].ToDecimal();
            ln.Fields["CX.UWC.TD.PMIMIPFINANCED"].Value = ln.Fields["NEWHUD2.X2187"].ToDecimal();
            ln.Fields["CX.UWC.TD.TOTALLOANAMT"].Value = ln.Fields["2"].ToDecimal();

            ln.Fields["CX.UWC.TD.LOANPURPOSE"].Value = ln.Fields["19"].ToString();
            ln.Fields["CX.UWC.TD.REFIPURPOSE"].Value = ln.Fields["299"].ToString();
            ln.Fields["CX.UWC.TD.REFITYPE"].Value = ln.Fields["URLA.X165"].ToString();
            ln.Fields["CX.UWC.TD.REFIPROGRAM"].Value = ln.Fields["URLA.X166"].ToString();

            ln.Fields["CX.UWC.TD.OCCUPANCY"].Value = ln.Fields["1811"].ToString();
            ln.Fields["CX.UWC.TD.LOANTYPE"].Value = ln.Fields["1172"].ToString();
            ln.Fields["CX.UWC.TD.LIENPOS"].Value = ln.Fields["420"].ToString();
            ln.Fields["CX.UWC.TD.AMORTIZATION"].Value = ln.Fields["608"].ToString();
            ln.Fields["CX.UWC.TD.TERM"].Value = ln.Fields["1347"].ToInt();
            ln.Fields["CX.UWC.TD.INTERESTRATE"].Value = ln.Fields["3"].ToDecimal();

            ln.Fields["CX.UWC.TD.LOANPROGRAM"].Value = ln.Fields["1401"].ToString();
            ln.Fields["CX.UWC.TD.HPELIGIBLE"].Value = ln.Fields["CX.LOAN.HPELIGIBLE"].ToString();
            ln.Fields["CX.UWC.TD.HRELIGIBLE"].Value = ln.Fields["CX.LOAN.HRELIGIBLE"].ToString();
            ln.Fields["CX.UWC.TD.CREDREFNUM"].Value = ln.Fields["300"].ToString();
            ln.Fields["CX.UWC.TD.QUALFICO"].Value = ln.Fields["VASUMM.X23"].ToInt();

            ln.Fields["CX.UWC.TD.TOTAlINCOME"].Value = ln.Fields["736"].ToDecimal();
            ln.Fields["CX.UWC.TD.NETRENTALINCOME"].Value = ln.Fields["924"].ToDecimal();
            ln.Fields["CX.UWC.TD.PROPERTYCOUNT"].Value = ln.Fields["CX.KM.VOM.COUNT"].ToInt();
            ln.Fields["CX.UWC.TD.FRONTENDRATIO"].Value = ln.Fields["740"].ToDecimal();
            ln.Fields["CX.UWC.TD.BACKENDRATIO"].Value = ln.Fields["742"].ToDecimal();

            //Proposed Monthly Payments
            ln.Fields["CX.UWC.PMP.PANDI"].Value = ln.Fields["228"].ToDecimal();
            ln.Fields["CX.UWC.PMP.SUBOPANDI"].Value = ln.Fields["229"].ToDecimal();
            ln.Fields["CX.UWC.PMP.HOI"].Value = ln.Fields["230"].ToDecimal();
            ln.Fields["CX.UWC.PMP.SPI"].Value = ln.Fields["URLA.X144"].ToDecimal();
            ln.Fields["CX.UWC.PMP.PROPTAXES"].Value = ln.Fields["1405"].ToDecimal();
            ln.Fields["CX.UWC.PMP.MI"].Value = ln.Fields["232"].ToDecimal();
            ln.Fields["CX.UWC.PMP.DUES"].Value = ln.Fields["233"].ToDecimal();
            ln.Fields["CX.UWC.PMP.OTHER"].Value = ln.Fields["234"].ToDecimal();
            ln.Fields["CX.UWC.PMP.TOTAL"].Value = ln.Fields["912"].ToDecimal();
            ln.Fields["CX.UWC.PMP.OTHERPYMTS"].Value = ln.Fields["350"].ToDecimal();
            ln.Fields["CX.UWC.PMP.TOTALPYMTS"].Value = ln.Fields["1187"].ToDecimal();
            ln.Fields["CX.UWC.PMP.CASHTOFROMBORR"].Value = ln.Fields["142"].ToDecimal();

            //Income Fields
            string strBP1BStartDate = ln.Fields["FE0151"].ToString();

            ln.Fields["CX.UWC.INC.BP1.BEMPLOYER"].Value = ln.Fields["FE0102"].ToString();
            ln.Fields["CX.UWC.INC.BP1.BPOSITION"].Value = ln.Fields["FE0110"].ToString();
            ln.Fields["CX.UWC.INC.BP1.BFAMILY"].Value = ln.Fields["FE0154"].ToString();
            ln.Fields["CX.UWC.INC.BP1.BSELFEMPL"].Value = ln.Fields["FE0115"].ToString();
            ln.Fields["CX.UWC.INC.BP1.BSEGTR25"].Value = ln.Fields["FE0155"].ToString();
            ln.Fields["CX.UWC.INC.BP1.BSELTN25"].Value = ln.Fields["FE0155"].ToString();
            ln.Fields["CX.UWC.INC.BP1.BSEINCOME"].Value = ln.Fields["FE0156"].ToDecimal();

            if (strBP1BStartDate != "//")
                ln.Fields["CX.UWC.INC.BP1.BDATE"].Value = strBP1BStartDate;
            
            string strBP1CBStartDate = ln.Fields["FE0251"].ToString();

            ln.Fields["CX.UWC.INC.BP1.CBEMPLOYER"].Value = ln.Fields["FE0202"].ToString();
            ln.Fields["CX.UWC.INC.BP1.CBPOSITION"].Value = ln.Fields["FE0210"].ToString();
            ln.Fields["CX.UWC.INC.BP1.CBFAMILY"].Value = ln.Fields["FE0254"].ToString();
            ln.Fields["CX.UWC.INC.BP1.CBSELFEMPL"].Value = ln.Fields["FE0215"].ToString();
            ln.Fields["CX.UWC.INC.BP1.CBSEGTR25"].Value = ln.Fields["FE0255"].ToString();
            ln.Fields["CX.UWC.INC.BP1.CBSELTN25"].Value = ln.Fields["FE0255"].ToString();
            ln.Fields["CX.UWC.INC.BP1.CBSEINCOME"].Value = ln.Fields["FE0256"].ToDecimal();

            if (strBP1CBStartDate != "//")
                ln.Fields["CX.UWC.INC.BP1.CBDATE"].Value = strBP1CBStartDate;

            ln.Fields["CX.UWC.INC.BP1.BBASE"].Value = ln.Fields["FE0119"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.BOT"].Value = ln.Fields["FE0120"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.BBONUS"].Value = ln.Fields["FE0121"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.BCOMMISSION"].Value = ln.Fields["FE0122"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.BMILENT"].Value = ln.Fields["FE0153"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.BOTHER"].Value = ln.Fields["FE0123"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.BTOTAL"].Value = ln.Fields["FE0112"].ToDecimal();

            ln.Fields["CX.UWC.INC.BP1.CBBASE"].Value = ln.Fields["FE0219"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.CBOT"].Value = ln.Fields["FE0220"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.CBBONUS"].Value = ln.Fields["FE0221"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.CBCOMMISSION"].Value = ln.Fields["FE0222"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.CBMILENT"].Value = ln.Fields["FE0253"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.CBOTHER"].Value = ln.Fields["FE0223"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.CBTOTAL"].Value = ln.Fields["FE0212"].ToDecimal();

            string strBP1BAStartDate = ln.Fields["FE0351"].ToString();

            ln.Fields["CX.UWC.INC.BP1.BAEMPLOYER"].Value = ln.Fields["FE0302"].ToString();
            ln.Fields["CX.UWC.INC.BP1.BAPOSITION"].Value = ln.Fields["FE0310"].ToString();
            ln.Fields["CX.UWC.INC.BP1.BAFAM"].Value = ln.Fields["FE0354"].ToString();
            ln.Fields["CX.UWC.INC.BP1.BASELFEMPL"].Value = ln.Fields["FE0315"].ToString();
            ln.Fields["CX.UWC.INC.BP1.BASEGTR25"].Value = ln.Fields["FE0355"].ToString();
            ln.Fields["CX.UWC.INC.BP1.BASELTN25"].Value = ln.Fields["FE0355"].ToString();

            if (strBP1BAStartDate != "//")
                ln.Fields["CX.UWC.INC.BP1.BADATE"].Value = strBP1BAStartDate;
            
            string strBP1CBAStartDate = ln.Fields["FE0451"].ToString();

            ln.Fields["CX.UWC.INC.BP1.CBAEMPLOYER"].Value = ln.Fields["FE0402"].ToString();
            ln.Fields["CX.UWC.INC.BP1.CBAPOSITION"].Value = ln.Fields["FE0410"].ToString();
            ln.Fields["CX.UWC.INC.BP1.CBAFAM"].Value = ln.Fields["FE0454"].ToString();
            ln.Fields["CX.UWC.INC.BP1.CBASE"].Value = ln.Fields["FE0415"].ToString();
            ln.Fields["CX.UWC.INC.BP1.CBASEGTR25"].Value = ln.Fields["FE0455"].ToString();
            ln.Fields["CX.UWC.INC.BP1.CBASELTN25"].Value = ln.Fields["FE0455"].ToString();

            if (strBP1CBAStartDate != "//")
                ln.Fields["CX.UWC.INC.BP1.CBADATE"].Value = strBP1CBAStartDate;

            ln.Fields["CX.UWC.INC.BP1.BABASE"].Value = ln.Fields["FE0319"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.BAOT"].Value = ln.Fields["FE0320"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.BABONUS"].Value = ln.Fields["FE0321"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.BACOMMISSION"].Value = ln.Fields["FE0322"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.BAMILENT"].Value = ln.Fields["FE0353"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.BAOTHER"].Value = ln.Fields["FE0323"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.BATOTAL"].Value = ln.Fields["FE0312"].ToDecimal();

            ln.Fields["CX.UWC.INC.BP1.CBABASE"].Value = ln.Fields["FE0419"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.CBAOT"].Value = ln.Fields["FE0420"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.CBABONUS"].Value = ln.Fields["FE0421"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.CBACOMMISSION"].Value = ln.Fields["FE0422"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.CBAMILENT"].Value = ln.Fields["FE0453"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.CBAOTHER"].Value = ln.Fields["FE0423"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.CBATOTAL"].Value = ln.Fields["FE0412"].ToDecimal();

            ln.Fields["CX.UWC.INC.BP1.BORRCB1"].Value = ln.Fields["URLAROIS0102"].ToString();
            ln.Fields["CX.UWC.INC.BP1.SOURCE1"].Value = ln.Fields["URLAROIS0118"].ToString();
            ln.Fields["CX.UWC.INC.BP1.INCOME1"].Value = ln.Fields["URLAROIS0122"].ToDecimal();

            ln.Fields["CX.UWC.INC.BP1.BORRCB2"].Value = ln.Fields["URLAROIS0202"].ToString();
            ln.Fields["CX.UWC.INC.BP1.SOURCE2"].Value = ln.Fields["URLAROIS0218"].ToString();
            ln.Fields["CX.UWC.INC.BP1.INCOME2"].Value = ln.Fields["URLAROIS0222"].ToDecimal();

            ln.Fields["CX.UWC.INC.BP1.BORRCB3"].Value = ln.Fields["URLAROIS0302"].ToString();
            ln.Fields["CX.UWC.INC.BP1.SOURCE3"].Value = ln.Fields["URLAROIS0318"].ToString();
            ln.Fields["CX.UWC.INC.BP1.INCOME3"].Value = ln.Fields["URLAROIS0322"].ToDecimal();

            ln.Fields["CX.UWC.INC.BP1.BOTHERTOTAL"].Value = ln.Fields["URLA.X42"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.CBOTHERTOTAL"].Value = ln.Fields["URLA.X43"].ToDecimal();
            ln.Fields["CX.UWC.INC.BP1.BPTOTINCOME"].Value = ln.Fields["URLA.X44"].ToDecimal();
        }
    }
}