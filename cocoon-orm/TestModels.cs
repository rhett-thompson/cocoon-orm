using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cocoon.ORM;

[Table]
public class WorkUnit
{


    [Column, PrimaryKey, IgnoreOnInsert, IgnoreOnUpdate]
    public int? WorkUnitID { get; set; }

    [Column]
    public int? ParentWorkUnitID { get; set; }

    [Column]
    public string DistrictID { get; set; }

    [Column]
    public string WorkUnitType { get; set; }

    [Column]
    public string WorkUnitAutoID { get; set; }

    [Column]
    public int? CreatedBy { get; set; }

    [Column]
    public DateTime? CreatedOnDate { get; set; }

    [Column]
    public int? AssignedToUser { get; set; }

    [Column]
    public DateTime? AssignedToUserOnDate { get; set; }

    [Column]
    public int? CheckedOutBy { get; set; }

    [Column]
    public DateTime? CheckedOutOnDate { get; set; }

    [Column]
    public int? ClosedBy { get; set; }

    [Column]
    public DateTime? ClosedByDate { get; set; }

    [Column]
    public int? ApprovedClosedBy { get; set; }

    [Column]
    public DateTime? ApprovedClosedByDate { get; set; }

    [Column]
    public bool IsEmergency { get; set; }

    [Column]
    public string Notes { get; set; }

    public string localPackageFile { get; set; }

}

[Table]
public class WorkUnitFileAttachment
{

    [Column, PrimaryKey, IgnoreOnUpdate]
    public string File { get; set; }

    [Column, IgnoreOnUpdate]
    public int WorkUnitID { get; set; }

    [Column, IgnoreOnInsert, IgnoreOnUpdate]
    public DateTime? CreatedDate { get; set; }

    [Column]
    public string WorkUnitFileAttachmentType { get; set; }

    [Column]
    public string OriginalFileName { get; set; }

    [Column]
    public string ContentType { get; set; }

    [Column]
    public int ContentLength { get; set; }

    [ForeignColumn("WorkUnitID", typeof(WorkUnit))]
    public string WorkUnitType { get; set; }

    [ForeignColumn("WorkUnitID", typeof(WorkUnit))]
    public string DistrictID { get; set; }

}

[Table]
public class PO
{

    [Column, PrimaryKey, IgnoreOnUpdate]
    public string PONum { get; set; }

    [Column]
    public DateTime PODate { get; set; }

    [Column]
    public string Notes { get; set; }

}
