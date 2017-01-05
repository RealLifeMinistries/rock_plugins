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
using DotLiquid;
using System.Runtime.Serialization;
using System.Text;
using Rock.Web.Cache;
using System.Drawing;

namespace com.reallifeministries.Attendance
{
    /// <summary>
    /// Attendence Entry
    /// </summary>
    [Category( "Attendance" )]
    [Description( "All Church Attendance" )]    
    [CodeEditorField("CustomLavaColumn","Custom Lava to insert into each person row. {{Person.[attributeName]}}",CodeEditorMode.Lava,CodeEditorTheme.Rock,200,false)]
    [IntegerField("Schedule Id","The schedule connected with this Attendance",true)]
    public partial class AttendanceEntry : RockBlock
    {
        protected RockContext ctx;
        private string _customLava;
        protected List<CampusWorshipAttendance> campusWorshipAttendance;

        protected void Page_Load(object sender, EventArgs e)
        {
            ctx = new RockContext();
            _customLava = GetAttributeValue("CustomLavaColumn");

            if (!IsPostBack)
            {
                pnlResults.Visible = false;                
                BindWorshipServicePicker();

                var lastAttendedDate = Session["attendance-attended-date"];
                if(lastAttendedDate != null) {
                    dpAttendanceDate.SelectedDate = (DateTime)lastAttendedDate;
                } else {
                    dpAttendanceDate.SelectedDate = GetLastSunday(DateTime.Now);
                }
                string personId = PageParameter("PersonId");
                var person = new PersonService(ctx).Get(personId.AsInteger());
                if (person != null){
                    tbName.Text = person.LastName + "," + person.FirstName;                    
                    var selectedCampusWorshipAttendance = Session["selected-campus-worship-attendance"];
                    if (selectedCampusWorshipAttendance != null )
                    {
                        ddlWorshipService.SelectedValue = selectedCampusWorshipAttendance.ToString();
                    }
                    btnSearch_Click(null, null);
                }
                
                
            }
            tbPhoneNumber.Focus();
        }

        private DateTime GetLastSunday(DateTime oDate)
        {
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
        protected void BindWorshipServicePicker()
        {
            var worshipServiceValues = new DefinedTypeService(ctx).Queryable().Where(t => t.Name == "Campus Worship Service").Select(d => d.DefinedValues);
            campusWorshipAttendance = new List<CampusWorshipAttendance>();
            foreach (var value in worshipServiceValues.FirstOrDefault())
            {
                value.LoadAttributes();
                var worshipService = value.AttributeValues.Where(a => a.Key == "WorshipService").Select(v => v.Value).FirstOrDefault();                                
                var campus = value.AttributeValues.Where(a => a.Key == "Campus").Select(v => v.Value).FirstOrDefault();
                var prayerCat = value.AttributeValues.Where(a => a.Key == "PrayerCategory").Select(v => v.Value).FirstOrDefault();
                campusWorshipAttendance.Add(new CampusWorshipAttendance
                {
                    Text = value.Value,
                    WorshipService = worshipService.Value,
                    Campus = campus.Value,
                    PrayerCategory = prayerCat.Value
                });                
            }
            ddlWorshipService.DataSource = campusWorshipAttendance;
            Session["campus-worship-attendance"] = campusWorshipAttendance;
            ddlWorshipService.DataBind();             

        }


        protected void btnSearch_Click( object sender, EventArgs e )
        {
            lblMessage.Text = null;
            var personService = new PersonService(ctx);
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
                lblMessage.Text = "No Phone Number or Name was entered, please enter a Phone Number or Name";
                return;
            }

            Session["attendance-attended-date"] = dpAttendanceDate.SelectedDate;            
            pnlResults.Visible = true;

            gResults.Caption = "Search Results";
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
            tbPhoneNumber.Focus();
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
            var campusWorshipAttendance = Session["campus-worship-attendance"] as List<CampusWorshipAttendance>;
            CampusWorshipAttendance selectedWorshipService = null;
            if (campusWorshipAttendance != null)
            {
                selectedWorshipService = campusWorshipAttendance.Find(m => m.Text == ddlWorshipService.SelectedValue);                
            }
            Guid? groupGuid = null;
            Guid? campusGuid = null;
            if (selectedWorshipService != null)
            {
                groupGuid = selectedWorshipService.WorshipService.AsGuid();
                campusGuid = selectedWorshipService.Campus.AsGuid();
            }       
            else
            {
                lblMessage.Text = "Could not record attendance, campus worship attendance not set";
                return;
            }     
            var people = new PersonService(ctx).GetByIds( peopleIds );
            
            foreach (Person person in people)
            {
                var lastSunday = GetLastSunday((DateTime)dpAttendanceDate.SelectedDate);
                var attendanceService = new AttendanceService(ctx);
                var attendance = attendanceService.Queryable().Where(a => a.PersonAliasId == person.PrimaryAliasId
                && a.SundayDate.Year == lastSunday.Year
                && a.SundayDate.Month == lastSunday.Month
                && a.SundayDate.Day == lastSunday.Day
                && a.Group.Guid == groupGuid.Value
                && a.Campus.Guid == campusGuid.Value).FirstOrDefault();
                if (attendance == null)
                {
                    attendanceService.Add(new Rock.Model.Attendance
                    {
                        PersonAlias = person.PrimaryAlias,
                        StartDateTime = (DateTime)dpAttendanceDate.SelectedDate,
                        Group = new GroupService(ctx).Get(groupGuid.Value),
                        Campus = new CampusService(ctx).Get(campusGuid.Value),
                        DidAttend = true
                    });
                    if (lblMessage.Text.Contains("Attendance Recorded For"))
                    {
                        lblMessage.Text += ", " + person.FirstName + " " + person.LastName;
                    }
                    else
                    {
                        lblMessage.Text += "Attendance Recorded For: " + person.FirstName + " " + person.LastName;
                    }                    
                }
                else
                {
                    lblMessage.Text += "Attendance Already Exists for " + person.FirstName + " " + person.LastName + "<BR>";
                }              
            }            

            ctx.SaveChanges();

            clearForm();
            clearResults();           
        }


        protected void gResults_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if (e.Row.RowType == DataControlRowType.DataRow  && !String.IsNullOrEmpty(_customLava))
            {
                TableCell cell = e.Row.Cells[1];
                var person = (Person)(e.Row.DataItem);
                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields(RockPage);
                if (person != null && person.RecordStatusValue.Value == "Inactive")
                {
                    foreach (TableCell c in e.Row.Cells)
                    {
                        c.ForeColor = Color.IndianRed;
                    }
                }                                
                campusWorshipAttendance = Session["campus-worship-attendance"] as List<CampusWorshipAttendance>;
                if (campusWorshipAttendance != null)
                {
                    var selectedWorshipService = campusWorshipAttendance.Find(m => m.Text == ddlWorshipService.SelectedValue);                        
                    if (selectedWorshipService != null){
                        Session["selected-campus-worship-attendance"] = ddlWorshipService.SelectedValue;
                        mergeFields.Add("WorshipService", selectedWorshipService.WorshipService);
                        mergeFields.Add("Campus", selectedWorshipService.Campus);
                        mergeFields.Add("PrayerCategory", selectedWorshipService.PrayerCategory);                        
                    }
                }
                mergeFields.Add("Person", person);
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
    public class CampusWorshipAttendance
    {
        public string Text { get; set; }
        public string Campus { get; set; }
        public string WorshipService { get; set; }
        public string PrayerCategory { get; set; }

        public override string ToString() { return Text; }        
    }
}
