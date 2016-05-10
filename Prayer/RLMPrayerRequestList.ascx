<%@ Control Language="C#" AutoEventWireup="true" CodeFile="RLMPrayerRequestList.ascx.cs" Inherits="RockWeb.Blocks.Prayer.PrayerRequestList" %>
<asp:UpdatePanel ID="upPrayerRequests" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlLists" runat="server" Visible="true">
            

            <div class="panel panel-block">
                <div class="panel-heading">
                    <h1 class="panel-title"><i class="fa fa-cloud-upload"></i> Prayer Requests</h1>
                </div>
                <div class="panel-body">
                    <div class="grid grid-panel">
                        <Rock:GridFilter ID="gfFilter" runat="server" OnApplyFilterClick="gfFilter_ApplyFilterClick" OnDisplayFilterValue="gfFilter_DisplayFilterValue">
                            <Rock:DateRangePicker ID="drpDateRange" runat="server" Label="Date Range" />
                            <Rock:CategoryPicker ID="catpPrayerCategoryFilter" runat="server" Label="Category" EntityTypeName="Rock.Model.PrayerRequest"/>
                        </Rock:GridFilter>

                        <Rock:ModalAlert ID="maGridWarning" runat="server" />

                        <Rock:Grid ID="gPrayerRequests" runat="server" AllowSorting="true" AllowPaging="true" RowItemText="request" OnRowSelected="gPrayerRequests_Edit" OnRowDataBound="gPrayerRequests_RowDataBound" >
                            <Columns>                             
                                <asp:HyperLinkField DataTextField="PersonName" DataNavigateUrlFields="PersonId" SortExpression="PersonName" DataNavigateUrlFormatString="~/Person/{0}" HeaderText="Person" />                                   
                                <asp:TemplateField HeaderText="Age" SortExpression="Age" >
                                    <ItemTemplate>
                                    <%# Eval("Person.Age") %>
                                </ItemTemplate>
                                </asp:TemplateField>                                
                                <asp:TemplateField HeaderText="Phone Number(s)" >
                                    <ItemTemplate>
                                    <%# ShowPhoneNumbers((IEnumerable<Rock.Model.PhoneNumber>)Eval("Person.PhoneNumbers")) %>
                                </ItemTemplate>
                                </asp:TemplateField> 
                                <asp:TemplateField HeaderText="Groups">
                                    <ItemTemplate>
                                    <%# ShowGroups((Rock.Model.Person)Eval("Person")) %>
                                </ItemTemplate>
                                </asp:TemplateField>           
                                <asp:TemplateField HeaderText="Region">
                                    <ItemTemplate>                                                                        
                                    <%# ShowRegion((Rock.Model.Person)Eval("Person")) %>
                                </ItemTemplate>
                                </asp:TemplateField>                                                 
                                <Rock:RockBoundField DataField="CategoryName" HeaderText="Category" SortExpression="CategoryName" />
                                <Rock:DateField DataField="EnteredDate" HeaderText="Entered" SortExpression="EnteredDate"/>
                                <Rock:RockBoundField DataField="Text" HeaderText="Request"/>                                
                                <Rock:DeleteField OnClick="gPrayerRequests_Delete"  />
                            </Columns>
                        </Rock:Grid>
                    </div>
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
