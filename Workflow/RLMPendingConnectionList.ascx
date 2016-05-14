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
                            <Rock:Grid ID="gWorkflows" runat="server" OnRowSelected="gWorkflows_Edit">
                                <Columns>                                    
                                    <Rock:RockBoundField DataField="ConnectionRequest" HeaderText="Connection" />                                    
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
