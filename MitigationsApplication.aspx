<%@ Page Language="C#" AutoEventWireup="true" Inherits="MitigationsApplicationWizard" MasterPageFile="~/MasterPages/TES.master"

    CodeBehind="MitigationsApplication.aspx.cs" %>



<%@ Register Src="~/UserControls/Consultant/MITApplication/Confirmation/ucMITConfirmation.ascx"

    TagName="Confirmation" TagPrefix="Confirmation" %>


<%@ Register Src="~/UserControls/Consultant/MITApplication/MitigationDetails/ucMitigationDetails.ascx"

    TagName="MitigationDetails" TagPrefix="MitigationDetails" %>

<%@ Register Src="~/UserControls/Consultant/MITApplication/UploadDocuments/ucMitigationDoc.ascx"

    TagName="ucMITSuppDoc" TagPrefix="ucMITSuppDoc" %>

<%@ Register Src="~/UserControls/Consultant/MITApplication/MitigationLocation/ucMitigationLocation.ascx"

    TagName="MITLocation" TagPrefix="MITLocation" %>

<asp:Content ID="cpJavaScript" runat="server" ContentPlaceHolderID="cpJavascript">

</asp:Content>

<asp:Content ID="contentBreadCrumbs" ContentPlaceHolderID="cpBreadCrumbs" runat="server">

    <div class="floatleft" runat="server" id="dvNavigationBar">

        <asp:HyperLink runat="server" ID="hplHome" NavigateUrl="#" Visible="false"></asp:HyperLink>

        <span>&gt;&gt;</span> <a runat="server" id="lnkNewMIT" navigateurl="#" class="current">

        </a>

    </div>

</asp:Content>

<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="cpTES">

    <!-- Header start-->

    <h3 class="floatleft headingMain ">

        <asp:Label runat="server" ID="lblTESTypeHeading"></asp:Label>

    </h3>

    <!-- Header end-->

    <asp:UpdatePanel runat="server" ID="updNocSteps">

        <ContentTemplate>

            <div class="site-wrapper floatleft">

                <div class="floatleft tabView">

                    <script type="text/javascript">

                        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(applyStyle);

                        function applyStyle() {

                            applyHelpStyle();

                            if (Sys.Browser.agent == Sys.Browser.InternetExplorer && Sys.Browser.version < 9) {

                                applyButtonStyleForIElessThan9();

                            }

                        }                            



                    </script>

                    <asp:Wizard ID="wcMITApplication" runat="server" CancelButtonText="Cancel" DisplayCancelButton="true"

                        DisplaySideBar="true" SideBarStyle-CssClass="steps" SideBarStyle-VerticalAlign="Top"

                        OnActiveStepChanged="wcMITApplication_ActiveStepChanged" OnNextButtonClick="wcMITApplication_NextButtonClick"

                        OnPreviousButtonClick="wcMITApplication_PreviousButtonClick" OnFinishButtonClick="wcMITApplication_FinishButtonClick"

                        OnSideBarButtonClick="wcMITApplication_SideBarButtonClick">

                        <SideBarStyle CssClass="steps" VerticalAlign="Top" />

                        <SideBarTemplate>

                            <asp:DataList ID="SideBarList" runat="server">

                                <ItemTemplate>

                                    <ul>

                                        <li id="MITSection" class="<%# GetClassForWizardStep(Container.DataItem) %>"><span

                                            class="num">

                                            <%# wcMITApplication.WizardSteps.IndexOf(Container.DataItem as WizardStep) + 1 %></span>

                                            <span class="stpInfo">

                                                <asp:LinkButton ID="SideBarButton" runat="server" Style="text-decoration: none" Enabled="true"

                                                    CommandName="GoToStep" CausesValidation="false" Text="Button" OnClientClick="javascript:if(FinalStepTitle != undefined && this.text != FinalStepTitle){ ShowLoading();}" />

                                            </span></li>

                                    </ul>

                                </ItemTemplate>

                            </asp:DataList>

                        </SideBarTemplate>

                        <StartNavigationTemplate>

                            <div class="floatright">

                                <asp:Button ID="btnCancel" runat="server" Text="Cancel" CausesValidation="false"

                                    CommandName="Cancel" CssClass="buttonfrm" />

                                <asp:Button ID="btnNext" runat="server" Text="Next >>" CausesValidation="false" CommandName="MoveNext"

                                    CssClass="buttonfrm" OnClientClick="ShowLoading();" />

                            </div>

                            <div class="floatleft padleft20">

                                <asp:Button ID="btnTemporarySave" runat="server" Text="Temporary Save" CausesValidation="false"

                                    CommandName="TemporarySave" class="buttonfrm" OnClick="wcMITApplication_TempSaveButtonClick" OnClientClick="ShowLoading();" />

                            </div>

                        </StartNavigationTemplate>

                        <StepNavigationTemplate>

                            <div class="floatright">

                                <asp:Button ID="btnPrevious" runat="server" Text="<< Previous" CausesValidation="false"

                                    CommandName="MovePrevious" class="buttonfrm" OnClientClick="ShowLoading();" />

                                <asp:Button ID="btnCancel" runat="server" Text="Cancel" CausesValidation="false"

                                    CommandName="Cancel" class="buttonfrm" />

                                <asp:Button ID="btnNext" runat="server" Text="Next >>" CausesValidation="true" CommandName="MoveNext"

                                    class="buttonfrm" OnClientClick="ShowLoading();" />

                            </div>

                            <div class="floatleft padleft20">

                                <asp:Button ID="btnTemporarySave" runat="server" Text="Temporary Save" CausesValidation="false"

                                    CommandName="TemporarySave" class="buttonfrm" OnClick="wcMITApplication_TempSaveButtonClick" OnClientClick="ShowLoading();" />

                            </div>

                        </StepNavigationTemplate>

                        <FinishNavigationTemplate>

                            <div class="floatright">

                                <asp:Button ID="btnPrevious" runat="server" Text="<< Previous" CausesValidation="false"

                                    CommandName="MovePrevious" class="buttonfrm" OnClientClick="ShowLoading();" />

                                <asp:Button ID="btnCancel" runat="server" Text="Cancel" CausesValidation="false"

                                    CommandName="Cancel" class="buttonfrm" />

                                <asp:Button ID="btnSubmit" runat="server" Text="Submit" CausesValidation="true" class="buttonfrm btnFinishWizard"

                                    CommandName="MoveComplete"  OnClientClick="ShowLoading();"/>

                            </div>

                            <div class="floatleft padleft20">

                                <asp:Button ID="btnTemporarySave" runat="server" Text="Temporary Save" CausesValidation="false"

                                    CommandName="TemporarySave" class="buttonfrm" OnClick="wcMITApplication_TempSaveButtonClick" OnClientClick="ShowLoading();" />

                            </div>

                        </FinishNavigationTemplate>

                    </asp:Wizard>

                </div>

                <div class="cls padbottom">

                </div>

                <asp:HiddenField ID="hdnBtnClickCount" runat="server" />

            </div>

            <div class="dialogLookup dialogErrorMessage" id="divErrorMessage">

                <div id="dialogErrorMessage-Lookup">

                    <div class="floatleft padtop">

                        <span>

                            <img src="../../Images/error.png" />Error! </span>

                    </div>

                    <div class=" cls">

                    </div>

                    <div class="dialogErrorMessage-body">

                        <ul>

                            <li><span id="lblErrorMessage"></span></li>

                            <li class="padbottom"></li>

                            <li>

                                <input class="closeLookup btn" type="button" value="OK" id="btnCloseErrorMsg" runat="server" />

                            </li>

                        </ul>

                    </div>

                </div>

            </div>

        </ContentTemplate>

    </asp:UpdatePanel>

</asp:Content>

