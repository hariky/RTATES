using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using TESFramework;
using TESConfigFramework;
using System.Xml;
using System.Xml.Linq;

using System.Data;
using System.Web.UI.HtmlControls;
using System.Net;
using System.Text;
using System.Web.Services;
using System.Threading;
using System.Collections.Specialized;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using TESFramework.Utils;
using TESFramework.Session;

public partial class MitigationsApplicationWizard : BaseWebPage
{
    private const string VALIDATE_STATUS_KEY = "VALIDATE_STATUS";
    private const string IS_TEMP_KEY = "IS_TEMP";
    private const string ERROR_MESSAGE = "ERROR_MESSAGE";
    private const string OLD_MIT_APPLICATION = "OLD_MIT_APPLICATION";

    public const string SECTION_NAME = "SectionName";
    public const string SECTION_NAME_NODE = "SectionNameNode";
    public const string FIELD = "Field";
    public const string FIELD_NODE = "FieldNode";
    public const string ORIGINAL_VALUE = "OriginalValue";
    public const string MODIFIED_VALUE = "ModifiedValue";
    private string FinalStepTitle = "Confirmation";
    public string postBackUrlInt { get { return Page.ResolveUrl("~/WebPages/Consultant/MITLists.aspx"); } }

    public List<UploadedDocuments> FilesToUpload { get; set; }

    public List<UploadedDocuments> FilesToArchive { get; set; }

    public override List<int> PageRoles
    {
        get
        {
            return ResourceAuthorization.AccessToCustomerPages;
        }
    }


    private bool requestedForModification
    {
        get
        {

            if (Session[MitigationApplication.IsModify] != null && (bool)Session[MitigationApplication.IsModify])
                return true;
            else
                return false;
        }
    }
    private bool m_LoadMITApplication = false;

    protected override void OnPreInit(EventArgs e)
    {
        if (Log != null && Log.Trace) { Log.WriteTrace("MitigationsApplication.aspx::OnPreInit:: Start"); };
        base.OnPreInit(e);
        try
        {
            if (!IsPostBack)
            {
                // CLEAN ALL SESSION VARIABLES
                Session.Remove(UploadedDocuments.SUPPORTING_DOCUMENTS_KEY);
                Session.Remove(UploadedDocuments.Uploaded_Files_KEY);
                Session.Remove(OLD_MIT_APPLICATION);
                Session.Remove(UploadedDocuments.FILES_MARKED_FOR_ARCHIVING);
                if (!AppSession.UserInfo.ConsultantActive.HasValue || !AppSession.UserInfo.ConsultantActive.Value ||
                    !AppSession.UserInfo.ConsUserActive.HasValue || !AppSession.UserInfo.ConsUserActive.Value)
                    Response.Redirect(Page.ResolveUrl("~/WebPages/Consultant/MITLists.aspx"));
                WebConstants.MSG_ALL_FIELDS_ARE_MANDATORY = GetResourceString(ResourceConstants.MSG_ALL_FIELDS_ARE_MANDATORY_UNLESS_SPECIFIED, "(All fields are mandatory unless specified otherwise)");
                if (base.AppConfiguration != null)
                {
                    if (!String.IsNullOrEmpty(TESFramework.Utils.Utils.DecodeAndParseQueryStringValue(WebConstants.IsTempMIT))
                        && !String.IsNullOrEmpty(TESFramework.Utils.Utils.DecodeAndParseQueryStringValue(WebConstants.MITAppNumber))
                        && !String.IsNullOrEmpty(TESFramework.Utils.Utils.DecodeAndParseQueryStringValue(WebConstants.TESReferenceNumber))) // Temporary MIT Application
                    {
                        ViewState[IS_TEMP_KEY] = "True";
                        AppSession.MITApplication = MitigationApplication.GetMITTempSavingByID(AppSession, base.AppConfiguration,

                            TESFramework.Utils.Utils.DecodeAndParseQueryStringValue(WebConstants.MITAppNumber), TESFramework.Utils.Utils.DecodeAndParseQueryStringValue(WebConstants.TESReferenceNumber));
                        Session[OLD_MIT_APPLICATION] = Copy(AppSession.MITApplication);
                        m_LoadMITApplication = true;
                    }
                    else
                    {                        AppSession.MITApplication = new MitigationApplication();

                        if (!string.IsNullOrEmpty(TESFramework.Utils.Utils.DecodeAndParseQueryStringValue(WebConstants.MITAppNumber))) // Existing MIT Application
                        {
                            string mitRefNo = TESFramework.Utils.Utils.DecodeAndParseQueryStringValue(WebConstants.MITAppNumber);
                            string tisRefNo = TESFramework.Utils.Utils.DecodeAndParseQueryStringValue(WebConstants.TESReferenceNumber);

                            AppSession.MITApplication = MitigationApplication.LoadMITApplnForEdit(AppSession, AppConfiguration, mitRefNo, tisRefNo);
                            if (CoreFramework.Utils.Utils.GetString(TESFramework.Utils.Utils.DecodeAndParseQueryStringValue(WebConstants.EditMIT)) == "true" || requestedForModification)
                            {
                                Session[OLD_MIT_APPLICATION] = Copy(AppSession.MITApplication);
                            }
                            m_LoadMITApplication = true;
                        }
                    }

                    if (AppSession != null && AppSession.UserInfo != null && AppSession.UserInfo.Roles != null)
                    {
                        LoadTESApplication();

                        if (AppSession.TESApplication == null )
                            return;

                    }
                }
            }

            LoadMITSectionsInWizard();
        }
        catch (Exception ex)
        {
            if (Log != null) Log.WriteError("MitigationsApplication.aspx::OnPreInit::Error - {0}", ex.Message);
        }
        if (Log != null && Log.Trace) { Log.WriteTrace("MitigationsApplication.aspx::OnPreInit:: End"); };
    }

    public static object Copy(object source)
    {

        if (source == null)
            return null;

        else
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();

            using (stream)
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);

                return formatter.Deserialize(stream);
            }
        }
    }

    private void LocalizeCaptions()
    {
        try
        {
            Button btn = (Button)wcMITApplication.FindControl("StartNavigationTemplateContainerID").FindControl("btnCancel");
            if (btn != null)
            {
                btn.Attributes.Add("onclick", "ConfirmNewMITApplicationCancel('" + GetResourceString(ResourceConstants.CANCEL_APPLICATION_CONFIRMATION_MSG_SINGLE_APP, "Are you sure you want to cancel?") + "'); return false;");
                btn.Text = GetResourceString(ResourceConstants.WP_CUST_TESAPP_BTNCANCEL, "Cancel");
            }

            btn = (Button)wcMITApplication.FindControl("StartNavigationTemplateContainerID").FindControl("btnNext");
            if (btn != null) btn.Text = GetResourceString(ResourceConstants.WP_CUST_TESAPP_BTNNEXT, "Next >>");

            btn = (Button)wcMITApplication.FindControl("StartNavigationTemplateContainerID").FindControl("btnTemporarySave");

            if (btn != null)
            {
                if (!requestedForModification && string.IsNullOrEmpty(TESFramework.Utils.Utils.DecodeAndParseQueryStringValue(WebConstants.IsRevalidateTES)))
                {
                    btn.Text = GetResourceString(ResourceConstants.WP_CUST_TESAPP_BTNTEMP, "Temporary Save");

                    btn.Visible = true;

                }
                else
                {
                    btn.Visible = false;
                }
            }

            btn = (Button)wcMITApplication.FindControl("StepNavigationTemplateContainerID").FindControl("btnTemporarySave");
            if (btn != null)
            {
                if (!requestedForModification && string.IsNullOrEmpty(TESFramework.Utils.Utils.DecodeAndParseQueryStringValue(WebConstants.isEdit)))
                {
                    btn.Text = GetResourceString(ResourceConstants.WP_CUST_TESAPP_BTNTEMP, "Temporary Save");

                    btn.Visible = true;

                }
                else
                {
                    btn.Visible = false;
                }
            }

            btn = (Button)wcMITApplication.FindControl("StepNavigationTemplateContainerID").FindControl("btnCancel");
            if (btn != null)
            {
                btn.Attributes.Add("onclick", "ConfirmNewMITApplicationCancel('" + GetResourceString(ResourceConstants.CANCEL_APPLICATION_CONFIRMATION_MSG_SINGLE_APP, "Are you sure you want to cancel?") + "');return false;");
                btn.Text = GetResourceString(ResourceConstants.WP_CUST_TESAPP_BTNCANCEL, "Cancel");
            }

            btn = (Button)wcMITApplication.FindControl("StepNavigationTemplateContainerID").FindControl("btnPrevious");
            if (btn != null) btn.Text = GetResourceString(ResourceConstants.WP_CUST_TESAPP_BTNPREVIOUS, "<< Previous");

            btn = (Button)wcMITApplication.FindControl("StepNavigationTemplateContainerID").FindControl("btnNext");
            if (btn != null) btn.Text = GetResourceString(ResourceConstants.WP_CUST_TESAPP_BTNNEXT, "Next >>");


            btn = (Button)wcMITApplication.FindControl("FinishNavigationTemplateContainerID").FindControl("btnCancel");
            if (btn != null)
            {
                btn.Attributes.Add("onclick", "ConfirmNewMITApplicationCancel('" + GetResourceString(ResourceConstants.CANCEL_APPLICATION_CONFIRMATION_MSG_SINGLE_APP, "Are you sure you want to cancel?") + "');return false;");
                btn.Text = GetResourceString(ResourceConstants.WP_CUST_TESAPP_BTNCANCEL, "Cancel");
            }

            btn = (Button)wcMITApplication.FindControl("FinishNavigationTemplateContainerID").FindControl("btnPrevious");
            if (btn != null) btn.Text = GetResourceString(ResourceConstants.WP_CUST_TESAPP_BTNPREVIOUS, "<< Previous");

            btn = (Button)wcMITApplication.FindControl("FinishNavigationTemplateContainerID").FindControl("btnTemporarySave");
           

            btn = (Button)wcMITApplication.FindControl("FinishNavigationTemplateContainerID").FindControl("btnSubmit");
            if (btn != null)
            {
                btn.Text = GetResourceString(ResourceConstants.WP_CUST_TESAPP_BTNSUBMIT, "Submit");
            }
        }
        catch (Exception ex)
        {
            Log.WriteError("MitigationsApplication.aspx::LocalizeCaptions::Error - {0}", ex.Message);
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        try
        {
            ScriptManager.RegisterClientScriptBlock(this, typeof(Page), "Register control", "var hdnBtnClickCount='" + hdnBtnClickCount.ClientID + "';var FinalStepTitle='" + FinalStepTitle + "';", true);

            SetEditModeClientSideVar();

            if (!IsPostBack)
            {
 
                RenewOnPointSession();

                hplHome.Text = GetResourceString(ResourceConstants.BREADCRUMB_MIT_HOME, "MIT Home");
                hplHome.NavigateUrl = Page.ResolveUrl("~/WebPages/Consultant/MITLists.aspx");
                LocalizeCaptions();

                if (CoreFramework.Utils.Utils.GetString(TESFramework.Utils.Utils.DecodeAndParseQueryStringValue(WebConstants.EditMIT)) == "true" || requestedForModification)
                {
                    Button btn1 = (Button)wcMITApplication.FindControl("StartNavigationTemplateContainerID").FindControl("btnTemporarySave");
                    Button btn2 = (Button)wcMITApplication.FindControl("FinishNavigationTemplateContainerID").FindControl("btnTemporarySave");
                    Button btn3 = (Button)wcMITApplication.FindControl("StepNavigationTemplateContainerID").FindControl("btnTemporarySave");
                    btn1.Visible = false;
                    btn2.Visible = false;
                    btn3.Visible = false;
                }



                if (AppSession != null && AppSession.MITApplication != null)
                {
                    lnkNewMIT.InnerHtml = GetResourceString(ResourceConstants.BREADCRUMB_LBL_APPLY_NEW_MIT, "Apply New Mitigation Application");
                    ValidationStatus[] listValidateStatus = new ValidationStatus[wcMITApplication.WizardSteps.Count];
                    for (int i = 0; i < wcMITApplication.WizardSteps.Count; i++)
                    {
                        if (i == 0)
                            listValidateStatus[i] = ValidationStatus.ACTIVE;
                        else
                            listValidateStatus[i] = ValidationStatus.INACTIVE;
                    }
                    ViewState[VALIDATE_STATUS_KEY] = listValidateStatus;
                    ViewState[ERROR_MESSAGE] = new string[wcMITApplication.WizardSteps.Count];
                }
                
            }
            else
            {
                if (AppSession != null && AppSession.MITApplication == null)
                {
                    string errorMsg = GetResourceString(ResourceConstants.UC_CUS_MITAPP_ERRORMSG_REAPPLY,
                                    "Error while submitting the MIT Application. You will have to apply for this MIT Application again.");
                    Log.WriteError(string.Format("MitigationsApplication.aspx::OnLoad::Error:: MITApplication is null - {0}", errorMsg));

                    btnCloseErrorMsg.Attributes.Add("onclick", "window.location.href='" + GetBaseURL() + "/WebPages/Consultant/MITLists.aspx'");

                    ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "showerrormsg", "ShowDialogErrorMsg('dialogErrorMessage', '" + errorMsg + "');", true);
                }
                string eventArgs = Request.Form["__EVENTARGUMENT"];
                if (!string.IsNullOrEmpty(eventArgs) && eventArgs.Contains("UpdatePanel"))
                {
                    HandlePostBack();
                }
            }
        }
        catch (Exception ex)
        {
            Log.WriteError(string.Format("MitigationsApplication.aspx::OnLoad::Error - {0}", ex.Message));
        }
    }

    private void SetEditModeClientSideVar()
    {
        if (CoreFramework.Utils.Utils.GetString(TESFramework.Utils.Utils.DecodeAndParseQueryStringValue(WebConstants.EditMIT)) == "true")
            ScriptManager.RegisterClientScriptBlock(this, typeof(Page), "AppEditMode", "var appEditMode=true; ", true);
        else
            ScriptManager.RegisterClientScriptBlock(this, typeof(Page), "AppEditMode", "var appEditMode=false; ", true);
    }

    protected override void OnLoadComplete(EventArgs e)
    {
        try
        {
            if (!IsPostBack)
            {
                if (AppSession != null && AppSession.MITApplication != null && ViewState[VALIDATE_STATUS_KEY] != null && m_LoadMITApplication)
                {
                    ValidationStatus[] listValidateStatus = (ValidationStatus[])ViewState[VALIDATE_STATUS_KEY];
                    int stepCnt = 0;
                    foreach (WizardStep step in wcMITApplication.WizardSteps)
                    {
                        foreach (Control control in step.Controls)
                        {
                            if (control is IMITWizardControl && !(control is ucConfirmation))
                            {
                                IMITWizardControl wizardControl = control as IMITWizardControl;
                                wizardControl.LoadControls(AppSession.MITApplication);

                                if (requestedForModification)
                                {
                                    wizardControl.DisableControls();
                                }

                                //if (CoreFramework.Utils.Utils.GetString(TESFramework.Utils.Utils.DecodeAndParseQueryStringValue(WebConstants.EditMIT)) == "true" && AppSession.MITApplication.MITStatus.ID == CoreFramework.Utils.Utils.GetInt32(MITStatusEnum.UpdateRequestedByLiaisonEngineer) && step.ID == "Step_2")
                                //{
                                //    wizardControl.DisableControls();
                                //}
                                string errorMessage = string.Empty;
                                listValidateStatus[stepCnt] = wizardControl.Validate(AppSession.MITApplication, out errorMessage);
                                stepCnt++;
                            }
                        }
                    }
                    ViewState[VALIDATE_STATUS_KEY] = listValidateStatus;
                    m_LoadMITApplication = false;
                }
            }
        }
        catch (Exception ex)
        {
            Log.WriteError("MitigationsApplication.aspx::OnLoadComplete::Error - {0}", ex.Message);
        }
    }

    private void LoadMITSectionsInWizard()
    {
        if (AppConfiguration != null && AppSession != null)
        {
            ContentPlaceHolder contentPlaceHolder = Page.Master.FindControl("cpTES") as ContentPlaceHolder;
            if (contentPlaceHolder != null)
            {
                Wizard wizardControl = contentPlaceHolder.FindControl("wcMITApplication") as Wizard;

                if (wizardControl != null)
                {
                    if (wizardControl.WizardSteps != null && wizardControl.WizardSteps.Count > 0)
                        wizardControl.WizardSteps.Clear();

                    List<WizardStep> steps = new List<WizardStep>();
                    //List<TISSection> rw = base.AppConfiguration.TISSections.Where(myRow => myRow.App_Type_Id == CoreFramework.Utils.Utils.GetInt32(ApplicationTypeEnum.IntroductoryTISApplication)).ToList();


                    List<TISSection> rw = base.AppConfiguration.TISSections.Where(myRow => myRow.ID == CoreFramework.Utils.Utils.GetInt32(MitigationApplicationENUM.Mitigation_Details) || myRow.ID == CoreFramework.Utils.Utils.GetInt32(MitigationApplicationENUM.Mitigation_WorkLocation) || myRow.ID == CoreFramework.Utils.Utils.GetInt32(MitigationApplicationENUM.Mitigation_Documents)).ToList();
                    foreach (TISSection mitWizSection in rw)
                    {                        
                        WizardStep ws = new WizardStep();
                        ws.StepType = WizardStepType.Step;
                        ws.ID = "Step_" + mitWizSection.ID.ToString();

                        switch ((MitigationApplicationENUM)Enum.Parse(typeof(MitigationApplicationENUM), mitWizSection.ID.ToString()))
                        {

                            case MitigationApplicationENUM.Mitigation_Details:
                                ws.Title = GetResourceString(mitWizSection.Name.ToString(), "Mitigation Details");
                                ws.Controls.Add(Page.LoadControl("~/UserControls/Consultant/MITApplication/MitigationDetails/ucMitigationDetails.ascx"));                                break;

                            case MitigationApplicationENUM.Mitigation_WorkLocation:
                                ws.Title = GetResourceString(mitWizSection.Name.ToString(), "Mitigation WorkLocation");
                                ucMitigationLocation workLoc = (ucMitigationLocation)Page.LoadControl("~/UserControls/Consultant/MITApplication/MitigationLocation/ucMitigationLocation.ascx");                                ws.Controls.Add(workLoc);                                break;


                            case MitigationApplicationENUM.Mitigation_Documents:
                                ws.Title = GetResourceString(mitWizSection.Name.ToString(), "Mitigation Documents");
                                ws.Controls.Add(Page.LoadControl("~/UserControls/Consultant/MITApplication/UploadDocuments/ucMitigationDoc.ascx"));                                break;
                        }
                        wizardControl.WizardSteps.Add(ws);
                    }

                    if (wizardControl.WizardSteps != null && wizardControl.WizardSteps.Count > 0)
                    {
                        //if (requestedForModification)
                        //{
                        //    //Insert as second last step in the wizard control
                        //    WizardStep wsAdditional = new WizardStep();
                        //    wsAdditional.StepType = WizardStepType.Step;
                        //    wsAdditional.ID = "Step_";
                        //    wsAdditional.Title = "Confirmation";
                        //    wsAdditional.Controls.Add(Page.LoadControl("~/UserControls/Consultant/MITApplication/Confirmation/ucConfirmation.ascx"));
                        //    wizardControl.WizardSteps.Add(wsAdditional);
                        //}

                        //Set first step as start and last step as finsh.
                        wizardControl.WizardSteps[0].StepType = WizardStepType.Start;
                        wizardControl.WizardSteps[wizardControl.WizardSteps.Count - 1].StepType = WizardStepType.Finish;
                        if (CoreFramework.Utils.Utils.GetString(TESFramework.Utils.Utils.DecodeAndParseQueryStringValue(WebConstants.EditMIT)) == "true")
                        {
                        }
                        else
                        {
                            //if (!requestedForModification)
                            //{
                            //Add confirmation step.
                            WizardStep wsComplete = new WizardStep();
                            wsComplete.StepType = WizardStepType.Complete;
                            wsComplete.ID = "Step_Complete";
                            wsComplete.Title = GetResourceString(FinalStepTitle, "Confirmation");
                            wsComplete.AllowReturn = false;
                            wsComplete.Controls.Add(Page.LoadControl("~/UserControls/Consultant/MITApplication/Confirmation/ucMITConfirmation.ascx"));
                            wizardControl.WizardSteps.Add(wsComplete);                            
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Return css class for wizard side bar control
    /// </summary>
    /// <param name="wizardStep"></param>
    /// <returns></returns>
    public string GetClassForWizardStep(object wizardStep)
    {
        string cssClass = string.Empty;
        try
        {
            if (wizardStep != null && wizardStep is WizardStep && ViewState[VALIDATE_STATUS_KEY] != null)
            {
                WizardStep step = wizardStep as WizardStep;

                ValidationStatus[] listValidateStatus = (ValidationStatus[])ViewState[VALIDATE_STATUS_KEY];
                if (listValidateStatus != null)
                {
                    switch (listValidateStatus[wcMITApplication.WizardSteps.IndexOf(step)])
                    {
                        case ValidationStatus.COMPLETE:
                            cssClass = "complete";
                            break;
                        case ValidationStatus.ACTIVE:
                            cssClass = "active";
                            break;
                        case ValidationStatus.INCOMPLETE:
                            cssClass = "incomplete";
                            break;
                        case ValidationStatus.ERRORS:
                            cssClass = "error";
                            break;
                        default:
                            cssClass = string.Empty;
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.WriteError(string.Format("MitigationsApplication.aspx::GetClassForWizardStep::Error - {0}", ex.Message));
        }
        return cssClass;
    }

    private void FillObjectAndSetValidationStatus()
    {
        if (AppSession != null && AppSession.MITApplication != null && ViewState[VALIDATE_STATUS_KEY] != null)
        {
            ValidationStatus[] listValidateStatus = (ValidationStatus[])ViewState[VALIDATE_STATUS_KEY];
            string[] finalErrorMessage = (string[])ViewState[ERROR_MESSAGE];

            WizardStepBase view = wcMITApplication.ActiveStep;
            foreach (Control control in view.Controls)
            {
                if (control is IMITWizardControl)
                {
                    IMITWizardControl wizardControl = control as IMITWizardControl;
                    string errorMessage = string.Empty;
                    AppSession.MITApplication = wizardControl.FillObject(AppSession.MITApplication);
                    listValidateStatus[wcMITApplication.ActiveStepIndex] = wizardControl.Validate(AppSession.MITApplication, out errorMessage);
                    finalErrorMessage[wcMITApplication.ActiveStepIndex] = errorMessage;

                    if (listValidateStatus[3] != ValidationStatus.INACTIVE && listValidateStatus[3] != ValidationStatus.INCOMPLETE && listValidateStatus[3] != ValidationStatus.ERRORS && wcMITApplication.ActiveStepIndex == 1 && wcMITApplication.WizardSteps[3].Controls[0] is IMITWizardControl)
                    {
                        IMITWizardControl engControl = wcMITApplication.WizardSteps[3].Controls[0] as IMITWizardControl;
                        string errMsg = string.Empty;
                        AppSession.MITApplication = engControl.FillObject(AppSession.MITApplication);
                        listValidateStatus[3] = engControl.Validate(AppSession.MITApplication, out errMsg);
                        finalErrorMessage[3] = errMsg;
                    }
                }
            }

            ViewState[VALIDATE_STATUS_KEY] = listValidateStatus;
            ViewState[ERROR_MESSAGE] = finalErrorMessage;

        }
    }

    private void TemporarySave()
    {
        if (AppSession != null && AppSession.MITApplication != null)
        {
            WizardStepBase view = wcMITApplication.ActiveStep;

            int activityTrackerID = AppSession.ActivityTracker.StartLog();

            foreach (Control control in view.Controls)
            {
                if (control is IMITWizardControl)
                {
                    IMITWizardControl wizardControl = control as IMITWizardControl;

                    AppSession.MITApplication = wizardControl.FillObject(AppSession.MITApplication);
                    //if (AppSession.MITApplication.SupportingDocuments != null && AppSession.MITApplication.SupportingDocuments.Count > 0)
                    //{
                    //    AppSession.MITApplication.SupportingDocuments.ForEach(doc => doc.IsNewDocument = false);

                    //}
                }
            }

            if (AppSession.MITApplication != null)
            {

                string resp = AppSession.MITApplication.TempSaveMITApplication(AppSession, AppSession.MITApplication.MITAppNumber, AppConfiguration);
                AppSession.MITApplication.MITAppNumber = String.IsNullOrEmpty(resp) ? AppSession.MITApplication.MITAppNumber : resp;
                //Update temp MITRefrenceNumber in ArcGIS data
                bool updateOnpoint = false;
                if (AppSession.MITApplication.WorkLocationDetails != null && AppSession.MITApplication.WorkLocationDetails.WorkAreaDetails != null)
                {
                    Dictionary<string, string> parameters = new Dictionary<string, string>();
                    parameters.Add(GISIntegration.EmbeddableKeys.APPLICATION_ID, AppSession.MITApplication.MITAppNumber);
                    parameters.Add(GISIntegration.EmbeddableKeys.WorkArea_ID, AppSession.MITApplication.WorkLocationDetails.WorkAreaID.ToString());

                    updateOnpoint = GISIntegration.UpdateApplicationStatus(EmbeddableUrlType.UpdateTesID, parameters, StudyTypeEnum.TIS);
                }
                if (!String.IsNullOrEmpty(resp))
                {
                    string postBackUrlExt = Page.ResolveUrl("~/WebPages/Consultant/MITLists.aspx");
                    TISType TISType = new TISType();
                    TISType.TISState = TISState.TempTIS;
                    Session[TISType.TIS_STATE_KEY] = TISType;
                    Session.Remove(OLD_MIT_APPLICATION);
                    Session.Remove(UploadedDocuments.FILES_MARKED_FOR_ARCHIVING);
                    ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "script", "jAlert('" + GetResourceString(ResourceConstants.TEMP_SAVE_SUCCESS_MSG, "Saved Successfully!") + "',window._AlertWindowTitle, window._OKText, 'info');", true);
                }
                else
                {
                    ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "script", "jAlert('" + GetResourceString(ResourceConstants.TEMP_SAVE_ERROR_MSG, "Application could not be saved!") + "',window._AlertWindowTitle, window._OKText, 'info');", true);
                }

                AppSession.ActivityTracker.EndLog(activityTrackerID, ActivityLogConstants.TEMP_SAVE_TIS_APPLICATION,
                                        string.Format("TEMP SAVE TIS APPLICATION: Temp save number {0}, User ID: {1}, User Name: {2}",
                                        resp, AppSession.UserInfo.UserID, AppSession.UserInfo.Name));
            }
        }
    }

    protected void wcMITApplication_NextButtonClick(object sender, WizardNavigationEventArgs e)
    {
        try
        {
            FillObjectAndSetValidationStatus();
            if (Session["ShowPopup"] != null && CoreFramework.Utils.Utils.GetBool(Session["ShowPopup"]))
            {
                e.Cancel = true;
                Session["ShowPopup"] = false;
            }

        }
        catch (Exception ex)
        {
            Log.WriteError(string.Format("MitigationsApplication.aspx::Next::Error - {0}", ex.Message));
        }
    }

    protected void wcMITApplication_TempSaveButtonClick(object sender, EventArgs e)
    {
        try
        {
            TemporarySave();
        }
        catch (Exception ex)
        {
            Log.WriteError(string.Format("MitigationsApplication.aspx::TeporarySave::Error - {0}", ex.Message));
        }
    }

    protected void wcMITApplication_PreviousButtonClick(object sender, WizardNavigationEventArgs e)
    {
        try
        {
            FillObjectAndSetValidationStatus();

            wcMITApplication.ActiveStepIndex = e.CurrentStepIndex - 1;
        }
        catch (Exception ex)
        {
            Log.WriteError(string.Format("MitigationsApplication.aspx::Previous::Error - {0}", ex.Message));
        }
    }

    protected void wcMITApplication_ActiveStepChanged(object sender, EventArgs e)
    {
        try
        {
            ValidationStatus[] listValidateStatus = (ValidationStatus[])ViewState[VALIDATE_STATUS_KEY];
            listValidateStatus[wcMITApplication.ActiveStepIndex] = ValidationStatus.ACTIVE;
        }
        catch (Exception ex)
        {
            Log.WriteError(string.Format("MitigationsApplication.aspx::ActiveStepChanged::Error - {0}", ex.Message));
        }
    }

    protected void wcMITApplication_FinishButtonClick(object sender, WizardNavigationEventArgs e)
    {
        try
        {
            string errorMsg = string.Empty;
            if (AppSession != null && AppSession.MITApplication != null && ViewState[VALIDATE_STATUS_KEY] != null)
            {
                MitigationApplication objOldMITApplication = new MitigationApplication();
                objOldMITApplication = Session[OLD_MIT_APPLICATION] as MitigationApplication;
                FillObjectAndSetValidationStatus();

                bool completeFlag = true;

                //Check for incomplete wizard steps and error messsages
                ValidationStatus[] listValidateStatus = (ValidationStatus[])ViewState[VALIDATE_STATUS_KEY];
                int count = listValidateStatus.Length - 1;
                if (CoreFramework.Utils.Utils.GetString(TESFramework.Utils.Utils.DecodeAndParseQueryStringValue(WebConstants.EditMIT)) == "true")
                {
                    count = listValidateStatus.Length;
                }
                string[] errorMessages = (string[])ViewState[ERROR_MESSAGE];
                for (int viewCount = 0; viewCount < count; viewCount++)
                {

                    if (listValidateStatus[viewCount] == ValidationStatus.COMPLETE)
                    {
                        completeFlag = completeFlag & true;
                        if (!string.IsNullOrEmpty(errorMessages[viewCount]))
                        {
                            errorMsg += wcMITApplication.WizardSteps[viewCount].Name + errorMessages[viewCount] + " | ";
                        }
                    }
                    else if (listValidateStatus[viewCount] == ValidationStatus.ERRORS)
                    {
                        listValidateStatus[viewCount] = ValidationStatus.ERRORS;
                        errorMsg += wcMITApplication.WizardSteps[viewCount].Name + errorMessages[viewCount] + " | ";
                        completeFlag = completeFlag & false;
                    }
                    else
                    {
                        listValidateStatus[viewCount] = ValidationStatus.INCOMPLETE;
                        errorMsg += wcMITApplication.WizardSteps[viewCount].Name + errorMessages[viewCount] + " | ";
                        completeFlag = completeFlag & false;
                    }
                }

                if (completeFlag && string.IsNullOrEmpty(errorMsg))
                {
                    AppSession.MITApplication.Applicant = AppSession.UserInfo;

                    bool response = false;
                    bool gisresponse = false;

                    if (CoreFramework.Utils.Utils.GetString(TESFramework.Utils.Utils.DecodeAndParseQueryStringValue(WebConstants.EditMIT)) == "true")
                    {
                        AppSession.MITApplication.FilesToArchive = Session[UploadedDocuments.FILES_MARKED_FOR_ARCHIVING] as List<UploadedDocuments>;

                        if (AppSession.MITApplication.IsAllMandatoryDocsNotUploaded && hdnBtnClickCount.Value == "")
                        {
                            listValidateStatus[wcMITApplication.ActiveStepIndex] = ValidationStatus.ACTIVE;
                            ScriptManager.RegisterClientScriptBlock(Page, typeof(Page), "Script", "ConfirmFinalApplicationSubmit('" + GetResourceString(ResourceConstants.MANDATORY_DOCUMENTS_MESSAGE, "Mandatory documents are not uploaded. Do you still want to continue?") + "');", true);

                            e.Cancel = true;
                        }
                        else if (hdnBtnClickCount.Value == "1" || !AppSession.MITApplication.IsAllMandatoryDocsNotUploaded)
                        {
                            if (response)
                            {
                                if (AppSession.MITApplication.WorkLocationDetails != null && AppSession.MITApplication.WorkLocationDetails.WorkAreaDetails != null)
                                {
                                    Dictionary<string, string> parameters = new Dictionary<string, string>();

                                    parameters.Add(GISIntegration.EmbeddableKeys.APPLICATION_ID, AppSession.MITApplication.MITAppNumber);
                                    parameters.Add(GISIntegration.EmbeddableKeys.WorkArea_ID, AppSession.MITApplication.WorkLocationDetails.WorkAreaDetails.WorkAreaID.ToString());

                                    response = GISIntegration.UpdateApplicationStatus(EmbeddableUrlType.UpdateTesID, parameters, StudyTypeEnum.TIS);
                                }
                                if (response)
                                    response = AppSession.MITApplication.ModifyMITApplication(AppSession, base.AppConfiguration);

                                if (response)
                                {
                                    ScriptManager.RegisterClientScriptBlock(this.Page, typeof(Page), "scriptalert", "jAlert('" + GetResourceString(ResourceConstants.UC_LABEL_APPLICATION_UPDATED, "Application has been updated successfully.") + "',window._AlertWindowTitle, window._OKText, 'info');$('#popup_ok').click(function () {window.open('" + postBackUrlInt + "','_self');});", true);
                                    foreach (Control control in wcMITApplication.WizardSteps[e.NextStepIndex].Controls)
                                    {
                                        if (control is IMITWizardControl)
                                        {
                                            IMITWizardControl IWizardControl = control as IMITWizardControl;
                                            IWizardControl.LoadControls(AppSession.MITApplication);
                                        }
                                    }
                                }
                                if (!response)
                                {
                                    errorMsg = GetResourceString(ResourceConstants.UC_CUS_MITAPP_ERRORMSG, "Error while submitting the Mitigation Application. Try again.");
                                    Log.WriteError(string.Format("MitigationsApplication.aspx::Finish::Error - {0}", errorMsg));
                                }
                            }
                            else
                            {
                                errorMsg = GetResourceString(ResourceConstants.UC_CUS_IMPORT_MASTER_PLOT_PLANT_ERRMSG, "Error while uploading  Master Plot Plan Landuse file into appropriate database tables.");
                                Log.WriteError(string.Format("MITApplication::Finish::Error - {0}", errorMsg));
                            }
                        }
                        else
                        {
                            errorMsg = GetResourceString(ResourceConstants.UC_CUS_MITAPP_ERRORMSG, "Error while submitting the TIS Application. Try again.");
                            Log.WriteError(string.Format("MitigationsApplication.aspx::Finish::Error - {0}", errorMsg));
                        }
                    }

                    else if (requestedForModification)
                    {

                        string errMsg = string.Empty;



                        Session.Remove(MitigationApplication.IsModify);

                        Session.Remove(OLD_MIT_APPLICATION);

                        if (string.IsNullOrEmpty(errMsg))

                            errorMsg = GetResourceString(ResourceConstants.UC_CUS_MITAPP_ERRORMSG, "Error while submitting modification of MIT Application. Try again.");

                        else

                            errorMsg = errMsg;

                        Log.WriteError(string.Format("MitigationsApplication.aspx::Finish::Error - {0}", errorMsg));
                    }
                    else
                    {

                        //Commented as message for mandatory docs is not required by RTA

                        //if (AppSession.MITApplication.IsAllMandatoryDocsNotUploaded && hdnBtnClickCount.Value == "")

                        //{

                        //    listValidateStatus[wcMITApplication.ActiveStepIndex] = ValidationStatus.ACTIVE;

                        //    ScriptManager.RegisterClientScriptBlock(Page, typeof(Page), "Script", "ConfirmFinalApplicationSubmit('" + GetResourceString(ResourceConstants.MANDATORY_DOCUMENTS_MESSAGE, "Mandatory documents are not uploaded. Do you still want to continue?") + "');", true);



                        //    e.Cancel = true;

                        //}

                        //else if (hdnBtnClickCount.Value == "1" || !AppSession.MITApplication.IsAllMandatoryDocsNotUploaded)

                        // {

                        lock (NewMITAppNumberLock)
                        {

                            Log.WriteDebug(string.Format("MITApplication::Lock Object::Hash code - {0}, logged in user - {1}, userid - {2},{3}", NewMITAppNumberLock.GetHashCode(), AppSession.UserInfo.Name, AppSession.UserInfo.UserID, AppSession.MITApplication.Applicant.Name));
                                                        
                            string newMITRefrenceNumber = string.Empty; 

                            ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "ShowLoading", "ShowLoading();", true);

                            Log.WriteDebug("MITApplication:: Submit function call");

                            response = AppSession.MITApplication.SubmitMITApplication(AppSession, base.AppConfiguration, AppSession.MITApplication.MITAppNumber, out newMITRefrenceNumber);


                            Log.WriteDebug(string.Format("MITApplication::Application Submitted Response - {0}", response));

                            if (response)
                            {

                                if (AppSession.MITApplication.WorkLocationDetails != null && AppSession.MITApplication.WorkLocationDetails.WorkAreaDetails != null && !string.IsNullOrEmpty(newMITRefrenceNumber))
                                {

                                    Dictionary<string, string> parameters = new Dictionary<string, string>();

                                    parameters.Add(GISIntegration.EmbeddableKeys.APPLICATION_ID, newMITRefrenceNumber);

                                    parameters.Add(GISIntegration.EmbeddableKeys.WorkArea_ID, AppSession.MITApplication.WorkLocationDetails.WorkAreaID.ToString());



                                    gisresponse = GISIntegration.UpdateApplicationStatus(EmbeddableUrlType.UpdateTesID, parameters, StudyTypeEnum.TIS);

                                    
                                    Log.WriteDebug(string.Format("MITApplication::Lock Object::Ref Number - {0}, logged in user - {1}, Update GIS Application response - {2}", newMITRefrenceNumber, AppSession.UserInfo.Name, response));

                                }

                            }

                            else
                            {

                                errorMsg = GetResourceString(ResourceConstants.UC_CUS_MITAPP_ERRORMSG, "Error while submitting the MIT Application. Try again.");

                                Log.WriteError(string.Format("MitigationsApplication.aspx::Finish::Error - {0}", errorMsg));

                            }

                            Log.WriteDebug(string.Format("MITApplication::Lock Object::Ref Number - {0}, logged in user - {1}, Submit TIS Application response - {2}", newMITRefrenceNumber, AppSession.UserInfo.Name, response));

                            
                        }

                        Log.WriteDebug(string.Format("MITApplication::Application Out of the Lock, logged in user - {0}", AppSession.UserInfo.Name));



                        if (response)
                        {

                            //Update MITRefrenceNumber in ArcGIS data

                            bool UpdateTesAppIdStatusId = false;

                            if (AppSession.MITApplication.MITAppNumber != null && AppSession.MITApplication.MITStatus != null)
                            {

                                Dictionary<string, string> parameters = new Dictionary<string, string>();

                                parameters.Add(GISIntegration.EmbeddableKeys.APPLICATION_ID, AppSession.MITApplication.MITAppNumber);

                                //parameters.Add(GISIntegration.EmbeddableKeys.WorkArea_ID, AppSession.MITApplication.WorkLocationDetails.WorkAreaID.ToString());

                                parameters.Add(GISIntegration.EmbeddableKeys.Status_ID, CoreFramework.Utils.Utils.GetInt32(GISStatusUpdate.New).ToString());



                                UpdateTesAppIdStatusId = GISIntegration.UpdateTesAppIdStatusId(EmbeddableUrlType.UpdateTesAppIdStatusId, parameters, StudyTypeEnum.TIS); ;

                                Log.WriteDebug(string.Format("MITApplication::Update GIS with status new response - {0}", response));

                            }

                            foreach (Control control in wcMITApplication.WizardSteps[e.NextStepIndex].Controls)
                            {

                                if (control is IMITWizardControl)
                                {

                                    IMITWizardControl IWizardControl = control as IMITWizardControl;

                                    IWizardControl.LoadControls(AppSession.MITApplication);

                                }



                            }



                            Log.WriteDebug("MITApplication::To send Email - Start a new Thread");

                            Thread th = new Thread(() => sendEmail(AppSession.MITApplication.MITAppNumber, AppConfiguration.EmailWSURL, AppSession.Log));

                            th.Start();



                        }

                        //if (!response)

                        //{

                        //    errorMsg = GetResourceString(ResourceConstants.UC_CUS_MITAPP_ERRORMSG, "Error while submitting the TIS Application. Try again.");

                        //    Log.WriteError(string.Format("MitigationsApplication.aspx::Finish::Error - {0}", errorMsg));

                        //}

                        // }

                        // }



                        //}

                    }

                    if (response)
                    {
                        // CLEAN ALL SESSION VARIABLES USED IN SUBMISSION
                        Session.Remove(UploadedDocuments.SUPPORTING_DOCUMENTS_KEY);
                        Session.Remove(UploadedDocuments.Uploaded_Files_KEY);
                        Session.Remove(OLD_MIT_APPLICATION);
                        Session.Remove(UploadedDocuments.FILES_MARKED_FOR_ARCHIVING);
                    }
                }
                else
                {
                    errorMsg = string.Format(GetResourceString(ResourceConstants.UC_CUS_TESAPP_ERRORMSG_INCOMPLETE,
                                                "You have incomplete information in {0}. Please complete all mandatory information before submitting."),
                                errorMsg.TrimEnd().TrimEnd('|').TrimEnd());

                    Log.WriteError(string.Format("MitigationsApplication.aspx::Finish::Error - {0}", errorMsg));
                }

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    listValidateStatus[wcMITApplication.ActiveStepIndex] = ValidationStatus.ACTIVE;
                    //ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "showerrormsg", "ShowDialogErrorMsg('dialogErrorMessage', '" + errorMsg + "');", true);
                    ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "script", "jAlert('" + errorMsg + "',window._AlertWindowTitle, window._OKText, 'info');", true);

                    e.Cancel = true;

                    //if (CoreFramework.Utils.Utils.GetString(TESFramework.Utils.Utils.DecodeAndParseQueryStringValue(WebConstants.EditMIT]) == "true")
                    //    Response.Redirect("~/WebPages/Consultant/MITLists.aspx", true);
                }

                ViewState[VALIDATE_STATUS_KEY] = listValidateStatus;
            }
            else
            {
                e.Cancel = true;
            }

            ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "HideLoading", "HideLoading();", true);
        }
        catch (Exception ex)
        {
            e.Cancel = true;
            Log.WriteError(string.Format("MitigationsApplication.aspx::Finish::Error - {0}", ex.Message));
        }

    }


    protected void wcMITApplication_SideBarButtonClick(object sender, WizardNavigationEventArgs e)
    {
        try
        {
            FillObjectAndSetValidationStatus();
            wcMITApplication.ActiveStepIndex = e.NextStepIndex;
        }
        catch (Exception ex)
        {
            Log.WriteError(string.Format("MitigationsApplication.aspx::SideBarButtonClick::Error - {0}", ex.Message));
        }
    }


    private UploadedDocuments CreateMITApplicationReceipt()
    {
        UploadedDocuments eReceipt = null;

        ManualResetEvent resetEvent = new ManualResetEvent(false);
        ThreadStart threadStarter = delegate
        {
            eReceipt = MitigationApplication.CreateMITApplicationReceipt(AppSession, AppConfiguration, DocTypeEnum.ApplicationReceipt, null);
            if (resetEvent != null) resetEvent.Set();
        };

        Thread thread = new Thread(threadStarter);
        thread.Start();

        bool eReceiptGenerated = false;

        //Wait for eReceipt Generation execution to finish or time out
        NameValueCollection appSettings = HttpContext.Current.GetSection("appSettings") as NameValueCollection;
        if (appSettings["DBCOMMAND_TIMEOUT"] != null)
        {
            if (WaitHandle.WaitAll(new WaitHandle[] { resetEvent }, CoreFramework.Utils.Utils.GetInt32(appSettings["DBCOMMAND_TIMEOUT"].ToString(), 30) * 1000, false))
            {
                eReceiptGenerated = true;
                if (Log.Debug)
                {
                    Log.WriteDebug("eReceipt Generation request thread finished before time out. Time out value is {0} seconds", CoreFramework.Utils.Utils.GetInt32(appSettings["DBCOMMAND_TIMEOUT"].ToString(), 30));
                }
            }
        }
        if (!eReceiptGenerated)
        {
            thread.Abort();

            eReceipt = null;

            if (Log.Debug)
            {
                Log.WriteDebug("eReceipt Generation has timed out. Time out value is {0} seconds", CoreFramework.Utils.Utils.GetInt32(appSettings["DBCOMMAND_TIMEOUT"].ToString(), 30));
            }
        }
        resetEvent.Close();
        resetEvent = null;

        return eReceipt;
    }

    private void HandlePostBack()
    {
        string eventArgs = Request.Form["__EVENTARGUMENT"];
        if (!string.IsNullOrEmpty(eventArgs))
        {
            if (eventArgs.Contains("UpdatePanel"))
            {
                //UpdatePanel|Feedback
                List<string> lstEventArgs = eventArgs.Replace("UpdatePanel|", "").Split('|').ToList<string>();
                List<UpdatePanelCollection> lstUpdatePanels = lstEventArgs.Select(p =>
                        (UpdatePanelCollection)Enum.Parse(typeof(UpdatePanelCollection), p)).ToList<UpdatePanelCollection>();
                RefreshUpdatePanel(lstUpdatePanels);
            }
        }
    }
    public override void RefreshUpdatePanel(List<UpdatePanelCollection> updatePanels)
    {
        try
        {
            foreach (UpdatePanelCollection updatePanel in updatePanels)
            {
                switch (updatePanel)
                {
                    case UpdatePanelCollection.WorkArea:
                        ucWorkArea workLoc = new ucWorkArea();
                        foreach (WizardStep tisWizSection in wcMITApplication.WizardSteps)
                        {
                            foreach (Control control in tisWizSection.Controls)
                            {
                                if (control is IMITWizardControl && (control is ucWorkArea))
                                {
                                    IMITWizardControl wizardControl = control as IMITWizardControl;
                                    if (AppSession != null && AppSession.MITApplication == null)
                                    {
                                        AppSession.MITApplication = new MitigationApplication();
                                    }
                                    AppSession.MITApplication = wizardControl.FillObject(AppSession.MITApplication);
                                }
                            }
                        }
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Log.WriteError("MITDetails::RefreshUpdatePanel::Error - {0}", ex.Message);
        }
    }


    private void sendEmail(string Ref_Num, string emailWsURL, LoggingFramework.ILog log)
    {
        Log.WriteDebug(string.Format("MITApplication::sendEmail - start in new thread"));
        Thread.Sleep(120000); //4 minsss
        Log.WriteDebug(string.Format("MITApplication::sendEmail - sleep finished "));
        Util.SendEmail(emailWsURL, Ref_Num, 1, string.Empty, log);
        Log.WriteDebug(string.Format("MITApplication::sendEmail - end in new thread"));
    }

    [System.Web.Script.Services.ScriptMethod()]
    [System.Web.Services.WebMethod]
    public static List<string> GetClientList(string prefixText, int count)
    {
        List<string> clientList = new List<string>();
        TESFramework.Session.AppSession appSession = null;
        try
        {
            //appSession = HttpContext.Current.Session[TESFramework.Session.AppSession.APPSESSION_KEY] as TESFramework.Session.AppSession;
            //if (appSession.Clients.Count > 0)
            //{
            //    var clients = from cl in appSession.Clients
            //                  where cl.Name.StartsWith(prefixText, StringComparison.OrdinalIgnoreCase)
            //                  select cl;
            //    foreach (var cl in clients)
            //    {
            //        string item = AjaxControlToolkit.AutoCompleteExtender.CreateAutoCompleteItem(cl.Name, cl.ID.ToString());
            //        clientList.Add(item);
            //    }
            //}
        }
        catch (Exception ex)
        {
            if (appSession != null) { appSession.Log.WriteError(string.Format("MitigationsApplication.aspx::GetClientList::Error - {0}", ex.Message)); }
        }
        return clientList;
    }

    private void LoadTESApplication()
    {
        if (Log != null) Log.WriteTrace("MitigationApplication::LoadTESApplication - Start");

        int? discussionType = null;

        if (AppSession != null && AppSession.UserInfo != null && AppSession.UserInfo.Roles != null)
        {
            if (AppSession.UserInfo.Roles.Select(r => r.ID).Any(ResourceAuthorization.AccessToCustomerPages.Contains) || AppSession.UserInfo.Roles.Select(r => r.ID).Any(ResourceAuthorization.Developer.Contains))
            {
                discussionType = CoreFramework.Utils.Utils.GetInt32(DiscussionTypeEnum.Consultant);
            }

            TESApplication tesApplication = new TESApplication();

            if (AppSession.UserInfo.Roles.Select(r => r.ID).Any(ResourceAuthorization.AccessToCustomerPages.Contains) || AppSession.UserInfo.Roles.Select(r => r.ID).Any(ResourceAuthorization.Developer.Contains))
                tesApplication = tesApplication.GetTESApplicationByReferenceNumber(TESFramework.Utils.Utils.DecodeAndParseQueryStringValue(WebConstants.TESReferenceNumber), discussionType, AppSession, AppConfiguration, true);
            else
                tesApplication = tesApplication.GetTESApplicationByReferenceNumber(TESFramework.Utils.Utils.DecodeAndParseQueryStringValue(WebConstants.TESReferenceNumber), discussionType, AppSession, AppConfiguration, false);
            
            AppSession.TESApplication = tesApplication;
        }

        if (Log != null) Log.WriteTrace("MitigationApplication::LoadTESApplication - End");
    }
    [WebMethod]
    public static string LoadExistingFiles()
    {
        return FileUpload.LoadExistingFiles(false);
    }

    [WebMethod]
    public static bool RemoveAllFiles()
    {
        TESFramework.Session.AppSession appSession = null;
        try
        {
            LinkedList<ViewDataUploadFilesResult> r = new LinkedList<ViewDataUploadFilesResult>();
            r = (LinkedList<ViewDataUploadFilesResult>)HttpContext.Current.Session[UploadedDocuments.Uploaded_Files_KEY];
            List<UploadedDocuments> documents = (List<UploadedDocuments>)HttpContext.Current.Session[UploadedDocuments.SUPPORTING_DOCUMENTS_KEY];
            List<UploadedDocuments> filesForArchiving = null;
            if (HttpContext.Current.Session[UploadedDocuments.FILES_MARKED_FOR_ARCHIVING] == null)
                filesForArchiving = new List<UploadedDocuments>();
            else
                filesForArchiving = HttpContext.Current.Session[UploadedDocuments.FILES_MARKED_FOR_ARCHIVING] as List<UploadedDocuments>;

            if (documents != null && documents.Count > 0)
            {
                if (documents.Where(a => a.RequestFrom == "SupportDoc") != null ||
                    documents.Where(a => a.RequestFrom == "MethodologyDoc") != null ||
                    documents.Where(a => a.RequestFrom == "ModellingAddDoc") != null ||
                    documents.Where(a => a.RequestFrom == "TISAppAddDoc") != null)
                {
                    filesForArchiving.AddRange(documents.Where(a => a.RequestFrom == "SupportDoc"));

                    documents.RemoveAll(a => a.RequestFrom == "SupportDoc");
                    documents.RemoveAll(a => a.RequestFrom == "MethodologyDoc");
                    documents.RemoveAll(a => a.RequestFrom == "ModellingAddDoc");
                    documents.RemoveAll(a => a.RequestFrom == "TISAppAddDoc");
                    HttpContext.Current.Session[UploadedDocuments.SUPPORTING_DOCUMENTS_KEY] = documents;
                    HttpContext.Current.Session[UploadedDocuments.FILES_MARKED_FOR_ARCHIVING] = filesForArchiving;
                }

                if (r != null &&
                    (r.Where(a => a._requestFrom == "SupportDoc") != null ||
                    r.Where(a => a._requestFrom == "MethodologyDoc") != null ||
                    r.Where(a => a._requestFrom == "ModellingAddDoc") != null ||
                    r.Where(a => a._requestFrom == "TISAppAddDoc") != null))
                {
                    List<ViewDataUploadFilesResult> filesToRemove = null;
                    if (r.Where(file => file._requestFrom == "SupportDoc") != null)
                        filesToRemove = r.Where(file => file._requestFrom == "SupportDoc").ToList();
                    else if (r.Where(file => file._requestFrom == "MethodologyDoc") != null)
                        filesToRemove = r.Where(file => file._requestFrom == "MethodologyDoc").ToList();
                    else if (r.Where(file => file._requestFrom == "ModellingAddDoc") != null)
                        filesToRemove = r.Where(file => file._requestFrom == "ModellingAddDoc").ToList();
                    else if (r.Where(file => file._requestFrom == "TISAppAddDoc") != null)
                        filesToRemove = r.Where(file => file._requestFrom == "TISAppAddDoc").ToList();
                    foreach (ViewDataUploadFilesResult fileToRemove in filesToRemove)
                    {
                        r.Remove(fileToRemove);
                    }
                    HttpContext.Current.Session[UploadedDocuments.Uploaded_Files_KEY] = r;
                }
            }

            appSession = HttpContext.Current.Session[TESFramework.Session.AppSession.APPSESSION_KEY] as TESFramework.Session.AppSession;
            appSession.MITApplication.MitigationDocuments = null;
        }
        catch (Exception ex)
        {
            if (appSession != null) { appSession.Log.WriteError(string.Format("MitigationsApplication.aspx::RemoveAllFiles::Error - {0}", ex.Message)); }
        }
        return true;
    }

    [System.Web.Services.WebMethod]
    public static bool ValidateDates(string startDate, string endDate)
    {
        DateTime fdate = DateTime.MinValue, tdate = DateTime.MinValue;
        TESFramework.AppConfiguration appConfig = null;
        TESFramework.Session.AppSession appSession = null;
        bool flg = false;
        try
        {
            appConfig = HttpContext.Current.Application[TESFramework.AppConfiguration.APPCONFIG_KEY] as TESFramework.AppConfiguration;
            appSession = HttpContext.Current.Session[TESFramework.Session.AppSession.APPSESSION_KEY] as TESFramework.Session.AppSession;
            fdate = TESFramework.Utils.Utils.ParseDate(startDate
                , BaseConfig<TESAppConfiguration>.GetListItem(appConfig.TESAppConfigurations, "Name", TESAppConfiguration.DATE_FORMAT, appSession.Log).Value
                , appSession.Log);
            tdate = TESFramework.Utils.Utils.ParseDate(endDate
                , BaseConfig<TESAppConfiguration>.GetListItem(appConfig.TESAppConfigurations, "Name", TESAppConfiguration.DATE_FORMAT, appSession.Log).Value
                , appSession.Log);
            if (tdate != DateTime.MinValue && fdate != DateTime.MinValue && !(fdate > tdate))
            {
                flg = true;
            }

        }
        catch (Exception ex)
        {
            if (appSession != null) { appSession.Log.WriteError(string.Format("MitigationsApplication.aspx::ValidateDates::Error - {0}", ex.Message)); }
        }
        return flg;
    }

    [System.Web.Services.WebMethod]
    public static bool IsValidDate(string sDate)
    {
        DateValidation dv = new DateValidation();
        DateTime formattedDate = DateTime.MinValue;
        TESFramework.AppConfiguration appConfig = null;
        TESFramework.Session.AppSession appSession = null;
        bool flg = false;
        try
        {
            appConfig = HttpContext.Current.Application[TESFramework.AppConfiguration.APPCONFIG_KEY] as TESFramework.AppConfiguration;
            appSession = HttpContext.Current.Session[TESFramework.Session.AppSession.APPSESSION_KEY] as TESFramework.Session.AppSession;
            formattedDate = TESFramework.Utils.Utils.ParseDate(sDate
                , BaseConfig<TESAppConfiguration>.GetListItem(appConfig.TESAppConfigurations, "Name", TESAppConfiguration.DATE_FORMAT, appSession.Log).Value
                , appSession.Log);
            if (formattedDate != DateTime.MinValue)
            {
                flg = true;
            }
        }
        catch (Exception ex)
        {
            if (appSession != null) { appSession.Log.WriteError(string.Format("MitigationsApplication.aspx::ValidateDates::Error - {0}", ex.Message)); }
        }
        return flg;
    }

    [System.Web.Services.WebMethod]
    public static void CancelMITApplication(string hdnWorkAreaID)
    {
        var thisPage = new MitigationsApplicationWizard();

        TESFramework.Session.AppSession appSession = HttpContext.Current.Session[TESFramework.Session.AppSession.APPSESSION_KEY] as TESFramework.Session.AppSession;
        appSession.MITApplication = null;
        if (HttpContext.Current.Session[UploadedDocuments.SUPPORTING_DOCUMENTS_KEY] != null)
        {
            List<UploadedDocuments> documents = (List<UploadedDocuments>)HttpContext.Current.Session[UploadedDocuments.SUPPORTING_DOCUMENTS_KEY];
            List<int> documentIDsToDelete = documents.Where(doc => doc.IsNewDocument).Select(doc => doc.UploadedDocumentID).ToList();
            //if (documentIDsToDelete != null && documentIDsToDelete.Count > 0)
            //    UploadedDocuments.DeleteUploadedDocument(appSession, documentIDsToDelete);
        }
    }

    [System.Web.Services.WebMethod]
    public static void RenewOnPointSession()
    {
        TESFramework.Session.AppSession appSession = HttpContext.Current.Session[TESFramework.Session.AppSession.APPSESSION_KEY] as TESFramework.Session.AppSession;
        TESFramework.AppConfiguration appConfig = HttpContext.Current.Application[TESFramework.AppConfiguration.APPCONFIG_KEY] as TESFramework.AppConfiguration;

        if (appConfig != null && appSession != null && appConfig.TESAppConfigurations != null)
        {
            //Util.GetResponse(appConfig.TESAppConfigurations.FirstOrDefault(a => a.Name == MITConfigFramework.TESAppConfiguration.ONPOINT_RENEW_SESSION_URL).Value, appSession.Log);
        }
    }
    //[System.Web.Services.WebMethod]
    //public static void emailSend()
    //{
    //    TESFramework.Session.AppSession appSession = HttpContext.Current.Session[TESFramework.Session.AppSession.APPSESSION_KEY] as TESFramework.Session.AppSession;
    //    TESFramework.AppConfiguration appConfig = HttpContext.Current.Application[TESFramework.AppConfiguration.APPCONFIG_KEY] as TESFramework.AppConfiguration;
    //    if (appConfig != null && appSession != null && appSession.MITApplication != null)
    //    {
    //        Util.SendEmail(appConfig.EmailWSURL, appSession.MITApplication.MITAppNumber, 1, string.Empty, appSession.Log);
    //    }
    //}

}






