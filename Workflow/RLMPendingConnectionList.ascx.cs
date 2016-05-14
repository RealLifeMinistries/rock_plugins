using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace com.reallifeministries
{
    [DisplayName( "Pending Connection List (RLM Custom)" )]
    [Category( "Connections" )]
    [Description( "Lists all the pending connections based on a Group and a Group Role" )]

    [GroupField( "Group", "Either pick a specific group or choose <none> to have group be determined by the groupId page parameter",false )]    
    public partial class RLMPendingConnectionList : RockBlock, ISecondaryBlock
    {
        #region Private Variables

        private DefinedValueCache _inactiveStatus = null;
        private Group _group = null;        
        private bool _canView = false;
        private RockContext ctx = new RockContext();
        #endregion



        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );            
            // if this block has a specific GroupId set, use that, otherwise, determine it from the PageParameters
            Guid groupGuid = GetAttributeValue( "Group" ).AsGuid();
            int groupId = 0;
            
            if ( groupGuid == Guid.Empty )
            {
                groupId = PageParameter( "GroupId" ).AsInteger();
            }

            if (!(groupId == 0 && groupGuid == Guid.Empty))
            {
                string key = string.Format("Group:{0}", groupId);
                _group = RockPage.GetSharedItem(key) as Group;
                if (_group == null)
                {
                    _group = new GroupService(ctx).Queryable("GroupType.Roles")
                        .Where(g => g.Id == groupId || g.Guid == groupGuid)
                        .FirstOrDefault();
                    RockPage.SaveSharedItem(key, _group);
                }

                if (_group != null && _group.IsAuthorized(Authorization.VIEW, CurrentPerson))
                {
                    _canView = true;                    
                }
            }
           
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                pnlContent.Visible = _canView;
                if ( _canView )
                {                    
                    BindWorkflowsGrid();
                }
            }
        }

        #endregion

        #region Workflows Grid
        
        /// <summary>
        /// Handles the GridRebind event of the gWorkflows control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void gWorkflows_GridRebind( object sender, EventArgs e )
        {
            BindWorkflowsGrid();
        }

        #endregion

        #region Internal Methods        

        /// <summary>
        /// Binds the group members grid.
        /// </summary>
        protected void BindWorkflowsGrid()
        {
            if ( _group != null )
            {
                pnlWorkflowActivities.Visible = true;
                
                nbRoleWarning.Visible = false;                
                gWorkflows.Visible = true;                

                var qry = new WorkflowService(ctx).Queryable("WorkflowType")
                        .Where(w =>
                            w.ActivatedDateTime.HasValue &&
                            !w.CompletedDateTime.HasValue && w.Activities.Where(a => a.AssignedGroupId == _group.Id).FirstOrDefault() != null).OrderByDescending(w => w.ActivatedDateTime);
                var dataSource = qry.ToList();
                gWorkflows.DataSource = dataSource;
                gWorkflows.DataBind();                
            }
            else
            {
                pnlWorkflowActivities.Visible = false;
            }
        }

        protected void gWorkflows_Entry(object sender, RowEventArgs e)
        {
            //var pageGuid = new PageService(ctx).Queryable().Where(p => p.InternalName == "WorkflowEntry").Select(p => p.Guid).FirstOrDefault();
            //Dictionary<String, String> queryParams = new Dictionary<string, string>();
            //queryParams.Add()
            //NavigateToPage(pageGuid,)
            Response.Redirect(String.Format("~/WorkflowEntry/{0}/{1}", e.RowKeyId, _group.Id), false);
            Context.ApplicationInstance.CompleteRequest();
        }
        #endregion

        #region ISecondaryBlock

        /// <summary>
        /// Sets the visible.
        /// </summary>
        /// <param name="visible">if set to <c>true</c> [visible].</param>
        public void SetVisible( bool visible )
        {
            pnlContent.Visible = visible;
        }

        #endregion
    }
}