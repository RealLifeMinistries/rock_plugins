<%@ Control Language="C#" AutoEventWireup="true" CodeFile="AttendanceEntry.ascx.cs" Inherits="com.reallifeministries.Attendance.AttendanceEntry" %>

<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <% if (!String.IsNullOrEmpty(lblMessage.Text)) { %>
        <p class="alert alert-info">
            <i class="fa fa-info-circle"></i> <asp:label ID="lblMessage" runat="server"  />
        </p>
        <% } %>

        <asp:Panel runat="server" ID="pnlSearch" DefaultButton="btnSearch" >
            <fieldset>
                <legend>Attendance Info</legend>
                <Rock:DatePicker runat="server"  id="dpAttendanceDate" Required="true" Label="Attended Date" />
                <Rock:DataDropDownList ID="ddlWorshipService" SourceTypeName="Rock.Model.DefinedType, Rock" PropertyName="Name" runat="server" Label="Worship Service" />
            </fieldset>                      
            <fieldset>
                <legend>Search For Person</legend>                   
                <Rock:RockTextBox ID="tbPhoneNumber" runat="server" Label="Phone Number"  />
                <Rock:RockTextBox ID="tbName" runat="server" Label="Name" />
                <p>
                    <Rock:BootstrapButton ID="btnSearch" runat="server" CssClass="btn btn-lg btn-primary" Text="<i class='fa fa-search'></i> Search" OnClick="btnSearch_Click" />
                    <Rock:BootstrapButton ID="btnClear" runat="server" CssClass="btn btn-default" Text="Clear" OnClick="btnClear_Click" />  
                </p>
            </fieldset>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlResults" visible="false">
            <Rock:Grid runat="server" ID="gResults" AllowSorting="true" cssClass="people-results" DataKeyNames="Id" OnRowDataBound="gResults_RowDataBound" >
                <Columns>
                    <Rock:RockTemplateField>
                        <HeaderTemplate>
                            <input type="checkbox" onclick="javascript:ToggleAllRows(this);"  />
                        </HeaderTemplate>
                        <ItemTemplate>
                            <asp:CheckBox runat="server" ID="chkSelect" />
                        </ItemTemplate>
                    </Rock:RockTemplateField>
                    <Rock:RockTemplateField HeaderText="">
                        <ItemTemplate></ItemTemplate>
                    </Rock:RockTemplateField>
                    <Rock:RockTemplateField>
                        <ItemTemplate>
                            <asp:LinkButton runat="server" ID="btnFamily" OnClick="btnFamily_Click" CommandArgument='<%# Eval("ID") %>'>
                                <i class="fa fa-group"></i>
                            </asp:LinkButton>
                        </ItemTemplate>
                    </Rock:RockTemplateField>
                    <Rock:RockTemplateField HeaderText="Name">
                        <ItemTemplate>                            
                            <asp:HyperLink runat="server" Target="_blank" Text='<%# Eval("FullNameReversed") %>' NavigateUrl='<%# Eval("ID","~/Person/{0}") %>' ToolTip='<%# ShowToolTip((Int32)Eval("ID")) %>' />                            
                        </ItemTemplate>
                    </Rock:RockTemplateField>
                    <Rock:RockTemplateField HeaderText="Address">
                        <ItemTemplate><%#  GetAddress((Int32)Eval("ID")) %></ItemTemplate>
                    </Rock:RockTemplateField>
                    <Rock:RockTemplateField HeaderText="Phone Number(s)">                        
                        <ItemTemplate>                            
                            <%# ShowPhoneNumbers((IEnumerable<Rock.Model.PhoneNumber>)Eval("PhoneNumbers")) %>
                        </ItemTemplate>                             
                    </Rock:RockTemplateField>                                
                    <asp:BoundField HeaderText="Email" DataField="Email"/>        
                    <Rock:RockTemplateField HeaderText="Record Status">
                        <ItemTemplate>
                            <%# Eval("RecordStatusValue.Value").ToString() %>
                        </ItemTemplate>
                    </Rock:RockTemplateField>
                </Columns>
                
            </Rock:Grid>
            <p>
                <Rock:BootstrapButton ID="btnRecord" runat="server" Text="<i class='fa fa-save'></i> Record Attendance" CssClass="btn btn-primary btn-lg" OnClick="btnRecord_Click" />
            </p>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>

<script>
    function ToggleAllRows(chk) {
        var $gResults = $(".people-results");
        var $chk = $(chk)
        $gResults.find(':checkbox').not($chk).prop('checked', chk.checked);
    }
   
</script>
