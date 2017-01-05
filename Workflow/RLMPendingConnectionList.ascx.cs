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
    [LinkedPage("Entry Page", "Page used to enter form information for a workflow.")]
    public partial class RLMPendingConnectionList : RockBlock, ISecondaryBlock
    {
        #region Private Variables

        private Group _group = null;        
        private bool _canView = false;
        private RockContext ctx = new RockContext();
        #endregion



        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gWorkflows.DataKeyNames = new string[] { "Id" };
            gWorkflows.Actions.ShowAdd = false;
            gWorkflows.IsDeleteEnabled = false;
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
                var workflowService = new WorkflowService(ctx);
                var qry = workflowService.Queryable("WorkflowType")
                        .Where(w =>
                            w.ActivatedDateTime.HasValue &&
                            !w.CompletedDateTime.HasValue && w.Activities.Where(a => a.AssignedGroupId == _group.Id).FirstOrDefault() != null).OrderByDescending(w => w.ActivatedDateTime);
                var workflowList = qry.ToList();
                List<PendingConnection> connectionList = new List<PendingConnection>();
                foreach (var workflow in workflowList)
                {
                    PendingConnection pc = new PendingConnection();                    
                    pc.WorkflowType = workflow.WorkflowType;
                    pc.ActivityName = workflow.ActiveActivityNames;
                    pc.Status = workflow.Status;
                    pc.Id = workflow.Id;
                    pc.ActivatedDateTime = workflow.ActivatedDateTime.Value;
                    workflow.LoadAttributes();
                    string connRequest = String.Empty;
                    var conReqAttr = workflow.AttributeValues.Where(a => a.Key == "ConnectionRequest").FirstOrDefault().Value;
                    if (conReqAttr != null)
                    {
                        pc.ConnectionRequest = conReqAttr.ValueFormatted;
                    }
                    connectionList.Add(pc);
                }
                gWorkflows.DataSource = connectionList;
                gWorkflows.DataBind();                
            }
            else
            {
                pnlWorkflowActivities.Visible = false;
            }
        }

        /// <summary>
        /// Handles the Edit event of the gWorkflows control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gWorkflows_Edit(object sender, RowEventArgs e)
        { 
            var workflow = new WorkflowService(new RockContext()).Get(e.RowKeyId);
            if (workflow != null)
            {
                var qryParam = new Dictionary<string, string>();
                qryParam.Add("WorkflowId", workflow.Id.ToString());                
                qryParam.Add("WorkflowTypeId", workflow.WorkflowTypeId.ToString());
                NavigateToLinkedPage("EntryPage", qryParam);
            }
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

        private class PendingConnection
        {
            public int Id { get; set; }
            public String ActivityName { get; set; }
            public WorkflowType WorkflowType{ get; set; }
            public DateTime ActivatedDateTime { get; set; }
            public String ConnectionRequest { get; set; }
            public String Status { get; set; }
        }
    }
}