using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Web.UI.WebControls;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Reporting;
using Rock;

namespace com.reallifeministries.Reporting.Group
{
    /// <summary>
    /// 
    /// </summary>
    [Description( "Select the Schedule of the Group" )]
    [Export( typeof( DataSelectComponent ) )]
    [ExportMetadata( "ComponentName", "Select Group's Schedule" )]
    public class ScheduleSelect : DataSelectComponent
    {
        #region Properties

        /// <summary>
        /// Gets the name of the entity type. Filter should be an empty string
        /// if it applies to all entities
        /// </summary>
        /// <value>
        /// The name of the entity type.
        /// </value>
        public override string AppliesToEntityType
        {
            get
            {
                return typeof( Rock.Model.Group ).FullName;
            }
        }

        /// <summary>
        /// Gets the section that this will appear in in the Field Selector
        /// </summary>
        /// <value>
        /// The section.
        /// </value>
        public override string Section
        {
            get
            {
                return base.Section;
            }
        }

        /// <summary>
        /// The PropertyName of the property in the anonymous class returned by the SelectExpression
        /// </summary>
        /// <value>
        /// The name of the column property.
        /// </value>
        public override string ColumnPropertyName
        {
            get
            {
                return "Schedule";
            }
        }

        /// <summary>
        /// Gets the type of the column field.
        /// </summary>
        /// <value>
        /// The type of the column field.
        /// </value>
        public override Type ColumnFieldType
        {
            get { return typeof( Rock.Model.Schedule ); }
        }

        /// <summary>
        /// Gets the default column header text.
        /// </summary>
        /// <value>
        /// The default column header text.
        /// </value>
        public override string ColumnHeaderText
        {
            get
            {
                return "Schedule";
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        /// <value>
        /// The title.
        /// </value>
        public override string GetTitle( Type entityType )
        {
            return "Schedule";
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="entityIdProperty">The entity identifier property.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override Expression GetExpression( RockContext context, MemberExpression entityIdProperty, string selection )
        {
            int? groupScheduleTypeValue = selection.AsInteger();

            var groupScheduleQuery = new GroupService( context ).Queryable()
                .Select( p => p.Schedule.Id == groupScheduleTypeValue);

            var selectExpression = SelectExpressionExtractor.Extract(groupScheduleQuery, entityIdProperty, "p" );

            return selectExpression;
        }

        /// <summary>
        /// Creates the child controls.
        /// </summary>
        /// <param name="parentControl"></param>
        /// <returns></returns>
        public override System.Web.UI.Control[] CreateChildControls( System.Web.UI.Control parentControl )
        {
            RockDropDownList scheduleTypeList = new RockDropDownList();
            scheduleTypeList.Items.Clear();
            foreach ( var value in DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.GROUP_LOCATION_TYPE.AsGuid() ).DefinedValues.OrderBy( a => a.Order ).ThenBy( a => a.Value ) )
            {
                scheduleTypeList.Items.Add( new ListItem( value.Value, value.Guid.ToString() ) );
            }

            scheduleTypeList.Items.Insert( 0, Rock.Constants.None.ListItem );

            scheduleTypeList.ID = parentControl.ID + "_groupscheduleType";
            scheduleTypeList.Label = "Group Type";
            parentControl.Controls.Add( scheduleTypeList );

            return new System.Web.UI.Control[] { scheduleTypeList };
        }

        /// <summary>
        /// Renders the controls.
        /// </summary>
        /// <param name="parentControl">The parent control.</param>
        /// <param name="writer">The writer.</param>
        /// <param name="controls">The controls.</param>
        public override void RenderControls( System.Web.UI.Control parentControl, System.Web.UI.HtmlTextWriter writer, System.Web.UI.Control[] controls )
        {
            base.RenderControls( parentControl, writer, controls );
        }

        /// <summary>
        /// Gets the selection.
        /// </summary>
        /// <param name="controls">The controls.</param>
        /// <returns></returns>
        public override string GetSelection( System.Web.UI.Control[] controls )
        {
            if ( controls.Count() == 1 )
            {
                RockDropDownList dropDownList = controls[0] as RockDropDownList;
                if ( dropDownList != null )
                {
                    return dropDownList.SelectedValue;
                }
            }

            return null;
        }

        /// <summary>
        /// Sets the selection.
        /// </summary>
        /// <param name="controls">The controls.</param>
        /// <param name="selection">The selection.</param>
        public override void SetSelection( System.Web.UI.Control[] controls, string selection )
        {
            if ( controls.Count() == 1 )
            {
                RockDropDownList dropDownList = controls[0] as RockDropDownList;
                if ( dropDownList != null )
                {
                    dropDownList.SetValue( selection );
                }
            }
        }

        #endregion
    }
}
