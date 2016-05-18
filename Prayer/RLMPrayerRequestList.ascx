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

                        <Rock:Grid ID="gPrayerRequests" runat="server" AllowSorting="true" AllowPaging="true" RowItemText="request" OnRowSelected="gPrayerRequests_Edit" OnRowDataBound="gPrayerRequests_RowDataBound"  ExcelExportBehavior="AlwaysInclude" >
                            <Columns>                             
                                <Rock:PersonField DataField="Person" HeaderText="Person" SortExpression="Person.LastName" />
                                <Rock:RockBoundField DataField="Person.Age" HeaderText="Age"/>                                                                                                
                                <Rock:PhoneNumbersField DataField="Person.PhoneNumbers" HeaderText="Phone Number(s)" />
                                <Rock:RockTemplateField HeaderText="Groups">
                                    <ItemTemplate>
                                         <%# ShowGroups((Rock.Model.Person)Eval("Person")) %>
                                    </ItemTemplate>
                                </Rock:RockTemplateField>                               
                                
                                <Rock:RockTemplateField HeaderText="Region">
                                    <ItemTemplate>                                                                        
                                        <%# ShowRegion((Rock.Model.Person)Eval("Person")) %>
                                    </ItemTemplate>
                                </Rock:RockTemplateField>                                  
                                <Rock:RockBoundField DataField="CategoryName" HeaderText="Category" SortExpression="CategoryName" />
                                <Rock:DateField DataField="EnteredDate" HeaderText="Entered" SortExpression="EnteredDate"/>
                                <Rock:RockBoundField DataField="Text" HeaderText="Request"/>                                
                                <Rock:RockTemplateField HeaderText="Address" ExcelExportBehavior="AlwaysInclude" Visible="false">
                                    <ItemTemplate>
                                        <%# ShowAddress((Rock.Model.Person)Eval("Person")) %>
                                    </ItemTemplate>
                                    </Rock:RockTemplateField>
                                <Rock:DeleteField OnClick="gPrayerRequests_Delete"  />
                            </Columns>
                        </Rock:Grid>
                    </div>
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
