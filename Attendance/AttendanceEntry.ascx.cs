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
                //BindCampusPicker();
                BindWorshipServicePicker();

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
                        //ddlCampus.SelectedCampusId = selectedCampus;
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
        protected void BindWorshipServicePicker()
        {
            var worshipServiceValues = new DefinedTypeService(ctx).Queryable().Where(t => t.Name == "Campus Worship Service").Select(d => d.DefinedValues);
            List<CampusWorshipService> campusWorshipServices = new List<CampusWorshipService>();
            foreach (var value in worshipServiceValues.FirstOrDefault())
            {
                value.LoadAttributes();
                var worshipService = value.Attributes.Where(a => a.Key == "WorshipService").Select(v => v.Value).FirstOrDefault();
                campusWorshipServices.Add(new CampusWorshipService
                {
                    Text = value.Value,
                    WorshipService = worshipService.GetAttributeValue("WorshipService"),
                    Campus = worshipService.GetAttributeValue("Campus"),
                    PrayerCategory = worshipService.GetAttributeValue("PrayerCategory"),
                });                
            }            
            ddlWorshipService.DataSource =  campusWorshipServices;            
            ddlWorshipService.DataBind();             

        }


        protected void btnSearch_Click( object sender, EventArgs e )
        {
            lblMessage.Text = null;

            Session["attendance-attended-date"] = dpAttendanceDate.SelectedDate;
            //if (ddlCampus.SelectedCampusId != null)
            //{
            //    Session["campus-id"] = ddlCampus.SelectedCampusId;
            //}
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

                //var campus_id = ddlCampus.SelectedValue;
                //if (!String.IsNullOrEmpty( campus_id ))
                //{
                //    attendance.CampusId = Convert.ToInt32(campus_id);
                //}
                
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
                
                mergeFields.Add( "Person", e.Row.DataItem );
                if (ddlWorshipService.SelectedItem != null)
                {
                    var selectedWorshipService = ddlWorshipService.SelectedItem;
                    mergeFields.Add("WorshipService", selectedWorshipService);
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
