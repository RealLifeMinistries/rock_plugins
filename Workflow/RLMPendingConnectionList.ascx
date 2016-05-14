<%@ Control Language="C#" AutoEventWireup="true" CodeFile="RLMPendingConnectionList.ascx.cs" Inherits="com.reallifeministries.RLMPendingConnectionList" %>
<script runat="server">

    protected void Unnamed_Click(object sender, RowEventArgs e)
    {

    }
</script>


<asp:UpdatePanel ID="upList" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlContent" runat="server">

            <div id="pnlWorkflowActivities" runat="server">

                <div class="panel panel-block">
                
                    <div class="panel-heading clearfix">
                        <h1 class="panel-title pull-left">
                            <i class="fa fa-users"></i>
                            Connection Requests
                        </h1>
                    </div>

                    <div class="panel-body">
                        <Rock:ModalAlert ID="mdGridWarning" runat="server" />

                        <Rock:NotificationBox ID="nbRoleWarning" runat="server" NotificationBoxType="Warning" Title="No roles!" Visible="false" />
                        
                        <div class="grid grid-panel">                            
                            <Rock:Grid ID="gWorkflows" runat="server" DisplayType="Full" AllowPaging="true" AllowSorting="true">
                                <Columns>
                                    <Rock:RockTemplateField HeaderText="Connection Pending Entry">
                                        <ItemTemplate>                                                                                     
                                            <asp:HyperLink runat="server" Target="_blank" Text='Connection Pending Entry' NavigateUrl='<%# String.Format("~/WorkflowEntry/{0}/{1}", Eval("WorkflowType.Id"), Eval("Workflow.Id")) %>' />                            
                                        </ItemTemplate>
                                    </Rock:RockTemplateField> 
                                    <Rock:RockBoundField DataField="ConnectionRequest" HeaderText="ConnectionRequest" />
                                    <Rock:RockBoundField DataField="ActivityName" HeaderText="Activity Name"/>
                                    <Rock:RockBoundField DataField="WorkflowType.Name" HeaderText="Workflow Type"/>                                    
                                    <Rock:DateTimeField DataField="ActivatedDateTime" HeaderText="Requested Time" />
                                    <Rock:RockBoundField DataField="Status" HeaderText="Status" />                                                            
                                </Columns>
                            </Rock:Grid>
                        </div>
                    </div>
                </div>
            </div>

        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
