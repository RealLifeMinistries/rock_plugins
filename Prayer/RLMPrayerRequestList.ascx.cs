// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using System.Collections.Generic;

namespace RockWeb.Blocks.Prayer
{
    [DisplayName( "RLM Prayer Request List" )]
    [Category( "Prayer" )]
    [Description( "Custom RLM Block to display a list of prayer requests for the configured top-level group category." )]

    [SecurityAction( Authorization.APPROVE, "The roles and/or users that have access to approve prayer requests and comments." )]

    [LinkedPage( "Detail Page", Order = 0 )]    
    public partial class PrayerRequestList : RockBlock
    {
        #region Fields

        /// <summary>
        /// The prayer request key parameter used in the QueryString for detail page.
        /// </summary>
        private static readonly string _PrayerRequestKeyParameter = "prayerRequestId";

        /// <summary>
        /// Holds whether or not the person can add, edit, and delete.
        /// </summary>
        private bool _canAddEditDelete = false;

        /// <summary>
        /// Holds whether or not the person can approve requests.
        /// </summary>
        private bool _canApprove = false;

        private RockContext rockContext = new RockContext();

        #endregion

        #region Filter's User Preference Setting Keys
        /// <summary>
        /// Constant like string-key-settings that are tied to user saved filter preferences.
        /// </summary>
        public static class FilterSetting
        {
            public static readonly string PrayerCategory = "Prayer Category";
            public static readonly string DateRange = "Date Range";            
            public static readonly string ShowExpired = "Show Expired";
        }
        #endregion  

        #region Base Control Methods

        /// <summary>
        /// Handles the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            BindFilter();

            gPrayerRequests.DataKeyNames = new string[] { "Id" };
            gPrayerRequests.Actions.AddClick += gPrayerRequests_Add;
            gPrayerRequests.GridRebind += gPrayerRequests_GridRebind;

            // Block Security and special attributes (RockPage takes care of View)
            _canApprove = IsUserAuthorized( "Approve" );
            _canAddEditDelete = IsUserAuthorized( Authorization.EDIT );
            gPrayerRequests.Actions.ShowAdd = _canAddEditDelete;
            gPrayerRequests.IsDeleteEnabled = _canAddEditDelete;
        }

        /// <summary>
        /// Handles the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                BindGrid();
            }

            base.OnLoad( e );
        }

        #endregion

        #region Grid Filter
        /// <summary>
        /// Binds any needed data to the Grid Filter also using the user's stored
        /// preferences.
        /// </summary>
        private void BindFilter()
        {
            // set the date range filter
            drpDateRange.DelimitedValues = gfFilter.GetUserPreference( FilterSetting.DateRange );

            // Set the category picker's selected value
            int selectedPrayerCategoryId = gfFilter.GetUserPreference( FilterSetting.PrayerCategory ).AsInteger();
            Category prayerCategory = new CategoryService(rockContext).Get( selectedPrayerCategoryId );
            catpPrayerCategoryFilter.SetValue( prayerCategory );            
        }

        /// <summary>
        /// Handles the Apply Filter event for the GridFilter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void gfFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            gfFilter.SaveUserPreference( FilterSetting.DateRange, drpDateRange.DelimitedValues );

            gfFilter.SaveUserPreference( FilterSetting.PrayerCategory, catpPrayerCategoryFilter.SelectedValue == Rock.Constants.None.IdValue ? string.Empty : catpPrayerCategoryFilter.SelectedValue );
            
            BindGrid();
        }

        /// <summary>
        /// Handles displaying the stored filter values.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e as DisplayFilterValueArgs (hint: e.Key and e.Value).</param>
        protected void gfFilter_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "Date Range":
                    e.Value = DateRangePicker.FormatDelimitedValues( e.Value );
                    break;

                // don't display dead setting
                case "From Date":
                    e.Value = string.Empty;
                    break;

                // don't display dead setting
                case "To Date":
                    e.Value = string.Empty;
                    break;

                case "Prayer Category":

                    int categoryId = e.Value.AsIntegerOrNull() ?? All.Id;
                    if ( categoryId == All.Id )
                    {
                        e.Value = "All";
                    }
                    else
                    {
                        var category = Rock.Web.Cache.CategoryCache.Read( categoryId );
                        if ( category != null )
                        {
                            e.Value = category.Name;
                        }
                    }

                    break;
            }
        }

        #endregion

        #region Prayer Request Grid

        /// <summary>
        /// Binds the grid to a list of Prayer Requests.
        /// </summary>
        private void BindGrid()
        {
            PrayerRequestService prayerRequestService = new PrayerRequestService(rockContext);
            SortProperty sortProperty = gPrayerRequests.SortProperty;

            var prayerRequests = prayerRequestService.Queryable().Select( a =>
                new
                {
                    a.Id,                    
                    Person = a.RequestedByPersonAlias != null ? a.RequestedByPersonAlias.Person : null,                                        
                    CategoryName = a.CategoryId.HasValue ? a.Category.Name : null,                    
                    EnteredDate = a.EnteredDateTime,                    
                    a.Text,                    
                    a.CategoryId,
                    CategoryParentCategoryId = a.CategoryId.HasValue ? a.Category.ParentCategoryId : null
                } );

            // Filter by prayer category if one is selected...
            int selectedPrayerCategoryID = catpPrayerCategoryFilter.SelectedValue.AsIntegerOrNull() ?? All.Id;
            if ( selectedPrayerCategoryID != All.Id && selectedPrayerCategoryID != None.Id )
            {
                prayerRequests = prayerRequests.Where( c => c.CategoryId == selectedPrayerCategoryID
                    || c.CategoryParentCategoryId == selectedPrayerCategoryID );
            }
                       
            // Filter by Date Range
            if ( drpDateRange.LowerValue.HasValue )
            {
                DateTime startDate = drpDateRange.LowerValue.Value.Date;
                prayerRequests = prayerRequests.Where( a => a.EnteredDate >= startDate );
            }

            if ( drpDateRange.UpperValue.HasValue )
            {
                // Add one day in order to include everything up to the end of the selected datetime.
                var endDate = drpDateRange.UpperValue.Value.AddDays( 1 );
                prayerRequests = prayerRequests.Where( a => a.EnteredDate < endDate );
            }
            

            if ( sortProperty != null )
            {
                gPrayerRequests.SetLinqDataSource(prayerRequests.Sort( sortProperty ));
            }
            else
            {
                gPrayerRequests.SetLinqDataSource(prayerRequests.OrderByDescending( p => p.EnteredDate ).ThenByDescending( p => p.Id ));
            }

            gPrayerRequests.EntityTypeId = EntityTypeCache.Read<PrayerRequest>().Id;
            gPrayerRequests.ExportSource = ExcelExportSource.ColumnOutput;
            gPrayerRequests.DataBind();
        }

        /// <summary>
        /// Handles the Add event of the gPrayerRequests control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void gPrayerRequests_Add( object sender, EventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", _PrayerRequestKeyParameter, 0 );
        }

        /// <summary>
        /// Handles the Edit event of the gPrayerRequests control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gPrayerRequests_Edit( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", _PrayerRequestKeyParameter, e.RowKeyId );
        }

        protected string ShowPhoneNumbers(IEnumerable<PhoneNumber> phoneNumbers)
        {
            var phones = string.Empty;
            if (phoneNumbers != null)
            {
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
            }            
            return phones;
        }
        protected string ShowGroups(Person person)
        {
            if (person == null)
                return String.Empty;
            int[] groupTypes = { 25, 27, 34 };            
            GroupMemberService groupMemberService = new GroupMemberService(rockContext);
            var qry = groupMemberService.Queryable("Person,GroupRole,Group", false)
                .Where(gm => gm.PersonId == person.Id).OrderBy(g => g.Group.GroupType.Id);
            var groupNames = qry.Where(g => groupTypes.Contains(g.Group.GroupType.Id)).Select(s => new
            {
                GroupName = s.Group.Name + " (" + s.GroupRole.Name + ")",
                GroupTypeId = s.Group.GroupTypeId
            }).ToList();
            groupNames.OrderBy(g => g.GroupTypeId);

            return String.Join(", ", groupNames.Select(s => s.GroupName));
        }

        protected string ShowAddress(Person person)
        {
            if (person == null)
                return String.Empty;
            Location homeLocation = person.GetHomeLocation(rockContext);
            if (homeLocation != null)
                return homeLocation.FormattedAddress;
            else
                return String.Empty;
        }

        protected string ShowRegion(Person person)
        {
            string region = "No Region";
            if (person == null)
                return region;

            var groupLocations = rockContext.GroupLocations
                        .Where(gl => gl.Location.GeoFence != null ).ToList();
            var homeLocation = person.GetHomeLocation(rockContext);
            var regionLoc = groupLocations.Where(gl => homeLocation !=  null && homeLocation.GeoPoint != null &&
                           homeLocation.GeoPoint.Intersects(gl.Location.GeoFence)
                        ).Select(gl => gl.Location).FirstOrDefault();
            
            if (regionLoc != null)
            {                
                region = regionLoc.Name;
            }
            return region;
        }

        /// <summary>
        /// Handles the CheckChanged event of the gPrayerRequests IsApproved field.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gPrayerRequests_CheckChanged( object sender, RowEventArgs e )
        {
            bool failure = true;

            if ( e.RowKeyValue != null )
            {
                
                PrayerRequestService prayerRequestService = new PrayerRequestService( rockContext );
                PrayerRequest prayerRequest = prayerRequestService.Get( e.RowKeyId );

                if ( prayerRequest != null )
                {
                    failure = false;

                    // if it was approved, set it to unapproved... otherwise
                    if ( prayerRequest.IsApproved ?? false )
                    {
                        prayerRequest.IsApproved = false;
                    }
                    else
                    {
                        prayerRequest.IsApproved = true;
                        prayerRequest.ApprovedByPersonAliasId = CurrentPersonAliasId;
                        prayerRequest.ApprovedOnDateTime = RockDateTime.Now;

                        // reset the flag count only to zero ONLY if it had a value previously.
                        if ( prayerRequest.FlagCount.HasValue && prayerRequest.FlagCount > 0 )
                        {
                            prayerRequest.FlagCount = 0;
                        }                        
                    }

                    rockContext.SaveChanges();
                }

                BindGrid();
            }

            if ( failure )
            {
                maGridWarning.Show( "Unable to approve that prayer request", ModalAlertType.Warning );
            }
        }

        /// <summary>
        /// Handles the Delete event of the gPrayerRequests control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gPrayerRequests_Delete( object sender, RowEventArgs e )
        {
            var rockContext = new RockContext();
            PrayerRequestService prayerRequestService = new PrayerRequestService( rockContext );
            PrayerRequest prayerRequest = prayerRequestService.Get( e.RowKeyId );

            if ( prayerRequest != null )
            {
                DeleteAllRelatedNotes( prayerRequest, rockContext );

                string errorMessage;
                if ( !prayerRequestService.CanDelete( prayerRequest, out errorMessage ) )
                {
                    maGridWarning.Show( errorMessage, ModalAlertType.Information );
                    return;
                }

                prayerRequestService.Delete( prayerRequest );
                rockContext.SaveChanges();
            }

            BindGrid();
        }

        /// <summary>
        /// Deletes all related notes.
        /// </summary>
        /// <param name="prayerRequest">The prayer request.</param>
        private void DeleteAllRelatedNotes( PrayerRequest prayerRequest, RockContext rockContext )
        {
            var noteTypeService = new NoteTypeService( rockContext );
            var noteType = noteTypeService.Get( Rock.SystemGuid.NoteType.PRAYER_COMMENT.AsGuid() );
            var noteService = new NoteService( rockContext );
            var prayerComments = noteService.Get( noteType.Id, prayerRequest.Id );
            foreach ( Note prayerComment in prayerComments )
            {
                noteService.Delete( prayerComment );
            }

            rockContext.SaveChanges();
        }

        /// <summary>
        /// Handles the GridRebind event of the gPrayerRequests control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void gPrayerRequests_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        /// <summary>
        /// Handles the hyperlink
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs" /> instance containing the event data.</param>
        protected void gPrayerRequests_RowDataBound( object sender, GridViewRowEventArgs e )
        {           
            
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                foreach ( TableCell cell in e.Row.Cells )
                {                    
                    foreach ( Control c in cell.Controls )
                    {
                        Toggle toggle = c as Toggle;
                        if ( toggle != null )
                        {
                            toggle.Enabled = false;
                        }
                    }
                }                
            }                    
        }

        #endregion
    }
}