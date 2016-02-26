using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Attribute;
using Rock.Constants;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using Rock.Web.Cache;
using System.Drawing;
using System.Text;

namespace com.reallifeministries.Attendance
{
    /// <summary>
    /// Attendence Entry
    /// </summary>
    [Category( "Attendance" )]
    [Description( "All Church Attendance" )]
    [GroupField("Attending Group","Usually the attendance area/Checkin group",true)]
    [CodeEditorField("CustomLavaColumn","Custom Lava to insert into each person row. {{Person.[attributeName]}}",CodeEditorMode.Lava,CodeEditorTheme.Rock,200,false)]
    [IntegerField("Schedule Id","The schedule connected with this Attendance",true)]
    public partial class AttendanceEntry : RockBlock
    {
        protected RockContext ctx;
        private string _customLava;

        protected void Page_Load(object sender, EventArgs e)
        {
            ctx = new RockContext();
            _customLava = GetAttributeValue("CustomLavaColumn");

            if (!IsPostBack)
            {
                pnlResults.Visible = false;
                BindCampusPicker();

                var lastAttendedDate = Session["attendance-attended-date"];
                if(lastAttendedDate != null) {
                    dpAttendanceDate.SelectedDate = (DateTime)lastAttendedDate;
                } else {
                    dpAttendanceDate.SelectedDate = GetLastSunday();
                }
                string personId = PageParameter("PersonId");
                var person = new PersonService(ctx).Get(personId.AsInteger());
                if (person != null){
                    tbName.Text = person.LastName + "," + person.FirstName;
                    int selectedCampus = 0;
                    var sessionCampus = Session["campus-id"];
                    if (sessionCampus != null && Int32.TryParse(sessionCampus.ToString(), out selectedCampus))
                    {
                        ddlCampus.SelectedCampusId = selectedCampus;
                    }
                    btnSearch_Click(null, null);
                }                

            }
        }

        private DateTime GetLastSunday()
        {

            DateTime oDate = DateTime.Now;

            DayOfWeek dow = DateTime.Now.DayOfWeek;

            if ((int)dow < 5)
            {

                return DateTime.Now.AddDays( 7 - (int)dow - 7 );
            }
            else
            {
                return DateTime.Now.AddDays( 7 - (int)dow );
            }

        }
        protected void BindCampusPicker()
        {
            ddlCampus.Campuses = CampusCache.All();
        }       

        protected void btnSearch_Click( object sender, EventArgs e )
        {
            lblMessage.Text = null;

            Session["attendance-attended-date"] = dpAttendanceDate.SelectedDate;
            if (ddlCampus.SelectedCampusId != null)
            {
                Session["campus-id"] = ddlCampus.SelectedCampusId;
            }
            var personService = new PersonService( ctx );
            pnlResults.Visible = true;

            gResults.Caption = "Search Results";

            if (!String.IsNullOrEmpty(tbPhoneNumber.Text))
            {
                gResults.DataSource = personService.GetByPhonePartial( tbPhoneNumber.Text ).ToList();
            }
            else if (!String.IsNullOrEmpty( tbName.Text ))
            {
                gResults.DataSource = personService.GetByFullName( tbName.Text, true ).ToList();
            }
            else
            {
                gResults.DataSource = ctx.People.ToList();
            }
            gResults.DataBind();
        }
      
        protected void btnFamily_Click( object sender, EventArgs e )
        {
            LinkButton btn = (LinkButton)sender;
            int id = Convert.ToInt32( btn.CommandArgument );

            var personService = new PersonService( ctx );
            var person = personService.Get( id );

            person.LoadAttributes();

            gResults.Caption = "Family of " + person.FullName;
            gResults.DataSource = person.GetFamilyMembers(true).Select( gm => gm.Person ).ToList();
            gResults.DataBind();
        }
        protected void btnClear_Click( object sender, EventArgs e )
        {
            clearResults();
            clearForm();
            lblMessage.Text = null;
        }

        protected void clearForm()
        {
            tbName.Text = null;
            tbPhoneNumber.Text = null;
        }

        protected void clearResults()
        {
            gResults.DataSource = null;
            gResults.Caption = null;
            gResults.DataBind();
            pnlResults.Visible = false;
        }
        protected string ShowToolTip(int personId)
        {
            var person = new PersonService(ctx).Get(personId);
            var stringBuilder = new StringBuilder();
            if (person != null)
            {                
                if (!string.IsNullOrEmpty(person.MiddleName))
                {
                    stringBuilder.Append(string.Format("Middle Name: {0}{1}", person.MiddleName, Environment.NewLine));
                }
                if (person.BirthDate.HasValue)
                {                    
                    stringBuilder.Append(string.Format("Birthdate: {0}{1}", person.BirthDate.Value.ToShortDateString(), Environment.NewLine));
                }
                if (person.ConnectionStatusValue != null)
                {
                    stringBuilder.Append(string.Format("Connection Status: {0}{1}", person.ConnectionStatusValue.Value, Environment.NewLine));                    
                }
                if (person.MaritalStatusValue != null)
                {
                    stringBuilder.Append(string.Format("Marital Status: {0}{1}", person.MaritalStatusValue.Value, Environment.NewLine));
                }
                var roles = person.GetFamilyMembers(true).Where(s => s.PersonId == person.Id).Select(r => r.GroupRole.Name);                
                if (roles != null)
                {
                    stringBuilder.Append(string.Format("Family Role: {0}{1}", string.Join(", ", roles), Environment.NewLine));
                }
                if (!string.IsNullOrEmpty(person.GradeFormatted))
                {
                    stringBuilder.Append(string.Format("Grade: {0}{1}", person.GradeFormatted, Environment.NewLine));
                }
            }
            return stringBuilder.ToString();
        }
        protected string ShowPhoneNumbers(IEnumerable<PhoneNumber> phoneNumbers)
        {
            var phones = string.Empty;
            foreach (var number in phoneNumbers)
            {
                if (number.NumberTypeValue.Value == "Home" || number.NumberTypeValue.Value == "Mobile")
                {
                    if (!string.IsNullOrEmpty(number.Number))
                    {
                        phones += string.Format("{0} : {1}<br> ", number.NumberTypeValue.Value, number.NumberFormatted);
                    }                    
                }
            }
            return phones;          
        }
        protected string GetAddress(int personId)
        {
            var person = new PersonService(ctx).Get(personId);
            if (person != null)
            {
                var homeAddress = person.GetHomeLocation();
                if (homeAddress != null) {
                    return homeAddress.GetFullStreetAddress();
                }                
            }
            return "";
        }
        protected string
        FormatPhoneNumber(bool unlisted, object countryCode, object number, int phoneNumberTypeId, bool smsEnabled = false)
        {
            string formattedNumber = "Unlisted";

            string cc = countryCode as string ?? string.Empty;
            string n = number as string ?? string.Empty;

            if (!unlisted)
            {
                if (GetAttributeValue("DisplayCountryCode").AsBoolean())
                {
                    formattedNumber = PhoneNumber.FormattedNumber(cc, n, true);
                }
                else
                {
                    formattedNumber = PhoneNumber.FormattedNumber(cc, n);
                }
            }

            // if the page is being loaded locally then add the tel:// link
            if (RockPage.IsMobileRequest)
            {
                formattedNumber = string.Format("<a href=\"tel://{0}\">{1}</a>", n, formattedNumber);
            }

            var phoneType = DefinedValueCache.Read(phoneNumberTypeId);
            if (phoneType != null)
            {
                if (smsEnabled)
                {
                    formattedNumber = string.Format("{0} <small>{1} <i class='fa fa-comments'></i></small>", formattedNumber, phoneType.Value);
                }
                else
                {
                    formattedNumber = string.Format("{0} <small>{1}</small>", formattedNumber, phoneType.Value);
                }
            }

            return formattedNumber;
        }
        protected void btnRecord_Click( object sender, EventArgs e )
        {
            var peopleIds = new List<int>();

            foreach (GridViewRow row in gResults.Rows)
            {
                CheckBox cb = (CheckBox)row.FindControl( "chkSelect" );
                if (cb.Checked)
                {
                    string dataKey = gResults.DataKeys[row.RowIndex].Value.ToString();
                    if (!String.IsNullOrEmpty(dataKey))
                    {
                        peopleIds.Add( Convert.ToInt32(dataKey) );
                    } 
                }
            }


            var groupGuid = GetAttributeValue( "AttendingGroup" ).AsGuid();
            var personService = new PersonService( ctx );
            var attendanceService = new AttendanceService( ctx );
            var groupService = new GroupService( ctx );

            var people = personService.GetByIds( peopleIds );
            var scheduleId = GetAttributeValue( "ScheduleId" ).AsIntegerOrNull();

            foreach (Person person in people)
            {
                Rock.Model.Attendance attendance = ctx.Attendances.Create();
                attendance.PersonAlias = person.PrimaryAlias;
                attendance.StartDateTime = (DateTime)dpAttendanceDate.SelectedDate;
                attendance.Group = groupService.Get( groupGuid );
                attendance.ScheduleId = scheduleId;               

                var campus_id = ddlCampus.SelectedValue;
                if (!String.IsNullOrEmpty( campus_id ))
                {
                    attendance.CampusId = Convert.ToInt32(campus_id);
                }
                
                attendanceService.Add( attendance );
            }

            ctx.SaveChanges();

            clearForm();
            clearResults();

            lblMessage.Text = "Attendance Recorded FOR: " + String.Join( ", ", people.Select(p => p.FullName ).ToArray() );
        }


        protected void gResults_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if (e.Row.RowType == DataControlRowType.DataRow  && !String.IsNullOrEmpty(_customLava))
            {
                TableCell cell = e.Row.Cells[1];
                var mergeFields = Rock.Web.Cache.GlobalAttributesCache.GetMergeFields( null );
                var person = (Person)(e.Row.DataItem);
                if (person.RecordStatusValue.Value == "Inactive") {
                    foreach (TableCell c in e.Row.Cells)
                    {
                        c.ForeColor = Color.IndianRed;                        
                    }
                    
                }
                mergeFields.Add( "Person", person);
                if (ddlCampus.SelectedCampusId.HasValue)
                {
                    var campus = (new CampusService(ctx).Get(ddlCampus.SelectedCampusId.Value));
                    mergeFields.Add("Campus", campus);                    
                }
                if (HttpContext.Current != null && HttpContext.Current.Items.Contains( "CurrentPerson" ))
                {
                    var currentPerson = HttpContext.Current.Items["CurrentPerson"] as Person;
                    if (currentPerson != null)
                    {
                        mergeFields.Add( "CurrentPerson", currentPerson );
                    }
                }

                cell.Text = _customLava.ResolveMergeFields( mergeFields );
            }
        }
}
}
