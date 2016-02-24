using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Attribute;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using DotLiquid;
using System.Runtime.Serialization;

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
                    dpAttendanceDate.SelectedDate = GetLastSunday();
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
                new AttendanceService(ctx).Add(new Rock.Model.Attendance
                {
                    PersonAlias = person.PrimaryAlias,
                    StartDateTime = (DateTime)dpAttendanceDate.SelectedDate,
                    Group = new GroupService(ctx).Get(groupGuid.Value),                   
                    Campus = new CampusService(ctx).Get(campusGuid.Value),
                });
            }            

            ctx.SaveChanges();

            clearForm();
            clearResults();

            lblMessage.Text = "Attendance Recorded FOR: " + String.Join( ", ", people.Select(p => p.FirstName + " " + p.LastName ).ToArray() );
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
    public class CampusWorshipAttendance
    {
        public string Text { get; set; }
        public string Campus { get; set; }
        public string WorshipService { get; set; }
        public string PrayerCategory { get; set; }

        public override string ToString() { return Text; }        
    }
}
